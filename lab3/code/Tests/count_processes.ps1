$startTime = Get-Date
$initialProcesses = Get-Process -Name "dotnet", "testhost" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id

Write-Host "Initial processes: $($initialProcesses.Count)"

dotnet test --configuration Debug

$endTime = Get-Date
$finalProcesses = Get-Process -Name "dotnet", "testhost" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id

$newProcesses = $finalProcesses | Where-Object { $initialProcesses -notcontains $_ }
Write-Host "Processes created during tests: $($newProcesses.Count)"