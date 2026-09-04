# HUD-000 – Vorbereitung des Mobile-HUD- und Combat-Control-Upgrades

**Status:** verbindlicher Vorbereitungsauftrag  
**Projekt:** Eidren  
**Zielplattform:** Smartphone im Landscape-Format  
**Referenzstand der Bestandsprüfung:** 20.08.2026  
**Referenzvideo:** `ScreenRecording_08-20-2026 03-26-08_1.MP4`  
**Nicht Bestandteil dieses Auftrags:** Implementierung, Umbau produktiver Prefabs, Änderung von Gameplaywerten oder Erzeugung finaler Art-Assets

---

## 1. Auftrag

Das Mobile-HUD- und Combat-Control-Upgrade für Eidren wird fachlich, gestalterisch und technisch so vorbereitet, dass die spätere Implementierung auf dem realen Projektstand aufbaut und nicht auf veralteten Annahmen.

Diese Phase liefert eine verbindliche Umsetzungsgrundlage. Sie analysiert den vorhandenen Code, das bestehende HUD-Prefab, die Datenmodelle, Szenen, UI-Assets, Tests und das Referenzvideo. Daraus entstehen ein abgestimmtes UX-Konzept, Wireframes für relevante Smartphone-Formate, eine Zielarchitektur, eine Targeting-Spezifikation, ein Asset- und Animationsplan sowie ein priorisierter Implementierungs- und Testplan.

In dieser Phase werden ausdrücklich keine produktiven HUD-, Combat-, Input-, Prefab-, Szenen- oder Assetänderungen vorgenommen.

## 2. Ziel der Vorbereitung

Nach Abschluss dieses Auftrags müssen alle wesentlichen Entscheidungen für die spätere Umsetzung geklärt und dokumentiert sein:

- Welche vorhandenen Systeme werden unverändert weiterverwendet?
- Welche Komponenten werden erweitert oder refaktoriert?
- Welche neuen Komponenten sind tatsächlich notwendig?
- Wie sieht das HUD in Exploration, normalem Kampf und Bosskampf aus?
- Wie werden alle aktuellen Waffenfamilien und Eidra unterstützt?
- Wie funktionieren automatische Zielauswahl, Zielstabilität und Auto-Facing genau?
- Wie verhalten sich Kampf- und Kontextaktionen zueinander?
- Welche neuen Icons, Portraits, Rahmen und Animationen werden benötigt?
- Wie werden Touch-Ergonomie, Safe Areas, Seitenverhältnisse und Performance nachgewiesen?
- In welcher Reihenfolge kann die Umsetzung mit möglichst geringem Regressionsrisiko erfolgen?

Das Ergebnis ist kein loses Ideendokument, sondern eine prüfbare Spezifikation für die anschließende Implementierungsphase.

## 3. Verbindlicher aktueller Projektstand

Die folgenden Erkenntnisse ersetzen ältere Annahmen des ursprünglichen HUD-Auftrags.

### 3.1 HUD-Architektur

Das aktuelle HUD ist bereits als authored Prefab mit mehreren Presentern aufgebaut. `CombatHUD.cs` ist eine Bindungs- und Validierungsschicht und erzeugt die Oberfläche nicht monolithisch zur Laufzeit.

Relevante vorhandene Komponenten sind insbesondere:

- `CombatHUD`
- `CombatHudStatusPresenter`
- `CombatHudActionPresenter`
- `CombatHudActionSlot`
- `CombatHudInputPresenter`
- `CombatHudInteractionPresenter`
- `CombatHudResultPresenter`
- `InteractionButton`
- `VirtualJoystick`
- `SafeAreaPanel`
- `MinimapPresenter`
- `AbilityRangePreview`
- `HudAbilityTooltip`

Diese Aufteilung ist grundsätzlich wiederverwendbar. Eine vollständige Neuentwicklung des HUD-Systems ist nicht vorgesehen.

### 3.2 Waffen

Der aktuelle Stand ist nicht auf Hammer und Dolche beschränkt.

Vorhanden sind drei Waffenfamilien:

- Hammer
- Dolche
- Speer

Zusätzlich existieren mehrere Waffenvarianten innerhalb dieser Familien. Der Spieler führt weiterhin zwei ausgerüstete Waffenplätze und kann unmittelbar zwischen ihnen wechseln.

