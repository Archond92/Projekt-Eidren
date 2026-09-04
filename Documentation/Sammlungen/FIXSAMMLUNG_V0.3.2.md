# Fixsammlung v0.3.2

**Status: abgeschlossen und veröffentlicht am 20.08.2026.** Neun Einträge
(F32-001 bis F32-009), alle umgesetzt. Ausgeliefert als Vorabversion
`v0.3.2` — Paket `Eidren-v0.3.2-windows-x64.zip`, SHA-256
`e7ffcbe61842378ca9d4cdd3d410490dab34c8f5ae60744bf7fed759377f335d`,
101 Dateien, 253,41 MiB. Abnahme: EditMode 1235/1235, PlayMode 144/0 bei
7 bewussten Übersprüngen, Player-Smoke auf dem gebauten Paket bestanden.

Neue Meldungen gehören in eine neue Sammlung. Die zurückgestellten Punkte
dieser Runde stehen in `OFFENE_PUNKTE.md`.

Die Nummerierung **F32-xxx** ist bewusst getrennt von der abgeschlossenen
`FIXSAMMLUNG_V0.3.1.md` (F31-001 bis F31-021), der abgeschlossenen
`FIXSAMMLUNG_V0.2.1.md` (F-001 bis F-014) und der abgeschlossenen
`WUNSCHSAMMLUNG_20260814.md` (W-001 bis W-010, T-001). Die laufende
`FEATURESAMMLUNG_V0.4.md` (N04-xxx) bleibt unberührt.

**Basis:** v0.3.1 (Commit `1513f3d`, Branch `codex/v0.3-release`).
**Gespieltes Paket:** `Eidren-v0.3.1-windows-x64`, gebaut am 19.08.2026, 00:52.
Alle Befunde unten sind gegen genau diesen Stand geprüft — der Code in den
Zitaten steckt also in der Fassung, die der Test gesehen hat.

## Bewusst nicht aufgenommen

**Tutorial-Questkette bei Bestandsspielständen (19.08.2026, Nutzerentscheid:
„passt so").** Gemeldet war, dass die Questkette im Spiel nicht erscheint.
Befund: Sie ist im v0.3.1-Paket vollständig enthalten — Dienst, Katalog
(17 Schritte / 940 EP) und das Aufgaben-Label im Kampf-HUD. Der laufende
Spielstand wurde am 16.08. angelegt, also vor der Kette, und beim ersten Laden
unter 0.3.1 greift die dokumentierte Migrationsregel aus
`ENTWURF_QUESTSYSTEM_V031.md` §4: **ein vorhandener Eidra schließt die Kette
ab.** Im Spielstand steht seither

```json
"quest": { "chainId": "quest.tutorial.first_eidra", "stepIndex": 17,
           "stepProgress": 0, "completed": true }
```

bei drei Eidra im Bestand. `QuestHudPresenter.Configure` blendet das Label bei
abgeschlossener Kette dauerhaft aus; die Abschlussmeldung bleibt ebenfalls aus,
sie ist echten Fängen vorbehalten. **In einem neuen Spiel erscheint die Kette
ab Schritt 1.** Bestandsspielstände sehen sie nie — das ist so entschieden und
bleibt so.

---

## F32-001 — Gebäude sind von weit weg anwählbar, direkt daneben gewinnt der Nachbar

**Bereich:** Interaktion, Zielauswahl, Basisbau

**Beobachtung (19.08.2026):** Ein Gebäude lässt sich noch aus großer Entfernung
anwählen. Steht die Figur dagegen unmittelbar davor, ist manchmal nur das
**Nachbargebäude** wählbar. Gewünscht: Ein Gebäude ist anwählbar, wenn man
direkt daran steht.

**Befund (19.08.2026) — beide Hälften der Beobachtung haben dieselbe Wurzel:
gemessen wird zum Mittelpunkt, nicht zum Gebäude.**

**1. Die Entfernung zählt bis zum Drehpunkt der Zelle.**

```csharp
// InteractionTargetSelector.cs
private static bool IsInRange(IInteractable target, in InteractionContext context)
    => … && Distance(target, in context) <= Mathf.Max(0f, target.InteractionRange);

private static float Distance(IInteractable target, in InteractionContext context)
    => InteractionUtility.FlatDistance(context.ActorTransform.position, target.InteractionPosition);
```

`InteractionPosition` ist bei jedem Gebäudeteil schlicht `transform.position`
(`WorkbenchController.cs:42`, `StorageContainer.cs:57`) — also der Mittelpunkt
der Rasterzelle.

**2. Die Maße dazu.** Das Baraster hat `CellSize = 1` Meter
(`BuildGridOrigin.cs:7`), und **jedes** Gebäude ist genau eine Zelle groß.
Nachgemessen an den Prefab-Kollidern:

| Prefab | Kollider (x/y/z) |
|---|---|
| Workbench, StorageChest | 1 / 1,5 / 1 |
| Smelter | 1 / 2 / 1 |
| Sawmill | 1 / 2,2 / 1 |
| Ropewalk | 1 / 1,6 / 1 |
| Stonecutter | 1 / 1,4 / 1 |
| CookingPot | 1 / 1 / 1 |
| FarmPlot | 2 / 0,15 / 2 |
| Wall, Door | 1 / 2,6 / 0,2 |

Die Figur hat Radius 0,48 (`Player.prefab`, CharacterController). **Berührt sie
eine Gebäudewand, ist ihr Mittelpunkt 0,5 + 0,48 = rund 0,98 m vom
Gebäudemittelpunkt entfernt** — das ist die kleinste physisch mögliche
Entfernung.

**3. Die Reichweiten stehen dagegen bei 2,4 bis 2,8 m** — gesetzt beim Bau:

```csharp
// BuildingInstanceView.cs:59, :68
Workbench.Configure(…, 2.4f, value.CraftingStation);
Storage.Configure(InstanceId, "Lagerkiste öffnen", null, 2.5f);
```

(Acker 2,8 · Ressourcenknoten 2,7 · Weltkiste 2,5 · Schmiedekiste 2,5 ·
Formwall 3,0 · Verlies-Ein/Ausgang 2,8.)

**Damit ist der erste Teil der Beobachtung erklärt:** 2,4 m vom Mittelpunkt
eines 1 m breiten Körpers heißt **rund 1,9 m freie Luft zwischen Figur und
Wand** — knapp zwei weitere Gebäudebreiten Abstand. „Direkt dran" (0,98 m) und
„zwei Zellen weit weg" (2,4 m) sind für die Prüfung derselbe Fall.

**4. Der Nachbar gewinnt, weil Rang und Blickrichtung so schwer wiegen wie eine
ganze Rasterzelle.** Die Rangfolge unter den Kandidaten entscheidet nicht die
Entfernung, sondern:

```csharp
private const float PriorityReach = 0.01f;   // 1 Rangpunkt = 1 cm
private const float FacingReach   = 0.5f;    // genaues Anvisieren = 50 cm

EffectiveDistance = Distance − Priority × 0,01 − Alignment × 0,5
```

Die vergebenen Ränge: Werkbank/Station 80, Acker 82, Lagerkiste 85, Weltkiste
88, Beutebehälter 90, Schmiedekiste/Formwall 91, Verliesportal 96. Der
Rangbonus reicht also von 0,80 m bis 0,96 m, der Blickbonus schwankt zwischen
−0,5 m und +0,5 m — **zusammen bis zu 1,46 m Verschiebung auf einem Raster mit
1 m Zellabstand.** Ein Nachbar eine Zelle weiter kann das Gebäude schlagen, an
dem die Figur klebt.

Durchgerechnet für den gemeldeten Fall — Figur berührt die Werkbank, die
Lagerkiste steht eine Zelle daneben:

| Blickrichtung | Werkbank (Rang 80) | Lagerkiste (Rang 85) | Sieger |
|---|---|---|---|
| genau auf die Werkbank | 0,98 − 0,80 − 0,50 = **−0,32** | 1,40 − 0,85 − 0,35 = **0,20** | Werkbank (Abstand 0,52) |
| 45° zur Kiste gedreht | 0,98 − 0,80 − 0,36 = **−0,18** | 1,40 − 0,85 − 0,50 = **0,05** | Werkbank (Abstand **0,23**) |

Und genau hier greift die Dämpfung gegen sich selbst: Ein Zielwechsel verlangt

```csharp
// InteractionController.cs:22,26 — Vorgabewerte
directionHysteresis = 0.12f;
distanceHysteresis  = 0.3f;
```

Der Abstand von **0,23 unterschreitet die 0,3** — war die Kiste beim Hinlaufen
einmal das Ziel, **bleibt** sie es, obwohl die Figur die Werkbank berührt. Das
erklärt das „manchmal": Es hängt daran, welches Ziel beim Herangehen zuerst
gegriffen hat und wie die Figur zuletzt gedreht war (`FacingDirection` ist die
Laufrichtung, sonst `transform.forward`).

**5. Nebenbefund zur Kandidatenmenge:** Die Suche selbst greift 6 m weit
(`detectionRadius = 6f`, `InteractionController.cs:19`) und sammelt alles per
`OverlapSphere` ein; die Vorauswahl trifft erst `IsInRange`. Bei 2,4–2,5 m
Reichweite konkurrieren in einer dicht bebauten Basis rund **zwei Zellringe in
alle Richtungen** gleichzeitig um das Ziel.

**Stoßrichtung (noch nicht beauftragt):**

1. **Bis zum Körper messen statt bis zum Drehpunkt.** `Distance` auf den
   nächsten Punkt des Kollider-`bounds` legen (flach, wie bisher ohne Höhe).
   Dann ist „direkt dran" ≈ 0 m, und **ein** Reichweitenwert (Größenordnung
   1,0–1,2 m Wandabstand) passt für jede Grundfläche — vom 1×1-Ofen bis zum
   2×2-Acker, der heute eine eigene größere Zahl braucht. Das erledigt beide
   Hälften der Meldung auf einen Schlag: Das weite Anwählen fällt weg, und das
   berührte Gebäude hat immer die kleinste Entfernung.
