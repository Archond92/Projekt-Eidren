# Featuresammlung v0.4

**Status:** Sammlung läuft.

**Nutzerentscheid 17.08.2026: N04-001 und N04-002 wandern nach v0.3.1** und
warten nicht auf v0.4. Die Einträge bleiben aus Gründen der Nachvollziehbarkeit
hier stehen; ihr Umsetzungsstand wird je Eintrag geführt. Weitere N04-Einträge
sammeln weiterhin für v0.4.

Der Nummernkreis **N04-xxx** (Neuerungen für v0.4) ist bewusst getrennt von den
Fixsammlungen `FIXSAMMLUNG_V0.2.1.md` (F-001 bis F-014) und
`FIXSAMMLUNG_V0.3.1.md` (F31-xxx) sowie der abgeschlossenen
`WUNSCHSAMMLUNG_20260814.md` (W-001 bis W-010, T-001). Anders als jene beiden
Sammlungen enthält diese Datei **keine Fehler, sondern neue Funktionen** — der
Unterschied zählt, weil ein Fehler eine Ursache hat und ein Feature eine
Entscheidung braucht.

**Basis:** v0.3.0-dev (Commit `370396f`, Tag `v0.3.0-dev`), Branch
`codex/v0.3-release`. Alle Fundstellen unten sind an diesem Stand geprüft.

---

## N04-001 — Minimap oben rechts

**Bereich:** HUD, Weltdarstellung, Zonendaten

**Wunsch (17.08.2026):** Eine Minimap oben rechts, die einen kleinen Ausschnitt
der Karte zeigt — mit den Ressourcen und den Gegnern. Gegner als rote Punkte,
Bosse mit einem Boss-Symbol.

**Vorab entschieden (17.08.2026):** **Ausschnitt um die Figur**, die Karte
scrollt mit. Nicht die ganze Zone. Begründung des Nutzers folgend dem Wortlaut
„kleiner Ausschnitt"; sachlich gestützt dadurch, dass die Zonenfläche rund
sechsfach überdimensioniert ist — eine Gesamtansicht wäre größtenteils leer und
die Punkte darauf zu klein zum Unterscheiden.

### Befund (17.08.2026, Codestand v0.3.0-dev)

**Eine Minimap existiert nicht.** Die einzige Karte im Spiel ist die
**Weltkarte** (`WorldMapCanvasView.cs`, `WorldMapConnectionView.cs`): eine
knotenbasierte Reisekarte mit Gebietsknoten und Verbindungen. Sie kennt keinen
Zonengrundriss und keine Positionen innerhalb einer Zone. Als Vorlage taugt sie
nur für den UI-Aufbau, nicht für die Daten.

**Die Ecke oben rechts ist belegt.** Aus `Assets/_Game/Resources/UI/CombatHUD.prefab`
ausgelesen (Referenzauflösung 1920 × 1080, `ScreenMatchMode` 0, Match 1 —
also höhenbezogen skaliert):

| Element | Ankerpunkt | Position | Größe |
|---|---|---|---|
| `InventoryEdgeAction` | oben rechts (1/1) | −112 / −22 | 72 × 72 |
| `EidraSwitch` | oben rechts (1/1) | −155 / −44 | 72 × 72 |
| `EidraSkill2` | oben rechts (1/1) | −100 / −150 | 100 × 100 |
| `AttackAction` | oben rechts (1/1) | −140 / −300 | 144 × 144 |

Positionen sind Mittelpunkte im Ankerraum des Elternobjekts (`SafeAreaPanel`).
`InventoryEdgeAction` und `EidraSwitch` liegen damit zusammen in den obersten
rund **80 Pixeln** und den rechten rund **190 Pixeln** — genau dort, wo die
Minimap hin soll. Eine Karte von etwa 260 × 260 in der Ecke überdeckt beide
vollständig. **Das ist eine Layoutentscheidung, keine Kleinigkeit** — sie muss
vor der Umsetzung fallen, weil danach Prefab und Tests daran hängen.

**Die Datenquellen fehlen, aber das Muster dafür steht.** Weder Gegner noch
Ressourcenknoten sind irgendwo registriert; die einzigen Stellen, die sie
sammeln, sind Abnahme-Runner über `FindObjectsByType`
(`Auftrag5CaptureRunner.cs:154–156`, `G001VisualAbnahmeRunner.cs:304`). Für
eine Minimap ist dieser Weg **verboten** — §8 schließt `Find*` in pro Frame
durchlaufenen Schleifen aus. Das passende Muster liegt schon im Projekt:
`CombatTargetRegistry` (`Combat/CombatTargeting.cs:68–95`) mit
`Register`/`Unregister` und einem `RuntimeInitializeOnLoadMethod`-Reset, der
§2 erfüllt.