Das neue HUD muss deshalb datengetrieben mit beliebigen Kombinationen aus zwei ausgerüsteten Waffen funktionieren. Die Synergie `Hammer → Stagger → Dolche → Backstab` bleibt wichtig, darf aber nicht durch eine feste Hammer-/Dolch-Verdrahtung andere gültige Ausrüstungen oder den Speer beschädigen.

### 3.3 Eidra

Der aktuelle Stand enthält mindestens:

- Terrock
- Noctarion
- Ignivar

Jedes Eidra besitzt zwei Fähigkeiten. Das aktive Gespann hat bis zu zwei Plätze; in der Spielwelt ist nur das aktuell ausgewählte Eidra aktiv dargestellt. Der Spieler kann weitere gefangene Eidra über die bestehende Gespannverwaltung zuweisen.

Das HUD darf daher nicht fest auf Terrock und Noctarion begrenzt werden. Portrait, Akzentfarbe, Passivinformation und beide Skill-Slots müssen aus den Daten des aktuell aktiven Eidra entstehen.

### 3.4 Context Actions

Ein kontextabhängiges Interaktionssystem ist bereits vorhanden. Es unterstützt:

- automatische Zielsuche mit Priorität, Distanz, Blickrichtung und Hysterese;
- Instant-, Hold- und Timed-Interaktionen;
- dynamische Verben und Icons;
- Ressourcen, Loot, Weltgegenstände, Truhen, Stationen und weitere Interactables;
- Fortschrittsanzeige;
- Abbruch bei Angriff, Dodge, Schaden, Tod, Zielverlust und Szenenwechsel;
- eine eigene neutrale Zielmarkierung für Interaktionsobjekte.

Der spätere HUD-Umbau soll dieses System präsentieren und bei Bedarf erweitern, nicht parallel neu implementieren.

### 3.5 Combat Targeting und Auto-Facing

Aktuell existieren:

- eine Registry kampffähiger Ziele;
- eine einfache Suche nach dem nächsten Ziel innerhalb einer Reichweite;
- eine leichte automatische Ausrichtung zu Angriffsbeginn, sofern das Ziel innerhalb des Assistenzwinkels liegt;
- gesonderte Feldzielsuche für Eidra-Fähigkeiten.

Noch nicht vorhanden ist ein dauerhaft ausgewähltes, gewichtetes und für HUD, Angriffe und Fähigkeiten gemeinsam nutzbares Combat Target mit stabiler Zielbindung.

### 3.6 Boss- und Combat-Feedback

Bereits vorhanden sind:

- Boss-HP;
- Boss-Stagger;
- Bosszustände;
- Stagger-Events und Stagger-Dauer im Gegnercode;
- gepoolte World-Space-Texte, Trefferimpulse und Auren;
- normales Schadens-, Backstab- und Stagger-Feedback.

Noch zu spezifizieren sind insbesondere:

- normales Target-HUD für Feldgegner;
- eigener Combat Target Ring;
- Krit-, `MISS`- und `RESIST`-Kategorien;
- Kollisionsvermeidung bei mehreren Zahlen;
- Stagger-Fenster und kontextabhängiger Waffenhinweis;
- klare Trennung zwischen ausgewähltem Ziel, getroffenem Ziel und Interaktionsziel.

### 3.7 Mobile-Konfiguration

Vorhanden sind:

- `SafeAreaPanel` auf Basis von `Screen.safeArea`;
- ein `CanvasScaler` mit Referenzauflösung 1920 × 1080 und höhenorientierter Skalierung;
- ein Touch-Joystick;
- Umschaltung zwischen Touch-, Tastatur-/Maus- und Gamepad-Darstellung;
- Tests für den authored HUD-Prefab und Teile der Touch-Flächen.

Die aktuellen Player Settings erlauben jedoch weiterhin Portrait-Ausrichtungen. Die spätere Umsetzungsphase muss eine verbindliche Landscape-Konfiguration vorsehen. In diesem Vorbereitungsauftrag wird die notwendige Änderung nur dokumentiert.

### 3.8 Minimap

