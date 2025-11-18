param(
    [Parameter(Mandatory=$false)]
    [string]$ResultsDirectory
)

# Если папка не передана как аргумент, запрашиваем у пользователя
if (-not $ResultsDirectory) {
    $ResultsDirectory = Read-Host "Введите папку с результатами бенчмарка"
}

# Проверяем, что папка существует
if (-not (Test-Path $ResultsDirectory)) {
    Write-Host "Error: '$ResultsDirectory' not found!" -ForegroundColor Red
    exit 1
}

# Агрегируем результаты
$aggregateScriptDir = "benchmark_aggregate_results"

$libsDir = "$aggregateScriptDir/libs"
if (-not (Test-Path $libsDir)) {
    New-Item -ItemType Directory -Path $libsDir -Force | Out-Null
    pip install -t $libsDir matplotlib numpy scipy pandas
}


$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_cpu_mem.py" "$ResultsDirectory"
$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_serialization_time.py" "$ResultsDirectory"
$env:PYTHONPATH = "$libsDir;$env:PYTHONPATH"
python "$aggregateScriptDir/aggregate_scenario_timings.py" "$ResultsDirectory"