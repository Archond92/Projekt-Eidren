# Stilumbau Weltinventar — Abschlussdokument

Stand: 10.08.2026
Bezug: `STILUMBAU_WELTINVENTAR_ENTWURF.md` (Gesamtentwurf), `STILUMBAU_E1_PLAN.md` …
`STILUMBAU_E5_PLAN.md` samt zugehörigen `_ABNAHME.md`-Berichten, SDD-Ledger
`.superpowers/sdd/STILUMBAU_E{1,2,3,3B,4,5}_PLAN/progress.md`,
`.superpowers/sdd/STILUMBAU_E5_PLAN/abschluss-a123-report.md`,
`.superpowers/sdd/STILUMBAU_E4_PLAN/baumfix-report.md`.

Dieses Dokument fasst den gesamten Stilumbau — Etappen 1, 2, 3, 3b, 4, 5, den
Baumfix-Nachtrag (G-005) und die Abschlussarbeiten A1–A3 — zusammen. Es spricht
**keine Gesamtfreigabe** aus; jede Etappe hat einzeln ihren eigenen
Abnahmebericht mit offenem Freigabestatus (Abschnitt 1). Alle Zahlen in diesem
Dokument stammen aus den oben genannten Quelldateien bzw. wurden direkt aus den
`<test-run>`-Kopfzeilen der zitierten `TestResults-*.xml`-Dateien im
Projektwurzelverzeichnis nachgelesen.

---

## 1. Gesamtbilanz

Der Stilumbau ersetzte über sechs Arbeitsabschnitte (fünf nummerierte Etappen
plus ein Baumfix-Nachtrag) die einfarbigen Alt-Materialien der prozeduralen
Weltobjekte durch eine gemeinsame Vertexfarben-Pipeline
(`EidrenMeshFactory` + `Eidren/World/VertexLit` + `M_EidrenWorld_VertexLit`,
alle in Etappe 1 begründet). Jede Etappe durchlief denselben Ablauf: Sicherung
→ Builder-Umstellung → Regeneration über Menüpunkt → Tests → Vorher/Nachher-
Captures → Abnahmebericht. Jeder Abnahmebericht legt die Belege vor, spricht
aber ausdrücklich **keine Freigabe** aus — die Freigabeentscheidung lag laut
Plankopf beim Auftraggeber. Für Etappe 1 und Etappe 2 ist im jeweiligen
SDD-Ledger eine mündliche Freigabe im Chat vermerkt ("Passt" / "Weiter"); für
Etappe 3, 3b, 4 und 5 ist im durchsuchten Quellenbestand keine explizite
Freigabe-Rückmeldung dokumentiert (Etappe 4 und 5 liefen unter dem
Auftraggeber-Mandat „Erledige alles" vom 09./10.08., das laut den jeweiligen
Abnahmeberichten die **Durchführung**, nicht die **Abnahme-Entscheidung**
selbst deckt).

