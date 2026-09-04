# Wunschsammlung vom 14. August 2026

**Status:** Alle zehn Einträge sind umgesetzt: W-001/003/004/005/006 am
14.08.2026, W-002/007/008/009/010 am 15.08.2026; W-005 wurde am 15.08. auf
Nutzerentscheid zurückgenommen (Abgebaut-Optik gilt wieder). Nachweise je
Eintrag. Die Fixes ab W-007 sind noch NICHT in der ausgelieferten Exe vom
15.08. (d00c4535…) — für den nächsten Snapshot neu bauen.

**Abschlussläufe (14.08.2026):** Volle EditMode-Suite 1036/1036 grün
(`TestResults-wfix-editmode.xml`), volle PlayMode-Suite 118 bestanden /
0 fehlgeschlagen / 4 bekannte Headless-Skips (Captures + Terrock-Grafiktest,
`TestResults-wfix-playmode.xml`). Läufe vor W-006; W-006 separat grün.

**Nebenbefunde aus der Umsetzung (nicht beauftragt, nur notiert):**

- Das Item `iron_spear` („Eisenspeer") verwendet als Inventar-Icon den
  Platzhalter `ITEM_TMP_Pickaxe.png` (`ItemContentTable.cs`, Zeile ~60) —
  im Rucksack sieht der Eisenspeer wie eine Spitzhacke aus.
- Abbau ohne Werkzeug (blosse Hände) nutzt weiter die prozedurale
  Wipp-Pose; deren Dauer ist unendlich und wird nach dem Abbau nicht aktiv
  zurückgesetzt — die Figur kann geneigt stehenbleiben. Vorbestehend, von
  W-001 unberührt.
- `CopperVein_Active.prefab` hatte die `CopperMiningVisualFeedback`-
  Komponente erneut verloren (Asset-Rebuild der Parallelsitzung vom
  14.08.); per `CopperMiningFeedbackRebuilder` wiederhergestellt, die zwei
  betroffenen PlayMode-Baseline-Tests sind wieder grün.

Die Nummerierung W-xxx ist bewusst von der abgeschlossenen
`FIXSAMMLUNG_V0.2.1.md` (F-001 bis F-014) getrennt. Wo ein Wunsch einen
dort zurückgestellten Eintrag berührt, ist das vermerkt.

## T-001 — Tester-Spielstand mit Voll-Ausbau (15.08.2026)

**Auftrag:** Die nächste Tester-Exe soll mit Maximallevel, kompletter
Eisenrüstung, allen Eisenwaffen und -werkzeugen sowie allen Gebieten und
Technologien starten.

**Umsetzung:** `TesterSeedSaveBuilder` erzeugt
`Assets/_Game/Resources/Data/TesterSeedSave.json` **aus den echten Inhalten**
(Technologiebaum, Fortschrittskurve, Gegenstandskatalog) — nichts ist doppelt
gepflegt. Inhalt: Stufe 2 / Level 40 (Maximum der höchsten Stufe), 29
Technologien, 8 Gebiete + Flags `garon_defeated`/`tier_2_unlocked`,
Eisenhammer und -dolche angelegt, Eisenspeer im Rucksack, komplette
Eisenrüstung (Helm/Harnisch/Handschuhe/Beinschutz), alle drei Eisenwerkzeuge,
alle drei Eidra (zwei aktiv) sowie Baumaterial-Vorräte.

Zwei Wege ins Spiel:

1. **Ohne eigenen Spielstand:** Der Stand wird beim ersten Start automatisch
   eingespielt (`SaveGameService.TrySeedFromTemplate`), „FORTSETZEN" führt
   direkt hinein. Ein **vorhandener Spielstand wird nie überschrieben**.
2. **Mit eigenem Spielstand:** „NEUES SPIEL" startet in dieser Testfassung
   direkt im Voll-Ausbau-Stand (`SaveGameService.TryApplyTemplate`), ohne
   Dateiarbeit. Der eigene Spielstand bleibt über „FORTSETZEN" erreichbar,
   bis im neuen Spiel gespeichert wird.

**Nicht enthalten — bewusst:** Vier Technologieknoten
(`technology.19/21/22/23`) tragen das Flag `retired.never` und sind dauerhaft
stillgelegt; sie erscheinen im Baum als „FORTSCHRITTSFLAG FEHLT" und sind
auch regulär unerreichbar. „Alle Technologien" heißt daher 29 von 33.

**Nachweise:** `TesterSeedSaveTests` — der Spielstand wird über den echten
Ladepfad geladen (inkl. Migration, Mapper, Quarantäne-Prüfung) und auf
Maximallevel, alle Technologien, alle Gebiete, Eisenausrüstung, Werkzeuge und
Eidra geprüft; dazu die Seed-Mechanik (schreibt nur ohne vorhandenen
Spielstand, lehnt unbrauchbare Vorlagen ab). Logs `seed-*`.

**Zwei Regeln der Architekturwache haben unterwegs zugeschlagen** (beide
korrekt, beide behoben):

- §7 (max. 400 Zeilen je Laufzeitdatei): `SaveGameService.cs` wuchs auf 443,
  `MeshActorPresentation.cs` auf 404 und `TechnologyTreeWindow.cs` auf 419
  Zeilen. Alle drei sind nach Projektkonvention als `partial` aufgeteilt —
  `SaveGameService.Vorlage.cs` (Vorlage-Spielstände),
  `MeshActorPresentation.Haende.cs` (Waffen, Werkzeuge, leere Hände) und
  `TechnologyTreeWindow.Scrollen.cs` (Scrollverhalten).
- Die Clip-Anzahl der Wanderer.glb ist von 24 auf 25 gestiegen (neuer
  `Oeffnen`-Clip aus W-009); `Wanderer3DAssetTests` prüft die Zahl jetzt
  gegen 25 und führt die vier neuen Clips namentlich in der scharfen
  Namensliste.

## W-001 — Abbau spielt noch die alte Animation statt der Abbau-Clips aus dem 3D-Modell

**Status: umgesetzt am 14.08.2026.** `ResolveClipName` kennt jetzt den
Stamm `harvest` für die Werkzeug-Träger (Axt/Spitzhacke/Sense);
`MeshActorPresentation.SetHarvestTool/ClearHarvestTool` blenden während des
Abbaus das Werkzeug-Mesh statt der Waffe ein und stellen sie danach wieder
her; `PlayerVisualAnimator` mappt das aufgelöste Werkzeug-Item
(`ToolStanceForItem`: axe→Axt, pickaxe→Spitzhacke, scythe→Sense) und spielt
den Abbau-Clip als Schleife (loop, sonst setzt `PlayTimed` ihn pro Frame auf
t=0 zurück). Das 2D-Sprite-Werkzeug ist bei der 3D-Figur unterdrückt
(`PlayerHarvestVisual`, Zustandsflags bleiben erhalten). Abbau ohne Werkzeug
bleibt beim prozeduralen Rückfall. Nachweise:
`MeshActorPresentationTests` (Clip-Auflösung, Mapping, Mesh-Wechsel),
`PlayerPresentationPlayModeTests.Player3D_AbbauZeigtWerkzeugMeshUndSpieltAbbauClip`,
`InteractionIntegrationTests.CopperMining_ZeigtWerkzeugMeshMitAbbauClipStattSpriteWerkzeug`
(jeweils rot→grün, Logs `w001-*`).

**Bereich:** Spielerfigur, Ressourcenabbau, Animation

**Beobachtung:** Beim Abbauen von Ressourcen läuft weiterhin die alte
Darstellung (prozedurales Wippen der Figur plus pendelndes 2D-Sprite-Werkzeug),
obwohl die Wanderer.glb die passenden Abbau-Animationen bereits enthält.

**Ursache (diagnostiziert am 14.08.2026):** Der Code referenziert die
Abbau-Clips an keiner Stelle. Im Einzelnen:

- Die `Wanderer.glb` enthält laut `README_Wanderer3D.md` die Clips
  `Abbau_Axt` (0,95 s), `Abbau_Spitzhacke` (1,05 s) und `Abbau_Sense`
  (1,50 s) sowie Träger-Varianten von `Ruhe_`/`Gehen_`/`Laufen_` für Axt,
  Spitzhacke und Sense und die Werkzeug-Meshes `Waffe_Axt/_Spitzhacke/_Sense`
  (Werkzeuge am 07.08.2026 ergänzt).
- Beim Abbau ruft `PlayerVisualAnimator.Update` den Zustand
  `SetAuthoredState("harvest", …)` auf. `MeshActorPresentation.ResolveClipName`
  kennt aber nur die Stämme `idle`, `move`, `hammer*`, `dagger*`, `spear*` —
  für `harvest` liefert sie `null`.
- Dadurch greift der Rückfall `StartProceduralForStem("harvest")` →
  `ProceduralPose.Harvest`: das alte prozedurale Wippen (Y-Hub ~6 cm,
  8° Neigung) in `MeshActorPresentation.Zustaende.cs`. Der Rückfall stammt
  aus dem Wanderer3D-Einbau-Plan, der vor der Werkzeug-Ergänzung geschrieben
  wurde („Ernten prozedural, kein Clip in der GLB“ — inzwischen überholt).
- Zusätzlich hängt `PlayerHarvestVisual` weiterhin das 2D-Sprite-Werkzeug mit
  72-Grad-Pendel an die rechte Hand (bewusst zunächst beibehalten, siehe
  F-006 der Fixsammlung).
- `SetWeaponStance` kennt nur die drei Waffen-Stances (Speer, Dolche,
  Hammer); die Werkzeug-Stances und Werkzeug-Meshes werden nie aktiviert.

**Stoßrichtung für die spätere Umsetzung (noch nicht beauftragt):**
`ResolveClipName` um die Abbau-Stämme erweitern und beim Abbau Stance und
Werkzeug-Mesh passend zum aufgelösten Werkzeug (Axt/Spitzhacke/Sense)
umschalten; das 2D-Sprite-Werkzeug aus `PlayerHarvestVisual` entfällt dann
(deckungsgleich mit der in F-006 notierten neuen Stoßrichtung
„Sprite-Werkzeug entfernen und Abbau über Waffenmesh plus Harvest-Clip
darstellen“).

**Berührt:** F-006 (zurückgestellt) aus `FIXSAMMLUNG_V0.2.1.md`.

## W-002 — Kontrollbutton zeigt noch die alte Fingerhand-Grafik

**Status: umgesetzt am 15.08.2026.** Das erinnerte „neue Icon" war im
Projekt nicht auffindbar (siehe Suchbefund); auf Wunsch des Nutzers wurde
stattdessen eine neue Hand **im Stil der Werkzeug-Glyphen** erstellt:
offene, greifende Hand, flache Creme-Füllung, dicke dunkelbraune Kontur,
Ocker-Bündchen am Gelenk — passend zu `ui_interaction_axe/pickaxe/scythe`.
`ui_interaction_hand.png` wurde in place ersetzt (GUID und Importeinstellungen
unverändert, alle Verweise inkl. Kistenhände zeigen automatisch das neue
Bild). Quelle regenerierbar:
`Assets/_Game/Resources/Art/UI/Source~/make_interaction_hand.py`.
Die alte Fingerhand liegt als Sicherung im Sitzungs-Scratchpad
(`ui_interaction_hand.alt-fingerhand.png`). Nachweis: `InteractionIconTests`
grün nach dem Tausch (`w002-green.log`).

**Bereich:** HUD, Interaktionsbutton

**Beobachtung:** Der Kontrollbutton (Interaktionsbutton) zeigt weiterhin die
alte Fingerhand-Glyphe. Hier war bereits vereinbart, ein neues Bild
einzusetzen; das ist noch nicht geschehen.

**Befund zur Verortung (14.08.2026):** Die Glyphe ist
`Assets/_Game/Resources/Art/UI/ui_interaction_hand.png`. Sie wird von den
Editor-Buildern erzeugt bzw. verdrahtet (`InteractionIconContentBuilder`,
`FixCollectionArtBuilder`, `WorldChestContentBuilder`); daneben existieren
bereits werkzeugspezifische Glyphen (`ui_interaction_axe/pickaxe/scythe`).
Dieselbe Hand-Grafik wird außerdem für die Kistenhände missbraucht — das ist
der zurückgestellte F-005.

**Offen zu klären vor Umsetzung:** Wie das neue Bild aussehen soll bzw. ob
eine Vorlage existiert.

**Suchbefund (14.08.2026):** Der Nutzer erinnert, dass das neue Icon bereits
existieren sollte. Eine Suche über Assets, Tools und Documentation (PNGs
seit 08.08., Namen mit ui/icon/interact/hand/button, GRAFIKAUFTRAEGE,
Belege-Ordner) fand **kein** neues Icon — im Projekt liegen nur die vier
bekannten Glyphen (hand/axe/pickaxe/scythe, alle handgemalt 256×256).
Entweder liegt das Bild außerhalb des Projektordners oder es wurde nie
abgelegt. Warten auf Zulieferung/Beschreibung; alternativ Neuentwurf im Stil
der Werkzeug-Glyphen.

**Berührt:** F-005 (zurückgestellt) aus `FIXSAMMLUNG_V0.2.1.md`.

## W-006 — Startbildschirm zeigt noch „V0.1 · AUFBRUCH"

**Bereich:** Hauptmenü, Versionskennzeichnung

**Beobachtung (14.08.2026):** Der Startbildschirm trägt weiterhin den
Untertitel „V0.1 · AUFBRUCH", obwohl der Stand v0.2 ist.

**Ursache:** Das Subtitle-Label ist in `MainMenu.unity` serialisiert
(Text „V0.1  ·  AUFBRUCH") und stammt aus
`EidrenSceneStructureBuilder.cs` (Zeile ~192), der denselben String bei
Neubauten wieder erzeugen würde.

**Umsetzung:** Beide Stellen auf „V0.2  ·  TESTVERSION" umgestellt
(Sprachgebrauch der Release Notes; ein Beiname wie „Aufbruch" für v0.2 ist
nicht dokumentiert — sobald einer feststeht, an denselben zwei Stellen
ersetzen). Nachweis: `MainMenuVersionTests` (rot→grün).

## W-007 — Baumenü: Gebäudebilder hängen übereinander

**Bereich:** Heimatbasis, Baumenü (BASISBAU)

**Beobachtung (15.08.2026, Spieltest der neuen Exe):** Im Baumenü liegen
die Bilder der Gebäude übereinander; die Katalogkarten (Rahmen,
Beschriftung, Kosten) fehlen komplett — nur die Icons sind sichtbar,
nahezu deckungsgleich gestapelt.

**Ursache (diagnostiziert am 15.08.2026):**
`BuildingMenuUiBuilder.UpgradeHorizontalLayout` (F-014) setzt
`childControlWidth = false` an der HorizontalLayoutGroup der
Katalogzeile. Damit ignoriert die LayoutGroup die
`LayoutElement.preferredWidth = 300` der elf Einträge und verwendet deren
Eigenbreite — die im Prefab 0 ist. Alle Einträge kollabieren auf Breite 0
im 12-px-Abstand; nur die fest dimensionierten Icon-Kinder bleiben
sichtbar und stapeln sich. Der F-014-Testnachweis prüfte nur die
Überschneidungsfreiheit der **Bereiche**, nicht die Breite der
**Einträge**; die bildbasierte Abnahme stand ausdrücklich noch aus.

**Stoßrichtung:** `childControlWidth = true` (damit greift die
preferredWidth), Prefab per Builder neu erzeugen; Test, der die
aufgelösten Eintragsbreiten > 0 und disjunkte X-Bereiche prüft.

**Status: umgesetzt am 15.08.2026.** `childControlWidth = true` im
`BuildingMenuUiBuilder`, Prefab per `UpgradeForAutomation` neu erzeugt.
Nachweis: `BuildingMenuCatalogLayoutTests` (rot→grün) plus bestehende
`BuildingMenuLayoutTests` unverändert grün (Logs `w578-*`,
`w007-builder.log`).

## W-008 — Keine Abbau-Animation mit Kupfer-/Eisenwerkzeugen

**Bereich:** Spielerfigur, Ressourcenabbau, Animation (Folge zu W-001)

**Beobachtung (15.08.2026):** Beim Holzfällen gibt es keine Animation.

**Ursache (diagnostiziert am 15.08.2026):**
`MeshActorPresentation.ToolStanceForItem` mappt nur die exakten Basis-IDs
`axe`/`pickaxe`/`scythe`. Die Stufen-Werkzeuge `copper_axe`, `iron_axe`,
`copper_pickaxe`, `iron_pickaxe`, `copper_scythe`, `iron_scythe` fallen
durch → kein Abbau-Clip. Der prozedurale Rückfall ist zusätzlich wirkungslos,
weil `SetAuthoredState("harvest")` pro Frame `StartProcedural` neu startet
und der Wipp-Timer dadurch nie anläuft (vorbestehend, siehe Nebenbefunde).
Wer Tier-2-Werkzeuge trägt (typischer Spielstand), sieht daher gar keine
Animation.

**Stoßrichtung:** Suffix-Mapping (`pickaxe` vor `axe` prüfen!) oder Mapping
über die Werkzeug-Tags; Tests für alle neun Werkzeug-IDs.

**Status: umgesetzt am 15.08.2026.** `ToolStanceForItem` mappt per Suffix
(pickaxe→Spitzhacke vor axe→Axt, scythe→Sense); alle neun Werkzeug-IDs
getestet. Nachweis: erweiterte `MeshActorPresentationTests` (rot→grün,
Logs `w578-*`). Der wirkungslose prozedurale Rückfall (Timer-Reset pro
Frame) bleibt als Nebenbefund notiert.

## W-009 — Kistenöffnung: 3D-Figur soll knien und die Hände bewegen

**Bereich:** Gebietskisten, Animation (ersetzt die Stoßrichtung von F-005/F-006)

**Wunsch (15.08.2026):** Die Animation beim Kistenöffnen gefällt nicht.
Vorschlag des Nutzers: Das 3D-Modell soll eine eigene Animation bekommen —
vor der Kiste knien und die Hände davor bewegen, als ob es etwas öffnet.

**Machbarkeit (Einschätzung 15.08.2026):** Die Wanderer.glb wird aus
`Source~/wanderer.html` generiert (Rig mit 20 Knochen, Export- und
Prüfskripte vorhanden). Ein neuer Clip „Oeffnen" (Knie-Pose + zyklische
Handbewegung, ~1,5 s Schleife) ist dort autorierbar; Einbau analog W-001:
Stamm `open` in `ResolveClipName`, ausgelöst während der Timed-Interaktion
an `WorldChestContainer`, die 2D-Handgrafiken (`OpeningHand_Left/Right`)
entfallen. Erledigt zugleich F-005 und die Kistenhälfte von F-006.

**Freigegeben am 15.08.2026; Umsetzung läuft.** Clip `Oeffnen` (1,60 s,
49 Bilder) in `wanderer.html` autoriert: Kniestand auf dem rechten Knie,
linker Fuß vorn aufgestellt, Oberkörper zur Kiste geneigt, Hände arbeiten
gegenläufig auf Deckelhöhe; Fuß-IK für den Clip deaktiviert
(`plantAmount = 0`). GLB neu exportiert und verifiziert (25 Animationen,
Skinning-Abgleich 0,000 mm). Wichtige Renderer-Notiz: `render.js` der
Wanderer-Quelle nimmt **Radiant**-Winkel (Defaults 0.58/0.16) und
`argv[11]` ist die Ausrüstung — die Grad-/Ziel-Z-Belegung aus
`KREATURENSERIE_STAND.md` gilt dort nicht. Code: `ResolveClipName`-Stamm
`open` → „Oeffnen", `MeshActorPresentation.SetBareHands()` (leere Hände,
Rückkehr über `ClearHarvestTool`), `PlayerHarvestVisual.IsOpening`
(Container+Timed), Animator spielt den Clip als Schleife;
`WorldChestHandsRemovalBuilder` entfernt die 2D-Hände aus den drei
Kisten-Prefabs. Nachweise (rot belegt): `MeshActorPresentationTests`
(open-Stamm, SetBareHands), `WorldChestHandsTests`,
`InteractionIntegrationTests.ChestOpening_SpieltOeffnenClipUndZeigtKeineSpriteHaende`;
Grün-Läufe stehen aus.

