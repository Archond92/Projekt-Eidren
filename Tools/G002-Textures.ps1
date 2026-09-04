# G-002: erzeugt die fehlenden Bodentexturen und die gemeinsamen Zusatzlagen.
#
# Zwei Aufgaben:
#
# 1. Detail- und Makrolage (Assets/_Game/Art/Zones/Shared/)
#    Graustufen, kachelbar, um 0,5 zentriert. Der Shader Eidren/Ground Detail
#    rechnet sie als Multiplikator auf die Basisfarbe; 0,5 ist neutral.
#
# 2. Eigene Basistexturen fuer TwilightGrove, VeilMarsh und GreyRifts.
#    Diese drei Gebiete liehen sich bisher die Textur eines Nachbargebiets und
#    waren dadurch von ihm nicht zu unterscheiden (Abnahmekriterium 7).
#    Sie werden NICHT neu gemalt, sondern aus der Geschwistertextur abgeleitet:
#    Farbgradation plus eine gebietseigene Zusatzlage. So bleibt der gemalte
#    Stil der Vorlage erhalten (Abnahmekriterium 6), waehrend Farbe und
#    Oberflaechencharakter eigenstaendig werden.
#
# Deterministisch: gleiche Saat -> gleiche Bilder. Erneutes Ausfuehren
# ueberschreibt die Ergebnisse identisch.

