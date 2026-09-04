# Offene Punkte nach v0.3.2

**Stand:** 25.08.2026
**Status:** aktiv — das Mid-Poly-Upgrade ist technisch abgenommen;
**OP-02, OP-03 und OP-07 sind erledigt.**
**Nummernkreis:** OP-xx (nur für diese Liste; Fixnummern F32-xxx und
Featurenummern N04-xxx bleiben, wo sie sind)

Diese Liste sammelt alles, was nach dem Abschluss von v0.3.2 offen geblieben
ist: Richtungsentscheidungen, zurückgestellte Fixes und notierte Ideen. Die
erledigten Punkte bleiben als Entscheidungsprotokoll erhalten.

**Nicht Teil dieser Liste:** die abgeschlossene Mid-Poly-Umstellung. Auftrag
und Abnahmebelege liegen in `AUFTRAG_MIDPOLY_GESAMTUMSTELLUNG.md`
(MIDPOLY-000) und `MidPoly_Phase7/MIDPOLY_FINAL_ASSET_REPORT.json`.

---

## Am 20.08.2026 entschieden — keine Änderung nötig

Vier Punkte standen zur Prüfung am Spielgefühl und sind vom Nutzer bestätigt
worden. Sie sind damit **erledigt** und werden hier nur festgehalten, damit
sie nicht erneut aufgemacht werden:

| Punkt | Stand | Antwort |
|---|---|---|
| Gebäudereichweite | 2,0 m ab Zellmitte (Acker 2,5 m) | war als Zwischenstand bestätigt; seit OP-02 durch 1,5 m ab Körperoberfläche ersetzt |
| Ignivars Passiv „Treffer entzünden" | 20 % des Trefferschadens über 4 s | passt so |
| Tooltip an den Fähigkeitsknöpfen | über der Knopfleiste, feste Breite, wächst nach unten | passt so |
| Minimap | Größe, Sichtradius, Farben | passt so |

---

## OP-01 — Automatisches Spielen

**Herkunft:** N04-002, gesammelt am 17.08.2026, nie beauftragt.
**Umfang:** groß. Der größte offene Posten dieser Liste.

Eine **Spielerfunktion**, kein Werkzeug: Die Figur spielt selbsttätig weiter —
sammelt, kämpft, bewegt sich zwischen Zielen. Bereits entschieden ist:
Abbruch bei jeder Spielereingabe, Abbruch bei leerer Zone, **kein**
Ausweichen, **kein** Selbstschutz bei niedrigem Leben.

**Was schon steht:** Die vollständige virtuelle Eingabeschicht in
`PlayerInputReader` (`SetVirtualMove`, `SetVirtualInteract`, `PressAttack`
…) — über sie läuft bereits der Touch-HUD. Eine Automatik sitzt **darüber**
und muss kein Gameplaysystem anfassen. Das ist der Grund, warum dieser Punkt
trotz seiner Größe nicht riskant ist.

**Was zu klären ist:** Bibel §26 verlangt den vollen Wegtest je Zone, sobald
eine Außenzone Pflichtziele bekommt — und eine Automatik macht jeden Knoten
zum Pflichtziel. Der Wegtest ist also Teil des Auftrags, nicht Beiwerk.

---

## OP-02 — Reichweiten messen bis zum Körper, nicht bis zum Mittelpunkt

**Herkunft:** F32-001, Stoßrichtung 1 — bewusst zurückgestellt.
**Status:** **erledigt am 25.08.2026.**

Interaktion und Nahkampf messen jetzt die flache Entfernung bis zur
**physischen Oberfläche** des Ziels. Vorrang hat der nächste nicht auslösende
Collider; fehlt er, folgen Renderergrenze, Triggergrenze und erst zuletzt der
Transform-Ursprung. Breite Gebäude und große Gegner werden dadurch an ihrer
sichtbaren Wand beziehungsweise ihrem Körper erreicht, ohne dass man zum
Mittelpunkt laufen muss.

Umgestellt sind Zielauswahl und Haltesitzung, offene Lager- und
Werkbankfenster, alle produktiven Interaktionsziele sowie Kampfzielsuche,
Nahkampfsektor, Zielhilfe und das Distanzfenster des Speers. Die Abnahme liegt
in `OP02_KOERPERREICHWEITEN_ABNAHME.md`.

Die Hilfsabfragen verwenden wiederverwendete Puffer und erzeugen im regulären
Abfragepfad keinen neuen Listenmüll pro Bild.

---

## OP-03 — Reichweiten sind untereinander uneinheitlich

**Herkunft:** Nebenbefund aus F32-001.
**Status:** **mit OP-02 erledigt am 25.08.2026.**

