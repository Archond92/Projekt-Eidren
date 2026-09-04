# Wanderer-Viewer-Abnahme vom 26.08.2026

> **Historischer V4-Befund.** Die hier aufgeführten offenen Punkte wurden in
> Viewer V5 behoben. Maßgeblich ist die
> `WANDERER_VIEWER_ABNAHME_V5_20260826.md` im selben Ordner.

**Geprüft:** `WANDERER_3D_VIEWER_V4.html` über den lokalen Viewer auf
`127.0.0.1:8749`.

## Kurzurteil

Der Viewer lädt stabil und bildet den größten Teil des Wanderer-Umfangs ab.
Alle 20 angebotenen Waffen- und Werkzeugvarianten sowie alle 25 Animationen
lassen sich auswählen und rendern. Die früher dokumentierte frei schwebende
Ausrüstung und die Skalierungsverformung der Arme bei
`Abbau_Spitzhacke` sind in dieser V4 nicht mehr reproduzierbar.

Die Abnahme ist trotzdem nicht bestanden. Der eingebettete Stand besitzt
keinen nackten Grundkörper, alle sechs Laufanimationen haben einen sichtbaren
Sprung an der Schleifengrenze, `Oeffnen` ist praktisch eine statische falsche
Pose und die zweite Hand greift bei den Abbauanimationen nicht ans Werkzeug.
Außerdem ist der Viewer als Prüfwerkzeug unvollständig, weil er nur LOD0 zeigt
und Rückseitenfehler durch deaktiviertes Backface-Culling verdecken kann.

## Prüfumfang und bestandene Punkte

| Bereich | Ergebnis |
| --- | --- |
| Rüstungszustände | Stoff, Kupfer und Eisen vollständig sichtbar; `Ohne` fehlerhaft |
| Ausrüstung | 20/20 Varianten auswählbar und gerendert |
| Animationen | 25/25 Clips geladen; je vier Zeitpunkte geprüft |
| Animationsdaten | Schlüsselwerte endlich, Zeitachsen aufsteigend |
| Ruhe- und Gehschleifen | Anfang und Ende schließen sauber |
| Handkopplung | Ausrüstung folgt der führenden Hand |
| Skalierung | Keine animierte Knochen-Skalierung; alter Spitzhackenfehler behoben |
| Laufzeitfehler | Keine Warnung und kein Fehler im Viewer-Protokoll |

Der eingebettete GLB-Stand enthält 162 Knoten, 102 Meshes, 34 Materialien,
ein Skelett und 25 Animationen. Jede Animation steuert dieselben 20 Knochen
über 60 Kanäle an.

## Offene Befunde

### 1. Grundkörper fehlt im Viewer

Im Zustand `Ohne` bleiben nur Kopf, Bart und Augen sichtbar. Der Viewer
enthält beim Modul `Basis` genau diese sechs Kopf-Renderer und noch nicht den
mit F33-002 erzeugten Grundkörper. Damit prüft V4 einen älteren Kandidatenstand.

**Zuordnung:** F33-002 und F33-006.

### 2. Alle Laufanimationen springen beim Schleifenwechsel

Bei allen sechs `Laufen`-Clips unterscheiden sich erster und letzter Frame
identisch: maximal 0,04224 m Translation und 15,122 Grad Rotation. Der
Posensprung ist beim Wechsel vom Ende zurück zum Anfang sichtbar. Ruhe und
Gehen zeigen diesen Fehler nicht.

**Zuordnung:** F33-003.

### 3. `Oeffnen` zeigt keine brauchbare Öffnungsbewegung

Der Clip beginnt bereits in einer stark zurückgelehnten, knienden Pose. Über
die gesamte Dauer von 1,25 Sekunden verändert sich die Haltung nur minimal;
es gibt weder einen Übergang aus dem Stand noch eine erkennbare Greif- oder
Öffnungsaktion.

**Zuordnung:** F33-004.

### 4. Abbauwerkzeuge werden nur mit einer Hand geführt

Bei Axt, Spitzhacke und Sense bleibt die freie Hand neben dem Werkzeug oder
schwebt am Körper. Sie greift den Stiel während des Schlages nicht. Das
Werkzeug selbst ist inzwischen korrekt an die führende Hand gekoppelt.

**Zuordnung:** F33-005.

### 5. Viewer deckt die wichtigen Modellprüfungen nicht vollständig ab

V4 enthält nur LOD0. LOD1 und LOD2 können nicht ein- oder gegeneinander
geprüft werden. Das Rendering ist außerdem beidseitig; dadurch bleiben
fehlende oder falsch ausgerichtete Rückseiten im Viewer möglicherweise
unsichtbar. Eine Versions- oder Prüfsummenanzeige für den eingebetteten
Kandidaten fehlt ebenfalls.

**Zuordnung:** F33-006.

## Nächste Abnahmebedingungen

- V4 aus dem aktuellen Kandidaten einschließlich F33-002 neu erzeugen.
- Die sechs Laufclips mit geschlossener Start-/Endpose exportieren.
- `Oeffnen` als vollständige Stand-Greifen-Öffnen-Rückkehr-Bewegung ersetzen.
- Bei den drei Abbauclips den Kontakt der zweiten Hand am Stiel einbacken.
- LOD-Auswahl und Backface-Culling-Schalter in den Viewer aufnehmen.
- Danach dieselbe 25-Clips-mal-vier-Zeitpunkte-Prüfung wiederholen.
