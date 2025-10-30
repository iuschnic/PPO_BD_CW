# Функция для парсинга логов k6 и извлечения времен выполнения сценариев
function Parse-K6ScenarioTimings {
    param(
        [string]$RunDir,
        [string]$ContainerName = "ppo_bd_cw-benchmark-1"
    )
    
    $logsDir = Join-Path $RunDir "http_requests_summary"
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
    
    try {
        # Получаем логи контейнера benchmark
        $logs = docker logs $ContainerName 2>&1
        
        # Ищем строки содержащие SCENARIO_TIMING
        $scenarioLines = $logs | Where-Object { $_ -match "SCENARIO_TIMING" }
        
        if ($scenarioLines.Count -gt 0) {
            # Парсим JSON из каждой строки
            $scenarioTimings = @()
            
            foreach ($line in $scenarioLines) {
                try {
                    
                    # Извлекаем JSON часть - все что между фигурными скобками
                    if ($line -match '\{.*\}') {
                        $jsonPart = $matches[0]
						$jsonPart = $jsonPart -replace '\\"', '"'
                        
                        $data = $jsonPart | ConvertFrom-Json
                        
                        # Сохраняем для JSONL
                        $scenarioTimings += $jsonPart
                    }
                    
                } catch {
                    Write-Host "Failed to parse line: $line" -ForegroundColor Red
                    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            
            if ($scenarioTimings.Count -gt 0) {
                Write-Host "Successfully parsed $($scenarioTimings.Count) scenario timing records" -ForegroundColor Green
                
                # Сохраняем как JSONL
                $scenarioTimings | Out-File -FilePath (Join-Path $logsDir "scenario_timings.jsonl") -Encoding UTF8
                
            } else {
                Write-Host "No valid scenario timing records could be parsed" -ForegroundColor Yellow
            }
            
        } else {
            Write-Host "No SCENARIO_TIMING records found in logs" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "Failed to parse k6 logs: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Функция для сохранения результатов бенчмарка
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
    
    # Парсим логи k6 и сохраняем времена выполнения сценариев
    Parse-K6ScenarioTimings -RunDir $RunDir
    
    Write-Host "Results saved to: $RunDir" -ForegroundColor Green
}

# Функция для получения следующего номера run
function Get-NextRunNumber {
    param([string]$ResultsBaseDir)
    
    if (-not (Test-Path $ResultsBaseDir)) {
        return 1
    }
    
    $existingRuns = Get-ChildItem $ResultsBaseDir -Directory | Where-Object { $_.Name -match "^run_(\d+)$" }
    
    if ($existingRuns.Count -eq 0) {
        return 1
    }
    
    # Извлекаем номера из имен папок и находим максимальный
    $maxNumber = 0
    foreach ($run in $existingRuns) {
        if ($run.Name -match "run_(\d+)") {
            $number = [int]$matches[1]
            if ($number -gt $maxNumber) {
                $maxNumber = $number
            }
        }
    }
    
    return $maxNumber + 1
}

$successfulRuns = 0
$failedRuns = 0
$runs = 10

Write-Host "Starting $runs benchmark iterations..." -ForegroundColor Green
$resultsBaseDir = "benchmark_results"

# Создаем основную папку если её нет
if (-not (Test-Path $resultsBaseDir)) {
    New-Item -ItemType Directory -Path $resultsBaseDir | Out-Null
}

# Получаем текущее количество run'ов
$existingRunCount = 0
if (Test-Path $resultsBaseDir) {
    $existingRuns = Get-ChildItem $resultsBaseDir -Directory | Where-Object { $_.Name -match "^run_\d+$" }
    $existingRunCount = $existingRuns.Count
    Write-Host "Found $existingRunCount existing run(s)" -ForegroundColor Yellow
}

# Получаем номер для первого нового run'а
$startRunNumber = Get-NextRunNumber -ResultsBaseDir $resultsBaseDir
Write-Host "Starting new runs from number: $startRunNumber" -ForegroundColor Green

for ($i = 0; $i -lt $runs; $i++) {
    $currentRunNumber = $startRunNumber + $i
    Write-Host "`n=== Iteration $($i + 1)/$runs (Run $currentRunNumber) ===" -ForegroundColor Yellow
    
    # Создаем папку для текущего прогона
    $runDir = "$resultsBaseDir/run_$currentRunNumber"
    New-Item -ItemType Directory -Path $runDir -Force | Out-Null
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
        Save-BenchmarkResults -RunDir $runDir -Iteration $currentRunNumber
        
        $successfulRuns++
        Write-Host "Iteration $($i + 1) completed successfully (Run $currentRunNumber)" -ForegroundColor Green
        
    } catch {
        $failedRuns++
        Write-Host "Iteration $($i + 1) failed: $($_.Exception.Message)" -ForegroundColor Red
        
        # Сохраняем доступные результаты даже при ошибке
        try {
            Write-Host "Saving available results for failed run..." -ForegroundColor Gray
            Save-BenchmarkResults -RunDir $runDir -Iteration $currentRunNumber
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
    if ($i -lt ($runs - 1)) {
        Write-Host "Preparing for next iteration..." -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
}

# Подсчитываем общее количество run'ов после выполнения
$totalRuns = 0
if (Test-Path $resultsBaseDir) {
    $allRuns = Get-ChildItem $resultsBaseDir -Directory | Where-Object { $_.Name -match "^run_\d+$" }
    $totalRuns = $allRuns.Count
}

Write-Host "`nBenchmark execution completed!" -ForegroundColor Green
Write-Host "Successful runs in this session: $successfulRuns" -ForegroundColor Green
Write-Host "Failed runs in this session: $failedRuns" -ForegroundColor Red
Write-Host "Total runs in benchmark_results: $totalRuns" -ForegroundColor Cyan

docker-compose -f docker-compose-benchmark.yml down --volumes --remove-orphans 2>&1 | Out-Null

# Агрегируем результаты
$aggregateScriptDir = "aggregate_benchmark_results"

$libsDir = "$aggregateScriptDir/libs"
if (-not (Test-Path $libsDir)) {
    New-Item -ItemType Directory -Path $libsDir | Out-Null
    pip install -t $libsDir matplotlib numpy scipy pandas
}

$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_cpu_mem.py"
$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_serialization_time.py"
$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_scenario_timings.py"

Write-Host "`nAll aggregation scripts completed!" -ForegroundColor Green
Write-Host "Total dataset now contains $totalRuns runs for analysis" -ForegroundColor Cyan