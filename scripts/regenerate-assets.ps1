# Regenerate MSIX visual assets as valid PNGs. Replaces placeholders only —
# final artwork should be produced in Visual Studio's Asset Generator (open
# Package.appxmanifest → Visual Assets tab) or a dedicated design tool.
#
# Run from repo root:  pwsh -File scripts\regenerate-assets.ps1

Add-Type -AssemblyName System.Drawing

$bg     = [System.Drawing.Color]::FromArgb(255, 237, 123, 35)  # basketball orange
$ball   = [System.Drawing.Color]::FromArgb(255, 139,  60,  17) # darker burnt orange
$stroke = [System.Drawing.Color]::FromArgb(255,  30,  20,  10) # near-black

function New-Placeholder {
    param(
        [string] $Path,
        [int]    $Width,
        [int]    $Height,
        [switch] $Transparent
    )

    $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

    if ($Transparent) {
        $g.Clear([System.Drawing.Color]::Transparent)
    } else {
        $g.Clear($bg)
    }

    # Basketball: centered circle ~70% of the shorter side.
    $size = [Math]::Min($Width, $Height)
    $d    = [int]($size * 0.7)
    $x    = [int](($Width  - $d) / 2)
    $y    = [int](($Height - $d) / 2)

    $ballBrush = New-Object System.Drawing.SolidBrush($ball)
    $g.FillEllipse($ballBrush, $x, $y, $d, $d)
    $ballBrush.Dispose()

    $strokeWidth = [Math]::Max(1, [int]($size * 0.02))
    $pen = New-Object System.Drawing.Pen($stroke, $strokeWidth)
    $g.DrawEllipse($pen, $x, $y, $d, $d)

    # Seam lines: vertical + horizontal through the ball.
    $cx = $x + $d / 2
    $cy = $y + $d / 2
    $g.DrawLine($pen, $cx, $y, $cx, $y + $d)
    $g.DrawLine($pen, $x, $cy, $x + $d, $cy)

    $pen.Dispose()
    $g.Dispose()

    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    Write-Host "Generated $Path  ($Width x $Height)"
}

$assets = Join-Path $PSScriptRoot '..\NBAExtension\Assets'
$assets = [System.IO.Path]::GetFullPath($assets)

New-Placeholder "$assets\StoreLogo.png"                                      50   50
New-Placeholder "$assets\Square44x44Logo.scale-200.png"                      88   88
New-Placeholder "$assets\Square44x44Logo.targetsize-24_altform-unplated.png" 24   24 -Transparent
New-Placeholder "$assets\Square150x150Logo.scale-200.png"                   300  300
New-Placeholder "$assets\Wide310x150Logo.scale-200.png"                     620  300
New-Placeholder "$assets\SplashScreen.scale-200.png"                       1240  600
New-Placeholder "$assets\LockScreenLogo.scale-200.png"                       48   48

Write-Host "`nDone. Verify with:  Get-ChildItem $assets\*.png | ForEach-Object { Get-Content `$_.FullName -TotalCount 1 -Encoding Byte | Select-Object -First 4 }"
