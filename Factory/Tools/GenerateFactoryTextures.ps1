Add-Type -AssemblyName System.Drawing

function New-Color {
    param([int]$R, [int]$G, [int]$B, [int]$A = 255)
    return [System.Drawing.Color]::FromArgb($A, $R, $G, $B)
}

function Set-PixelSafe {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X,
        [int]$Y,
        [System.Drawing.Color]$Color
    )

    if ($X -lt 0 -or $X -ge $Bitmap.Width -or $Y -lt 0 -or $Y -ge $Bitmap.Height) {
        return
    }

    if ($Color.A -le 0) {
        return
    }

    $Bitmap.SetPixel($X, $Y, $Color)
}

function Fill-Rect {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Color
    )

    for ($ix = $X; $ix -lt ($X + $Width); $ix++) {
        for ($iy = $Y; $iy -lt ($Y + $Height); $iy++) {
            Set-PixelSafe $Bitmap $ix $iy $Color
        }
    }
}

function Draw-HLine {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X1,
        [int]$X2,
        [int]$Y,
        [System.Drawing.Color]$Color
    )

    for ($x = [Math]::Min($X1, $X2); $x -le [Math]::Max($X1, $X2); $x++) {
        Set-PixelSafe $Bitmap $x $Y $Color
    }
}

function Draw-VLine {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X,
        [int]$Y1,
        [int]$Y2,
        [System.Drawing.Color]$Color
    )

    for ($y = [Math]::Min($Y1, $Y2); $y -le [Math]::Max($Y1, $Y2); $y++) {
        Set-PixelSafe $Bitmap $X $y $Color
    }
}

function Draw-Line {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X1,
        [int]$Y1,
        [int]$X2,
        [int]$Y2,
        [System.Drawing.Color]$Color
    )

    $dx = [Math]::Abs($X2 - $X1)
    $sx = if ($X1 -lt $X2) { 1 } else { -1 }
    $dy = -[Math]::Abs($Y2 - $Y1)
    $sy = if ($Y1 -lt $Y2) { 1 } else { -1 }
    $err = $dx + $dy

    while ($true) {
        Set-PixelSafe $Bitmap $X1 $Y1 $Color
        if ($X1 -eq $X2 -and $Y1 -eq $Y2) {
            break
        }

        $e2 = 2 * $err
        if ($e2 -ge $dy) {
            $err += $dy
            $X1 += $sx
        }
        if ($e2 -le $dx) {
            $err += $dx
            $Y1 += $sy
        }
    }
}

function Fill-Ellipse {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Color
    )

    $rx = ($Width - 1) / 2.0
    $ry = ($Height - 1) / 2.0
    $cx = $X + $rx
    $cy = $Y + $ry

    for ($ix = $X; $ix -lt ($X + $Width); $ix++) {
        for ($iy = $Y; $iy -lt ($Y + $Height); $iy++) {
            $nx = if ($rx -eq 0) { 0 } else { ($ix - $cx) / $rx }
            $ny = if ($ry -eq 0) { 0 } else { ($iy - $cy) / $ry }
            if (($nx * $nx) + ($ny * $ny) -le 1.0) {
                Set-PixelSafe $Bitmap $ix $iy $Color
            }
        }
    }
}

function Outline-Ellipse {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Color
    )

    $rx = ($Width - 1) / 2.0
    $ry = ($Height - 1) / 2.0
    $cx = $X + $rx
    $cy = $Y + $ry

    for ($ix = $X; $ix -lt ($X + $Width); $ix++) {
        for ($iy = $Y; $iy -lt ($Y + $Height); $iy++) {
            $nx = if ($rx -eq 0) { 0 } else { ($ix - $cx) / $rx }
            $ny = if ($ry -eq 0) { 0 } else { ($iy - $cy) / $ry }
            $distance = ($nx * $nx) + ($ny * $ny)
            if ($distance -ge 0.72 -and $distance -le 1.15) {
                Set-PixelSafe $Bitmap $ix $iy $Color
            }
        }
    }
}

function Draw-DotPattern {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$ColorA,
        [System.Drawing.Color]$ColorB
    )

    for ($ix = 0; $ix -lt $Width; $ix++) {
        for ($iy = 0; $iy -lt $Height; $iy++) {
            $color = if ((($ix + $iy) % 2) -eq 0) { $ColorA } else { $ColorB }
            Set-PixelSafe $Bitmap ($X + $ix) ($Y + $iy) $color
        }
    }
}