Eine funktionsfähige Minimap ist bereits vorhanden. Sie wird nicht als hypothetischer Platzhalter behandelt. In den Wireframes ist sie als bestehendes System zu berücksichtigen und nur dann zu reduzieren oder auszublenden, wenn dies für den jeweiligen HUD-Zustand begründet ist.

## 4. Erkenntnisse aus dem Referenzvideo

Das Referenzvideo wurde vollständig betrachtet. Die Auswertung bezieht sich ausschließlich auf allgemeine Mobile-UX-Prinzipien. Grafiken, Marken, Figuren und konkrete Assets des Referenzspiels dürfen nicht übernommen werden.

### 4.1 Räumliche Verteilung

- Der virtuelle Bewegungsstick liegt isoliert links unten.
- Der rechte untere Bereich bildet einen kompakten Daumencluster.
- Eine Primäraktion ist deutlich größer als die umliegenden Aktionen.
- Kleinere Aktionen liegen mit kurzen Daumenwegen ring- beziehungsweise wabenförmig um die Primäraktion.
- Spieler- und Zielstatus liegen kompakt am oberen Rand.
- Die Bildschirmmitte bleibt überwiegend der Spielwelt vorbehalten.
- Die große Questanzeige des Referenzspiels ist für Eidren zu dominant und soll kompakter interpretiert werden.

### 4.2 Zielauswahl

- Gegner werden ohne pixelgenaues Antippen ausgewählt.
- Das aktuelle Ziel erhält einen klaren roten, segmentierten Bodenring.
- Interaktionsobjekte und NPCs verwenden eine neutrale helle Auswahlmarkierung.
- Name und kompakte Statusleiste des ausgewählten Ziels erscheinen oben.
- Ziele wechseln automatisch, wenn das bisherige Ziel nicht mehr plausibel ist.
- Die Figur orientiert sich bei Angriffen zum ausgewählten Gegner, ohne dass ein zweiter Zielstick benötigt wird.

Für Eidren soll die Zielbindung stabiler und nachvollziehbarer spezifiziert werden als im sichtbaren Referenzverhalten. Ein gültiges Ziel darf nicht bei kleinen Bewertungsunterschieden springen.

### 4.3 Kontextinteraktionen

- Der große kontextabhängige Aktionsplatz wechselt unter anderem zwischen Hand, Werkzeug und Gespräch.
- Nicht verfügbare Aktionen werden direkt in der Welt erklärt, beispielsweise durch einen benötigten Werkzeugtyp.
- Es erscheinen keine zusätzlichen permanenten Spezialbuttons für jede Objektart.
- Die Kontextaktion verschwindet wieder, sobald kein passendes Ziel vorhanden ist.

Eidren besitzt die dafür nötige technische Grundlage bereits. Zu klären ist nur die endgültige Platzierung und die Priorität gegenüber gleichzeitig möglichem Kampf.

### 4.4 Trefferfeedback

- Schadenszahlen erscheinen unmittelbar am Ziel.
- Ausgehender Schaden ist farblich von erlittenem Schaden getrennt.
- `Verfehlt` erscheint als eigene, neutrale Rückmeldung.
- Mehrere Zahlen werden leicht versetzt dargestellt.
- Texte erscheinen sofort, bleiben ungefähr 0,5 bis 1 Sekunde sichtbar und verschwinden ohne lange Animation.
- Kurze Trefferreaktionen und kleine Impact-Effekte ergänzen die Zahlen.

### 4.5 Grenzen der Videoreferenz

Das Video zeigt normale Kämpfe, Ressourcen, NPCs und Weltinteraktionen. Es zeigt keinen Bosskampf mit Stagger-HUD, keinen Eidra-Wechsel und keine für Eidren geeignete Darstellung mehrerer Fähigkeiten.

Boss-HUD, Eidra-HUD, Waffenwechsel und Stagger-Kommunikation müssen daher aus den bestehenden Eidren-Systemen und der eigenen Art Direction entwickelt werden.

## 5. Verbindliche UX-Leitlinien für die Planung

### 5.1 Bildschirmmitte

Die Mitte gehört dem Spiel. Spieler, aktives Eidra, Gegner, Telegraphen, Projektile, Trefferfeedback und Interaktionsziele müssen erkennbar bleiben.

### 5.2 Linker Daumen

