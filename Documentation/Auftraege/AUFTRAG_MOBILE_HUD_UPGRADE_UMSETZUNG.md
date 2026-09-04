# HUD-100 – Umsetzung des Mobile-HUD- und Combat-Control-Upgrades

**Status:** verbindlicher Produktionsauftrag  
**Projekt:** Eidren  
**Zielplattform:** Smartphone im Landscape-Format  
**Ausgangsbasis:** aktueller Projektstand vom 20.08.2026  
**Vorbereitungsauftrag:** `Documentation/Auftraege/AUFTRAG_MOBILE_HUD_UPGRADE_VORBEREITUNG.md`  
**UX-Referenz:** `ScreenRecording_08-20-2026 03-26-08_1.MP4`

---

## 1. Auftrag

Das bestehende HUD und die Kampfbedienung von Eidren werden zu einer responsiven, touchgerechten Mobile-Oberfläche weiterentwickelt.

Die Umsetzung baut auf den vorhandenen Systemen auf. Bereits funktionierende Mechaniken, Datenmodelle, Presenter, Context Actions, Cooldowns, Minimap, Safe-Area-Unterstützung und Combat-Feedback-Pools werden weiterverwendet und gezielt erweitert. Ein vollständiger Neubau des HUDs oder der Kampfmechanik ist ausdrücklich nicht vorgesehen.

Das neue HUD soll nicht mehr alle verfügbaren Funktionen gleichzeitig als gleichwertige Buttons präsentieren. Es zeigt genau die Aktionen und Informationen, die im aktuellen Zustand relevant sind.

Der zentrale Spielfluss bleibt erhalten und wird besser kommuniziert:

**Bewegen → Gegner lesen → ausweichen → geeignete Waffe einsetzen → Stagger-Fenster erkennen → Waffe wechseln → Backstab oder Reichweitenvorteil nutzen → Eidra-Fähigkeiten kombinieren.**

## 2. Zielbild

Nach Abschluss dieses Auftrags besitzt Eidren:

- ein echtes Landscape-Mobile-HUD statt eines verkleinerten Desktop-HUDs;
- einen kompakten Combat Cluster für den rechten Daumen;
- einen dominanten Hauptangriff mit Icon der aktiven Waffe;
- einen unmittelbar verständlichen Wechsel zur zweiten ausgerüsteten Waffe;
- einen gut erreichbaren Dodge-Button;
- ein datengetriebenes HUD für das aktive Eidra und seine zwei Fähigkeiten;
- kontextabhängige Interaktionen ohne unnötige permanente Spezialbuttons;
- eine stabile automatische Gegnerauswahl ohne zweiten Zielstick;
- leichtes Auto-Facing bei selbst ausgelösten Angriffen;
- einen klaren Combat Target Ring;
- ein kompaktes Target-HUD für normale Gegner und Eliten;
- ein Boss-HUD mit HP, Stagger und relevanten Zuständen;
- verständliches Treffer-, Backstab-, Stagger- und Fehlschlagfeedback;
- Exploration-, Combat- und Boss-Combat-Zustände;
- responsive Layouts für 16:9, 19,5:9 und 20:9;
- sichere Touch-Ziele innerhalb der jeweiligen Safe Area;
- eine erweiterbare Architektur ohne neue HUD-Monolithen;
- automatisierte Regressionstests und dokumentierte Mobile-Abnahme.

## 3. Verbindliche aktuelle Baseline

Die Umsetzung darf nicht auf ältere Annahmen zurückfallen.

### 3.1 Waffen

Eidren besitzt aktuell drei Waffenfamilien:

- Hammer;
- Dolche;
- Speer.

Innerhalb der Familien existieren mehrere Varianten. Zwei Waffen können gleichzeitig ausgerüstet sein. Das HUD muss jede gültige Kombination datengetrieben darstellen.

Die bekannte Synergie `Hammer → Stagger → Dolche → Backstab` bleibt ein wichtiger Spezialfall, ist aber keine fest verdrahtete globale Ausrüstung. Ein Stagger-Hinweis auf Dolche darf nur erscheinen, wenn tatsächlich eine Dolchwaffe im alternativen Slot liegt.

### 3.2 Eidra

Mindestens folgende Eidra sind aktuell vorhanden:

- Terrock;
- Noctarion;
- Ignivar.

Jedes besitzt zwei Fähigkeiten. Das aktive Gespann hat bis zu zwei Plätze; nur das aktuell ausgewählte Eidra ist gleichzeitig aktiv in der Spielwelt. Das HUD muss mit allen vorhandenen und zukünftigen Eidra funktionieren, ohne für jedes neue Eidra Sondercode im Presenter zu benötigen.

### 3.3 Vorhandene Systeme

Folgende Systeme werden als Ausgangspunkt weiterverwendet:

- authored `CombatHUD.prefab`;
- `CombatHUD` als Bindungsschicht;
- bestehende `CombatHud*Presenter`;
- `PlayerInputReader` und Multiplattform-Eingaben;
- `PlayerCombatController` und Weapon-Switch-Buffer;
- `PlayerMotor`, Dodge und Ausdauer;
- `EidraTeamController` und Wechsel-/Skill-Cooldowns;
- `InteractionController`, `InteractionTargetSelector` und `InteractionButton`;
- Instant-, Hold- und Timed-Interaktionen;
- `BossController`, `EnemyControllerBase` und deren HP-/Stagger-Events;
- `CombatTargetRegistry`;
- `CombatFeedback` und bestehende Object Pools;
- `SafeAreaPanel`;
- `MinimapPresenter`;
- vorhandene UI-, Waffen-, Eidra- und Fähigkeitsassets;
- bestehende EditMode- und PlayMode-Tests.

## 4. Verbindliche UX-Regeln

### 4.1 Die Bildschirmmitte gehört dem Spiel

Spieler, aktives Eidra, Gegner, Telegraphen, Projektile, Trefferfeedback und Interaktionsobjekte müssen jederzeit erkennbar bleiben. Große permanente UI-Flächen in der Bildschirmmitte sind ausgeschlossen.

### 4.2 Linker Daumen

Links unten befindet sich ausschließlich der Bewegungsstick. Um den Stick werden keine permanenten Kampf- oder Verwaltungsbuttons angeordnet.

### 4.3 Rechter Daumen

Rechts unten liegt der Combat Cluster. Die wichtigsten Aktionen müssen ohne Umgreifen erreichbar sein.

Priorität und Größenhierarchie:

1. Hauptangriff – größte Aktion;
2. Dodge – zweitwichtigste jederzeit verfügbare Kampfaktion;
3. zweite ausgerüstete Waffe – kleiner als Angriff;
4. aktives Eidra – Portrait und Wechselbutton;
5. zwei Fähigkeiten des aktiven Eidra;
6. Context Action;
7. Consumables und seltenere Aktionen.

### 4.4 Keine Kopie des Referenzspiels

Aus dem Referenzvideo werden nur Prinzipien übernommen:

- klare Größenhierarchie;
- kurze Daumenwege;
- automatische Zielauswahl;
- Bodenmarkierung;
- kompakte Zielanzeige;
- versetzte Trefferzahlen;
- situationsabhängige Interaktion;
- randorientierte HUD-Verteilung.

Nicht übernommen werden konkrete Grafiken, Rahmen, Marken, Farben, Figuren oder exakte Buttonformen.

## 5. Umsetzungspaket 1 – Sicherheitsnetz und Bestandsbeleg

Vor produktiven Änderungen wird der aktuelle Zustand reproduzierbar festgehalten.

### Aufgaben

- vollständigen relevanten EditMode- und PlayMode-Ausgangslauf durchführen;
- vorhandene HUD-, Weapon-Switch-, Eidra-, Interaction-, Boss- und Minimap-Tests identifizieren;
- Ausgangsscreenshots für Exploration, normalen Kampf und Garon sichern;
- aktuelle Prefab-Hierarchie und serialisierte Referenzen dokumentieren;
- vorhandene Touch-Flächen, Canvas-Skalierung und Safe-Area-Verhalten erfassen;
- bestehende offene oder bewusst übersprungene Tests dokumentieren;
- eine Datei- und Systemmatrix `unverändert / erweitert / ersetzt / neu` anlegen.

### Abnahme

- Ausgangstests sind dokumentiert;
- bestehende Fehler sind von neuen Regressionen unterscheidbar;
- produktive Änderungen beginnen erst nach einem belastbaren Ausgangsbeleg.

## 6. Umsetzungspaket 2 – Persistentes Combat-Targeting

Die einfache Suche nach dem nächsten Ziel wird zu einem stabilen, gemeinsam nutzbaren Combat-Target-System erweitert.

### 6.1 Verantwortung

Das System verwaltet genau ein aktuelles Combat Target oder kein Ziel. Es stellt einen Snapshot und Ereignisse für HUD, Angriffe, Fähigkeiten und Target Ring bereit.

Der Snapshot enthält mindestens:

- Zielidentität;
- Transform beziehungsweise sinnvollen Zielpunkt;
- Anzeigename;
- Kategorie normal, Elite oder Boss;
- aktuelle und maximale HP;
- aktuelle und maximale Staggerwerte, sofern vorhanden;
- Gegnerzustand;
- Lebens- und Aktivitätsstatus;
- Entfernung zur erreichbaren Trefferfläche;
- Zeitpunkt der Auswahl beziehungsweise letzten Bestätigung.

### 6.2 Kandidatenfilter

Ein Ziel ist nur gültig, wenn es:

- lebt;
- aktiv ist;
- nicht zum Spieler gehört;
- grundsätzlich angreifbar ist;
- innerhalb der konfigurierten Erfassungsreichweite liegt;
- einen gültigen Zielpunkt besitzt;
- nicht durch Szenenwechsel oder Rückkehrzustand ungültig geworden ist.

Die Reichweite soll bei großen Gegnern nicht nur zum Transform-Mittelpunkt gemessen werden. Collider, definierter Zielradius oder ein vergleichbarer erreichbarer Oberflächenabstand sind zu berücksichtigen.

### 6.3 Bewertung

Die Auswahl berücksichtigt mindestens:

