import json
import os
import statistics
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np

def aggregate_cpu_mem_results():
    script_dir = Path(__file__).parent
    base_dir = script_dir
    runs_dir = script_dir.parent / "benchmark_results"
    final_results_dir = base_dir / "final_results" / "cpu_mem"
    final_results_dir.mkdir(parents=True, exist_ok=True)
    
    # Собираем все run_i папки
    run_dirs = [d for d in runs_dir.iterdir() if d.is_dir() and d.name.startswith("run_")]
    run_dirs.sort()
    
    if not run_dirs:
        print("No run directories found!")
        return
    
    print(f"Found {len(run_dirs)} run directories")
    
    all_metrics = []
    all_stats = []
    
    # Читаем данные из всех прогонов
    for run_dir in run_dirs:
        cpu_mem_dir = run_dir / "cpu_mem_results"
        if not cpu_mem_dir.exists():
            print(f"Warning: {cpu_mem_dir} not found, skipping...")
            continue
            
        # Ищем файлы metrics и stats
        metrics_files = list(cpu_mem_dir.glob("*docker_metrics.json"))
        stats_files = list(cpu_mem_dir.glob("*docker_resource_stats.json"))
        
        if metrics_files:
            with open(metrics_files[0], 'r') as f:
                all_metrics.append(json.load(f))
        if stats_files:
            with open(stats_files[0], 'r') as f:
                all_stats.append(json.load(f))
    
    if not all_metrics or not all_stats:
        print("No data found to aggregate!")
        return
    
    # Агрегируем statistics
    aggregated_stats = aggregate_stats(all_stats)
    
    # Агрегируем metrics
    aggregated_metrics = aggregate_metrics(all_metrics)
    
    # Сохраняем агрегированные данные
    with open(final_results_dir / "docker_resource_stats.json", 'w') as f:
        json.dump(aggregated_stats, f, indent=2)
    
    with open(final_results_dir / "docker_metrics.json", 'w') as f:
        json.dump(aggregated_metrics, f, indent=2)
    
    # Строим графики
    create_plots(aggregated_metrics, final_results_dir)
    
    print(f"Aggregated data saved to {final_results_dir}")

def aggregate_stats(all_stats):
    """Агрегирует статистические данные по всем прогонам"""
    services = ['webcli', 'postgres']
    metrics = ['cpu', 'memory']
    stats_types = ['min', 'max', 'average', 'median']
    
    aggregated = {}
    
    for service in services:
        aggregated[service] = {}
        for metric in metrics:
            aggregated[service][metric] = {}
            for stat_type in stats_types:
                # Собираем все значения для данного типа статистики
                values = []
                for stats in all_stats:
                    if service in stats and metric in stats[service]:
                        values.append(stats[service][metric][stat_type])
                
                if values:
                    if stat_type == 'min':
                        aggregated[service][metric][stat_type] = min(values)
                    elif stat_type == 'max':
                        aggregated[service][metric][stat_type] = max(values)
                    elif stat_type == 'average':
                        aggregated[service][metric][stat_type] = statistics.mean(values)
                    elif stat_type == 'median':
                        aggregated[service][metric][stat_type] = statistics.median(values)
            
            # Добавляем количество образцов (берем среднее)
            sample_counts = []
            for stats in all_stats:
                if service in stats and metric in stats[service]:
                    sample_counts.append(stats[service][metric].get('samples', 0))
            
            if sample_counts:
                aggregated[service][metric]['samples'] = int(statistics.mean(sample_counts))
    
    return aggregated

def aggregate_metrics(all_metrics):
    """Агрегирует метрики по всем прогонам"""
    services = ['webcli', 'postgres']
    metrics = ['cpu', 'memory']
    
    aggregated = {
        "container_map": all_metrics[0]["container_map"],
        "metrics_data": {},
        "collection_info": {
            "duration_seconds": all_metrics[0]["collection_info"]["duration_seconds"],
            "samples_per_service": 0,
            "timestamp": "aggregated",
            "total_runs": len(all_metrics)
        }
    }
    
    for service in services:
        aggregated["metrics_data"][service] = {}
        for metric in metrics:
            # Собираем все временные ряды для данной метрики
            all_series = []
            max_length = 0
            
            for metrics_data in all_metrics:
                if (service in metrics_data["metrics_data"] and 
                    metric in metrics_data["metrics_data"][service]):
                    series = metrics_data["metrics_data"][service][metric]
                    all_series.append(series)
                    max_length = max(max_length, len(series))
            
            if all_series:
                # Выравниваем все ряды по длине (дополняем последним значением)
                aligned_series = []
                for series in all_series:
                    if len(series) < max_length:
                        # Дополняем последним значением
                        padded_series = series + [series[-1]] * (max_length - len(series))
                        aligned_series.append(padded_series)
                    else:
                        aligned_series.append(series[:max_length])
                
                # Усредняем по всем прогонам
                averaged_series = []
                for i in range(max_length):
                    values_at_point = [series[i] for series in aligned_series]
                    averaged_series.append(statistics.mean(values_at_point))
                
                aggregated["metrics_data"][service][metric] = averaged_series
    
    # Обновляем количество образцов
    for service in services:
        for metric in metrics:
            if service in aggregated["metrics_data"] and metric in aggregated["metrics_data"][service]:
                aggregated["collection_info"]["samples_per_service"] = len(
                    aggregated["metrics_data"][service][metric]
                )
                break
    
    return aggregated

