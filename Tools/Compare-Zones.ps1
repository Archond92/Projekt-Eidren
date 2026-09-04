# G-002, Abnahmekriterium 7: jedes Startgebiet braucht eine eigene,
# unterscheidbare Bodenwirkung.
#
# Der Auftragstext nennt "alle vier Startgebiete". Das ist gegenueber dem Stand
# des Projekts veraltet: v0.2 hat acht Gebiete (sieben Aussengebiete plus
# HomeBase). Geprueft werden deshalb alle acht.
#
# Gemessen wird die Basistextur jedes Gebiets: mittlere Farbe und der Abstand
# zum jeweils aehnlichsten anderen Gebiet. Vor G-002 liehen sich drei Gebiete
# die Textur eines Nachbarn woertlich - TwilightGrove von Greenwood, VeilMarsh
# von Marsh, GreyRifts von Quarry - und hatten zu diesem Nachbarn den Abstand
# null. Genau das soll diese Messung ausschliessen.
#
#   .\Tools\Compare-Zones.ps1

param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

# Zuordnung wie in AreaArtAssetBuilder.cs
$gebiete = [ordered]@{
    "Greenwood"     = "Greenwood\greenwood_meadow_v01.png"
    "Marsh"         = "Marsh\marsh_peat_moss_v01.png"
    "Quarry"        = "Quarry\quarry_gravel_v01.png"
    "EmberRuins"    = "EmberRuins\ember_ash_v01.png"
    "TwilightGrove" = "TwilightGrove\twilight_leaf_litter_v01.png"
    "VeilMarsh"     = "VeilMarsh\veil_wet_silt_v01.png"
    "GreyRifts"     = "GreyRifts\rift_cold_scree_v01.png"
    "HomeBase"      = "HomeBase\home_trampled_clay_v01.png"
}

$zones = Join-Path $ProjectRoot "Assets\_Game\Art\Zones"
$werte = @{}

foreach ($name in $gebiete.Keys) {
    $pfad = Join-Path $zones $gebiete[$name]
    if (-not (Test-Path $pfad)) { throw "Textur fehlt: $pfad" }

    $bmp = New-Object System.Drawing.Bitmap $pfad
    # Auf 128 verkleinern: die mittlere Farbe aendert sich dadurch nicht
    # nennenswert, die Messung laeuft aber in Sekunden statt Minuten.
    $k = New-Object System.Drawing.Bitmap 128, 128
    $g = [System.Drawing.Graphics]::FromImage($k)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($bmp, 0, 0, 128, 128)
    $g.Dispose(); $bmp.Dispose()

    $r = 0.0; $gr = 0.0; $b = 0.0
    for ($y = 0; $y -lt 128; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            $p = $k.GetPixel($x, $y)
            $r += $p.R; $gr += $p.G; $b += $p.B
        }
    }
    $k.Dispose()
    $n = 128 * 128
    # Klammern noetig: das Komma bindet in PowerShell staerker als die Division,
    # ohne sie wird durch ein Array geteilt.
    $werte[$name] = @(($r / $n), ($gr / $n), ($b / $n))
}

# Abstand zum aehnlichsten anderen Gebiet
$zeilen = @()
foreach ($name in $gebiete.Keys) {
    $a = $werte[$name]
    $minAbstand = [double]::MaxValue
    $naechster = ""
    foreach ($other in $gebiete.Keys) {
        if ($other -eq $name) { continue }
        $o = $werte[$other]
        $d = [math]::Sqrt(($a[0]-$o[0])*($a[0]-$o[0]) + ($a[1]-$o[1])*($a[1]-$o[1]) + ($a[2]-$o[2])*($a[2]-$o[2]))
        if ($d -lt $minAbstand) { $minAbstand = $d; $naechster = $other }
    }
    $zeilen += [pscustomobject]@{
        Gebiet      = $name
        R           = [math]::Round($a[0], 1)
        G           = [math]::Round($a[1], 1)
        B           = [math]::Round($a[2], 1)
        Naechstes   = $naechster
        Abstand     = [math]::Round($minAbstand, 1)
    }
}

$zeilen | Format-Table -AutoSize
$min = ($zeilen | Measure-Object -Property Abstand -Minimum).Minimum
"Kleinster Abstand zwischen zwei Gebieten: $min"
"Vor G-002 war dieser Wert 0,0 - drei Gebiete teilten sich die Textur eines Nachbarn."