- Entfernung;
- Winkel zur Blick- oder Bewegungsrichtung;
- Reichweite der aktiven Waffe;
- bisheriges Ziel;
- zuletzt angegriffenes oder getroffenes Ziel;
- Bossrelevanz;
- sinnvolle Erreichbarkeit;
- Zielzustand.

Ein Boss erhält eine Gewichtung, aber keine harte globale Sperre. Ein weit entfernter Boss darf keinen direkt erreichbaren Feldgegner blockieren.

### 6.4 Zielstabilität

Das aktuelle Ziel bleibt bestehen, solange es gültig und ausreichend plausibel ist. Ein Kandidat übernimmt nur bei einem klaren Bewertungsvorteil oder wenn das aktuelle Ziel ungültig wird.

Erforderlich sind:

- Bewertungs-Hysterese;
- kurze Verlust-Gnadenzeit;
- deterministische Gleichstandsauflösung;
- sofortiger Verlust bei Tod oder Deaktivierung;
- nachvollziehbarer Wechsel beim Weglaufen;
- kein Flackern zwischen ähnlich positionierten Gegnern.

### 6.5 Gemeinsame Nutzung

Normale Angriffe und Eidra-Fähigkeiten sollen denselben plausiblen Zielbezug verwenden. Fähigkeitsreichweiten und spezielle Zielbedingungen bleiben erhalten. Eine Fähigkeit darf ein gemeinsames Ziel ablehnen und nach klar definierten Regeln ein alternatives gültiges Ziel suchen, aber nicht unbemerkt dauerhaft einen zweiten konkurrierenden Zielzustand pflegen.

### 6.6 Performance

- keine LINQ- oder Listenallokationen pro Frame;
- vorhandene Registry oder wiederverwendbare Buffer nutzen;
- Bewertung in sinnvoller Frequenz durchführen;
- Zielereignisse nur bei tatsächlicher Änderung auslösen;
- Profiler-Beleg für den Mehrgegnerfall liefern.

### Tests

- ein Gegner vor dem Spieler;
- Gegner hinter dem Spieler;
- zwei ähnlich weit entfernte Gegner;
- deutlich besserer neuer Kandidat;
- aktuelles Ziel kurz außerhalb des Optimalwinkels;
- Ziel außerhalb der Waffenreichweite;
- Wechsel der aktiven Waffe;
- Boss plus naher Feldgegner;
- Ziel stirbt oder wird deaktiviert;
- Spieler läuft weg;
- großer Gegner mit Mittelpunkt außerhalb, aber Trefferfläche innerhalb der Reichweite;
- deterministisches Verhalten unabhängig von Registry-Reihenfolge.

## 7. Umsetzungspaket 3 – Auto-Facing

Das vorhandene Auto-Facing zu Angriffsbeginn wird an das persistente Combat Target angebunden und touchgerecht abgestimmt.

### Regeln

- Auto-Facing erfolgt nur aufgrund einer vom Spieler ausgelösten Kampfaktion;
- keine automatische Bewegung;
- kein automatischer Angriff;
- kein dauerhaftes Verfolgen während freier Bewegung;
- keine harte 180-Grad-Drehung zu einem unplausiblen Ziel;
- maximale Assistenzwinkel und Rotationsstärke kommen aus konfigurierbaren Daten oder klar benannten Einstellungen;
- Dodge-Richtung bleibt von der Spielereingabe bestimmt;
- Root-, Combo- und Weapon-Switch-Timing bleiben erhalten.

### Tests

- Angriff leicht neben dem Ziel;
- Angriff am Rand des Assistenzwinkels;
- Ziel hinter dem Spieler;
- Angriff ohne Ziel;
- Bewegung in Gegenrichtung;
- Dodge unmittelbar vor und nach Angriff;
- Hammer-, Dolch- und Speerkombos.

## 8. Umsetzungspaket 4 – Combat Target Ring

Das aktuelle Combat Target erhält eine eigene Bodenmarkierung. Sie ist technisch und visuell vom vorhandenen Interaction Indicator getrennt.

### Varianten

- normaler Gegner: schlichter bedrohlicher Ring;
- Elite: markantere, aber nicht größere Informationsdichte;
- Boss: größer und stärker gewichtet;
- ungültiges oder verlorenes Ziel: kurzes, dezentes Ausblenden.

### Anforderungen

- vollständig eigenständige Eidren-Grafik;
- klare Lesbarkeit über unterschiedlichen Böden;
- keine Sichtbehinderung des Gegners;
- korrekter Bodenbezug bei unterschiedlichen Gegnergrößen;
- kein Z-Fighting;
- keine neue Materialinstanz pro Zielwechsel;
- Combat Ring und Interaction Ring dürfen nicht widersprüchlich gleichzeitig dominieren;
- Ring folgt dem Ziel ohne sichtbares Springen;
- Pooling oder Wiederverwendung einer einzigen Indicator-Instanz.

### Tests

- normaler Gegner, Elite und Boss;
- bewegliches Ziel;
- Ziel auf geneigtem oder erhöhtem Boden;
- Ziel hinter Vegetation;
- schneller Zielwechsel;
- Zielverlust;
- gleichzeitiges Interaktionsziel in der Nähe.

