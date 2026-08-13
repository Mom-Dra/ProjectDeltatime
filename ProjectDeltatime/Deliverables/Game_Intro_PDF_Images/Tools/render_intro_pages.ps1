Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageDirectory = Split-Path -Parent $scriptDirectory
$sourceDirectory = Join-Path $packageDirectory 'Source'
$finalDirectory = Join-Path $packageDirectory 'Final'

$width = 1920
$height = 1080
$ink = [System.Drawing.Color]::FromArgb(255, 8, 16, 27)
$cyan = [System.Drawing.Color]::FromArgb(255, 45, 211, 236)
$orange = [System.Drawing.Color]::FromArgb(255, 255, 153, 53)
$white = [System.Drawing.Color]::FromArgb(255, 242, 247, 250)
$muted = [System.Drawing.Color]::FromArgb(255, 173, 193, 205)

function New-Canvas {
    $bitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear($ink)
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Draw-Cover {
    param([System.Drawing.Graphics]$Graphics, [string]$Path, [System.Drawing.Rectangle]$Target)

    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        $sourceRatio = $image.Width / [double]$image.Height
        $targetRatio = $Target.Width / [double]$Target.Height
        if ($sourceRatio -gt $targetRatio) {
            $sourceWidth = [int]($image.Height * $targetRatio)
            $sourceX = [int](($image.Width - $sourceWidth) / 2)
            $source = New-Object System.Drawing.Rectangle($sourceX, 0, $sourceWidth, $image.Height)
        }
        else {
            $sourceHeight = [int]($image.Width / $targetRatio)
            $sourceY = [int](($image.Height - $sourceHeight) / 2)
            $source = New-Object System.Drawing.Rectangle(0, $sourceY, $image.Width, $sourceHeight)
        }
        $Graphics.DrawImage($image, $Target, $source, [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally {
        $image.Dispose()
    }
}

function Draw-Contain {
    param([System.Drawing.Graphics]$Graphics, [string]$Path, [System.Drawing.Rectangle]$Target)

    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        $scale = [Math]::Min($Target.Width / [double]$image.Width, $Target.Height / [double]$image.Height)
        $drawWidth = [int]($image.Width * $scale)
        $drawHeight = [int]($image.Height * $scale)
        $drawX = $Target.X + [int](($Target.Width - $drawWidth) / 2)
        $drawY = $Target.Y + [int](($Target.Height - $drawHeight) / 2)
        $Graphics.DrawImage($image, (New-Object System.Drawing.Rectangle($drawX, $drawY, $drawWidth, $drawHeight)))
    }
    finally {
        $image.Dispose()
    }
}

function Add-Block {
    param([System.Drawing.Graphics]$Graphics, [System.Drawing.Rectangle]$Rectangle, [System.Drawing.Color]$Color)
    $brush = New-Object System.Drawing.SolidBrush($Color)
    try { $Graphics.FillRectangle($brush, $Rectangle) }
    finally { $brush.Dispose() }
}

function Add-Gradient {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Rectangle]$Rectangle,
        [System.Drawing.Color]$Start,
        [System.Drawing.Color]$End,
        [System.Drawing.Drawing2D.LinearGradientMode]$Direction
    )
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($Rectangle, $Start, $End, $Direction)
    try { $Graphics.FillRectangle($brush, $Rectangle) }
    finally { $brush.Dispose() }
}

function Draw-Label {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$Text,
        [float]$Size,
        [System.Drawing.Color]$Color,
        [float]$X,
        [float]$Y,
        [string]$Family = 'Malgun Gothic',
        [System.Drawing.FontStyle]$Style = [System.Drawing.FontStyle]::Bold
    )
    $font = New-Object System.Drawing.Font($Family, $Size, $Style, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object System.Drawing.SolidBrush($Color)
    try { $Graphics.DrawString($Text, $font, $brush, $X, $Y) }
    finally { $brush.Dispose(); $font.Dispose() }
}

function Draw-Line {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Color]$Color,
        [float]$Thickness,
        [float]$X1,
        [float]$Y1,
        [float]$X2,
        [float]$Y2
    )
    $pen = New-Object System.Drawing.Pen($Color, $Thickness)
    try { $Graphics.DrawLine($pen, $X1, $Y1, $X2, $Y2) }
    finally { $pen.Dispose() }
}

function Draw-Frame {
    param([System.Drawing.Graphics]$Graphics, [System.Drawing.Color]$Color = $cyan)
    $pen = New-Object System.Drawing.Pen($Color, 2)
    try {
        $Graphics.DrawRectangle($pen, 36, 36, $width - 72, $height - 72)
        $Graphics.DrawLine($pen, 36, 132, 36, 36)
        $Graphics.DrawLine($pen, 132, 36, 36, 36)
        $Graphics.DrawLine($pen, $width - 36, $height - 132, $width - 36, $height - 36)
        $Graphics.DrawLine($pen, $width - 132, $height - 36, $width - 36, $height - 36)
    }
    finally { $pen.Dispose() }
}