param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$Size = 1024
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class G002Textures
{
    // --- kachelbares Value-Noise ------------------------------------------
    // Das Gitter wird modulo der Periode gelesen, damit die linke Kante auf die
    // rechte passt. Ohne das haette jede Kachelgrenze eine sichtbare Naht.
    static double Hash(int x, int y, int period, int seed)
    {
        unchecked
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            h = h ^ (h >> 16);
            return (h & 0x7fffffff) / (double)0x7fffffff;
        }
    }

    static double Smooth(double t) { return t * t * (3.0 - 2.0 * t); }

    static double ValueNoise(double x, double y, int period, int seed)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        double xf = x - xi, yf = y - yi;
        double u = Smooth(xf), v = Smooth(yf);
        double a = Hash(xi,     yi,     period, seed);
        double b = Hash(xi + 1, yi,     period, seed);
        double c = Hash(xi,     yi + 1, period, seed);
        double d = Hash(xi + 1, yi + 1, period, seed);
        return (a * (1 - u) + b * u) * (1 - v) + (c * (1 - u) + d * u) * v;
    }

    // fBm ueber mehrere Oktaven. Die Periode verdoppelt sich mit der Frequenz,
    // sonst bricht die Kachelbarkeit in den hoeheren Oktaven.
    //
    // persistence steuert, wieviel Gewicht die feinen Oktaven behalten. 0,5 ist
    // der uebliche Wert und ergibt weiche Fleckigkeit; hoehere Werte verschieben
    // die Energie nach oben und ergeben Koernung. Das Ergebnis wird durch die
    // Amplitudensumme geteilt, der Startwert von amp ist deshalb ohne Wirkung.
    public static double Fbm(double x, double y, int basePeriod, int octaves, int seed, double persistence)
    {
        double sum = 0, amp = 1.0, norm = 0;
        int period = basePeriod;
        double freq = 1.0;
        for (int i = 0; i < octaves; i++)
        {
            sum  += ValueNoise(x * freq, y * freq, period, seed + i * 97) * amp;
            norm += amp;
            amp  *= persistence;
            freq *= 2.0;
            period *= 2;
        }
        return sum / norm;
    }

    // Die Zusatzlagen der abgeleiteten Texturen wurden mit der festen
    // Persistenz 0,5 entworfen und sollen sich nicht mitaendern.
    public static double Fbm(double x, double y, int basePeriod, int octaves, int seed)
    {
        return Fbm(x, y, basePeriod, octaves, seed, 0.5);
    }

    static byte Clamp(double v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)(v + 0.5);
    }

    // --- 1. Zusatzlagen ----------------------------------------------------
    // contrast steuert, wie weit die Werte von 0,5 abweichen duerfen.
    //
    // kornZellen/deckung/amplitude legen eine Streulage darueber: vereinzelte
    // harte Koerner, die auf einem eigenen ungeglaetteten Gitter sitzen. Der
    // Hash wird direkt gelesen statt ueber ValueNoise, denn geglaettetes
    // Rauschen haette weiche Raender - und genau die harte Kante traegt hier
    // den Nachbarabstand. kornZellen = 0 schaltet die Lage ab.
    //
    // deckung ist der Flaechenanteil je Vorzeichen; 0,02 ergibt 2 Prozent helle
    // und 2 Prozent dunkle Koerner, zusammen 4 Prozent Kornflaeche.
    public static void WriteNoise(string path, int size, int cells, int octaves, int seed,
                                  double contrast, double persistence,
                                  int kornZellen, double deckung, double amplitude)
    {
        using (var bmp = new Bitmap(size, size, PixelFormat.Format24bppRgb))
        {
            var d = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            byte[] buf = new byte[d.Stride * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    double n = Fbm(x / (double)size * cells, y / (double)size * cells, cells, octaves, seed, persistence);
                    double v = 0.5 + (n - 0.5) * contrast;

                    if (kornZellen > 0 && deckung > 0)
                    {
                        // Alle Texel einer Kornzelle teilen einen Zufallswert.
                        // Daraus entstehen Koerner von size/kornZellen Texeln
                        // Kantenlaenge mit harter Kante.
                        int kx = x * kornZellen / size;
                        int ky = y * kornZellen / size;
                        double k = Hash(kx, ky, kornZellen, seed + 5000);
                        if (k < deckung)            { v -= amplitude; }
                        else if (k > 1.0 - deckung) { v += amplitude; }
                    }

                    byte b = Clamp(v * 255.0);
                    int o = y * d.Stride + x * 3;
                    buf[o] = b; buf[o + 1] = b; buf[o + 2] = b;
                }
            System.Runtime.InteropServices.Marshal.Copy(buf, 0, d.Scan0, buf.Length);
            bmp.UnlockBits(d);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    // Die Makrolage traegt keine Streu; diese Ueberladung haelt ihren Aufruf kurz.
    public static void WriteNoise(string path, int size, int cells, int octaves, int seed, double contrast, double persistence)
    {
        WriteNoise(path, size, cells, octaves, seed, contrast, persistence, 0, 0.0, 0.0);
    }

    // --- 2. abgeleitete Gebietstexturen ------------------------------------
    // Ablauf je Pixel:
    //   1. Quellfarbe in Helligkeit und Farbabweichung zerlegen
    //   2. Saettigung daempfen, Zielton einmischen, Helligkeit skalieren
    //   3. gebietseigene Zusatzlage aufmodulieren (Streu, Schlick, Geroell)
    //
    // Der Zielton wird ueber die *relative* Helligkeit der Vorlage aufgetragen
    // (lum / Mittelwert), nicht ueber die absolute. Sonst faellt das Ergebnis
    // bei dunklen Vorlagen wie marsh_mud ins Schwarze, und der Zielton haette
    // keine vorhersagbare Helligkeit. So gilt: Mittelwert des Ergebnisses
    // entspricht ungefaehr Zielton * Helligkeitsfaktor.
    //
    // Rueckgabe: mittlere Ergebnisfarbe als {r, g, b} in 0..255, damit sich die
    // Unterscheidbarkeit der Gebiete (Abnahmekriterium 7) belegen laesst.
    public static double[] Derive(
        string sourcePath, string targetPath, int size,
        double tintR, double tintG, double tintB,   // Zielton 0..1
        double tintAmount,                          // wie stark der Zielton einmischt
        double saturation,                          // 1 = unveraendert, 0 = grau
        double value,                               // Helligkeitsfaktor
        string overlay, int seed)
    {
        using (var raw = new Bitmap(sourcePath))
        using (var src = new Bitmap(size, size, PixelFormat.Format24bppRgb))
        {
            using (var g = Graphics.FromImage(src))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(raw, 0, 0, size, size);
            }

            var sd = src.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] sb = new byte[sd.Stride * size];
            System.Runtime.InteropServices.Marshal.Copy(sd.Scan0, sb, 0, sb.Length);
            src.UnlockBits(sd);

            // erster Durchgang: mittlere Helligkeit der Vorlage
            double lumSum = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int o = y * sd.Stride + x * 3;
                    lumSum += 0.299 * sb[o + 2] + 0.587 * sb[o + 1] + 0.114 * sb[o];
                }
            double lumMean = Math.Max(1.0, lumSum / (size * (double)size)) / 255.0;

            double outR = 0, outG = 0, outB = 0;

            using (var dst = new Bitmap(size, size, PixelFormat.Format24bppRgb))
            {
                var dd = dst.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                byte[] db = new byte[dd.Stride * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int o = y * sd.Stride + x * 3;
                        double b = sb[o] / 255.0, gg = sb[o + 1] / 255.0, r = sb[o + 2] / 255.0;

                        double lum = 0.299 * r + 0.587 * gg + 0.114 * b;

                        // relative Helligkeit: traegt die gemalte Struktur,
                        // im Mittel 1.0
                        double struc = lum / lumMean;

                        // Saettigung um die eigene Helligkeit herum daempfen
                        r  = lum + (r  - lum) * saturation;
                        gg = lum + (gg - lum) * saturation;
                        b  = lum + (b  - lum) * saturation;

                        // Zielton einmischen: der Ton kommt vom Ziel, die
                        // Helligkeitsstruktur weiterhin von der Vorlage
                        r  = r  * (1 - tintAmount) + tintR * struc * tintAmount;
                        gg = gg * (1 - tintAmount) + tintG * struc * tintAmount;
                        b  = b  * (1 - tintAmount) + tintB * struc * tintAmount;

                        r *= value; gg *= value; b *= value;

                        double nx = x / (double)size, ny = y / (double)size;

                        if (overlay == "laubstreu")
                        {
                            // gefallenes Laub: mittelgrosse ockerfarbene Flecken,
                            // per Schwelle vereinzelt statt flaechig
                            double blob = Fbm(nx * 24, ny * 24, 24, 3, seed);
                            double mask = Math.Min(1.0, Math.Max(0.0, (blob - 0.55) / 0.30));
                            mask = mask * mask * 0.85;
                            r  = r  * (1 - mask) + 0.34 * struc * mask;
                            gg = gg * (1 - mask) + 0.24 * struc * mask;
                            b  = b  * (1 - mask) + 0.11 * struc * mask;
                        }
                        else if (overlay == "nassschlick")
                        {
                            // Nassglanz: breite, flache Aufhellungen wie stehendes
                            // Wasser, dazu dunkle Senken dazwischen
                            double wet = Fbm(nx * 7, ny * 7, 7, 4, seed);
                            double f = (wet - 0.5) * 2.0;
                            double mul = 1.0 + f * 0.30;
                            r *= mul; gg *= mul * 1.02; b *= mul * 1.05;
                        }
                        else if (overlay == "geroell")
                        {
                            // Geroell: kleine harte Splitter, hell und dunkel,
                            // ueber eine enge Schwelle vereinzelt
                            double chip = Fbm(nx * 96, ny * 96, 96, 2, seed);
                            double bright = Math.Max(0.0, (chip - 0.70) / 0.30);
                            double dark   = Math.Max(0.0, (0.30 - chip) / 0.30);
                            double mul = 1.0 + bright * 0.45 - dark * 0.35;
                            r *= mul; gg *= mul; b *= mul;
                        }

                        int d2 = y * dd.Stride + x * 3;
                        db[d2]     = Clamp(b  * 255.0);
                        db[d2 + 1] = Clamp(gg * 255.0);
                        db[d2 + 2] = Clamp(r  * 255.0);

                        outR += db[d2 + 2]; outG += db[d2 + 1]; outB += db[d2];
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(db, 0, dd.Scan0, db.Length);
                dst.UnlockBits(dd);
                dst.Save(targetPath, ImageFormat.Png);
            }

            double n = size * (double)size;
            return new double[] { outR / n, outG / n, outB / n };
        }
    }
}
'@

