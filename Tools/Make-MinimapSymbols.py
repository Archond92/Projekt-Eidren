"""Erzeugt die beiden Minimap-Symbole (N04-001) als weisse Silhouetten mit Alpha.

Weiss + Alpha, damit der Presenter sie ueber Image.color einfaerben kann:
Boss rot, Kiste bernstein. Gezeichnet wird vierfach ueberabgetastet und dann
heruntergerechnet — das ergibt saubere Kanten ohne Treppen.
"""

import os
import sys
from PIL import Image, ImageDraw

SIZE = 128
SS = 4            # Ueberabtastung
S = SIZE * SS


def scaled(*values):
    return [v * SS for v in values]


def new_mask():
    return Image.new("L", (S, S), 0)


def finish(mask, path):
    mask = mask.resize((SIZE, SIZE), Image.LANCZOS)
    out = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    out.putalpha(mask)
    white = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 255))
    white.putalpha(mask)
    white.save(path)
    return path


def boss_skull(path):
    mask = new_mask()
    d = ImageDraw.Draw(mask)

    # Hirnschale
    d.ellipse(scaled(24, 16, 104, 92), fill=255)
    # Kiefer, etwas schmaler und unten abgerundet
    d.rounded_rectangle(scaled(42, 74, 86, 108), radius=10 * SS, fill=255)

    # Augenhoehlen ausschneiden. Sie sind das einzige Merkmal, das bei 18 px auf
    # der Karte ueberlebt — deshalb bewusst gross.
    d.ellipse(scaled(34, 40, 61, 72), fill=0)
    d.ellipse(scaled(67, 40, 94, 72), fill=0)
    # Nasenoeffnung
    d.polygon([(64 * SS, 60 * SS), (55 * SS, 80 * SS), (73 * SS, 80 * SS)], fill=0)
    # Zahnspalten: drei breite statt vier schmalen, sonst laufen sie klein zu
    for x in (55, 68):
        d.rectangle(scaled(x, 88, x + 6, 108), fill=0)
    # Trennfuge zwischen Schaedel und Kiefer
    d.rectangle(scaled(42, 82, 86, 87), fill=0)

    return finish(mask, path)


def chest(path):
    mask = new_mask()
    d = ImageDraw.Draw(mask)

    # Korpus
    d.rounded_rectangle(scaled(20, 62, 108, 106), radius=6 * SS, fill=255)
    # Deckel als Halbrund
    d.pieslice(scaled(20, 34, 108, 90), start=180, end=360, fill=255)

    # Fuge zwischen Deckel und Korpus
    d.rectangle(scaled(20, 60, 108, 66), fill=0)
    # Schlossblech ueberbrueckt die Fuge und macht die Kiste als Kiste lesbar
    d.rounded_rectangle(scaled(56, 54, 72, 78), radius=3 * SS, fill=255)
    # Schluesselloch
    d.ellipse(scaled(61, 62, 67, 68), fill=0)

    return finish(mask, path)


def dot(path):
    """Gegner und Ressourcenknoten. Ohne eigenes Sprite zeichnet Unity ein
    Quadrat — deshalb der runde Punkt als eigenes Bild."""
    mask = new_mask()
    d = ImageDraw.Draw(mask)
    d.ellipse(scaled(14, 14, 114, 114), fill=255)
    return finish(mask, path)


def line(path):
    """Kartenrand. Eine volle weisse Flaeche, die als duenner Balken gestreckt
    wird — ein gestreckter Punkt saehe wie eine Linse aus, keine Linie."""
    mask = new_mask()
    ImageDraw.Draw(mask).rectangle(scaled(0, 0, 128, 128), fill=255)
    return finish(mask, path)


if __name__ == "__main__":
    target = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(target, exist_ok=True)
    print(boss_skull(os.path.join(target, "UI_MinimapBoss.png")))
    print(chest(os.path.join(target, "UI_MinimapChest.png")))
    print(dot(os.path.join(target, "UI_MinimapDot.png")))
    print(line(os.path.join(target, "UI_MinimapLine.png")))
