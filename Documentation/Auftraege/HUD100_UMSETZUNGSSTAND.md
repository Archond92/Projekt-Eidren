# HUD-100 – Umsetzungsstand

Stand: 04.09.2026. Grundlage: `AUFTRAG_MOBILE_HUD_UPGRADE_UMSETZUNG.md`.
Status: Paket 1 dokumentiert; persistentes Targeting aus Paket 2, begrenztes
Auto-Facing aus Paket 3 und Combat Target Ring aus Paket 4 produktiv eingebunden.
Regression und Grenzen siehe die jeweiligen Paketabschnitte.
HUD-Prefab unverändert; native Overlay-/Touch-Abnahme und Gesamtabnahme bleiben offen.

## Aktueller Projektstand

- Windows-Entwicklerstand 0.3.4, Unity 6000.3.0f1.
- Die aktuelle Programmierbibel hat Vorrang: mobile Auslegung, zunächst
  Windows-Tests. Android-/iOS-Toolchains und Geräteabnahme bleiben der Portierung vorbehalten.
- Das Git-Repository enthält Releases, nicht den lokalen Unity-Quellcode.
  Ein sauberes `git status` belegt deshalb keine unveränderten Assets.
- `CombatHUD` ist bereits eine schlanke Bindungsschicht mit autoriertem Prefab,
  separaten Presentern, Safe Area, einer Context Action und Minimap.
- Die Reichweite zur Körperoberfläche ist bereits implementiert:
  `CombatTargetGeometry.ClosestBodyPoint/FlatDistanceToBody`. Nicht neu bauen.
- Ausgangsbefund aus Paket 1: Angriffe bekamen eine `SceneCombatTargetQuery`,
  Eidra hielten einen separaten Zielzustand. Der Targeting-Schritt ersetzt dies
  im produktiven Kompositionspfad durch dieselbe `PersistentCombatTargetQuery`.
  Der lokale Eidra-Verweis ist jetzt der Kontext der jeweiligen Aktion.
- Der Eidra-Wechsel liegt oben rechts **innerhalb** des unteren Combat Clusters:
  `CombatHUD/SafeArea/ActionCluster/EidraSwitch`. Die Kommentare und lokalen
  Kantenvergleiche der Minimap-Tests hatten zunächst eine Position am oberen
  Bildschirmrand nahegelegt; der neue Hierarchie-/Weltkoordinatenbeleg widerlegt
  diese erste Annahme. Lokale Anker und Positionen müssen mit ihrem Elternpfad
  interpretiert werden.
- Der bisherige Safe-Area-Cluster-Test prüft lediglich eine Mindestbreite.
  Die neuen Tests prüfen transformierte Rechtecke über die echte Hierarchie.
- 2340 × 1080 ist **19,5:9**, nicht 20:9. Der alte Capture-Runner benennt es
  irreführend als 20:9 und simuliert trotz Dateiname keine seitliche Notch.
  Neue Belege unterscheiden 2340 × 1080 und echte 2400 × 1080.

## Paket 1: implementiertes Sicherheitsnetz

### Rein lesender Prefab-Audit

`Assets/_Game/Editor/CombatHudBaselineAudit.cs`

- Lädt isolierten Prefab-Inhalt, entlädt ihn in `finally`, speichert ihn nie.
- Erfasst vollständige RectTransform-Hierarchie, Aktivzustände, Anker,
  Positionen, Größen und Pivots vor der Simulation.
- Erfasst serialisierte Objektreferenzen einschließlich fehlender Referenzen.
- Erfasst Canvas-Skalierung, Prefab-SHA-256, Unity-Version und UTC-Zeit.
- Berechnet tatsächliche Bedienrechtecke in Safe-Area-Koordinaten über
  `GetWorldCorners` und `InverseTransformPoint`; berücksichtigt den Elternpfad.