Links unten befindet sich ausschließlich der Bewegungsstick. Permanente Kampf- oder Verwaltungsaktionen werden nicht um den Stick gruppiert.

### 5.3 Rechter Daumen

Rechts unten entsteht ein zusammenhängender Combat Cluster mit folgenden Prioritäten:

1. Hauptangriff als größte Aktion;
2. Dodge als jederzeit erreichbare Sekundäraktion;
3. alternative ausgerüstete Waffe als kleiner Weapon-Swap-Button;
4. Portrait des aktiven Eidra als Wechselbutton;
5. genau zwei Skillbuttons des aktiven Eidra;
6. situationsabhängige Context Action;
7. wichtige Consumables in einer untergeordneten Ebene.

Die Vorbereitung muss klären, ob Context Action und Hauptangriff getrennte, überlappungsfreie Plätze erhalten oder ob der Kontextplatz nur außerhalb konkreter Kampfgefahr die Primärposition übernehmen darf. Normaler Kampf darf durch eine nahe Ressource, Leiche oder Truhe nicht blockiert werden.

### 5.4 Waffenanzeige

- Der Hauptangriff zeigt die tatsächlich aktive Waffe beziehungsweise deren Familie.
- Der kleinere Swap-Button zeigt die tatsächlich ausgerüstete alternative Waffe.
- Beim Wechsel tauschen aktive und alternative Darstellung eindeutig ihre Rollen.
- Das Konzept muss Hammer, Dolche, Speer und zukünftige Waffenfamilien unterstützen.
- Während eines Stagger-Fensters darf der alternative Button nur dann einen Dolchhinweis erhalten, wenn dort tatsächlich Dolche ausgerüstet sind.
- Ist bereits eine Dolchwaffe aktiv, darf kein sinnloser Wechselhinweis erscheinen.

### 5.5 Eidra-Anzeige

- Das Portrait des aktiven Eidra dient als Wechselbutton.
- Nur die zwei Fähigkeiten des aktiven Eidra sind sichtbar.
- Skill-Icons, Cooldowns, Akzentfarbe und optionale Passivinformation kommen aus den Daten.
- Der bestehende Wechsel-Cooldown bleibt relevant und muss im Konzept sichtbar werden.
- Terrock, Noctarion, Ignivar und zukünftige Eidra müssen ohne neue HUD-Sonderlogik darstellbar sein.

### 5.6 HUD-Zustände

Mindestens drei Zustände sind zu konzipieren:

**Exploration**

- Spielerstatus
- Bewegungsstick
- Waffenstatus beziehungsweise Hauptaktion
- relevante Context Action
- wichtige Consumables
- Minimap und kompakte Zielanzeige

**Combat**

- zusätzlich Combat Target Ring
- normales Target-HUD
- Dodge
- aktives Eidra und zwei Skills
- Combat Feedback

**Boss Combat**

- Bossname
- Boss-HP
- Boss-Stagger
- relevante Bosszustände
- Stagger-Fenster
- situationsabhängiger Waffenhinweis

Die Übergänge müssen ohne harte Layoutsprünge, wechselnde Daumenpositionen oder störende Vollbildanimationen funktionieren.

## 6. Arbeitsumfang dieser Vorbereitungsphase

### Arbeitspaket A – Bestandsaufnahme

Alle relevanten Systeme werden mit ihrem tatsächlichen aktuellen Verhalten dokumentiert.

Mindestens zu prüfen sind:

- `CombatHUD.cs`
- alle `CombatHud*Presenter`
- `CombatHUD.prefab`
- `PlayerCombatController.cs`
- `CombatTargeting.cs`
- `PlayerInputReader.BuildingPointer.cs`
- `PlayerMotor.cs`
- `EidraTeamController.cs`
- `BossController.cs`
- `EnemyControllerBase.cs`
- `InteractionController.cs`
- `InteractionTargetSelector.cs`
- `InteractionButton.cs`
- `ResourceTargetIndicator.cs`
- `ResourceNode.cs`
- `CombatFeedback.cs`
- `VirtualJoystick.cs`
- `AbilityRangePreview.cs`
- `ConsumableController.cs`
- `SafeAreaPanel.cs`
- `MinimapPresenter.cs`
- Waffen-, Eidra-, Fähigkeits-, Gegner- und Bossdaten
- relevante Editor- und PlayMode-Tests
- Player Settings und Build-Szenen
- vorhandene UI-, Waffen- und Eidra-Art-Assets