function Draw-Keycap {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Rectangle]$Rectangle,
        [string]$Label,
        [System.Drawing.Color]$Accent = $cyan,
        [float]$FontSize = 32
    )
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = 18
    $path.AddArc($Rectangle.X, $Rectangle.Y, $radius, $radius, 180, 90)
    $path.AddArc($Rectangle.Right - $radius, $Rectangle.Y, $radius, $radius, 270, 90)
    $path.AddArc($Rectangle.Right - $radius, $Rectangle.Bottom - $radius, $radius, $radius, 0, 90)
    $path.AddArc($Rectangle.X, $Rectangle.Bottom - $radius, $radius, $radius, 90, 90)
    $path.CloseFigure()
    $fill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(238, 14, 32, 48))
    $pen = New-Object System.Drawing.Pen($Accent, 3)
    $font = New-Object System.Drawing.Font('Segoe UI', $FontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object System.Drawing.SolidBrush($white)
    try {
        $Graphics.FillPath($fill, $path)
        $Graphics.DrawPath($pen, $path)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $layout = New-Object System.Drawing.RectangleF($Rectangle.X, $Rectangle.Y, $Rectangle.Width, $Rectangle.Height)
        $Graphics.DrawString($Label, $font, $brush, $layout, $format)
        $format.Dispose()
    }
    finally {
        $brush.Dispose(); $font.Dispose(); $pen.Dispose(); $fill.Dispose(); $path.Dispose()
    }
}

function Save-Page {
    param([hashtable]$Canvas, [string]$Name)
    $path = Join-Path $finalDirectory $Name
    $Canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Canvas.Graphics.Dispose()
    $Canvas.Bitmap.Dispose()
}

$stage3 = Join-Path $sourceDirectory 'Stage3Preview.png'
$stage4 = Join-Path $sourceDirectory 'Stage4Preview.png'
$stage5 = Join-Path $sourceDirectory 'Stage5Preview.png'
$stage6 = Join-Path $sourceDirectory 'Stage6Preview.png'
$tutorialSouth = Join-Path $sourceDirectory 'source_tutorial_south.png'
$tutorialNorth = Join-Path $sourceDirectory 'source_tutorial_north.png'

# 01 — overall action shooting.
$page = New-Canvas
Draw-Cover $page.Graphics $stage5 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Gradient $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, 940, $height)) ([System.Drawing.Color]::FromArgb(220, 6, 15, 25)) ([System.Drawing.Color]::FromArgb(0, 6, 15, 25)) ([System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(70, 110, 12, 238)) $cyan
Draw-Label $page.Graphics 'TIME-BENDING' 32 $cyan 110 115 'Segoe UI'
Draw-Label $page.Graphics 'ACTION SHOOTER' 68 $white 108 158 'Segoe UI'
Draw-Label $page.Graphics 'A fast tactical fight in a world you can slow down.' 28 $muted 112 250 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics
Save-Page $page '01_time_bending_action_shooter.png'

# 02 — world time slows when the player is still.
$page = New-Canvas
Draw-Cover $page.Graphics $stage4 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, $height)) ([System.Drawing.Color]::FromArgb(212, 4, 11, 22))
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(840, 88, 850, 904)) ([System.Drawing.Color]::FromArgb(225, 7, 21, 36))
Draw-Contain $page.Graphics $tutorialSouth (New-Object System.Drawing.Rectangle(880, 118, 770, 844))
$panelPen = New-Object System.Drawing.Pen($cyan, 3)
$page.Graphics.DrawRectangle($panelPen, 870, 108, 790, 864)
$panelPen.Dispose()
Draw-Label $page.Graphics 'STILLNESS' 30 $cyan 110 206 'Segoe UI'
Draw-Label $page.Graphics 'SLOWS THE WORLD' 62 $white 106 250 'Segoe UI'
Draw-Label $page.Graphics 'Stop. Observe. Decide.' 32 $muted 110 345 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Line $page.Graphics $cyan 3 110 406 692 406
Draw-Label $page.Graphics 'The game clock nearly stops when the player stops moving.' 26 $white 110 435 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics
Save-Page $page '02_stillness_slows_the_world.png'

