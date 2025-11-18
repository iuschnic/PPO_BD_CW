import json
import os
import statistics
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np
import sys

'''def aggregate_cpu_mem_results():
    script_dir = Path(__file__).parent
    base_dir = script_dir
    runs_dir = script_dir.parent / "benchmark_results"
    final_results_dir = base_dir / "final_results" / "cpu_mem"
    final_results_dir.mkdir(parents=True, exist_ok=True)'''
def aggregate_cpu_mem_results(input_directory):    
    script_dir = Path(__file__).parent
    base_dir = Path(input_directory)
    if not base_dir.exists():
        print(f"Error: Input directory '{base_dir}' does not exist!")
        return
    final_results_dir = script_dir / f"{input_directory}_aggregated" / "cpu_mem"
    final_results_dir.mkdir(parents=True, exist_ok=True)
    
    # Собираем все run_i папки
    run_dirs = [d for d in base_dir.iterdir() if d.is_dir() and d.name.startswith("run_")]
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
        "metrics_std": {},
        "collection_info": {
            "duration_seconds": all_metrics[0]["collection_info"]["duration_seconds"],
            "samples_per_service": 0,
            "timestamp": "aggregated",
            "total_runs": len(all_metrics)
        }
    }
    
    for service in services:
        aggregated["metrics_data"][service] = {}
        aggregated["metrics_std"][service] = {}
        
        for metric in metrics:
            all_series = []
            max_length = 0
            
            for metrics_data in all_metrics:
                if (service in metrics_data["metrics_data"] and 
                    metric in metrics_data["metrics_data"][service]):
                    series = metrics_data["metrics_data"][service][metric]
                    all_series.append(series)
                    max_length = max(max_length, len(series))
            
            if all_series:
                print(f"\n=== Processing {service} {metric} ===")
                print(f"Number of runs: {len(all_series)}")
                
                # Выравниваем все ряды по длине (дополняем последним значением)
                aligned_series = []
                for run_idx, series in enumerate(all_series):
                    if len(series) < max_length:
                        padded_series = series + [series[-1]] * (max_length - len(series))
                        aligned_series.append(padded_series)
                    else:
                        aligned_series.append(series[:max_length])
                
                averaged_series = []
                std_series = []
                
                for i in range(max_length):
                    values_at_point = [series[i] for series in aligned_series]
                    avg_value = statistics.mean(values_at_point)
                    averaged_series.append(avg_value)
                    
                    if len(values_at_point) > 1:
                        std_value = statistics.stdev(values_at_point)
                    else:
                        std_value = 0
                    std_series.append(std_value)
                
                aggregated["metrics_data"][service][metric] = averaged_series
                aggregated["metrics_std"][service][metric] = std_series
                
                print(f"Std values for first 10 points: {[round(s, 4) for s in std_series[:10]]}")
                print(f"Std values for last 10 points: {[round(s, 4) for s in std_series[-10:]]}")
    
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
    
    sampling_interval = 4
    
    plt.style.use('default')
    fig, axes = plt.subplots(2, 2, figsize=(15, 10))
    fig.suptitle('CPU and Memory Usage Metrics (Aggregated across all runs)', fontsize=16, fontweight='bold')
    
    for i, service in enumerate(services):
        for j, metric in enumerate(metrics):
            ax = axes[j, i]
            
            if (service in aggregated_metrics["metrics_data"] and 
                metric in aggregated_metrics["metrics_data"][service] and
                service in aggregated_metrics["metrics_std"] and
                metric in aggregated_metrics["metrics_std"][service]):
                
                data = aggregated_metrics["metrics_data"][service][metric]
                std_data = aggregated_metrics["metrics_std"][service][metric]
                
                # ПРОВЕРКА: убедимся, что std_data содержит разные значения
                print(f"\n=== Plotting {service} {metric} ===")
                print(f"Data length: {len(data)}, Std data length: {len(std_data)}")
                print(f"First 5 std values: {[round(s, 4) for s in std_data[:5]]}")
                print(f"Last 5 std values: {[round(s, 4) for s in std_data[-5:]]}")
                
                time_points = [t * sampling_interval for t in range(len(data))]
                
                ax.plot(time_points, data, linewidth=2, 
                       label=f'{service} {metric}', 
                       color='blue' if service == 'webcli' else 'red')
                
                ax.fill_between(time_points, 
                               [data[i] - std_data[i] for i in range(len(data))],
                               [data[i] + std_data[i] for i in range(len(data))],
                               alpha=0.3, color='blue' if service == 'webcli' else 'red',
                               label='±1 Std Dev')
                
                ax.set_title(f'{service.upper()} - {metric_names[metric]}', fontweight='bold')
                ax.set_xlabel('Time (seconds)')
                ax.set_ylabel(metric_names[metric])
                ax.grid(True, alpha=0.3)
                ax.legend()
                
                avg_value = statistics.mean(data)
                max_value = max(data)
                min_value = min(data)
                avg_std = statistics.mean(std_data)
                
                stats_text = f'Avg: {avg_value:.2f}\nMax: {max_value:.2f}\nMin: {min_value:.2f}\nAvg Std: {avg_std:.2f}'
                ax.text(0.02, 0.98, stats_text, transform=ax.transAxes, 
                       verticalalignment='top', bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig(output_dir / 'cpu_memory_metrics.png', dpi=300, bbox_inches='tight')
    plt.close()
    
    # Создаем отдельные графики для каждого сервиса и метрики
    for service in services:
        for metric in metrics:
            if (service in aggregated_metrics["metrics_data"] and 
                metric in aggregated_metrics["metrics_data"][service] and
                service in aggregated_metrics["metrics_std"] and
                metric in aggregated_metrics["metrics_std"][service]):
                
                plt.figure(figsize=(10, 6))
                data = aggregated_metrics["metrics_data"][service][metric]
                std_data = aggregated_metrics["metrics_std"][service][metric]
                
                # Создаем временную шкалу: каждый замер через 5 секунд
                time_points = [t * sampling_interval for t in range(len(data))]
                
                plt.plot(time_points, data, linewidth=2.5, 
                        color='blue' if service == 'webcli' else 'red',
                        label=f'{service} {metric}')
                
                # Добавляем область стандартного отклонения (РАЗНОЕ для каждой точки)
                plt.fill_between(time_points, 
                                [data[i] - std_data[i] for i in range(len(data))],
                                [data[i] + std_data[i] for i in range(len(data))],
                                alpha=0.3, color='blue' if service == 'webcli' else 'red',
                                label='±1 Std Dev')
                
                plt.title(f'{service.upper()} - {metric_names[metric]}\n(Aggregated across {aggregated_metrics["collection_info"]["total_runs"]} runs)', 
                         fontweight='bold', fontsize=14)
                plt.xlabel('Time (seconds)')
                plt.ylabel(metric_names[metric])
                plt.grid(True, alpha=0.3)
                plt.legend()
                
                # Добавляем статистику
                avg_value = statistics.mean(data)
                max_value = max(data)
                min_value = min(data)
                avg_std = statistics.mean(std_data)
                
                stats_text = f'Average: {avg_value:.2f}\nMaximum: {max_value:.2f}\nMinimum: {min_value:.2f}\nAvg Std Dev: {avg_std:.2f}'
                plt.figtext(0.02, 0.02, stats_text, fontsize=10, 
                           bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
                
                plt.tight_layout()
                filename = f'{service}_{metric}_metrics.png'
                plt.savefig(output_dir / filename, dpi=300, bbox_inches='tight')
                plt.close()
    
    print(f"Plots saved to {output_dir}")

'''if __name__ == "__main__":
    aggregate_cpu_mem_results()'''
def main():
    if len(sys.argv) > 1:
        input_directory = sys.argv[1]
        print(f"Processing cpu mem results from: {input_directory}")
        aggregate_cpu_mem_results(input_directory)
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()