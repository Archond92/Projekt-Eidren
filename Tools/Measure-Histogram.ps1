# G-003: misst Histogramm und Tonwertumfang eines Captures.
#
#   .\Tools\Measure-Histogram.ps1 G001-Captures\Lauf1\01_uebersicht.png
#   .\Tools\Measure-Histogram.ps1 (Get-ChildItem G001-Captures\Lauf1\*.png).FullName
#
# Ergaenzt Measure-Ground.ps1 um genau die Groessen, die G-003 braucht und das
# Bodenwerkzeug nicht liefert:
#
# - Perzentile P1/P5/P50/P95/P99 - der nutzbare Tonwertumfang. P99 minus P1 ist
#   die Kennzahl "Tonwertumfang" der Abnahme; Minimum und Maximum allein waeren
#   durch einzelne Ausreisserpixel wertlos.
# - Anteil ausgebrannt (>= 250) und abgesoffen (<= 5) - Kriterium 6 der
#   G-003-Abnahme verlangt, dass Zeichnung in Lichtern und Tiefen erhalten
#   bleibt. Die Schwellen liegen bewusst nicht auf 255/0: Tonemapping drueckt
#   Werte asymptotisch dagegen, ohne sie je ganz zu erreichen.
# - Histogramm als 16er-Bloecke fuer den Bericht, als Text statt als Bild -
#   vergleichbar per Diff.
#
# Helligkeitsformel ITU-R 0.299/0.587/0.114 wie in Measure-Ground.ps1 und
# G001VisualAbnahmeRunner, damit alle Zahlen der Abnahme vergleichbar bleiben.

param(
    # Histogrammbloecke mit ausgeben
    [switch]$Bloecke,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Pfad
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if (-not ([System.Management.Automation.PSTypeName]'HistogramMeasure').Type) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class HistogramMeasure
{
    // 256 Eintraege Histogramm, danach mean und stdDev
    public static double[] Measure(string path)
    {
        using (var raw = new Bitmap(path))
        using (var bmp = new Bitmap(raw.Width, raw.Height, PixelFormat.Format24bppRgb))
        {
            using (var g = Graphics.FromImage(bmp)) { g.DrawImage(raw, 0, 0, raw.Width, raw.Height); }

            int w = bmp.Width, h = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = data.Stride;
            byte[] buf = new byte[stride * h];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            bmp.UnlockBits(data);

            double[] result = new double[258];
            double sum = 0, sqSum = 0;
            long n = (long)w * h;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int o = y * stride + x * 3;
                    double l = 0.299 * buf[o + 2] + 0.587 * buf[o + 1] + 0.114 * buf[o];
                    int bin = (int)Math.Round(l); if (bin > 255) bin = 255;
                    result[bin]++;
                    sum += l; sqSum += l * l;
                }
            double mean = sum / n;
            result[256] = mean;
            result[257] = Math.Sqrt(sqSum / n - mean * mean);
            return result;
        }
    }
}
'@
}

function Perzentil([double[]]$hist, [long]$gesamt, [double]$p) {
    [long]$ziel = [math]::Ceiling($gesamt * $p / 100.0)
    [long]$lauf = 0
    for ($i = 0; $i -lt 256; $i++) {
        $lauf += [long]$hist[$i]
        if ($lauf -ge $ziel) { return $i }
    }
    return 255
}

foreach ($p in $Pfad) {
    $full = Resolve-Path $p
    $m = [HistogramMeasure]::Measure($full.Path)
    $hist = $m[0..255]
    [long]$gesamt = 0; foreach ($v in $hist) { $gesamt += [long]$v }

    [long]$dunkel = 0; for ($i = 0; $i -le 5;   $i++) { $dunkel += [long]$hist[$i] }
    [long]$hell   = 0; for ($i = 250; $i -le 255; $i++) { $hell += [long]$hist[$i] }

    $p1 = Perzentil $hist $gesamt 1
    $p99 = Perzentil $hist $gesamt 99

    [pscustomobject]@{
        Datei        = Split-Path -Leaf $full.Path
        Ordner       = Split-Path -Leaf (Split-Path -Parent $full.Path)
        Mittel       = [math]::Round($m[256], 3)
        StdAbw       = [math]::Round($m[257], 3)
        P1           = $p1
        P5           = Perzentil $hist $gesamt 5
        P50          = Perzentil $hist $gesamt 50
        P95          = Perzentil $hist $gesamt 95
        P99          = $p99
        Tonwertumfang = $p99 - $p1
        AbgesoffenProz = [math]::Round($dunkel * 100.0 / $gesamt, 3)
        AusgebranntProz = [math]::Round($hell * 100.0 / $gesamt, 3)
    }

    if ($Bloecke) {
        $max = 0.0
        $bl = New-Object double[] 16
        for ($b = 0; $b -lt 16; $b++) {
            for ($i = $b * 16; $i -lt ($b + 1) * 16; $i++) { $bl[$b] += $hist[$i] }
            if ($bl[$b] -gt $max) { $max = $bl[$b] }
        }
        for ($b = 0; $b -lt 16; $b++) {
            $balken = ""
            if ($max -gt 0) { $balken = "#" * [int][math]::Round($bl[$b] / $max * 50) }
            "{0,3}-{1,3} {2,6:N2}% {3}" -f ($b * 16), ($b * 16 + 15), ($bl[$b] * 100.0 / $gesamt), $balken
        }
    }
}
