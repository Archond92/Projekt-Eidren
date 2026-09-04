# Fixsammlung für Eidren v0.2.1

**Status:** F-001 bis F-004 sowie F-007, F-008, F-009, F-011, F-012 und F-013
sind am 7. August 2026 in den Quellen umgesetzt (Kernverhalten per
EditMode-Test nachgewiesen; vollständige Abnahme laut jeweiliger
Kriterienliste steht noch aus). F-014 ist bis auf die Gebäudesymbole umgesetzt.

**Zurückgestellt hinter die 3D-Arbeiten:** F-005, F-006 und F-010 betreffen
alle die Gebietskiste, für die ein neues 3D-Modell mit eigener
Öffnungsanimation entsteht; sie sind gemeinsam damit zu behandeln. Ebenso die
Gebäudesymbole aus F-014 — auch für Gebäude entstehen neue 3D-Modelle, aus
denen die Icons später gerendert werden.

Damit ist kein Eintrag mehr unbearbeitet offen: Alles, was ohne die neuen
3D-Modelle sinnvoll umsetzbar war, ist in den Quellen umgesetzt.

**Hinweis zu den Analysen:** Zwei Ursachenangaben dieser Sammlung haben der
Prüfung nicht standgehalten. In F-002 war die vermutete Ursache falsch; die
tatsächliche (fehlende Füllgrafik) lag quer zu F-002, F-003 und F-011. In F-012
war die beschriebene Bereinigung bereits erfolgt, offen war nur das
Rückfallrisiko. Analysen dieser Sammlung sind vor der Umsetzung zu prüfen,
nicht zu übernehmen.

**Version:** Eidren 0.2.1

In dieser Datei werden beobachtete Fehler und konkrete Bedienungsverbesserungen
für das Wartungsupdate v0.2.1 gesammelt. Die Einträge beschreiben Problem,
Ursache, Sollverhalten und Abnahme. Die Umsetzung erfolgt getrennt von dieser
Sammlung.

## F-001 — XP-Panel zeigt das Charakterbild hinter dem Rucksack-Button

**Status:** umgesetzt am 7. August 2026, gemeinsam mit F-004. Der Builder
`FixCollectionHudBuilder.UpgradeXpHud` übernimmt kein erstbestes Sprite mehr:
Das separate `ExperiencePanel` oben rechts wird entfernt, XP-Hintergrund und
-Füllung sind reine Farbflächen ohne Fremd-Sprite. Das Charakterbild kommt im
HUD nur noch einmal vor (Portrait im Spielerstatus). Nachweis:
`CombatHudXpTests` (rot vor Builder-Lauf, grün danach; Logs
`f001-red.log`/`f001-builder.log`/`f001-green.log`). Bildbasierte Abnahme der
Bildschirmüberschneidungen steht aus.

**Bereich:** Kampf-HUD, Spielerfortschritt und visuelle Hierarchie

**Beobachtung:** Rechts oben, unmittelbar hinter beziehungsweise unter dem
Rucksack-Button, erscheint ein großer Ausschnitt des Charakterbildes. In diesem
Bereich sollte sich die nachgerüstete Level- und XP-Anzeige befinden. Der
Bildfehler und die fehlende XP-Anzeige gehören damit zum selben fehlerhaften
HUD-Element.

**Analyse:** Der sichtbare Kopf gehört nicht zur Weltfigur. Der Builder
`FixCollectionHudBuilder.UpgradeXpHud` sucht pauschal das erste unter dem
Kampf-HUD gefundene `Image` mit einem Sprite und verwendet dieses Sprite als
allgemeine Grafik für die XP-Nachrüstung. Im erzeugten Prefab ist diese erste
Grafik `Assets/_Game/Resources/Art/UI/player_portrait.png`.

Dasselbe Charakterbild wird anschließend dreifach zugewiesen:

- als Hintergrundgrafik von `ExperiencePanel`,
- als Hintergrund von `ExperienceBack`,
- und als gefüllte Grafik von `ExperienceFill`.

Das XP-Panel ist mit einer Größe von 270×76 oben rechts bei `(-40, -38)`
verankert. Der Rucksack-Button liegt ebenfalls oben rechts bei `(-22, -20)` und
ist 190×64 groß. Beide Flächen überschneiden sich deutlich. Dadurch liegt das
falsch skalierte beziehungsweise zugeschnittene Charakterbild hinter dem
Rucksack-Button, während XP-Leiste und Level nicht als verständliche Anzeige
erkennbar sind.

**Sollverhalten:** Das Charakterporträt wird ausschließlich im vorgesehenen
Spielerstatus links oben verwendet. XP-Hintergrund und XP-Füllung verwenden
neutrale, dafür vorgesehene Balkengrafiken. Level und XP werden gemäß F-004 in
den Spielerstatus links oben integriert. Hinter dem Rucksack-Button befindet
sich weder ein Charakterbild noch ein anderes überlappendes HUD-Panel.

**Vorgeschlagene Behebung:** Die XP-Nachrüstung darf kein beliebiges erstes
Sprite aus der Prefab-Hierarchie übernehmen. Rahmen, Hintergrund und Füllung
werden explizit mit den vorgesehenen UI-Grafiken verdrahtet oder als einfache
farbige Flächen ohne Fremd-Sprite aufgebaut. Die Positionierung erfolgt
gemeinsam mit dem linken Spielerstatus und nicht als separates Panel am rechten
Bildrand.

**Reproduktion:**

1. Einen Spielstand laden und ein Gebiet mit aktivem Kampf-HUD betreten.
2. Den Bereich rechts oben hinter dem Rucksack-Button betrachten.
3. Den dort sichtbaren Ausschnitt des Charakterbildes feststellen.
4. Prüfen, dass an derselben Stelle keine verständliche XP-Leiste und kein
   lesbarer Levelwert erscheinen.

**Abnahme:**

- `player_portrait.png` wird weder von `ExperiencePanel` noch von
  `ExperienceBack` oder `ExperienceFill` verwendet.
- Das Charakterbild erscheint ausschließlich einmal im vorgesehenen
  Spielerstatus links oben.
- XP-Hintergrund und XP-Füllung sind als eindeutiger horizontaler Balken
  erkennbar.
- Das Ergebnis hängt nicht von der Reihenfolge der `Image`-Komponenten im
  Prefab ab.
- Rechts oben liegt kein XP-Panel hinter oder unter dem Rucksack-Button.
- Der Rucksack-Button bleibt innerhalb der Safe Area und jederzeit vollständig
  bedienbar.
- Die automatisierte Abnahme prüft die konkret verwendeten Sprites und die
  tatsächlichen Bildschirmüberschneidungen.

## F-002 — Ausdauerbalken reagiert nicht sichtbar auf Ausweichen

**Status:** umgesetzt am 7. August 2026. **Die unten vermutete Ursache war
falsch und ist ersetzt** — siehe „Tatsächliche Ursache".

**Bereich:** Spielerbewegung und Kampf-HUD

**Beobachtung:** Beim Drücken der Leertaste führt der Spieler die Ausweichrolle
aus, der türkise Ausdauerbalken links oben sinkt jedoch nicht sichtbar ab.

**Analyse:** Die Leertaste ist korrekt mit `Dodge` verbunden. Eine erfolgreiche
Ausweichrolle zieht im `PlayerMotor` 28 Ausdauer ab und löst anschließend
`StaminaChanged` aus. Der HUD-Presenter hört auf dieses Ereignis und schreibt
den Quotienten aus aktueller und maximaler Ausdauer in `staminaFill`.

Der Datenpfad für den Verbrauch ist damit grundsätzlich vorhanden.

**Tatsächliche Ursache (geprüft am 7. August 2026):** Die ursprüngliche
Vermutung — radiale statt horizontale Füllung — trifft **nicht** zu. Alle
Statusbalken standen bereits auf `Filled`/`Horizontal` mit Ursprung links und
waren korrekt verdrahtet (`staminaFill` → `PlayerStatus/Stamina/Fill`). Auch
Verbrauch und Regeneration im `PlayerMotor` sind korrekt: Die Ausweichrolle
zieht 28 Punkte ab, während der Rolle regeneriert nichts, danach 23 Punkte pro
Sekunde.

Der Fehler lag in der Darstellung: **Keiner der Füllgrafiken war ein Sprite
zugewiesen.** Eine `Image` mit `Type.Filled` ohne Sprite fällt in Unity auf
`Graphic.OnPopulateMesh` zurück und zeichnet das volle Rechteck — `fillAmount`
bleibt wirkungslos. Das ist im Test `GefuellteGrafikOhneSprite_IgnoriertDen
Fuellwert` gemessen belegt: ohne Sprite 200 px Meshbreite trotz `fillAmount`
0,25, mit Sprite 50 px.

Betroffen waren nicht nur die Ausdauer, sondern **alle sechs horizontalen
Balken** (Leben, Ausdauer, Resonanz, Boss-Leben, Boss-Stagger und die neue
XP-Leiste): Sie standen unabhängig vom Wert dauerhaft voll. Das erklärt
zugleich die Beobachtung aus F-003 (dauerhaft gefüllter Resonanzbalken); die
dort behobene fehlende Ereignisbindung war ein zweiter, eigenständiger Fehler.
Die neun radialen Grafiken (Cooldowns, Interaktionsring) besaßen Sprites und
funktionierten.

**Behebung:** `FixCollectionHudBuilder.UpgradeStatusBars` erzeugt die schlichte
Füllgrafik `Assets/_Game/Resources/Art/UI/ui_bar_fill.png` und weist sie jeder
gefüllten Grafik ohne Sprite zu (idempotent, sechs Grafiken gepatcht).
Nachweis: `CombatHudBarFillTests` (Logs
`f002-red.log`/`f002-builder.log`/`f002-green.log`). Die Zahlenabnahme
(100 → 72 im laufenden Build) steht als PlayMode-Prüfung aus.