# 03 — throwing and taking weapons.
$page = New-Canvas
Draw-Cover $page.Graphics $stage5 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Gradient $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, 240)) ([System.Drawing.Color]::FromArgb(200, 5, 13, 22)) ([System.Drawing.Color]::FromArgb(0, 5, 13, 22)) ([System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$dashPen = New-Object System.Drawing.Pen($orange, 8)
$dashPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
$page.Graphics.DrawCurve($dashPen, [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF(710, 760)), (New-Object System.Drawing.PointF(955, 610)), (New-Object System.Drawing.PointF(1235, 430))))
$dashPen.Dispose()
$ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 205, 95), 6)
$page.Graphics.DrawEllipse($ringPen, 1158, 350, 150, 150)
$ringPen.Dispose()
Draw-Label $page.Graphics 'THROW - STUN - TAKE OVER' 52 $white 92 94 'Segoe UI'
Draw-Label $page.Graphics 'Turn an enemy weapon into your next advantage.' 27 $muted 96 160 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics $orange
Save-Page $page '03_throw_stun_takeover.png'

# 04 — limited vision.
$page = New-Canvas
Draw-Cover $page.Graphics $stage3 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, $height)) ([System.Drawing.Color]::FromArgb(205, 1, 6, 16))
$cone = New-Object System.Drawing.Drawing2D.GraphicsPath
$cone.AddPolygon([System.Drawing.Point[]]@((New-Object System.Drawing.Point(1020, 1000)), (New-Object System.Drawing.Point(470, 120)), (New-Object System.Drawing.Point(1570, 120))))
$page.Graphics.SetClip($cone)
Draw-Cover $page.Graphics $stage3 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
$page.Graphics.ResetClip()
$conePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(205, 45, 211, 236), 4)
$page.Graphics.DrawPath($conePen, $cone)
$conePen.Dispose(); $cone.Dispose()
Add-Gradient $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, 235)) ([System.Drawing.Color]::FromArgb(225, 4, 12, 23)) ([System.Drawing.Color]::FromArgb(0, 4, 12, 23)) ([System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
Draw-Label $page.Graphics 'LIMITED VISION' 58 $white 92 86 'Segoe UI'
Draw-Label $page.Graphics 'Read the shadows. Prepare for sudden attacks.' 28 $muted 96 155 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics
Save-Page $page '04_limited_vision.png'

# 05 — stage-clear replay.
$page = New-Canvas
Draw-Cover $page.Graphics $stage6 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Gradient $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, 1040, $height)) ([System.Drawing.Color]::FromArgb(190, 5, 13, 23)) ([System.Drawing.Color]::FromArgb(0, 5, 13, 23)) ([System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
$ghostBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(90, 52, 226, 242))
$ghostPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 89, 236, 246), 4)
for ($i = 0; $i -lt 3; $i++) {
    $diameter = 100 + ($i * 25)
    $page.Graphics.FillEllipse($ghostBrush, 1018 - ($i * 120), 620 + ($i * 37), $diameter, $diameter)
    $page.Graphics.DrawEllipse($ghostPen, 1018 - ($i * 120), 620 + ($i * 37), $diameter, $diameter)
}
$ghostPen.Dispose(); $ghostBrush.Dispose()
Draw-Label $page.Graphics 'REPLAY THE MOMENT' 60 $white 94 102 'Segoe UI'
Draw-Label $page.Graphics 'Relive the action you created after clearing a stage.' 28 $muted 98 176 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics
Save-Page $page '05_stage_clear_replay.png'

# 06 — controls reference page.
$page = New-Canvas
Draw-Cover $page.Graphics $stage5 (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, $height)) ([System.Drawing.Color]::FromArgb(227, 4, 13, 24))
Draw-Label $page.Graphics 'CONTROLS' 68 $white 100 86 'Segoe UI'
Draw-Label $page.Graphics 'KEYBOARD + MOUSE' 26 $cyan 104 170 'Segoe UI'

Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(110, 250, 100, 74)) 'W'
Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(42, 334, 100, 74)) 'A'
Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(152, 334, 100, 74)) 'S'
Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(262, 334, 100, 74)) 'D'
Draw-Label $page.Graphics 'MOVE' 30 $white 400 322 'Segoe UI'

Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(585, 250, 152, 74)) 'LMB' $orange 27
Draw-Label $page.Graphics 'ATTACK / AUTO FIRE' 30 $white 770 272 'Segoe UI'
Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(585, 346, 152, 74)) 'RMB' $orange 27
Draw-Label $page.Graphics 'THROW WEAPON' 30 $white 770 368 'Segoe UI'

Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(110, 506, 100, 74)) 'Q' $orange
Draw-Label $page.Graphics 'DEADLINE' 30 $white 245 528
Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(585, 506, 184, 74)) 'SPACE'
Draw-Label $page.Graphics 'DASH' 30 $white 800 528 'Segoe UI'

Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(110, 654, 100, 74)) 'E'
Draw-Label $page.Graphics 'CATCH / PICK UP / SWAP' 30 $white 245 676 'Segoe UI'
Draw-Keycap $page.Graphics (New-Object System.Drawing.Rectangle(585, 654, 100, 74)) 'R'
Draw-Label $page.Graphics 'RESTART' 30 $white 720 676 'Segoe UI'