def create_plots(aggregated_metrics, output_dir):
    """Создает графики CPU и памяти для webcli и postgres"""
    services = ['webcli', 'postgres']
    metrics = ['cpu', 'memory']
    metric_names = {'cpu': 'CPU Usage (%)', 'memory': 'Memory Usage (MB)'}
    
    # Настройка стиля графиков
    plt.style.use('default')
    fig, axes = plt.subplots(2, 2, figsize=(15, 10))
    fig.suptitle('CPU and Memory Usage Metrics (Aggregated across all runs)', fontsize=16, fontweight='bold')
    
    for i, service in enumerate(services):
        for j, metric in enumerate(metrics):
            ax = axes[j, i]
            
            if (service in aggregated_metrics["metrics_data"] and 
                metric in aggregated_metrics["metrics_data"][service]):
                
                data = aggregated_metrics["metrics_data"][service][metric]
                time_points = list(range(len(data)))
                
                ax.plot(time_points, data, linewidth=2, 
                       label=f'{service} {metric}', 
                       color='blue' if service == 'webcli' else 'red')
                
                ax.set_title(f'{service.upper()} - {metric_names[metric]}', fontweight='bold')
                ax.set_xlabel('Time Sample')
                ax.set_ylabel(metric_names[metric])
                ax.grid(True, alpha=0.3)
                ax.legend()
                
                # Добавляем статистику на график
                avg_value = statistics.mean(data)
                max_value = max(data)
                min_value = min(data)
                
                stats_text = f'Avg: {avg_value:.2f}\nMax: {max_value:.2f}\nMin: {min_value:.2f}'
                ax.text(0.02, 0.98, stats_text, transform=ax.transAxes, 
                       verticalalignment='top', bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig(output_dir / 'cpu_memory_metrics.png', dpi=300, bbox_inches='tight')
    plt.close()
    
    # Создаем отдельные графики для каждого сервиса и метрики
    for service in services:
        for metric in metrics:
            if (service in aggregated_metrics["metrics_data"] and 
                metric in aggregated_metrics["metrics_data"][service]):
                
                plt.figure(figsize=(10, 6))
                data = aggregated_metrics["metrics_data"][service][metric]
                time_points = list(range(len(data)))
                
                plt.plot(time_points, data, linewidth=2.5, 
                        color='blue' if service == 'webcli' else 'red',
                        label=f'{service} {metric}')
                
                plt.title(f'{service.upper()} - {metric_names[metric]}\n(Aggregated across {aggregated_metrics["collection_info"]["total_runs"]} runs)', 
                         fontweight='bold', fontsize=14)
                plt.xlabel('Time Sample')
                plt.ylabel(metric_names[metric])
                plt.grid(True, alpha=0.3)
                plt.legend()
                
                # Добавляем статистику
                avg_value = statistics.mean(data)
                max_value = max(data)
                min_value = min(data)
                std_value = statistics.stdev(data) if len(data) > 1 else 0
                
                stats_text = f'Average: {avg_value:.2f}\nMaximum: {max_value:.2f}\nMinimum: {min_value:.2f}\nStd Dev: {std_value:.2f}'
                plt.figtext(0.02, 0.02, stats_text, fontsize=10, 
                           bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
                
                plt.tight_layout()
                filename = f'{service}_{metric}_metrics.png'
                plt.savefig(output_dir / filename, dpi=300, bbox_inches='tight')
                plt.close()
    
    print(f"Plots saved to {output_dir}")

if __name__ == "__main__":
    aggregate_cpu_mem_results()