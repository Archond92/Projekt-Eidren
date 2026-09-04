# G-004 — Bodenschatten und Objektverankerung: Abnahme

**Stand:** 11. August 2026
**Bezug:** [G004_PLAN.md](G004_PLAN.md), [G004_ENTWURF.md](G004_ENTWURF.md), Auftrag G-004 in [GRAFIKAUFTRAEGE_V0.2.1.md](GRAFIKAUFTRAEGE_V0.2.1.md)
**Baseline für den Namensdiff:** `TestResults-stil-e5-final.xml` (831 Tests, 760 bestanden, 71 bekannte Fehlschläge)

**Diese Abnahme spricht keine Freigabe aus.** Sie legt die Belege vor; die
Entscheidung liegt beim Auftraggeber (Abschnitt 8).

---

## Kurzfassung

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Figur, Gegner, Eidra und alle stehenden Weltobjekte besitzen einen Bodenschatten | erfüllt für die 80 Weltprefabs und die Spielfigur; Sprite-Gegner unverändert |
| 2 | Schatten sitzt an der Standposition, nicht versetzt | erfüllt, Bildbeleg |
| 3 | Folgt Bewegung ohne sichtbare Verzögerung | **nicht bildlich belegt** — konstruktiv erfüllt |
| 4 | Bei Rolle und Sprung verkleinert sich der Schatten | **nicht belegt** — kein Bewegungscapture verfügbar |
| 5 | Auf geneigtem Untergrund liegt der Schatten auf der Oberfläche | **gegenstandslos** — es gibt keinen geneigten Untergrund |
| 6 | Schattenrichtung stimmt mit der Lichtrichtung aus G-003 überein | erfüllt |
| 7 | Auf dem Boden aus G-002 sichtbar, ohne harten Fleck | erfüllt, Bildbeleg |
| 8 | Bildrate sinkt nicht messbar ab | **nicht entscheidbar** — Messstreuung größer als der Effekt |
| — | Keine Regression (Namensdiff) | erfüllt — **0 neue Fehlschläge**, 1 behoben |
| — | Freigabe durch Auftraggeber | **steht aus** |

---

## 1. Was gebaut wurde

**Kontaktdecal im Bauweg.** `WorldContactShadowBuilder.Attach(GameObject)` hängt
das Decal an, bevor ein Prefab geschrieben wird. Neun erzeugende Builder rufen
es in ihrem `Save()`-Engpass auf. Damit ist die Erdung ein Bauteil und keine
nachträgliche Dekoration — der Grund, warum die G-003-Decals im Stilumbau
restlos verlorengingen (Abschnitt 6).

**Weiche Schatten.** `Eidren_URP.asset` `m_SoftShadowsSupported: 0 → 1`,
`ZoneLightingBuilder.SunShadows` `Hard → Soft`. `SunEuler (71.7, 156.2, 0)` und
`SunShadowStrength 0,55` blieben unangetastet — G-003-Invariante.

**Aufnahmeregel.** Ein Decal bekommt, was höher als **0,2 m** steht
(`bounds.size.y`, nur `MeshRenderer`). Herleitung: [G004_HOEHENTABELLE.md](G004_HOEHENTABELLE.md).

## 2. Abdeckung — 77 von 80 Prefabs

| Gruppe | mit Decal |
| --- | --- |
| `Prefabs/Resources/Visuals` | 18 von 18 |
| `Prefabs/Environment/AreaArtVariants` | 24 von 24 |
| `Prefabs/Environment/StyleProof` | 14 von 16 |
| `Prefabs/Buildings/Level01` | 8 von 9 |
| `Prefabs/Loot/WorldChests` | 3 von 3 |
| `Prefabs/Containers/Forge` | 8 von 8 |
| `Prefabs/Stations` | 2 von 2 |

Ohne Decal bleiben genau die drei, die die Regel ausschließen soll:
`SP_GroundCover_Moss` (0,120 m), `SP_GroundCover_Grass` (0,150 m),
`BLD_Floor_L01` (0,160 m).

