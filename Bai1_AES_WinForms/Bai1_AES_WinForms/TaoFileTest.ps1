param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [ValidateRange(1, 128)]
    [int]$SizeGiB = 1
)

# Tạo dữ liệu ngẫu nhiên theo khối, không nạp toàn bộ tệp vào RAM.
# Việc tạo tệp thử nghiệm này tách biệt với thời gian mã hóa/giải mã trong ứng dụng.
$targetBytes = [long]$SizeGiB * 1GB
$bufferSize = 1MB
$buffer = New-Object byte[] $bufferSize
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$directory = Split-Path -Parent $OutputPath

if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$stream = $null
try {
    $stream = [System.IO.FileStream]::new(
        $OutputPath,
        [System.IO.FileMode]::Create, 
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None,
        $bufferSize,
        [System.IO.FileOptions]::SequentialScan
    )

    [long]$written = 0
    [long]$lastReportedGiB = -1
    while ($written -lt $targetBytes) {
        $rng.GetBytes($buffer)
        $remaining = $targetBytes - $written
        $count = [int][Math]::Min([long]$buffer.Length, $remaining)
        $stream.Write($buffer, 0, $count)
        $written += $count

        $currentGiB = [long][Math]::Floor($written / 1GB)
        if ($currentGiB -gt $lastReportedGiB) {
            $percent = [Math]::Min(100, [Math]::Round(($written / [double]$targetBytes) * 100, 1))
            Write-Host ("Da tao {0:N1}% ({1:N2} GiB / {2:N2} GiB)" -f $percent, ($written / 1GB), $SizeGiB)
            $lastReportedGiB = $currentGiB
        }
    }

    $stream.Flush($true)
    Write-Host "Hoan tat: $OutputPath"
    Write-Host ("Dung luong: {0:N0} byte" -f $written)
}
finally {
    if ($null -ne $stream) { $stream.Dispose() }
    $rng.Dispose()
    [Array]::Clear($buffer, 0, $buffer.Length)
}