Ergebnis ist eine Matrix mit den Spalten:

| System | Existiert | Wiederverwenden | Erweitern | Refaktorieren | Neu | Kritische Verträge |
|---|---|---|---|---|---|---|

### Arbeitspaket B – Verbindliche UX-Spezifikation

Für jedes HUD-Element werden dokumentiert:

- Zweck;
- Sichtbarkeitszustand;
- Priorität;
- Anker und ungefähre Größe;
- Touch-Ziel;
- Datenquelle;
- Eingabeereignis;
- Cooldown- oder Sperrzustand;
- Animation;
- Verhalten bei fehlenden Daten;
- Verhalten bei Touch, Tastatur/Maus und Gamepad.

### Arbeitspaket C – Wireframes

Zu erstellen sind Wireframes für:

- Exploration ohne Interaktionsziel;
- Exploration mit Ressource;
- Exploration mit NPC oder Truhe;
- normaler Kampf mit einem Gegner;
- normaler Kampf mit mehreren Gegnern;
- Bosskampf gegen Garon;
- aktives Stagger-Fenster;
- Waffenwechsel;
- Eidra-Wechsel;
- Skill-Cooldown;
- deaktivierte oder nicht verfügbare Aktion.

Jeder relevante Zustand wird mindestens für folgende Formate geprüft:

- 1920 × 1080 beziehungsweise 16:9;
- 2340 × 1080 beziehungsweise 19,5:9;
- 2400 × 1080 beziehungsweise 20:9;
- jeweils mit repräsentativer linker und rechter Safe-Area-Einrückung.

Die Wireframes müssen die sichtbare Spielwelt, typische Gegnerpositionen und Telegraphenflächen mit darstellen. Reine UI-Flächen ohne Spielkontext reichen nicht aus.

### Arbeitspaket D – Targeting-Spezifikation

Es wird ein konkretes, testbares Targeting-Modell beschrieben.

Mindestens zu berücksichtigen sind:

- Entfernung zur erreichbaren Trefferfläche, nicht nur zum Transform-Mittelpunkt;
- Winkel zur Blick- oder Bewegungsrichtung;
- aktuelle Waffenreichweite;
- aktuelles Ziel;
- letzter angegriffener oder getroffener Gegner;
- Gegnerzustand und Lebensstatus;
- Boss-Priorität ohne harte Boss-Sperre;
- Sichtbarkeit beziehungsweise sinnvolle Erreichbarkeit;
- Zielstabilität und Hysterese;
- Verhalten beim Weglaufen;
- Verhalten bei Tod, Deaktivierung oder Szenenwechsel;
- Verhalten bei mehreren Zielen auf ähnlicher Bewertung;
- gemeinsamer oder koordinierter Zielbezug für normale Angriffe und Eidra-Fähigkeiten.

Die Spezifikation enthält:

- Bewertungsfaktoren und deren Priorität;
- Bedingungen für Zielbeibehaltung;
- Bedingungen für Zielwechsel;
- Bedingungen für Zielverlust;
- Auto-Facing-Winkel und maximale Rotationshilfe;
- Ausschluss automatischer Bewegung und automatischer Angriffe;
- deterministische Testfälle.

### Arbeitspaket E – Target Ring und Target-HUD

Zu planen sind eigenständige Eidren-Varianten für:

- normalen Gegner;
- Elitegegner;
- Boss;
- neutrales Interaktionsziel.

Combat Target Ring und Interaction Indicator bleiben semantisch und technisch unterscheidbar. Beide dürfen nicht gleichzeitig widersprüchliche Ziele kommunizieren.

Für das normale Target-HUD sind mindestens festzulegen:

- Anzeigename;
- HP;
- relevante Zustände;
- Sichtbarkeitsdauer nach Zielverlust;
- Verhalten bei Zielwechsel;
- Verhalten bei fehlendem Anzeigenamen.

Für Bosse kommen Stagger und tatsächlich relevante Bosszustände hinzu.