- Exportiert vier JSON-Berichte in einen neuen zeitgestempelten Ordner unter
  `TestResults-Archiv/HUD100/`. Menü: `Eidren/HUD/Export HUD-100 Baseline`.
- Explizite Grenze: simulierte Prefab-Geometrie, keine Gerätebedienung,
  Raycast- oder native Screen-Abnahme.

### Sechs neue EditMode-Tests

`Assets/_Game/Editor/Tests/CombatHudResponsiveBaselineTests.cs`

- Zehn Bedienflächen einschließlich Context Action und Inventar liegen bei
  fünf Auflösungs-/Safe-Area-Kombinationen vollständig innerhalb der Safe Area
  und überschneiden sich nicht.
- Formate: 1920 × 1080, 2340 × 1080, 2400 × 1080, 1920 × 1200, 1280 × 720.
- Breite Formate und kleine Auflösung enthalten asymmetrische seitliche und
  obere/untere Insets. Keine Gleichsetzung mit physischen Gerätepixeln oder dp.
- Referenzauflösung 1920 × 1080, höhenorientierte Canvas-Skalierung,
  acht erforderliche HUD-Bindungen, fehlende Referenzen und unveränderte
  Prefab-Datei werden überprüft.

### Windows-Bildbelege

`Assets/_Game/Tests/PlayMode/CombatHudBaselineCaptureTests.cs`

Der neue Test erzeugt Exploration in vier Formaten sowie normalen Kampf und
Garon bei 16:9. Er verwendet echte Szenen, Touch-Anzeigefamilie, feste Weltsaat
und geprüften Gegner-/Bosszustand. Testspielstände und Ausgaben liegen pro Lauf
in einem neuen Belegordner. Teardown räumt geladene Spielszenen und Services ab.

Die Bilder sind Kamera-Renderbelege. Das HUD wird nur für den Renderdurchgang
temporär von Overlay auf ScreenSpaceCamera umgestellt und nach seiner
autorierten Skalierung gerendert; anschließend wird der Zustand wiederhergestellt.
Das ist weder ein nativer Bildschirmmitschnitt noch ein Notch-/Touch-Gerätenachweis.

Geprüfte Bildserie: `TestResults-Archiv/HUD100/captures-20260903-222655-891/`.
Die Ordnerzeit ist UTC; lokal entstand sie am 04.09.2026.
Sechs PNGs samt jeweiliger Metadatendatei: Exploration (16:9, 19,5:9, 20:9,
16:10), normaler Kampf und Garon. Für normale Gegner und Boss wartet der Test
nach Teleport auf eine beruhigte Kamera und prüft die Sichtbarkeit von Spieler
und Ziel. Der erste Probe-Capture zeigte Garon am Bildrand; diese Serie unter
`captures-20260903-222437-775/` bleibt als Vorlauf erhalten, ist aber nicht der
maßgebliche Bildbeleg.

Sichtprüfung der maßgeblichen Serie:

- Rechter Aktionscluster und Spielerstatus bleiben auch in den breiteren
  Formaten randorientiert; keine abgeschnittenen Hauptaktionen sichtbar.
- Exploration und normaler Kampf zeigen weiterhin praktisch dieselbe
  Aktionsdichte. Die Zustandssteuerung ist noch nicht umgesetzt.
- Normaler Kampf zeigt eine gegnergebundene Leiste, aber noch kein neues
  kompaktes Target-HUD und keinen persistenten Combat Target Ring.
- Garon zeigt HP/Stagger und den technischen Text `CHOOSEATTACK`.
  `CombatHudStatusPresenter.RefreshBossState` bestätigt die Ursache:
  unmittelbares `EnemyState.ToString().ToUpperInvariant()`.
- Der Questtext liegt in diesen Renderbelegen über Teilen des Spielerstatus;
  der Touchstick wirkt noch wie eine rechteckige Platzhalterfläche. Vor einer
  Layoutkorrektur im späteren Paket ist das zusätzlich im nativen Spielbild zu
  überprüfen, da Kamera-Render und Overlay-Aufnahme nicht identisch sind.

