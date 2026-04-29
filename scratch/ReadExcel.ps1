$xlsxPath = "e:\Musa\VS SQL PROJ 2026\KTC DOTNET SQL 2026\20.04.2026 ULTRATECH.xlsx"
$tempDir  = "e:\Musa\VS SQL PROJ 2026\KTC DOTNET SQL 2026\scratch\xlsxtemp"

if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
New-Item -ItemType Directory -Path $tempDir | Out-Null
Copy-Item $xlsxPath "$tempDir\file.zip"
Expand-Archive "$tempDir\file.zip" -DestinationPath "$tempDir\extracted" -Force

# Build shared strings lookup
$sharedStrings = @{}
$ssPath = "$tempDir\extracted\xl\sharedStrings.xml"
if (Test-Path $ssPath) {
    [xml]$ss = Get-Content $ssPath -Encoding UTF8
    $i = 0
    foreach ($si in $ss.sst.si) {
        # handle both plain text and rich text
        if ($si.t -ne $null) { $sharedStrings[$i] = $si.t }
        elseif ($si.r -ne $null) { $sharedStrings[$i] = ($si.r | ForEach-Object { $_.t }) -join "" }
        $i++
    }
}

# Read worksheet
$sheetPath = "$tempDir\extracted\xl\worksheets\sheet1.xml"
[xml]$sheet = Get-Content $sheetPath -Encoding UTF8
$rows = $sheet.worksheet.sheetData.row

Write-Host ""
Write-Host "=== ALL COLUMN HEADERS (first non-empty header row) ===" -ForegroundColor Cyan
$headerPrinted = $false
foreach ($row in $rows) {
    $cells = $row.c
    $hasText = $false
    foreach ($cell in $cells) {
        if ($cell.t -eq "s") {
            $v = $sharedStrings[[int]$cell.v]
            if ($v -and $v.Trim()) { $hasText = $true; break }
        }
    }
    if (-not $hasText) { continue }

    foreach ($cell in $cells) {
        $val = ""
        if ($cell.t -eq "s") { $val = $sharedStrings[[int]$cell.v] }
        elseif ($cell.v) { $val = $cell.v }
        $col = $cell.r -replace '\d+', ''
        Write-Host "  Col $col : $val"
    }
    $headerPrinted = $true
    if ($headerPrinted) { break }
}

Write-Host ""
Write-Host "=== FIRST 5 DATA ROWS (Col values) ===" -ForegroundColor Cyan
$rowCount = 0
$dataStarted = $false
foreach ($row in $rows) {
    # skip rows before data
    if (-not $dataStarted) {
        $cells = $row.c
        $hasText = $false
        foreach ($cell in $cells) {
            if ($cell.t -eq "s") {
                $v = $sharedStrings[[int]$cell.v]
                if ($v -and $v.Trim()) { $hasText = $true; break }
            }
        }
        if ($hasText) { $dataStarted = $true; continue }  # skip the header row itself
        continue
    }
    if ($rowCount -ge 5) { break }
    $cells = $row.c
    $rowData = @()
    foreach ($cell in $cells) {
        $val = ""
        if ($cell.t -eq "s") { $val = $sharedStrings[[int]$cell.v] }
        elseif ($cell.v) { $val = $cell.v }
        $col = $cell.r -replace '\d+', ''
        $rowData += "$col=[$val]"
    }
    Write-Host "  Row $($row.r): $($rowData -join '  ')"
    $rowCount++
}

Write-Host ""
Write-Host "Done! Check above for column names." -ForegroundColor Green