### Arbeitspaket F – Combat-Feedback-Spezifikation

Zu definieren sind:

- normaler Schaden;
- kritischer Schaden, sofern mechanisch vorhanden;
- Backstab;
- Stagger-Schaden;
- `MISS`;
- `RESIST`;
- Spielerschaden;
- Heilung und Buffs.

Für jede Kategorie werden Text, Farbe, Größe, Position, Versatz, Dauer und Priorität festgelegt. Die bestehende Pooling-Lösung wird als Ausgangspunkt betrachtet.

Es ist ausdrücklich zu prüfen, ob `MISS`, `RESIST` und Krit derzeit echte Gameplayereignisse besitzen. Das HUD darf keine Zustände erfinden, die der Combat-Code nicht zuverlässig meldet.

### Arbeitspaket G – Art- und Asset-Plan

Vorhandene Eidren-Assets werden auf gemeinsame Merkmale untersucht:

- Silhouette;
- Formensprache;
- Liniengewicht;
- Materialgefühl;
- Detailgrad;
- Farbsystem;
- Licht und Schatten;
- Perspektive;
- Lesbarkeit bei realer Buttongröße.

Danach entsteht eine Assetliste mit Status:

- vorhanden und verwendbar;
- vorhanden, aber zu überarbeiten;
- fehlt als Prototyp;
- fehlt als finales Asset.

Mindestens zu berücksichtigen sind:

- alle Waffenfamilien und benötigten Varianten;
- Dodge;
- Portraits für Terrock, Noctarion und Ignivar;
- alle sechs aktuellen Eidra-Fähigkeiten;
- Interaktion;
- Abbauen beziehungsweise Werkzeugkontexte;
- Loot;
- Heiltrank;
- Bufffood;
- Target Rings;
- normale, Elite- und Bossrahmen;
- Cooldownmaske und Sperrzustand.

Für fehlende Assets werden präzise Spezifikationen und Image-Generation-Prompts vorbereitet. In dieser Phase werden noch keine finalen Icons erzeugt.

Mindestens zwei eigenständige Rahmenrichtungen sind zu vergleichen:

- leicht polygonal beziehungsweise hexagonal;
- organischer Fantasy-/Survival-Rahmen.

Die Auswahl wird anhand der bestehenden Eidren-Art begründet.

### Arbeitspaket H – Animations- und Zustandsplan

Für folgende HUD-Reaktionen werden Dauer, Intensität und Auslöser festgelegt:

- Button-Press;
- Weapon Swap;
- Eidra-Wechsel;
- radialer Cooldown;
- Aktion wieder verfügbar;
- Stagger-Fenster;
- Dolchhinweis;
- Trefferimpuls;
- Zielwechsel;
- Ein- und Ausblenden der HUD-Zustände.

Animationen müssen unmittelbar, kurz und funktional bleiben. Dauerhaftes starkes Pulsieren, große Bounce-Bewegungen und dekorative Partikel über dem HUD sind ausgeschlossen.

### Arbeitspaket I – Architektur- und Migrationsplan

Die Vorbereitung beschreibt eine Zielarchitektur, ohne sie bereits umzusetzen.

Zu prüfen sind unter anderem folgende Verantwortlichkeiten:

- HUD-Zustandssteuerung;
- Player Status;
- normales Target-HUD;
- Boss-HUD;
- Combat Action Cluster;
- Weapon Action Slot;
- Eidra-HUD;
- Ability Slot;
- Context Action;
- Damage Number Pool;
- Objective-HUD;
- Combat Target Selection;
- Combat Target Indicator.

Bestehende Presenter werden bevorzugt erweitert oder sinnvoll geteilt. Neue Komponenten werden nur eingeführt, wenn eine klare Verantwortung nicht in die vorhandene Struktur passt.

Der Migrationsplan muss die authored-Prefab-Regel respektieren: Produktive UI-Hierarchien werden nicht als Laufzeit-Fallback mit `new GameObject` erzeugt.

### Arbeitspaket J – Test- und Abnahmeplan

Der spätere Testplan umfasst mindestens:

- Bewegung und Joystick;
- Dodge und Ausdauer;
- alle drei Waffenfamilien;
- zwei beliebige gleichzeitig ausgerüstete Waffen;
- Weapon Swap während und außerhalb von Angriffen;
- Hammer-Kombos;
- Dolch-Kombos und Backstab;
- Speer-Kombos und Reichweite;
- Stagger;
- Terrock, Noctarion und Ignivar;
- alle sechs aktuellen Eidra-Fähigkeiten;
- Eidra-Wechsel und Wechsel-Cooldown;
- Consumables;
- Resource Nodes;
- Instant-, Hold- und Timed-Interaktionen;
- Loot, Truhen, NPCs und Stationen;
- normales Targeting mit einem und mehreren Gegnern;
- Targeting bei großen Gegnern;
- Garon inklusive HP und Stagger;
- Sieg, Niederlage und Restart;
- Tastatur/Maus, Gamepad und Touch;
- 16:9, 19,5:9 und 20:9;
- Safe Areas;
- Speicher-/Lade- und Szenenwechselregressionen;
- CPU-, Speicher-, Canvas-Rebuild- und Allokationsprofiling.

## 7. Systeme, die nicht beschädigt werden dürfen

- bestehende Waffenfamilien, Waffenvarianten und Ausrüstungsplätze;
- Waffenprogression, Crafting, Inventar und Savegame-Zuordnung;
- Combo-, Weapon-Switch- und Input-Buffer-Logik;
- Backstab- und Stagger-Berechnung;
- Speerreichweite und Waffensignaturen;
- alle drei aktuellen Eidra und ihre Fähigkeiten;
- Eidra-Roster, Gespannzuweisung und aktives Eidra;
- Wechsel- und Skill-Cooldowns;
- Bosszustände und Garons Angriffsschleife;
- Interaction Sessions und deren Abbruchregeln;
- Ressourcenabbau, Loot, Container und Weltgegenstände;
- Multiplattform-Input für Touch, Tastatur/Maus und Gamepad;
- Minimap;
- bestehende Safe-Area-Unterstützung;
- authored HUD-Prefab und seine Validierung;
- bestehende EditMode- und PlayMode-Regressionstests;
- Bau-, Crafting-, Inventar-, Technologie- und Gespannfenster.

## 8. Nichtziele

Dieser Vorbereitungsauftrag umfasst nicht:

- Implementierung des neuen HUDs;
- Änderung von Kampfschaden, Staggerwerten, Reichweiten oder Cooldowns;
- Entfernung von Speer oder Ignivar;
- Umbau der Weltstruktur oder Progression;
- Neuentwicklung des Eidra-Systems;
- Austausch vorhandener Bossangriffe;
- finale Icon-Produktion;
- Änderung produktiver Szenen oder Prefabs;
- spontane Umbenennung bestehender Klassen oder Dateien;
- eine funktionslose Ersatz-Minimap;
- Kopieren konkreter Grafiken oder Layoutdetails des Referenzspiels.

## 9. Technische Risiken, die in der Vorbereitung zu klären sind

1. Normale Angriffe und Eidra-Fähigkeiten verwenden derzeit unterschiedliche Zielpfade.
2. Die aktuelle Combat-Zielsuche ist reichweitenbasiert und besitzt kein persistentes Zielmodell.
3. Distanzmessung zum Transform-Mittelpunkt ist bei großen Gegnern bereits als Problem bekannt.
4. Combat Target Ring und Interaction Indicator könnten ohne Prioritätsregeln widersprüchliche Ziele zeigen.
5. Das aktuelle Boss-HUD ist eng an einen übergebenen Boss gebunden; normale Gegner besitzen kein entsprechendes HUD.
6. Die Action-Presenter-Struktur enthält bereits viele serialisierte Referenzen und darf nicht erneut zu einem großen Sammelblock anwachsen.
7. Bestehende Prefab-Builder und Tests erwarten konkrete Namen, Größen und Referenzen.
8. Touch-Tooltips über langes Halten können mit Skill-Aktivierung und Range Preview konkurrieren.
9. Die vorhandene Combat-Feedback-Poolung erzeugt World-Space-Text; zusätzliche Kategorien und Versatzregeln müssen ohne neue Laufzeitallokationen funktionieren.
10. Portrait- und Icon-Assets sind für den erweiterten aktuellen Inhalt nicht vollständig als mobile Endfassungen vorhanden.
11. Die Player Settings erlauben Portrait, obwohl das Zielbild Landscape verlangt.
12. Das HUD muss neben Combat auch vorhandene Bau-, Inventar-, Crafting-, Technologie- und Gespannfenster konfliktfrei unterstützen.

