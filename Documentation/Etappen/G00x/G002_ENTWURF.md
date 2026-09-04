# G-002 — Bodenmaterial und Geländedarstellung (Entwurf)

**Stand:** 5. August 2026
**Auftrag:** `Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md`, G-002
**Voraussetzung:** G-000 und G-001 abgeschlossen

---

## 1. Befund vor der Arbeit

Der Auftragstext beschreibt den Boden als „flaue braune Masse" und nennt als
Ausgangswert einen mittleren Nachbarpixel-Abstand von **2,58**. Beide Angaben
stammen aus den alten `VisualFixSmokeFinal`-Captures, also von **vor** der
Shader-Reparatur in G-001. Sie sind nicht mehr der Ist-Zustand.

### 1.1 Gemessener Ist-Zustand

Messung auf `G001-Captures/Lauf1/04_boden_leer.png`, derselben Position, die
G-001 für genau diesen Zweck angelegt hat:

| Größe | Spec-Angabe | real gemessen |
| --- | ---: | ---: |
| mittlere Helligkeit | 51,6 | **50,738** |
| Standardabweichung | 5,3 | **8,043** |
| mittleres Nachbardelta | 2,58 | **5,486** |

Die Helligkeit stimmt, die Streuung nicht: Der Boden trägt real gut die
doppelte Struktur, die der Auftrag ihm zuschreibt. Ein Teil der geforderten
Verbesserung ist durch die Shader-Reparatur aus G-001 bereits gebucht.

### 1.2 Die eigentliche Ursache — deckende Blend-Patches

Der Boden jeder Zone besteht aus zwei Lagen:

- eine große Basisfläche mit `{Zone}_Ground_Base.mat`
- ein bis zwei aufgelegte Flicken `GroundBlend_N` mit `{Zone}_Ground_Blend_N.mat`
  auf dem Shader `Eidren/Area Art Blend`

`AreaArtGroundBuilder.BuildFeatheredPatch` erzeugt diese Flicken **mit
Vertexfarben**, deren Alphakanal von innen nach außen ausblendet
(`AreaArtGroundBuilder.cs:56`). Genau dafür sind sie da: weiche Ränder.

**Der Shader liest diese Vertexfarben nicht.** Weder der Dekompilat-Platzhalter
noch die in G-001 wiederhergestellte Fassung deklarieren ein `COLOR`-Semantic.
G-001 hat bewusst nur das Verhalten hergestellt, das die *Materialdaten*
beschreiben — die *Meshdaten* waren dort nicht im Blick.

Zusätzlich sind alle Blend-Texturen **vollständig deckend**; eine Stichprobe
über `greenwood_meadow_v01`, `greenwood_soil_v01` und `home_grass_v01` ergibt
Alpha 255 an jedem geprüften Pixel. Damit gilt im Fragment:

```
color.a = tex.a (1) * _BaseColor.a (1) = 1
```

Bei `SrcAlpha`/`OneMinusSrcAlpha` ersetzt der Flicken den Untergrund also
vollständig. Die Ausblendung wird verworfen, der Flicken deckt.

**Folge, am Beispiel Greenwood belegt:**

| Fläche | Textur | mittleres RGB | Luma |
| --- | --- | --- | ---: |
| `Ground_Base` | `greenwood_meadow_v01` | 65 / 75 / 34 (grün) | 67 |
| `Ground_Blend_1` | `greenwood_soil_v01` | 69 / 45 / 27 (braun) | 50 |

Die Zone ist als **grüne Wiese mit aufgelegten Erdflecken** angelegt. Gemessen
wird im Capture ein Mittelwert von 50,7 — der Braunwert der Erde. Vom grünen
Basisboden ist nichts zu sehen.

Gegenprobe über die Geometrie: Die Zone ist 80 Einheiten groß (Tiling 13,333
mal `GroundTileWorldSize` 6). `GroundBlend_1` misst damit 49,6 × 43,2 bei
lokaler Position (−14,4 / 8) und spannt x von −39,2 bis 10,4 sowie z von −13,6
bis 29,6. Die Kamera von `04_boden_leer` steht bei (−13 / −7) mit Ortho 6, also
mit ihrem gesamten Ausschnitt **innerhalb** des Flickens.