## 9. Umsetzungspaket 5 – Target-HUD und Boss-HUD

### 9.1 Normales Target-HUD

Wenn ein normaler Gegner oder eine Elite ausgewählt ist, erscheint kompakt am oberen Rand:

- Anzeigename;
- HP-Leiste;
- nur tatsächlich relevante Zustandsicons;
- optional Stagger, wenn dies für diesen Gegnertyp spielmechanisch sinnvoll und lesbar ist.

Die Anzeige verschwindet bei Zielverlust mit einem kurzen Übergang. Sie darf nicht bei jedem kleinen Zielwechsel das gesamte obere Layout verschieben.

### 9.2 Boss-HUD

Bei aktivem Bosskampf zeigt das HUD:

- Bossname, beispielsweise `GARON`;
- HP;
- Stagger;
- relevante Zustände wie `STAGGERED` oder `ENRAGED`, nur wenn sie echte Mechanik repräsentieren;
- optional verbleibende Stagger-Zeit, wenn sie zuverlässig aus dem Bosszustand bereitgestellt werden kann.

Das Boss-HUD darf nicht ausschließlich an eine beim HUD-Start fest übergebene Instanz gekoppelt bleiben, wenn Szenen oder Begegnungen einen späteren Bosswechsel erfordern.

### 9.3 Stagger-Fenster

Wenn ein Ziel gestaggert ist:

- reagiert die Stagger-Anzeige sichtbar;
- erscheint kurz ein kompakter Status;
- kann die verbleibende Zeit dezent dargestellt werden;
- wird eine ausgerüstete alternative Dolchwaffe subtil hervorgehoben;
- erscheint keine große Tutorialmeldung;
- wird kein Hinweis auf eine nicht ausgerüstete Waffe erzeugt.

### Tests

- normaler Gegner;
- Elite;
- Garon;
- Zielwechsel normal → Elite → Boss;
- Bosskampf beginnt und endet;
- Stagger füllt, startet, läuft und endet;
- Boss stirbt;
- Ziel ohne Anzeigename;
- HUD-Bindung nach Szenenwechsel.

## 10. Umsetzungspaket 6 – HUD-Zustandssteuerung

Eine kleine, eindeutige Zustandssteuerung verwaltet die Sichtbarkeit relevanter HUD-Gruppen.

### Exploration

Sichtbar:

- Joystick bei Touch;
- Spielerstatus;
- Hauptangriff beziehungsweise Waffenstatus;
- relevante Context Action;
- wichtige Consumables;
- Minimap;
- kompakte Objective-Anzeige.

Reduziert oder verborgen:

- normales Target-HUD;
- Boss-HUD;
- unnötige Kampfzustände.

### Combat

Zusätzlich sichtbar:

- Combat Target Ring;
- normales Target-HUD;
- Dodge;
- aktives Eidra;
- zwei Eidra-Fähigkeiten;
- Combat Feedback.

### Boss Combat

Zusätzlich beziehungsweise priorisiert:

- Boss-HUD;
- Boss-Stagger;
- relevante Bosszustände;
- Stagger-Fenster;
- Objective-Anzeige weiter reduziert.

### Übergänge

- keine wechselnden Positionen der primären Touch-Aktionen;
- keine harten Layoutsprünge;
- kurze Alpha-/Scale-Übergänge;
- Eingaben werden nicht während rein visueller Übergänge verschluckt;
- Gameplay-Disable, Pause, Inventar, Crafting, Technologie, Bauen und Eidra-Gespannfenster bleiben berücksichtigt.

## 11. Umsetzungspaket 7 – Responsives HUD-Prefab

Das bestehende authored `CombatHUD.prefab` wird schrittweise neu angeordnet. Laufzeit-Fallbacks, die UI-Hierarchien mit `new GameObject` aufbauen, bleiben verboten.

### 11.1 Player Status

Kompakt am oberen Rand:

- HP;
- Ausdauer;
- optional Portrait;
- Progression nur so groß, wie sie im Kampf sinnvoll lesbar bleibt.

### 11.2 Minimap

- bestehendes System weiterverwenden;
- vorzugsweise oben links;
- Safe Area einhalten;
- nicht künstlich vergrößern;
- keine Konkurrenz mit Player Status oder Objective.

### 11.3 Objective

Maximal kompakte Darstellung, beispielsweise:

`Eisen sammeln  3/8`

oder:

`Besiege Garon`

Keine große permanente Questbox im Kampf.

### 11.4 Canvas und Safe Area

- 16:9, 19,5:9 und 20:9 unterstützen;
- linke und rechte Aussparungen berücksichtigen;
- Dynamic Island, Notches und abgerundete Ecken aussparen;
- Player Settings verbindlich auf unterstützte Landscape-Ausrichtungen konfigurieren;
- Orientierung und Safe Area auf Android und iOS prüfen;
- Canvas-Skalierung anhand realer Touch-Größe und nicht nur Editor-Pixel bewerten.

### 11.5 Touch-Ziele

Regelmäßig genutzte Aktionen müssen eine ausreichende effektive Touch-Fläche besitzen:

- Angriff;
- Dodge;
- Weapon Swap;
- Eidra-Wechsel;
- Skill 1;
- Skill 2;
- Context Action.

Visuelle Form und Raycast-Fläche dürfen unterschiedlich groß sein, sofern Überlappungen ausgeschlossen und die Grenzen nachvollziehbar bleiben.

## 12. Umsetzungspaket 8 – Combat Cluster und Waffenanzeige

### 12.1 Hauptangriff

- größter Button im Cluster;
- zeigt die aktive Waffe beziehungsweise deren familiengerechtes Icon;
- keine generische Schwertgrafik;
- direkte Anbindung an vorhandenes Attack-Input-Ereignis;
- klare Zustände für verfügbar, gesperrt, gedrückt und Combo-Fenster;
- keine unnötige Textbeschriftung.

### 12.2 Weapon Swap

- kleinerer Button oberhalb oder schräg oberhalb des Hauptangriffs;
- zeigt die tatsächlich ausgerüstete alternative Waffe;
- bei Wechsel tauschen aktive und alternative Darstellung unmittelbar ihre Rollen;
- vorhandener Switch-Buffer bleibt erhalten;
- gesperrter Zustand bei fehlender Zweitwaffe;
- Hammer, Dolche, Speer und Varianten werden datengetrieben aufgelöst.

### 12.3 Dodge

- kleiner als Angriff, aber größer als Nebenaktionen;
- unmittelbar erreichbar;
- klare Silhouette;
- sichtbarer Ausdauer-/Sperrzustand;
- kein überdetailliertes Icon;
- tatsächliches Dodge-Verhalten bleibt im `PlayerMotor`.

### 12.4 Stagger-Hinweis

Der Dolchhinweis verwendet eine kurze, dezente Kombination aus Glow und sanftem Pulsieren. Er endet sofort, wenn:

- Stagger endet;
- Ziel verloren geht;
- Dolche aktiv werden;
- alternative Waffe keine Dolche mehr ist;
- Gameplay deaktiviert wird.

## 13. Umsetzungspaket 9 – Datengetriebenes Eidra-HUD

### 13.1 Portrait und Wechsel

- Portrait des aktiven Eidra ist zugleich Wechselbutton;
- Wechsel aktualisiert Portrait, Akzent, Skills und optionale Passivinformation;
- Wechsel-Cooldown wird visuell dargestellt;
- fehlendes Portrait besitzt einen Eidren-eigenen neutralen Fallback;
- keine falsche Zuordnung von Fähigkeitsicons als Portraitersatz.

### 13.2 Fähigkeiten

Sichtbar sind genau die zwei Fähigkeiten des aktiven Eidra.

Aktuell müssen unterstützt werden:

- Terrock: Felsbrecher und Steinhaut;
- Noctarion: Schattenschritt und Rückenmal;
- Ignivar: beide aktuell hinterlegten Fähigkeiten aus den Daten.

Anforderungen:

- Icon aus `AbilityData` oder definierter Fallback;
- radiale Cooldownmaske;
- optionale kleine Sekundenanzeige;
- Sperr- und Fehlerzustand;
- Range Preview;
- Touch-Tooltip ohne Konflikt mit Aktivierung;
- keine Sonderabfrage anhand fest verdrahteter Eidra-IDs im Presenter.

### 13.3 Touch-Konflikte

Kurztipp, langes Halten, Tooltip und Range Preview müssen eindeutig aufgelöst werden. Ein Tooltip darf keine Fähigkeit unbeabsichtigt auslösen, und ein normaler Skill-Tipp darf nicht durch eine lange Verzögerung unresponsiv wirken.

## 14. Umsetzungspaket 10 – Context Action Integration

Das bestehende Interaktionssystem bleibt die einzige Quelle für Context Actions.

### Unterstützte Beispiele

- Erzader → Abbauen;
- Baum → Fällen;
- Pflanze → Sammeln;
- Truhe → Öffnen;
- Loot oder Leiche → Plündern;
- NPC → Interagieren;
- fangbares Eidra → Fangen;
- Station → Benutzen.

### Prioritätsregeln

- ein Interaktionsziel darf normalen Kampf nicht blockieren;
- bei aktivem plausiblen Gegner bleibt Angriff als Primäraktion erhalten;
- Context Action erhält einen eigenen erreichbaren Platz oder übernimmt die Primärposition nur in eindeutig kampffreien Situationen;
- Hold- und Timed-Interaktionen behalten ihren Fortschrittsring;
- gesperrte Interaktionen zeigen einen kurzen, konkreten Grund;
- Interaktionsziel und Combat Target werden visuell getrennt;
- bestehende Abbruchregeln bleiben erhalten.

### Tests

- Ressource ohne Gegner;
- Ressource neben Gegner;
- Loot nach Kampf;
- NPC neben Ressource;
- Truhe neben Ressource;
- Hold-/Timed-Abbruch durch Angriff, Dodge und Schaden;
- Werkzeug fehlt;
- Ziel wechselt bei geringer und deutlicher Bewertungsdifferenz.

## 15. Umsetzungspaket 11 – Combat Feedback

