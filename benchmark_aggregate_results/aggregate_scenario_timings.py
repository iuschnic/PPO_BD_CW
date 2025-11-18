import json
import pandas as pd
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np
import sys

def aggregate_scenario_timings(input_directory):    
    script_dir = Path(__file__).parent
    base_dir = Path(input_directory)
    if not base_dir.exists():
        print(f"Error: Input directory '{base_dir}' does not exist!")
        return
    final_results_dir = script_dir / f"{input_directory}_aggregated" / "http_time_results"
    final_results_dir.mkdir(parents=True, exist_ok=True)
    
    # Собираем все run_i папки
    run_dirs = [d for d in base_dir.iterdir() if d.is_dir() and d.name.startswith("run_")]
    run_dirs.sort()
    
    if not run_dirs:
        print("No run directories found!")
        return
    
    print(f"Found {len(run_dirs)} run directories")
    
    # Собираем данные в виде списка списков (каждый внутренний список - один run_i)
    all_runs_data = []
    all_runs_timestamps = []  # Новый список для временных меток
    
    for run_dir in run_dirs:
        jsonl_file = run_dir / "http_requests_summary" / "scenario_timings.jsonl"
        
        if jsonl_file.exists():
            try:
                content = read_file_with_bom(jsonl_file)
                lines = content.splitlines()
                
                run_entries = []
                for line in lines:
                    if line.strip():
                        data = json.loads(line.strip())
                        run_entries.append({
                            'timestamp': data['timestamp'],
                            'duration': data['duration']
                        })
                
                # Сортируем записи по timestamp
                run_entries.sort(key=lambda x: x['timestamp'])
                
                # Разделяем на отдельные списки
                timestamps = [entry['timestamp'] for entry in run_entries]
                durations = [entry['duration'] for entry in run_entries]
                
                all_runs_timestamps.append(timestamps)
                all_runs_data.append(durations)
                
            except Exception as e:
                print(f"Error loading {jsonl_file}: {e}")
        else:
            print(f"File not found: {jsonl_file}")
    
    if not all_runs_data:
        print("No scenario timing data found!")
        return
    
    # Проверяем, что во всех прогонах одинаковое количество замеров
    lengths = [len(run) for run in all_runs_data]
    if len(set(lengths)) > 1:
        print(f"Warning: Different number of measurements in runs: {lengths}")
        # Берем минимальное количество замеров
        min_length = min(lengths)
        all_runs_data = [run[:min_length] for run in all_runs_data]
        all_runs_timestamps = [ts[:min_length] for ts in all_runs_timestamps]
        print(f"Using first {min_length} measurements from each run")
    else:
        min_length = lengths[0]
    
    # Конвертируем все timestamp в относительное время в секундах ОТНОСИТЕЛЬНО НАЧАЛА КАЖДОГО ПРОГОНА
    all_runs_relative_times = []
    for timestamps in all_runs_timestamps:
        if not timestamps:
            continue
        # Находим самый ранний timestamp в данном прогоне
        earliest_in_run = min(timestamps)
        relative_times = []
        for ts in timestamps:
            # Относительное время от начала данного прогона
            relative_time = (ts - earliest_in_run) / 1000.0
            relative_times.append(relative_time)
        all_runs_relative_times.append(relative_times)
    
    # Создаем DataFrame для усреднения duration
    df_durations = pd.DataFrame(all_runs_data).T  # Строки - номера замеров, столбцы - run_i
    
    # Создаем DataFrame для временных меток (берем среднее время для каждого измерения)
    df_times = pd.DataFrame(all_runs_relative_times).T
    
    # Вычисляем статистику для каждого номера замера
    avg_duration = df_durations.mean(axis=1)
    std_duration = df_durations.std(axis=1)
    min_duration = df_durations.min(axis=1)
    max_duration = df_durations.max(axis=1)
    
    # Вычисляем среднее относительное время для каждого измерения
    avg_relative_time = df_times.mean(axis=1)
    
    # Создаем графики с передачей временных меток
    create_plots(avg_duration, std_duration, min_duration, max_duration, 
                len(all_runs_data), min_length, final_results_dir, avg_relative_time)
    create_distribution_plots(all_runs_data, final_results_dir, len(all_runs_data))
    
    print(f"Graphs saved to {final_results_dir}")


