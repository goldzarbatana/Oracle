
$filePath = 'G:\UnityEditor\TimeAura\Assets\Scripts\Features\UI\Nexus\NexusController.cs'
$lines = Get-Content $filePath
$stack = New-Object System.Collections.Generic.Stack[int]
$lineNumber = 1

foreach ($line in $lines) {
    for ($i = 0; $i -lt $line.Length; $i++) {
        if ($line[$i] -eq '{') {
            $stack.Push($lineNumber)
        }
        elseif ($line[$i] -eq '}') {
            if ($stack.Count -eq 0) {
                Write-Host "Extra closing brace at line $lineNumber"
            }
            else {
                $stack.Pop() | Out-Null
            }
        }
    }
    $lineNumber++
}

if ($stack.Count -gt 0) {
    Write-Host "Missing closing braces for starts at lines: $($stack -join ', ')"
}
else {
    Write-Host "Braces are balanced."
}