Prefab-Export: `TestResults-Archiv/HUD100/20260903-222240-039/`.
Vier JSON-Berichte, jeweils 92 Hierarchieknoten und 515 Referenzeinträge.
SHA-256 des HUD-Prefabs vor/nach dem Schritt:
`b6d62b6fee591d727e84094c64e076e5506b1711c43cf5609a0a3009b1f5961c`.

## Testnachweise

Alle Pfade relativ zur Projektwurzel; vollständige XML- und Logdateien bleiben erhalten.

| Lauf | Ergebnis | Beleg unter `TestResults-Archiv/` |
| --- | --- | --- |
| Unveränderte EditMode-Baseline | 1357 bestanden, 0 Fehler | `hud100-baseline-editmode-20260904.xml` |
| PlayMode ohne Grafikgerät | 144 bestanden, 1 Fehler, 12 übersprungen | `hud100-baseline-playmode-20260904.xml` |
| PlayMode mit Grafikgerät | 154 bestanden, 0 Fehler, 3 übersprungen | `hud100-baseline-playmode-graphics-20260904.xml` |
| Erster Lauf neuer Layouttests | 1 bestanden, 5 Fehler im neuen Audit | `hud100-responsive-20260904.xml` |
| Vollständiger EditMode-Lauf nach Korrektur | 1363 bestanden, 0 Fehler | `hud100-editmode-final-20260904.xml` |
| Neuer Capture-Test, erster isolierter Lauf | 1 bestanden; Kameraframing anschließend verbessert | `hud100-captures-20260904.xml` |
| Vollständiger PlayMode-Lauf mit neuem Capture-Test und Grafikgerät | 155 bestanden, 0 Fehler, 3 bekannte Skips | `hud100-playmode-final-20260904.xml` |

Die jeweils gleichnamigen Logdateien liegen unter `Logs/`.
Die finalen EditMode-/PlayMode-Logs enthalten keine C#-Compilerfehler.

Der Grafikfehler war `RenderTexture.Create failed` in
`BuildingSystemIntegrationTests.BuildingSurvivesSceneChangeWithTransformAndLevel`:
Der bestehende Test ruft `Camera.Render()` auf und benötigt ein Grafikgerät.
Daraufhin wurde gezielt die Laufumgebung geändert, kein Fehler ignoriert und
kein Gameplay-Code angepasst. Alle grafikbedingt übersprungenen Prüfungen
liefen anschließend mit Grafikgerät.

Die fünf neuen Auditfehler waren eine unvollständige Kontrollinventur:
`InteractionButton` implementiert Pointer-Interfaces, erbt aber nicht von
`Selectable`. Der Audit erfasst nun beide Arten. Alle sechs neuen Tests sind grün.

Die drei verbleibenden vorhandenen Skips im Grafiklauf sind ausdrücklich:

- `ItemAufnahmeDiagnoseTests.Aufnahmepfad_EchtesTiming`: protokollierende Diagnose.
- `ItemAufnahmeDiagnoseTests.Aufnahmepfad_Stationen`: protokollierende Diagnose.
- `KreaturenInWeltCaptureTests.KreaturenInWelt_Captures`: separater Capture-Auftrag.

## Relevantes vorhandenes Testnetz

- HUD: `CombatHudPrefabTests`, `CombatHudWeaponIconTests`,
  `CombatHudStatusPresenterTests`, `CombatHudBarFillTests`, `CombatHudXpTests`,
  `CombatHudIntegrationTests`.
- Combat/Waffen: `CombatGeneralizationTests`, `WeaponSwitchTests`,
  `WeaponV02Tests`, `CombatFeedbackPoolingPlayModeTests`.
- Eidra: `EidraAbilityTests`, `EidraCaptureTests`,
  `Fixrunde031EidraTests`, `Fixrunde032EidraTests`, `WildEidraIntegrationTests`.