function Save-Sprite {
    param(
        [string]$Path,
        [scriptblock]$Draw
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $bitmap = New-Object System.Drawing.Bitmap 16, 16, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        & $Draw $bitmap
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

$outline = New-Color 38 31 24
$woodDark = New-Color 86 54 30
$woodMid = New-Color 134 86 48
$woodLight = New-Color 186 131 78
$leafDark = New-Color 42 105 50
$leafMid = New-Color 74 145 69
$leafLight = New-Color 116 184 96
$grassDark = New-Color 60 120 47
$grassMid = New-Color 95 167 73
$grassLight = New-Color 143 209 99
$stoneDark = New-Color 76 82 88
$stoneMid = New-Color 111 118 124
$stoneLight = New-Color 160 166 171
$sandDark = New-Color 185 164 106
$sandMid = New-Color 216 194 128
$sandLight = New-Color 242 224 163
$ironDark = New-Color 98 108 112
$ironMid = New-Color 164 172 174
$ironLight = New-Color 216 223 219
$copperDark = New-Color 120 72 45
$copperMid = New-Color 190 112 62
$copperLight = New-Color 232 154 96
$coalDark = New-Color 24 25 27
$coalMid = New-Color 49 51 54
$coalLight = New-Color 84 86 90
$skin = New-Color 225 198 162
$hair = New-Color 98 63 38
$shirt = New-Color 74 108 152
$pants = New-Color 64 72 92
$boot = New-Color 54 42 34
$rabbitFur = New-Color 197 186 176
$rabbitShade = New-Color 154 142 132
$rabbitEar = New-Color 224 174 180
$fishBody = New-Color 92 154 186
$fishShade = New-Color 55 111 148
$fishLight = New-Color 168 214 234
$furnaceGlow = New-Color 231 133 61
$furnaceGlowDark = New-Color 111 58 31
$meat = New-Color 170 89 84
$meatLight = New-Color 212 136 128
$bone = New-Color 230 223 205
$ropeDark = New-Color 137 116 66
$ropeLight = New-Color 201 181 119

$textureRoot = 'C:\Users\wintake\GameEngine\AkiGamesEngine\Factory\Content\Textures'

function Draw-TreeCanopy {
    param([System.Drawing.Bitmap]$Bitmap, [string]$SizeMode)

    switch ($SizeMode) {
        'Sapling' {
            Fill-Ellipse $Bitmap 5 4 6 5 $leafDark
            Fill-Ellipse $Bitmap 6 3 4 5 $leafMid
            Fill-Rect $Bitmap 7 8 2 6 $woodMid
            Draw-VLine $Bitmap 7 8 14 $outline
            Draw-VLine $Bitmap 8 8 14 $woodDark
        }
        'Young' {
            Fill-Ellipse $Bitmap 3 2 10 8 $leafDark
            Fill-Ellipse $Bitmap 4 2 8 8 $leafMid
            Fill-Ellipse $Bitmap 6 3 4 4 $leafLight
            Fill-Rect $Bitmap 6 9 4 6 $woodMid
            Fill-Rect $Bitmap 7 9 1 6 $woodDark
            Fill-Rect $Bitmap 8 9 1 6 $woodLight
        }
        default {
            Fill-Ellipse $Bitmap 1 2 14 8 $leafDark
            Fill-Ellipse $Bitmap 2 1 12 9 $leafMid
            Fill-Ellipse $Bitmap 5 2 6 5 $leafLight
            Fill-Rect $Bitmap 6 9 4 6 $woodMid
            Fill-Rect $Bitmap 7 9 1 6 $woodDark
            Fill-Rect $Bitmap 8 9 1 6 $woodLight
            Draw-HLine $Bitmap 5 10 8 $leafDark
        }
    }
}

function Draw-Rock {
    param([System.Drawing.Bitmap]$Bitmap)
    Fill-Ellipse $Bitmap 2 5 12 8 $stoneDark
    Fill-Ellipse $Bitmap 3 6 10 6 $stoneMid
    Fill-Ellipse $Bitmap 7 6 4 3 $stoneLight
    Draw-HLine $Bitmap 5 10 12 $stoneLight
    Draw-HLine $Bitmap 4 9 13 $outline
}

function Draw-OreItem {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [System.Drawing.Color]$Dark,
        [System.Drawing.Color]$Mid,
        [System.Drawing.Color]$Light
    )

    Fill-Ellipse $Bitmap 3 5 10 7 $Dark
    Fill-Ellipse $Bitmap 4 6 8 5 $Mid
    Fill-Ellipse $Bitmap 8 6 3 2 $Light
    Set-PixelSafe $Bitmap 6 7 $Light
    Set-PixelSafe $Bitmap 5 10 $outline
}

function Draw-GroundBase {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [System.Drawing.Color]$ColorA,
        [System.Drawing.Color]$ColorB,
        [System.Drawing.Color]$Crack
    )

    Draw-DotPattern $Bitmap 0 0 16 16 $ColorA $ColorB
    Draw-Line $Bitmap 1 5 6 4 $Crack
    Draw-Line $Bitmap 10 3 14 5 $Crack
    Draw-Line $Bitmap 3 11 8 10 $Crack
    Draw-Line $Bitmap 9 12 14 11 $Crack
}