Der „flaue braune Boden" ist demnach kein Gestaltungsmangel der Textur, sondern
ein deckend gezeichneter Erdflicken, der die Wiese verdeckt.

### 1.3 Drei Zonen ohne eigene Bodentexturen

v0.2 hat sieben Kampfzonen. Drei davon verweisen auf die Texturen ihrer
Nachbarzonen:

| Zone | Basistextur | Herkunft |
| --- | --- | --- |
| TwilightGrove | `greenwood_soil_v01` | Greenwood |
| VeilMarsh | `marsh_mud_v01` | Marsh |
| GreyRifts | `quarry_rock_v01` | Quarry |

Sie rendern fehlerfrei, sind aber von ihrer Spenderzone nicht zu unterscheiden.
Das verletzt das Abnahmekriterium „eigene, unterscheidbare Bodenwirkung".

### 1.4 Verworfener Ansatz — Detaillage allein

Vor dem Entwurf wurde geprüft, ob eine reine Shader-Detaillage genügt. Die
Formel `base * lerp(1, detail*2, stärke) * makro` wurde offline auf das echte
Capture gerechnet (`TempReview/G002-Vorschau/`):

| | Mittel | StdAbw | Nachbardelta |
| --- | ---: | ---: | ---: |
| Ist | 50,738 | 8,043 | 5,486 |
| mit Detaillage | 48,884 | 11,587 | **7,046** |

Faktor 1,28 — und das Ergebnis liest sich als Schleifpapier, nicht als
gestalteter Boden. Hochfrequentes Rauschen ist nicht das, was fehlt; der Boden
hat davon bereits genug. Es fehlen **lesbare Strukturen** auf Objektgröße.

Die Detaillage bleibt deshalb im Entwurf, aber als unterstützendes Element mit
geringem Gewicht, nicht als Kern der Lösung.

## 2. Entwurf

Vier Bausteine, nach Wirkung geordnet.

### 2.1 Vertexfarben-Alpha im Blend-Shader — der Kern

`Assets/_Game/Shaders/AreaArtBlend.shader` bekommt das `COLOR`-Semantic zurück:

```
Attributes:  float4 color : COLOR;
Varyings:    float4 color : COLOR;
frag:        half4 c = SAMPLE(_BaseMap, uv) * _BaseColor * input.color;
```

Damit wirkt die Ausblendung, die im Mesh bereits liegt. Ohne eine einzige neue
Textur entstehen dadurch:

- der grüne Wiesenboden wird sichtbar
- Erdflecken bekommen weiche Ränder statt Deckung
- über die Fläche entsteht echte Materialvariation
- Streuung und Nachbardelta steigen deutlich

Das ist die Reparatur eines zweiten Dekompilierschadens derselben Art wie in
G-001 — diesmal auf der Meshseite statt der Materialseite.

### 2.2 Ground-Detail-Shader

Neuer Shader `Eidren/Ground Detail` für die `Ground_Base`-Materialien, die
heute auf `URP/Unlit` liegen. Drei Abtastebenen über eine UV-Menge:

| Ebene | Frequenz | Zweck |
| --- | --- | --- |
| Basis | Tiling wie bisher (Zonengröße / 6) | Farbe und Grundcharakter |
| Detail | 4–5× Basis | Mikrostruktur, Körnung |
| Makro | 1/3 Basis | großflächige Variation gegen Kachelwiederholung |

```
albedo = base * lerp(1, detail * 2, _DetailStrength) * macro
```

Stärken als Material-Properties, damit jede Zone eigene Gewichtung bekommt.
Fällt eine Detail- oder Makrotextur weg, ist die Ebene neutral — der Shader
bleibt damit auf Zonen anwendbar, die noch keine hat.

### 2.3 Eigene Texturen für die drei Platzhalterzonen

Je Zone eine Basis- und ein bis zwei Blendtexturen, im gemalten Stil der
vorhandenen Sätze und in einer Farbstimmung, die die Zone von ihrer bisherigen
Spenderzone trennt:

| Zone | Richtung |
| --- | --- |
| TwilightGrove | kühles Blaugrün, Waldboden mit Laubstreu |
| VeilMarsh | trübes Graugrün, nasser Schlick |
| GreyRifts | kalter Blaugrau-Stein, Geröll |

### 2.4 Bodenbewuchs und Streu über `Decorations`

`AreaArtDecoration` (Prefab plus Dichte) existiert bereits im Datenmodell und
wird von `AreaArtSceneBuilder.BuildDecoration` verbaut — die Listen sind nur
leer. Je Zone werden Bewuchs- und Streuprefabs mit Dichte hinterlegt; Vorlage
ist `SP_GroundCover_Moss.prefab`.

Randbedingungen:

- Kollisionsfrei, damit Bewegung, Kampf und Abbau unbehindert bleiben
- Außerhalb des Bauraster-Bereichs oder ohne Bauverbot
- In `04_boden_leer` abschaltbar wie die `ResourceNode`-Objekte, damit die
  Messfläche eine reine Bodenfläche bleibt

## 3. Abnahme

Die neun Kriterien aus dem Auftrag, mit dem jeweiligen Nachweisweg:

| # | Kriterium | Nachweis |
| --- | --- | --- |
| 1 | Nachbardelta mindestens dreifach | Messung an `04_boden_leer`, siehe 3.1 |
| 2 | Struktur ohne sichtbares Kachelraster | Capture aus Spielkameradistanz |
| 3 | Drei mischbare Materialien mit Übergängen | Blend-Lagen je Zone |
| 4 | Gras und Erde mit erkennbarer Materialkante | Vorher/Nachher an derselben Position |
| 5 | Bewuchs und Streu gegen leere Flächen | Capture je Zone |
| 6 | Gemalter Stil bleibt gewahrt | Sichtprüfung gegen `ReferenceExtracted/` |
| 7 | Jede Zone mit eigener Bodenwirkung | Capture aller sieben Zonen |
| 8 | Bildrate fällt nicht messbar ab | Messung im Windows-x64-Build |
| 9 | Vorher/Nachher aus identischer Position | `G001-capture.ps1 -Baseline` |

### 3.1 Zielwert für Kriterium 1

Der Auftrag verlangt „mindestens das Dreifache des heutigen Wertes von 2,58",
also **7,74**. Dieser Bezugswert ist nach 1.1 überholt; der reale Ausgangswert
ist 5,486.

Nachgewiesen wird deshalb gegen **beide** Bezugsgrößen, und der Bericht nennt
beide offen:

- absolut gegen den Auftragswert: Ziel ≥ 7,74
- relativ gegen den realen Ausgangswert 5,486

Ein Zielwert wird nicht kleingerechnet: Maßstab ist das Dreifache des realen
Ausgangswerts, also **≥ 16,46**. Wird er verfehlt, wird das mit Zahl benannt
statt umdefiniert.

## 4. Was nicht zu G-002 gehört

- **Beleuchtung und Tonwertaufbau** — G-003. Der Boden wird unbeleuchtet
  bewertet; `Ground_Base` liegt heute auf `URP/Unlit`.
- **Bodenschatten** — G-004.
- **Objektdurchdringungen und Weltdichte** — G-009. G-002 bringt Bewuchs ein,
  räumt aber keine falsch platzierten Weltobjekte auf.
- **Perspektive der Weltobjekte** — G-010.

## 5. Risiken

| Risiko | Umgang |
| --- | --- |
| Vertexfarben-Fix ändert alle 15 Blend-Flächen in allen Zonen | Vorher/Nachher je Zone aufnehmen, nicht nur Greenwood |
| Sichtbar werdender Wiesenboden verschiebt die Bildhelligkeit spürbar | Als erwartete Änderung dokumentieren; G-003 stimmt danach ab |
| Bewuchs kostet Bildrate | Dichte je Zone begrenzen, Bildrate vor Abschluss messen |
| Paralleler `codex`-Agent im selben Projektordner | `Temp/UnityLockfile` abwarten statt entfernen |