**Was an Zustand bereits vorhanden ist:**

- `ResourceNode.IsExhausted` / `IsMined` (`Interaction/ResourceNode.cs:70–72`) —
  abgebaute Knoten lassen sich also ohne neues Feld ausblenden oder ausgrauen.
- **Bosse sind am Typ erkennbar:** `BossController : EnemyControllerBase`
  (`AI/BossController.cs:14`). Dazu `ZoneDefinition.IsBossZone` und
  `BossAreaController`. Für das Boss-Symbol braucht es **kein neues Datenfeld**,
  nur eine Typprüfung beim Registrieren.
- `EnemyNavigationData.DetectionRange` — die Reichweite, ab der ein Gegner den
  Spieler bemerkt. Relevant für die Frage unten, ob die Karte Gegner zeigt, die
  den Spieler noch nicht gesehen haben.

**Die Kartenausrichtung ist eine echte Entscheidung.** Die Kamera steht
orthografisch mit fester Drehung 52°/45° (`IsometricCamera.cs:41`, gesetzt in
Zeile 175). Damit fallen Weltachsen und Bildschirmachsen um 45° auseinander:

- **Nordorientiert** (Weltachsen): stabil, die Karte dreht sich nie — aber
  „rechts auf der Karte" ist nicht „rechts auf dem Bildschirm".
- **Kameraorientiert** (um 45° gedreht): Richtung auf der Karte entspricht der
  Laufrichtung auf dem Schirm. Weil die Kamera **nie** dreht, ist das eine
  einmalige feste Drehung, kein laufender Rechenaufwand.

Empfehlung: kameraorientiert. Der Vorteil — Kartenrichtung gleich Laufrichtung —
kostet hier nichts, weil die Drehung konstant ist.

**Zur Markermenge:** Ein Außengebiet trägt laut Bibel M1.7 **72
Wirtschaftsknoten** auf 76 × 76 Spielfläche, dazu Nebenknoten (Beeren) und die
Gegner der Zone. Ein Ausschnitt zeigt davon nur einen Teil, aber die Registrierung
hält alle. Marker müssen deshalb **gepoolt** werden; das Vorbild dafür ist
`BuildGridMarkPool` (`Presentation/BuildGridMarkPool.cs`).

**Bibelbindungen, die den Einbauweg vorgeben:**

- **§16 (UI wird autoriert, nicht erzeugt):** Die Minimap gehört in
  `CombatHUD.prefab`, angelegt über einen Editor-Builder — Vorbilder sind
  `WeaponIconHudBuilder.cs` und `FixCollectionHudBuilder.cs`. Kein Aufbau zur
  Laufzeit. `CombatHudPrefabTests` prüft die Vollständigkeit des Prefabs und
  muss mitwachsen; `CombatHUD.ValidateReferences` wirft bereits bei
  unvollständigem Prefab.
- **§8 (`Update()` ist heilig):** keine Allokation, kein `GetComponent`, kein
  LINQ im Kartentakt.
- **§7 (Klassenbudget):** eigener `MinimapPresenter` neben den bestehenden
  Presentern, nicht in `CombatHUD` hineingeschrieben.
- `SafeAreaPanel` ist im HUD vorhanden — die Minimap muss innerhalb der
  sicheren Fläche liegen, sonst verschwindet sie auf Handys unter der Kerbe.

**Nebenbefund zur Bibel:** Der Beleg unter §16 ist überholt. Er hält fest, dass
`CombatHUD` sich „mit über 1300 Zeilen und sechs `Build*`-Methoden selbst
zusammenbaut". Das stimmt nicht mehr: `CombatHUD.cs` ist auf 81 Zeilen
geschrumpft, das Prefab liegt unter `Assets/_Game/Resources/UI/CombatHUD.prefab`,
und `ValidateReferences` verbietet Laufzeit-Notbehelfe ausdrücklich. Die Regel
gilt unverändert — nur ihr Beleg gehört nachgeführt.

**Erledigt am 20.08.2026.** §16 trägt den Nachtrag jetzt selbst; die Zahl ist
beim Nachziehen nachgemessen worden und lautet **98 Zeilen**, nicht 81 —
zwischen der Notiz oben und heute ist das HUD um die Minimap-Anbindung
gewachsen.

### Stoßrichtung (Vorschlag, noch nicht beauftragt)