**Nachweis:** `GroundContactTests`, 160 Fälle (80 Prefabs × 2 Methoden),
`TestResults-g004-t5-gruen.xml` — 160/160. **Idempotenz:** zweiter Kettenlauf,
danach unverändert 160/160 (`TestResults-g004-t5-gruen2.xml`), kein doppeltes
Decal.

## 3. Regression

| | Baseline E5 | G-004 |
| --- | ---: | ---: |
| Tests gesamt | 831 | 991 |
| bestanden | 760 | 921 |
| fehlgeschlagen | 71 | 70 |

**Namensdiff: 0 neue Fehlschläge (`=>`), 1 behoben (`<=`).** Behoben ist
`StyleProofContentTests.GreenwoodVolume_ContainsRequiredOverrides` — einer der
beiden Altlasten-Fehlschläge aus den Etappen 4 und 5. Beleg:
`TestResults-g004-final3.xml`.

Nach dem Entfernen der beiden temporären Editor-Einstiege
(`GroundContactRebuildChain`, `G004CaptureEntry`) wurde die Suite wiederholt:
`TestResults-g004-final4.xml`, unverändert 991 / 921 / 70 und weiterhin 0 neue
Fehlschläge. Nichts hing an den Helfern.

**Dauerhaft im Projekt verbleiben** nur zwei Dateien:
`Assets/_Game/Editor/GroundContactAudit.cs` (Höhenbericht, Menüpunkt) und
`Assets/_Game/Editor/Tests/GroundContactTests.cs` (der Vertrag).

**Testanpassung, offen ausgewiesen.** Die Stilprüfungen in
`EidrenWorldStyleTests` und `VisualAssetTests` verlangen an *jedem* Mesh
Vertexfarben und an *jedem* Renderer `M_EidrenWorld_VertexLit`. Das Decal ist
beides nicht — es trägt das eingebaute Quad und `M_World_ContactShadow`. Die
Prüfungen klammern den `ContactShadow`-Teilbaum jetzt aus, statt ihre
Zusicherungen abzuschwächen; für jedes echte Bauteil bleiben sie unverändert
scharf. Der Vertrag des Decals steht vollständig in `GroundContactTests`.

## 4. Bildbelege

`TempReview/G004/vorher/` (5 PNG, G-001-Strecke vor G-004),
`TempReview/G004/nachher/` (5 PNG, danach),
`TempReview/G004/inwelt/` (2 PNG, `SP_Tree_A` und `SP_Plant_Bush` in
Zone_Greenwood). Vergleichsbasis für die In-Welt-Bilder ist
`TempReview/StilumbauE4/inwelt/` vom selben Tag, 15:35 — nach dem Stilumbau,
vor G-004.

**Sichtbefund In-Welt:** Der Schattenwurf war bereits vor G-004 vorhanden
(`LightShadows.Hard` seit G-003). G-004 ersetzt dessen hart abgegrenzte,
sechseckige Kante durch einen weich auslaufenden Rand. Baum und Strauch stehen
sichtbar auf dem Boden; die Pilz- und Blütengruppen tragen eigene kleine
Schatten.

**Vom Kontaktdecal ist in den beiden In-Welt-Aufnahmen nichts eindeutig zu
unterscheiden.** Bei 71,7° Sonnenhöhe liegt der Wurf so eng am Objektfuß, dass
er die Fläche des Decals überdeckt. Das Decal ist nachweislich vorhanden
(Abschnitt 2) und kostet nichts, sein eigenständiger Beitrag ist an diesen
beiden Objekten aber nicht belegt. Bewertung siehe Abschnitt 8, Punkt 2.

**Die fünf G-001-Positionen taugen nicht als Nachweis für G-004.** Mittlere
Pixeldifferenz vorher/nachher: 0,006 / 0,009 / 0,624 / 0,000 / 0,006 von 255.
Zwei Positionen zeigen leeren Boden beziehungsweise Bodenbewuchs, der von der
Aufnahmeregel ausgenommen ist; die anderen drei sind Figuren-Nahaufnahmen. Die
Strecke wurde für G-002 (Bodenmaterial) und G-003 (Tonwert) gewählt. Das ist
eine Aussage über den Messpunkt, nicht über die Wirkung.