**Sollverhalten:** Jede erfolgreiche Ausweichrolle reduziert die türkise
Ausdauerleiste sofort und deutlich von rechts nach links. Die Leiste bleibt
während des Verbrauchs stabil ablesbar und füllt sich anschließend entsprechend
der tatsächlichen Regeneration wieder auf. Wird wegen zu geringer Ausdauer
nicht ausgewichen, findet auch kein weiterer Verbrauch statt.

**Abnahme:**

- Eine Ausweichrolle bei voller Ausdauer reduziert Wert und Balken exakt von
  100 auf 72.
- Mehrere zulässige Ausweichrollen werden jeweils genau einmal dargestellt.
- Die sichtbare Füllung entspricht jederzeit `CurrentStamina / MaxStamina`.
- Die Leiste leert sich horizontal und nicht radial oder sprunghaft.
- Regeneration und Balken laufen mit derselben Rate bis maximal 100.
- Tastatur, Gamepad und Touch verwenden denselben Verbrauch und dieselbe
  Anzeige.

## F-003 — Resonanzbalken bleibt ohne aktives Eidra gefüllt

**Status:** umgesetzt am 7. August 2026. `CombatHudStatusPresenter` abonniert
jetzt `ActiveEidraChanged` (Abmeldung in `Unbind`); Aktivieren, Wechseln und
Entfernen aktualisieren den Balken ohne Neuaufbau des HUDs. Nachweis:
`CombatHudStatusPresenterTests` (rot vor, grün nach der Änderung; Logs
`f003-red.log`/`f003-green.log`). Vollständige Abnahme (Speichern/Laden,
Gebietswechsel im PlayMode) steht aus.

**Bereich:** Eidra-Team und Kampf-HUD

**Beobachtung:** Der goldene Resonanzbalken links oben wird vollständig gefüllt
angezeigt, obwohl kein Eidra aktiv ist.

**Analyse:** `CombatHudStatusPresenter.Bind` setzt den Resonanzbalken nur beim
Aufbau des HUDs auf voll oder leer. Anders als der Aktions-Presenter abonniert
der Status-Presenter das Ereignis `ActiveEidraChanged` nicht. Ändert sich das
aktive Team nach diesem einmaligen Abbild, bleibt die Anzeige auf dem alten
Zustand stehen. Der Balken zeigt damit keinen verlässlichen aktuellen
Eidra-Zustand.

**Sollverhalten:** Ohne aktives Eidra ist der goldene Balken vollständig leer.
Sobald ein Eidra aktiv wird, aktualisiert sich die Anzeige unmittelbar. Wird das
aktive Eidra entfernt, abgewählt oder durch Laden beziehungsweise Szenenwechsel
nicht wiederhergestellt, wird die Anzeige sofort geleert.

**Abnahme:**

- Ein neuer Spielstand ohne gefangenes oder aktives Eidra zeigt einen leeren
  goldenen Balken.
- Aktivieren, Wechseln und Entfernen eines Eidra aktualisiert die Anzeige ohne
  erneutes Erzeugen des HUDs.
- Speichern, Laden und Gebietswechsel stellen exakt den aktuellen Teamzustand
  dar.
- Die Anzeige reagiert auf dieselbe Wahrheitsquelle wie die Eidra-Fähigkeiten
  und die sichtbare Begleiterfigur.
- Der Balken zeigt keinen alten Zustand aus einer vorherigen Szene oder
  Bindung.

## F-004 — XP-Leiste und Charakterlevel fehlen links oben