1. **Zwei schlanke Registrierungen** nach dem Muster von `CombatTargetRegistry`:
   eine für `EnemyControllerBase` (mit Bossunterscheidung über den Typ), eine für
   `ResourceNode`. An-/Abmeldung in `OnEnable`/`OnDisable`, statischer Reset nach
   §2.
2. **`MinimapPresenter`** im HUD, gebunden wie die übrigen Presenter über
   `CombatHUD.Initialize`. Aktualisierung **getaktet** (Vorschlag 10 Hz, wie die
   `displayedTenth`-Prüfung, die §8 ausdrücklich lobt), nicht pro Frame.
3. **Projektion** Welt-XZ → Kartenfläche als reine Rechnung: Position minus
   Spielerposition, feste 45°-Drehung, Maßstab, abschneiden am Kartenrand. Kein
   Raycast, keine zweite Kamera, keine Allokation.
4. **Untergrund: getöntes Feld statt gerenderter Karte.** Eine zweite
   Orthokamera auf eine RenderTexture würde einen vollständigen zweiten
   Renderdurchlauf pro Frame kosten — für Punkte auf einer Fläche ist das nicht
   zu rechtfertigen. Falls später ein echter Kartenuntergrund gewünscht wird, ist
   der richtige Weg ein **einmal je Zone gebackenes Bild**, kein Live-Rendern.
5. **Platzierung:** Vorschlag — Minimap in die Ecke, `InventoryEdgeAction` und
   `EidraSwitch` darunter rücken. Das braucht einen Bildabgleich, keinen
   Testwert.

### Zu klären vor der Umsetzung

- **Radius des Ausschnitts in Metern.** Ohne Zahl kein Maßstab. Anhaltspunkt:
  Spielfläche 76 × 76; ein Radius von 20 m ergibt einen Ausschnitt von 40 × 40 m,
  also gut die halbe Kantenlänge einer Zone.
- **Ressourcen einfarbig oder nach Material eingefärbt?** Vier Materialien je
  Gebiet (M1.7) — Farbe nach Material wäre ohne Zusatzaufwand möglich.
- **Abgebaute Knoten ausblenden oder ausgrauen?** Ausgrauen zeigt, wo man schon
  war; Ausblenden hält die Karte sauber.
- **Zeigt die Karte Gegner, die den Spieler noch nicht bemerkt haben?** Das ist
  ein Spähvorteil und verändert das Spielgefühl in gefährlichen Gebieten
  (Glutruinen). Alternative: nur Gegner innerhalb ihrer `DetectionRange`.
- **Boss-Symbol:** eigene Grafik (dann Grafikauftrag) oder ein größerer,
  andersfarbiger Marker mit Rahmen. Ohne Auftrag ist nur die zweite Variante
  sofort machbar.
- **Gilt die Minimap auch in Eidra-Schmiede und Verlies?** Dort ist die
  Raumstruktur der eigentliche Inhalt — Punkte ohne Wände sind dort womöglich
  irreführend.

**Berührt:** N04-002 — die Automatik läuft sichtbar auf derselben Karte; beide
Einträge brauchen dieselbe Gegner- und Knotenregistrierung. Wer N04-001 zuerst
baut, hat die Datenbasis für N04-002 bereits stehen.

### Umsetzung (17.08.2026, für v0.3.1)

**Entscheidungen des Nutzers vor der Umsetzung:**

| Frage | Entscheidung |
|---|---|
| Platzierung | Karte **unter** die beiden Eckknöpfe; nichts Gewohntes verschiebt sich |
| Boss-Symbol | eigene Grafik (kein Ring aus Bordmitteln) |
| Sichtbare Gegner | **alle** im Umkreis, auch unbemerkte |
| Kisten | erscheinen, sobald sie **einmal im Bild** waren, und bleiben dann |
| Kistenarten | Welttruhen, Schmiedetruhen, eigene Lagerkisten, Beutesäcke |

**Gebaut, je Schicht:**

| Datei | Inhalt |
|---|---|
| `Core/MinimapProjection.cs` | Welt-XZ → Kartenpunkt, 45°-Drehung, Radiusprüfung |
| `Core/MinimapRegistry.cs` | `IMinimapMarker`, `IMinimapDiscoverable`, Registrierung mit Reset (§2) |
| `Interaction/ResourceNode.Minimap.cs` | Knoten als Marker, abgebaut = ausgegraut |
| `AI/EnemyControllerBase.Minimap.cs` | Gegner als Marker, `MinimapKind` überschreibbar |
| `AI/BossController.Minimap.cs` | Boss als eigene Markerart |
| vier Kistenklassen | Kistenmarker mit Entdeckungsmerker |
| `UI/MinimapPresenter.cs` | Zeichnen, 10-Hz-Takt, Sichtprüfung der Kisten |
| `UI/MinimapMarkerPool.cs` | Wiederverwendung statt Erzeugung im Takt (§8) |
| `UI/MinimapStyle.cs` | Farben und Größen an einer Stelle |
| `Editor/MinimapHudBuilder.cs` | Einbau ins HUD-Prefab (§16), idempotent |
| `Tools/Make-MinimapSymbols.py` | erzeugt die drei Symbole deterministisch |