function New-Dir([string]$p) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Force $p | Out-Null } }

$zones  = Join-Path $ProjectRoot "Assets\_Game\Art\Zones"
$shared = Join-Path $zones "Shared"
New-Dir $shared

# --- Zusatzlagen ---------------------------------------------------------
# Die Zellzahl ist gemessen, nicht geschaetzt (siehe Tools\Try-Detail.ps1).
#
# Massgeblich ist der Nachbarabstand der Textur bei der Aufloesung, in der sie
# tatsaechlich auf dem Bildschirm landet. Die gemalten Basistexturen liegen dort
# bei 0,036 (greenwood_soil) bis 0,055 (greenwood_meadow). Die erste Fassung der
# Detaillage - 16 Zellen, Persistenz 0,5, Kontrast 0,6 - erreichte 0,0071 und
# war damit fuenfmal glatter als die Texturen, die sie strukturieren sollte.
# Die Messung bestaetigte das: die Erdflaeche lag vor der Detaillage bei 5,457
# und danach bei 5,348, also unveraendert.
#
#   Detail  96 Zellen, 4 Oktaven, Kontrast 1,00, Persistenz 0,75
#
#           Die Kachelung liegt bei _DetailScale 0,42 bewusst in der
#           Vergroesserung: 1024 Texel auf 6/0,42 = 14,3 Welteinheiten sind
#           71,6 Texel je Einheit gegen rund 80 Bildschirmpixel je Einheit.
#           Ein Texel deckt damit etwa 1,1 Pixel, das Mipmapping greift nicht.
#
#           Der erste Versuch lag bei _DetailScale 1,05 in der Verkleinerung
#           und war damit 2,24-fach minifiziert. Vorhergesagt waren daraus 2,6
#           zusaetzliches Nachbardelta, gemessen wurden 0,73: die Vorhersage
#           hatte mit bikubischer Verkleinerung gerechnet, die GPU nimmt aber
#           Mipmaps, und die Kameraneigung von 52 Grad erhoeht die Verkleinerung
#           entlang der Tiefenachse zusaetzlich. In der Vergroesserung faellt
#           diese Fehlerquelle weg.
#
#           4 Oktaven statt 2: bei gleichem Nachbarabstand entscheidet die
#           Oktavenzahl darueber, ob die Lage als Fernsehrauschen oder als
#           Oberflaeche liest. Die 2-Oktaven-Variante erreichte 0,054 statt
#           0,041, sah aber wie Bildstoerung aus (Abnahmekriterium 6).
#
#           Streu: Korn 512 (zwei Texel), Deckung 0,02, Amplitude 0,30
#
#           Die Koernung allein blieb unter dem Abnahmewert. Der Zusammenhang
#           ist messbar und nicht linear: Nachbarabstaende zweier unabhaengiger
#           Lagen setzen sich quadratisch zusammen. Am ganzen Bild von
#           04_boden_leer gilt
#
#               Ausgangswert                          5,486
#               mit Koernung, gemessen                7,444
#               Beitrag der Koernung                  Wurzel(7,444^2-5,486^2) = 5,031
#
#           Fuer den Auftragswert 7,74 braucht es einen Beitrag von
#           Wurzel(7,74^2-5,486^2) = 5,460.
#
#           Der erste Versuch nahm an, der Beitrag skaliere linear mit dem
#           Nachbarabstand der Lage, und waehlte 0,0508 (Kandidat S1 aus
#           Tools\Try-Streu3.ps1) fuer erwartete 8,33. Gemessen wurden 7,733 -
#           knapp unter dem Ziel. Die Annahme war falsch: eine Steigerung der
#           Lage um 24,5 Prozent brachte nur 8,3 Prozent mehr Beitrag, weil die
#           bilineare Filterung die harte Kante zwei Texel grosser Koerner
#           wieder aufweicht. Aus den zwei Messpunkten
#
#               Lage 0,0408 -> Beitrag 5,031 (Gesamtbild 7,444)
#               Lage 0,0508 -> Beitrag 5,450 (Gesamtbild 7,733)
#
#           folgt eine Steigung von rund 42 Beitrag je Einheit Nachbarabstand.
#           Gewaehlt wurde daraufhin 0,0639 (Kandidat S3, Deckung 0,04,
#           Amplitude 0,35), was rund 8,1 im Gesamtbild erwarten laesst. Die
#           Reserve ist Absicht: das Modell hat schon einmal zu hoch gegriffen.
#
#           Zwei Vorrunden sind verworfen: Try-Streu.ps1 und Try-Streu2.ps1
#           rechneten gegen den Erd-Ausschnitt (5,457) statt gegen das ganze
#           Bild und kamen auf einen noetigen Nachbarabstand von 0,084. Ihre
#           Kandidaten erreichten den Wert mit 12 bis 25 Prozent Kornflaeche
#           und lasen sich als Bildrauschen. Der Ausschnitt war als Diagnose
#           gedacht, nicht als Abnahmemass.
#   Makro   bei _MacroScale 0,137 -> Wiederholung alle 6/0,137 = 43,8 Einheiten
#           4 Zellen je Kachel    -> weiche Flecken von rund 11 Einheiten
#           Die Makrolage traegt bewusst keine Koernung: sie soll die
#           grossflaechige Wiederholung brechen, nicht die Oberflaeche.
#
# Keiner der beiden Faktoren ist ein ganzzahliger Teiler von 1. Waeren sie es,
# laege jede Zusatzlage phasengleich auf der Basiskachel und das Gesamtbild
# wiederholte sich weiter im 6-Einheiten-Raster. Genau dieses Raster soll
# Abnahmekriterium 2 unsichtbar machen.
$detail = Join-Path $shared "ground_detail_grain_v01.png"
$macro  = Join-Path $shared "ground_macro_variation_v01.png"
# Die Korngroesse haengt an der Aufloesung: $Size/2 ergibt immer Koerner von
# zwei Texeln Kantenlaenge, unabhaengig davon, mit welcher Groesse das Skript
# laeuft.
[G002Textures]::WriteNoise($detail, $Size,  96, 4, 20491, 1.00, 0.75, [int]($Size / 2), 0.04, 0.35)
[G002Textures]::WriteNoise($macro,  $Size,   4, 4, 20492, 0.80, 0.50)
"geschrieben: $detail"
"geschrieben: $macro"

