# G-002: misst Mittelwert, Standardabweichung und mittleren Nachbarpixel-Abstand
# eines oder mehrerer Captures. Gleiche Helligkeitsformel wie G001VisualAbnahmeRunner
# (ITU-R 0.299/0.587/0.114), damit die Zahlen mit dem Abnahmelauf vergleichbar sind.
#
#   .\Tools\Measure-Ground.ps1 G001-Captures\Lauf1\04_boden_leer.png
#   .\Tools\Measure-Ground.ps1 (Get-ChildItem G001-Captures\Lauf1\*.png).FullName
#   .\Tools\Measure-Ground.ps1 -Bereich Erde  G001-Captures\Referenz\04_boden_leer.png
#
# Der Auftrag G-002 nennt zwei getrennte Ausgangswerte: Erdflaeche 2,58 bei
# StdAbw 5,3 und Mittel 51,6, Grasflaeche 8,84 bei 11,1 und 65,3. Beide wurden
# also auf Teilflaechen gemessen, nicht auf dem ganzen Bild. Wer das ganze Bild
# misst, mittelt beide Materialien und bekommt einen Wert, der zu keinem der
# beiden Abnahmewerte passt. -Bereich waehlt deshalb den Ausschnitt.
#
# Die Kaesten gelten fuer 04_boden_leer/05_boden_bewuchs bei 1920x1080: Erde
# liegt links, Gras rechts, die Materialkante laeuft schraeg von oben links nach
# unten rechts. Beide Kaesten halten deutlichen Abstand zur Kante, damit kein
# Uebergangspixel in die Messung geraet.

param(
    [ValidateSet("Voll", "Erde", "Gras")]
    [string]$Bereich = "Voll",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Pfad
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if (-not ([System.Management.Automation.PSTypeName]'GroundMeasure').Type) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class GroundMeasure
{
    // mean, stdDev, mittlerer Nachbarabstand, ausgewertete Pixel
    // rx/ry/rw/rh begrenzen den Ausschnitt; rw <= 0 bedeutet ganzes Bild.
    public static double[] Measure(string path, int rx, int ry, int rw, int rh)
    {
        using (var raw = new Bitmap(path))
        using (var bmp = new Bitmap(raw.Width, raw.Height, PixelFormat.Format24bppRgb))
        {
            using (var g = Graphics.FromImage(bmp)) { g.DrawImage(raw, 0, 0, raw.Width, raw.Height); }

            int iw = bmp.Width, ih = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, iw, ih), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = data.Stride;
            byte[] buf = new byte[stride * ih];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            bmp.UnlockBits(data);

            if (rw <= 0 || rh <= 0) { rx = 0; ry = 0; rw = iw; rh = ih; }
            rx = Math.Max(0, Math.Min(rx, iw - 1));
            ry = Math.Max(0, Math.Min(ry, ih - 1));
            rw = Math.Min(rw, iw - rx);
            rh = Math.Min(rh, ih - ry);

            double[] lum = new double[rw * rh];
            double sum = 0;
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                {
                    int o = (y + ry) * stride + (x + rx) * 3;
                    double l = 0.299 * buf[o + 2] + 0.587 * buf[o + 1] + 0.114 * buf[o];
                    lum[y * rw + x] = l;
                    sum += l;
                }
            double mean = sum / (rw * (double)rh);

            double var = 0;
            for (int i = 0; i < lum.Length; i++) { double d = lum[i] - mean; var += d * d; }
            double std = Math.Sqrt(var / lum.Length);

            double dsum = 0; long dcount = 0;
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                {
                    double l = lum[y * rw + x];
                    if (x + 1 < rw) { dsum += Math.Abs(l - lum[y * rw + x + 1]); dcount++; }
                    if (y + 1 < rh) { dsum += Math.Abs(l - lum[(y + 1) * rw + x]); dcount++; }
                }

            return new double[] { mean, std, dsum / dcount, rw * (double)rh };
        }
    }
}
'@
}

# x, y, Breite, Hoehe
$kasten = @{
    "Voll" = @(0, 0, 0, 0)
    "Erde" = @(60, 200, 390, 800)
    "Gras" = @(1600, 100, 280, 900)
}
$k = $kasten[$Bereich]

foreach ($p in $Pfad) {
    $full = Resolve-Path $p
    $m = [GroundMeasure]::Measure($full.Path, $k[0], $k[1], $k[2], $k[3])
    [pscustomobject]@{
        Datei     = Split-Path -Leaf $full.Path
        Ordner    = Split-Path -Leaf (Split-Path -Parent $full.Path)
        Bereich   = $Bereich
        Mittel    = [math]::Round($m[0], 3)
        StdAbw    = [math]::Round($m[1], 3)
        NachbarD  = [math]::Round($m[2], 3)
        Pixel     = [int]$m[3]
    }
}