**Vorschlagswerte, noch nicht am Bild abgeglichen:** Kartenfeld 240 × 240 bei
Referenzauflösung 1920 × 1080, Kartenradius 110 Punkte, Weltradius 22 m
(Ausschnitt 44 × 44 m), Symbolgrößen Boss 32, Kiste 26, Gegner 14, Knoten 12,
Figur 16. Alle Werte liegen als Felder im Prefab und werden nach dem ersten
Bildabgleich gemeinsam nachgezogen.

**Nachweis:** 27 neue Tests in fünf Klassen, alle grün. Volle Suiten nach dem
Einbau: EditMode 1133/1133, PlayMode 120 grün + 4 übersprungen, 0 Fehlschläge.

**Offen:** Der Bildabgleich im laufenden Spiel — Größen, Radius und Farben sind
Optikentscheidungen und stehen bewusst als Vorschlag. Ebenfalls offen: ob die
Karte in Verlies und Schmiede sinnvoll ist (dort ist die Raumstruktur der
eigentliche Inhalt) und ob geleerte Kisten ausgegraut werden sollen.

**Status: umgesetzt am 17.08.2026 für v0.3.1, Optik noch nicht abgeglichen.**

---

## N04-002 — Automatisches Spielen

**Bereich:** Eingabe, Spielfluss, HUD, Balance

**Wunsch (17.08.2026):** Ein Knopf, den man anklicken kann. Die Figur läuft
daraufhin die ganze Karte ab, baut alles ab und greift jeden Gegner an. Sie
weicht nicht aus.

**Vorab entschieden (17.08.2026):**

- **Spielerfunktion im fertigen Spiel**, kein reines Entwicklerwerkzeug.
- **Abbruch bei Spielereingabe** und **wenn die Zone leergeräumt ist**.
- **Kein Abbruch bei niedrigem Leben** — ausdrücklich nicht gewählt. Die
  Automatik schützt sich nicht selbst; die Figur kann sterben.

### Befund (17.08.2026, Codestand v0.3.0-dev)

**Der Anschlusspunkt ist bereits vollständig vorhanden — das ist der wichtigste
Befund.** `PlayerInputReader` (`Input/PlayerInputReader.BuildingPointer.cs`)
bietet eine geschlossene virtuelle Eingabeschicht:

| Methode | Zeile | Zweck |
|---|---|---|
| `SetVirtualMove(Vector2)` | 299 | Laufrichtung |
| `SetVirtualInteract(bool)` | 308 | Abbauen (halten) |
| `PressAttack()` | 318 | Angriff |
| `PressDodge()` | 323 | Ausweichen (hier bewusst ungenutzt) |
| `PressConsumable(int)` | 343 | Trank / Nahrung |
| `PressSwitchWeapon()` | 328 | Waffenwechsel |

Der Touch-HUD steuert das Spiel **ausschließlich** über diese Schnittstelle —
`attack.Button.onClick.AddListener(_input.PressAttack)`
(`CombatHudActionPresenter.cs:156`). `PlayerMotor` liest ausschließlich
`_input.Move` (`Player/PlayerMotor.cs:104`).

**Folge:** Die Automatik kann als „virtueller Spieler" **über** der
Eingabeschicht sitzen. `PlayerMotor`, `PlayerCombatController` und
`InteractionController` bleiben unangetastet. Das ist die saubere Lösung nach §1
(Schichten) — und es bedeutet, dass die Automatik kein einziges Gameplaysystem
umbaut.

**Wegfindung ist vorhanden, aber nur zur Hälfte belegt.** Jede Zone hat eine
`NavMeshSurface` (`ZoneController.cs:77`, `:118`); `NavMesh.SamplePosition` wird
bereits benutzt (`ZoneController.cs:265`), `NavMesh.CalculatePath` steht damit
zur Verfügung. **Der Aufwandstreiber ist aber nicht die Wegfindung, sondern
§26.** Die Bibel sagt dort wörtlich, dass die Außenzonen bisher nur eine
20 Einheiten lange Strecke durch die Kartenmitte prüfen — „das ist eine
Durchlässigkeitsprobe, kein Erreichbarkeitsbeweis für Ziele am Rand. Sobald eine
Außenzone Pflichtziele bekommt, gilt für sie dieselbe Anforderung wie für die
Schmiede."