| Ziel | Alter Stand | Neuer Stand |
|---|---|
| Gebäude | 2,0 m ab Ursprung | 1,5 m ab Oberfläche |
| Acker | 2,5 m ab Ursprung | 1,5 m ab Oberfläche |
| Weltkisten | 2,6 m ab Ursprung | 1,5 m ab Oberfläche |
| Ressourcenknoten | 2,7 m ab Ursprung | 1,5 m ab Oberfläche |
| Portale | 2,8 m ab Ursprung | 1,5 m ab Oberfläche |

Produktive Prefabs, Datenassets, Laufzeitvorgaben und die betroffenen Builder
verwenden denselben Standardwert. Ein enges Kontaktband von 0,55 m sorgt nur
für die Zielwahl dafür, dass ein tatsächlich berührtes Gebäude nicht von
einem Nachbarn verdrängt wird; es erweitert die erlaubte Interaktionsdistanz
nicht.

---

## OP-04 — Schmelzbrand: die Zweitwirkung ist eine Annahme

**Herkunft:** F32-004. **Umfang:** klein (ein Zahlenwert und ein Text).

Der Schmelzbrand senkt bei gepanzerten Zielen den Schutz. Was er gegen
**ungepanzerte** Ziele tut, war eine offene Frage mit drei Vorschlägen
(a)/(b)/(c) — sie ist nie beantwortet worden. Umgesetzt und ausgeliefert ist
**(a): +15 % erlittener Schaden für 6 Sekunden**.

Das ist die einzige Stelle in v0.3.2, an der eine unbeantwortete Rückfrage
durch eine Entscheidung von mir ersetzt wurde. Sie steht deshalb hier, bis
sie bestätigt oder geändert ist.

---

## OP-05 — Tooltips für Waffen, Tränke und den Eidra-Wechsel

**Herkunft:** N04-003, Rest. **Umfang:** klein.

Das Tooltip-Feld im HUD ist allgemein gebaut, angebunden sind bisher nur die
beiden Fähigkeitsknöpfe. Die Beschreibungstexte liegen bereits in
`ItemDefinition`. Es fehlt im Wesentlichen die Verdrahtung an den übrigen
Knöpfen.

---

## OP-06 — Rucksäcke

**Herkunft:** Nutzeraussage vom 16.08.2026, nie in einen Eintrag überführt.
**Umfang:** unklar, weil nie ausgearbeitet.

Rucksäcke sollen das Inventar über die 16 Plätze hinaus erweitern. Die
Platzgrenze aus M5 ist deshalb bei Entwürfen kein Ausschlusskriterium mehr,
sondern eine Reihenfolgefrage.

---

## OP-07 bis OP-09 — Aus der v0.3.1-Runde

Drei Punkte, die damals angesprochen, aber nie beauftragt wurden. Sie stehen
unter „Bewusst nicht aufgenommen" in `FIXSAMMLUNG_V0.3.1.md`:

- **OP-07 — Waffenreichweiten ändern: erledigt mit OP-02 am 25.08.2026.** Die
  numerischen Waffenreichweiten bleiben unverändert; Treffer, Zielhilfe und
  Speer-Distanzfenster messen jetzt bis zur Körperoberfläche. Große Gegner
  verlangen daher kein Hineinlaufen mehr.
- **OP-08 — Gnadenfenster für den Kombo-Abbruch beim Ausweichen.** Wer mitten
  in der Kombo ausweicht, verliert sie ganz; ein kurzes Fenster würde das
  Fortsetzen erlauben.
- **OP-09 — Gegner-EP-Rechnung.** Ergänzung zu F31-006; die Zahlengrundlage
  liegt dort bereits.

---

## OP-10 — Weltsaat als Spielfunktion (Vorschlag, nicht beauftragt)

**Herkunft:** Nebenprodukt der Testarbeit am 20.08.2026. **Umfang:** klein.

Seit dem 20.08. gibt es `ZoneStateService.DefaultMasterSeed`. Produktiv steht
er auf `null`, die Welt ist also weiterhin je Durchgang zufällig — die
Testsuite nagelt ihn nur für ihre Läufe fest.

Damit ist eine **wählbare Weltsaat** technisch fast geschenkt: Wer dieselbe
Zahl eingibt, bekommt dieselbe Welt und kann sie mit anderen teilen. Für ein
Spiel mit gewürfelten Außengebieten ist das eine naheliegende Funktion.

**Das ist mein Vorschlag, keine Bestellung.** Was fehlt, ist die Oberfläche
(Eingabefeld im Neues-Spiel-Dialog) und die Ablage im Spielstand.

---

## OP-11 bis OP-16 — Aus der v0.3.4-Runde (03.09.2026)