Die vorhandenen Pools werden erweitert, nicht ersetzt.

### Kategorien

- normaler Schaden;
- Backstab;
- Stagger-Schaden;
- Spielerschaden;
- Heilung;
- Buff;
- `MISS`;
- `RESIST`;
- kritischer Treffer, sofern eine echte Kritmechanik existiert.

### Semantik

- `MISS` erscheint nur, wenn eine Aktion auf ein gültiges ausgewähltes Ziel gerichtet war und der Treffer tatsächlich verfehlt wurde; Angriffe ins Leere erzeugen nicht automatisch Spam.
- `RESIST` erscheint nur, wenn die Gameplayauflösung einen Effekt nachweislich widerstanden oder vollständig blockiert hat.
- Krit wird nur dargestellt, wenn der Combat-Code einen echten kritischen Treffer meldet. Backstab ist kein beliebig umbenannter Krit.
- Feedbacktexte werden aus strukturierten Combat-Ereignissen erzeugt, nicht aus geratenen Schadenswerten.

### Darstellung

- sofort am Ziel;
- ungefähr 0,5 bis 1,2 Sekunden sichtbar;
- leichte deterministische oder kontrolliert zufällige Positionsvariation;
- mehrere Zahlen dürfen sich nicht exakt überdecken;
- Kategorien besitzen klare Farbe, Gewichtung und Größe;
- Backstab und Stagger bleiben lesbar, ohne den Kampfbereich zu fluten;
- keine Allokation pro Treffer nach Aufwärmung des Pools.

### Trefferreaktion

- kurzer Impact;
- vorhandene Gegnerreaktion weiterverwenden;
- keine übergroßen HUD-Partikel;
- keine dauerhafte Bildschirmerschütterung durch normale Treffer.

## 16. Umsetzungspaket 12 – Eigene Eidren-Assets

Vorhandene UI-Assets werden zuerst geprüft und bevorzugt weiterverwendet oder stilistisch überarbeitet.

### Benötigte Motive

- Hammerfamilie;
- Dolchfamilie;
- Speerfamilie;
- Dodge;
- Terrock-Portrait;
- Noctarion-Portrait;
- Ignivar-Portrait;
- alle sechs aktuellen Eidra-Fähigkeiten;
- neutrale Interaktion;
- Abbauen und Fällen;
- Loot;
- Heiltrank;
- Bufffood;
- normale, Elite- und Boss-Target-Rings;
- Buttonrahmen;
- Cooldown- und Sperrzustände.

### Stilregeln

- klare Silhouette;
- wenige große Formen statt Mikrodetails;
- Smartphone-Lesbarkeit bei tatsächlicher Endgröße;
- transparente Hintergründe bei Einzelicons;
- Eidren-eigene Fantasy-/Survival-Formsprache;
- Material- und Farbbezug zu bestehenden Waffen, Eidra und Weltassets;
- keine generischen Unity-Icons;
- keine stilfremden Asset-Pack-Symbole;
- keine Kopien aus dem Referenzvideo.

### Rahmenvarianten

Mindestens zwei Varianten werden prototypisch verglichen:

- polygonal beziehungsweise leicht hexagonal;
- organischer Fantasy-/Survival-Rahmen.

Die bessere Variante wird anhand der vorhandenen Eidren-Art und der Lesbarkeit im vollständigen HUD gewählt.

### Asset-Abnahme

Jedes finale Icon wird geprüft bei:

- tatsächlicher HUD-Größe;
- 16:9, 19,5:9 und 20:9;
- hellem, dunklem und unruhigem Spielhintergrund;
- normal, gedrückt, gesperrt und im Cooldown;
- Android- und iOS-Screenshotgröße.

## 17. Umsetzungspaket 13 – HUD-Animationen

Geeignet sind:

- kurzer Button-Press;
- radialer Cooldown;
- kurzer Weapon Swap;
- kurzer Eidra-Wechsel;
- dezentes Stagger-Pulsieren;
- kompakter Trefferimpuls;
- kurzes Ein- und Ausblenden von Target- und Boss-HUD;
- sanfter Zustandswechsel ohne Positionssprung.

Ausgeschlossen sind:

- dauerhaftes starkes Pulsieren;
- große Bounce-Animationen;
- lange Übergänge;
- dekorative Partikel über dem HUD;
- Animationen, die Touch-Eingaben verzögern oder blockieren.

## 18. Architekturvorgaben

### 18.1 Grundsatz

Bestehende Presenter werden bevorzugt weiterverwendet, aber nicht mit beliebig vielen neuen Verantwortlichkeiten belastet.

Eine mögliche Zielaufteilung umfasst:

- HUD State Controller;
- Player Status Presenter;
- Target Presenter;
- Boss Presenter;
- Combat Action Cluster Presenter;
- Weapon Slot Presenter;
- Eidra Presenter;
- Ability Slot;
- Context Action Presenter;
- Combat Target Controller;
- Combat Target Indicator;
- Damage Number Pool;
- Objective Presenter.

Die endgültige Aufteilung folgt dem Vorbereitungsauftrag und muss vor dem Refactoring kurz dokumentiert werden.