- Interaktion: `InteractionSystemTests`, `InteractionIntegrationTests`,
  `WorldChestInteractionModeTests`.
- Karte: `MinimapHudPrefabTests`, `MinimapPresenterTests`,
  `MinimapProjectionTests`, `MinimapRegistryTests`, `MinimapMarkerTests`.
- Architektur/Komposition und Boss-Tests bleiben Teil der vollständigen Läufe.

## Datei- und Systemmatrix

Dies ist die geplante Änderungsrichtung, keine Behauptung bereits erledigter Pakete.

| System/Dateigruppe | Richtung | Aktueller Schritt |
| --- | --- | --- |
| `CombatHudBaselineAudit` und zwei neue Testklassen | neu | implementiert, Verifikation siehe oben |
| `CombatHUD.prefab`, `CombatHUD`, bestehende Presenter | erweitert | bisher unverändert |
| `SafeAreaPanel`, `MinimapPresenter`, `PlayerInputReader` | wiederverwendet | unverändert |
| `CombatTargetGeometry`, Waffen-/Eidra-Daten | wiederverwendet | unverändert |
| Persistenter Zielservice, Snapshot, Bewertung | neu | Targeting-Schritt implementiert |
| `SceneCombatTargetQuery`, `PlayerCombatController` | erweitert/Adapter | produktiv persistente Query; Paket 3 mit einmaliger begrenzter Drehhilfe |
| `EidraTeamController`, `PlayerPrefabBindings` | erweitert | gemeinsam injizierte Instanz; bestehende isolierte Testaufbauten kompatibel |
| Combat Target Ring, normales Target-HUD | neu | Ring aus Paket 4 implementiert; Target-HUD aus Paket 5 offen |
| Boss-HUD, HUD-Zustände, Combat Cluster | erweitert | Paket 5–8 offen |
| Context Action, Feedback-Pools, Assets, Animationen | gezielt erweitert | spätere Pakete offen |
| Saveformat, Release-Builds, Android/iOS-Konfiguration | unverändert | nicht Teil dieses Schritts |

## Vor dem visuellen Umbau aufzulösende Spezifikationspunkte

1. M12 enthält widersprüchliche Größen: 144/100/72 als festes Raster,
   daneben 148/104 und mindestens 88 für Wechselaktionen. Der Iststand ist
   kein stillschweigender neuer Designentscheid; Paket 7 muss ein einziges
   konsistentes Größenraster festlegen und die Tests passend migrieren.
2. Auftrag verlangt das aktive Eidra im Cluster; M12 und heutiges Prefab
   verwenden den Wechsel zum inaktiven Eidra. Aktivportrait und Wechselziel
   müssen vor der Layoutmigration eindeutig getrennt werden.
3. Die Minimap-Tests vergleichen zum Teil lokale Kanten aus unterschiedlichen
   Elternkoordinaten. Beim visuellen Umbau sind diese durch Vergleiche in
   gemeinsamen Koordinaten zu ergänzen. Der Eidra-Wechsel muss nicht erst in
   den Cluster zurückgeführt werden: Er ist dort bereits enthalten.

## Targeting-Schritt: gemeinsames Kampfziel

### Implementierung

- `Core/CombatTargetSelection.cs`: Unity-unabhängige Bewertung, stabile
  Gleichstandsauflösung über Ziel-ID, Hysterese und Verlust-Gnadenzeit.
- `Combat/PersistentCombatTargetQuery.cs`: pro Spieler eine Instanz, von
  `PlayerPrefabBindings` an Combat und Eidra injiziert. Keine neue
  Szenenkomponente, kein Laufzeit-Prefabaufbau und keine Assetmigration.