Save-Sprite (Join-Path $textureRoot 'Ground\sand.png') {
    param($bmp)
    Draw-DotPattern $bmp 0 0 16 16 $sandMid $sandLight
    foreach ($point in @(
        @(2, 2), @(6, 3), @(11, 2), @(13, 5),
        @(4, 7), @(8, 8), @(12, 10), @(3, 12), @(9, 13)
    )) {
        Set-PixelSafe $bmp $point[0] $point[1] $sandDark
    }
}

Save-Sprite (Join-Path $textureRoot 'Ground\grass.png') {
    param($bmp)
    foreach ($point in @(
        @(2, 12), @(4, 10), @(5, 13), @(7, 9), @(8, 12), @(10, 10), @(12, 13), @(13, 9)
    )) {
        Draw-Line $bmp $point[0] 15 $point[0] $point[1] $grassLight
    }
    foreach ($point in @(
        @(1, 13), @(3, 11), @(6, 14), @(9, 11), @(11, 14), @(14, 12)
    )) {
        Draw-Line $bmp $point[0] 15 $point[0] $point[1] $grassMid
    }
    Set-PixelSafe $bmp 4 8 $grassDark
    Set-PixelSafe $bmp 10 8 $grassDark
}

Save-Sprite (Join-Path $textureRoot 'Ground\stone.png') {
    param($bmp)
    Draw-GroundBase $bmp $stoneMid $stoneLight $stoneDark
    Set-PixelSafe $bmp 4 4 $stoneDark
    Set-PixelSafe $bmp 12 6 $stoneDark
    Set-PixelSafe $bmp 7 12 $stoneDark
}

Save-Sprite (Join-Path $textureRoot 'Ground\cave.png') {
    param($bmp)
    Draw-DotPattern $bmp 0 0 16 16 (New-Color 36 37 40) (New-Color 28 29 32)
    Draw-Line $bmp 2 4 6 5 (New-Color 50 49 52)
    Draw-Line $bmp 9 6 13 5 (New-Color 46 45 48)
    Draw-Line $bmp 3 11 7 10 (New-Color 42 41 44)
    Draw-Line $bmp 10 12 14 11 (New-Color 40 39 42)
}

Save-Sprite (Join-Path $textureRoot 'Ground\water.png') {
    param($bmp)
    Fill-Rect $bmp 0 0 16 16 (New-Color 46 103 150)
    Draw-Line $bmp 1 4 5 3 (New-Color 118 177 214)
    Draw-Line $bmp 7 6 12 5 (New-Color 135 192 224)
    Draw-Line $bmp 3 10 8 9 (New-Color 102 166 206)
    Draw-Line $bmp 10 12 14 11 (New-Color 128 184 216)
    Set-PixelSafe $bmp 6 3 (New-Color 180 224 244)
    Set-PixelSafe $bmp 9 9 (New-Color 180 224 244)
}

Save-Sprite (Join-Path $textureRoot 'Ground\stone_ore.png') {
    param($bmp)
    Draw-GroundBase $bmp $stoneMid $stoneLight $stoneDark
    Fill-Ellipse $bmp 3 3 4 3 $stoneLight
    Fill-Ellipse $bmp 10 8 3 3 $stoneLight
    Fill-Ellipse $bmp 7 11 4 3 $stoneLight
    Set-PixelSafe $bmp 5 4 $outline
    Set-PixelSafe $bmp 11 9 $outline
    Set-PixelSafe $bmp 8 12 $outline
}

