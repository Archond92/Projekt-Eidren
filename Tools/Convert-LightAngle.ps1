# G-003: rechnet zwischen der Ausrichtung des Richtungslichts und der Richtung,
# aus der das Licht im fertigen Bild zu kommen scheint, um.
#
#   .\Tools\Convert-LightAngle.ps1 -SonneX 48 -SonneY -32          # Ist-Stand
#   .\Tools\Convert-LightAngle.ps1 -BildWinkel 113.5               # Sollrichtung
#
# Warum das Werkzeug noetig ist: Measure-LightDirection.ps1 liefert die
# eingemalte Lichtrichtung als Winkel in der Bildebene. Die Sonne wird aber als
# Euler-Drehung in der Welt gesetzt. Zwischen beiden steht die Kamera, und die
# steht auf Euler(52, 45, 0) - geneigt UND gedreht. Ohne diese Umrechnung laesst
# sich Kriterium 1 aus G-003 ("Lichtrichtung der Szene stimmt mit der
# eingemalten ueberein") nicht pruefen, sondern nur behaupten.
#
# Winkelkonvention wie in Measure-LightDirection.ps1:
#   0 Grad = Licht von rechts, 90 = von oben, 180 = von links, 270 = von unten.
#
# Bei -BildWinkel wird die Loesung ohne Vorder-/Rueckanteil gewaehlt, das Licht
# liegt dann genau in der Bildebene. Diese Wahl ist nicht zwingend: zu jedem
# Bildwinkel gibt es eine ganze Schar von Sonnenstellungen, die sich nur in der
# Tiefe unterscheiden. Die Bildebenen-Loesung ist die neutrale davon - weder
# Gegen- noch Frontlicht - und wird deshalb als Ausgangspunkt genommen.
# -Tiefe verschiebt sie: negative Werte holen die Sonne zur Kamera (Frontlicht),
# positive schieben sie dahinter (Gegenlicht).

param(
    # Ausrichtung des Lichts als Unity-Euler. Angeben fuer die Richtung Sonne -> Bild.
    [double]$SonneX = [double]::NaN,
    [double]$SonneY = [double]::NaN,

    # Gewuenschte Richtung im Bild. Angeben fuer die Richtung Bild -> Sonne.
    [double]$BildWinkel = [double]::NaN,

    # Anteil senkrecht zur Bildebene, nur bei -BildWinkel. -1 bis 1.
    [double]$Tiefe = 0,

    # Kameradrehung. Vorgabe ist der Stand aus G001VisualAbnahmeRunner.
    [double]$KameraX = 52,
    [double]$KameraY = 45
)

$ErrorActionPreference = "Stop"

function ToRad([double]$g) { $g * [math]::PI / 180 }
function ToDeg([double]$r) { $r * 180 / [math]::PI }

# Begrenzt auf -1..1 vor dem Arcussinus. Die Schranken muessen 1.0 und -1.0
# heissen, nicht 1 und -1: bei Ganzzahlliteralen waehlt PowerShell die
# Min(int,int)-Ueberladung und rundet das Argument. [math]::Min(1, 0.7431)
# ergibt dann 1 und der Winkel wird zu 90 Grad.
function Clamp1([double]$w) { [math]::Max(-1.0, [math]::Min(1.0, $w)) }

# Unity setzt Euler-Drehungen in der Reihenfolge Z, X, Y zusammen; bei Z = 0
# bleibt R = Ry * Rx. Beide Funktionen unten bilden genau das ab.
function RotYX([double]$gx, [double]$gy, [double[]]$v) {
    $cx = [math]::Cos((ToRad $gx)); $sx = [math]::Sin((ToRad $gx))
    $cy = [math]::Cos((ToRad $gy)); $sy = [math]::Sin((ToRad $gy))
    # erst um X
    $x1 = $v[0]; $y1 = $v[1] * $cx - $v[2] * $sx; $z1 = $v[1] * $sx + $v[2] * $cx
    # dann um Y
    @(($x1 * $cy + $z1 * $sy), $y1, (-$x1 * $sy + $z1 * $cy))
}
function Dot([double[]]$a, [double[]]$b) { $a[0]*$b[0] + $a[1]*$b[1] + $a[2]*$b[2] }