## 5. Bildrate (Kriterium 8)

Median in Millisekunden, gemessen über die G-001-Strecke:

| Position | Ausgangsstand | nach G-004, Messung 1 | Messung 2 |
| --- | ---: | ---: | ---: |
| `boden_vorher` | 1,396 | 1,665 | 1,527 |
| `boden_nachher` | 1,466 | 1,741 | **1,077** |
| `boden_bewuchs` | 1,841 | 1,984 | **1,501** |

Zwei Läufe **desselben** Builds unterscheiden sich stärker voneinander (1,741
gegen 1,077 ms) als jeder von ihnen vom Ausgangsstand; Messung 2 ist auf zwei
von drei Positionen schneller als der Ausgangsstand. Die Streuung des
Instruments überdeckt den Effekt.

**Ergebnis: Ein systematischer Abfall ist nicht nachweisbar — und ein
Nichtabfall ebenso wenig.** Das Kriterium ist mit diesem Instrument bei dieser
Genauigkeit nicht entscheidbar. Sichtbare Renderer stiegen von 79 auf 86 an der
dichtesten Position; die Bildzeiten liegen durchgehend unter 2 ms.

## 6. Warum die G-003-Decals verlorengingen

Vorbefund, weil er die Bauweise von G-004 bestimmt hat: `WorldContactShadowBuilder`
hatte die Decals im Rahmen von G-003 korrekt in die Prefabs gebaut. Der
Stilumbau (Etappen 1 bis 5) hat dieselben Prefabs anschließend regeneriert und
die `ContactShadow`-Kinder dabei überschrieben. Messung vor Beginn von G-004:
0 von 18, 0 von 24, 0 von 16 und 0 von 9 Prefabs trugen noch ein Decal;
erhalten waren sie ausschließlich eingebacken in den acht Zonenszenen.

