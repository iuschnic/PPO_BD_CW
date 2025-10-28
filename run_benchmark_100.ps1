# Функция для сохранения результатов бенчмарка (должна быть определена ДО основного кода)
function Save-BenchmarkResults {
    param(
        [string]$RunDir,
        [int]$Iteration
    )
    
    # Копируем результаты из временных папок в папку прогона
    $sourceDirs = @(
        @{Source = "temp_serialization_time_results"; Dest = "serialization_time_results"},
        @{Source = "temp_http_requests_summary"; Dest = "http_requests_summary"},
        @{Source = "temp_cpu_mem_results"; Dest = "cpu_mem_results"}
    )
    
    foreach ($dirMapping in $sourceDirs) {
        if (Test-Path $dirMapping.Source) {
            $destPath = Join-Path $RunDir $dirMapping.Dest
            Copy-Item $dirMapping.Source -Destination $destPath -Recurse -Force
            Write-Host "Copied: $($dirMapping.Dest)" -ForegroundColor Gray
        }
    }
    
    Write-Host "Results saved to: $RunDir" -ForegroundColor Green
}

$successfulRuns = 0
$failedRuns = 0
$runs = 2

Write-Host "Starting $runs benchmark iterations..." -ForegroundColor Green
$resultsBaseDir = "benchmark_results"
if (Test-Path $resultsBaseDir) {
    Write-Host "Cleaning up previous benchmark results..." -ForegroundColor Yellow
    Remove-Item $resultsBaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $resultsBaseDir | Out-Null

for ($i = 1; $i -le $runs; $i++) {
    Write-Host "`n=== Iteration $i/$runs ===" -ForegroundColor Yellow
    
    # Создаем папку для текущего прогона
    $runDir = "$resultsBaseDir/run_$i"
    if (Test-Path $runDir) {
        Remove-Item $runDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $runDir | Out-Null
    Write-Host "Results will be saved to: $runDir" -ForegroundColor Gray
    
    try {
        # Полностью останавливаем и очищаем предыдущий запуск
        Write-Host "Cleaning up previous run..." -ForegroundColor Gray
        docker-compose -f docker-compose-benchmark.yml down --volumes --remove-orphans 2>&1 | Out-Null
        
        # Ждем полной остановки
        Start-Sleep -Seconds 3
        
        # Создаем временные папки для этого прогона
        $tempDirs = @(
            "temp_serialization_time_results",
            "temp_http_requests_summary", 
            "temp_cpu_mem_results"
        )
        
        foreach ($tempDir in $tempDirs) {
            if (Test-Path $tempDir) {
                Remove-Item $tempDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $tempDir | Out-Null
        }
        
        # Запускаем все сервисы заново с временными папками
        Write-Host "Starting fresh containers..." -ForegroundColor Gray
        docker-compose -f docker-compose-benchmark.yml up --build -d 2>&1 | Out-Null
        
        # Ждем когда все сервисы станут здоровыми (максимум 2 минуты)
        Write-Host "Waiting for services to be healthy..." -ForegroundColor Gray
        $timeout = 0
        $maxWait = 120 # 2 минуты
        
        do {
            Start-Sleep -Seconds 5
            $timeout += 5
            
            $webcli_healthy = docker-compose -f docker-compose-benchmark.yml ps webcli | Select-String "healthy"
            $postgres_healthy = docker-compose -f docker-compose-benchmark.yml ps postgres | Select-String "healthy"
            
            if ($timeout -ge $maxWait) {
                throw "Services health check timeout"
            }
            
        } while (-not $webcli_healthy -or -not $postgres_healthy)
        
        Write-Host "All services healthy. Benchmark running..." -ForegroundColor Green
        
        # Ждем завершения бенчмарка (максимум 10 минут)
        $benchmarkTimeout = 0
        $maxBenchmarkWait = 600 # 10 минут
        
        do {
            Start-Sleep -Seconds 5
            $benchmarkTimeout += 5
            $status = docker-compose -f docker-compose-benchmark.yml ps benchmark | Select-String "Up"
            
            if ($benchmarkTimeout -ge $maxBenchmarkWait) {
                throw "Benchmark execution timeout"
            }
            
        } while ($status)
        
        # Сохраняем результаты бенчмарка в папку прогона
        Write-Host "Saving benchmark results..." -ForegroundColor Gray
        Save-BenchmarkResults -RunDir $runDir -Iteration $i
        
        $successfulRuns++
        Write-Host "Iteration $i completed successfully" -ForegroundColor Green
        
    } catch {
        $failedRuns++
        Write-Host "Iteration $i failed: $($_.Exception.Message)" -ForegroundColor Red
        
        # Сохраняем доступные результаты даже при ошибке
        try {
            Write-Host "Saving available results for failed run..." -ForegroundColor Gray
            Save-BenchmarkResults -RunDir $runDir -Iteration $i
        } catch {
            Write-Host "Failed to save results: $($_.Exception.Message)" -ForegroundColor DarkRed
        }
        
        # Принудительно останавливаем все при ошибке
        docker-compose -f docker-compose-benchmark.yml down --volumes --remove-orphans 2>&1 | Out-Null
    } finally {
        # Очищаем временные папки
        $tempDirs = @(
            "temp_serialization_time_results",
            "temp_http_requests_summary",
            "temp_cpu_mem_results"
        )
        
        foreach ($tempDir in $tempDirs) {
            if (Test-Path $tempDir) {
                Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
    
    # Короткая пауза между запусками
    if ($i -lt $runs) {
        Write-Host "Preparing for next iteration..." -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
}

Write-Host "`nAll $runs iterations completed!" -ForegroundColor Green
Write-Host "Successful runs: $successfulRuns" -ForegroundColor Green
Write-Host "Failed runs: $failedRuns" -ForegroundColor Red

docker-compose -f docker-compose-benchmark.yml down --volumes --remove-orphans 2>&1 | Out-Null


$aggregateScriptDir = "aggregate_benchmark_results"

$libsDir = "$aggregateScriptDir/libs"
if (-not (Test-Path $libsDir)) {
    New-Item -ItemType Directory -Path $libsDir | Out-Null
	pip install -t $libsDir matplotlib numpy
}

$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_cpu_mem.py"