# Kamerabasis in Weltkoordinaten
$right   = RotYX $KameraX $KameraY @(1, 0, 0)
$up      = RotYX $KameraX $KameraY @(0, 1, 0)
$forward = RotYX $KameraX $KameraY @(0, 0, 1)

if (-not [double]::IsNaN($SonneX) -and -not [double]::IsNaN($SonneY)) {
    # Richtung, in die das Licht laeuft
    $dir = RotYX $SonneX $SonneY @(0, 0, 1)
    # Richtung, aus der es kommt
    $L = @(-$dir[0], -$dir[1], -$dir[2])

    $sx = Dot $L $right
    $sy = Dot $L $up
    $sz = Dot $L $forward
    $winkel = (ToDeg ([math]::Atan2($sy, $sx)) + 360) % 360

    [pscustomobject]@{
        Richtung    = "Sonne -> Bild"
        SonneEuler  = "($SonneX, $SonneY, 0)"
        Hoehe       = [math]::Round((ToDeg ([math]::Asin((Clamp1 $L[1])))), 1)
        BildWinkel  = [math]::Round($winkel, 1)
        Tiefe       = [math]::Round($sz, 3)
        Art         = if ($sz -lt -0.15) { "Frontlicht" } elseif ($sz -gt 0.15) { "Gegenlicht" } else { "Seitenlicht" }
    }
}
elseif (-not [double]::IsNaN($BildWinkel)) {
    if ([math]::Abs($Tiefe) -ge 1) { throw "-Tiefe muss zwischen -1 und 1 liegen." }
    $eben = [math]::Sqrt(1 - $Tiefe * $Tiefe)
    $a = $eben * [math]::Cos((ToRad $BildWinkel))
    $b = $eben * [math]::Sin((ToRad $BildWinkel))

    # L = a*right + b*up + Tiefe*forward
    #
    # Die Klammern um jedes Element sind nicht Zierde: in PowerShell bindet das
    # Komma staerker als * und +. Ohne sie liest der Parser die drei Zeilen als
    # einen einzigen Ausdruck und multipliziert gegen ein Array.
    $L = @(
        ($a * $right[0] + $b * $up[0] + $Tiefe * $forward[0]),
        ($a * $right[1] + $b * $up[1] + $Tiefe * $forward[1]),
        ($a * $right[2] + $b * $up[2] + $Tiefe * $forward[2])
    )

    if ($L[1] -le 0) {
        Write-Warning ("Die Sonne stuende {0:N1} Grad UNTER dem Horizont. Bei diesem Bildwinkel ist -Tiefe zu veraendern." -f (ToDeg ([math]::Asin((Clamp1 $L[1])))))
    }

    # Umkehrung von RotYX(x, y, (0,0,1)) = (cos x * sin y, -sin x, cos x * cos y)
    $D = @(-$L[0], -$L[1], -$L[2])
    $gx = ToDeg ([math]::Asin((Clamp1 (-$D[1]))))
    $gy = ToDeg ([math]::Atan2($D[0], $D[2]))

    # Gegenprobe: zurueckrechnen und den Bildwinkel erneut bestimmen.
    $probe = RotYX $gx $gy @(0, 0, 1)
    $pL = @(-$probe[0], -$probe[1], -$probe[2])
    $pWinkel = (ToDeg ([math]::Atan2((Dot $pL $up), (Dot $pL $right))) + 360) % 360

    [pscustomobject]@{
        Richtung   = "Bild -> Sonne"
        BildWinkel = $BildWinkel
        SonneEuler = "($([math]::Round($gx,1)), $([math]::Round($gy,1)), 0)"
        Hoehe      = [math]::Round((ToDeg ([math]::Asin((Clamp1 $L[1])))), 1)
        Tiefe      = $Tiefe
        Gegenprobe = [math]::Round($pWinkel, 1)
    }
}
else {
    throw "Entweder -SonneX und -SonneY angeben, oder -BildWinkel."
}
