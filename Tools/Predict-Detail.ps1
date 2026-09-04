# G-002, Hilfsrechnung: wieviel Nachbarpixel-Delta bringt die Detaillage wirklich?
#
# Der Shader rechnet  detailMul = lerp(1, 2*d, s)  mit d aus der Graustufentextur.
# Fuer zwei Nachbarpixel gilt damit  dLuminanz = Mittelhelligkeit * 2 * s * dd,
# wobei dd der Nachbarabstand der Detailtextur ist - aber gemessen in der
# Aufloesung, in der sie tatsaechlich auf dem Bildschirm landet.
#
# Genau daran scheitert die erste Fassung: bei _DetailScale 2,7 liegen
# 1024 Texel auf 6/2,7 = 2,22 Welteinheiten, also 461 Texel je Einheit. Die
# Kamera loest rund 80 Bildschirmpixel je Einheit auf. Die Textur wird also
# rund 5,8-fach verkleinert, und das Mipmapping mittelt genau die Frequenzen
# weg, die Abnahmekriterium 1 misst.
#
# Dieses Skript bildet das nach: es verkleinert die Detailtextur um den
# jeweiligen Faktor (das entspricht der Mip-Auswahl), misst dort den
# Nachbarabstand und rechnet ihn auf Luminanz um.
#
#   .\Tools\Predict-Detail.ps1

param(
    [string]$Textur = "Assets\_Game\Art\Zones\Shared\ground_detail_grain_v01.png",
    [double]$Mittelhelligkeit = 51.4,
    [double]$Staerke = 0.35,
    [double]$PixelJeEinheit = 80.0,
    [double]$KachelWelt = 6.0,
    # Verkleinerung 1,0 - also Texel gleich Bildschirmpixel - liegt bei
    # 6 / (1024/80) = 0,469. Darunter wird die Textur vergroessert und das
    # Mipmapping greift nicht mehr.
    [double[]]$Skalen = @(2.7, 1.6, 1.05, 0.8, 0.586, 0.469, 0.375)
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$voll = (Resolve-Path (Join-Path "C:\Users\phine\Documents\Projekt Eidren" $Textur)).Path
$quelle = New-Object System.Drawing.Bitmap $voll
$texel = $quelle.Width

foreach ($scale in $Skalen) {

    # Wiederholungslaenge in Welteinheiten und daraus die Texeldichte
    $wiederholung = $KachelWelt / $scale
    $texelJeEinheit = $texel / $wiederholung
    $verkleinerung = $texelJeEinheit / $PixelJeEinheit

    # Die Textur auf die Aufloesung bringen, in der sie auf dem Schirm landet.
    $ziel = [int][math]::Max(8, [math]::Round($texel / $verkleinerung))
    $klein = New-Object System.Drawing.Bitmap $ziel, $ziel
    $g = [System.Drawing.Graphics]::FromImage($klein)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($quelle, 0, 0, $ziel, $ziel)
    $g.Dispose()

    # Nachbarabstand der verkleinerten Detailtextur, normiert auf 0..1
    $sum = 0.0; $n = 0
    for ($y = 0; $y -lt $ziel; $y++) {
        for ($x = 0; $x -lt $ziel; $x++) {
            $c = $klein.GetPixel($x, $y).R / 255.0
            if ($x + 1 -lt $ziel) { $sum += [math]::Abs($c - ($klein.GetPixel($x + 1, $y).R / 255.0)); $n++ }
            if ($y + 1 -lt $ziel) { $sum += [math]::Abs($c - ($klein.GetPixel($x, $y + 1).R / 255.0)); $n++ }
        }
    }
    $klein.Dispose()
    $dd = $sum / $n

    # dLuminanz = Mittelhelligkeit * 2 * Staerke * dd
    $beitrag = $Mittelhelligkeit * 2.0 * $Staerke * $dd

    [pscustomobject]@{
        DetailScale   = $scale
        WiederholungM = [math]::Round($wiederholung, 2)
        TexelJeEinheit= [math]::Round($texelJeEinheit, 0)
        Verkleinerung = [math]::Round($verkleinerung, 2)
        DeltaTextur   = [math]::Round($dd, 4)
        BeitragLumina = [math]::Round($beitrag, 3)
    }
}
$quelle.Dispose()