Save-Sprite (Join-Path $textureRoot 'Ground\iron_ore.png') {
    param($bmp)
    Draw-GroundBase $bmp $stoneMid $stoneLight $stoneDark
    Fill-Ellipse $bmp 3 3 4 4 $ironMid
    Fill-Ellipse $bmp 10 4 3 3 $ironLight
    Fill-Ellipse $bmp 8 10 4 3 $ironMid
    Set-PixelSafe $bmp 5 5 $ironDark
    Set-PixelSafe $bmp 11 5 $outline
    Set-PixelSafe $bmp 9 11 $ironLight
}

Save-Sprite (Join-Path $textureRoot 'Ground\copper_ore.png') {
    param($bmp)
    Draw-GroundBase $bmp $stoneMid $stoneLight $stoneDark
    Fill-Ellipse $bmp 2 4 4 4 $copperMid
    Fill-Ellipse $bmp 9 3 5 4 $copperLight
    Fill-Ellipse $bmp 7 10 4 4 $copperMid
    Set-PixelSafe $bmp 4 5 $copperDark
    Set-PixelSafe $bmp 11 4 $outline
    Set-PixelSafe $bmp 8 11 $copperLight
}

Save-Sprite (Join-Path $textureRoot 'Ground\coal_ore.png') {
    param($bmp)
    Draw-GroundBase $bmp $stoneMid $stoneLight $stoneDark
    Fill-Ellipse $bmp 3 3 4 3 $coalMid
    Fill-Ellipse $bmp 10 4 3 4 $coalDark
    Fill-Ellipse $bmp 7 10 5 3 $coalMid
    Set-PixelSafe $bmp 4 4 $coalLight
    Set-PixelSafe $bmp 11 6 $coalLight
    Set-PixelSafe $bmp 9 11 $outline
}

Save-Sprite (Join-Path $textureRoot 'Entities\player.png') {
    param($bmp)
    Fill-Ellipse $bmp 5 1 6 5 $skin
    Fill-Rect $bmp 5 1 6 2 $hair
    Fill-Rect $bmp 4 6 8 4 $shirt
    Fill-Rect $bmp 4 7 1 2 $skin
    Fill-Rect $bmp 11 7 1 2 $skin
    Fill-Rect $bmp 5 10 3 4 $pants
    Fill-Rect $bmp 8 10 3 4 $pants
    Fill-Rect $bmp 4 14 4 1 $boot
    Fill-Rect $bmp 8 14 4 1 $boot
    Set-PixelSafe $bmp 7 3 $outline
    Set-PixelSafe $bmp 9 3 $outline
}

Save-Sprite (Join-Path $textureRoot 'Entities\rabbit.png') {
    param($bmp)
    Fill-Ellipse $bmp 4 7 8 6 $rabbitFur
    Fill-Ellipse $bmp 8 4 5 5 $rabbitFur
    Fill-Ellipse $bmp 8 1 2 5 $rabbitFur
    Fill-Ellipse $bmp 10 1 2 5 $rabbitFur
    Fill-Ellipse $bmp 8 2 1 3 $rabbitEar
    Fill-Ellipse $bmp 10 2 1 3 $rabbitEar
    Fill-Ellipse $bmp 4 8 6 4 $rabbitShade
    Set-PixelSafe $bmp 11 6 $outline
    Set-PixelSafe $bmp 3 9 $rabbitFur
}

Save-Sprite (Join-Path $textureRoot 'Entities\fish.png') {
    param($bmp)
    Fill-Ellipse $bmp 3 6 8 5 $fishBody
    Fill-Ellipse $bmp 4 7 6 3 $fishLight
    Draw-Line $bmp 10 8 14 5 $fishShade
    Draw-Line $bmp 10 8 14 11 $fishShade
    Draw-Line $bmp 10 8 14 8 $fishShade
    Draw-Line $bmp 6 5 8 3 $fishShade
    Set-PixelSafe $bmp 5 8 $outline
}

Save-Sprite (Join-Path $textureRoot 'Objects\rock.png') {
    param($bmp)
    Draw-Rock $bmp
}

Save-Sprite (Join-Path $textureRoot 'Objects\high_grass.png') {
    param($bmp)
    Draw-Line $bmp 4 15 5 7 $grassDark
    Draw-Line $bmp 7 15 7 4 $grassLight
    Draw-Line $bmp 10 15 9 6 $grassDark
    Draw-Line $bmp 12 15 11 8 $grassMid
    Draw-Line $bmp 2 15 4 9 $grassMid
    Draw-Line $bmp 6 15 9 10 $grassLight
}