Notiert bei der Umsetzung von F34-001 bis F34-008 (`FIXSAMMLUNG_V0.3.4.md`).
Keiner dieser Punkte ist ein Fehler im Sinne der Fixliste; es sind
Autoren- und Richtungsfragen.

| Punkt | Befund | Vorschlag |
|---|---|---|
| **OP-11 Gebäude zeigen im Spiel nur LOD2 — erledigt am 03.09.2026 (v0.3.4)** | Die elf baubaren Gebäude behielten aus F33-001 die Schwellen 0,58/0,28; bei der Spielkamera (Bildschirmhöhe 0,05–0,12) ist damit immer die gröbste Stufe aktiv. Gleiches bei Truhen und Loot (0,58/0,28 bzw. 0,62/0,30). | Wie bei Vegetation in F34-001 `MidpolyLodThresholds.ApplyOrthographicThresholds` anwenden, wenn die feinen Stufen sichtbar werden sollen. Sichtprüfung mit der echten Kamera, nicht mit `ForceLOD(0)`. |
| **OP-12 Angriffs- und Abbauclips länger als die Kampffenster** | `Angriff_Speer` 1,167 s gegen 0,48/0,52/0,68 s; Hammer und Dolche ähnlich. Der Deckel aus F34-007 (1,5-fach) zeigt nur den Anfang des Clips. Alle drei Kombostufen spielen denselben Clip. | Clips in Blender auf die Fenster der Waffendaten (`Data/Weapons/*.asset`) abstimmen, je Kombostufe ein eigener Clip. |
| **OP-13 `Abbau_Axt` schwingt nicht mit den Armen** | Arme bewegen sich 28 Grad, die Axt dreht sich im Griff 54 Grad; Körperpose entspricht dem Sensenclip. Alle Werkzeuge hängen an `staff` unter der linken Hand; `dagR` unter der rechten Hand ist ungenutzt. | Axtschlag mit Oberkörper und beiden Armen neu animieren; Werkzeuge ggf. rechtshändig binden. |
| **OP-14 Laufschleifen enden bei Frame 17,6** | `Laufen_*`-Quellbereiche enden bei 17,6 statt 18; der Export sampelt ganze Frames, `footR` weicht am Schleifenende 5,4 Grad ab. | Bereich in Blender auf 0–18 setzen oder letzten Frame = ersten Frame. |
| **OP-15 Farbraum Gamma mit glTFast** | Projekt rendert in Gamma; glTFast wandelt Farben für Linear. Farbton bleibt, Beleuchtung wirkt flacher als in der Blender-Abnahme. Vertexfarben werden vom glTFast-Shader nicht ausgewertet. | Bewusst entscheiden: Linear-Farbraum (alle Materialien/Lichter nachziehen) oder eigener Vertexfarben-Shader für Mid-Poly. Siehe [[eidren-licht-invarianten]]-Notiz: Tonemapping in Gamma+LDR wirkungslos. |
| **OP-17 Ordnungsabhängiger EditMode-Test** | `WorldMapArtDirectionTests.ThemeChange_ChangesViewWithoutChangingNodeData` fällt nur im vollen Lauf (1357 Tests) mit `ArgumentNullException(definition)`: `map.Nodes[1]` ist dann ein entladenes Unity-Objekt. Isoliert 11/11, EditMode-Namensraum + WorldMap 1239/1239, alle Nicht-EditMode-Klassen 129/129 — nur die Vereinigung kippt. Seit 03.09.2026 lädt der Test den Knoten notfalls frisch. | Wer den Auslöser finden will: EditMode-Namensraum halbieren und jeweils mit den 18 Nicht-EditMode-Klassen kombinieren. Kandidaten: Tests, die Szenen öffnen und damit ungenutzte Assets entladen. |
| **OP-16 Phase-0-Captures „Active" sind Ruhebilder** | In `Etappen/MidPoly/Phase0/Captures` sind bei acht Ressourcen die `_Active`-Bilder byteidentisch mit den Ruhebildern. | Beim nächsten Capture-Lauf den Aktivzustand wirklich auslösen. |

## Reihenfolge, wenn es weitergeht

Ein Vorschlag, keine Festlegung:

1. **OP-04 und OP-05** sind klein und unabhängig; sie passen in jede Lücke.
2. **OP-01** als eigene Etappe mit dem Wegtest aus §26.
3. **OP-06, OP-08, OP-09 und OP-10** nach Bedarf.

**Wichtig für alle:** Das Grafikupgrade ist freigegeben. Wer einen
Geometrie-Builder anfasst oder Assets neu baut, muss sicherstellen, dass der
Builder die abgenommenen Mid-Poly-Assets erhält (MIDPOLY-000, Abschnitt 5.7
und Abnahmepunkt 6). Vor jedem Builder-Lauf prüfen, was im Ausgabepfad liegt.