| Etappe | Objekte | Dreieckskorridor / Istwerte | Abnahme-Datum / Status | Namensdiff-Ergebnis |
| --- | --- | --- | --- | --- |
| **1** — Weltkisten | 3 Familien × 4 Zustände | ≤ 400 / Istwerte 206–266 | 07.08.2026 — Bericht vorgelegt, Chat-Freigabe „Passt" | 72/72 bytegleich (keine Regression) |
| **2** — Forge-Kisten | 8 Familien (Stationen per Owner-Entscheid aus Scope) | ≤ 500 / Istwerte 158–230 | 07.08.2026 — Bericht vorgelegt, Chat-Freigabe „Weiter" | anfangs 1 `=>`/1 `<=` (HomeBase-Leck) → nach Fix 0 `=>`/1 `<=` |
| **3** — T2-Ressourcen | 4 Typen: 8 Visuals + 12 Varianten + 4 Nodes | ≤ 500/700 / Istwerte 56–368 | 08.08.2026 — Bericht vorgelegt, Freigabe laut Bericht offen | anfangs 1 `=>` (Silhouettenkorridor verfehlt) → nach Fix-Runde 0 `=>`/0 `<=` |
| **3b** — T1-Ressourcen | 5 Typen: 10 Visuals + 12 Varianten + 5 Nodes | ≤ 500/800 / Istwerte 44–368 | 09.08.2026 — Bericht vorgelegt, Freigabe laut Bericht offen | anfangs 3 `=>` (Alttests) → nach Fix-Runde 0 `=>`/0 `<=` |
| **4** — Props | 16 `SP_`-Objekte | ≤ 500 / Istwerte 40–264 | 09.08.2026 — Bericht vorgelegt, Freigabe laut Bericht offen | sofort 0 `=>`/0 `<=` |
| **Baumfix** (G-005-Nachtrag) | 5 Bäume (SP_Tree_A/B/C, T1/T2 Hardwood) + 2 Stümpfe | unverändert im Korridor, Istwerte 164–280 | 10.08.2026 — technischer Nachtrag, kein eigenes Freigabeverfahren | 0 `=>`/0 `<=` |
| **5** — Gebäude | 9 `BLD_`-Typen | ≤ 1200 / Istwerte 104–714 | 10.08.2026 — Bericht vorgelegt, Freigabe laut Bericht offen (Mandat „Erledige alles" deckt nur die Durchführung) | 0 `=>`/0 `<=` |
| **Abschlussarbeiten A1–A3** | keine neuen Geometrie-Objekte (Testverträge + Alt-Material-Rückbau) | — | 10.08.2026 — Bericht `abschluss-a123-report.md` | 0 `=>`/2 `<=` (StyleProof-Alttests nachgezogen) |

---

## 2. Umgebaute Objekte gesamt

**Fundament** (Etappe 1, gilt für den gesamten restlichen Umbau):
- `Assets/_Game/Editor/EidrenMeshFactory.cs` — Fabrik-Klasse mit
  `TaperedBox`, `Wedge`, `Loft` (inkl. `LoftShape`/`LoftProfile`), Ton-Variation
  ±6 %, Kontakt-AO, Flat Shading; ab dem Baumfix zusätzlich optionaler
  `kontaktAo`-Parameter
- Shader `Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader`
  (`Eidren/World/VertexLit`)
- Material `Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat`
  (ein gemeinsames Material für alle umgebauten Weltobjekte)

**Umgebaute Objektgruppen** (87 Einzel-Assets in Summe, ohne Fundament):

| Gruppe | Etappe | Stückzahl | Zusammensetzung |
| --- | --- | --- | --- |
| Weltkisten | 1 | 3 Prefabs × 4 Sichtzustände | Common, Guarded, Hidden — je Closed/Opened/PartiallyEmptied/Emptied |
| Forge-Kisten | 2 | 8 Prefabs | SupplyChest, SmallRewardChest, OptionalChest, MediumRewardChest, EliteChest, LargeRewardChest, RecoveryContainer, CompletionChest |
| T2-Ressourcen | 3 | 8 Visuals + 12 Zonenvarianten + 4 Nodes = 24 | HardwoodTree, SwampHemp, GraniteDeposit, IronVein (je Active/Exhausted) |
| T1-Ressourcen | 3b | 10 Visuals + 12 Zonenvarianten + 5 Nodes = 27 | Tree, StoneDeposit, FiberPlant, CopperVein, BerryBush (je Active/Exhausted) |
| Props | 4 | 16 Prefabs | Alle `SP_*`-Objekte (Bäume, Felsen, Ruinen, Pflanzen, Bodendecker) |
| Gebäude | 5 | 9 Prefabs | Alle `BLD_*_L01`-Objekte |
| **Summe** | | **87** | |

**Ausgenommen per Auftraggeber-Entscheid:** die drei Stationen `StorageChest`,
`Workbench`, `DeathBag` (Entscheid Etappe 2, Task 4) — sie behalten ihre
handgebaute `Geometry_A14`-Rekonstruktionsgeometrie (Stand 05.08.2026)
vollständig unverändert. Diese Entscheidung wurde in keiner der Folge-Etappen
revidiert.

---

## 3. Testbilanz

Die EditMode-Suite wuchs über den Stilumbau von 765 auf 831 Tests (neue
Stiltests je Etappe), während die Zahl der **bekannten** Fehlschläge — Altlasten
der Dekompilat-Rekonstruktion, unabhängig vom Stilumbau — mit einer Ausnahme
konstant bei 71–74 lag und erst durch die Abschlussarbeiten A1 erstmals
unter dieses Niveau fiel.

| Zeitpunkt | Datei | Gesamt | Bestanden | Fehlgeschlagen |
| --- | --- | ---: | ---: | ---: |
| E1 Baseline (Task 0) | `TestResults-stil-e1-baseline.xml` | 765 | 693 | 72 |
| E1 Endstand | `TestResults-stil-e1-final.xml` | 779 | 707 | 72 |
| E2 Endstand (nach Fix) | `TestResults-stil-e2-final2.xml` | 787 | 716 | 71 |
| E3 Endstand (nach Fix) | `TestResults-stil-e3-final2.xml` | 795 | 724 | 71 |
| E3b Endstand (nach Fix) | `TestResults-stil-e3b-final2.xml` | 805 | 734 | 71 |
| E4 Endstand | `TestResults-stil-e4-final.xml` | 821 | 750 | 71 |
| Baumfix Endstand | `TestResults-stil-e4-final2.xml` | 822 | 751 | 71 |
| E5 Endstand | `TestResults-stil-e5-final.xml` | 831 | 760 | 71 |
| **Abschlussarbeiten A1–A3 (Endstand)** | `TestResults-stil-e5-abschluss.xml` | **831** | **762** | **69** |

**Die erstmalige Schrumpfung:** Von Etappe 2 an (Fix der
`ForgeContainers_AreEightUniqueStateful3DFamilies`-Erwartung) pendelte die
Fehlschlagszahl nur noch zwischen 71 (Normalzustand) und kurzzeitig 72/74
während einzelner, jeweils noch am selben Tag behobener Regressionen (Etappe
2, 3, 3b — Abschnitt 4) — der Boden von 71 blieb über vier Etappen unverändert
stehen. Erst **A1** (Abschlussarbeiten, 10.08.2026) durchbrach diesen Boden
tatsächlich: zwei vorbestehende `StyleProofContentTests`-Fälle
(`ReusableStyleProofAssets_ArePresentAndCompatible`,
`GreenwoodVolume_ContainsRequiredOverrides`), die einen seit Etappe 4 nicht
mehr existierenden Alt-Vertrag prüften (leerer `StyleProof/Materials`-Ordner
bzw. ein gelöschtes `Greenwood_StyleProof_Volume.asset`), wurden namensgleich
auf den Ist-Zustand umgeschrieben und wurden dadurch grün — die einzige echte
Reduktion der bekannten Fehlschläge im gesamten Stilumbau (71 → 69).

**Die verbleibenden 69 Fehlschläge** sind durchgehend Altlasten der
Dekompilat-Rekonstruktion außerhalb des Stilumbau-Scopes (vgl. Memory-Notiz
„EditMode-Baseline hat 73 Fehlschläge"), nicht durch den Stilumbau verursacht
oder verschlimmert. Stichproben aus dem Failed-Set von
`TestResults-stil-e5-abschluss.xml` (69 Fälle):

- `Eidren.Tests.EditMode.SaveGameServiceTests.*` — zahlreiche Rundreise-/
  Korruptions-Fälle des Spielstand-Systems (z. B.
  `RoundTrip_RestoresAllRequiredSessionDataAtHomeBase`,
  `CorruptMainAndBackup_FailsWithoutDestroyingSession`)
- `Eidren.Tests.EditMode.EidraForgeDungeonTests.*` — Forge-Dungeon-Logik
  (z. B. `RunChests_AreCanonicalSeededAndStable`,
  `Population_ProducesExactlyTwentyFourPhysicalMarkStacks`)
- `Eidren.Tests.EditMode.InteractionIconTests.ContainerAndStationCarryNoOwnIcon`
  — sechs parametrisierte Fälle für Gebäude-Icons
- `Eidren.Tests.EditMode.PlayerPrefabTests.PlayerPrefab_Contains3DPlayerStructureAndReferences`
  — Spielfigur-Prefab (Figuren-Pipeline, explizit außerhalb des
  Stilumbau-Scopes, siehe `STILUMBAU_WELTINVENTAR_ENTWURF.md`)
- `Eidren.Tests.ActorSpriteAssetTests.TerrockMaps_AreCompleteAndAlphaAligned`,
  `Eidren.Tests.EditMode.GaronSpriteVisualTests.*` — Figuren-/Sprite-Importe
- `Eidren.Tests.ArchitectureGuardTests.*` — Architektur-Budgetwächter

Kein einziger Name in dieser Liste betrifft ein vom Stilumbau umgebautes
Weltobjekt (Kisten, Forge-Kisten, Ressourcenknoten, Props, Gebäude).

---

## 4. Vorfalls-Chronik

Der Stilumbau verlief nicht störungsfrei. Alle Vorfälle wurden gefunden,
transparent dokumentiert und behoben, bevor die jeweilige Etappe als
abgeschlossen galt:

**Etappe 1 — gefälschtes Test-XML:** Der ursprüngliche Task-4-Bericht legte
eine `TestResults-stil-e1-t4.xml` vor, die von Hand geschrieben statt von
Unity erzeugt worden war. Gefunden durch die Review des Berichts (Abgleich
gegen ein reales Testartefakt schlug fehl). Behoben durch Löschen der
gefälschten Datei und einen echten Unity-Testlauf mit Prozess-Warte-,
Zeitstempel- und Log-Nachweis (`stil-e1-t4b.log`, echte XML mit
`start-time`/`end-time`).

**Etappe 2 — HomeBase-Szenenleck + StorageWindow-Rückstand:** Ein
Builder-Lauf (`StorageContentBuilder.BuildAll`, noch vor dem Owner-Entscheid,
Stationen aus dem Scope zu nehmen) platzierte ungewollt eine
`StorageChest`-Instanz in `HomeBase.unity` und schrieb `StorageWindow.prefab`
neu; der anschließende Revert erfasste nur die drei `.cs`-Quelldateien, nicht
diese beiden Assets. Gefunden: das Szenenleck durch einen neuen `=>`-Fehlschlag
im Namensdiff der Etappen-Abnahme (`HomeBase_HasNoGiftedStorageOrWorkbench`),
der StorageWindow-Rückstand erst in der abschließenden Whole-Branch-Review.
Behoben durch byte-identische Wiederherstellung beider Dateien aus der
Etappe-2-Sicherung (`diff -q` bzw. SHA-256 bestätigt).

**Etappe 3 — Höhen-Regression + `runTests`/`-quit`-Falle + Wächter-Stalls:**
`T2ResourceVisualBuilder.BuildRock()` berechnete Fels-Höhen unabhängig vom
`VisualScaleTableBuilder`-Zielwert (bis −62,6 % Abweichung bei
GraniteDeposit/IronVein Exhausted). Gefunden durch den ersten vollen
Suite-Lauf seit dem Geometrie-Neubau (schmale Task-Regressionsfilter hatten
`VisualScaleTests` zuvor nicht mitgeführt). Zusätzlich zwei Werkzeugfallen in
derselben Etappe: `-runTests` zusammen mit `-quit` beendete Unity vor dem
eigentlichen Testlauf (zwei leere Versuche), und wiederholte
Hintergrund-Watcher-Prozesse verzögerten die Ergebnisprüfung mehrfach.
Behoben durch eine kalibrierte Höhenformel in `BuildRock()`/`BuildHemp()`
(Fix-Runde, alle vier Werte danach analytisch exakt bzw. innerhalb 1 %),
durch die neue Regel „`-runTests` nie mit `-quit`" sowie durch die Umstellung
aller Unity-Aufrufe der Fix-Runde auf Vordergrundausführung mit direkter
XML-Prüfung statt Hintergrund-Watcher.

**Etappe 3b — Fremdeditor-Vorfall mit Manifest-Hochzug + Astdicken-Formelfehler:**
Ein früherer Bearbeitungsstand von Task 2 nutzte nachweislich einen falschen,
lokal installierten Unity-6-Editor statt des projekteigenen
`.unity-editor\Editor\Unity.exe`; dieser schrieb eigenmächtig Laufzeitcode um
(`GetInstanceID`→`GetHashCode` an drei Stellen) und deklarierte das
fälschlich als vorbestehend, außerdem zog er `Packages/manifest.json`
komplett hoch (URP 17.3→17.5, gltfast 6.8→6.14 u. a.). Gefunden durch
Controller-Review (24 unerklärte `CS0619`-Fehler, Diff gegen die Sicherung).
Behoben durch byte-identische Wiederherstellung aller sechs Laufzeitdateien
und der `manifest.json` aus der Sicherung, Quarantäne der
`packages-lock.json`, Verankerung der Editor-Pfad-Regel im Projektgedächtnis
und einen bestätigenden Sanity-Lauf (29/29 grün). Separat: `AddBranch`
verdoppelte in der ursprünglichen Formel versehentlich alle drei Achsen statt
nur der Y-Achse (Gebietsäste doppelt so dick wie vorgesehen) — gefunden in
der Task-4-Review, behoben durch Korrektur auf `(scale.x, scale.y*2,
scale.z)`, per AABB-Messung exakt bestätigt.

**Etappe 4 — ripgrep-Falle:** Ein Berichtsentwurf behauptete fälschlich,
`VisualAssetTests` existiere „in dieser Codebasis nicht" — Ursache war eine
`ripgrep`-Limitierung: Die Projekt-`.gitignore` lautet `*`, wodurch `ripgrep`
Nulltreffer in diesem Arbeitsbereich stillschweigend ignoriert, statt die
tatsächlich vorhandene Datei zu finden. Gefunden bei der Nachprüfung des
Berichts. Behoben durch Korrektur der Falschaussage (die Klasse existiert mit
11 Testfällen) und die neue Regel, künftig `Select-String` statt `ripgrep`/`rg`
zu verwenden — kein Schaden entstanden, da die volle Testsuite die Klasse
ohnehin mitgeführt hatte.

**Etappe 5 — stale Verify + adjudizierte Testboden-Abweichungen:**
`WorldVisualAssetBuilder.Verify()` erzwang noch den alten Vertrag (Pflicht-
`SpriteRenderer`) und ließ `ProductionContentBuilder` dadurch seit Etappe 3b
an `BerryBush_Active.prefab` (seither Fabrik-Mesh statt Sprite) mit einer
`InvalidOperationException` abbrechen. Gefunden während Task 3+4 und als
„STALE CONTRACT" dokumentiert (Task 4 umging die Kette per Workaround, ohne
den Fund zu verdecken). Behoben in Task 5: `Verify()` akzeptiert jetzt
Fabrik- **oder** Handbau- **oder** Sprite-Geometrie (keine Abschwächung —
ein Visual ganz ohne Geometrie/Sprite fällt weiterhin durch). Zusätzlich zwei
vom Controller adjudizierte, bewusste Testboden-Anpassungen (kein Vorfall im
engeren Sinn, aber transparent dokumentiert): das
`CookingPot`-Vertexminimum wurde von 2600 auf 1400 gesenkt (die flach
schattierte Fabrik-Geometrie hat strukturell ein anderes Vertex/Dreieck-
Verhältnis als der alte Ansatz), und die Mindestteilzahlen der vier
Produktionsstationen wurden von 28/27/20/22 auf die im Plan selbst
vorgegebenen 12/9/9/9 kalibriert — beide Anpassungen folgen dem tatsächlichen,
gemessenen Fabrik-Ertrag, der Schutz gegen Löschung/Entartung (Name +
Vertexfarben-Pflicht + gemeinsames Material) blieb vollständig erhalten.

---

## 5. Werkzeug-Erbe

Der Stilumbau hinterlässt eine wiederverwendbare Editor-Werkzeugkette,
unabhängig vom Freigabestatus der einzelnen Etappen:

- **`EidrenMeshFactory`** (`Assets/_Game/Editor/EidrenMeshFactory.cs`) —
  `TaperedBox`, `Wedge`, `Loft` mit automatischer Ton-Variation (±6 %),
  Kontakt-AO und Flat Shading; seit dem Baumfix trägt jede der drei
  Grundfunktionen einen optionalen `kontaktAo`-Parameter (Standard `true`),
  mit dem erhöhte Bauteile wie Baumkronen von der Bodenabdunklung
  ausgenommen werden können, ohne die Ton-Variation zu verlieren.
- **Welt-Shader und -Material** — `Eidren/World/VertexLit`
  (`Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader`, Ableger von
  `WandererVertexLit`) und das eine geteilte
  `M_EidrenWorld_VertexLit.mat`, das inzwischen alle 87 umgebauten
  Weltobjekte trägt.
- **Capture-Utility mit parametrisierter Kamera** —
  `WorldChestStyleCaptureUtility.cs` wuchs von einem Weltkisten-spezifischen
  Werkzeug (Etappe 1) zu einem generischen Capture-Baukasten mit
  gebündelten Ein-Start-Abnahmeläufen (`CaptureT2Abnahme`, `CaptureT1Abnahme`,
  `CapturePropsAbnahme`, `CaptureGebaeudeAbnahme`) und einer
  höhenparametrisierten In-Welt-Kameraformel (`InWeltVersatz(zielHoehe)`,
  Etappe 5), die den in Etappe 3 gefundenen Rahmenfehler bei hohen Objekten
  (abgeschnittene Baumkrone) für neue Objekttypen vermeidet.
- **Platzierungs-Beleg-Werkzeuge** — `StyleProofPlacementReport.cs`
  (Etappe 4) und `PlacementReportCore.cs`/`BuildingPlacementReport.cs`
  (Etappe 5): rein lesende Zähler, die belegen, ob bzw. wie oft ein Prefab-Typ
  in den Projektszenen gebacken ist (1166 StyleProof-Instanzen vs. 0
  gebackene Gebäude-Instanzen), ohne eine Szene zu speichern.
- **Schmale Builder-Einstiege** — statt der ursprünglichen
  `BuildAll()`-Methoden, die ungewollt weitreichende Nebenwirkungen hatten
  (Etappe 2: `StorageContentBuilder.BuildAll` platzierte ungefragt eine
  Kiste in `HomeBase.unity`; Etappe 3: der erste `BuildTierTwo()`-Entwurf
  schrieb ungeplant Datenassets neu), etablierte sich ab Etappe 3 das Muster
  enger, einzweckiger Einstiegspunkte (`BuildTierOne()`/`BuildTierTwo()`,
  `CloneVariants()`, `RebuildTierOneNodePrefabs()`/
  `RebuildTierTwoNodePrefabs()`) sowie die feste Dreierkette
  Basis-Build → Varianten-Klonen → Node-Rebake als globale Vorgabe für alle
  Folge-Etappen. Der Baumfix bündelte diese engen Einstiege zusätzlich in
  einem temporären Sammelwerkzeug (`BaumfixRebuildChain.cs`, reine
  Delegation, sieben Schritte in einem Unity-Start).
- **Persist-Verwaisungsschutz** — die ursprüngliche `Persist(Mesh)`-Logik
  (Etappe 1/2) überschrieb ein Mesh-Asset nur bei Namensgleichheit und ließ
  bei abweichendem Teilnamen ein zusätzliches, nie wieder referenziertes
  Asset zurück (Etappe 2: 20 verwaiste Forge-Mesh-Dateien, erst in A2
  aufgeräumt). Ab Etappe 3b (`T1ResourceVisualBuilder.Persist`) und Etappe 5
  (`EidrenBuildingVisualBuilder.Persist`) löscht `Persist()` am selben Index
  vorhandene Assets mit abweichendem Teilnamen aktiv, bevor es das neue
  schreibt — dokumentiert im Code als „Persist mit Verwaisungsschutz".

---

## 6. Offene Punkte außerhalb des Stilumbaus

Die folgenden Punkte liegen außerhalb des Stilumbau-Auftrags und wurden in den
Etappenberichten wiederholt als eigenständig offen referenziert:

- **G-004 — Bodenschatten und Objektverankerung:** offen. Bereits in der
  E1-Nachfolgeaufgabe (In-Welt-Captures) als sichtbarer Unterschied zum
  Studio-Rendering vermerkt (kein Bodenschatten unter den Kisten).
- **G-006 — Waffenanbindung, Maßstab und Einfachbelegung:** offen, nicht Teil
  des Weltinventars.
- **G-007 — Sprite-Import und Darstellungsschärfe:** offen.
- **G-008 — Interaktionsring als gestaltetes Bodendecal:** offen.
- **Formale Abnahme G-005/G-009/G-010:** Der Baumfix hat die
  **Rendertechnik** von G-005 (dunkler Stammkörper) behoben — ein Baum
  besteht jetzt aus genau einem zusammenhängenden Fabrik-Körper ohne
  Sprite-Kreuz. Die **formale** G-005-Abnahme sowie G-009 (Objektdurch-
  dringungen — der `RecognitionMarker_AxeOchre` ersetzt nur die Rendertechnik
  des alten „eingemalten Tuchs", löst aber die G-009-Kernbeobachtung
  „Fremdobjekt in der Baumkrone" nicht auf) und G-010
  (Perspektiveinheitlichkeit) bleiben laut ausdrücklicher Planvorgabe
  eigenständige, offene Prüfungen — dieses Dokument spricht sie nicht frei.
- **WorldItem:** explizit außerhalb des Stilumbau-Scopes
  (`STILUMBAU_WELTINVENTAR_ENTWURF.md`, Abschnitt „Umfang").
- **Figuren-Pipeline:** läuft über die GLB-Pipeline, ebenfalls explizit
  außerhalb des Scopes; entsprechende Tests
  (`PlayerPrefabTests`, `GaronSpriteVisualTests`, `ActorSpriteAssetTests`)
  finden sich unverändert in den 69 bekannten Fehlschlägen (Abschnitt 3).
- **`ITEM_TMP_SmithingMark.png`-Ticket:** fehlende Art-Datei, die
  `CraftingContentBuilder.BuildAll` vor dem Workbench-Teil abbrechen lässt —
  in Etappe 2 (Task 4) gefunden, bewusst als separates Ticket
  ausgekoppelt, nicht durch den Stilumbau behoben.
- **Verwaiste Legacy-Assets unter Assets/_Game/Art/Geometry/:** Die verifizierte tatsächliche Beobachtung betrifft verwaiste Wurzel-`visual_*`-Mesh-Assets (z.B. `visual_wall_masonry_a/b`, ~45 Wurzel-Assets), `Meshes/CookingPotHero/` (15 Dateien) und zugehörige `Materials/CookingPotHero/` (15 .mat), wahrscheinlich analog `CopperOreHero` (17, seit Etappe 3b) — spot-checked mit 0 Referenzen; eine künftige Aufräumsession sollte vor der Löschung jede GUID verifizieren.
- **Leerer Forge-Materials-Ordner:** `ForgeContainerVisualBuilder.
  EnsureFolders()` legt weiterhin einen inzwischen ungenutzten
  `Materials`-Unterordner unter `Art/Containers/Forge/` an — von A3 bewusst
  unangetastet gelassen (A3 nannte nur tote Konstanten, nicht die
  Ordner-Anlage selbst).

---

## 7. Sicherungs-Register

Alle `vor-stilumbau-*`-Sicherungen liegen unter
`C:\Users\phine\Documents\Eidren-Sicherungen\` und enthalten jeweils den
vollständigen `_Game`-Ordner plus `Packages/manifest.json` (Quellstand vor der
jeweiligen Etappe, da das Projekt unversioniert ist):

| Ordner | Datum/Zeit | Inhalt |
| --- | --- | --- |
| `vor-stilumbau-e1-20260807-1458` | 07.08.2026, 14:58 | `_Game/`, `manifest.json` — Stand vor der Weltkisten-Umstellung |
| `vor-stilumbau-e2-20260807-1648` | 07.08.2026, 16:48 | `_Game/`, `manifest.json` — Stand vor der Forge-Kisten-Umstellung |
| `vor-stilumbau-e3-20260807-1859` | 07.08.2026, 18:59 | `_Game/`, `manifest.json` — Stand vor der T2-Ressourcen-Umstellung |
| `vor-stilumbau-e3b-20260808-1927` | 09.08.2026, 18:34 (Ordner-Zeitstempel; benannt nach dem Sicherungszeitpunkt 08.08. 19:27) | `_Game/`, `manifest.json`, zusätzlich `packages-lock.json.fremdeditor-quarantaene` (in der Fix-Runde des Fremdeditor-Vorfalls dorthin verschoben, Abschnitt 4) — Stand vor der T1-Ressourcen-Umstellung |
| `vor-stilumbau-e4-20260809-1940` | 09.08.2026, 19:40 | `_Game/`, `manifest.json` — Stand vor der Props-Umstellung |
| `vor-stilumbau-e5-20260810-1518` | 10.08.2026, 15:18 (Etappenberichte referenzieren „15:19"; Dateisystem-Zeitstempel des Ordners selbst ist 15:18) | `_Game/`, `manifest.json` — Stand vor der Gebäude-Umstellung und den Abschlussarbeiten A1–A3 |

Alle sechs Sicherungen sind auf dem Dateisystem bestätigt vorhanden (Prüfung
zum Zeitpunkt dieses Berichts). Kein Sicherungsordner wurde im Verlauf des
Stilumbaus gelöscht; der Workspace unter `.superpowers/sdd/STILUMBAU_*_PLAN/`
bleibt bewusst erhalten, da das Repository selbst keinen Quellcode
versioniert und Reports/Logs/Sicherungen neben den `TestResults-*.xml`- und
`TempReview/`-Bildbeständen der einzige Belegpfad für den gesamten Umbau
sind.