- `Combat/CombatTargetSnapshot.cs`: Identität/Transform, Zielpunkt, Name,
  normal/Elite/Boss, HP/Stagger, Zustand, lebend/aktiv, Oberflächenabstand,
  Auswahlzeit und letzte Bestätigung. `Changed` meldet nur tatsächliche
  sichtbare Änderungen, nicht jeden unveränderten Bestätigungstakt.
- Gegner-Wrapper haben Vorrang vor ihrer `Damageable`-Komponente. Damit
  bleiben Stagger und Gegnerzustand erhalten; ein zurückkehrender Gegner
  kann nicht über den zweiten Registry-Eintrag wieder ausgewählt werden.
- Auswahl regulär mit 10 Hz, harte Invalidierung zusätzlich pro Spieler-Update
  und beim Lesen des Snapshots. Eine ausgelöste Aktion prüft frisch.
- Sicht-/Hindernisprüfung mit wiederverwendetem Raycast-Puffer. Keine neue
  Pfadsuche: Sichtlinie ist hier ein Angreifbarkeitsfilter, keine Zusicherung
  eines begehbaren NavMesh-Pfads um Hindernisse.
- `CombatTargetFacing` kapselt die Assistenz: identischer waffenspezifischer
  Winkel und Suchradius, nur bei ausgelöstem Angriff. Seit Paket 3 dreht sie
  proportional und begrenzt statt hart vollständig zum Ziel.
  Keine automatische Spielerbewegung oder Spielerattacke, keine freie Verfolgung.
- Letzter assistierter Angriff bzw. erfolgreicher Treffer beeinflusst die
  Bewertung kurzzeitig. Schadenswerte, Kombofenster und Dodge bleiben unverändert.
  Die bestehenden Hitboxen können weiterhin mehrere Gegner treffen; der
  gemeinsame Fokus ist kein neuer exklusiver Schadensfilter.
- Eidra wählen pro neuer Aktion aus dem gemeinsamen Service. Ihre vorhandene
  Reichweitenprüfung (einschließlich bisherigem Mittelpunktmaß) bleibt bestehen;
  ein entsprechender Filter darf das Hauptziel ablehnen und ein gültiges
  Alternativziel wählen, ohne den gemeinsamen Lock-on umzuschreiben.
  Spezielle Bedingungen wie ein freier Schattenschritt-Zielpunkt werden weiterhin
  unmittelbar vor Ausführung durch die vorhandene Fähigkeitslogik validiert.
- Laufende Casts werden nicht durch neue Scans umgezielt. Ein Begleitschlag
  hält sein eigenes Aktionsziel bis zur Trefferprüfung. Die Begleitautomatik
  beginnt nicht allein wegen der Zielerfassung einen Angriff auf ruhende Gegner.
- Tod, Deaktivierung und Übergangsabbruch leeren den Zielzustand. Die
  Vollständigkeitsprüfung verlangt in allen fünf Zonen eine gemeinsame Instanz.

### Vorläufige, ausdrücklich benannte Einstellungen

Erfassung 12 m, harte Freigabe 14 m, Gnadenzeit 0,25 s,
Wechselvorteil 0,65 Bewertungspunkte, letzte Aktion relevant für 1,2 s.
Gewichte: Entfernung 3, Richtung 2, innerhalb aktiver Waffenreichweite +4,
Boss +0,35, letzte Aktion +0,8, gestaggertes Ziel +0,3.
Die Werte stehen in `CombatTargetSelectionSettings`; kein neues Balancing-Asset
oder Savefeld. Feinabstimmung im echten Kampf bleibt Teil der weiteren Abnahme.
Gleichstände sind innerhalb einer laufenden Sitzung unabhängig von der
Registry-Reihenfolge; Unity-Instanz-IDs sind ausdrücklich keine Save-IDs.

### Prüfungen und Befunde

- Erster voller EditMode-Lauf: 1381/1386 bestanden. Fünf neue Adaptertests
  hatten ihre Dummys nicht registriert: normale MonoBehaviour-OnEnable-Aufrufe
  laufen im EditMode nicht. Testaufbau korrigiert, keine Produktionsfilter gelockert.