Save-Sprite (Join-Path $textureRoot 'Objects\tree_sapling.png') {
    param($bmp)
    Draw-TreeCanopy $bmp 'Sapling'
}

Save-Sprite (Join-Path $textureRoot 'Objects\tree_young.png') {
    param($bmp)
    Draw-TreeCanopy $bmp 'Young'
}

Save-Sprite (Join-Path $textureRoot 'Objects\tree.png') {
    param($bmp)
    Draw-TreeCanopy $bmp 'Tree'
}

Save-Sprite (Join-Path $textureRoot 'Objects\ladder.png') {
    param($bmp)
    Draw-VLine $bmp 5 2 13 $woodDark
    Draw-VLine $bmp 6 2 13 $woodLight
    Draw-VLine $bmp 9 2 13 $woodDark
    Draw-VLine $bmp 10 2 13 $woodLight
    foreach ($y in 4, 6, 8, 10, 12) {
        Draw-HLine $bmp 5 10 $y $woodMid
    }
}

Save-Sprite (Join-Path $textureRoot 'Objects\boat.png') {
    param($bmp)
    Fill-Rect $bmp 4 8 8 4 $woodMid
    Draw-Line $bmp 2 9 4 8 $woodDark
    Draw-Line $bmp 2 10 4 11 $woodDark
    Draw-Line $bmp 11 8 13 9 $woodLight
    Draw-Line $bmp 11 11 13 10 $woodLight
    Draw-HLine $bmp 4 11 8 $woodLight
    Draw-HLine $bmp 4 11 11 $woodDark
}

Save-Sprite (Join-Path $textureRoot 'Objects\furnace.png') {
    param($bmp)
    Fill-Rect $bmp 3 3 10 10 $stoneMid
    Fill-Rect $bmp 4 4 8 2 $stoneLight
    Draw-HLine $bmp 3 12 3 $outline
    Draw-HLine $bmp 3 12 12 $outline
    Draw-VLine $bmp 3 3 12 $outline
    Draw-VLine $bmp 12 3 12 $outline
    Fill-Rect $bmp 6 8 4 3 $furnaceGlowDark
    Fill-Rect $bmp 7 9 2 1 $furnaceGlow
}

Save-Sprite (Join-Path $textureRoot 'Objects\stone_wall.png') {
    param($bmp)
    Fill-Rect $bmp 1 3 14 10 $stoneMid
    Draw-DotPattern $bmp 1 3 14 10 $stoneMid $stoneLight
    Draw-HLine $bmp 1 14 6 $stoneDark
    Draw-HLine $bmp 1 14 9 $stoneDark
    Draw-VLine $bmp 5 3 6 $stoneDark
    Draw-VLine $bmp 10 3 6 $stoneDark
    Draw-VLine $bmp 3 9 12 $stoneDark
    Draw-VLine $bmp 8 6 9 $stoneDark
    Draw-VLine $bmp 12 9 12 $stoneDark
    Draw-HLine $bmp 1 14 3 $outline
    Draw-HLine $bmp 1 14 12 $outline
}

Save-Sprite (Join-Path $textureRoot 'Objects\wood_wall.png') {
    param($bmp)
    Fill-Rect $bmp 2 2 12 12 $woodMid
    foreach ($x in 4, 7, 10) {
        Draw-VLine $bmp $x 2 13 $woodDark
    }
    Draw-HLine $bmp 2 13 2 $woodLight
    Set-PixelSafe $bmp 5 5 $outline
    Set-PixelSafe $bmp 9 9 $outline
}

Save-Sprite (Join-Path $textureRoot 'Objects\wood_door_closed.png') {
    param($bmp)
    Fill-Rect $bmp 3 2 10 12 $woodMid
    Fill-Rect $bmp 4 3 8 10 $woodLight
    Draw-HLine $bmp 3 12 2 $woodDark
    Draw-HLine $bmp 3 12 13 $woodDark
    Draw-VLine $bmp 3 2 13 $woodDark
    Draw-VLine $bmp 12 2 13 $woodDark
    Set-PixelSafe $bmp 10 8 $outline
    Set-PixelSafe $bmp 11 8 $ropeLight
}

Save-Sprite (Join-Path $textureRoot 'Objects\wood_door_open.png') {
    param($bmp)
    Fill-Rect $bmp 3 2 10 12 (New-Color 28 24 20 130)
    Fill-Rect $bmp 3 2 4 12 $woodMid
    Fill-Rect $bmp 4 3 2 10 $woodLight
    Draw-VLine $bmp 3 2 13 $woodDark
    Draw-VLine $bmp 6 2 13 $woodDark
    Set-PixelSafe $bmp 5 8 $ropeLight
}

