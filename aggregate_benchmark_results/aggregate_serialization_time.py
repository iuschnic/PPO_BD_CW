import json
import re
import statistics
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np
from collections import defaultdict

def aggregate_serialization_results():
    # Пути
    script_dir = Path(__file__).parent
    base_dir = script_dir.parent / "benchmark_results"
    final_results_dir = script_dir / "final_results" / "serialization_time"
    final_results_dir.mkdir(parents=True, exist_ok=True)
    
    # Собираем все run_i папки
    run_dirs = [d for d in base_dir.iterdir() if d.is_dir() and d.name.startswith("run_")]
    run_dirs.sort()
    
    if not run_dirs:
        print("No run directories found!")
        return
    
    print(f"Found {len(run_dirs)} run directories")
    
    # Словарь для хранения данных по DTO и номерам замеров
    all_serialization_data = defaultdict(lambda: defaultdict(list))
    
    # Читаем данные из всех прогонов
    for run_dir in run_dirs:
        serialization_dir = run_dir / "serialization_time_results"
        if not serialization_dir.exists():
            print(f"Warning: {serialization_dir} not found, skipping...")
            continue
            
        # Ищем файлы логов
        log_files = list(serialization_dir.glob("*.log"))
        
        for log_file in log_files:
            process_log_file(log_file, all_serialization_data)
    
    if not all_serialization_data:
        print("No serialization data found!")
        return
    
    # Усредняем данные по номерам замеров
    averaged_data = average_measurements(all_serialization_data)
    
    # Строим отдельные графики для UserDto и DistrResultDto
    create_individual_plots(averaged_data, final_results_dir)
    
    print(f"Serialization analysis completed! Results saved to {final_results_dir}")

def process_log_file(log_file, all_serialization_data):
    """Обрабатывает файл лога и извлекает данные сериализации с порядковыми номерами"""
    
    # Регулярное выражение для извлечения данных сериализации
    serialize_pattern = r'\[BENCHMARK_SERIALIZE\] Type: "([^"]+)", TimeNs: ([\d.]+), SizeBytes: (\d+)'
    
    with open(log_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
        # Счетчики для каждого DTO типа
        dto_counters = defaultdict(int)
        
        for line in lines:
            # Ищем только записи о сериализации
            if '[BENCHMARK_SERIALIZE]' in line:
                match = re.search(serialize_pattern, line)
                if match:
                    dto_type = match.group(1)
                    time_ns = float(match.group(2))
                    size_bytes = int(match.group(3))
                    
                    # Получаем порядковый номер для этого DTO
                    measurement_number = dto_counters[dto_type]
                    dto_counters[dto_type] += 1
                    
                    # Сохраняем данные с порядковым номером
                    all_serialization_data[dto_type][measurement_number].append({
                        'time_ns': time_ns,
                        'size_bytes': size_bytes
                    })

def average_measurements(all_serialization_data):
    """Усредняет данные по порядковым номерам замеров"""
    averaged_data = {}
    
    for dto_type, measurements_by_number in all_serialization_data.items():
        # Находим максимальный номер замера
        max_measurement = max(measurements_by_number.keys()) if measurements_by_number else -1
        
        if max_measurement < 0:
            continue
            
        avg_times = []
        std_times = []
        
        # Усредняем для каждого номера замера
        for measurement_num in range(max_measurement + 1):
            if measurement_num in measurements_by_number:
                measurements = measurements_by_number[measurement_num]
                times = [m['time_ns'] for m in measurements]
                
                avg_times.append(statistics.mean(times))
                std_times.append(statistics.stdev(times) if len(times) > 1 else 0)
        
        if avg_times:  # Если есть данные для усреднения
            averaged_data[dto_type] = {
                'measurement_numbers': list(range(len(avg_times))),
                'avg_times': avg_times,
                'std_times': std_times,
                'total_runs': len(set().union(*[set()] + [list(measurements_by_number.keys())])),
                'total_measurements': len(avg_times)
            }
    
    return averaged_data

def create_individual_plots(averaged_data, output_dir):
    """Создает отдельные графики для UserDto и DistrResultDto"""
    
    # Фильтруем только нужные DTO
    target_dtos = ['UserDto', 'DistributionResultDto']
    filtered_data = {dto: data for dto, data in averaged_data.items() if dto in target_dtos}
    
    if not filtered_data:
        print(f"None of the target DTOs found. Available DTOs: {list(averaged_data.keys())}")
        return
    
    # Цвета для графиков
    colors = {'UserDto': 'blue', 'DistributionResultDto': 'red'}
    
    # Строим отдельные графики для каждого DTO
    for dto_type, data in filtered_data.items():
        plt.figure(figsize=(12, 6))
        
        measurement_numbers = data['measurement_numbers']
        avg_times = data['avg_times']
        std_times = data['std_times']
        
        # Основная линия - среднее время
        plt.plot(measurement_numbers, avg_times, 
                linewidth=2, color=colors[dto_type], marker='o', markersize=3, label='Average Time')
        
        # Область стандартного отклонения
        plt.fill_between(measurement_numbers,
                        np.array(avg_times) - np.array(std_times),
                        np.array(avg_times) + np.array(std_times),
                        alpha=0.3, color=colors[dto_type], label='±1 Std Dev')
        
        plt.xlabel('Measurement Number (Sequential Order)', fontsize=12)
        plt.ylabel('Serialization Time (nanoseconds)', fontsize=12)
        plt.title(f'{dto_type} - Serialization Time vs Measurement Number\n(Averaged across {data["total_runs"]} runs)', 
                 fontsize=14, fontweight='bold')
        plt.legend()
        plt.grid(True, alpha=0.3)
        
        # Статистика для этого DTO
        avg_time = statistics.mean(avg_times)
        max_time = max(avg_times)
        min_time = min(avg_times)
        avg_std = statistics.mean(std_times)
        
        stats_text = f'Average: {avg_time:.1f} ns\nMin: {min_time:.1f} ns\nMax: {max_time:.1f} ns\nAvg Std: {avg_std:.1f} ns\nMeasurements: {data["total_measurements"]}'
        
        plt.text(0.02, 0.98, stats_text, transform=plt.gca().transAxes, verticalalignment='top',
                 bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
        
        plt.tight_layout()
        plt.savefig(output_dir / f'{dto_type.lower()}_serialization_time.png', dpi=300, bbox_inches='tight')
        plt.close()
        
        print(f"Created plot for {dto_type}")

if __name__ == "__main__":
    aggregate_serialization_results()