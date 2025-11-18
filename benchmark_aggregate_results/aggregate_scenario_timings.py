import json
import pandas as pd
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np
import sys

'''def aggregate_scenario_timings():
    # Пути
    script_dir = Path(__file__).parent
    base_dir = script_dir.parent / "benchmark_results"
    final_results_dir = script_dir / "final_results" / "http_time_results"
    final_results_dir.mkdir(parents=True, exist_ok=True)'''
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
    
    for run_dir in run_dirs:
        jsonl_file = run_dir / "http_requests_summary" / "scenario_timings.jsonl"
        
        if jsonl_file.exists():
            try:
                content = read_file_with_bom(jsonl_file)
                lines = content.splitlines()
                
                run_durations = []
                for line in lines:
                    if line.strip():
                        data = json.loads(line.strip())
                        run_durations.append(data['duration'])
                
                all_runs_data.append(run_durations)
                
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
        print(f"Using first {min_length} measurements from each run")
    else:
        min_length = lengths[0]
    
    # Создаем DataFrame для усреднения
    df = pd.DataFrame(all_runs_data).T  # Транспонируем: строки - номера замеров, столбцы - run_i
    
    # Вычисляем статистику для каждого номера замера
    avg_duration = df.mean(axis=1)
    std_duration = df.std(axis=1)
    min_duration = df.min(axis=1)
    max_duration = df.max(axis=1)
    
    # Создаем графики
    create_plots(avg_duration, std_duration, min_duration, max_duration, len(all_runs_data), min_length, final_results_dir)
    
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

def create_plots(avg_duration, std_duration, min_duration, max_duration, runs_count, measurements_count, output_dir):
    """Создает графики усредненного времени выполнения"""
    
    plt.style.use('default')
    
    sampling_interval = 0.1
    
    # Подготовка данных для обоих графиков
    time_points = [t * sampling_interval for t in range(len(avg_duration))]
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
    
    stats_text = f'Runs: {runs_count}\nMeasurements per run: {measurements_count}\nSampling interval: {sampling_interval}s\nOverall average: {avg_duration.mean():.1f}ms'
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
    
    
    
'''if __name__ == "__main__":
    aggregate_scenario_timings()'''
def main():
    if len(sys.argv) > 1:
        input_directory = sys.argv[1]
        print(f"Processing scenario timings results from: {input_directory}")
        aggregate_scenario_timings(input_directory)
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()