**Status: umgesetzt am 15.08.2026.** Alle Nachweise grün: EditMode 54/54
(`w009-green-edit`), PlayMode-Kettentest samt aller Interaktions- und
Präsentations-Regressionen 11/11 (`w009-green2`). Der Kettentest belegt in
der echten Zone: Oeffnen-Clip läuft während der Kistenöffnung, keine
Sprite-Hände an der Kiste, Clip endet nach dem Abbruch (Crossfade ~0,12 s
wird abgewartet). Damit sind auch F-005 und die Kistenhälfte von F-006
erledigt; die Werkzeughälfte von F-006 war schon durch W-001 abgelöst.

**Nebenbefund Testlandschaft:** `CopperVein_Hold225SecondsCollectsExactlyOnce`
schlug in genau einem Klassenlauf fehl („nach 6 s nicht abgebaut") und lief
in der Paar-Probe sowie im Wiederholungslauf grün — zeitabhängige Flake
(Verdacht: wandernder Gegner trifft den Spieler, Schaden bricht die Session
regelkonform ab). Falls sie wieder auftritt: Abbruchgrund im Test
instrumentieren.

## W-010 — Technologiebaum nicht vollständig einsehbar, Scrollen fehlt

**Bereich:** Technologiebaum-Fenster

**Beobachtung (15.08.2026, mit Screenshot):** Der Technologiebaum (30+
Karten in 5er-Rastern) ragt unten aus dem Bildschirm; man kann nicht
scrollen. Zusätzlich überlagern sich im unteren Bereich Bedienhinweise,
der FREISCHALTEN-Button und die Karten (Reihe 21/24), und eine Karte (19)
erscheint ohne Beschriftung als gelbe Fläche.

**Stoßrichtung:** Scrollbereich (ScrollRect) um das Kartenraster,
Bedienhinweise und Aktionsbuttons aus dem Kartenbereich in eine feste
Fußleiste; Diagnose der leeren Karte 19 bei der Umsetzung. (Ursache noch
nicht diagnostiziert.)

**Status: umgesetzt am 15.08.2026.** Neuer `TechnologyTreeScrollBuilder`:
NodeGrid (GridLayoutGroup bleibt) liegt jetzt in einem ScrollRect mit
Maske und ContentSizeFitter; Hint, FREISCHALTEN und SCHLIESSEN sitzen in
einer festen Fußleiste. Tastatur-/Gamepad-Auswahl folgt per
`TechnologyTreeWindow.VerticalScrollTargetFor` +
`EnsureSelectedVisible` in den sichtbaren Bereich. Nachweis:
`TechnologyTreeScrollTests` (rot→grün) plus bestehende
`TechnologyTreeTests` grün (Logs `w010-*`). Die gelbe Karte 19 aus dem
Screenshot ist mutmaßlich der Auswahlzustand (Marker überdeckt den Text)
— noch nicht separat diagnostiziert, als Restpunkt notiert.

## W-003 — Werkzeug-Meshes (u. a. Sense) sind dauerhaft sichtbar, auch mit Eisenspeer

**Status: umgesetzt am 14.08.2026.** `InitiallyInactiveNodes` im
`Wanderer3DPlayerBuilder` umfasst jetzt alle 18 Slot-/Waffen-/Werkzeug-
Knoten; das Player-Prefab ist neu gebaut. Zur Laufzeit blendet
`SetWeaponStance` (über `ApplyHandMeshes`) die Werkzeuge bei jedem
Waffenwechsel mit aus. Nachweise: `MeshActorPresentationTests.
SetWeaponStance_DeaktiviertAuchDieWerkzeugMeshes` und erweiterte
`PlayerPrefabTests` (rot→grün, Logs `w003-*`).

**Bereich:** Spielerfigur, Ausrüstungsdarstellung

**Beobachtung:** Mit ausgerüstetem Eisenspeer wirkt es, als trüge die Figur
gleichzeitig die Sense (Screenshot vom 14.08.2026: Sensenblatt ragt seitlich
aus der Figur).

**Ursache (diagnostiziert am 14.08.2026):** Die drei Werkzeug-Meshes werden
an keiner Stelle deaktiviert — sie sind seit dem GLB-Import dauerhaft aktiv:

- `Wanderer3DPlayerBuilder.InitiallyInactiveNodes` deaktiviert beim
  Prefab-Bau genau 15 Knoten: 12 Rüstungsslot-Meshes plus `Waffe_Speer`,
  `Waffe_Dolche`, `Waffe_Hammer`. Die später ergänzten Werkzeuge
  `Waffe_Axt`, `Waffe_Spitzhacke`, `Waffe_Sense` (07.08.2026) fehlen in der
  Liste und bleiben im Prefab aktiv.
- Auch zur Laufzeit schaltet `MeshActorPresentation.SetWeaponStance` nur
  über `WeaponMeshNames` (dieselben drei Waffen) — die Werkzeuge werden nie
  ausgeblendet.
- Damit sind grundsätzlich **alle drei** Werkzeug-Meshes permanent sichtbar
  (sie überlagern sich an der Hand; die Sense fällt wegen des langen Stiels
  am stärksten auf) — unabhängig von der ausgerüsteten Waffe.

**Stoßrichtung für die spätere Umsetzung (noch nicht beauftragt):** Die
sechs `Waffe_*`-Knoten gemeinsam verwalten (Builder-Liste um die drei
Werkzeuge ergänzen, `WeaponMeshNames` erweitern oder Werkzeuge separat
schalten) und das Player-Prefab neu bauen. Hängt mit W-001 zusammen: Beim
Abbau sollen die Werkzeug-Meshes ja gezielt aktiviert werden.

**Berührt:** W-001; Player-Prefab muss nach Builder-Korrektur neu erzeugt
werden.

## W-004 — Angriffs- und Waffenwechselbutton zeigen falsche Waffen-Icons (Speer fehlt)

**Status: umgesetzt am 14.08.2026.** `CombatHudActionPresenter` löst Icons
jetzt je Waffenfamilie auf (`IconForWeapon` über `Identity.Family`, neues
Feld `spearIcon`, verdrahtet mit `ITEM_TMP_IronSpear` per
`WeaponIconHudBuilder`). Der Wechselbutton zeigt die tatsächlich verfügbare
Zweitwaffe (neue Property `PlayerCombatController.AlternateWeapon`) und ist
ohne Zweitwaffe gesperrt mit ausgeblendetem Icon statt eines Phantom-Icons.
Nachweise: `CombatHudWeaponIconTests` (rot→grün) plus unveränderte
`WeaponSwitchTests` (Logs `w004-*`).

**Bereich:** Kampf-HUD, Aktionsbuttons

**Beobachtung:** Mit ausgerüstetem Eisenspeer zeigt der Angriffsbutton das
Hammer-Icon. Der Waffenwechselbutton zeigt das Dolch-Icon, obwohl gar keine
zweite Waffe getragen wird.

**Ursache (diagnostiziert am 14.08.2026):**
`CombatHudActionPresenter.RefreshWeapon` kennt nur zwei Waffen-Icons
(`hammerIcon`, `daggersIcon`) — vermutlich Stand v0.1, als es nur Hammer und
Dolche gab:

- Angriffsbutton: `current.Id == "daggers" ? daggersIcon : hammerIcon` —
  jede Waffe außer Dolchen (also auch jeder Speer) bekommt das Hammer-Icon.
- Waffenwechselbutton ohne Zweitwaffe (`previous == null`): zeigt das
  „Gegenstück“ aus dem Hammer/Dolch-Paar zur aktuellen Waffe — beim Speer
  also das Dolch-Icon. Eine Behandlung für „nur eine Waffe getragen“
  existiert nicht; der Button wird lediglich gesperrt, wenn gar keine Waffe
  da ist.
- Ein Speer-Icon ist im Presenter nicht vorgesehen (das Icon-Array aus
  `ConfigureReferences` enthält keins).

**Stoßrichtung für die spätere Umsetzung (noch nicht beauftragt):** Icons je
Waffenfamilie auflösen (inkl. Speer) statt Zweierpaar; Wechselbutton bei nur
einer getragenen Waffe ausblenden oder sperren statt ein Phantom-Icon zu
zeigen. Zu klären: Woher kommt ein Speer-Icon (neue Grafik nötig?).

## W-005 — Kupferader soll nach dem Abbau grafisch unverändert bleiben

**Bereich:** Ressourcen, Gebietsdarstellung

**Beobachtung/Wunsch (14.08.2026):** Eine abgebaute Kupferader soll genauso
aussehen wie vor dem Abbau. Einziger Unterschied: Sie ist nicht mehr
abbaubar.

**Ist-Verhalten (diagnostiziert am 14.08.2026):** `CopperVein.asset` steht
auf `respawnMode: OnZoneEntry` und trägt ein eigenes
`CopperVein_Exhausted.prefab`. `ResourceNode.ApplyVisualState` blendet beim
Abbau das aktive Visual aus und das Abgebaut-Visual ein — daher der
Optikwechsel.

**Umsetzung (14.08.2026):** Neues Definitions-Flag
`keepsAppearanceWhenExhausted` (`ResourceNodeDefinition`), ausgewertet in
`ResourceNode.ApplyVisualState`: Mit Flag bleibt das aktive Visual sichtbar,
das Abgebaut-Visual bleibt aus; Interaktionssperre (`IsExhausted`,
deaktivierter Trigger) unverändert. Gesetzt nur für die Kupferader
(`CopperVeinAppearanceBuilder`, außerdem im `ResourceContentBuilder` für
künftige Voll-Rebuilds). Nachweis: `ResourceNodeAppearanceTests` (rot→grün;
Gegenprobe: Baum/Stein/Faser wechseln weiterhin).

**Zurückgenommen am 15.08.2026:** Nach dem Spieltest entschied der Nutzer,
dass die Ader doch wieder sichtbar zur Abgebaut-Optik wechseln soll. Das
Flag ist an der Kupferader wieder aus (Builder umfunktioniert, Tests
invertiert, `ResourceContentBuilder` zurückgestellt); der Mechanismus
selbst bleibt für künftige Fälle bestehen. Nachweis: angepasste
`ResourceNodeAppearanceTests` (rot→grün, Logs `w578-*`).