2. **Die Boni unter den Zellabstand drücken.** Rang- und Blickbonus dürfen
   zusammen nicht mehr wert sein als der Abstand zweier Nachbarn. Vorschlag:
   Blickbonus nur noch als Stichentscheid innerhalb eines schmalen Bandes
   (z. B. wenn beide Entfernungen enger als 0,25 m beieinanderliegen) statt als
   fester 0,5-m-Rabatt; `PriorityReach` entsprechend kleiner.
3. **Achtung, gemeinsamer Weg:** Derselbe Selektor bedient Ressourcenknoten,
   Kisten, Weltgegenstände und Portale. Die Körpermessung hilft dort ebenso
   (es ist dieselbe Wurzel wie beim Erz-hinter-Kiste-Fall vom 15.08.), **aber
   sämtliche Reichweitenwerte müssen danach einmal neu gesetzt werden** — 2,4
   ab Oberfläche wäre noch großzügiger als heute.

**Test:** Zwei Prüfungen auf dem reinen Selektor (EditMode, ohne Szene):
(a) Figur berührt Gebäude A, Nachbar B eine Zelle weiter, Blickrichtung um 45°
zu B gedreht → A muss gewinnen, auch wenn B zuvor das Ziel war;
(b) Figur steht 1,5 m vor der Wand von A → A darf **nicht** mehr anwählbar
sein. Ein reiner „Gebäude ist anwählbar"-Test genügt nicht — es *ist*
anwählbar, nur aus der falschen Entfernung.

**Status: umgesetzt am 19.08.2026** — in der eingegrenzten Fassung, siehe
Umsetzungsstand am Ende. Die Körpermessung bleibt als spätere Aufräumarbeit
offen.

---

## F32-002 — Leben bleibt bei 100 stehen, der Rüstungsbonus ist unerreichbar

**Bereich:** Spielerfigur, Rüstung, Heilung

**Beobachtung (19.08.2026):** Das Leben soll auf den vollen Höchstwert steigen
können — per Trank oder durch einen Besuch der Heimatbasis. Aktuell ist trotz
Stoffrüstung bei 100 Schluss.

**Befund (19.08.2026) — der Bonus wird gesetzt, aber der Auffüllschritt läuft
davor und füllt nur bis zum Grundwert.**

