Write-Host "Starting 100 benchmark iterations..." -ForegroundColor Green

$successfulRuns = 0
$failedRuns = 0
$runs = 10

for ($i = 1; $i -le $runs; $i++) {
    Write-Host "`n=== Iteration $i/$runs ===" -ForegroundColor Yellow
    
    try {
        # Полностью останавливаем и очищаем предыдущий запуск
        Write-Host "Cleaning up previous run..." -ForegroundColor Gray
        docker-compose -f docker-compose-benchmark.yml down --volumes --remove-orphans 2>&1 | Out-Null
        
        # Ждем полной остановки
        Start-Sleep -Seconds 3
        
        # Запускаем все сервисы заново
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
        
        $successfulRuns++
        Write-Host "Iteration $i completed successfully" -ForegroundColor Green
        
    } catch {
        $failedRuns++
        Write-Host "Iteration $i failed: $($_.Exception.Message)" -ForegroundColor Red
        
        # Принудительно останавливаем все при ошибке
        docker-compose -f docker-compose-benchmark.yml down --volumes --remove-orphans 2>&1 | Out-Null
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