- Danach 24/24 fokussierte Kern-/Adaptertests bestanden. Darunter
  Hysterese, Gnadenzeit, Tod, Deaktivierung, Zerstörung, Eigenhierarchie,
  Wand/Trigger, große Trefferfläche, Ereignisruhe, HP und Reichweiten-Fallback.
- Erweiterter voller EditMode-Lauf: 1389/1389 bestanden, einschließlich
  Wrapper-/Return-Filter und fähigkeitsspezifischer Ablehnung des Hauptziels.
- Fokussierter PlayMode-Vorlauf: 6/7 bestanden; der neue Fähigkeitstest
  kontrollierte seinen lokalen Aktionskontext erst nach Ende der Aktion.
  Er prüft nun das gewählte Ziel beim Auslösen und anschließend genau einen
  Cooldownstart. Der gemeinsame Snapshot, nicht der vergangene lokale Cast,
  ist der dauerhafte Zielzustand. Fehler-/Erfolgsläufe bleiben als XML erhalten.
- Warme Messung: 48 Collider-Dummys, 200 erzwungene Scans im Windows-Editor,
  ohne Grafikgerät: im fokussierten Lauf 33,0262 ms gesamt (ca. 0,165 ms/Scan),
  im letzten Voll-Lauf 54,7535 ms (ca. 0,274 ms/Scan), beide **0 Byte Managed-GC**.
  Vorlauf mit 20 Scans, unveränderte Registry/Geometrie, keine Ereignisempfänger.
  Kein Gerätebenchmark und kein Ersatz für ein vollständiges Spiel-Profiler-Capture.

Finaler EditMode-Lauf: **1393/1393 bestanden** (30 zusätzliche EditMode-Fälle
gegenüber Paket 1). Beleg: `hud100-targeting-editmode-final2.xml`.
Finaler PlayMode-Grafiklauf: **156 bestanden, 0 Fehler, 3 unveränderte bekannte
Skips** (159 Fälle). Beleg: `hud100-targeting-playmode-final.xml`.
Die neue Sechs-Fähigkeiten-Prüfung und die gemeinsame Verdrahtung in allen
fünf Zonen sind im Voll-Lauf bestanden. Beide finalen Logs sind frei von
C#-Compilerfehlern. Es wurde kein Release-/Gerätebuild erstellt.
Artefakte: `TestResults-Archiv/hud100-targeting-*`, dazu gleichnamige `Logs/*.log`.

## Paket 3: Auto-Facing

### Implementierung

- `Core/CombatTargetFacingRules.cs` enthält die Unity-unabhängige Entscheidung
  für Korrekturstärke und maximale Einzeldrehung. Die Regeln sind ausdrücklich
  nach `WeaponFamily` benannt und erfordern keine Migration der elf
  Waffenvarianten oder des Saveformats.
- Hammer: 70 Prozent der Abweichung, höchstens 32 Grad; Dolche: 85 Prozent,
  höchstens 28 Grad; Speer: 65 Prozent, höchstens 22 Grad. Der bestehende
  `AttackAssistAngle` bleibt davor der harte Waffenfamilien-Kegel
  (Hammer 120, Dolche 75, Speer 50 Grad Gesamtwinkel).
- `CombatTargetFacing.Apply` wird weiterhin ausschließlich am Anfang von
  `PlayerCombatController.AttackRoutine` aufgerufen. Es gibt keinen Aufruf aus
  freier Bewegung, Ziel-Update, Eidra-Logik oder Dodge.
- Die Drehung erfolgt genau einmal über `Quaternion.RotateTowards`. Das System
  verschiebt den Spieler nicht, startet keinen Angriff und verfolgt das Ziel
  nicht weiter. Ein Ziel hinter dem Spieler bleibt außerhalb des Kegels.
