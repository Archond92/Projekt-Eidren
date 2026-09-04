# Entwurf: Tutorial-Questkette bis zum ersten Eidra (F31-006)

Stand: 17.08.2026 · Zieltermin: v0.3.1 · Grundlage: FIXSAMMLUNG_V0.3.1.md,
Eintrag F31-006 (Zahlenbasis vom 16.08.2026)

## 1. Ziel und Rahmen

Eine geführte Questkette, die den Einstieg in Schritten begleitet und mit dem
**Fangen des ersten Eidra** endet. Jeder Schritt gibt EP. Die Kette erzeugt
genau die einmaligen EP, die der Auszählung vom 16.08. zufolge in der frühen
Phase fehlen (Einmalquellen decken nur 29 % bis Level 24) — ohne die
Levelkurve `auf 25 gerundet (125 + 55 × Level)` aus der Bibel (M13.4)
anzutasten und ohne die Erträge im späten Spiel zu verzerren.

**Erste Fassung, bewusst begrenzt (Vorgabe aus F31-006):** Schritte docken
NUR an bereits vorhandene Spielereignisse an — Knoten abgeschlossen, Rezept
gefertigt, Gebäude gebaut, Gegner besiegt, Eidra gefangen. Es wird KEIN
Sammelzähler eingeführt (der tote `RecordGatheredUnits`-Pfad wurde in
F31-004 entfernt und bleibt draußen).

## 2. Die Kette

Anker aus der Bibel: Der Einmalvorrat der Heimatbasis trägt bis Level 4 und
deckt genau Werkbank, Axt, Lagerkiste und Hammer — das ist der Anfang. Das
Fanggerät setzt seit F31-007 Sägewerk und Seilerei voraus; die Kette führt
über genau diese Stationen.

| # | Schritt (Anzeigetext) | Bedingung | EP |
|---|---|---|---|
| 1 | Ernte 2 Faserpflanzen | Knoten `resource.fiber_plant` × 2 | 30 |
| 2 | Baue eine Werkbank | Gebäude `building.workbench` | 40 |
| 3 | Fertige die Axt | Rezept `craft_axe` | 40 |
| 4 | Fälle 2 Bäume | Knoten `resource.tree` × 2 | 30 |
| 5 | Baue eine Lagerkiste | Gebäude `building.storage_chest` | 40 |
| 6 | Fertige den Hammer | Rezept `craft_hammer` | 40 |
| 7 | Baue 2 Steinvorkommen ab | Knoten `resource.stone_deposit` × 2 | 40 |
| 8 | Fertige die Spitzhacke | Rezept `craft_pickaxe` | 50 |
| 9 | Fertige die Sense | Rezept `craft_scythe` | 50 |
| 10 | Besiege 2 Wildlinge | Gegner besiegt × 2 | 60 |
| 11 | Baue 2 Kupferadern ab | Knoten `resource.copper_vein` × 2 | 50 |
| 12 | Baue den Schmelzofen | Gebäude `building.smelter` | 60 |
| 13 | Fertige einen Kupferbarren | Rezept `craft_copper_bar` | 60 |
| 14 | Baue das Sägewerk | Gebäude `building.sawmill` | 60 |
| 15 | Baue die Seilerei | Gebäude `building.ropewalk` | 60 |
| 16 | Fertige das Fanggerät | Rezept `craft_catch_device` | 80 |
| 17 | Fange deinen ersten Eidra | Eidra gefangen | 150 |

**Summe: 940 EP.** Zusammen mit den ohnehin fälligen Erstfertigungs- und
Erstbau-EP der Stationen trägt die Kette den Spieler bis zum Fanggerät auf
etwa Level 5 (Kurve: Level 1→4 kosten zusammen 705 EP). Das deckt sich mit
dem Bibel-Prüfpunkt „Basisvorrat trägt bis Level 4" und lässt „Garon ab
Level 12" unberührt.