### 18.2 Datenfluss

- Gameplayzustände liefern Ereignisse oder Snapshots;
- Presenter visualisieren, entscheiden aber keine Kampfregeln;
- UI-Buttons leiten Eingaben über den vorhandenen Input Reader weiter;
- Targeting bestimmt keine automatische Bewegung oder Angriffe;
- Icons und Fähigkeiten kommen aus Datenassets;
- keine wiederholten `Find*`-Suchen im normalen Framepfad;
- keine per-Frame-Allokationen in Targeting, Cooldowns oder Combat Feedback.

### 18.3 Prefab-Regel

Die produktive HUD-Hierarchie wird im Prefab gepflegt. Editor-Builder und Prefab-Tests werden gemeinsam mit autorisierten Layoutänderungen aktualisiert. Runtime-Fallbacks für fehlende Pflichtreferenzen sind verboten.

## 19. Verbindliche Migrationsreihenfolge

Die Umsetzung erfolgt in kleinen, einzeln abnehmbaren Schritten:

1. Ausgangstests und Belege;
2. persistentes Targeting hinter bestehenden Schnittstellen;
3. Auto-Facing an neues Zielmodell anbinden;
4. Combat Target Ring;
5. normales Target-HUD;
6. HUD-Zustandssteuerung;
7. responsives Prefab-Grundlayout;
8. Weapon- und Dodge-Cluster;
9. datengetriebenes Eidra-HUD;
10. Context-Action-Prioritäten;
11. Boss-HUD und Stagger-Fenster;
12. erweitertes Combat Feedback;
13. finale Art und Animationen;
14. Mobile-Abnahme und Profiling;
15. vollständige Regression.

Nach jedem Schritt:

- kompilieren;
- fokussierte Tests ausführen;
- relevante Screenshots oder kurze Capture sichern;
- keine bekannten roten Tests in den nächsten Schritt tragen;
- geänderte Verträge dokumentieren.

Ein Big-Bang-Austausch des vollständigen HUDs ist ausgeschlossen.

## 20. Regressionstest

Vor Abschluss werden mindestens geprüft:

### Bewegung und Eingabe

- Touch-Joystick;
- Tastatur/Maus;
- Gamepad;
- Bewegung in allen Kamerarichtungen;
- Dodge-Richtung;
- Ausdauerverbrauch und Regeneration;
- Pause und Gameplay-Disable.

### Waffen

- Hammer;
- Dolche;
- Speer;
- alle verfügbaren Varianten;
- zwei beliebige ausgerüstete Waffen;
- Weapon Swap;
- Swap-Buffer während Combos;
- Hammer-, Dolch- und Speerkombos;
- Backstab;
- Stagger;
- Reichweitenverhalten großer Gegner.

### Eidra

- Terrock;
- Noctarion;
- Ignivar;
- alle sechs aktuellen Fähigkeiten;
- Range Preview;
- Cooldowns;
- Fähigkeitsfehler;
- Eidra-Wechsel;
- Wechsel-Cooldown;
- Gespannverwaltung;
- Zielwahl gegen normale Gegner und Bosse.

### Interaktionen

- Ressourcen;
- Loot;
- Truhen;
- NPCs;
- Stationen;
- fangbare Eidra;
- Instant, Hold und Timed;
- Werkzeuganforderungen;
- Abbruch durch Angriff, Dodge, Schaden, Tod und Szenenwechsel.

### Gegner und Boss

- ein Gegner;
- mehrere Gegner;
- Elite;
- Garon;
- Target Ring;
- Zielwechsel;
- HP-Anzeige;
- Stagger-Anzeige;
- Stagger-Fenster;
- Sieg;
- Niederlage;
- Restart.

### UI und Geräteformate

- Exploration;
- normaler Kampf;
- Bosskampf;
- Inventar;
- Crafting;
- Technologie;
- Bauen;
- Eidra-Gespannfenster;
- Minimap;
- Objectives;
- 16:9;
- 19,5:9;
- 20:9;
- Safe Area links und rechts;
- Android Landscape;
- iOS Landscape.

## 21. Performance-Abnahme

Das neue HUD darf keine merklichen Ruckler oder unnötigen Allokationen erzeugen.

Zu belegen sind mindestens:

- keine dauerhaften GC-Allokationen durch Targeting;
- keine Allokation pro Treffer nach Pool-Aufwärmung;
- keine unnötigen Materialinstanzen durch Target Rings;
- kontrollierte Canvas-Rebuilds;
- keine teuren Hierarchiesuchen pro Frame;
- stabile Frametimes bei mehreren Gegnern und vielen Treffern;
- korrekte Freigabe beziehungsweise Wiederverwendung gepoolter Elemente nach Szenenwechsel.

## 22. Systeme, die nicht beschädigt werden dürfen