Save-Sprite (Join-Path $textureRoot 'Objects\snare.png') {
    param($bmp)
    Draw-VLine $bmp 8 9 14 $woodDark
    Outline-Ellipse $bmp 4 3 8 7 $ropeLight
    Outline-Ellipse $bmp 5 4 6 5 $ropeDark
}

Save-Sprite (Join-Path $textureRoot 'Objects\snare_caught.png') {
    param($bmp)
    Draw-VLine $bmp 8 9 14 $woodDark
    Outline-Ellipse $bmp 4 3 8 7 $ropeLight
    Fill-Ellipse $bmp 6 6 5 4 $meat
    Fill-Ellipse $bmp 8 7 2 1 $meatLight
}

Save-Sprite (Join-Path $textureRoot 'Objects\wood_floor.png') {
    param($bmp)
    Draw-DotPattern $bmp 0 0 16 16 $woodMid $woodLight
    foreach ($y in 3, 7, 11, 15) {
        Draw-HLine $bmp 0 15 $y $woodDark
    }
}

Save-Sprite (Join-Path $textureRoot 'Items\stone.png') {
    param($bmp)
    Fill-Ellipse $bmp 4 6 8 5 $stoneMid
    Fill-Ellipse $bmp 6 7 3 2 $stoneLight
    Set-PixelSafe $bmp 9 10 $stoneDark
}

Save-Sprite (Join-Path $textureRoot 'Items\wood.png') {
    param($bmp)
    Fill-Rect $bmp 4 6 8 4 $woodMid
    Draw-HLine $bmp 4 11 6 $woodDark
    Draw-HLine $bmp 4 11 9 $woodLight
    Set-PixelSafe $bmp 4 8 $woodLight
    Set-PixelSafe $bmp 11 8 $woodDark
}

Save-Sprite (Join-Path $textureRoot 'Items\stick.png') {
    param($bmp)
    Draw-Line $bmp 4 12 11 4 $woodMid
    Draw-Line $bmp 5 12 12 4 $woodDark
    Draw-Line $bmp 8 8 10 9 $woodLight
}

Save-Sprite (Join-Path $textureRoot 'Items\rope.png') {
    param($bmp)
    Outline-Ellipse $bmp 3 4 10 8 $ropeLight
    Outline-Ellipse $bmp 5 6 6 4 $ropeDark
}

Save-Sprite (Join-Path $textureRoot 'Items\sand.png') {
    param($bmp)
    Fill-Ellipse $bmp 3 8 10 4 $sandMid
    Fill-Ellipse $bmp 5 7 6 3 $sandLight
    Draw-HLine $bmp 4 11 11 $sandDark
}

Save-Sprite (Join-Path $textureRoot 'Items\iron_ore.png') {
    param($bmp)
    Draw-OreItem $bmp $ironDark $ironMid $ironLight
}

Save-Sprite (Join-Path $textureRoot 'Items\copper_ore.png') {
    param($bmp)
    Draw-OreItem $bmp $copperDark $copperMid $copperLight
}

Save-Sprite (Join-Path $textureRoot 'Items\coal_ore.png') {
    param($bmp)
    Draw-OreItem $bmp $coalDark $coalMid $coalLight
}

Save-Sprite (Join-Path $textureRoot 'Items\fishing_rod.png') {
    param($bmp)
    Draw-Line $bmp 4 13 10 4 $woodMid
    Draw-Line $bmp 5 13 11 4 $woodDark
    Draw-Line $bmp 10 4 13 3 $ropeLight
    Draw-Line $bmp 13 3 13 8 $ropeLight
    Set-PixelSafe $bmp 12 8 $outline
    Set-PixelSafe $bmp 13 8 $outline
}

Save-Sprite (Join-Path $textureRoot 'Items\rabbit_meat.png') {
    param($bmp)
    Fill-Ellipse $bmp 4 6 8 6 $meat
    Fill-Ellipse $bmp 6 7 4 3 $meatLight
    Fill-Rect $bmp 10 8 3 2 $bone
    Set-PixelSafe $bmp 12 7 $bone
    Set-PixelSafe $bmp 12 10 $bone
}

Write-Output "Generated Factory texture set in $textureRoot"
