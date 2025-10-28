#!/usr/bin/env python3
import docker
import json
import time
import statistics
import os
from datetime import datetime
import matplotlib.pyplot as plt

class DockerMetricsCollector:
    def __init__(self):
        self.client = docker.from_env()
        self.container_map = {}
        self.metrics_data = {}
        self.setup_container_mapping()
        
    def setup_container_mapping(self):
        """Автоматически находим контейнеры по именам сервисов"""
        try:
            containers = self.client.containers.list()
            print("Available containers:")
            for container in containers:
                print(f"  - {container.name} (Status: {container.status})")
                if 'webcli' in container.name.lower():
                    self.container_map['webcli'] = container.name
                    self.metrics_data['webcli'] = {'cpu': [], 'memory': []}
                elif 'postgres' in container.name.lower():
                    self.container_map['postgres'] = container.name
                    self.metrics_data['postgres'] = {'cpu': [], 'memory': []}
            
            print(f"Found containers: {self.container_map}")
            
        except Exception as e:
            print(f"Error listing containers: {e}")
    
    '''def get_container_stats(self, container_name):
        """Получение метрик контейнера через Docker API"""
        try:
            container = self.client.containers.get(container_name)
            stats = container.stats(stream=False)
            
            # CPU calculation
            cpu_delta = stats['cpu_stats']['cpu_usage']['total_usage'] - stats['precpu_stats']['cpu_usage']['total_usage']
            system_delta = stats['cpu_stats']['system_cpu_usage'] - stats['precpu_stats']['system_cpu_usage']
            cpu_percent = 0.0
            
            if system_delta > 0 and cpu_delta > 0:
                cpu_percent = (cpu_delta / system_delta) * 100.0 * stats['cpu_stats']['online_cpus']
            
            # Memory calculation (в MB)
            memory_usage = stats['memory_stats']['usage'] / (1024 * 1024)
            
            return {
                'cpu_percent': round(cpu_percent, 2),
                'memory_mb': round(memory_usage, 2),
                'timestamp': time.time()
            }
        except Exception as e:
            print(f"Error getting stats for {container_name}: {e}")
            return None'''
    def _calculate_cpu_percent(self, stats):
        """Расчет использования CPU в процентах"""
        try:
            cpu_stats = stats.get('cpu_stats', {})
            precpu_stats = stats.get('precpu_stats', {})
            
            cpu_delta = cpu_stats.get('cpu_usage', {}).get('total_usage', 0) - \
                       precpu_stats.get('cpu_usage', {}).get('total_usage', 0)
            
            system_delta = cpu_stats.get('system_cpu_usage', 0) - \
                          precpu_stats.get('system_cpu_usage', 0)
            
            online_cpus = cpu_stats.get('online_cpus', 1)
            
            cpu_percent = 0.0
            if system_delta > 0 and cpu_delta > 0:
                cpu_percent = (cpu_delta / system_delta) * 100.0 * online_cpus
            
            return min(cpu_percent, 100.0)
        except Exception as e:
            print(f"Error calculating CPU percent: {e}")
            return 0.0
            
    def get_container_stats(self, container_name):
        """Упрощенный способ через docker stats"""
        try:
            container = self.client.containers.get(container_name)
            stats = container.stats(stream=False)
            
            # Docker уже вычисляет проценты за нас
            cpu_percent = 0.0
            if 'cpu_stats' in stats and 'cpu_usage' in stats['cpu_stats']:
                # Можно использовать предрасчитанные значения
                cpu_percent = self._calculate_cpu_percent(stats)
            
            memory_usage = stats['memory_stats']['usage'] / (1024 * 1024)
            
            return {
                'cpu_percent': round(cpu_percent, 2),
                'memory_mb': round(memory_usage, 2),
                'timestamp': time.time()
            }
        except Exception as e:
            print(f"Error getting stats for {container_name}: {e}")
            return None
    
    def collect_metrics(self, duration=60):
        """Сбор метрик в течение указанного времени"""
        if not self.container_map:
            print("No containers found to monitor!")
            return
            
        print(f"Starting Docker metrics collection for {duration} seconds...")
        print(f"Monitoring: {list(self.container_map.keys())}")
        
        end_time = time.time() + duration
        sample_count = 0
        
        while time.time() < end_time:
            sample_count += 1
            current_time = datetime.now().strftime("%H:%M:%S")
            
            # Collect metrics for each found service
            for service_name, container_name in self.container_map.items():
                stats = self.get_container_stats(container_name)
                if stats:
                    self.metrics_data[service_name]['cpu'].append(stats['cpu_percent'])
                    self.metrics_data[service_name]['memory'].append(stats['memory_mb'])
                    
                    if sample_count % 5 == 1:  # Логируем каждые 5 секунд
                        print(f"[{current_time}] {service_name}: CPU {stats['cpu_percent']}%, Memory {stats['memory_mb']}MB")
                else:
                    print(f"[{current_time}] Failed to get stats for {service_name} ({container_name})")
            
            time.sleep(1)
        
        print(f"Collection completed. Collected {sample_count} samples.")
    
    def calculate_statistics(self):
        """Расчет статистики: min, max, average, median"""
        stats_summary = {}
        
        for service, metrics in self.metrics_data.items():
            stats_summary[service] = {}
            for metric_name, values in metrics.items():
                if values:
                    stats_summary[service][metric_name] = {
                        'min': round(min(values), 2),
                        'max': round(max(values), 2),
                        'average': round(statistics.mean(values), 2),
                        'median': round(statistics.median(values), 2),
                        'samples': len(values)
                    }
                else:
                    stats_summary[service][metric_name] = {
                        'min': 0, 'max': 0, 'average': 0, 'median': 0, 'samples': 0
                    }
        
        return stats_summary
    
    def create_plots(self, results_dir):
        """Создание графиков утилизации ресурсов"""
        if not self.metrics_data:
            print("No data to create plots")
            return
            
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        
        for service_name in self.metrics_data.keys():
            if not self.metrics_data[service_name]['cpu']:
                print(f"No data for {service_name}, skipping plot")
                continue
                
            # Создаем графики только для CPU и Memory
            fig, axes = plt.subplots(1, 2, figsize=(15, 6))
            fig.suptitle(f'Docker Resource Utilization - {service_name.capitalize()}', fontsize=16)
            
            time_points = list(range(len(self.metrics_data[service_name]['cpu'])))
            
            # CPU Usage
            axes[0].plot(time_points, self.metrics_data[service_name]['cpu'], 'b-', linewidth=1, alpha=0.7)
            axes[0].set_title('CPU Usage (%)')
            axes[0].set_ylabel('CPU %')
            axes[0].set_xlabel('Time (seconds)')
            axes[0].grid(True, alpha=0.3)
            
            # Memory Usage
            axes[1].plot(time_points, self.metrics_data[service_name]['memory'], 'r-', linewidth=1, alpha=0.7)
            axes[1].set_title('Memory Usage (MB)')
            axes[1].set_ylabel('Memory (MB)')
            axes[1].set_xlabel('Time (seconds)')
            axes[1].grid(True, alpha=0.3)
            
            plt.tight_layout()
            plot_path = os.path.join(results_dir, f'{timestamp}_{service_name}_docker_resources.png')
            plt.savefig(plot_path, dpi=300, bbox_inches='tight')
            plt.close()
            
            print(f"Saved Docker metrics plot: {plot_path}")
    
    def save_metrics(self, results_dir):
        """Сохранение метрик и статистики"""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        
        # Сохраняем сырые данные
        raw_data_path = os.path.join(results_dir, f'{timestamp}_docker_metrics.json')
        with open(raw_data_path, 'w') as f:
            json.dump({
                'container_map': self.container_map,
                'metrics_data': self.metrics_data,
                'collection_info': {
                    'duration_seconds': 60,
                    'samples_per_service': len(self.metrics_data.get('webapi', {}).get('cpu', [])),
                    'timestamp': timestamp
                }
            }, f, indent=2)
        
        # Сохраняем статистику
        stats_summary = self.calculate_statistics()
        stats_path = os.path.join(results_dir, f'{timestamp}_docker_resource_stats.json')
        with open(stats_path, 'w') as f:
            json.dump(stats_summary, f, indent=2)
        
        print(f"Saved raw Docker metrics: {raw_data_path}")
        print(f"Saved Docker statistics: {stats_path}")
        
        return stats_summary

def main():
    collector = DockerMetricsCollector()
    
    if not collector.container_map:
        print("No containers found to monitor. Exiting.")
        return
    
    # Собираем метрики 60 секунд
    collector.collect_metrics(duration=60)
    
    # Сохраняем результаты
    results_dir = '/scripts/results'
    os.makedirs(results_dir, exist_ok=True)
    
    stats_summary = collector.save_metrics(results_dir)
    #collector.create_plots(results_dir)
    
    #print("\n=== DOCKER RESOURCE USAGE SUMMARY ===")
    #print(json.dumps(stats_summary, indent=2))

if __name__ == "__main__":
    main()