Draw-Label $page.Graphics 'MOUSE' 30 $cyan 1180 255 'Segoe UI'
Draw-Line $page.Graphics $cyan 3 1180 303 1740 303
Draw-Label $page.Graphics 'AIM' 42 $white 1180 330 'Segoe UI'
Draw-Label $page.Graphics 'Move the mouse to aim.' 25 $muted 1180 400 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(110, 820, 1665, 116)) ([System.Drawing.Color]::FromArgb(100, 45, 211, 236))
Draw-Label $page.Graphics 'WASD MOVE  |  MOUSE AIM  |  LMB ATTACK  |  RMB THROW WEAPON' 28 $white 142 840 'Segoe UI'
Draw-Label $page.Graphics 'Q DEADLINE  |  SPACE DASH  |  E CATCH / PICK UP / SWAP  |  R RESTART' 28 $white 142 885 'Segoe UI'
Draw-Frame $page.Graphics
Save-Page $page '06_controls_reference.png'

# 07 — Deadline survival ability.
$page = New-Canvas
Draw-Cover $page.Graphics $tutorialNorth (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Add-Block $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, $height)) ([System.Drawing.Color]::FromArgb(84, 13, 80, 105))
$deadlinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(240, 87, 244, 255), 8)
for ($i = 0; $i -lt 4; $i++) {
    $offset = $i * 74
    $page.Graphics.DrawEllipse($deadlinePen, 760 - $offset, 508 - $offset, 400 + ($offset * 2), 260 + ($offset * 2))
}
$deadlinePen.Dispose()
Add-Gradient $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, 270)) ([System.Drawing.Color]::FromArgb(220, 5, 13, 24)) ([System.Drawing.Color]::FromArgb(0, 5, 13, 24)) ([System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
Draw-Label $page.Graphics 'DEADLINE' 70 $white 96 86 'Segoe UI'
Draw-Label $page.Graphics 'Freeze the crisis. Prepare your escape.' 29 $cyan 100 171 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics $cyan
Save-Page $page '07_deadline_escape.png'

# 08 — four-weapon showcase, normalized to the page size used by the set.
$page = New-Canvas
Draw-Cover $page.Graphics (Join-Path $finalDirectory '08_weapons_loadout.png') (New-Object System.Drawing.Rectangle(0, 0, $width, $height))
Draw-Frame $page.Graphics
Save-Page $page '08_weapons_loadout.png'

# 09 — browser play mockup based on a real game scene.
$page = New-Canvas
Add-Gradient $page.Graphics (New-Object System.Drawing.Rectangle(0, 0, $width, $height)) ([System.Drawing.Color]::FromArgb(255, 5, 15, 28)) ([System.Drawing.Color]::FromArgb(255, 14, 49, 69)) ([System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
$shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(150, 0, 0, 0))
$page.Graphics.FillRectangle($shadow, 244, 174, 1440, 792)
$shadow.Dispose()
$browserFill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 20, 35, 50))
$page.Graphics.FillRectangle($browserFill, 210, 132, 1440, 792)
$browserFill.Dispose()
$browserBar = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 34, 57, 75))
$page.Graphics.FillRectangle($browserBar, 210, 132, 1440, 86)
$browserBar.Dispose()
for ($i = 0; $i -lt 3; $i++) {
    $dotBrush = New-Object System.Drawing.SolidBrush(@($orange, $cyan, $white)[$i])
    $page.Graphics.FillEllipse($dotBrush, 244 + ($i * 34), 164, 18, 18)
    $dotBrush.Dispose()
}
$urlFill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 12, 26, 39))
$page.Graphics.FillRectangle($urlFill, 386, 153, 1120, 44)
$urlFill.Dispose()
Draw-Label $page.Graphics 'WEB BUILD - PLAY NOW' 20 $muted 416 161 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Cover $page.Graphics $stage6 (New-Object System.Drawing.Rectangle(230, 238, 1400, 666))
$browserPen = New-Object System.Drawing.Pen($cyan, 3)
$page.Graphics.DrawRectangle($browserPen, 210, 132, 1440, 792)
$browserPen.Dispose()
Draw-Label $page.Graphics 'INSTANT PLAY' 58 $white 116 962 'Segoe UI'
Draw-Label $page.Graphics 'Access the web build and play without installation.' 28 $muted 608 978 'Segoe UI' ([System.Drawing.FontStyle]::Regular)
Draw-Frame $page.Graphics
Save-Page $page '09_browser_instant_play.png'

Write-Host "Rendered 8 composited pages in $finalDirectory"