**Genau das tut diese Funktion: Sie macht jeden einzelnen Knoten zum
Pflichtziel.** Damit greift §26 in voller Härte — ein vollständiger Wegtest je
Außenzone, vom Startpunkt bis zum entferntesten Knoten. Das ist der eigentliche
Umfang dieses Eintrags, nicht die Automatik selbst.

**Die gewählte Abbruchbedingung „Zone leergeräumt" ist am heutigen Stand
unerreichbar — sie wird es erst mit erweitertem Rucksack und T1-Werkzeug.** Aus
den Zahlen der Bibel:

| Größe | Wert | Quelle |
|---|---|---|
| Wirtschaftsknoten je Gebiet | 72 | M1.7 |
| Ertrag je Knoten (T0-Werkzeug) | 2 Einheiten | M1.7 |
| **Ertrag eines vollständigen Laufs** | **144 Einheiten** | daraus |
| Rucksackplätze | 16 | M5 |
| Stapelgröße | 10 | M5 |
| **Fassungsvermögen bei perfekter Stapelung** | **160 Einheiten** | daraus |

Rechnerisch passen 144 in 160 — praktisch nicht, und der Grund ist die
Stapelgrenze: Greenwood liefert 90 / 36 / 12 / 6 Einheiten, das sind
**9 / 4 / 2 / 1 = genau 16 Plätze**. Ein einziger vollständiger Lauf füllt den
Rucksack also **exakt bis zum Rand** — ohne einen freien Platz für Werkzeug,
Waffe, Nahrung, Beeren oder Gegnerbeute, die alle ebenfalls Plätze belegen. Die
Bibel hält dazu ausdrücklich fest:
**„Der Engpass eines Ausflugs ist der Rucksack, nicht die Zone."**

**Entscheidung des Nutzers (17.08.2026): Es wird später Rucksäcke geben, die
das Inventar erweitern — die Platzgrenze ist damit kein Hindernis, sondern eine
Frage der Reihenfolge.** Zwei Folgerungen bleiben trotzdem stehen:

- Solange der Rucksack 16 Plätze hat, **funktioniert die Abbruchbedingung
  „Zone leergeräumt" nicht**. Entweder kommt die Automatik nach der
  Rucksackerweiterung, oder das Verhalten bei vollem Rucksack muss vorher
  festgelegt werden.
- Auch ein größerer Rucksack läuft irgendwann voll. Das Verhalten in diesem
  Fall ist also ohnehin zu klären, nur nicht mehr als Sperre für das Feature.

**Die härtere Grenze ist der Werkzeugverschleiß — und die fällt mit größeren
Rucksäcken nicht weg.** `DurabilityRules.ToolWear` zieht je **abgeschlossenem**
Knoten Haltbarkeit ab (`Core/DurabilityRules.cs:17–39`), gestaffelt nach
relativem Tier: 3 bei gleichem Tier, 2 eine Stufe darüber, 4 eine Stufe darunter.
Aus den Assets gelesen: Axt, Sense und Spitzhacke haben **100** Haltbarkeit
(T0), die Kupferfassungen **120** (T1). Gerechnet auf ein volles Gebiet:

| Gebiet (45er-Material) | Werkzeug T0 | Verschleiß je Knoten | reicht für | gebraucht |
|---|---|---:|---:|---:|
| Greenwood · Weichholz | Axt (100) | 3 | 33 | **45** |
| Nebelmoor · Pflanzenfaser | Sense (100) | 3 | 33 | **45** |
| Steinbruch · Bruchstein | Spitzhacke (100) | 3 | 33 | **45** |
| Glutruinen · Kupfererz (T1) | Spitzhacke (100) | 4 | 25 | **45** |

**Mit T0-Werkzeug bricht das Hauptwerkzeug also in jedem Gebiet mitten im Lauf**
— nach 33 von 45 Knoten, in den Glutruinen schon nach 25. Die drei Nebenmengen
(18 / 6 / 3) bleiben unter der Grenze und sind unkritisch. Ab **T1-Werkzeug**
kippt das: 120 Haltbarkeit bei Verschleiß 2 gegen T0-Rohstoffe reicht für 60
Knoten und damit für die 45. Ein vollständiger Zonenlauf ist folglich **erst ab
T1-Werkzeug** überhaupt möglich.