## 10. Erwartete Deliverables

Am Ende dieser Vorbereitungsphase werden geliefert:

1. Kurzbericht zum realen Projektstand;
2. Wiederverwenden-/Erweitern-/Refaktorieren-/Neu-Matrix;
3. dokumentierte Abweichungen gegenüber dem ursprünglichen Auftrag;
4. vollständige UX-Spezifikation aller HUD-Elemente;
5. Wireframes für Exploration, Combat und Boss Combat;
6. Varianten für 16:9, 19,5:9, 20:9 und Safe Areas;
7. Targeting- und Auto-Facing-Spezifikation;
8. Spezifikation für Combat Target Ring und normales Target-HUD;
9. Boss-HUD- und Stagger-Fenster-Spezifikation;
10. Context-Action-Prioritätsregeln;
11. Combat-Feedback-Spezifikation;
12. Art-Direction-Ableitung und Assetinventar;
13. Liste fehlender Icons, Portraits und Rahmen;
14. Prompts und technische Anforderungen für fehlende Assets;
15. HUD-Animationsplan;
16. Zielarchitektur und Migrationsplan;
17. priorisierter Implementierungs-Backlog mit Abhängigkeiten;
18. Regressionstest- und Mobile-Abnahmeplan;
19. Risikoliste mit Gegenmaßnahmen;
20. Liste aller Entscheidungen, die vor der Implementierung freigegeben werden müssen.

## 11. Abnahmekriterien der Vorbereitungsphase

Der Vorbereitungsauftrag ist abgeschlossen, wenn:

- alle Aussagen auf dem real vorhandenen Projektstand beruhen;
- Speer, Waffenvarianten und Ignivar vollständig berücksichtigt sind;
- kein Dokument mehr von nur zwei fest verdrahteten Waffen- oder Eidra-Typen ausgeht;
- bestehende Context-, Minimap-, Safe-Area-, Cooldown- und Pooling-Systeme korrekt eingeordnet sind;
- Exploration, Combat und Boss Combat als prüfbare Zustände beschrieben sind;
- alle wichtigen Elemente konkrete Anker, Größenhierarchien und Touch-Ziele besitzen;
- das Targeting-Verhalten deterministisch testbar beschrieben ist;
- normale Gegner, Eliten, Bosse und Interaktionsziele visuell unterscheidbar sind;
- die Priorität zwischen Kampf und Kontextinteraktion eindeutig ist;
- alle Wireframes auf den geforderten Seitenverhältnissen funktionieren;
- die Zielarchitektur vorhandene Komponenten bevorzugt weiterverwendet;
- der Umsetzungs-Backlog in risikoarmen, einzeln testbaren Schritten vorliegt;
- keine produktive Projektdatei außerhalb der Dokumentation verändert wurde;
- keine HUD-, Combat-, Input-, Prefab-, Szenen- oder Art-Implementierung vorgenommen wurde.

## 12. Empfohlene Reihenfolge der späteren Umsetzung

Diese Reihenfolge ist in der Vorbereitung zu validieren und anschließend als separate Implementierungsaufträge auszuarbeiten:

1. gemeinsames persistentes Combat-Target-Modell;
2. Combat Target Ring und normales Target-HUD;
3. HUD-Zustandssteuerung;
4. neues responsive Layout des authored HUD-Prefabs;
5. Weapon- und Dodge-Cluster;
6. datengetriebenes Eidra-HUD;
7. Context-Action-Integration und Prioritätsregeln;
8. Boss-HUD und Stagger-Kommunikation;
9. erweitertes Combat Feedback;
10. finale Icons, Portraits, Rahmen und Animationen;
11. Mobile-Tests, Profiling und vollständige Regression;
12. Abnahmescreenshots für Exploration, normalen Kampf und Garon.

Jeder Schritt benötigt eine eigene Abnahme und muss rückbaubar bleiben. Ein Big-Bang-Austausch des vollständigen HUDs ist ausgeschlossen.