**1. Die Zahlen.** Grundmaximum 100 (`PlayerPrefabBindings.cs:60`,
`maxHealth = 100f`). Der Rüstungsbonus aus F31-018 gibt je Teil so viele
Lebenspunkte, wie es Prozent Schutz beiträgt (`ProtectionRules.ArmorHealthBonus`
= Summe × 100). Die volle Stoffrüstung („Wanderer") trägt:

| Teil | Schutzbeitrag |
|---|---|
| Stoffkapuze | 0,02 |
| Stoffmantel | 0,04 |
| Stoffarmschienen | 0,01 |
| Stoffschuhe | 0,03 |
| **Summe** | **0,10 → +10 LP** |

**Das echte Maximum ist mit voller Stoffrüstung also 110.**

**2. Die Reihenfolge beim Betreten einer Zone.** `ZonePlayerSpawner` ruft als
Erstes:

```csharp
SpawnedPlayer.PrepareForRuntime();      // ZonePlayerSpawner.Bindings.cs:156
    → damageable.Initialize(maxHealth); // PlayerPrefabBindings.cs:142
```

und `Initialize` setzt den Bonus **zurück** und füllt auf den Grundwert:

```csharp
// Damageable.cs:85
_baseMaxHealth = Mathf.Max(1f, maxHealth);
_maxHealthBonus = 0f;
MaxHealth = _baseMaxHealth;   // 100
CurrentHealth = MaxHealth;    // 100
```

Erst **danach** — beim Anbinden von Ausrüstung und Kampf
(`BindExplorationCombat` → `BindSessionSelections` → `RefreshArmor`) — kommt
der Bonus zurück:

```csharp
// PlayerPrefabBindings.cs:295
damageable.SetMaxHealthBonus(_equipmentDurability.TotalArmorHealthBonus());
```

`SetMaxHealthBonus` hebt bewusst nur das Maximum und heilt nicht (so
dokumentiert, damit Anlegen kein Gratis-Heilen ist). **Ergebnis: Nach jedem
Zonenwechsel steht die Figur auf 100/110** — die oberen 10 Punkte sind nie
gefüllt.

**3. Es gibt keinen eigenen „Heimatbasis heilt"-Schritt.** Im Code steht keine
Heilung bei der Ankunft; dass ein Basisbesuch heilt, ist ein Nebeneffekt genau
dieses `Initialize` — und deshalb fest an den **Grundwert 100** gebunden, nie
an das echte Maximum.

**4. Der Trank ist an sich richtig gebaut.** `Damageable.Heal` klemmt sauber auf
`MaxHealth` (also 110), der Heiltrank gibt 55 LP. Aus 100/110 müsste ein Trank
also auf 110 gehen.

**5. Am Spielstand gegengeprüft — der Bonus kommt an, es fehlt nur das
Auffüllen.** Der laufende Spielstand (`savegame.json`, zuletzt gespeichert
18.08. 23:22 UTC, `buildVersion: 0.3.1`) trägt alle vier Stoffteile in genau
den vier gewerteten Rüstungsslots:

| Slot | Gegenstand | Haltbarkeit |
|---|---|---|
| 2 = Head | `armor_wanderer_hood` | 64 |
| 3 = Chest | `armor_wanderer_coat` | 64 |
| 4 = Hands | `armor_wanderer_bracers` | 64 |
| 5 = Legs | `armor_wanderer_legs` | 64 |

Alle vier sind heil, und `TryGetArmor` prüft die Haltbarkeit ohnehin nicht.
`ArmorSlots` zählt genau diese vier — es geht also kein Beitrag verloren. **Das
Maximum steht im Spiel damit nachweislich auf 110, die Leiste zeigt 100/110.**
Der Fehler ist ausschließlich das fehlende Auffüllen; ein zweiter Fehler liegt
nicht vor.

**Stoßrichtung (noch nicht beauftragt):**

1. **Beim Spawn erst die Rüstung kennen, dann füllen.** Sauberste Fassung:
   `Initialize` behält den gesetzten Bonus (statt ihn auf 0 zu setzen), und der
   Erstauffüllschritt läuft nach `RefreshArmor`. Alternativ ein `HealToFull()`
   direkt hinter `RefreshArmor` im Spawn-Weg — eine Zeile, aber der Bonus
   bliebe an der Reihenfolge hängen.
2. **Den Basisbesuch zur echten Regel machen.** Heilung bei Ankunft in der
   Heimatbasis ausdrücklich auslösen (`HealToFull` **nach** dem Rüstungsbonus),
   statt sie als Nebeneffekt der Initialisierung mitlaufen zu lassen. Dann
   heilt die Basis auf 110 und nicht auf 100 — und die Regel steht an einer
   Stelle, die man auch prüfen kann.
3. Am Trank ist nichts zu ändern.

**Test:** EditMode auf `Damageable` — Bonus setzen, dann initialisieren, dann
auf vollen Stand heilen: Ergebnis muss `MaxHealth` sein, nicht `_baseMaxHealth`.
Dazu ein PlayMode-Nachweis: Figur mit voller Stoffrüstung in die Heimatbasis
setzen und den vollen Stand (110) belegen.

**Status: umgesetzt am 19.08.2026 — im zweiten Anlauf.**

> ### Nachtrag: Der erste Anlauf war wirkungslos (19.08.2026, abends)
>
> Nach dem Spielen des v0.3.2-Pakets gemeldet: **weiterhin nur 100 Leben mit
> Stoffrüstung.** Der Befund war richtig, die Umsetzung nur halb.
>
> Die Analyse oben nennt **zwei** nötige Schritte: `Initialize` darf den Bonus
> nicht verwerfen, **und** nach dem Anbinden der Rüstung muss aufgefüllt
> werden. Umgesetzt wurde nur der erste. Der zweite ist zwischen Analyse und
> Umsetzung verloren gegangen — gemeldet wurde trotzdem „umgesetzt".
>
> **Warum die Änderung nichts bewirkte:** Beim Zonenwechsel entsteht die Figur
> neu. Ihr `Damageable` ist frisch, der Bonus steht auf 0, `Initialize` füllt
> also auf 100 — und erst danach hebt `RefreshArmor` das Maximum auf 110. Dass
> `Initialize` den Bonus jetzt behält, hilft nur, wenn dasselbe Damageable
> erneut initialisiert wird; genau das passiert beim Zonenwechsel nie.
>
> **Warum der Test es gedeckt hat:** Er setzte den Bonus **vor** `Initialize`
> — eine Reihenfolge, die es im Spiel nicht gibt. Er war rot, wurde grün, und
> hat nichts über den echten Ablauf ausgesagt.
>
> **Jetzt umgesetzt:** `damageable.HealToFull()` in `BindSessionSelections`,
> direkt hinter `RefreshArmor()`. Bewusst dort und nicht in `RefreshArmor`
> selbst — die Methode läuft auch beim Anlegen von Rüstung im laufenden Spiel,
> und dort darf sie nicht heilen (F31-018).
>
> **Neuer Nachweis, diesmal am echten Ablauf** (`Fixrunde032LebenTests`,
> PlayMode): Zone laden, die vier Stoffteile in die Sitzung legen, in die
> Heimatbasis wechseln, Leben messen. Vorher rot mit `Expected: 110 / But was:
> 100` — also exakt der gemeldete Zustand.
>
> **Lehre:** Ein Test, der eine Reihenfolge nachbaut statt sie zu benutzen,
> beweist nichts über die Reihenfolge. Wo ein Fehler an der Abfolge hängt,
> gehört der Nachweis in den echten Ablauf.

---

## F32-003 — Noctarions zweite Fähigkeit (Rückenmal) wirkt nur gegen Bosse

**Bereich:** Eidra, Fähigkeiten, Kampf

**Beobachtung (19.08.2026):** Die zweite Fähigkeit des lilanen Eidra
funktioniert im Kampf nicht.

**Befund (19.08.2026) — der lilane Eidra ist Noctarion, seine zweite Fähigkeit
ist das Rückenmal, und sie ist per Prüfung auf Bosse beschränkt.**

Zuordnung zweifelsfrei aus den Daten: `Noctarion.asset` trägt
`UiAccent 0,58 / 0,27 / 0,83` (Violett), `Skill1: Schattenschritt`,
**`Skill2: Rückenmal`** (Abklingzeit 9 s, Reichweite 12, Wirkdauer 6 s,
Rückenschaden ×1,45).

**Die Sperre steht an zwei Stellen:**

```csharp
// EidraTeamController.cs:584 — Prüfung vor dem Einsatz
if (ability.ExecutionType == AbilityExecutionType.BackMark && _boss == null)
{
    failure = "ZIEL KANN NICHT MARKIERT WERDEN";
    return false;
}

// EidraTeamController.cs:608 — die Ausführung selbst
private void BeginBackMark(AbilityData ability)
{
    ClearBackMark();
    _markedTarget = _boss;                 // BossController, kein Feldgegner
    _markedTarget.MarkBack(ability.EffectDuration, ability.BackDamageMultiplier);
    …
}
```

`MarkBack` gibt es **nur** am `BossController` (`BossController.cs:92`); dort
liegen auch `_backMarkUntil`/`_backMarkMultiplier` und die einzige Auswertung
im Schadensweg (`ModifyHealthDamage`, `BossController.cs:116`). Der gemeinsame
Gegner-Grundtyp `EnemyControllerBase` kennt das Rückenmal überhaupt nicht.

**Das ist der Rest von F31-016.** Die dortige Umsetzung hat die Zielsuche auf
Feldgegner erweitert — `_combatTarget` wird inzwischen mit dem nächsten
lebenden Gegner besetzt, und `TargetBattleActive` gilt für Feldgegner immer
(F31-016b, `EidraTeamController.cs:99`). Deshalb funktioniert **Skill 1
(Schattenschritt) draußen**, Skill 2 aber nicht. F31-016 hatte das ausdrücklich
offengelassen:

> „Bei der Erweiterung ist je Fähigkeit zu entscheiden, ob sie gegen normale
> Gegner sinnvoll ist oder bewusst auf Bosse beschränkt bleibt. `BackMark` auf
> Bosse zu beschränken kann gewollt sein."

**Mit dieser Meldung ist die Entscheidung gefallen: Das Rückenmal soll auch
gegen Feldgegner wirken.**

**Stoßrichtung (noch nicht beauftragt):** Das Rückenmal von `BossController`
nach `EnemyControllerBase` hochziehen — Zustand (`_backMarkUntil`,
`_backMarkMultiplier`), `MarkBack`/`ClearBackMark`, die Auswertung in
`ModifyHealthDamage` und das Aura-Bild. Der `BossController` erbt es dann,
statt es selbst zu führen. Anschließend fallen die `_boss`-Sonderwege in
`TryValidateAbility` und `BeginBackMark` weg; markiert wird `_combatTarget`.
Zu beachten: Die Aura ist heute auf Bossmaße gesetzt (Radius 2,35, Texthöhe
4,7) — für Feldgegner sind die kleineren Werte aus dem Fähigkeitsweg
(1,35 / 3,25) richtig, die dort für Nicht-Stagger-Fähigkeiten schon existieren.

**Test:** PlayMode — Rückenmal gegen einen **normalen** Gegner auslösen, den
Rückentreffer danach messen und den Faktor 1,45 belegen; dazu die Rückkehr auf
den Normalwert nach 6 Sekunden. Ein Bosskampf-Test kann den Fehler nicht
finden, dort funktioniert alles.

**Status: umgesetzt am 19.08.2026** — gemeinsam mit dem Grundsatz aus
**F32-004**, siehe Umsetzungsstand am Ende.

---

## F32-004 — Grundsatz: jede Waffe und jede Fähigkeit wirkt gegen jeden Gegner

**Bereich:** Kampf, Waffen, Eidra-Fähigkeiten — **Regel, nicht Einzelfall**

**Vorgabe (19.08.2026), vom Nutzer als OBERSTE PRÄMISSE gesetzt:** Jede
Fähigkeit und alle Waffen sollen **immer an allen Gegnern, Bossen und Eidra
im Kampf** nutzbar sein. Keine Waffe und keine Fähigkeit darf an einen
bestimmten Gegnertyp gebunden sein.

> Diese Prämisse steht über den einzelnen Einträgen. Wo ein Befund sie
> verletzt, ist das ein Fehler — unabhängig davon, ob jemand ihn gemeldet
> hat. Ein Wächtertest hält sie fest (siehe Umsetzungsstand).

**Prüfung (19.08.2026) — beide Seiten einmal vollständig durchgesehen. Ergebnis:
Die Waffen erfüllen den Grundsatz bereits, bei den Fähigkeiten verstoßen zwei
von sechs dagegen.**

### A. Waffen — kein Handlungsbedarf

Der Trefferweg fragt an keiner Stelle, **welche** Waffe zuschlägt oder **welcher
Typ** getroffen wird:

```
MeleeWeaponHitbox.Evaluate  →  CombatTargetRegistry.TryResolve
                            →  CombatHitResolver.TryApply
                            →  IDamageable.ApplyDamage
```

Getroffen wird jedes registrierte Ziel im Trefferfeld, unabhängig von der Waffe.
`WeaponFamily` wird ausschließlich in der Darstellung gelesen
(`PlayerWeaponVisual`, `WandererEquipmentVisual`, `PlayerVisualAnimator`, dazu
die Waffenskalierung in `PlayerCombatController`) — nirgends im Schadensweg.
Alle 11 Waffen (Hammer/Dolche/Speer je in Kupfer und Eisen sowie Siegelbrecher,
Aschfänge, Glutdorn) laufen durch dieselbe Kette.

Es gibt vier Sonderregeln im Schadensweg, aber **keine davon ist an einen
Gegnertyp gebunden** — sie hängen an der Lage und sind gewollt:

| Regel | Wo | Wirkung |
|---|---|---|
| Boss erst ab Kampfbeginn verwundbar | `BossController.CanReceiveDamage` | vor dem Kampf prallt jeder Treffer ab |
| Kernwächter: 35 % Schutz in geschlossener Phase | `CoreGuardianController.ApplyProtection` | 65 % gehen durch |
| Wildes Eidra nicht unter die Fluchtschwelle | `EidraWildController.ModifyHealthDamage` | Fangmechanik |
| Wildling: Fenster nach dem Stagger | `WildlingController.ModifyStaggerDamage` | Stagger-Widerstand |

(Dazu: Gegner im Zustand `Return` nehmen keinen Stagger.)

**Für die Waffen ist der Grundsatz also bereits erfüllt.** Sollte im Spiel
trotzdem eine Waffe gegen einen bestimmten Gegner wirkungslos wirken, liegt das
nicht an einer Typbindung — dann bitte Waffe, Gegner und Ort melden, das wäre
ein eigener Befund (Reichweite, Trefferwinkel oder Kollider).

**Nachgerechnet, damit der Punkt nicht wiederkommt:** Die Trefferprüfung misst
zum **Ursprung** des Gegners, nicht zu seinem Körper
(`MeleeHitboxGeometry.Contains(… target.Transform.position …)`) — dieselbe
Bauart wie in F32-001. Große Gegner verlieren dadurch einen Teil der wirksamen
Reichweite, **blockierend ist es aber nirgends**:

| Gegner | Körperradius | Abstand bei Berührung (+0,48 Figur) | Hammer 3,4 | Dolche 3,0 | Speer 4,2 |
|---|---|---|---|---|---|
| Garon | 1,25 | 1,73 | 1,67 Luft | 1,27 | 2,47 |
| Kernwächter | 1,10 | 1,58 | 1,82 | 1,42 | 2,62 |
| Granitpanzer | 0,68 | 1,16 | 2,24 | 1,84 | 3,04 |
| Wildling | 0,52 | 1,00 | 2,40 | 2,00 | 3,20 |

Jede Waffe erreicht jeden Gegner mit deutlichem Spielraum. Wird die Messung in
F32-001 auf Körper statt Drehpunkt umgestellt, sollte diese Stelle **im selben
Zug mitgeprüft** werden — sonst laufen zwei Messarten nebeneinander. Eine
Änderung der Waffenreichweiten selbst ist damit nicht gemeint; die bleibt wie
schon in der v0.3.1-Runde ausdrücklich draußen.

### B. Fähigkeiten — zwei von sechs sind an den Gegnertyp gebunden

Alle drei Eidra führen je zwei Fähigkeiten:

| Eidra | Fähigkeit 1 | Fähigkeit 2 | Stand gegen Feldgegner |
|---|---|---|---|
| Terrock | Felsbrecher (`StaggerStrike`) | Steinhaut (`Shield`) | beide **frei** (seit F31-016a/b) |
| Noctarion | Schattenschritt (`ShadowStep`) | Rückenmal (`BackMark`) | Skill 1 frei, **Skill 2 nur gegen Bosse** |
| Ignivar | Glutkreis (`EmberCircle`) | Schmelzbrand (`MoltenBrand`) | Skill 1 frei, **Skill 2 nur gegen Ziele mit Schutz** |

**Verstoß 1 — Rückenmal:** siehe **F32-003** (`_boss == null` ⇒
„ZIEL KANN NICHT MARKIERT WERDEN").

**Verstoß 2 — Schmelzbrand (Ignivar, Fähigkeit 2), bisher nicht gemeldet:**

```csharp
// EidraTeamController.cs:579
if (ability.ExecutionType == AbilityExecutionType.MoltenBrand && _combatTarget.Protection <= 0f)
{
    failure = "ZIEL HAT KEINEN SCHUTZ";
    return false;
}
```

Die Fähigkeit senkt den Schutz um 15 Prozentpunkte und verlangt deshalb einen
Schutzwert > 0. Nachgezählt in den Gegnerdaten hat den fast niemand:

| Gegner | Schutz |
|---|---|
| Granitpanzer | 0,20 |
| Risswächter | 0,15 |
| Kernwächter | 0,35 — **nur in geschlossener Phase** |
| Wildling, Rissling, Moorschleuder, Wurzelrammler | 0 |
| Wilde Eidra (Terrock, Noctarion, Ignivar) | 0 |
| **Garon** | **0** (kein Schutzfeld in `Garon.asset`) |

**Ignivars zweite Fähigkeit ist damit gegen 3 von 11 Gegnern einsetzbar — und
ausgerechnet gegen den Hauptboss Garon nicht.** Ignivar ist regulär fangbar
(`requirementAtFullHealth: 170`), das ist also kein toter Pfad.

**Stoßrichtung (noch nicht beauftragt):**

1. **Rückenmal:** wie in F32-003 beschrieben — Markierung von `BossController`
   nach `EnemyControllerBase` hochziehen, `_boss`-Sonderwege entfernen.
2. **Schmelzbrand:** Die Prüfung ist keine Typbindung, sondern eine
   Sinnhaftigkeitsprüfung — ohne Schutz gäbe es nichts zu senken. Sie muss
   trotzdem weg, sonst bleibt die Fähigkeit gegen die Mehrheit der Gegner
   gesperrt. Drei Wege, **Entscheidung des Nutzers nötig**:
   - **(a) Zweitwirkung gegen ungeschützte Ziele** — z. B. erhöhter erlittener
     Schaden für dieselbe Dauer (`DamageTakenMultiplier` existiert bereits in
     `Damageable`). Die Fähigkeit behält ihren Charakter „macht das Ziel
     weicher" und wirkt überall. **Empfehlung.**
   - **(b) Schutz in den negativen Bereich zulassen** — technisch am kleinsten,
     aber `Protection` wird auf 0..1 geklemmt; ohne Anpassung der Klemmung
     verpufft die Fähigkeit wirkungslos und der Spieler wartet umsonst auf die
     Abklingzeit.
   - **(c) Mehr Gegnern einen Schutzwert geben** — ändert die Balance aller
     Waffen mit und ist der größte Eingriff.
3. **Wächter gegen künftige Rückfälle:** eine Prüfung, die **jede** Fähigkeit
   aus dem Katalog gegen **jeden** Gegnertyp durchgeht und verlangt, dass die
   Freigabe nie aus Gründen des Zieltyps scheitert. Damit ist der Grundsatz
   dauerhaft festgehalten und nicht nur einmalig repariert — genau diese Lücke
   hat F31-016 offengelassen und uns zwei Runden später wieder eingeholt.

**Test:** (1) EditMode-Katalogtest über alle Fähigkeiten × alle Gegnerdefinitionen
(siehe Punkt 3). (2) PlayMode je betroffener Fähigkeit gegen einen **normalen**
Gegner mit Wirkungsnachweis — beim Schmelzbrand zusätzlich gegen ein Ziel **ohne**
Schutz, weil genau dort heute die Sperre greift.

**Status: umgesetzt am 19.08.2026** (Variante (a), siehe Umsetzungsstand).

---

## F32-005 — Ignivar hat kein Passiv, das HUD verspricht eines

**Bereich:** Eidra, Kampf, Anzeige

**Beobachtung (19.08.2026, beim Prüfen der Prämisse gefunden):** Ignivar
trägt `PassiveStaggerMultiplier 1,0` und `PassiveBackDamageMultiplier 1,0` —
also **gar keinen Passiveffekt**. Das HUD kündigt trotzdem
„+18 % RÜCKENSCHADEN" an.

**Befund:** Der Passivtext wurde an der **Rolle** entschieden, nicht an den
Daten des Eidra:

```csharp
PassiveDescription => (ActiveData.Role == EidraRole.Defend)
    ? "PASSIV  ·  22% SCHADENSREDUKTION  ·  AUTONOMER STAGGER"
    : "PASSIV  ·  AUTONOME ANGRIFFE  ·  +18% RÜCKENSCHADEN";
```

Jedes Angriffs-Eidra bekam denselben Satz, unabhängig von seinen Zahlen. Bei
Noctarion (1,18) stimmte er zufällig, bei Ignivar (1,0) war er eine leere
Zusage. Dieselbe Familie wie F32-004: Der Code verspricht etwas, das die
Daten nicht halten.

**Umgesetzt (19.08.2026) — zwei Teile:**

1. **Der Text kommt aus den Daten.** Ausgelagert nach `EidraPassiveText`
   (rein, ohne Unity, damit prüfbar — als Eigenschaft am Dienst war er nur
   mit komplettem Kampfaufbau erreichbar). Er nennt nur, was die Zahlen
   hergeben, und lässt eine Zeile weg, wenn der Wert 1,0 ist. Die 22 % der
   Defend-Rolle stammen jetzt aus derselben Konstante wie die Wirkung
   (`DefendDamageTakenMultiplier = 0,78`) statt aus zwei getrennten Stellen.
2. **Ignivar bekommt ein eigenes Passiv: „Treffer entzünden".** 20 % des
   Trefferschadens brennen über 4 Sekundentakte nach; ein neuer Treffer
   frischt auf und **stapelt nicht** (sonst bauen schnelle Waffen eine
   zweite Schadensquelle auf).

**Warum dieses Passiv — die Entscheidung im Dialog (19.08.):** Der Code kennt
nur zwei Passivhebel, Stagger und Rückenschaden. Terrock hat den Stagger
(1,22), Noctarion den Rücken (1,18) — und Noctarions ganzes Kit dreht sich
ums Hintergehen. Ignivar dieselben 18 % zu geben, hätte ihn zur besseren
Kopie gemacht (gleicher Bonus ohne Noctarions Stagger-Malus). Deshalb ein
**dritter Hebel**, passend zu Feuer und Zermürbung.

**Ausdrücklich verworfen: Rüstungsdurchdringung.** Das wäre die naheliegende
Feuer-Idee und das passive Echo des Schmelzbrands — scheitert aber an der
obersten Prämisse aus F32-004: **8 von 11 Gegnern tragen gar keine
Rüstung.** Es wäre derselbe Fehler wie beim Schmelzbrand, nur an anderer
Stelle. Aus demselben Grund verworfen: „mehr Schaden gegen brennende oder
gebrandmarkte Ziele" — ein Passiv, das erst nach einer eigenen Fähigkeit
greift, ist die meiste Zeit null.

Der Brand liegt wie das Rückenmal am gemeinsamen Gegnertyp
(`EnemyControllerBase.Burn.cs`) und hängt an keiner Eigenschaft des Ziels —
jeder Gegner kann brennen. Getaktet wird im Sekundenabstand aus dem
**bestehenden** `Update` (kein zweiter Frame-Pfad, §8), nach dem Vorbild des
Glutkreises; so erscheint pro Sekunde eine lesbare Schadenszahl statt eines
Zahlenregens pro Frame. Die Rechnung selbst ist reines C#
(`PassiveBurnState`).

**Damit sind die drei Eidra klar getrennt:**

| Eidra | Passiv | Spielgefühl |
|---|---|---|
| Terrock | −22 % erlittener Schaden · +22 % Stagger | hält aus, bricht Deckung |
| Noctarion | +18 % Rückenschaden (−8 % Stagger) | der eine harte Treffer von hinten |
| Ignivar | Treffer entzünden: 20 % über 4 s | anhaltender Druck |

**Nachweise:** EditMode — Verteilung auf die Takte (Summe = Gesamtschaden),
Auffrischen statt Stapeln, „jedes Eidra trägt einen wirksamen Passivwert",
„der Text verspricht nur, was die Daten halten". PlayMode — der Brand tickt
im laufenden Betrieb und zieht in Summe genau den mitgegebenen Schaden ab.

> **Nicht abgedeckt:** Die Stelle, an der ein Waffentreffer den Brand
> auslöst (`PlayerCombatController.ApplyPassiveBurn`), hat keinen
> automatischen Test — dafür bräuchte der Testaufbau eine echte Waffe samt
> Trefferfenster und Hitbox. Drei bewachte Zeilen; beim Spielen fällt es
> sofort auf, wenn ein Gegner nach einem Treffer nicht zu brennen beginnt.

**Status: umgesetzt am 19.08.2026.**

---

## F32-006 — Ein drittes gefangenes Eidra ist nicht einsetzbar

**Bereich:** Eidra, Bedienung

**Beobachtung (19.08.2026):** Es gibt keine Möglichkeit, ein drittes Eidra
gegen eines der beiden aktiven zu tauschen.

**Befund — bestätigt, und der Nutzer ist bereits betroffen.** Sein Roster:
`noctarion.001`, `noctarion.002`, `terrock.001`; aktiv sind `terrock.001`
und `noctarion.002`. **`noctarion.001` sitzt seit dem Fang auf der Bank und
war mit keinem Mittel des Spiels erreichbar.**

Die Ursache steht in `EidraRosterService.TryCapture`: Ein Fang wird **nur
dann aktiv, wenn ein Platz frei ist**.

```csharp
string[] activeInstanceIds = _roster.GetActiveInstanceIds();
if (activeInstanceIds.Length < ActiveSlotCapacity)   // sonst: ab auf die Bank
```

Sind beide Plätze belegt, landet jeder weitere Fang im Roster und bleibt
dort. Die Umschalttaste im Kampf (`TrySwitch`) wechselt nur zwischen den
**zwei aktiven** und holt nichts von der Bank. Von neun UI-Fenstern
(Inventar, Werkbank, Lager, Technologiebaum, Baumenü, Pause, …) betrifft
keines die Eidra.

Es gibt genau **eine** Ausnahme im ganzen Spiel:
`EidraForgeSceneController.SelectIgnivarForBoss` tauscht Ignivar nach dem
Fang in der Schmiede automatisch auf Platz 2 — ein Skript für diesen einen
Moment, keine Funktion.

**Verschärfend seit F32-005:** Ignivar hat jetzt ein eigenes Passiv. Wer ihn
außerhalb der Schmiede fängt, kommt ohne dieses Fenster nicht an ihn heran.

**Umgesetzt (19.08.2026):** Ein Fenster für das Eidra-Gespann auf **Taste G**
(Gamepad: D-Pad links — beide Belegungen waren frei).

- **Zuweisungsregel als reine Logik** (`EidraTeamAssignment`): freier Platz
  nimmt auf, belegter wird ersetzt, ein bereits aktives Eidra **tauscht** die
  Plätze statt sich zu verdoppeln, bei Kapazität 1 landet alles auf Platz 1.
  Das Ergebnis erfüllt den Vertrag von `TrySetActiveTeam` (dichte Liste,
  keine Duplikate, Kapazitätsgrenze).
- **Fenster** (`EidraTeamWindow` + `EidraTeamRowView`, Prefab über
  `EidraTeamUiPrefabBuilder`): Kopfzeile mit beiden Plätzen, darunter der
  Roster in einer Scrollfläche. Je Zeile Name, Instanz-Kennung, Zustand
  („AKTIV · PLATZ 1" / „auf der Bank") und die Platz-Knöpfe. Auf dem Platz,
  den ein Eidra schon belegt, verschwindet der Knopf.
- **Kein Eingriff in den Kampfcode.** Das Fenster ruft nur
  `TrySetActiveTeam`; `EidraTeamRosterBinding` hört auf `Roster.Changed` und
  setzt das Gespann live neu. Der Wechsel wirkt sofort, ohne Neuladen.
- Pausiert und schließt wie der Technologiebaum (Szenenwechsel, Tod,
  blockierte Eingabe).

**Nachweise:** EditMode — fünf Tests auf der Zuweisungsregel (freier Platz,
belegter Platz, Tausch, Kapazität 1, keine Duplikate), alle vorher rot.
PlayMode — in einer **echten Zone**: Der Spawner bindet das Fenster an, ein
Druck auf den tatsächlichen Button im Prefab wechselt den Bankspieler ein,
und der Roster übernimmt es.

> **Grenzen, bewusst gezogen:** Das Prefab hält **16 Zeilen**; bei mehr
> gefangenen Eidra steht „… und N weitere" darunter statt einer beliebig
> langen Liste. Keine Sortierung, kein Ziehen-und-Ablegen — zwei Knöpfe je
> Zeile lösen das gemeldete Problem vollständig.

**Status: umgesetzt am 19.08.2026.**

---

## F32-007 — Kisten im Verlies sind hinter Fels unsichtbar, nur das Preisschild schwebt

**Bereich:** Weltdarstellung, Sichtverdeckung, Eidra-Schmiede

**Beobachtung (19.08.2026, mit Bild):** Im Verlies steht „15 MARKEN" frei im
Raum — die Kiste darunter ist nicht zu sehen, sie steckt hinter der Geometrie.

**Befund (19.08.2026):** Die Sichtlinien-Ausblendung arbeitet ausschließlich
für **Akteure**:

```csharp
// ActorOcclusionTransparency.cs:82
_actors = System.Linq.Enumerable.ToArray(ActorPresentationRegistry.Active);
```

Registriert sind dort Spielerfigur und Kreaturen. **Kisten, Ressourcenknoten,
Gebäude und Portale stehen nicht in dieser Liste** — für sie wird nie ein
Strahl geworfen, also blendet auch nie ein Verdecker aus. Steht ein Fels
zwischen Kamera und Kiste, bleibt er undurchsichtig.

Sichtbar bleibt nur das Markenpreis-Label (aus der v0.3.1-Runde), weil es als
Weltlabel obenauf gezeichnet wird — daher der Eindruck einer schwebenden
Schrift ohne Gegenstand.

**Stoßrichtung (noch nicht beauftragt):** Die Ausblendung auf eine zweite
Gruppe erweitern — „wichtige Weltobjekte", die ebenfalls freigehalten werden.
Nicht pauschal alle Interaktionsziele: Jeder Strahl kostet, und die Prüfung
läuft pro Bild. Vorschlag: nur Ziele in Spielernähe (die Interaktionssuche
kennt ohnehin einen 6-m-Radius) und nur solche, die gerade sichtbar sein
sollen. Zu prüfen ist, ob die Kiste dann selbst als Verdecker gilt und sich
gegenseitig ausblendet.

**Umgesetzt (20.08.2026):** Eine eigene, kurze Liste
(`OcclusionFocusRegistry`) statt „alle Interaktionsziele" — jeder Eintrag
kostet pro Abtastung einen Strahl. Angemeldet sind **nur Kisten**
(Schmiedekisten und Weltkisten); Ressourcenknoten und Gebäude bleiben
bewusst draußen, dort ist eine Verdeckung kein Rätsel. Berücksichtigt werden
nur Einträge in **28 m Kameraentfernung** — was außerhalb des Bildes liegt,
muss niemand freihalten.

Die befürchtete Wechselwirkung ist ausdrücklich behandelt: Ein Fokusobjekt
darf sich **nicht selbst** ausblenden. Der Strahl endet knapp vor der Kiste,
die Prüfkugel (Radius 0,18) streift sie aber — ohne den Ausschluss wäre die
Kiste durchsichtig geworden statt der Fels. Ein Test, der nur „irgendetwas
blendet aus" geprüft hätte, wäre dabei grün geblieben.

**Nachweis** (`Fixrunde032SichtTests`, PlayMode): Kamera, Kiste, ein Fels
dazwischen — der Fels muss durchsichtig werden. Vorher rot mit Alpha 1,0.

> **Testfalle, die dabei auffiel:** Der erste Lauf zeigte Alpha 0,998 — der
> Mechanismus lief, aber das Wartebudget des Tests war in `Time.deltaTime`
> gerechnet, während die Blende mit `Time.unscaledDeltaTime` arbeitet. Im
> Batchmodus laufen beide weit auseinander (deltaTime gedeckelt,
> unscaledDeltaTime sehr klein). Das Budget zählt jetzt **Bilder**. Kein
> Spielfehler — im Spiel mit ~60 Bildern/s blendet es normal.

**Status: umgesetzt am 20.08.2026.**

---

## F32-008 — Man kann in Requisiten hineinlaufen (Esse in der Schmiede)

**Bereich:** Kollision, Eidra-Schmiede, Weltaufbau

**Beobachtung (19.08.2026, mit Bild):** Die Figur steht **im** achteckigen
Körper der Esse.

**Befund (19.08.2026) — die Requisiten der Schmiede haben grundsätzlich keine
Kollider.** Der Bauhelfer, durch den fast jedes Schmiedeteil entsteht:

```csharp
// EidraForgePropBuilder.Teil()
teil.AddComponent<MeshFilter>().sharedMesh = mesh;
teil.AddComponent<MeshRenderer>().sharedMaterial = material;
return teil;            // kein Collider
```

Im ganzen Builder gibt es genau **zwei** Kollider (ein BoxCollider an einer
Stelle, eine Auslöser-Kugel) — keiner davon gehört zur Esse. Ihr Körper heißt
`Esse_Koerper` und entsteht als achteckiger Loft über diesen Helfer.

**Das erklärt zugleich den nie reproduzierten Altbericht „Durchlaufen einer
Verlieswand" aus der v0.3.1-Runde.** Die **Wände** stammen aus
`EidraForgeGeometryBuilder`, und der setzt sehr wohl `BoxCollider` — deshalb
waren die Kollisionsprüfungen grün und die Suche lief ins Leere. Durchlaufen
lassen sich nicht die Wände, sondern die **Requisiten**.

**Stoßrichtung (noch nicht beauftragt):** Im Prop-Builder entscheiden, was
Hindernis ist und was Deko bleibt — Glutrisse und Glutadern am Boden sollen
begehbar bleiben, Esse, Windrohre und Amboss nicht. Sauber wäre ein Schalter
am Helfer (`Teil(..., fest: true)`), damit die Entscheidung je Teil sichtbar
im Code steht statt implizit zu fehlen. Danach die Schmiede neu bauen
(Achtung: Szenen-Rebuild wirft nachträglich eingebaute Teile ab — dieselbe
Falle wie beim Verlies-Eingang, sechster Fall).

**Test:** Ein Wächter, der über alle Teile der gebauten Schmiede geht und
verlangt, dass die als fest gekennzeichneten einen Kollider tragen. Ein
reiner „Wand hat Kollider"-Test reicht nicht — der war grün, während das
Problem danebenstand.

**Umgesetzt (20.08.2026) — mit umgekehrter Voreinstellung.** Der Helfer
`Teil(...)` setzt jetzt **standardmäßig einen Kollider**; die Ausnahmen
stehen sichtbar als `fest: false` im Code: Glutrisse, Glutadern, der
Glutschein in der Esseöffnung, die Pfannenglut und der Bauplatzgrund. Diese
Umkehrung ist Absicht — vorher war „kein Kollider" der stille Normalfall und
fiel niemandem auf; jetzt muss das Durchlaufen ausdrücklich hingeschrieben
werden.

Ein Kollider auf einer Bodenzeichnung wäre eine Stolperkante gewesen — ein
Fehler gegen einen anderen getauscht. Der Wächter prüft deshalb **beide
Richtungen**: fehlender Kollider am Aufragenden ist ein Fehler, vorhandener
Kollider an der Bodenzeichnung ebenso.

Schmiede neu gebaut. Der rote Lauf nannte namentlich: Windrohre 1–6,
`Esse_Koerper`, `Esse_Deckplatte` und sämtliche Geländer samt Pfosten. Nach
dem Neubau sind alle Schmiede-Wächter grün (13/13) — Grundriss, Requisiten,
Licht, Verliesdaten, Formwall und die bestehenden Wandprüfungen. **Der
Neubau hat nichts abgeworfen**, die dokumentierte Falle ist diesmal nicht
zugeschnappt.

**Status: umgesetzt am 20.08.2026.**

---

## Abgearbeitete Empfehlungen (20.08.2026)

Keine Nutzermeldungen, sondern eigene offene Punkte aus dieser Runde. Auf
Auftrag „mach die Empfehlungen, wo du nichts von mir brauchst".

### E-1 — Das Testloch an Ignivars Brand ist geschlossen

Der bestehende Brandtest ruft `ApplyBurn` **direkt am Gegner** auf. Er belegt
Takt und Schadenssumme, aber nicht, dass ein Waffentreffer den Brand
überhaupt auslöst — und genau dazwischen sitzt
`PlayerCombatController.ApplyPassiveBurn`.

Neu: `Fixrunde032BrandAmTrefferTests` fährt die ganze Kette in einer echten
Zone — gefangenes Ignivar, ausgerüstete Waffe, echter Angriffsknopf, echter
Treffer — und prüft danach, dass das Ziel brennt **und** ohne weiteren Angriff
weiter Leben verliert. Dazu eine Gegenprobe mit Terrock: Ohne Ignivar darf
derselbe Treffer nicht entzünden, sonst misst der Nachweis nicht das Passiv.

**Rot nachgewiesen:** Mit auskommentierter Aufrufstelle fällt der
Ignivar-Test (`Expected: True / But was: False`), die Gegenprobe bleibt grün.

### E-2 — Feste Weltsaat für die ganze PlayMode-Suite

Die Zonen wurden pro Lauf neu gewürfelt; ein grüner Wiederholungslauf bewies
nichts. Neu: `ZoneStateService.DefaultMasterSeed` (produktiv `null`, also
weiterhin zufällig) und ein `[SetUpFixture]` in der Testassembly, das die Saat
für den ganzen Lauf festnagelt und danach wieder abräumt.

**Zwei Dinge, die beim Bauen aufgefallen sind und ohne Test durchgerutscht wären:**

1. **`Reset()` ohne Argument hätte die Saat weggeworfen** — und genau so ruft
   `GameSession.StartNewGame` auf. Das Festnageln wäre still wirkungslos
   gewesen. Der EditMode-Test `ResetOhneArgument_BehaeltDieVorgabe` ist rot
   gestartet und hat es gezeigt.
2. **Unity führt Assembly-weite `ITestAction` nicht aus.** Der erste Anlauf
   hing als `[assembly: FesteWeltsaat]` an der Testassembly und feuerte nie —
   ohne dass ein einziger Test umfiel, denn ein fehlender Haken macht die
   Läufe nur wieder zufällig. Aufgefallen ist es allein durch
   `WeltsaatHakenTests`, einen Test, der nichts anderes prüft, als dass der
   Haken gelaufen ist. Merksatz: Stiller Infrastrukturcode braucht einen
   eigenen Zeugen.

### E-3 — Wer am Weltursprung baut, sorgt selbst für eine leere Welt

Die ursprüngliche Empfehlung lautete, alle **38** zonenladenden Tests auf
`Testumgebung.LeereWeltHinterlassen` umzustellen. Beim Nachsehen war die
andere Richtung die belastbarere: Betroffen sind nicht die 38 Lader, sondern
die **10 Tests, die am Weltursprung bauen und gar keine Szene laden** — sie
sind die Opfer. Neu ist deshalb `Testumgebung.LeereWeltBereitstellen()` als
erste Zeile in diesen 18 Testkörpern.

Der Unterschied ist nicht kosmetisch: In der Aufräum-Variante kann ein neu
geschriebener zonenladender Test die Ursprungstests weiterhin umwerfen, wenn
sein Autor die Regel nicht kennt. In dieser Variante nicht.

### E-4 — §16 der Bibel trug einen überholten Beleg

Er hielt fest, `CombatHUD` baue sich „mit über 1300 Zeilen und sechs
`Build*`-Methoden" selbst zusammen. Nachgemessen: **98 Zeilen, keine einzige
`Build*`-Methode.** Der alte Beleg bleibt stehen (er zeigt, wogegen die Regel
geschrieben wurde), darunter steht jetzt der heutige Stand.

### E-5 — Der CHANGELOG-Schnitt ist gesetzt

„Unveröffentlicht" mischte v0.3.1-Nachfixe mit den F32-Einträgen. Getrennt
anhand der Auslieferungscommits: Kernwächter-Leiste und Verlies-Minimap
gehören zu **Paketaustausch 4** (18.08., `f5c37f2`), Verlies-Ausgang,
Markenpreis und Felsportal zu **Paketaustausch 5** (19.08., `1513f3d`). Was
danach kam, ist v0.3.2. Der Abschnitt hält jetzt außerdem fest, dass der
gebaute Stand `ce7c137d` alles bis auf den umbrechenden Tooltip-Balken
enthält.

---

## F32-009 — Der Tooltip-Balken ist zu kurz für seinen Text

**Bereich:** HUD, Bedienung

**Beobachtung (20.08.2026, mit Bild):** Der schwarze Balken des Tooltips ist
für den Text zu kurz — „Abklingzeit 12 s" steht rechts außerhalb. Gewünscht:
zwei Reihen oder mehr, wenn nötig.

**Befund — beides sind Fehler in meinem eigenen Builder von wenige Stunden
zuvor (N04-003):**

```csharp
panelRect.sizeDelta = new Vector2(760f, 46f);           // feste Höhe
text.horizontalOverflow = HorizontalWrapMode.Overflow;  // läuft heraus statt umzubrechen
```

`Overflow` habe ich von den übrigen HUD-Beschriftungen übernommen, wo es
richtig ist — dort sind es kurze Einzelwörter, die nicht mitten im Wort
brechen sollen. Beim Tooltip ist es genau falsch: Der Text ist lang und
variabel.

**Und die längste Fassung ist ausgerechnet der Schmelzbrand**, seit er seit
F32-004 **beide** Wirkungen nennt („Schutz −15 %P, sonst +15 % Schaden"). Die
Zeile, die den Balken sprengt, ist also eine direkte Folge des Fixes vom
selben Tag — ein Muster übernommen, ohne zu prüfen, ob seine Begründung noch
gilt.

**Umgesetzt (20.08.2026):** Text auf **Umbrechen**, Balken wächst mit dem
Inhalt (`VerticalLayoutGroup` + `ContentSizeFitter` auf Vorzugsgröße), Breite
von 760 auf **900** und **fest**.

Die feste Breite ist Absicht: Ein Balken, der je nach Fähigkeit unterschiedlich
breit ist, springt bei jedem Überfahren in der Größe und liest sich unruhig.
Feste Breite, variable Zeilenzahl ist das ruhigere Verhalten.

**Nachweis:** `Fixrunde032TooltipBalkenTests` prüft am Prefab, dass der Text
umbricht **und** der Balken einen Größenanpasser auf Vorzugshöhe samt
Layoutgruppe trägt — damit niemand die Höhe später wieder festnagelt. Vorher
rot mit `Expected: Wrap / But was: Overflow`; danach 14/14 grün zusammen mit
den bestehenden HUD-Prefab-Wächtern.

**Status: umgesetzt am 20.08.2026 und ausgeliefert.** Zwischenzeitlich war
F32-009 auf Nutzerentscheid aus Bau 4 herausgehalten; mit dem
Veröffentlichungspaket vom 20.08. (`e7ffcbe6…`) ist er drin.

---

## Umsetzungsstand F32-001 bis F32-004 (19.08.2026)

Alle vier Einträge sind umgesetzt. Vorgehen durchgehend rot→grün: erst der
Test, der den gemeldeten Zustand festhält, dann die Änderung.

### F32-002 — Lebensmaximum

`Damageable.Initialize` setzt den Rüstungsbonus nicht mehr auf 0 zurück und
füllt auf `Grundwert + Bonus` statt auf den Grundwert. Damit kommt die Figur
bei der Ankunft in einer Zone — Heimatbasis eingeschlossen — auf **110/110**
statt 100/110. Am Trank war nichts zu ändern.

Roter Nachweis vor der Änderung: `Expected: 110.0f / But was: 100.0f`.

### F32-003 + F32-004 — Fähigkeiten ohne Zielbindung

- Das Rückenmal liegt jetzt an `EnemyControllerBase` (neue Teildatei
  `EnemyControllerBase.BackMark.cs` — die Hauptdatei hat 745 Zeilen und
  wächst nicht weiter; §7-Budget). `BossController` behält davon nur die
  größeren Aura-Maße (2,35 / 4,7) als Überschreibung; seine eigene
  `ModifyHealthDamage`-Fassung ist entfallen, weil sie nur das Rückenmal tat.
  `EidraWildController` rechnet die Fangklemmung jetzt auf dem **markierten**
  Schaden — sonst wäre das wilde Eidra der einzige Gegner ohne Wirkung.
- Beide Zielsperren in `TryValidateAbility` sind ersatzlos entfallen.
- **Schmelzbrand, Variante (a) wie empfohlen:** Gegen ein Ziel mit Schutz
  senkt er weiterhin den Schutz um 15 Prozentpunkte; gegen ein Ziel **ohne**
  Schutz trägt er denselben Betrag als Schadensaufschlag (+15 % erlittener
  Schaden für 6 s, neuer `Damageable.SetTimedDamageTakenModifier` nach dem
  Muster des vorhandenen Schutz-Modifikators). Die Aura nennt jeweils die
  wirksame Fassung. **Das war eine Annahme:** Die Rückfrage nach (a)/(b)/(c)
  blieb unbeantwortet, umgesetzt wurde die Empfehlung. Umstellen auf (b) oder
  (c) ist eine kleine Änderung an genau dieser Stelle.

Nachweise in PlayMode gegen einen gewöhnlichen Wildling (nicht gegen einen
Boss): Rückenmal setzt die Markierung; Schmelzbrand macht ein ungeschütztes
Ziel messbar weicher (10 → 11,5 Schaden bei gleichem Treffer). Roter Nachweis
vorher: „vorher 10, nachher 10".

**Wächtertest über die volle Matrix.** `KeineFaehigkeit_ScheitertAmGegnertyp`
(PlayMode) geht **alle sechs Fähigkeiten der drei Eidra** gegen einen
gewöhnlichen Wildling durch und verlangt, dass keine davon scheitert; die
Fehlermeldung nennt Fähigkeit und Grund. Damit ist die Prämisse festgenagelt
und nicht nur einmalig repariert.

> **Nachtrag zur Reihenfolge:** In der ersten Fassung dieses Umsetzungsstands
> stand hier, die Matrixprüfung sei „teurer als ihr Nutzen" und deshalb auf
> zwei Einzelnachweise reduziert. Nachdem der Nutzer die Regel zur **obersten
> Prämisse** erklärt hat, war diese Abwägung falsch — der Wächter ist
> nachgezogen worden.

### Zusätzlicher Befund aus der Prämissenprüfung: Boss-Vorfahrt ohne Reichweite

Nicht gemeldet, beim Nachprüfen gefunden. `TryAcquireFieldTarget` nahm bei
laufendem Bosskampf **immer** den Boss als Ziel — ohne jede
Reichweitenprüfung:

```csharp
if (_boss != null && _boss.IsAlive && _boss.BattleActive)
{
    _combatTarget = _boss;
    return;                      // keine Reichweite geprüft
}
```

Stand der Boss weiter weg, als die Fähigkeit reicht, scheiterte sie an
„AUSSER REICHWEITE" — **obwohl ein gewöhnlicher Gegner in Schlagweite
stand**. Damit war eine Fähigkeit mitten im Kampf unbenutzbar, was die
Prämisse direkt verletzt. Betroffen ist vor allem die Eidra-Schmiede, wo
neben dem Kernwächter weitere Gegner stehen.

**Umgesetzt:** Der Boss behält Vorfahrt, **solange er in Reichweite der
jeweiligen Fähigkeit steht**; sonst greift die normale Suche nach dem
nächstgelegenen gültigen Ziel. Situationen, die heute funktionieren, ändern
sich dadurch nicht — nur die, in denen die Fähigkeit bisher blockiert war.

Roter Nachweis vorher: `Expected: null / But was: "AUSSER REICHWEITE"` bei
einem Wildling in 5 m und einem Boss in 30 m (Felsbrecher-Reichweite 8).
Danach grün, zusammen mit beiden echten Bosskampf-Testklassen
(Garon-Begegnung, Kernwächter-Trefferkette) — der Boss in Reichweite behält
seinen Vorrang.

**Bewusst gelassen:** Ein Boss ist vor Kampfbeginn unverwundbar
(`BossController.CanReceiveDamage → BattleActive`). Das ist keine
Typbindung, sondern der Auslöser der Arena — sie sagt nicht „du bist ein
Boss, das gilt für dich nicht", sondern „dieser Kampf hat noch nicht
begonnen", dieselbe Kategorie wie „ein toter Gegner nimmt keinen Schaden".
Nach „**im Kampf** nutzbar" ist das konform. Falls der Nutzer es anders
sieht, fällt auch diese Prüfung.

### F32-001 — Zielauswahl am Gebäude

Umgesetzt ist die **eingegrenzte Fassung**, nicht die Körpermessung aus
Stoßrichtung 1:

1. **Blickbonus auf Stichentscheid gestutzt.** `FacingReach` 0,5 → 0,15;
   die Spannweite zwischen „genau angesehen" und „weggedreht" sinkt damit
   von 1,0 m auf 0,3 m, deutlich unter den Zellabstand von 1 m. Die
   Zielwechsel-Dämpfung sinkt entsprechend von 0,3 auf 0,2. **Der Rang
   bleibt bei 0,01** — siehe die Korrektur unten.
2. **Gebäudereichweiten auf eine Kachel Luft.** Werkbank, Lagerkiste,
   Schmelzofen, Sägewerk, Seilerei, Steinmetz und Kochtopf von 2,4–2,6 auf
   **2,0**; der Acker (als einziges Gebäude 2×2) von 2,8 auf **2,5**.

   > **Korrigiert am 19.08.2026 abends.** Der erste Anlauf setzte 1,6 (bzw.
   > 2,0 für den Acker) — „eine Armlänge" über der Berührung. Das war zu eng:
   > Es lässt nur 0,62 m Luft, weniger als eine halbe Kachel, und der Nutzer
   > meldete mit Bild, dass er sichtbar an der Werkbank steht und sie nicht
   > anwählen kann. In einer isometrischen Ansicht lässt sich die Figur nicht
   > auf 60 cm genau stellen. Neu ist **genau eine Kachel Luft** über der
   > Berührung, und die Prüfung ist ein **Band** statt einer Obergrenze — zu
   > eng fällt jetzt genauso durch wie zu weit. Der gemeldete Nachbarfall
   > bleibt behoben: Der lag an der Blickrichtung, nicht an der Reichweite. Bei Berührung sind
   es 0,98 m zur Zellmitte (1,48 m beim Acker) — bleibt gut eine halbe
   Figurenbreite Luft, statt bisher fast zwei Zellbreiten. Die Werte standen
   doppelt (Vorgabewert **und** eine zweite Zahl in `BuildingInstanceView`);
   die zweite ist entfallen, gebaut wird jetzt mit dem Wert der Komponente.

**Warum nicht die Körpermessung:** Sie hätte `InteractionSession`,
`StorageWindow`, `CraftingWindow`, die aus der Reichweite abgeleiteten
Trigger-Radien der Ressourcenknoten und sämtliche Reichweitenwerte auf einmal
umstellen müssen — vier Systeme für einen gemeldeten Fall an Gebäuden. Die
eingegrenzte Fassung ist am selben Test nachgewiesen und lässt die
Körpermessung als spätere Aufräumarbeit offen; der Eintrag bleibt dafür
stehen.

**Korrektur am eigenen Fix (19.08.2026, im Voll-Lauf aufgefallen).** Die
erste Fassung hatte **beide** Boni gestutzt, den Rang von 0,01 auf 0,002
mit. Das war eine Überkorrektur, und der Voll-PlayMode hat sie gefunden:
`LootIntegrationTests.CorpseIsTheInteractionTargetAndShowsTheHand` schlug
fehl — die Figur stand auf einer frischen Leiche, und ein zufällig etwas
näher gewürfelter Beerenstrauch zog das Ziel an sich.

Nachgerechnet am gemeldeten Fall: Zwischen Werkbank (Rang 80) und
Lagerkiste (Rang 85) liegen bei altem Gewicht **0,05 m** — der Rang war am
Nachbarproblem völlig unbeteiligt, schuld war allein die Blickrichtung mit
ihrer 1,0-m-Spannweite. Gebraucht wird der Rang dort, wo die Ränge **weit**
auseinanderliegen: Leiche (90) gegen Beerenstrauch (10) sind 0,8 m
Vorsprung, und genau den braucht man, um nach einem Kampf zu plündern statt
zu pflücken.

`PriorityReach` steht deshalb wieder auf 0,01. Der gemeldete Gebäudefall
bleibt behoben: Werkbank 0,074 gegen Kiste 0,400 — Vorsprung 0,33 und damit
über der Dämpfung von 0,2.

> **Lehre:** Zwei Stellschrauben gleichzeitig zu drehen, wenn eine reicht,
> verschiebt Fehler statt sie zu beheben. Der zufällig gewürfelte
> Beerenstrauch hat es aufgedeckt — ein grüner Lauf allein hätte es nicht
> getan ([[Zonen werden pro Lauf neu gewürfelt]]).

**Bewusst nicht mitgeändert:** Weltkisten, Weltgegenstände und wilde Eidra
behalten 2,6, Ressourcenknoten 2,7. Sie waren nicht gemeldet, und die
gestutzten Boni helfen ihnen bereits. Damit ist eine Kiste weiterhin aus
größerer Entfernung greifbar als eine Werkbank — falls das stört, ist es ein
eigener Eintrag.

### Nachweise

| Lauf | Ergebnis |
|---|---|
| **EditMode, Voll-Suite** | **1225/1225 grün** (Vorbaseline 1211 + 14 neue Tests) |
| **PlayMode, Voll-Suite** | **140 grün, 0 rot, 7 Skips** (Vorbaseline 139 Tests + 8 neue) |

> **Drei Testisolationsfallen in einer Runde** — Kampfziel-Liste (Fähigkeiten),
> Zeitbasis (Ausblendung), Hauptkamera (Ausblendung). Jedes Mal war der
> gefilterte Lauf grün und der Voll-Lauf rot, und jedes Mal lag es an etwas
> Statischem oder Globalem, das den einzelnen Test überlebt. Das ist kein
> Zufall mehr: Ein gemeinsamer Testaufbau, der Kamera, Registries und
> Szenenreste zurücksetzt, wäre billiger als weitere Einzelreparaturen.

Neue Tests: `Fixrunde032Tests` (EditMode, 8: Nachbarauswahl, Reichweiten,
Lebensmaximum, Rückenmal-Struktur, zwei Brandtests, Passivwerte, Passivtext)
sowie `Fixrunde032TeamTests` (EditMode, 5: Zuweisungsregel),
`Fixrunde032EidraTests` (PlayMode, 5: Rückenmal, Schmelzbrand,
Matrix-Wächter, Boss außer Reichweite, Nachbrand im Betrieb) und
`Fixrunde032TeamWindowTests` (PlayMode, 1: Einwechseln über das Fenster in
einer echten Zone).

**Exe:** `Eidren-v0.3.2-windows-x64.zip`, 265.703.670 Bytes (253,39 MiB
gepackt, 349,97 MiB entpackt, 101 Dateien), SHA-256
`01c123cbe8db917afe5921ba0b9b0ad84af33e3d3bcf20e43c7e7a21ca4dd9a4`, Smoke
grün mit Fade-Zwilling-Beweis. **Nicht veröffentlicht** — kein Tag, kein
GitHub-Release, keine Discord-Posts.

Die Architekturprüfungen (§5 Locator, §7 Zeilenbudget) und die bestehenden
Selektor-Tests aus `InteractionSystemTests` laufen unverändert mit — die
neuen Konstanten haben keinen davon gekippt. Die 7 Skips sind die bekannten
Grafik- und Capture-Tests, namensgleich zum Lauf davor.

### Nachtrag: die beiden PlayMode-Tests waren zuerst ordnungsabhängig

Im **gefilterten** Lauf grün, im **Voll-Lauf** rot — und zwar ohne
Fehlermeldung der Fähigkeit, nur ohne Wirkung. Ursache: Der Fähigkeitsdienst
sucht sein Ziel selbst aus der statischen `CombatTargetRegistry`, und dort
standen noch Gegner aus früheren Tests. Die Fähigkeit traf einen von ihnen,
unser Wildling blieb unberührt — die Zielprüfung war zufrieden, also fiel
keine Meldung.

Behoben in den Tests, nicht im Spielcode: Sie schalten vor dem Aufbau alle
fremden Gegner ab (beim Abschalten tragen sie sich aus der Liste aus) und
prüfen zusätzlich per Reflexion, dass das gewählte Ziel wirklich der eigene
Wildling ist — sonst nennt die Fehlermeldung den fremden Namen. Danach:
Voll-Lauf grün.

> **Merksatz für künftige Fähigkeitstests:** Ein grüner gefilterter Lauf
> beweist hier nichts. Die Kampfziel-Liste ist statisch und überlebt den
> Test; ein Test, der sein Ziel nicht selbst festnagelt, misst unter
> Umständen eine fremde Kreatur.

---