def read_file_with_bom(file_path):
    """Читает файл с учетом возможного BOM"""
    encodings = ['utf-8-sig', 'utf-8', 'latin-1']
    
    for encoding in encodings:
        try:
            with open(file_path, 'r', encoding=encoding) as f:
                return f.read()
        except UnicodeDecodeError:
            continue
    
    with open(file_path, 'rb') as f:
        content = f.read()
        if content.startswith(b'\xef\xbb\xbf'):
            content = content[3:]
        return content.decode('utf-8', errors='ignore')

def create_plots(avg_duration, std_duration, min_duration, max_duration, runs_count, measurements_count, output_dir, avg_relative_time):
    """Создает графики усредненного времени выполнения"""
    
    plt.style.use('default')
    
    # Подготовка данных для обоих графиков
    time_points = [t for t in avg_relative_time]
    measurement_numbers = list(range(len(avg_duration)))
    
    # График 1: По времени
    plt.figure(figsize=(14, 8))
    
    plt.plot(time_points, avg_duration, 
             label='Average Duration', linewidth=3, color='blue', marker='o', markersize=4)
    
    plt.fill_between(time_points, 
                    avg_duration - std_duration,
                    avg_duration + std_duration,
                    alpha=0.3, color='blue', label='±1 Std Dev')
    
    plt.xlabel('Time (seconds)', fontsize=12)
    plt.ylabel('Duration (ms)', fontsize=12)
    plt.title('Average Scenario Duration vs Time\n(Averaged across all runs)', fontsize=14, fontweight='bold')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    stats_text = f'Runs: {runs_count}\nMeasurements per run: {measurements_count}\nOverall average: {avg_duration.mean():.1f}ms'
    plt.text(0.02, 0.98, stats_text, transform=plt.gca().transAxes, verticalalignment='top',
             bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig(output_dir / 'averaged_duration_vs_time.png', dpi=300, bbox_inches='tight')
    plt.close()
    
    # График 2: По номеру измерения
    plt.figure(figsize=(14, 8))
    
    plt.plot(measurement_numbers, avg_duration, 
             label='Average Duration', linewidth=3, color='green', marker='o', markersize=4)
    
    plt.fill_between(measurement_numbers, 
                    avg_duration - std_duration,
                    avg_duration + std_duration,
                    alpha=0.3, color='green', label='±1 Std Dev')
    
    plt.xlabel('Measurement Number (Sequential Order)', fontsize=12)
    plt.ylabel('Duration (ms)', fontsize=12)
    plt.title('Average Scenario Duration vs Measurement Number\n(Averaged across all runs)', fontsize=14, fontweight='bold')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    stats_text = f'Runs: {runs_count}\nMeasurements per run: {measurements_count}\nOverall average: {avg_duration.mean():.1f}ms'
    plt.text(0.02, 0.98, stats_text, transform=plt.gca().transAxes, verticalalignment='top',
             bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig(output_dir / 'averaged_duration_vs_measurement.png', dpi=300, bbox_inches='tight')
    plt.close()
    
    # Дополнительный график: оба на одном (subplots)
    fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(14, 12))
    
    # Верхний график - по времени
    ax1.plot(time_points, avg_duration, 
             label='Average Duration', linewidth=2, color='blue', marker='o', markersize=3)
    ax1.fill_between(time_points, 
                    avg_duration - std_duration,
                    avg_duration + std_duration,
                    alpha=0.3, color='blue', label='±1 Std Dev')
    ax1.set_xlabel('Time (seconds)', fontsize=11)
    ax1.set_ylabel('Duration (ms)', fontsize=11)
    ax1.set_title('Average Scenario Duration vs Time', fontsize=13, fontweight='bold')
    ax1.legend()
    ax1.grid(True, alpha=0.3)
    
    # Нижний график - по номеру измерения
    ax2.plot(measurement_numbers, avg_duration, 
             label='Average Duration', linewidth=2, color='green', marker='o', markersize=3)
    ax2.fill_between(measurement_numbers, 
                    avg_duration - std_duration,
                    avg_duration + std_duration,
                    alpha=0.3, color='green', label='±1 Std Dev')
    ax2.set_xlabel('Measurement Number (Sequential Order)', fontsize=11)
    ax2.set_ylabel('Duration (ms)', fontsize=11)
    ax2.set_title('Average Scenario Duration vs Measurement Number', fontsize=13, fontweight='bold')
    ax2.legend()
    ax2.grid(True, alpha=0.3)
    
    
def create_distribution_plots(all_durations, output_dir, runs_count):
    """Создает графики распределения длительностей и перцентилей"""
    
    # Объединяем все измерения в один плоский список
    all_durations_flat = [duration for sublist in all_durations for duration in sublist]
    
    if not all_durations_flat:
        print("No duration data for distribution plots!")
        return
    
    # Рассчитываем перцентили
    percentiles = [50, 75, 90, 95, 99]
    percentile_values = np.percentile(all_durations_flat, percentiles)
    
    plt.style.use('default')
    
    # График 1: Гистограмма распределения
    plt.figure(figsize=(14, 8))
    
    # Автоматическое определение бинов
    n, bins, patches = plt.hist(all_durations_flat, bins=50, alpha=0.7, 
                               color='skyblue', edgecolor='black', linewidth=0.5)
    
    # Добавляем вертикальные линии для перцентилей
    colors = ['red', 'orange', 'green', 'blue', 'purple']
    for i, (p, value) in enumerate(zip(percentiles, percentile_values)):
        plt.axvline(value, color=colors[i], linestyle='--', linewidth=2, 
                   label=f'P{p}: {value:.1f}ms')
    
    plt.xlabel('Duration (ms)', fontsize=12)
    plt.ylabel('Frequency', fontsize=12)
    plt.title('Distribution of user scenario durations\n(All measurements from all runs)', 
              fontsize=14, fontweight='bold')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    stats_text = (f'Total measurements: {len(all_durations_flat):,}\n'
                  f'Runs: {runs_count}\n'
                  f'Mean: {np.mean(all_durations_flat):.1f}ms\n'
                  f'Std: {np.std(all_durations_flat):.1f}ms\n'
                  f'Min: {np.min(all_durations_flat):.1f}ms\n'
                  f'Max: {np.max(all_durations_flat):.1f}ms')
    
    plt.text(0.02, 0.98, stats_text, transform=plt.gca().transAxes, 
             verticalalignment='top', fontsize=10,
             bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig(output_dir / 'duration_distribution_histogram.png', dpi=300, bbox_inches='tight')
    plt.close()
    
    # График 2: Детальный перцентильный анализ
    plt.figure(figsize=(14, 8))
    
    # Расширенный набор перцентилей для детального анализа
    detailed_percentiles = np.arange(0, 101, 1)
    detailed_values = np.percentile(all_durations_flat, detailed_percentiles)
    
    plt.plot(detailed_percentiles, detailed_values, linewidth=2, color='purple')
    plt.xlabel('Percentile', fontsize=12)
    plt.ylabel('Duration (ms)', fontsize=12)
    plt.title('Percentile Analysis of Scenario Durations', 
              fontsize=14, fontweight='bold')
    plt.grid(True, alpha=0.3)
    
    # Выделяем ключевые перцентили
    key_percentiles = [50, 75, 90, 95, 99, 99.9]
    key_values = np.percentile(all_durations_flat, key_percentiles)
    
    for p, value in zip(key_percentiles, key_values):
        plt.plot(p, value, 'ro', markersize=8)
        plt.annotate(f'P{p}: {value:.1f}ms', 
                    xy=(p, value), xytext=(10, 10),
                    textcoords='offset points', fontsize=9,
                    bbox=dict(boxstyle='round,pad=0.3', facecolor='white', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig(output_dir / 'detailed_percentile_analysis.png', dpi=300, bbox_inches='tight')
    plt.close()
    
    # Сохраняем перцентили в CSV файл
    percentile_data = {
        'Percentile': percentiles + [99.9],
        'Duration_ms': list(percentile_values) + [np.percentile(all_durations_flat, 99.9)]
    }
    percentile_df = pd.DataFrame(percentile_data)
    percentile_df.to_csv(output_dir / 'percentile_analysis.csv', index=False)
    
    # Выводим статистику в консоль
    print("\n=== PERCENTILE ANALYSIS ===")
    print(f"Total measurements: {len(all_durations_flat):,}")
    print(f"P50 (median): {percentile_values[0]:.1f}ms")
    print(f"P75: {percentile_values[1]:.1f}ms")
    print(f"P90: {percentile_values[2]:.1f}ms")
    print(f"P95: {percentile_values[3]:.1f}ms")
    print(f"P99: {percentile_values[4]:.1f}ms")
    print(f"P99.9: {np.percentile(all_durations_flat, 99.9):.1f}ms")
    
    
    
def main():
    if len(sys.argv) > 1:
        input_directory = sys.argv[1]
        print(f"Processing scenario timings results from: {input_directory}")
        aggregate_scenario_timings(input_directory)
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()