- Dodge liest bei vorhandener Eingabe weiterhin unmittelbar den kamerabezogenen
  Eingabevektor im `PlayerMotor`; Auto-Facing verändert weder diesen Vektor noch
  `LastValidWorldMoveDirection`. Schaden, Hitboxen, Root-/Combozeiten und
  Weapon-Switch-Buffer wurden nicht geändert.

### Prüfungen

- Fokussierter EditMode-Lauf: 33/33 bestanden. Geprüft sind alle drei Profile,
  proportionale und gedeckelte Drehung, ungültige Werte, fehlendes Ziel,
  leichter Versatz, beide Seiten der Winkelgrenze, Ziel hinter dem Spieler,
  unveränderte Position sowie alle zehn Schritte der produktiven Hammer-,
  Dolch- und Speerkombos.
- Der erste Integrationslauf hatte drei fehlerhafte Sollwerte im Test: Er
  verglich den Transformwinkel statt des produktiv verwendeten nächsten Punkts
  der Box-Collider-Oberfläche. Die Tests berechnen nun dieselbe Körpergeometrie;
  Produktionscode und Assistenzgrenze wurden dafür nicht gelockert.
- Fokussierter PlayMode-Lauf: 1/1 bestanden. Ein echter `PlayerMotor` dodgt
  unmittelbar vor und nach einer Zielkorrektur weiter in die entgegengesetzte
  Eingaberichtung.
- Vollständiger Abschluss: **1.408/1.408 EditMode** sowie **157 PlayMode,
  0 Fehler, 3 unveränderte bekannte Skips** (160 Fälle). Belege:
  `TestResults-Archiv/hud100-autofacing-editmode-final.xml` und
  `TestResults-Archiv/hud100-autofacing-playmode-final.xml`; gleichnamige Logs
  ohne C#-Compilerfehler.
- Das HUD-Prefab blieb bei SHA-256
  `b6d62b6fee591d727e84094c64e076e5506b1711c43cf5609a0a3009b1f5961c`.
  Kein Release-/Gerätebuild wurde erstellt. Die Werte sind belastbare
  Startwerte, aber noch nicht auf einem Touchgerät feinabgenommen.

## Paket 4: Combat Target Ring

### Implementierung und Gestaltung

- `CombatTargetIndicator` ist als genau eine aktive Komponente mit zwei anfangs
  deaktivierten `LineRenderer`-Kindern im produktiven Player-Prefab autoriert.
  Es werden beim Zielwechsel weder GameObjects noch Materialien erzeugt.
- Primärring und sechzehnteilige Eidren-Akzentkontur verwenden dasselbe
  `M_CombatTargetRing`-Material. Der eigene Shader `Eidren/CombatTargetRing`
  ist texturfrei, transparent, schreibt keine Tiefe und verwendet `ZTest Always`.
  Dadurch bleibt die dünne Markierung hinter Vegetation lesbar, ohne den Gegner
  mit einer gefüllten Fläche zu verdecken.
- Normal: schmaler rot-oranger Ring ohne zweite Kontur. Elite: markanterer
  Bernstein-Akzent bei gleicher Informationsdichte. Boss: größere rote Form
  mit stärkerer, langsam rotierender Akzentkontur. Farben und Maße liegen in
  `CombatTargetRingStyles`, nicht als Presenter-Sonderfälle verstreut.
- Der Ring ermittelt sichtbare Renderer-/Collider-Grenzen des Ziels und tastet
  darunter den Boden ab. Eigene Spieler- und Zielcollider werden ausgefiltert;
  ein Abstand von 0,075 m verhindert Z-Fighting. Position, Normale und Radius
  werden geglättet, ein echter Zielwechsel setzt dieselbe Instanz diskret neu an.
- Zielverlust blendet in 0,18 s aus, Zielaufnahme in 0,11 s ein. Deaktivierung
  und Szenenwechsel lösen die Eventbindung und Interaktionsunterordnung wieder.