**Status:** umgesetzt am 7. August 2026, gemeinsam mit F-001. Level („LV 1")
und XP-Leiste sitzen jetzt im `PlayerStatus`-Block links oben: die Leiste
unterhalb der Resonanzleiste in der Balkenspalte (x 132), der Levelwert rechts
daneben — innerhalb des 400×150-Statuspanels und damit ohne Berührung mit dem
Rucksack-Button oben rechts. Der Testnachweis prüft Zugehörigkeit zum
Statusblock, Aktivität und Füllverhalten (`CombatHudXpTests`); die Bindung an
die Progression lief bereits über `ConfigureProgressionReferences` und
`RefreshProgression`. Abnahme über alle Auflösungen im PlayMode steht aus.

**Bereich:** Spielerfortschritt und Kampf-HUD

**Beobachtung:** Im Spielerstatus links oben werden weder das aktuelle
Charakterlevel noch eine XP-Leiste angezeigt, obwohl beides bereits als Fix für
v0.2 gefordert war.

**Analyse:** Die vorhandene Nachrüstung erzeugt `ExperiencePanel` als separates
Element oben rechts. Dort ist ebenfalls der dauerhafte Rucksack-Button
verankert. Zusätzlich verwendet das Panel durch F-001 fälschlicherweise das
Charakterporträt als Panel-, Hintergrund- und Füllgrafik. Die Umsetzung
entspricht daher weder der gewünschten Gruppierung im Spielerstatus links oben
noch einer verständlichen und überlagerungsfreien HUD-Struktur. Der bisherige
Abnahmetest kontrolliert lediglich, ob `ExperienceFill` und `LevelValue` als
Referenzen existieren. Er prüft nicht, ob beide Elemente aktiv, sichtbar,
korrekt bebildert, innerhalb der Safe Area und frei von anderen UI-Elementen
sind.

**Sollverhalten:** Das aktuelle Level und der XP-Fortschritt bis zum nächsten
Level sind dauerhaft und eindeutig im Spielerstatus links oben angeordnet. Sie
bilden zusammen mit Portrait, Lebenspunkten, Ausdauer und Eidra-Zustand einen
einheitlichen Block und konkurrieren nicht mit dem Rucksack-Button.

**Abnahme:**

- Links oben wird das aktuelle Level als eindeutiger Wert, beispielsweise
  `LV 1`, angezeigt.
- Direkt im selben Statusblock befindet sich eine klar erkennbare XP-Leiste.
- Die XP-Leiste zeigt exakt den Fortschritt vom aktuellen zum nächsten Level.
- Ein XP-Gewinn aktualisiert die Leiste unmittelbar; ein Levelaufstieg
  aktualisiert Levelwert und neuen XP-Fortschritt.
- Maximallevel, Speichern, Laden und Gebietswechsel werden korrekt dargestellt.
- Level und XP sind bei allen unterstützten Auflösungen aktiv, sichtbar und
  innerhalb der Safe Area.
- Kein Teil der Anzeige wird vom Rucksack-Button oder einem anderen
  HUD-Element überdeckt.
- Die automatisierte Abnahme prüft neben den Referenzen auch Aktivität,
  Bildschirmposition und Überschneidung.

## F-005 — Kistenöffnung zeigt zweimal das Kontrollbutton-Symbol

**Status:** offen; fehlerhafte Umsetzung von v0.2 F-016. Zurückgestellt hinter
den Wanderer3D-Einbau: Die Handgrafiken sind Sprite-Renderer an den
Kisten-Prefabs (`OpeningHand_Left/Right`). Neben der 3D-Spielfigur wirken
gezeichnete 2D-Hände voraussichtlich noch fremder; nach der Umstellung ist zu
prüfen, ob die Öffnung stattdessen über die Figur selbst (Harvest-/Bück-Pose
der `MeshActorPresentation`) plus Deckelanimation erzählt wird und die
Handgrafiken ganz entfallen. Sollverhalten vor Umsetzung neu fassen.

**Bereich:** Gebietskisten, Interaktion und Animation

**Beobachtung:** Während des Öffnens einer Gebietskiste erscheinen zwei große
Kopien des allgemeinen Kontrollbutton- beziehungsweise Interaktionssymbols. Die
Darstellung sieht nicht wie zwei Hände aus, die eine Kiste greifen und öffnen.

**Analyse:** Der Kisten-Builder lädt für beide Objekte `OpeningHand_Left` und
`OpeningHand_Right` dasselbe Asset
`Assets/_Game/Resources/Art/UI/ui_interaction_hand.png`. Dieses Asset gehört zur
256×256 großen Familie der HUD-Interaktionsglyphen. Für die rechte Seite wird
lediglich dieselbe Glyphe horizontal gespiegelt. Es existieren somit zwei
Kopien eines Bedienelements, aber keine eigens gezeichnete Zwei-Hand-Sequenz für
die Handlung in der Spielwelt.

Die bisherige automatisierte Abnahme verlangt nur, dass `leftHand` und
`rightHand` auf zwei Renderer verweisen und dass das verwendete Bild Transparenz
besitzt. Sie prüft weder das tatsächliche Motiv noch Griffposition,
Bewegungsrichtung oder die visuelle Unterscheidung vom Kontrollbutton-Symbol.

**Sollverhalten:** Beim Öffnen sind zwei eindeutig anatomische Hände sichtbar,
die von gegenüberliegenden Seiten an Deckel, Verschluss oder Kistenrand greifen
und gemeinsam eine nachvollziehbare Öffnungsbewegung ausführen. Die Animation
verwendet kein HUD- oder Kontrollbutton-Symbol.

**Abnahme:**

- Die Öffnungsanimation verwendet eigens dafür vorgesehene Handgrafiken und
  nicht `ui_interaction_hand.png` oder eine andere HUD-Glyphe.
- Linke und rechte Hand sind als zwei Hände erkennbar und nicht nur zwei
  gespiegelte Kopien eines allgemeinen Symbols.
- Beide Hände greifen sichtbar an plausiblen Stellen der jeweiligen Kiste an.
- Bewegung der Hände und Bewegung des Deckels ergeben gemeinsam einen
  verständlichen Öffnungsvorgang.
- Abbruch blendet die Hände aus und setzt die Kiste ohne sichtbaren Sprung in
  den geschlossenen Zustand zurück.
- Abschluss und dauerhafter geöffneter Zustand zeigen keine zurückbleibenden
  Handgrafiken.
- Die visuelle Abnahme prüft das gerenderte Motiv; vorhandene Renderer allein
  gelten nicht als Nachweis.

## F-006 — Öffnungs- und Abbauanimationen sind überdimensioniert

**Status:** offen. Zurückgestellt hinter den Wanderer3D-Einbau: Die
Kistenhände-Hälfte hängt an F-005. Die Werkzeug-Hälfte
(`PlayerHarvestVisual`, Sprite-Werkzeug mit 72-Grad-Pendel) bleibt laut
Einbau-Plan zunächst am Spieler-Prefab bestehen; nach der Umstellung schwingt
damit ein 2D-Sprite an einer 3D-Figur, die ihre Waffe bereits als Mesh hält.
Die Stoßrichtung ändert sich dann von „Sprite kleiner skalieren" zu
„Sprite-Werkzeug entfernen und Abbau über Waffenmesh plus Harvest-Clip
darstellen". Sollverhalten vor Umsetzung neu fassen.

**Bereich:** Weltinteraktionen, visuelle Größenordnung und Lesbarkeit

**Beobachtung:** Die visuellen Animationen beim Öffnen von Kisten und beim
Abbauen von Ressourcen sind im Verhältnis zu Spieler, Kiste und Ressourcenziel
zu groß. Sie dominieren das Bild und verdecken Teile der eigentlichen Handlung.

**Analyse:** Die beiden aktuellen Kistengrafiken werden als 256×256 große
Sprites mit einem lokalen Weltmaßstab von `0,72` erzeugt. Ihre Größe wird nicht
aus den Abmessungen der jeweiligen Kiste oder aus der sichtbaren Handgröße des
Spielers abgeleitet. Das Abbauwerkzeug besitzt einen separat festgelegten
Maßstab von `0,28` und einen großen Pendelausschlag von bis zu 72 Grad um seine
Ruhelage. Zwischen Öffnungs- und Abbaufeedback gibt es keine gemeinsame Regel
für sichtbare Weltgröße, Kameraabstand oder maximal überdeckte Fläche.

**Sollverhalten:** Öffnungs- und Abbauanimationen unterstützen die Handlung,
ohne Spieler, Zielobjekt oder Umgebung zu verdecken. Hände entsprechen ungefähr
der Handgröße der Spielfigur; Werkzeuge entsprechen der sichtbaren Ausrüstung
des Spielers. Die Animation bleibt bei üblicher Kameradistanz erkennbar, wirkt
aber als Teil der Welt und nicht wie ein groß eingeblendetes UI-Symbol.

**Abnahme:**

- Öffnungshände bleiben räumlich am Deckel beziehungsweise Verschluss der Kiste
  und überragen die Kistensilhouette nur in dem für die Bewegung nötigen Maß.
- Eine einzelne Hand ist nicht annähernd so groß wie die gesamte Kiste oder der
  Oberkörper des Spielers.
- Abbauwerkzeuge besitzen eine zur Spielerfigur passende Länge und Stärke.
- Der Abbauschwung verdeckt weder den gesamten Spieler noch das vollständige
  Ressourcenziel.
- Öffnungs- und Abbauanimationen verwenden eine gemeinsame dokumentierte
  Größenregel statt voneinander unabhängiger Rohskalierungen.
- Kleine, mittlere und große Kisten sowie alle Werkzeugfamilien werden in der
  tatsächlichen Spielkamera visuell geprüft.
- Die Darstellungen bleiben bei allen unterstützten Auflösungen und
  Seitenverhältnissen erkennbar, ohne durch die UI-Skalierung größer zu werden.

## F-007 — Kistenöffnung soll nach einmaligem Auslösen selbstständig laufen

**Status:** umgesetzt am 7. August 2026; Bedienungsänderung gegenüber v0.2
F-016. Geschlossene `WorldChestContainer` melden jetzt `InteractionMode.Timed`
statt `Hold` (geöffnete bleiben `Instant`); die Sicherheitsabbrüche der
`InteractionSession` bleiben unverändert wirksam. Nachweis:
`WorldChestInteractionModeTests` plus bestehender Sessiontest
`TimedInteraction_ContinuesAfterInputRelease` (Logs
`f007-red.log`/`f007-green.log`). Vollständige Abnahme (alle Eingabearten im
PlayMode) steht aus.

**Bereich:** Gebietskisten, Eingabe und zeitgebundene Interaktion

**Beobachtung:** Zum Öffnen einer geschlossenen Gebietskiste muss der
Kontrollbutton während des gesamten Vorgangs gedrückt gehalten werden. Beim
Ressourcenabbau genügt dagegen ein einmaliges Auslösen; der zeitgebundene
Vorgang läuft anschließend selbstständig bis zum Abschluss.

**Analyse:** Geschlossene `WorldChestContainer` melden derzeit
`InteractionMode.Hold`. Die gemeinsame `InteractionSession` bricht ausschließlich
diesen Modus ab, sobald `InteractHeld` nach dem ersten Tastendruck wieder false
wird. Ressourcen verwenden `InteractionMode.Timed`; deshalb laufen sie nach
einem einzelnen Klick beziehungsweise Tastendruck weiter. Die unterschiedliche
Bedienung entsteht somit nicht durch die Dauer oder Animation, sondern allein
durch den abweichenden Interaktionsmodus der Kiste.

**Sollverhalten:** Ein einmaliger Klick oder Tastendruck startet den vollständigen
zeitgebundenen Öffnungsvorgang. Der Kontrollbutton muss danach nicht weiter
gehalten werden. Die Kiste verwendet dasselbe Eingabeprinzip wie der
Ressourcenabbau und öffnet sich nach Ablauf der vorgesehenen Dauer automatisch.

Das selbstständige Weiterlaufen hebt die bestehenden Sicherheitsabbrüche nicht
auf: Verlässt der Spieler die Reichweite, wechselt das Ziel, greift der Spieler
an, weicht aus, erleidet Schaden, stirbt oder beginnt ein Szenenwechsel, wird
der Vorgang weiterhin abgebrochen und die Kiste bleibt geschlossen.

**Abnahme:**

- Ein einzelner kurzer Tastendruck auf den Interaktionsbutton startet die
  vollständige Kistenöffnung.
- Das Loslassen unmittelbar nach dem Start bricht die Öffnung nicht ab.
- Maus, Tastatur, Gamepad und Touch verhalten sich identisch.
- Weitere Klicks während des laufenden Vorgangs starten keine zweite Sitzung,
  beschleunigen die Öffnung nicht und öffnen die Kiste nicht mehrfach.
- Fortschritt und Zwei-Hand-Animation laufen nach dem Start bis zum selben
  Abschlusszeitpunkt weiter.
- Verlassen der Reichweite, Zielwechsel, Angriff, Ausweichen, Schaden, Tod,
  deaktivierte Eingabe und Szenenwechsel brechen den Vorgang weiterhin ab.
- Nach einem Abbruch bleibt die Kiste geschlossen und kann mit einem erneuten
  einzelnen Tastendruck wieder von vorn geöffnet werden.
- Bereits geöffnete Kisten benötigen keinen erneuten zeitgebundenen
  Öffnungsvorgang.

## F-008 — „ALLES NEHMEN“ auch für Gegnerloot anbieten

**Status:** umgesetzt am 7. August 2026. Die Freigaberegel ist als
`StorageWindow.AllowsTakeAll` zusammengeführt (Gebietskisten und
`EnemyLootContainer` ja, Heimatlager nein) und wirkt an beiden Prüfstellen:
Button-Sichtbarkeit und `TakeAll()`. Der Transfer selbst läuft unverändert
über den vorhandenen `ItemTransferService`. Nachweis:
`StorageWindowTakeAllTests` (Logs `f008-red.log`/`f008-green.log`).
Vollständige Abnahme (Sichtbarkeit und Voll-/Teiltransfer im PlayMode mit
einem `EnemyLootContainer`) steht aus.

**Bereich:** Gegnerloot, Inventar und Beutetransfer

**Beobachtung:** Das Beutefenster eines getöteten Gegners besitzt keinen Button
„ALLES NEHMEN“. Gegenstände müssen einzeln aus dem Gegnerloot in den Rucksack
übertragen werden, obwohl geöffnete Gebietskisten im selben Fenster bereits
eine Sammelentnahme anbieten.

**Analyse:** `EnemyLootContainer` und `WorldChestContainer` implementieren beide
`IInspectableContainer` und werden im selben `StorageWindow` angezeigt. Die
Sichtbarkeit des vorhandenen `TakeAllButton` ist jedoch ausdrücklich auf
`_container is WorldChestContainer` beschränkt. Dieselbe konkrete Typprüfung
steht am Anfang von `StorageWindow.TakeAll()` und beendet den Vorgang für jeden
anderen Behälter. Gegnerloot wird daher sowohl in der Oberfläche als auch in
der Transferfunktion ausgeschlossen, obwohl der allgemeine Transferservice
seine Plätze bereits verarbeiten kann.

**Sollverhalten:** Wird der Lootbehälter eines getöteten Gegners geöffnet, ist
der Button „ALLES NEHMEN“ sichtbar und bedienbar. Ein Betätigen überträgt alle
Gegenstände, die vollständig oder teilweise in den Rucksack passen, nach
denselben Stapel- und Kapazitätsregeln wie bei einer geöffneten Gebietskiste.
Nicht unterbringbare Restmengen bleiben unverändert beim Gegner.

Der Button bleibt auf echte Beutequellen beschränkt. Normale Lagerkisten der
Heimatbasis und andere Behälter erhalten durch diesen Fix nicht automatisch
dieselbe Einwegaktion.

**Abnahme:**

- Das geöffnete Beutefenster jedes unterstützten Gegnertyps zeigt den Button
  „ALLES NEHMEN“.
- Ein einzelnes Betätigen überträgt alle aktuell unterbringbaren Gegenstände aus
  dem Gegnerloot in den Rucksack.
- Vorhandene kompatible Stapel werden zuerst bis zu ihrer Maximalgröße
  aufgefüllt; anschließend werden freie Rucksackplätze verwendet.
- Bei teilweise oder vollständig vollem Rucksack gehen keine Gegenstände
  verloren und es entstehen keine Duplikate.
- Nicht übertragbare Gegenstände und Restmengen bleiben im selben
  Gegnerlootbehälter und können später entnommen werden.
- Nach vollständiger Entnahme ist der Gegnerloot leer; Markierung und
  Lebensdauer reagieren wie bei einer vollständigen Einzelentnahme.
- Mehrfaches Betätigen während eines laufenden Transfers startet keinen zweiten
  Transfer.
- Gebietskisten behalten ihr vorhandenes „ALLES NEHMEN“-Verhalten.
- Normale Heimatlager zeigen den Button weiterhin nicht allein aufgrund dieses
  Fixes.
- Maus, Tastatur, Gamepad und Touch können die Aktion eindeutig erreichen und
  auslösen.
- Die automatisierte Abnahme prüft nicht nur die Existenz des Buttons, sondern
  Sichtbarkeit und vollständigen beziehungsweise partiellen Transfer mit einem
  `EnemyLootContainer`.

## F-009 — „GLEICHES ABLEGEN“ für Lagerkisten der Heimatbasis

**Status:** umgesetzt am 7. August 2026. `StorageWindow.AllowsDepositMatching`
gibt die Aktion ausschließlich für `StorageContainer` frei;
`StorageWindow.DepositMatching` bestimmt die erlaubten Gegenstandsarten
**einmal vor** dem Transfer, sodass eine Art, die erst während der Aktion Platz
findet, nicht nachrückt. Der Transfer selbst läuft unverändert über den
`ItemTransferService` (Stapel zuerst auffüllen, dann freie Plätze; Instanz-ID
und Haltbarkeit bleiben erhalten). Ein volles Ziel bricht die Schleife
bewusst nicht ab, damit eine andere Art noch einen angebrochenen Stapel
auffüllen kann. Der Button „GLEICHES ABLEGEN“ teilt sich die Position mit
„ALLES NEHMEN“ — beide schließen einander aus. Nachweis: fünf Fälle in
`StorageWindowDepositTests` inklusive voller Kiste ohne Verlust/Duplikat
(Logs `f009-red.log`/`f009-green.log`), Prefab neu gebaut über
`StorageContentBuilder.BuildWindowForFixes`. Abnahme über alle Eingabearten im
PlayMode steht aus.

**Bereich:** Heimatbasis, Lagerkisten und Inventartransfer

**Beobachtung:** Beim Öffnen einer Lagerkiste in der Heimatbasis fehlt eine
Sammelaktion, mit der passende Gegenstände aus dem Rucksack automatisch in die
Kiste einsortiert werden können. Der gewünschte Button trägt die Beschriftung
„GLEICHES ABLEGEN“.

**Analyse:** `StorageContainer` und `StorageWindow` unterstützen derzeit nur den
manuellen Einzeltransfer. Das Fenster besitzt außerdem „ÜBERTRAGEN“ und für
bestimmte Beutequellen „ALLES NEHMEN“, aber keine umgekehrte Sammelaktion nach
bereits vorhandenen Gegenstandsarten. Der allgemeine Transferservice beherrscht
das Auffüllen kompatibler Stapel, das Belegen freier Zielplätze sowie den Erhalt
von Haltbarkeit und Instanzdaten bereits. Es fehlt die Auswahlregel, die vor dem
Transfer ausschließlich jene Gegenstandsarten aus dem Inventar bestimmt, die
zu Beginn der Aktion schon in dieser Heimatlagerkiste vorhanden sind.

**Sollverhalten:** Das Fenster einer Lagerkiste in der Heimatbasis zeigt den
Button „GLEICHES ABLEGEN“. Ein einmaliges Betätigen prüft, welche
Gegenstandsarten zu diesem Zeitpunkt bereits mindestens einmal in der Kiste
liegen, und überträgt anschließend alle passenden Gegenstände aus dem Rucksack,
soweit die Kiste sie aufnehmen kann.

Vorhandene kompatible Stapel werden zuerst aufgefüllt. Verbleibende Mengen
dürfen danach freie Kistenplätze belegen, solange ihre Gegenstandsart bereits
vor Beginn der Aktion in der Kiste vertreten war. Gegenstandsarten, die vorher
nicht in der Kiste lagen, bleiben vollständig im Rucksack. Eine während der
Aktion neu angelegte Ablage erweitert die Auswahl deshalb nicht nachträglich.
Bei instanzierten oder haltbaren Gegenständen erfolgt die Zuordnung nach
Gegenstandsart; jede einzelne Instanz behält dabei ihre eigene Instanz-ID und
Haltbarkeit.

Der Button ist eine Funktion der dauerhaften Heimatlager und erscheint nicht
automatisch bei Gebietskisten oder Gegnerloot. Ist kein passender Gegenstand
vorhanden oder besitzt die Kiste keine nutzbare Kapazität, bleibt der Bestand
unverändert und die Oberfläche gibt eine eindeutige Rückmeldung.

**Abnahme:**

- „GLEICHES ABLEGEN“ ist beim geöffneten `StorageContainer` der Heimatbasis
  sichtbar und eindeutig bedienbar.
- Enthält die Kiste beispielsweise Holz, aber kein Erz, wird Holz aus dem
  Rucksack abgelegt; Erz bleibt im Rucksack.
- Passende vorhandene Stapel werden zuerst bis zu ihrer Maximalgröße
  aufgefüllt; weitere passende Mengen verwenden anschließend freie
  Kistenplätze.
- Ist nur ein Teil der passenden Menge unterbringbar, wird genau dieser Teil
  übertragen und der Rest bleibt unverändert im Rucksack.
- Eine volle Kiste oder vollständig gefüllte passende Stapel führen weder zu
  Verlusten noch zu Duplikaten.
- Verschiedene passende Gegenstandsarten werden innerhalb derselben Aktion
  verarbeitet; die Reihenfolge ändert nicht die zulässigen Gegenstandsarten.
- Gegenstandsarten, die vor dem Klick nicht in der Kiste vorhanden waren,
  werden auch dann nicht abgelegt, wenn während derselben Aktion ein Platz frei
  wird.
- Instanzgegenstände behalten Instanz-ID, Haltbarkeit und sonstige
  instanzbezogene Daten.
- Der geänderte Inhalt der Heimatlagerkiste wird persistent gespeichert und
  nach Speichern und Laden unverändert wiederhergestellt.
- Mehrfaches Betätigen während eines laufenden Transfers startet keinen zweiten
  parallelen Transfer.
- Gebietskisten und Gegnerloot zeigen den Button nicht aufgrund dieses Fixes.
- Maus, Tastatur, Gamepad und Touch können die Aktion eindeutig erreichen und
  auslösen.

## F-010 — Gebietskisten müssen aufrecht und grafisch eindeutig wirken

**Status:** zurückgestellt am 7. August 2026 — für die Kiste entsteht ein neues
3D-Modell mit eigener Öffnungsanimation. Die prozedurale Geometrie in
`WorldChestContentBuilder` wird dadurch ersetzt; eine Korrektur der jetzigen
Proportionen wäre verworfene Arbeit. Gemeinsam mit F-005 und F-006 zu behandeln
(beide betreffen dieselbe Kiste).

**Messbefund als Vorgabe für das neue Modell** (ermittelt am 7. August 2026,
damit er nicht verlorengeht):

- Die gewöhnliche Kiste misst 1,15 × 0,54 × 0,72 (B × H × T) und ist damit
  **tiefer als hoch**. Das ist die eigentliche Ursache des „umgefallen"-
  Eindrucks: Eine liegende Kiste ist tiefer als hoch, eine stehende umgekehrt.
  Gleiches gilt für die bewachte (1,45 × 0,70 × 0,88) und die versteckte Kiste
  (1,00 × 0,48 × 0,64).
- Als prüfbare Regel für das neue Modell: **Höhe > Tiefe**, und Höhe mindestens
  etwa 55 % der Breite.
- Eine Kippung durch den Spawn ist bestätigt ausgeschlossen:
  `ZoneWorldChestPopulator` instanziiert ausschließlich mit
  `Quaternion.Euler(0, RotationY, 0)`.
- Weiterhin offen und unabhängig vom Modell: Die Spawnpunkte tragen eine feste
  Y-Höhe; eine Anpassung des Kistenfußes an die lokale Bodenhöhe findet nicht
  statt (kein Raycast). Das gehört beim Einbau des neuen Modells ergänzt.

**Bereich:** Startgebiete, Weltkisten, 3D-Darstellung und Platzierung

**Beobachtung:** Die Kisten in den Startgebieten sehen grafisch unpassend aus.
Ihre Silhouette und Lage in der Welt vermitteln den Eindruck, als wären die
Kisten auf die Seite gefallen, statt stabil und aufrecht auf dem Boden zu
stehen.

**Analyse:** Die Weltkisten werden aus sehr einfacher prozedural erzeugter
Geometrie aufgebaut. Besonders die gewöhnliche Kiste ist im Verhältnis zu
ihrer Breite und Tiefe sehr niedrig. Deckel, Beschläge und Verschluss erzeugen
aus der Spielkamera keine ausreichend klare Vorder- und Oberseite. Dadurch wird
die breite Seitenfläche als Liegefläche gelesen und die Kiste wirkt umgefallen.

Eine unbeabsichtigte Kippung durch den Spawn ist nicht die primäre Ursache: Der
`ZoneWorldChestPopulator` erzeugt die Instanzen ausschließlich mit einer
Y-Drehung; X- und Z-Rotation bleiben null. Die Spawnpunkte besitzen allerdings
eine fest eingetragene Y-Höhe und es gibt keine sichtbare Anpassung des
Kistenfußes an die konkrete Bodenfläche. Einsinken, Schweben oder eine
ungünstige Geländekante können den falschen Eindruck deshalb zusätzlich
verstärken. Alle vier Startgebiete verwenden dieselben drei
`WorldChest_Common`, `WorldChest_Guarded` und `WorldChest_Hidden`-Prefabs; der
Fehler liegt damit in der gemeinsamen visuellen und räumlichen Darstellung und
nicht an einer einzelnen Kiste.

**Sollverhalten:** Gebietskisten besitzen eine klar lesbare, aufrechte
Kistensilhouette. Der Korpus steht mit seiner Unterseite vollständig auf dem
Boden, der Deckel liegt eindeutig oberhalb des Korpus und der Verschluss ist an
der Vorderseite erkennbar. Höhe, Tiefe und Beschläge sind so proportioniert,
dass die Kiste aus der tatsächlichen Spielkamera weder wie ein umgefallener
Behälter noch wie ein flacher Block wirkt.

Beim Platzieren wird der visuelle Kistenfuß auf der lokalen Bodenhöhe
ausgerichtet. Die Kiste darf nicht sichtbar schweben oder im Boden versinken.
Ihre aufrechte Weltachse bleibt erhalten; eine Geländeanpassung darf keine
seitliche Kippung erzeugen. Die vorhandene Y-Drehung darf weiterhin für eine
natürliche Ausrichtung im Gebiet verwendet werden, muss die erkennbare Front
aber sinnvoll zur zugänglichen Spielfläche orientieren.

Die drei Kistenfamilien bleiben über Größe, Material, Verstärkung und regionale
Details unterscheidbar. Die Überarbeitung verändert keine Lootmenge, keine
Interaktionsreichweite und keinen gespeicherten Öffnungszustand.

**Abnahme:**

- Gewöhnliche, bewachte und versteckte Kisten stehen in Greenwood, Marsch,
  Steinbruch und Glutruinen sichtbar aufrecht.
- Unterseite, Korpus, Deckel und Frontverschluss sind aus der normalen
  Spielkamera eindeutig voneinander zu unterscheiden.
- Keine Kiste wirkt aufgrund ihrer Proportionen wie ein auf die Seite gelegter
  oder umgefallener Behälter.
- Die Transformrotation besitzt beim Spawn keine unbeabsichtigte X- oder
  Z-Neigung.
- Der Kistenfuß berührt den Boden ohne sichtbares Schweben, Einsinken oder
  Durchschneiden der Geländeoberfläche.
- Die Front zeigt nicht unlesbar vom zugänglichen Bereich weg; Spieler erkennen
  ohne Kameradrehung, dass es sich um eine zu öffnende Kiste handelt.
- Geschlossener, geöffneter, teilweise geleerter und vollständig geleerter
  Zustand bleiben mit der überarbeiteten Geometrie korrekt lesbar.
- Deckelbewegung, Zwei-Hand-Öffnungsanimation, Collider und Interaktionshinweis
  sitzen nach der grafischen Überarbeitung weiterhin an der Kiste.
- Alle drei Kistenfamilien bleiben visuell unterscheidbar, ohne dass eine davon
  erneut wie ein liegendes Objekt wirkt.
- Eine Gegenprüfung in den späteren Gebieten stellt sicher, dass die gemeinsam
  verwendeten Prefabs dort nicht verschlechtert werden.
- Loot, Persistenz, Öffnungsdauer und Bedienung bleiben unverändert.

## F-011 — Einheitliche Treffer-, Lebens- und Staggeranzeige für alle Gegner

**Status:** umgesetzt am 7. August 2026. Die drei Beobachtungen hatten **zwei
verschiedene Ursachen**:

*Balken:* `WildlingStatusBars` und dessen Ereignisbindung waren korrekt. In
`WildEidra.prefab` fehlte den beiden Füllgrafiken jedoch das Sprite — dieselbe
Ursache wie in F-002, weshalb der Lebensbalken keinen Wert verlor. Die neun
regulären Gegner-Prefabs besaßen Sprites. Zusätzlich stand der Staggerbalken in
**allen zehn** Prefabs auf `fillAmount: 1` und damit vorab gefüllt.

*Schadenszahlen:* `CombatFeedback.SpawnDamage` wurde in `WildlingController`
und `BossController` je einzeln aufgerufen, in `EidraWildController` gar nicht.
Die TierTwo- und Forge-Gegner nutzen `WildlingController` und waren deshalb
zufällig versorgt — eine gemeinsame Kette gab es nicht.

**Behebung:** Der Aufruf ist in die gemeinsame Basis
`EnemyControllerBase.ApplyDamage` gewandert (genau ein Aufrufer, direkt nach
`OnDamageResolved`); die Höhe kommt aus der überschreibbaren
`DamageFeedbackHeight` (Garon 4,25; sonst 2,4). Die Einzelaufrufe in Wildling
und Boss sind entfernt, ihr übriges Feedback (Trefferkuh, Stilkuh, Hit-Flash)
bleibt. Wirkungslose Treffer unterdrückt `SpawnDamage` bereits selbst.
`FixCollectionEnemyBarBuilder.UpgradeEnemyStatusBars` hat alle zehn Prefabs
repariert (2 Füllgrafiken gesetzt, 10 Staggerbalken geleert); der
`EidraCaptureContentBuilder` erzeugt neue Balken gleich korrekt.

Nachweis: `EnemyFeedbackTests` — Füllgrafik und leerer Staggerstart über alle
zehn Prefabs, plus Strukturtest „genau ein Aufrufer, und zwar in der Basis"
gegen doppelte Zahlen (Logs
`f011-red.log`/`f011-builder.log`/`f011-green.log`). Die geforderte
Spielmodus-Abnahme mit echter Waffenhitbox über Wildling, Welt-Eidra,
regulären Gegner, Elite und Boss steht aus.

**Bereich:** Kampf, Welt-Eidra, Gegnerstatus und Trefferfeedback

**Beobachtung:** Ein Eidra in der Welt zeigt zwar einen Lebens- und einen
Staggerbalken, reagiert in der sichtbaren Kampfdarstellung aber nicht wie ein
Wildling. Bei Angriffen erscheinen keine Schadenszahlen. Der Lebensbalken
verliert trotz eines Treffers keinen sichtbaren Wert. Der Staggerbalken ist
bereits vor dem ersten Treffer gefüllt und baut sich durch Staggerschaden nicht
nachvollziehbar auf.

Dieses Verhalten soll nicht nur für Eidra korrigiert werden. Jeder angreifbare
Gegner muss dieselbe verlässliche Grundkette aus Trefferauflösung,
Lebenspunkten, Stagger und sichtbarem Feedback verwenden.

**Analyse:** `EidraWildController` basiert wie das Wildling auf
`EnemyControllerBase` und besitzt damit grundsätzlich die gemeinsamen
Lebens-, Schadens- und Staggerwerte. Der sichtbare Rückmeldepfad ist jedoch
nicht vereinheitlicht: `WildlingController.OnDamageResolved()` erzeugt über
`CombatFeedback.SpawnDamage()` Schadenszahlen und Trefferfeedback. Die
entsprechende Überschreibung des Welt-Eidra aktiviert nur den Kampf und prüft
die Fluchtbedingung; Schadenszahlen und gleichwertiges Trefferfeedback werden
dort nicht ausgelöst.

Auch die Balken sind nicht als allgemeine Gegneranzeige verdrahtet. Das
Welt-Eidra verwendet die Komponente `WildlingStatusBars`. Die im Prefab
gespeicherten Füllstände beginnen bei eins, während die korrekten Werte erst
durch Bindung und Ereignisse des Gegnercontrollers gesetzt werden sollen. Die
Anzeige kann dadurch mit einem vollen Staggerbalken oder einem veralteten
Lebenswert stehen bleiben, wenn Bindung und Gegnerinitialisierung nicht in der
erwarteten Reihenfolge stattfinden. UI und Kampfzustand besitzen somit keine
ausreichend abgesicherte gemeinsame Initialisierung.

Die vorhandene Spielmodusabnahme für Welt-Eidra schwächt das Ziel überwiegend
durch einen direkten Aufruf von `ApplyDamage()`. Sie prüft nicht die komplette
Spielerkette aus Waffenhitbox, Zielauflösung, tatsächlich angewendetem Schaden,
Balkenaktualisierung und Schadenszahl. Eine fehlerhafte Ziel- oder
Feedbackverdrahtung kann deshalb bestehen, obwohl die isolierte
Controllerlogik funktioniert.

**Sollverhalten:** Jeder erfolgreiche Spielerangriff auf ein angreifbares
Gegnerziel wird genau einmal durch dessen gemeinsamen Gegnercontroller
aufgelöst. Der tatsächlich angewendete Lebens- und Staggerschaden aktualisiert
im selben Treffer den internen Kampfzustand, die beiden Weltbalken und das
sichtbare Trefferfeedback.

Der Lebensbalken startet bei voller Gesundheit und sinkt proportional zum
verbleibenden Leben. Der Staggerbalken startet leer und füllt sich proportional
zum aktuellen Staggerwert. Bei Erreichen des Maximums wird der Gegner
gestaggert; nach Ablauf des Staggerzustands wird der Balken entsprechend der
geltenden Kampfregel wieder geleert. Rückkehr- oder Regenerationsregeln dürfen
Werte nur dann zurücksetzen, wenn auch der zugrunde liegende Gegnerzustand
zurückgesetzt wird.

Jeder erfolgreiche Treffer zeigt eine Schadenszahl für den tatsächlich nach
allen Modifikatoren angewendeten Lebensschaden. Angewendeter Staggerschaden
wird wie beim Wildling ebenfalls sichtbar ausgewiesen. Ein einzelner Treffer
darf weder doppelte Zahlen noch doppelte Schadensanwendung erzeugen. Verfehlte,
blockierte oder vollständig wirkungslose Angriffe dürfen keine falsche
Schadenszahl anzeigen.

Wilde Eidra folgen dabei weiterhin ihrer besonderen Fangregel: Sie sterben
nicht, sondern fliehen an ihrer vorgesehenen Lebensschwelle. Bis zu dieser
Schwelle müssen Lebensverlust, Stagger und Schadenszahlen jedoch vollständig
und korrekt sichtbar sein. Bei Flucht oder erfolgreichem Fang verschwinden die
Statusbalken zusammen mit dem Eidra.

Die gemeinsame Regel gilt mindestens für Wildlinge, Welt-Eidra, reguläre
Gebietsgegner, Elitegegner und Bosse. Individuelle Farben, Größen oder
Bossdarstellungen dürfen abweichen; die Bedeutung und Datenquelle der Werte
nicht.

**Abnahme:**

- Ein unversehrtes Welt-Eidra erscheint mit vollem Lebensbalken und vollständig
  leerem Staggerbalken.
- Ein echter Treffer mit jeder unterstützten Spielerwaffe reduziert die
  zugrunde liegenden Lebenspunkte und im selben Frame sichtbar den
  Lebensbalken.
- Staggerschaden erhöht den zugrunde liegenden Staggerwert und füllt den
  Staggerbalken proportional von links nach rechts.
- Bei vollem Staggerwert beginnt genau einmal der Staggerzustand; nach dessen
  Ende entsprechen interner Wert und sichtbarer Balken wieder derselben
  Rücksetzregel.
- Jeder erfolgreiche Treffer zeigt genau eine Schadensanzeige mit dem
  tatsächlich angewendeten Lebensschaden sowie gegebenenfalls dem angewendeten
  Staggerschaden.
- Schutz, Multiplikatoren, Rückentreffer und sonstige Modifikatoren werden vor
  der Anzeige berücksichtigt; die Zahl darf nicht den ungefilterten
  Ausgangswert der Waffe zeigen.
- Angriffe außerhalb von Reichweite oder Trefferwinkel verändern weder Leben
  noch Stagger und erzeugen keine Schadenszahl.
- Das Welt-Eidra wird durch den ersten Treffer wie vorgesehen feindlich und
  bleibt danach normal angreifbar.
- An der Fluchtschwelle sinkt der Lebensbalken bis zum zulässigen Restwert; das
  Eidra flieht anschließend, ohne einen falschen Todeszustand auszulösen.
- Flucht und erfolgreicher Fang entfernen Lebens- und Staggeranzeige
  zuverlässig aus der Welt.
- Dieselben Prüfungen laufen mindestens gegen ein Wildling, ein Welt-Eidra,
  einen regulären weiteren Gegner, einen Elitegegner und einen Boss.
- Die automatisierte Spielmodusabnahme greift die Ziele mit der echten
  Spielerwaffe und deren Hitbox an; ein direkter `ApplyDamage()`-Aufruf allein
  gilt nicht als ausreichender Nachweis.
- Mehrere Gegner in Trefferreichweite erhalten nur entsprechend der gültigen
  Treffergeometrie Schaden; Statusbalken und Zahlen gehören jeweils zum
  tatsächlich getroffenen Ziel.

## F-012 — Sichtbare Platzhalter und funktionslose Grafikobjekte aus den Gebieten entfernen

**Status:** umgesetzt am 7. August 2026. **Befundkorrektur:** Die Bereinigung
selbst war bereits erfolgt. Eine Prüfung aller neun ausgelieferten Szenen
ergab: keines der sechzehn alten Testhindernisse ist noch vorhanden, kein
sichtbarer Renderer nutzt das generische `EidrenRuntimeMaterial`, und es gibt
weder fehlende noch Unity-Standardmaterialien. Die vier Startgebiete und alle
weiteren Zone-Szenen enthalten überhaupt kein sichtbares Primitiv mehr.
Verbliebene Primitive gibt es nur in der Heimatbasis (`WalkableGround` als
Bodenfläche, `ContactShadow`-Quads) und im Gewölbe `EidraForge` (Böden, Wände,
Korridore) — alle mit gestalteten Materialien und beabsichtigt.

Offen war damit allein das im Auftrag benannte **Rückfallrisiko**: 
`EidrenSceneStructureBuilder` erzeugte die grauen Testkörper weiterhin, sodass
ein Neuaufbau der Szenen sie in bereits bereinigte Gebiete zurückgeholt hätte.

**Behebung:** `CreateMassiveTestObstacles` samt Hilfsmethode `CreateObstacle`
und der `SafeArea`-Zylinder der Heimatbasis sind aus dem Szenenbauer entfernt.
Es waren reine Kollisions- und Navigationstestkörper des Aufbaus; da sie in
keiner Szene mehr liegen, ändert der Wegfall weder Laufwege noch
NavMesh-Erreichbarkeit oder Kollisionsgrenzen, und es bleiben keine
unsichtbaren Kollisionen zurück. `AreaArtSceneBuilder.RemoveLegacy` bleibt als
Sicherung für Altstände bestehen.

Nachweis: `PlaceholderCleanupTests` — vollständige Namensliste (statt bisher
zwei Vertretern) über alle neun Szenen, Regel „kein sichtbares Weltobjekt mit
generischem Material" und Strukturprüfung gegen die Wiedereinführung im
Szenenbauer (Logs `f012-red.log`/`f012-green.log`). Die bildbasierte
Sichtprüfung aus der Spielkamera pro Gebiet steht aus.

**Bereich:** Weltgrafik, Gebietsaufbau und technische Platzhalter

**Beobachtung:** In der Spielwelt befinden sich noch sichtbare Grafikobjekte,
die keine erkennbare Funktion und keine verständliche Bedeutung besitzen. Das
gezeigte Beispiel ist ein langer, einfarbig dunkelgrauer Körper auf dem
Boden. Er wirkt weder wie ein fertig gestalteter Bestandteil des Gebiets noch
wie ein eindeutig benutzbares oder relevantes Weltobjekt.

**Analyse:** Das Beispiel lässt sich ohne Gebiet und Hierarchiepfad nicht einem
einzelnen Objekt eindeutig zuordnen. Seine Form und Darstellung entsprechen
jedoch sehr wahrscheinlich den älteren Testhindernissen des
`EidrenSceneStructureBuilder`. Dieser erzeugt für Außengebiete große
`Cube`- und `Cylinder`-Primitive mit einem generischen
`EidrenRuntimeMaterial`. Sie tragen Namen wie `LargeTree_Trunk`,
`LargeRock_Shelf`, `MassiveDeadTree`, `LargeObstacle_West` oder
`RuinsWall_West`, besitzen aber keine regional ausgearbeitete Grafik.

Diese Objekte hatten während des Szenenaufbaus einen technischen Zweck als
große Kollisions- und Navigationstestkörper. Für Spieler sind sie dennoch nur
unfertige graue Platzhalter ohne lesbare Weltfunktion. Der spätere
`AreaArtSceneBuilder` versucht die alten Objekte anhand ihrer Namen zu
entfernen. Ein erneuter Lauf des grundlegenden Szenenbuilders kann sie jedoch
wieder erzeugen. Die persistierte Prüfung verbietet außerdem nur einzelne
Vertreter wie `LargeObstacle_West` und `RuinsWall_West`, nicht die vollständige
Liste. Dadurch können weitere Testkörper bestehen bleiben oder nach einem
Rebuild zurückkehren.

Nicht jede Dekoration benötigt eine Interaktion. Ein Baum, Stein oder
Ruinenrest darf rein atmosphärisch sein, muss dann aber als absichtlich
gestalteter Bestandteil des Gebiets erkennbar sein. Fehlerhaft sind sichtbare
technische Hilfsobjekte, generische Primitive, fehlende Materialien sowie
Objekte, deren Darstellung eine Funktion verspricht, die nicht existiert.

**Sollverhalten:** In ausgelieferten Gebieten ist kein technischer Platzhalter
und kein sichtbares Testobjekt vorhanden. Wird ein Objekt ausschließlich für
Navigation, Spawnlogik, Trigger oder Kollisionsbegrenzung benötigt, besitzt es
keinen sichtbaren Renderer. Wird seine sichtbare Form für Orientierung oder
Levelgestaltung benötigt, wird es durch ein regional passendes, fertig
gestaltetes Weltobjekt ersetzt.

Alle sichtbaren Weltobjekte werden einer der folgenden Rollen eindeutig
zugeordnet:

- funktionales Spielobjekt mit korrekter Interaktion und verständlichem
  Feedback;
- bewusst gesetzte, grafisch fertige Dekoration oder Landmarke;
- unsichtbares technisches Hilfsobjekt ohne Renderer.

Objekte außerhalb dieser Rollen werden entfernt. Die Prüfung beschränkt sich
nicht auf das abgebildete Exemplar, sondern umfasst Heimatbasis, Startgebiete,
spätere Gebiete und Gewölbe. Der Aufbau muss unabhängig von der Reihenfolge der
Editor-Builder stabil bleiben und darf entfernte Platzhalter nicht erneut in
gespeicherte Szenen schreiben.

**Abnahme:**

- Das abgebildete lange graue Platzhalterobjekt ist in der betroffenen Szene
  nicht mehr sichtbar.
- Alle Namen der alten Testhindernisse werden nach dem vollständigen
  Inhaltsaufbau in jeder auslieferbaren Szene geprüft, nicht nur zwei
  ausgewählte Namen.
- Kein sichtbares Weltobjekt verwendet undifferenzierte graue
  Standardgeometrie oder ein fehlendes beziehungsweise generisches Material als
  endgültige Darstellung.
- Benötigte Kollisions- und Navigationskörper sind entweder unsichtbar oder
  deckungsgleich mit einer fertigen regionalen Grafik.
- Entfernte Testkörper hinterlassen keine unsichtbaren Kollisionen, an denen
  der Spieler oder Gegner unerklärlich hängen bleiben.
- Reine Dekoration ist als Baum, Fels, Ruine, Pflanze oder andere beabsichtigte
  Gebietsgrafik klar lesbar und erzeugt keinen falschen Interaktionshinweis.
- Kisten, Ressourcen, Loot, Ausgänge und andere funktionale Weltobjekte besitzen
  weiterhin ihre vorgesehene Interaktion und können nicht versehentlich als
  Dekoration entfernt werden.
- Ein Neuaufbau der Szenen und ein anschließender Neuaufbau der Gebietsgrafik
  führen zum selben bereinigten Ergebnis.
- Auch die umgekehrte beziehungsweise vollständige offizielle Build-Reihenfolge
  kann keine alten Testprimitive wieder sichtbar machen.
- Heimatbasis, Greenwood, Marsch, Steinbruch, Glutruinen, spätere Gebiete und
  Gewölbe werden jeweils aus der normalen Spielkamera auf isolierte
  Platzhalter, fehlende Materialien und funktionslose Restobjekte geprüft.
- Die Bereinigung verändert keine beabsichtigten Laufwege, Spawnpositionen,
  NavMesh-Erreichbarkeit oder Kollisionsgrenzen.

## F-013 — XP für ein im Kampf besiegtes Welt-Eidra vergeben und anzeigen

**Status:** umgesetzt am 7. August 2026. `ZoneEidraPopulator` verbucht beim
Übergang `Departed(Fled)` genau einmal die Belohnung aus der Gegnerdefinition
über `PlayerProgressionService.RecordEnemyDefeated`; die Einmaligkeit sichert
das Selbstabmelden des Departure-Handlers, Fang (`Captured`), Szenenabbau und
Despawn vergeben nichts. Terrock und Noctarion besitzen jetzt
`experienceReward: 60` (Einordnung zwischen RootCharger 45 und GraniteShell
65 — im Balancing gern anpassen). Die „+XP“-Anzeige hängt am vorhandenen
`ExperienceGained`-Ereignis. Ignivar bleibt bewusst bei 0: Er läuft in der
Eidra-Schmiede über den separaten Fangpfad ohne Fluchtbelohnung. Nachweis:
`EidraDepartureRewardTests` (Logs `f013-red.log`/`f013-green.log`).
PlayMode-Abnahme mit echter Spielerwaffe steht aus.

**Bereich:** Welt-Eidra, Kampfbelohnung, Spielerfortschritt und XP-Feedback

**Beobachtung:** Wird ein Eidra in der Spielwelt durch Angriffe besiegt,
scheint der Spieler keine Erfahrungspunkte zu erhalten. Am Charakter erscheint
keine „+XP“-Rückmeldung und ein Fortschritt ist nicht erkennbar.

**Analyse:** Fangbare Welt-Eidra besitzen absichtlich keinen normalen
Todespfad. `EidraWildController` begrenzt den Lebensverlust an der
Fluchtschwelle und löst anschließend `Departed(Fled)` aus. Für Spieler wirkt
dies wie das Besiegen des Gegners, technisch stirbt das Eidra jedoch nicht.

Der Abgang wird im `ZoneEidraPopulator` derzeit nur als abgearbeitete
Zoneninstanz gespeichert. `HandleDeparted()` markiert das Eidra unabhängig vom
Grund als verschwunden, ruft aber keine XP-Vergabe auf. Zusätzlich besitzen
die Daten von Terrock und Noctarion aktuell jeweils
`experienceReward: 0`. Damit existiert weder ein positiver Belohnungswert noch
ein Dienstaufruf, der ihn an `PlayerProgressionService` übergibt.

Die sichtbare Rückmeldung am Charakter hängt am Ereignis
`PlayerProgressionService.ExperienceGained`. Weil keine XP verbucht werden,
wird auch `PlayerExperienceFeedback` nicht ausgelöst. Die fehlende
beziehungsweise falsch platzierte dauerhafte XP- und Levelanzeige im HUD wird
bereits in F-001 und F-004 behandelt; dieser Fix stellt sicher, dass bei einem
besiegten Eidra überhaupt ein korrekter Fortschrittswert entsteht.

**Sollverhalten:** Erreicht ein wildes Eidra durch einen gültigen Angriff des
Spielers oder seiner spielereigenen Kampfquelle die Fluchtschwelle, gilt dies
für die Spielerprogression als „besiegt“. Der Spieler erhält genau einmal die
im zugehörigen `EnemyDefinition` hinterlegte, positive XP-Belohnung. Das Eidra
flieht weiterhin und erzeugt weder einen Leichnam noch Gegnerloot.

Die XP werden beim bestätigten Übergang zu `Departed(Fled)` verbucht, nicht bei
jedem Treffer und nicht erst beim Verlassen der Szene. Der aktuelle XP-Wert und
gegebenenfalls das Level ändern sich sofort. Gleichzeitig erscheint am
Charakter eine gut lesbare Rückmeldung „+<Wert> XP“, und die in F-004
geforderte XP-Leiste aktualisiert sich auf denselben Fortschrittsstand.

Ein erfolgreicher Fang löst `Departed(Captured)` aus und darf nicht
versehentlich dieselbe Kampfbelohnung erhalten. Falls das Fangen später eigene
XP geben soll, benötigt es eine separat balancierte Fangbelohnung; es darf
nicht denselben Ereignispfad doppelt verwenden. Szenenabbau, Zonenwechsel und
technisches Entfernen eines Eidra vergeben ebenfalls keine XP.

Der konkrete XP-Wert bleibt datengetrieben und wird im Balancing festgelegt,
muss für jedes im Kampf besiegbare Welt-Eidra aber größer als null sein. Die
Anzeige verwendet immer den tatsächlich von der Progression akzeptierten Wert,
insbesondere an Level- und Fortschrittsgrenzen.

**Abnahme:**

- Terrock, Noctarion und jedes weitere fangbare Welt-Eidra besitzen in ihrer
  Gegnerdefinition eine positive, explizit geprüfte XP-Belohnung.
- Das Erreichen der Fluchtschwelle durch einen echten Spielerangriff erhöht die
  gespeicherten Spieler-XP exakt um den konfigurierten beziehungsweise von der
  Progression akzeptierten Wert.
- Im selben Abschluss erscheint am Spieler genau einmal „+<Wert> XP“.
- Die dauerhafte XP-Leiste und Levelanzeige aus F-004 reagieren ohne
  Szenenwechsel auf denselben Wert.
- Reicht die Belohnung für einen Levelaufstieg, werden Rest-XP und neues Level
  korrekt berechnet und angezeigt.
- Mehrere Treffer im Abschlussframe, die Fluchtanimation und das anschließende
  Deaktivieren erzeugen keine zweite Belohnung.
- Verlassen und erneutes Betreten der Zone vergibt für dieselbe bereits
  geflohene Eidra-Instanz keine weiteren XP.
- Ein erfolgreich gefangenes Eidra erhält nicht zusätzlich die
  Kampf-Fluchtbelohnung.
- Szenenwechsel, technisches Despawnen oder ein Neuaufbau der Population ohne
  Kampfsieg vergeben keine XP.
- Das Eidra bleibt gemäß Fangregel am Leben, flieht und erzeugt weder Leichnam
  noch Lootbehälter.
- Die automatisierte Spielmodusabnahme besiegt das Eidra über die echte
  Spielerwaffe, prüft den Übergang `Fled`, die einmalige XP-Differenz, das
  `ExperienceGained`-Ereignis und die sichtbare Rückmeldung.
- Eine zusätzliche Abnahme fängt ein Eidra erfolgreich und weist nach, dass
  dabei keine unbeabsichtigte Kampfbelohnung entsteht.

## F-014 — Baumenü als kompakten und klar lesbaren Baukatalog darstellen

**Status:** überwiegend umgesetzt am 7. August 2026. **Ausgenommen sind die
Gebäudesymbole** — für Gebäude entstehen neue 3D-Modelle; daraus gerenderte
Icons ersetzen die heutigen Platzhalter später. Die beiden Kriterien zu
Lagerkistenbild und fachfremden Symbolen bleiben bis dahin offen. Befund dazu:
Es fehlen schlicht die Dateien `BLD_StorageChest_L01.png` und
`BLD_Workbench_L01.png`; `BuildingCostContentBuilder.IconPath` weicht deshalb
auf `ITEM_TMP_Plank.png` beziehungsweise `ITEM_TMP_Hammer.png` aus. Die
übrigen neun Gebäude besitzen ihr eigenes Bild.

**Umgesetzt:**

- *Layout:* Die Katalogreihe war ein 66 % hoher Block für eine 92 px hohe
  Kartenreihe — daher die große leere Fläche. Sie ist jetzt ein flacher
  Streifen oben; darunter links Details und Rückmeldung, rechts die
  Aktionsspalte. Ein Test prüft, dass sich keine zwei Bereiche überdecken.
- *Gewichtung und Benennung:* „Vorschau" heißt jetzt **PLATZIEREN** und ist
  höher als der Schließen-Knopf, der als sekundäre Aktion nach unten rückt.
  Einheitliche Großschreibung: PLATZIEREN, VERSCHIEBEN, ABREISSEN, SCHLIESSEN
  (vorher gemischt „Vorschau", „Abriss", „Schließen", „VERSCHIEBEN").
- *Kategorien:* `RowFor` reichte eine leere Kategorie durch, deshalb erschien
  keine Überschrift. Sie wird jetzt gesetzt, und zwar nur bei der ersten Zeile
  einer Kategorie als Gruppeneröffnung — dadurch wird der bereits vorhandene,
  bislang fehlschlagende Projekttest `ExactlyOneHeaderOpensEachCategory` grün.
- *Kosten:* Statt „Holz 10/70" nennt der Detailbereich je Zeile
  „Holz: 10 benötigt · 70 vorhanden"; eine Fehlmenge steht als Text dabei
  („es fehlen 6"), auf der Karte kompakt als „Holz 10/4 (fehlt 6)". Mangel ist
  damit nicht mehr nur farblich erkennbar.

Nachweis: `BuildingMenuLayoutTests` (acht Fälle: Beschriftungen, Gewichtung,
Streifenhöhe, Überschneidungsfreiheit, Kategorien, beide Kostenformate) sowie
die bestehenden `BuildingCatalogTests` (Logs
`f014-red.log`/`f014-builder.log`/`f014-green2.log`). Baukosten,
Freischaltungen, Platzierungs- und Abrissregeln sind unverändert.

**Nebenbefund — veralteter Projekttest:**
`BuildingCatalogTests.ALockedRowShowsItsRequirementInsteadOfCost` schlägt seit
jeher fehl und widerspricht der geltenden Regel: `RebuildVisible` entfernt
nicht freigeschaltete Baupläne vollständig aus dem Katalog (v0.2 F-010, im
Sollverhalten oben bestätigt), sodass eine gesperrte Zeile ihre Anforderung
gar nicht anzeigen kann. Der Test gehört an die Regel angepasst oder
gestrichen — bewusst nicht durch eine Änderung am Verhalten „repariert".

Offen bleiben außerdem die bildbasierte Abnahme (Ein-Karten-Zustand und voller
Katalog) sowie die Prüfung bei 16:9, 16:10 und schmaler mobiler Safe Area.

**Bereich:** Heimatbasis, Baumenü, Baukatalog und responsive UI

**Beobachtung:** Das aktuelle Baumenü nutzt den Bildschirm nicht sinnvoll. Der
Titel steht allein am oberen Rand, die Informationen zum gewählten Bauobjekt
schweben davon getrennt im oberen Bereich, und ein sehr kleines Gebäudebild
liegt isoliert weit links. Zwischen Auswahl, Details und den großen Buttons
entsteht eine ausgedehnte leere Fläche.

Im gezeigten Zustand ist nur die freigeschaltete Lagerkiste vorhanden. Statt
einer klaren Kistenkarte erscheint ein kleines, schwer zuzuordnendes Motiv.
Auswahlrahmen, Kategorie und Zusammenhang zwischen Bild und Beschreibung sind
nicht erkennbar. Die Aktionen „Vorschau“, „VERSCHIEBEN“ und „Schließen“ sind
unterschiedlich beschriftet und gewichtet; insbesondere der sehr große
Schließen-Button dominiert das eigentliche Bauen.

**Analyse:** Das Menü besteht aus einem älteren, fest positionierten
Vollbildlayout, das nachträglich um einen horizontalen Katalog erweitert wurde.
`BuildingMenuUiBuilder.UpgradeHorizontalLayout()` verändert die Anordnung der
Katalogeinträge, ordnet aber den umgebenden Katalogbereich, die Details und die
Aktionsleiste nicht als gemeinsames adaptives Layout neu. Der
`ContentSizeFitter` verkleinert die Reihe auf ihre tatsächlich sichtbaren
Einträge. Bei nur einem freigeschalteten Bauplan bleibt deshalb eine einzelne
300-Pixel-Karte in einer für viele Elemente vorgesehenen Vollbildfläche zurück.

Die Lagerkiste verwendet in `BuildingCostContentBuilder.IconPath()` aktuell
`ITEM_TMP_Plank.png` als Symbol. Das ist ein provisorisches Brett- beziehungsweise
Materialbild und keine eindeutige Gebäudegrafik der Lagerkiste. Außerdem gibt
`BuildingMenuWindow.RowFor()` für jede Zeile eine leere Kategorie zurück. Die
vorbereiteten Überschriften „Struktur“, „Werkstätten“, „Versorgung“ und
„Landwirtschaft“ können dadurch nicht erscheinen.

Die bisherige Freischaltungsregel aus v0.2 F-010 bleibt gültig: Noch nicht
freigeschaltete Gebäude werden nicht als gesperrte Karten gezeigt. Dass in
einem frühen Spielstand nur wenige Baupläne sichtbar sind, ist daher für sich
kein Fehler. Das Layout muss gerade diesen Zustand sinnvoll darstellen und mit
weiteren Freischaltungen sauber wachsen.

**Sollverhalten:** Das Baumenü erscheint als zusammenhängender, klar
begrenzter Baukatalog innerhalb der Safe Area. Titel, Kategorien, horizontale
Gebäudekarten, Detailbereich und Aktionen folgen einer erkennbaren visuellen
Hierarchie. Die Oberfläche bleibt kompakt und erzeugt weder bei einem einzelnen
Eintrag noch bei vollständigem Katalog ungenutzte, auseinandergerissene
Flächen.

Jeder sichtbare Bauplan besitzt eine ausreichend große Karte mit passendem
Gebäudesymbol, Namen und kompakter Kostenübersicht. Die aktuell gewählte Karte
ist über Rahmen beziehungsweise Marker eindeutig hervorgehoben. Die Kategorie
des Eintrags ist lesbar. Nicht freigeschaltete Baupläne bleiben entsprechend
der bestehenden Regel verborgen und hinterlassen weder Lücken noch leere
Kategorien.

Der Detailbereich liegt unmittelbar neben oder unter der Auswahl und zeigt:

- Name und Kategorie des Bauobjekts;
- eine erkennbare Vorschau des tatsächlichen Gebäudes;
- benötigte und vorhandene Menge jeder Ressource mit eindeutiger Leserichtung;
- Rastergröße und gegebenenfalls eine kurze Platzierungsinformation;
- einen verständlichen Grund, falls die Platzierung aktuell nicht begonnen
  werden kann.

Die bisherige Schreibweise „Holz 10/70“ wird entweder sichtbar erklärt oder
durch eine eindeutigere Darstellung wie „10 benötigt · 70 vorhanden“ ersetzt.
Fehlende Materialien werden zusätzlich textlich beziehungsweise symbolisch und
nicht ausschließlich durch Farbe markiert.

Die primäre Aktion zum Start der Bauplatzierung heißt einheitlich
„PLATZIEREN“ oder „BAUEN“ statt des technisch klingenden „Vorschau“.
„VERSCHIEBEN“ und „ABREISSEN“ sind als getrennte Aktionen für bereits gebaute
Objekte erkennbar und erscheinen beziehungsweise werden aktiv, wenn ihr Modus
sinnvoll gestartet werden kann. „SCHLIESSEN“ bleibt gut erreichbar, wird aber
visuell als sekundäre Aktion behandelt.

**Abnahme:**

- Bei genau einem freigeschalteten Bauplan bildet das Menü weiterhin eine
  kompakte, zusammenhängende Einheit ohne große ungenutzte Zwischenräume.
- Bei mehreren Bauplänen stehen die Karten gleichmäßig in einer horizontalen
  Reihe; zusätzliche Einträge sind per klar sichtbarem horizontalem Scrollen
  oder Blättern erreichbar.
- Verborgene, nicht freigeschaltete Gebäude erzeugen keine leeren Karten,
  Abstände oder Kategorien.
- Die Lagerkiste zeigt ein eindeutiges Lagerkistenbild und nicht mehr das
  provisorische Brett-Itembild.
- Alle übrigen Baupläne zeigen ebenfalls das zugehörige Gebäude und kein
  fachfremdes Ressourcen- oder Platzhaltersymbol.
- Auswahlkarte, Detailtext und Aktionen gehören visuell eindeutig zusammen und
  wechseln synchron mit der Auswahl.
- Die Kategorien der sichtbaren Einträge werden korrekt aus den Gebäudedaten
  angezeigt; leere Kategorieüberschriften erscheinen nicht.
- Benötigte und vorhandene Ressourcenmengen sind ohne Vorwissen über die
  Reihenfolge der Zahlen verständlich.
- Materialmangel ist außer durch Farbe auch durch Text, Symbol oder Status
  erkennbar.
- Die primäre Platzierungsaktion ist eindeutig benannt und stärker gewichtet
  als Verschieben, Abreißen und Schließen.
- Verschieben und Abreißen sind nicht mit dem Neubau einer ausgewählten Karte
  verwechselbar und geben bei einem ungültigen Ziel einen konkreten Hinweis.
- Einheitliche Großschreibung, Schriftgrößen, Abstände, Buttonhöhen und
  Farbhierarchie entsprechen den übrigen Eidren-Menüs.
- Maus, Tastatur, Gamepad und Touch können Karten, Scrollbereich und alle
  Aktionen vollständig erreichen; der Fokus bleibt sichtbar.
- Das Layout wird mindestens bei 16:9, 16:10 und einer schmalen mobilen Safe
  Area geprüft. Texte, Karten und Buttons überdecken oder verlassen einander
  nicht.
- Die grafische Überarbeitung verändert keine Baukosten, Freischaltungen,
  Platzierungsregeln, Abrissregeln oder gespeicherten Gebäudezustände.
- Eine bildbasierte Abnahme prüft den gerenderten Ein-Karten-Zustand und einen
  vollständig freigeschalteten Katalog; reine Referenzprüfungen des Prefabs
  gelten nicht als ausreichender Nachweis.