**Die Waffe ist dagegen unkritisch.** `WeaponWear` kostet 1 je erfolgreichem
Angriff; ein Außengebiet trägt laut Zonen-Assets rund **6 Gegner**
(`Zone_Greenwood.asset` und die übrigen). Gegen Hammer (120) oder Dolche (220)
ist das kein Engpass.

**Die Rüstung verschleißt dafür schneller als im Handbetrieb.** `ArmorWear`
kostet 1 je erhaltenem Treffer, und die Automatik weicht ausdrücklich nicht aus
— sie trägt also jeden Schlag voll.

**Beim Tod greift die vorhandene Kette:** `PlayerDeathController`,
`DeathBagSpawner` (Todesbeutel) und der Respawn. Die Automatik muss sich dabei
sauber abmelden — §19 (Ereignisse werden abgemeldet) und §4 (irreversible
Aktionen prüfen am Commit-Punkt erneut).

**Balancewirkung — der Punkt, der über den Eintrag hinausreicht.** Als
Spielerfunktion verändert die Automatik den Fortschritt grundlegend. F31-005 hält
fest, dass bis Level 24 rund **12.800 EP aus Wiederholung** kommen müssen — etwa
1.070 Kupferadern. Wer diese Wiederholung an die Automatik abgeben kann, für den
ist eine langsame Kurve kein Problem mehr. **Die in F31-005 vorgeschlagene
Ertragserhöhung könnte dadurch überflüssig werden oder sogar überschießen.**
Beide Einträge gehören zusammen entschieden, sonst wird zweimal an derselben
Schraube gedreht.

### Stoßrichtung (Vorschlag, noch nicht beauftragt)

1. **`AutoPlayController` als virtueller Spieler** über `PlayerInputReader`.
   Kein Eingriff in Motor, Kampf oder Interaktion. Er darf ausschließlich das,
   was ein Spieler auch kann — damit bleibt jede bestehende Regel (Reichweite,
   Abklingzeit, Ausdauer, Verschleiß) automatisch gültig, ohne Sonderfall.
2. **Zustandsautomat** nach dem Vorbild von `EnemyStateMachine`: Ziel wählen →
   hinlaufen → abbauen oder angreifen → nächstes Ziel. Kein Zustand in `Update`
   verstreut.
3. **Zielauswahl über die NavMesh-Pfadlänge, nicht die Luftlinie.** Der nächste
   Punkt in Luftlinie kann hinter einem Hindernis liegen; ein Zickzacklauf wäre
   die sichtbare Folge. Die Registrierungen aus N04-001 liefern die Kandidaten.
4. **Abbruchbedingungen** (über die zwei entschiedenen hinaus zwingend nötig):
   jede Spielereingabe, Tod, Rucksack voll, Werkzeug gebrochen, Zone leer.
5. **Tests:** der Wegtest je Außenzone nach §26 (Pflichtziele = alle Knoten) und
   ein PlayMode-Lauf, der eine Zone automatisch abgrast und am Ende
   nachweist, dass kein erreichbarer Knoten übrig blieb. Ein Test, der nur prüft,
   dass sich die Figur bewegt, beweist nichts.

### Zu klären vor der Umsetzung

- **Was passiert bei vollem Rucksack?** Rücklauf zum Lager und weiter, Stopp mit
  Meldung, oder Weitersammeln unter Verlust. Durch die geplante
  Rucksackerweiterung ist das keine Sperre mehr, aber ein Rücklauf zum Lager
  bedeutet weiterhin Zonenwechsel und Lagerlogik — das ist der teure Weg.
- **Was passiert, wenn das Werkzeug bricht?** Ersatz aus dem Rucksack ziehen,
  unterwegs herstellen (die T0-Rezepte sind Handarbeit ohne Werkbank, M1.9) oder
  anhalten. Mit T0-Werkzeug tritt der Fall in **jedem** Gebiet ein, nicht nur im
  Ausnahmefall.
- **Darf die Automatik Tränke trinken und essen?** `PressConsumable` steht
  bereit. Ohne Heilung stirbt die Figur zwangsläufig; mit Heilung verbraucht die
  Automatik unbeaufsichtigt Vorräte.
- **Sammelt sie Beute auf?** `WorldItemController` und `EnemyLootContainer` sind
  da. Ohne Aufsammeln bleibt der Ertrag der Kämpfe liegen.
