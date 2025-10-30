# Агрегируем результаты
$aggregateScriptDir = "benchmark_aggregare_results"

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