- `ResourceTargetIndicator` behält sein gültiges Interaktionsziel, deaktiviert
  aber seine grüne Ringgrafik solange der Combat-Ring sichtbar oder im
  Einblenden ist. Damit konkurrieren beide Markierungen nicht optisch.

### Prüfungen und Sichtbefund

- Fokussiert: **8/8 EditMode** und **3/3 PlayMode**. Geprüft sind Normal-,
  Area-Elite- und Bossklassifikation, Stilhierarchie, genau eine Prefabinstanz,
  gemeinsames Material, Shaderzustand, bewegtes Ziel, geneigter/erhöhter Boden,
  schneller Wechsel, Zielverlust-Fade und ein gleichzeitiges Interaktionsziel.
- `CompositionCompletenessTests` bestätigt den Indicator in HomeBase,
  Greenwood, Quarry, Marsh und EmberRuins. Der Produktionsszenen-Test bestätigt
  Garon mit Bossvariante von einer freien Arena-Sichtlinie.
- Die neue Windows-Render-Serie liegt unter
  `TestResults-Archiv/HUD100/captures-20260904-184949-912/`. Der normale
  Kampfbeleg zeigt den roten Ring klar am Wildling. Im vorhandenen Garon-Capture
  steht der künstlich teleportierte Spieler auf der durch `EidraForgeEntrance`-
  Collider blockierten Portalseite; folgerichtig liefert das persistente
  Targeting dort kein Ziel und der Ring bleibt aus. Mehrere freie Seiten wurden
  separat diagnostiziert, der dauerhafte Garon-Test verwendet eine davon.
- Der erste vollständige PlayMode-Lauf hatte 158 bestandene Fälle und einen
  Fehler im alten Interaktionsring-Vertrag: Er verlangte die grüne Markierung
  auch bei aktivem Combat-Ring. Der Test prüft nun weiterhin Zielerhalt und
  Eingabepfade, akzeptiert aber die spezifizierte visuelle Unterordnung.
- Abschlusslauf: **1.413/1.413 EditMode** sowie **159 PlayMode, 0 Fehler,
  3 unveränderte bekannte Skips** (162 Fälle). Belege:
  `TestResults-Archiv/hud100-targetring-editmode-final.xml` und
  `TestResults-Archiv/hud100-targetring-playmode-final2.xml`; gleichnamige Logs
  enthalten keine C#-Compilerfehler.
- Player-Prefab vor Paket 4:
  `c24000b9f8746cb4bae0800df010302889b63ea8da9191817af76843a47e9bfa`,
  danach: `e7a5d9258a70c88a83d79f8f4a008a9cd8e80014f987de36a5980d6af79d2fd3`.
  Das Combat-HUD-Prefab blieb unverändert bei
  `b6d62b6fee591d727e84094c64e076e5506b1711c43cf5609a0a3009b1f5961c`.
  Kein Release- oder Gerätebuild wurde erstellt.

### Noch offen

- Normales Target-HUD, Boss-HUD und neue HUD-Zustandssteuerung.
- Rotationsprofile in echten bewegten Mehrgegner-Szenarien und auf Touchgerät
  feinabnehmen; funktional und regressiv ist Paket 3 abgeschlossen.
- Echtspiel-Profiler-Beleg und native Bildabnahme
  mit sichtbarer Zielanzeige. Kein Anspruch auf vollständige Abnahme von Paket 2.
- Responsive Layoutmigration, aktives Eidra-Portrait, Feedback/Assets/Animationen.

Sicherungs-/Rücknahmehinweise: `Backups/2026-09-04-HUD100/LIESMICH.md`,
`Backups/2026-09-04-HUD100-Targeting/LIESMICH.md` und
`Backups/2026-09-04-HUD100-AutoFacing/LIESMICH.md` sowie
`Backups/2026-09-04-HUD100-TargetRing/LIESMICH.md`.