Die Sense (Schritt 9) steht auf ausdrücklichen Wunsch in der Kette
(„Baue die Axt, die Sense, usw.", 16.08.2026).

## 3. Architektur

Nach dem Muster der bestehenden Kataloge und Dienste (§9: Logik in reinem
C#, Unity nur als Träger):

- **`QuestChainDefinition`** (ScriptableObject, `Eidren.Data`): geordnete
  Schrittliste; je Schritt `id`, Anzeigetext, Bedingungsart (Enum:
  `ResourceNodeCompleted`, `RecipeCrafted`, `BuildingConstructed`,
  `EnemyDefeated`, `EidraCaptured`), Ziel-Id (bei Knoten/Rezept/Gebäude),
  Zielanzahl (bei Zählschritten), EP. Gebaut und gepflegt über einen
  Editor-Builder (`QuestChainContentBuilder`), registriert in der
  ContentDatabase wie `TechnologyTree_V01`.
- **`QuestProgressService`** (reines C#, `Eidren.Core`): hält Definition und
  Laufstand (Schrittindex, Zähler, abgeschlossen). Eingänge:
  `NotifyResourceNodeCompleted(nodeId)`, `NotifyRecipeCrafted(recipeId)`,
  `NotifyBuildingConstructed(buildingId)`, `NotifyEnemyDefeated()`,
  `NotifyEidraCaptured()`. Bei Schrittabschluss vergibt er die EP über den
  `PlayerProgressionService` (neue Methode `RecordQuestStepCompleted(int)`,
  symmetrisch zu `RecordEnemyDefeated`) und feuert `StepChanged` für das HUD.
- **Verdrahtung an bestehenden Aufruforten** (kein neuer Spielfluss):
  `CraftingService` (neben `RecordRecipeCrafted`), `BuildingService.Batch`
  (neben `RecordBuildingConstructed`), `ZoneResourcePopulator` (neben
  `RecordResourceNodeCompleted` — dort ist die Knoten-Id bekannt, die der
  Progression-Aufruf selbst nicht führt), Gegner-Tod (neben
  `RecordEnemyDefeated`), `EidraCaptureTarget` (nach erfolgreichem
  `TryCapture`).
- **HUD:** ein schlichtes Aufgaben-Label (aktueller Schritt + Zählerstand),
  per UI-Builder in das bestehende HUD eingebaut; bei Kettenabschluss eine
  Abschlussmeldung, danach blendet das Label dauerhaft aus.

## 4. Persistenz und Migration (v14 → v15)

- Neuer Save-Baustein `QuestChainState { ChainId, StepIndex, StepProgress,
  Completed }`, Spielstandsversion **15**, Migrationsfall 14 in der
  bestehenden Kette.
- **Bestandsspielstände:** Die Migration leitet den Stand aus den bereits
  gespeicherten Erstlisten ab (`FirstCraftedRecipeIds`,
  `FirstBuiltBuildingIds`, Eidra-Bestand im Roster):
  - Ist ein Eidra im Bestand → Kette gilt als abgeschlossen. (Nur das
    Fanggerät zu besitzen schließt NICHT ab: Wer nie gefangen hat, soll den
    Abschluss-Schritt noch sehen und verdienen.)
  - Sonst werden Rezept- und Gebäudeschritte, deren Bedingung der Bestand
    schon erfüllt, still und **ohne EP** abgehakt, sobald die Kette sie
    erreicht; die Kette steht damit stets auf dem ersten wirklich offenen
    Schritt.
  - Zählschritte (Knoten, Gegner) sind aus dem Bestand nicht ableitbar und
    bleiben offen — sie sind schnell nachgeholt und geben dann regulär EP.
  Begründung: Die Quest-EP sind Einstiegshilfe, kein Nachschlag für
  Fortgeschrittene; zugleich kann sich niemand durch die Migration EP
  erschleichen, weil EP nur für echte, neue Abschlüsse fließen.

## 5. Bibel-Nachtrag

Die EP-Tabelle der Kette (Abschnitt 2) wird in der PROGRAMMIERBIBEL unter
M13.4 nachgetragen, damit Bibel und Code nicht auseinanderlaufen.

## 6. Nachweise (TDD-Schichten)

1. **EditMode, rein:** Schrittfolge und Zähler (Reihenfolge erzwungen, kein
   Überspringen), EP-Vergabe je Abschluss, Kettenabschluss, falsche Ereignisse
   ohne Wirkung, Persistenz-Roundtrip, Migrationsableitung (alle drei Fälle
   aus Abschnitt 4).
2. **EditMode, Katalog:** Kette vollständig (17 Schritte, Summe 940 EP),
   jede Ziel-Id existiert im jeweiligen Katalog (Rezepte, Gebäude,
   Ressourcenknoten), Abschluss-Schritt ist der Eidra-Fang.
3. **EditMode, Save:** Migrationskette 14→15 (Gate `> 15`, Zukunftsfall 16),
   nach dem Muster der bestehenden Migrationstests.
4. **PlayMode:** Zone laden, ersten Schritt im HUD sehen; einen Schritt real
   auslösen (z. B. Rezept fertigen) und das Vorrücken samt EP-Gutschrift
   nachweisen.