Die Kopplung war eine Konvention („nach jedem Builder-Lauf `BuildAll()` erneut
aufrufen"), kein Vertrag. G-004 macht sie zu einem Vertrag: Das Decal entsteht
im Bauweg, und `GroundContactTests` wird rot, sobald es fehlt.

## 7. Vorfälle und Befunde

1. **`ITEM_TMP_SmithingMark.png` fehlt** und lässt `CraftingContentBuilder.BuildAll()`
   vollständig scheitern (`FileNotFoundException` in `ItemContentAssetBuilder`).
   Für G-004 umgangen; der Blocker besteht fort und trifft jeden, der diesen
   Builder aufruft.
2. **Zwei Stations-Prefabs lassen sich nicht neu bauen.** `BuildStorageChestPrefab`
   und `BuildWorkbenchPrefab` kehren früh zurück, solange die handgebaute
   `Geometry_A14`-Geometrie vorhanden ist (Etappe-2-Entscheid). Sie bekommen ihr
   Decal direkt am Prefab über `AddToPrefab`. Unbedenklich, weil kein Lauf es
   wegwerfen kann; die `Attach`-Aufrufe in beiden Buildern bleiben für einen
   späteren Neubau stehen.
3. **Kistenbounds waren durch Sprites verzerrt.** Die drei Weltkisten tragen die
   Sprite-Glyphen `OpeningHand_Left/Right` (F-005). Über alle Renderer gemessen
   ergaben sie 11,4 × 6,15 × 8,26 statt 1,2 × 0,59 × 0,79. `Attach` misst
   deshalb ausschließlich `MeshRenderer`.
4. **F-010 ist nicht behoben.** Die korrigierten Kistenmaße bestätigen die Regel
   „Höhe > Tiefe" als weiterhin verletzt: `WorldChest_Common` 0,594 hoch bei
   0,792 tief, `WorldChest_Hidden` 0,535 bei 0,954. Gehört zu F-010, hier nur
   festgehalten.
5. **Nebenwirkungen zurückgenommen.** Ein erster Kettenlauf rief noch
   `StorageContentBuilder.BuildAll()` auf und schrieb dabei
   `Resources/UI/StorageWindow.prefab` und `Scenes/HomeBase.unity` mit. Beide
   sind aus der Task-0-Sicherung wiederhergestellt, danach erneut 160/160.
6. **Lootdaten unverändert.** Die sieben `Data/Loot/WorldChests`-Assets zeigen
   als einzige Änderung `type: 2 → type: 3` in den Prefab-Referenzen — gleiche
   GUIDs, gleiche fileIDs, 73 Zeilen vorher wie nachher. Ein
   Serialisierungsmerker, kein Inhalt.
7. **Werkzeugbefund G-001.** Der zweite Capture-Lauf des Skripts
   `Tools/G001-capture.ps1` nimmt nichts auf; `Stop-StalePlayer` läuft nur
   einmal zu Beginn, nicht zwischen den Läufen. Einzelläufe sind zuverlässig
   (dreimal belegt). Die Determinismusprüfung wurde deshalb mit zwei
   Einzelläufen erbracht: maximale mittlere Pixeldifferenz 0,357 bei
   Schwellwert 1,0.

## 8. Offene Punkte

1. **Kriterien 3 und 4 sind nicht belegt.** Bewegung, Sprung und Ausweichrolle
   erfordern ein Capture aus dem laufenden Spiel; die G-001-Strecke fotografiert
   ausschließlich feste Kamerapositionen. Konstruktiv sind sie erfüllt — der
   echte Schattenwurf folgt der Figur ohne eigene Logik — nachgewiesen sind sie
   nicht. Für einen echten Nachweis müsste die Capture-Strecke um bewegte
   Aufnahmen erweitert werden.
2. **Der eigenständige Beitrag des Kontaktdecals ist nicht belegt** (Abschnitt 4).
   Zur Entscheidung: Es kostet nichts und ist per Test abgesichert, aber wenn es
   optisch nichts beiträgt, wäre die ehrliche Konsequenz, entweder seine
   Deckkraft anzuheben oder es zugunsten des reinen Schattenwurfs fallenzulassen.
   Das ist eine Gestaltungsentscheidung des Auftraggebers.
3. **Kriterium 8 ist nicht entscheidbar** (Abschnitt 5). Für eine belastbare
   Aussage bräuchte es mehrere Läufe je Stand auf einer ruhenden Maschine.
4. **Kriterium 1 ist für die Sprite-Gegner nicht neu geprüft.** Sie behalten
   `DynamicActorGroundShadow` unverändert; dass jedes Gegner-Prefab tatsächlich
   einen Blob trägt, wurde im Rahmen von G-004 nicht nachgemessen.
5. **Freigabe durch den Auftraggeber steht aus.**

## 9. Nachvollziehen

Volle EditMode-Suite:

    & "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
      -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
      -runTests -testPlatform EditMode -testResults "TestResults-g004-final3.xml" -logFile "g004-final3.log"

**Wartemuster beachten:** `&` kehrt zurück, bevor Unity fertig ist, und
`Wait-Process -Name Unity` allein genügt nicht — ist Unity noch nicht
registriert, kehrt es sofort zurück und der beendende Aufruf nimmt Unity mit.
Erst auf das Erscheinen, dann auf das Verschwinden des Prozesses warten (Muster
in [G004_PLAN.md](G004_PLAN.md), Globale Vorgaben).

Höhenbericht neu erzeugen: Menüpunkt `Eidren/V0.2/G004/Hoehenbericht schreiben`
beziehungsweise `-executeMethod Eidren.Editor.GroundContactAudit.SchreibeHoehenberichtFuerAutomation`.

**Belege:** `g004-hoehen.csv`, `g004-t1-hoehen*.log`, `g004-t3-red*.log`,
`g004-t4-compile.log`, `g004-t5-rebuild*.log`, `g004-t5-gruen*.log`,
`g004-t6-lighting.log`, `g004-t7-build.log`, `g004-t7-capture*.log`,
`g004-t7-inwelt.log`, `g004-final*.log` sowie die zugehörigen
`TestResults-g004-*.xml`.

**Sicherung des Standes vor G-004:**
`C:\Users\phine\Documents\Eidren-Sicherungen\vor-g004-20260811-0120`
(5.477 Dateien, 847,5 MB).