- **Nur die aktuelle Zone oder Zonenwechsel?** „Die ganze Map" ist zweideutig:
  die Zone oder die Weltkarte. `MapExitVolume` und `MapExitCoordinator` würden
  den Wechsel tragen.
- **Gilt sie in Eidra-Schmiede und Verlies?** Dort gibt es Räume, Truhen und
  einen Bossablauf — anderes Verhalten als im Freien.
- **Verhalten gegenüber Bossen.** Garon arbeitet mit Telegraphen
  (`BossController.HasActiveTelegraph`), auf die ein Spieler mit Ausweichen
  antwortet. Eine Automatik, die nicht ausweicht, läuft dort in den sicheren
  Tod. Vorschlag: Bosse auslassen, solange nicht ausdrücklich anders gewünscht.
- **Sichtbarkeit des Knopfs:** von Anfang an oder erst freigeschaltet? Ein von
  Beginn an verfügbarer Automatikknopf verändert das erste Spielerlebnis
  vollständig.
- **Zeitraffer?** Nicht gewünscht bisher, aber die naheliegende Anschlussfrage,
  sobald jemand zusieht, wie die Figur 72 Knoten abläuft.
- **Eidra-Begleiter:** kämpfen sie mit, und wechselt die Automatik sie
  (`PressSwitchEidra`)?

**Berührt:**

- **F31-005 (Leveltempo)** — siehe Balanceabsatz oben. Vor einer
  Ertragserhöhung entscheiden, ob die Automatik kommt.
- **N04-001 (Minimap)** — teilt sich die Gegner- und Knotenregistrierung.
- **§26 der Bibel** — muss bei Umsetzung um den Wegtest für Außenzonen ergänzt
  werden; die Bibel sieht diesen Fall bereits ausdrücklich vor.

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## N04-003 — Tooltip für Fähigkeiten

**Bereich:** HUD, Bedienung

**Wunsch (19.08.2026):** Beim Überfahren einer Fähigkeit mit der Maus — später
mit dem Finger in der mobilen Fassung — soll ein Tooltip erscheinen, der
erklärt, was die Fähigkeit tut.

**Befund (19.08.2026, Codestand v0.3.2):** Der Wunsch trifft auf **drei
Lücken**, und die mittlere ist die wichtigste.

**1. Es gibt im ganzen Spiel keine Tooltip-Infrastruktur.** Kein
`IPointerEnterHandler`, keine Hover-Behandlung, kein Tooltip-Fenster. Die
Codetreffer auf „Tooltip" sind ausnahmslos `[Tooltip]`-Attribute für den
Unity-Inspektor — Text für den Entwickler, der im Spiel nie erscheint.

**2. Fähigkeiten tragen überhaupt keinen Beschreibungstext.** `AbilityData`
führt Anzeigename, Icon und die Zahlen (Abklingzeit, Reichweite, Ansagezeit,
Wirkdauer, Stagger, Rückenschadenfaktor, Radius, Schaden pro Sekunde,
Schutzsenkung) — **kein Beschreibungsfeld**. Zum Vergleich: `ItemDefinition`
hat eines, und es wird in Werkbank und Inventar im Detailfeld angezeigt. Bei
Fähigkeiten gibt es schlicht nichts anzuzeigen.

**3. Berührung kennt kein „Überfahren".** Auf dem Handy gibt es kein Hover;
der Auslöser muss dort ein anderer sein.

**Zu entscheiden (zwei Fragen):**

**(a) Woher kommt der Text?**

1. **Aus den Daten erzeugt** — z. B. „Rückenmal · Rückentreffer ×1,45 für 6 s ·
   Reichweite 12 · Abklingzeit 9 s". Vorbild ist `EidraPassiveText` aus
   F32-005: Ein erzeugter Text **kann nicht behaupten, was das Spiel nicht
   tut** — genau der Fehler, den Ignivars Passivanzeige gemacht hat. Pflegt
   sich bei jeder Balanceänderung von selbst. **Empfehlung.**
2. **Von Hand geschrieben** — ein neues Feld je Fähigkeit. Schöner formuliert,
   aber sechs Texte, die bei jeder Zahlenänderung nachgezogen werden müssen
   und sonst still falsch werden.
3. Beides: erzeugte Zahlenzeile plus ein kurzer geschriebener Satz darüber.

**(b) Wie wird er mobil ausgelöst?**

1. **Langes Drücken** zeigt den Tooltip, kurzes Antippen löst wie bisher aus.
   Kein Konflikt mit der Bedienung. **Empfehlung.**
2. Erstes Antippen zeigt, zweites löst aus — sicherer, aber es kostet im Kampf
   einen Tastendruck.