# --- abgeleitete Gebietstexturen -----------------------------------------
# Aufbau je Eintrag: Quelle, Ziel, Zielton (r,g,b), Tonanteil, Saettigung,
# Helligkeit, Zusatzlage, Saat.
$abgeleitet = @(
    @{
        Quelle  = "Greenwood\greenwood_soil_v01.png"
        Ziel    = "TwilightGrove\twilight_leaf_litter_v01.png"
        Ton     = @(0.20, 0.27, 0.21)   # kuehles Blaugruen, gedaempft
        Anteil  = 0.80
        Saett   = 0.50
        Wert    = 1.00
        Overlay = "laubstreu"
        Saat    = 31
    },
    @{
        Quelle  = "Marsh\marsh_mud_v01.png"
        Ziel    = "VeilMarsh\veil_wet_silt_v01.png"
        Ton     = @(0.26, 0.29, 0.26)   # truebes Graugruen
        Anteil  = 0.85
        Saett   = 0.35
        Wert    = 1.00
        Overlay = "nassschlick"
        Saat    = 57
    },
    @{
        Quelle  = "Quarry\quarry_rock_v01.png"
        Ziel    = "GreyRifts\rift_cold_scree_v01.png"
        Ton     = @(0.31, 0.34, 0.41)   # kaltes Blaugrau
        Anteil  = 0.85
        Saett   = 0.30
        Wert    = 1.00
        Overlay = "geroell"
        Saat    = 83
    }
)

$mittel = @()
foreach ($e in $abgeleitet) {
    $src = Join-Path $zones $e.Quelle
    $dst = Join-Path $zones $e.Ziel
    if (-not (Test-Path $src)) { throw "Quelltextur fehlt: $src" }
    New-Dir (Split-Path -Parent $dst)
    $m = [G002Textures]::Derive(
        $src, $dst, $Size,
        $e.Ton[0], $e.Ton[1], $e.Ton[2],
        $e.Anteil, $e.Saett, $e.Wert,
        $e.Overlay, $e.Saat)
    "geschrieben: $dst"
    $mittel += [pscustomobject]@{
        Textur = Split-Path -Leaf $dst
        R = [math]::Round($m[0], 1)
        G = [math]::Round($m[1], 1)
        B = [math]::Round($m[2], 1)
        Vorlage = Split-Path -Leaf $src
    }
}

""
"Mittlere Farbe der abgeleiteten Texturen:"
$mittel | Format-Table -AutoSize
