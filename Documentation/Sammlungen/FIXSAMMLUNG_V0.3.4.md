# Fixsammlung v0.3.4

**Status: umgesetzt, abgesichert und am 03.09.2026 als v0.3.4 veröffentlicht** (https://github.com/Archond92/Projekt-Eidren/releases/tag/v0.3.4; EditMode 1357/1357, PlayMode 154 + 3 Skips; Download 130,99 MiB, SHA-256 65ee1481…). Die acht
Meldungen aus `FIXLISTE.md` (Beobachtungen aus der Exe v0.3.3 vom 30.08.2026)
sind als F34-001 bis F34-008 aufgenommen. Alle Befunde wurden vor der
Umsetzung im Arbeitsstand belegt; Code und Assets waren seit dem Build
unverändert.

Ein gemeinsamer Nenner zieht sich durch F34-001, F34-002 und F34-003: Die
Mid-Poly-LOD-Schwellen (0,55/0,24/0,06 bzw. 0,58/0,28/0,07) sind Werte für
eine perspektivische Kamera. Die Spielkamera ist orthografisch mit Halbhöhe
7,4 (Zoom bis 5,5), und die Standard-Qualitätsstufe „Medium" trägt einen
LOD-Bias von 0,7. Die Bildschirmhöhe eines Objekts ist damit
`Größe / 14,8 × 0,7`; ein 1-m-Objekt erreicht 0,047. Alles unter der letzten
Schwelle wird komplett ausgeblendet, alles unter der ersten zeigt nur die
gröbste Stufe. F33-001 hatte das für die elf baubaren Gebäude behoben, aber
nicht für Truhen, Loot, Vegetation, Umweltkit und Kreaturen.

## F34-001 — Fasern auf dem Boden schwer zu erkennen

**Bereich:** Ressourcen, Mid-Poly-Vegetation, LOD, Zonenvarianten

**Beobachtung (03.09.2026):** Faserpflanzen sind im Sumpf kaum zu finden.

**Befund:** Die Faserpflanze ist mit 0,82 × 0,91 × 0,72 m das kleinste
Ressourcenobjekt (Steinvorkommen 2,56 × 1,99 m). Ihre LOD-Gruppe (`m_Size`
0,906) erreicht bei der Spielkamera nur 0,043 Bildschirmhöhe und lag damit
unter der Ausblendgrenze 0,06 — die Pflanze wurde gar nicht gerendert, auch
nicht beim engsten Zoom (0,058). Sichtbar blieben nur die drei
Erkennungsschilfhalme der Zonenvariante, und deren Material trug die
abgedunkelte Erschöpft-Farbe: Der Variantenbauer schrieb Aktiv- und
Erschöpft-Zustand in dasselbe Materialasset, der zweite Lauf überschrieb den
ersten (betraf alle neun Identity-Materialien). Dazu hat das gefallene
Faserbündel (`WorldItem`, Größe 0,585) dieselbe 0,07-Grenze wie die Truhen
und war ebenfalls dauerhaft ausgeblendet.

**Fix:** Neuer Editor-Helfer `MidpolyLodThresholds` leitet die Schwellen aus
der Bildschirmhöhe bei der Spielkamera ab: LOD0 ab 60 % der eigenen Höhe,
LOD1 ab 35 %, Ausblenden bei 0,01 wie in F33-001. Die vier Migrationen für
Ressourcen, Tier-2-Vegetation, Tier-2-Erz und Umweltkit rufen ihn nach dem
Aufbau auf; die 37 vorhandenen Prefabs (StyleProof, Zonenvarianten,
Ressourcen-Visuals) wurden direkt auf diese Werte gesetzt. Der
Variantenbauer legt für den Erschöpft-Zustand ein eigenes Material
`…_identity_exhausted` an; die neun Aktiv-Materialien tragen wieder ihre
Zonenfarbe, neun Erschöpft-Kopien wurden angelegt und in den
Erschöpft-Varianten verdrahtet. `WorldItem` und `DeathBag` fallen unter
F34-003.

**Nachweis:** `MidpolyOrthographicLodTests` prüft alle LOD-Gruppen unter
Vegetation, Ressourcen und Umweltkit darauf, dass LOD0 bei der Spielkamera
aktiv ist, und alle Mid-Poly-Gruppen darauf, dass sie nicht ausgeblendet
werden.

## F34-002 — Büschen fehlt das Grün

**Bereich:** Umweltkit, Mid-Poly-Vegetation, LOD

**Beobachtung (03.09.2026):** Büsche wirken, als fehle die grüne Darstellung.

**Befund:** Die Farben sind korrekt (LeafDark 53/118/75, LeafLight 93/153/97,
keine Grundfarbe, kein Cutout, doppelseitig). Der Busch (`m_Size` 1,5)
erreicht bei der Spielkamera aber nur 0,071 Bildschirmhöhe und zeigte deshalb
dauerhaft LOD2 mit 804 von 3840 Dreiecken. Die feinen hellgrünen Blattspitzen
sind genau das, was die Dezimierung entfernt; übrig bleibt der dunkle Kern.
Dasselbe galt für Beerenbusch, Sumpfhanf, Farn, Bodendecker und Blumen
(Blumen 0,023: ganz ausgeblendet). Die Vertexfarben, die die alten
Projekt-Shader nutzten, spielen im glTFast-Materialpfad keine Rolle mehr; das
ist kein Fehler, aber ein Unterschied zur Blender-Abnahme.

**Fix:** Siehe F34-001 — alle 16 StyleProof-Prefabs zeigen bei der
Spielkamera LOD0.

**Nachweis:** `MidpolyOrthographicLodTests`.

## F34-003 — Truhen in der Spielwelt unsichtbar

**Bereich:** Weltkisten, Forge-Truhen, Bodenloot, Kreaturen, LOD

**Beobachtung (03.09.2026):** Truhen sind in den Zonen nicht zu sehen.

**Befund:** Derselbe Fehler wie F33-001, nur bei anderen Familien: Die
Weltkisten-Migration schreibt als letzte LOD-Schwelle 0,07, die
Lagerkisten-Migration seit F33-001 0,01. Die drei Weltkisten liegen bei der
Spielkamera bei 0,054 / 0,069 / 0,047 (Hidden sogar ohne Bias bei 0,068)
und wurden gecullt. Die Kisten werden korrekt erzeugt (10 Spawnpunkte je
Zone); Layer, Culling-Maske, Materialien und Rückseiten sind unauffällig.
Erkennbar war im Spiel nur der Kontaktschatten, der außerhalb der LOD-Gruppe
liegt. Mitbetroffen mit identischer Grenze: acht Forge-Truhen (0,95 bis
1,62 m), `WorldItem` (0,585), `DeathBag` (0,895) und die 15 Kreaturen-Prefabs
unter `Prefabs/Actors/3D` (Terrock 1,378 → 0,065 unter 0,07). Die
Weltkisten-Prefabs waren seit dem 25.08. unverändert, also vor F33-001. Der
Sichtnachweis `WorldChestSichtCapture` konnte den Fehler nicht zeigen: Er
erzwingt LOD0 und rendert perspektivisch.

**Fix:** Letzte Schwelle 0,01 in den Migrationen für Weltkisten,
Forge-Container und Bodenloot sowie in den elf Kreaturen-Migrationen; die 13
Truhen-/Loot-Prefabs und 15 Kreaturen-Prefabs wurden direkt gesetzt.

**Nachweis:** `MidpolyWorldChestTests`, `MidpolyForgeContainerTests` und
`MidpolyWorldLootTests` verlangen jetzt wie die F33-001-Tests, dass die
letzte Schwelle unter der Bildschirmhöhe bei Kamera 7,4 mit Bias 0,7 liegt.
`MidpolyOrthographicLodTests` deckt Kreaturen und Gebäude mit ab.

## F34-004 — Beim Verschieben zuerst das Gebäude auf dem Bodenfeld wählen

**Bereich:** Basisbau, Verschieben, Abreißen

**Beobachtung (03.09.2026):** Beim Verschieben wird das Bodenfeld getroffen
statt des Gebäudes darauf.

**Befund:** Es gibt keine Klick- oder Raycast-Auswahl. Verschieben und
Abreißen suchen die dem Spieler nächste Gebäudeinstanz im Umkreis von 4,5 m,
und Bodenfelder sind gewöhnliche Instanzen. Da der Spieler beim Bauen auf
dem Boden steht (Abstand nahe 0), gewann der Boden fast immer gegen das eine
Zelle entfernte Gebäude. Bei Gleichstand entschied die Einfügereihenfolge.
Weder Priorität noch Belegungsprüfung existierten an dieser Stelle, obwohl
das Belegungsraster mit getrennten Boden- und Objektschichten bereitliegt
und beim Platzieren benutzt wird.

**Fix:** Die Auswahl im Baucontroller bekommt eine Rangordnung: Gebäude
(Objekt, Dekoration, Kante) vor Bodenfeld, innerhalb eines Rangs die
Distanz, bei Gleichstand die Instanz-ID. Gilt für Verschieben und Abreißen.

**Nachweis:** PlayMode-Test
`MoveSelectionPrefersTheBuildingOverTheFloorBeneathIt`: Spieler auf der
Bodenzelle, Werkbank darauf — Verschieben und Abreißen wählen die Werkbank.

## F34-005 — Bodenfeld nur wählbar, wenn kein Gebäude darauf steht

**Bereich:** Basisbau, Verschieben, Abreißen

**Beobachtung (03.09.2026):** Ein belegtes Bodenfeld soll nicht wählbar sein.

**Befund:** Die Machbarkeitsprüfung kannte nur „Gebäude beschäftigt" und
„Lager nicht leer". Ein Boden unter einer Schmelze (die Boden voraussetzt)
ließ sich wegziehen und die Schmelze stand ohne Boden da.

**Fix:** Ein Bodenfeld ist nur Kandidat, wenn auf keiner seiner Zellen ein
Objekt oder eine Dekoration steht (Prüfung über das Belegungsraster). Steht
nur ein belegter Boden in Reichweite, lautet die Rückmeldung „Zuerst das
Gebäude auf dem Bodenfeld versetzen." statt „Kein Gebäude in Reichweite."

**Nachweis:** Derselbe PlayMode-Test: Nach dem Abriss der Werkbank bleibt der
Boden erhalten, Abreißen auf der belegten Zelle trifft nie den Boden.

## F34-006 — Der Speer ruckelt beim Laufen stark

**Bereich:** Wanderer, Mid-Poly-Export, Animationsclips

**Beobachtung (03.09.2026):** Der Speer zuckt beim Laufen.

**Befund:** Es gibt keinen Handanker zur Laufzeit; alle Handobjekte sind zu
100 % an den Knochen `staff` gebunden und werden mitanimiert. Der Fehler
steckte in der exportierten `Wanderer.glb`: In `Laufen_Speer` sprang `staff`
in den ersten vier Frames um 41 / 106 / 131 / 78 Grad pro Frame, `handL`
um 35 / 71 / 60 Grad, und die Schleife schloss nicht (`footR` 27,7 Grad
zwischen erstem und letztem Frame). Bei 0,708 s Cliplänge wiederholte sich
das 1,4-mal pro Sekunde. Dasselbe Muster in allen 28 Clips (`Angriff_Speer`
bis 166 Grad, `Laufen_Ohne` bis 171 Grad). Die Blender-Quelle ist sauber
(Deltas unter 1 Grad, Schleifenschluss exakt). Ursache war der Bake im
Exportskript: Es setzte pro Frame `PoseBone.matrix`, und dieser Setter
rechnet gegen die zuletzt evaluierte Elternpose — also den Stand des
vorigen Frames bzw. der Ruhepose. Jede Knochenebene hinkte damit einen
Frame hinterher, am stärksten am Clipanfang.

**Fix:** `Tools/export_wanderer_modelle_runtime.py` berechnet die lokale
Pose analytisch aus den Zielmatrizen (`basis = (Elternpose × Ruheversatz)⁻¹
× Ziel`) und schreibt `matrix_basis`. Die GLB wurde neu exportiert (GUID
unverändert, 28 Clips, 20 Knochen) und ersetzt `Wanderer.glb` sowie die
Sicherungskopie `Source~/Wanderer_MidPoly_Runtime.glb`; die alte Fassung
liegt im Papierkorb.

**Nachweis:** Messung an der neuen GLB: `Laufen_Speer` `staff` 0,2 / 0,4 /
0,6 / 0,8 Grad pro Frame, Schleifenschluss 0,06 Grad; `Angriff_Speer` 0,3 /
0,8 / 0,9 Grad. Verbleibender Rest: `footR` 5,4 Grad am Schleifenende der
Laufclips, weil deren Quellbereich bei Frame 17,6 statt 18 endet
(Autoren-Punkt, siehe OFFENE_PUNKTE). `Tools/verify_wanderer_runtime_glb.py`
und `Wanderer3DAssetTests` bestätigen Struktur und Clipanzahl.

## F34-007 — Angriffsanimation mit dem Speer eigenartig und viel zu schnell

**Bereich:** Wanderer, Kampf, Clipwiedergabe

**Beobachtung (03.09.2026):** Der Speerangriff läuft als Zuckung ab.

**Befund:** Zwei Ursachen. Erstens der Exportfehler aus F34-006: Im
ersten Frame von `Angriff_Speer` summierten sich 1208 Grad Knochenrotation.
Zweitens die Zeitstauchung: `PlayTimed` setzt die Geschwindigkeit auf
Cliplänge geteilt durch Kampffenster. `Angriff_Speer` dauert 1,167 s, die
drei Speer-Kombostufen 0,48 / 0,52 / 0,68 s — also 2,43 / 2,24 / 1,72-fache
Geschwindigkeit (Hammer bis 1,88, Dolche bis 3,49). Alle drei Kombostufen
spielen denselben Clip.

**Fix:** F34-006 beseitigt die Zuckung. Zusätzlich deckelt `PlayTimed` die
Geschwindigkeit bei 1,5 (`MaxTimedPlaybackSpeed`). Im 0,48-s-Fenster läuft
der Clip damit bis 0,72 s und zeigt Anlauf und Stoß (0,46 bis 0,71 s); den
Rest überblendet der Folgezustand. Die Clips selbst sollten für die
Kampffenster neu abgestimmt werden (OFFENE_PUNKTE).

**Nachweis:** EditMode-Test
`PlayTimed_DeckeltDieAbspielgeschwindigkeitAufAnderthalb`.

## F34-008 — Beim Abbauen läuft nicht die richtige Animation

**Bereich:** Wanderer, Abbau, Werkzeugzuordnung

**Beobachtung (03.09.2026):** Beim Holzfällen läuft die falsche Animation.

**Befund:** Die Zuordnung Werkzeug → Haltung → Clip ist korrekt und
getestet (Axt → `Abbau_Axt`, Spitzhacke → `Abbau_Spitzhacke`, Sense →
`Abbau_Sense`). Holz der Stufe 0 darf aber ohne Werkzeug abgebaut werden.
In diesem Fall lieferte die Werkzeugauflösung `null`, und `HideTool()`
schaltete `IsHarvesting` ab — der Animator spielte während des Abbaus die
normale Ruhe- oder Laufschleife. Der dokumentierte prozedurale Rückfall
„ohne Werkzeug" war unerreichbar. Mit Axt kam der Exportfehler aus F34-006
hinzu; außerdem trägt `Abbau_Axt` kaum Armbewegung (Arme 28 Grad, Axt im
Griff 54 Grad) und gleicht in der Körperpose dem Sensenclip — ein
Autoren-Punkt.

**Fix:** Abbau von Hand hält den Abbauzustand: `PlayerHarvestVisual`
meldet ein leeres Werkzeug statt abzuschalten, der Animator setzt leere
Hände und die Presentation spielt den prozeduralen Abbau. Mit Werkzeug
greift F34-006.

**Nachweis:** Bestehende Abbau-Integrationstests (Werkzeugauflösung,
`IsHarvesting`, unterdrücktes Sprite-Werkzeug) laufen unverändert; die
Mesh-Presentation-Tests decken Haltung und Handmeshes ab.

## Abschluss

**Nachweis am 03.09.2026, Editor 6000.3.0f1, headless:**

| Suite | Ergebnis | Lauf |
|---|---|---|
| EditMode (`-nographics`) | 1357 / 1357 bestanden | `TestResults-Archiv/f34-editmode-11.xml` |
| PlayMode (mit Grafikgerät) | 149 bestanden, 3 übersprungen, 0 Fehler | `TestResults-Archiv/f34-playmode-3.xml` |

Gegenüber der Baseline 0.3.3 (EditMode 1355, PlayMode 148 + 7 Skips):
zwei 2D-Sheet-Prüfungen entfallen (Terrock-/Noctarion-Alpha), vier neue
EditMode-Tests (`MidpolyOrthographicLodTests` ×3, Geschwindigkeitsdeckel),
ein neuer PlayMode-Test (Verschiebe-Auswahl). Die zwei PlayMode-Tests
`ActorPresentationFollowUpTests` laufen jetzt mit der 3D-Presentation statt
mit der 2D-Sprite-Presentation, deren Terrock-Sheets als Altlast entfernt
wurden. PlayMode braucht ein echtes Grafikgerät (kein `-nographics`), sonst
scheitert der F33-001-Rendertest an `RenderTexture.Create`.

**Sichtbelege mit der echten Spielkamera** (`Fixrunde034SichtCaptureTests`,
PlayMode mit Grafikgerät, Qualität Medium, LOD-Bias 0,7, Kamera 7,4; Bilder
unter `TempReview/F34-Sicht/`): Weltkiste in Greenwood und Faserpflanze im
Sumpf werden gerendert und tragen den Interaktionsring, der Busch in der
Heimatbasis zeigt LOD0. Der Wanderer hält den Speer über sechs Laufbilder
(0,12 s Abstand) ruhig vor dem Körper, im Angriff aufrecht ohne Sprünge.
Abbau am Baum ohne Axt meldet `IsHarvesting = true` bei leerem Werkzeug.

**Veröffentlicht am 03.09.2026 als v0.3.4** (Entwickler-MidPoly-Build mit Vollausbau wie v0.3.3, gleicher Produktname). Dazu OP-11: Gebäude, Stationen, Truhen und Loot zeigen jetzt ebenfalls LOD0 bei der Spielkamera. Der Build ist mit 131 MiB weniger als halb so groß wie v0.3.3, weil die 2D-Sprite-Sheets (unkomprimiert rund 700 MB im Player) nicht mehr enthalten sind.