- Weltstruktur und Zonen;
- Progression;
- Savegame-Kompatibilität;
- Inventar, Equipment und Crafting;
- Waffenvarianten und Ausrüstungsplätze;
- Waffenprogression;
- Combo- und Weapon-Switch-Logik;
- Backstab- und Stagger-Regeln;
- Speermechanik;
- Eidra-Roster und Gespannverwaltung;
- Terrock, Noctarion und Ignivar;
- Bossangriffe und Garons grundlegender Combat Loop;
- Interaktions- und Abbruchregeln;
- Resource Nodes und Loot;
- Minimap;
- Bau-, Crafting-, Inventar- und Technologiefenster;
- Tastatur-/Maus- und Gamepad-Unterstützung;
- bestehende Tests, sofern sie nicht nachweislich eine absichtlich geänderte HUD-Anforderung prüfen.

## 23. Nichtziele

Dieser Auftrag ist kein kompletter Neubau des Spiels.

Nicht ohne gesonderte Freigabe ändern:

- Kampfbalance;
- Schadens- oder Staggerwerte;
- Cooldowns;
- Gegner-KI außerhalb notwendiger Zielinformationen;
- Bossangriffe;
- Welt- oder Queststruktur;
- Progressionssystem;
- Anzahl der Waffenfamilien;
- Anzahl der Eidra;
- Crafting- oder Ausrüstungssystem;
- Savegame-Schema, sofern keine zwingende Migration dokumentiert ist.

## 24. Erwartete Deliverables

Am Ende werden geliefert:

1. funktionsfähiges responsives Mobile-HUD;
2. persistentes Mobile-Targeting;
3. Combat Target Ring für normal, Elite und Boss;
4. Auto-Facing ohne Auto-Combat;
5. normales Target-HUD;
6. Boss-HUD mit HP und Stagger;
7. Exploration-, Combat- und Boss-Combat-Zustände;
8. neuer Weapon- und Dodge-Cluster;
9. datengetriebenes Eidra-HUD;
10. integrierte Context Actions;
11. erweitertes Combat Feedback;
12. passende Eidren-Icons, Portraits und Rahmen oder klar markierte freigegebene Prototypen;
13. automatisierte EditMode- und PlayMode-Tests;
14. Mobile- und Performance-Abnahmebericht;
15. Dokumentation der Architektur und Datenflüsse;
16. vollständige Übersicht aller geänderten Dateien;
17. bekannte offene Punkte;
18. Screenshots in 16:9, 19,5:9 und 20:9;
19. Screenshots für Exploration, normalen Kampf und Garon-Bosskampf;
20. kurze Captures für Targetwechsel, Weapon Swap, Eidra-Wechsel und Stagger-Fenster.

## 25. Finale Abnahmekriterien

Der Auftrag ist erst abgeschlossen, wenn:

### Mobile UX

- das HUD auf einem Smartphone komfortabel bedienbar ist;
- die rechte Hand Angriff, Dodge, Wechsel und Skills ohne Umgreifen erreicht;
- Touch-Ziele nicht überlappen;
- Safe Areas eingehalten werden;
- Landscape verbindlich funktioniert.

### Freie Spielfläche

- die Bildschirmmitte weitgehend frei bleibt;
- Gegner und Telegraphen nicht durch permanente UI verdeckt werden;
- Objectives und Minimap kompakt bleiben.

### Waffen

- aktive und alternative Waffe unmittelbar erkennbar sind;
- Hammer, Dolche, Speer und Varianten korrekt funktionieren;
- Weapon Swap und Buffer unverändert zuverlässig bleiben;
- Stagger-Hinweise nur für tatsächlich ausgerüstete Dolche erscheinen.

### Eidra

- aktives Eidra und beide Fähigkeiten sofort verständlich sind;
- Terrock, Noctarion und Ignivar ohne Presenter-Sonderfälle funktionieren;
- Cooldowns, Wechsel und Range Preview korrekt bleiben.

### Targeting

- kein zweiter Zielstick nötig ist;
- das Ziel stabil bleibt;
- Zielwechsel nachvollziehbar sind;
- Bosspriorität nahe Feldgegner nicht blockiert;
- große Gegner korrekt nach erreichbarer Trefferfläche behandelt werden;
- Auto-Facing keine Bewegung oder Angriffe übernimmt.

### Context Actions

- situative Aktionen keine Sammlung permanenter Spezialbuttons benötigen;
- Kampf nicht durch Ressourcen oder Loot blockiert wird;
- bestehende Hold-/Timed-Mechaniken erhalten bleiben.

### Boss Combat

- Garons HP und Stagger eindeutig lesbar sind;
- Stagger-Zustand und Fenster korrekt kommuniziert werden;
- der Hammer-/Dolch-Flow unterstützt, aber nicht erzwungen wird.

### Art Style

- alle finalen UI-Elemente wie Bestandteil von Eidren wirken;
- keine fremden Standardicons oder Referenzkopien enthalten sind;
- Icons bei tatsächlicher Smartphone-Größe lesbar bleiben.

### Qualität und Performance

- fokussierte und vollständige Regressionstests grün sind;
- keine neue dauerhafte GC-Last entsteht;
- keine merklichen neuen Ruckler auftreten;
- alle geänderten Verträge dokumentiert sind;
- bekannte offene Punkte klar benannt sind;
- keine unkontrollierten Änderungen außerhalb des HUD- und Combat-Control-Scopes erfolgt sind.