3. Eigener Infoknopf, der alle Fähigkeiten erklärt.

**Umfang:** Ein Tooltip-Fenster im HUD-Prefab (autoriert wie die übrigen
Fenster), ein kleines Verhalten an den Fähigkeitsknöpfen von
`CombatHudActionPresenter`, und — bei Variante 1 — eine reine Textfunktion
nach dem Muster von `EidraPassiveText`. Sinnvollerweise gleich so gebaut,
dass auch Waffen, Tränke und Eidra denselben Tooltip nutzen können; die
Knöpfe liegen ohnehin nebeneinander in derselben Leiste.

**Umgesetzt (20.08.2026) — Entscheidungen wie empfohlen:**

- **Text aus den Daten** (`EidraAbilityText`, rein und geprüft). Je Fähigkeit
  Wirkung, Reichweite und Abklingzeit aus denselben Zahlen, die der Kampf
  anwendet. Ein geschriebener Text hätte bei der nächsten Balanceänderung
  still gelogen — genau der Fehler aus F32-005.
- **Steinhaut nennt keine Reichweite.** Ihre Reichweite ist 0, weil sie auf
  die eigene Figur wirkt; „Reichweite 0" hinzuschreiben wäre irreführender
  als nichts. An dieser 0 ist die Fähigkeit in F31-016 schon einmal
  gescheitert, weil eine Prüfung sie falsch gelesen hat. Ein Test hält es
  fest.
- **Zwei Auslöser:** am Rechner Überfahren mit der Maus, mobil **langes
  Drücken** (0,4 s). Kurzes Antippen löst die Fähigkeit weiter aus — der
  Tooltip kostet im Kampf keinen Tastendruck.
- **Ein Feld für alle Knöpfe**, mittig über der Leiste (verdeckt weder
  Lebensleiste noch Minimap). Eingebaut per **Prefab-Patch**
  (`HudTooltipBuilder`), nicht per Neubau — ein Neubau hat in dieser Runde
  mehrfach gewachsene Bestandteile abgeworfen.
- Fehlt das Feld (altes Prefab), bleiben die Auslöser still statt zu werfen.

**Nachweise:** EditMode 5 Tests auf dem Textbau (Name und Abklingzeit immer,
Staggerwert aus den Daten, Schild ohne Reichweite, Rückenmal mit Faktor und
Dauer), alle vorher rot. PlayMode: Im echten HUD sind Feld und beide Auslöser
vorhanden, ohne Eidra bleibt der Text leer, nach dem Fang nennt er Terrocks
Fähigkeiten.

> **Noch nicht angeschlossen:** Waffen, Tränke und der Eidra-Wechsel. Das
> Feld ist dafür ausgelegt, der Wunsch betraf die Fähigkeiten. Für
> Gegenstände gibt es in `ItemDefinition` bereits Beschreibungstexte, die
> sich direkt verwenden ließen.

**Status: umgesetzt am 20.08.2026.**

---

## N04-004 — Grafikaufwertung auf Mid Poly (NICHT in dieser Arbeitslinie)

**Bereich:** Weltdarstellung, Assets — **außerhalb dieser Sammlung**

**Nutzerentscheid 20.08.2026: „Ignoriere bitte die Mid Poly Umstellung, die
baue ich woanders."**

Der Eintrag bleibt als Wegweiser stehen, damit die Umstellung hier nicht
versehentlich noch einmal aufgemacht wird. Der ausgearbeitete Auftrag liegt in
`Documentation/Auftraege/AUFTRAG_MIDPOLY_GESAMTUMSTELLUNG.md`
(MIDPOLY-000, 20.08.2026) und wird an anderer Stelle bearbeitet.

**Was hier trotzdem zu beachten bleibt:** Abschnitt 5.7 und Abnahmepunkt 6 des
Auftrags verlangen, dass **kein produktiver Builder freigegebene
Mid-Poly-Arbeit zurücksetzt**. Wer in dieser Arbeitslinie einen
Geometrie-Builder anfasst oder Assets neu baut, kann also fremde Arbeit
abwerfen — dieselbe Falle, die in der v0.3.1-Runde viermal zugeschlagen hat.
Vor jedem Builder-Lauf gilt deshalb: prüfen, ob der Ausgabepfad inzwischen
Mid-Poly-Assets enthält.

**Status: bewusst nicht Teil dieser Sammlung (20.08.2026).**

---

## Offene Sammlung

Weitere v0.4-Features kommen als **N04-004** ff. hierher. Erst wenn die Runde
vollständig ist, wird sie beauftragt.
