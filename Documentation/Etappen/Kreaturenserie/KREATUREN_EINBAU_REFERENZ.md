# Kreaturen-Einbau in Unity

**Stand: 13. August 2026 — alle 14 Kreaturen im Spiel**

Der Einbau der 3D-Kreaturen in Unity, in zwei Stufen — beide abgeschlossen:

1. **Darstellungs-Prefabs bauen** — alle 14, abgenommen.
2. **Anbindung an die Spiellogik** — elf Gegner-Prefabs umgestellt
   (zehn Gegner + Boss), die drei Eidra über Laufzeit-Wrapper.

Dazwischen lag ein Befund, den erst die Sichtprüfung in Unity aufdeckte:
einseitige `platte()`-Flächen in allen Quellen (Abschnitt „Rückseiten-Befund").

Wer eine weitere Figur einbaut, braucht nur diesen Text.

Verwandt: `KREATURENSERIE_STAND.md` (Bau der Modelle),
`WANDERER3D_EINBAU_PLAN.md` (das Muster, dem dieser Einbau folgt).

## Warum eine eigene Darstellungsschicht

`MeshActorPresentation` ist für den **Wanderer** gebaut und trägt die
Kreaturen nicht:

| | Wanderer | Kreaturen |
| --- | --- | --- |
| Clipnamen | `Ruhe_<Haltung>`, `Angriff_Hammer` … | `Ruhe`, `Gehen`, `Angriff` … |
| Haltungen | Speer, Dolche, Hammer | keine |
| Treffer, Taumeln, Tod, Erscheinen | **prozedural**, weil die GLB sie nicht hat | **echte Clips** |
| Rüstungs- und Waffenknoten | ja | keine |

Der prozedurale Rückfall wäre bei den Kreaturen ein Rückschritt: sie tragen
für jeden Zustand einen autorierten Clip.

Deshalb **`CreatureMeshPresentation`**
(`Scripts/Presentation/CreatureMeshPresentation.cs`) als Schwesterklasse.
`ActorVisualState` bleibt unangetastet.

**Ein Zustand, ein Clipname, kein Ersatz.** Fehlt ein Clip, bleibt der
laufende stehen — sichtbar falsch ist besser als unsichtbar erfunden.

## Die Namenstabelle

Die Kreaturen tragen unterschiedlich viele Zustände: **acht** bei den elf
V02-Kreaturen, **zehn** bei den Eidra, **neun** beim Boss. `ActorVisualState`
fasst nur acht. Die darüberhinausgehenden laufen über
`SetAuthoredState(stem)`, das schon frei benannte Zustände kennt;
`StemToClip` übersetzt den Sprite-Stamm in den Clipnamen der GLB:

| Sprite-Stamm | Clip | gilt für |
| --- | --- | --- |
| `idle` `move` `hit` `stagger` `death` `appear` | Ruhe Gehen Treffer Taumeln Tod Erscheinen | alle |
| `telegraph` | Telegraph | alle außer Garon |
| `attack` | Angriff | die elf V02-Kreaturen |
| `wildattack` `flee` | Wildangriff, Flucht | Noctarion, Terrock |
| `shadowstep` `backmark` | Schattenschritt, Schattenmal | Noctarion |
| `rockbreaker` `stonehide` | Felsbrecher, Steinhaut | Terrock |
| `front` `spin` `charge` `return` | Frontschlag, Wirbel, Ansturm, Rückkehr | Garon |

**Diese Tabelle ist der eigentliche Wert des Einbaus.** Ohne sie liefe er bei
Noctarion, Terrock und Garon ins Leere — deren Clipnamen sind nicht die ihrer
Sprite-Zustände.

`CreatureMeshPresentationTests` prüft sie **gegen die GLB-Dateien**: jede
erwartete Clipliste wird direkt aus dem JSON-Teil der GLB gelesen und
verglichen. Ein Tippfehler in der Tabelle fiele sonst erst im Spiel auf, als
stumm stehende Figur.

## Der Bauweg

`CreatureMeshBuilder` (`Editor/CreatureMeshBuilder.cs`), idempotent, drei
Schritte je Figur:

1. **Legacy-Import erzwingen.** glTFast importiert GLB standardmäßig mit
   Mecanim/Animator; die Schicht braucht eine `Animation`-Komponente mit
   Legacy-Clips. Gesetzt wird `importSettings.animationMethod` auf 1.
   **Derselbe Befund wie beim Wanderer** — dort steht er in
   `Wanderer3DPlayerBuilder.EnsureLegacyAnimationImport`.
2. **Material sichern.** Ein gemeinsames `M_Kreatur_VertexLit.mat` auf dem
   Shader `Eidren/Actors/WandererVertexLit`. Die Kreaturen tragen ihre Farben
   als Vertexfarben, genau wie der Wanderer.
3. **Prefab bauen.** GLB instanziieren, Material auf alle
   `SkinnedMeshRenderer`, `Animation` holen, `playAutomatically = false`,
   `CreatureMeshPresentation` anhängen und mit der **Sollhöhe** konfigurieren.

Einstiegspunkte im Menü:

- `Eidren/V0.2/Build Riftling3D (Referenzfall)`
- `Eidren/V0.2/Build alle Kreaturen 3D`

## Was der Referenzlauf ergeben hat

```
Eidren.Editor.CreatureMeshBuilder.BuildRiftling
  -> Assets/_Game/Prefabs/Actors/3D/Riftling_3D.prefab
```

| Prüfung | Ergebnis |
| --- | ---: |
| Kompilierung | sauber, keine `error CS` |
| `CreatureMeshPresentationTests` | **27 von 27 bestanden** |
| Prefab trägt die Schicht | ja |
| Sollhöhe am Prefab | 1,70 m |
| Clips am Prefab | alle acht |

### Regressionsvergleich gegen die Baseline

Voller EditMode-Lauf nach dem Bau **aller** Prefabs, per Namensdiff gegen
`TestResults-stil-e5-abschluss.xml` (nicht gegen `failed=0` — die Baseline hat
69 bekannte Fehlschläge). Beleg: `TestResults-k3d-t5-abnahme.xml`.

| | Baseline | nach dem Einbau |
| --- | ---: | ---: |
| Testfälle | 831 | 1018 |
| Fehlschläge | 69 | 70 |

**Neu fehlgeschlagen: einer** —
`StyleProofContentTests.ReusableStyleProofAssets_ArePresentAndCompatible`:
ein `ContactShadow`-Renderer in `SP_Accent_GlowMushrooms.prefab` nutzt
`M_World_ContactShadow` statt `M_EidrenWorld_VertexLit`.

**Das kommt nicht aus dieser Arbeit.** Das Prefab wurde am 11.08. um 01:44
geändert, das Material um 01:51 — beides vor Beginn des Einbaus und nach dem
Baseline-Lauf vom 10.08. Der Einbau hat ausschließlich
`Scripts/Presentation/CreatureMeshPresentation.cs`,
`Editor/CreatureMeshBuilder.cs`, `Editor/Tests/CreatureMeshPresentationTests.cs`
und die Kreaturenordner angefasst. Als eigener Befund weitergegeben.

Kein bisher roter Test ist grün geworden — der Einbau hat also auch nichts
verdeckt.

**Die GLB-Dateien waren vorher nie importiert** — keine der 15 hatte eine
`.meta`. Der erste Builderlauf hat den Import ausgelöst.

## Alle 14 Figuren — gebaut und abgenommen

`CreatureMeshBuilder.BuildAll()` hat alle 14 Prefabs erzeugt; in
`Assets/_Game/Prefabs/Actors/3D/` liegen sie neben dem Spieler-Prefab.

`JedesPrefab_TraegtSchichtHoeheUndAlleClips` prüft **jede** Figur auf drei
Dinge und meldet alle Abweichungen gesammelt:

1. Das Prefab trägt `CreatureMeshPresentation`.
2. Die Sollhöhe am Prefab stimmt mit der verbindlichen überein.
3. **Alle** Clips seiner GLB hängen an der `Animation` — acht bei den
   V02-Kreaturen, zehn bei Noctarion und Terrock, neun beim Garon.

Ergebnis: **27 von 27 Testfällen bestanden**, kein Fehlschlag.

| Prüffall | Fälle |
| --- | ---: |
| `ResolveClipName_DecktAlleZustaendeAb` | 8 |
| `StemToClip_UebersetztAlleSpriteZustaende` | 13 |
| `StemToClip_LiefertNullStattErsatz` | 3 |
| `JedeKreatur_TraegtGenauDieErwartetenClips` | 1 (alle 14 GLB) |
| `JederClipnameDerKreaturen_IstUeberEinenStammErreichbar` | 1 |
| `JedesPrefab_TraegtSchichtHoeheUndAlleClips` | 1 (alle 14 Prefabs) |

Damit ist auch belegt, dass die Zusatzzustände der Eidra und des Bosses am
Prefab ankommen: ihre Clips (`Flucht`, `Schattenschritt`, `Schattenmal`,
`Felsbrecher`, `Steinhaut`, `Frontschlag`, `Wirbel`, `Ansturm`, `Rueckkehr`)
sind Teil der geprüften Listen.

## Der Gegner-Umbau

Die Darstellungs-Prefabs allein ändern nichts am Spielbild. Erst der Umbau der
Gegner-Prefabs stellt sie scharf. Am **Riftling** durchgespielt, die übrigen
stehen aus.

### Warum ersetzt und nicht ergänzt

`ActorPresentationLocator.Find` (`Scripts/Presentation/ActorPresentation.cs`)
sucht mit **`includeInactive: true`** und liefert die **erste** Darstellung im
Baum. Bliebe die `SpriteActorPresentation` stehen, entschiede die
Kindreihenfolge, welche Darstellung der Gegner ansteuert.

**Ein Nebeneinander ist daher nicht stabil, sondern zufällig** — und
Deaktivieren genügt nicht, weil der Locator auch inaktive Knoten sieht. Die
Sprite-Darstellung muss weg.

### Was der Umbau anfassen darf — und was nicht

Vor dem ersten Eingriff nachgesehen, was am Sprite hängt. Ergebnis: **nichts.**

| Bauteil | hängt am Aktorbild? | Befund |
| --- | --- | --- |
| `DynamicActorGroundShadow` | nein | eigener `shadowRenderer`, misst dessen Sprite, zielt auf die Wurzel |
| `ActorSpriteVfx` | nein | erzeugt seinen `SpriteRenderer` zur Laufzeit selbst |
| `WildlingStatusBars` | nein | kein Bezug auf Renderer oder Höhe |
| Anker, Trefferzonen | nein | reine Transformationen |
| `SpriteActorAnimator` | **ja, ein Feld** | `presentationBehaviour` |

Der `HandPaintedSprite`-Knoten hatte **keinen einzigen Fremdverweis** außer der
Kindliste seines Elternknotens und eben jenem Feld. Sein Wegfall reißt nichts
mit.

**`SpriteActorAnimator` trägt seinen Namen zu Unrecht.** Er arbeitet
ausschließlich gegen `IActorPresentation` und `IAuthoredStatePresentation` —
beide erfüllt `CreatureMeshPresentation`. Kein Cast auf die Sprite-Klasse, also
keine Änderung nötig. Seine Stämme (`telegraph`, `attack`, `stagger`, `move`)
deckt die Namenstabelle ab.

Die Sprite-Klasse führt zusätzlich `IArmorPresentation` — das liest nur
`PlayerVisualAnimator`. Für Gegner geht dabei nichts verloren.

### Der Umbauweg

`CreatureEnemyRewire` (`Editor/CreatureEnemyRewire.cs`), idempotent, fasst nur
den `Visual`-Knoten an:

1. Vorhandenen `Mesh3D`-Knoten entfernen — macht den Lauf wiederholbar.
2. Jede `SpriteActorPresentation` entfernen. Sitzt sie auf eigenem Knoten (der
   Regelfall), fällt der Knoten; sitzt sie auf `Visual` oder der Wurzel, nur
   die Komponente.
3. `<Name>_3D.prefab` als `Mesh3D` unter `Visual` einhängen, Lage auf null,
   Maßstab auf eins.
4. `SpriteActorAnimator.presentationBehaviour` per `SerializedObject` umhängen.
5. **Nachweisen**, dass `ActorPresentationLocator.Find` jetzt die
   Kreaturenschicht liefert — sonst wird geworfen und **nicht gespeichert**.

Schritt 5 ist der Kern. Ohne ihn fiele ein misslungener Umbau erst im Spiel
auf, als stumm stehende Figur.

Menüeintrag: `Eidren/V0.2/Gegner auf 3D umstellen — Riftling (Referenzfall)`.
**Bewusst kein Sammeleintrag**, solange die übrigen 13 nicht abgenommen sind.

### Was der Referenzlauf ergeben hat

Am Riftling, gegen die Prefab-Datei nachgeprüft statt der Logmeldung geglaubt:

| Prüfung | Ergebnis |
| --- | --- |
| `HandPaintedSprite` | entfernt |
| `SpriteActorPresentation` | keine mehr im Prefab |
| `Mesh3D` | verschachtelte Instanz von `Riftling_3D.prefab` |
| `GroundShadow`, drei Anker, Trefferzonen | unberührt |
| `presentationBehaviour` | zeigt in die neue Instanz |
| Locator | liefert `CreatureMeshPresentation` |

**Bodenlage geprüft:** das Modell spannt 0,012 – 1,712 m, also exakt 1,700 m
bei 12 mm Bodenfreiheit. Alle Knoten des Gegners stehen auf Maßstab 1 und Lage
null — das Modell erbt keine Sprite-Skalierung und sitzt bei `Visual` y = 0
richtig auf dem Boden, passend zum `FootAnchor` bei 0,04.

### Ein Test musste angepasst werden

`V02ContainerVisualTests.TierTwoGameplayPrefabs_UseTheirOwnActorIdentity`
verlangte, dass **jedes** TierTwo-Prefab eine `SpriteActorPresentation` mit
eigener `ActorId` trägt — genau das, was der Umbau entfernt.

Vor der Anpassung geprüft, ob dabei etwas Echtes verlorengeht: **`ActorId`
liest kein Spielcode.** Nur Tests und `V02ActorVisualBuilder`; es ist ein
reiner Marker der 2D-Pipeline.

Der Test unterscheidet jetzt: Gegner **mit** 3D-Schicht dürfen **keine**
Sprite-Darstellung mehr tragen, Gegner **ohne** müssen weiter ihre eigene
`ActorId` haben. Die Wache bleibt damit scharf — und ein halb umgebautes
Prefab, das beides trägt, fällt neu auf.

### Regressionsvergleich — Endstand nach dem Vollausbau

Volles Paket nach allen Umbauten: EditMode, PlayMode und ein Isolationslauf.
Belege: `TestResults-k3d-final-editmode.xml`, `…-final-playmode.xml`,
`…-isolation.xml`.

| | Baseline | Endstand |
| --- | ---: | ---: |
| EditMode-Fälle | 831 | 1019 |
| EditMode-Fehlschläge | 69 | **69 — Namensdiff leer** |
| PlayMode-Fälle (Baseline 06.08.) | 117 | 117 |
| PlayMode-Fehlschläge | 4 | 6 |

**EditMode: kein einziger neuer Fehlschlag.** Der zwischenzeitliche
`StyleProofContentTests`-Fall ist durch den nachgezogenen
ContactShadow-Vertrag wieder grün. Kein bisher roter Test ist grün geworden —
nichts wurde verdeckt.

**PlayMode: zwei neue Rote, beide entlastet bzw. zugeordnet:**

- `FloatingText_IsResetAndReused` — **isoliert grün**, also
  ordnungsabhängig; derselbe Befund wie beim Isolationslauf vom 06.08.
- `CopperMining_SynchronizesTwoVisibleImpactBursts` — **auch isoliert rot**,
  ein echter Schaden, aber nicht aus dieser Arbeit: **kein Prefab im Projekt
  trägt mehr `CopperMiningVisualFeedback`**; die CopperVein-Visuals wurden am
  10.08. vom Stilumbau neu geschrieben und haben die Komponente verloren.
  Die PlayMode-Suite lief seit dem 06.08. nicht mehr, deshalb fiel es erst
  jetzt auf. Als eigene Aufgabe weitergegeben.
- `CurrentWallPrefab_BlocksPlayerMovement` war im Zwischenlauf einmal rot,
  im Endlauf und isoliert grün — flatternd, nicht aus dieser Arbeit.

Neu und grün: `UmgebauteGegner_ZeigenAufDieKreaturenschicht` prüft an allen
**elf** umgestellten Prefabs, dass keine Sprite-Darstellung übrig ist, der
Modellknoten steht, der Animator darauf zeigt, der Locator die Schicht
liefert, genau EIN Animator existiert und Schatten wie Anker den Umbau
überlebt haben. Dazu `Garon3D_LoadsAllBossStatesAtProductionScale` am
lebenden Boss und `TheMarshUsesCanonicalNoctarion3DVisuals` an den wilden
Eidra im Marsh.

## Der Vollausbau — alle 14 im Spiel (13.08.2026)

Nach der Abnahme des Referenzfalls kam die Freigabe für den Rest. Stand jetzt:

**Alle elf Gegner mit eigenem Prefab sind umgestellt** (`RewireAll`,
Menü „Gegner auf 3D umstellen — alle elf"). Zwei Sonderfälle, die das
Werkzeug dabei gelernt hat:

- **Garons Knoten heißt `Garon_Visual`**, nicht `Visual`. Der Einhängepunkt
  wird deshalb notfalls über den `SpriteActorAnimator` gefunden, der in jedem
  Fall auf diesem Knoten sitzt.
- **Die Forge-Gegner betten ihr 2D-Prefab als eigenen Knoten ein**
  (`Visual/EmberEater_2D/…`) — mit Ankern und Bodenschatten DARIN und
  dadurch mit **zwei** `SpriteActorAnimator` und zwei `ActorSpriteVfx`.
  Nach dem Umbau würden beide Animatoren dieselbe Kreaturenschicht
  ansteuern; der Umbau lässt je Typ genau den ersten stehen (den der
  Controller per `GetComponentInChildren` bindet). Der Abnahmetest prüft
  „genau ein Animator" seither mit.

**Die drei ohne eigenes Prefab (Ignivar, Noctarion, Terrock) laufen jetzt
über 3D-Wrapper.** `EidraWildController` und `EidraTeamController` laden die
Darstellung zur Laufzeit per `Resources.Load` aus einer Pfadtabelle und
erwarten im geladenen Prefab einen `SpriteActorAnimator` (der Wild-Controller
castet darauf und ruft `Bind`). `EidraVisual3DBuilder` baut deshalb Wrapper,
die dem 2D-Prefab strukturell gleichen — er **kopiert das 2D-Prefab und
stellt die Kopie mit denselben Schritten um wie ein Gegner-Prefab**. Damit
erben die Wrapper jede Feldkonfiguration (Stems, Partikelfarben,
Schattenmaße), statt sie zu duplizieren. Die beiden Pfadtabellen zeigen auf
`Prefabs/Actors/3D/<Name>_3D`; die 2D-Prefabs bleiben liegen (ihre eigenen
Prüfungen `NoctarionPresentationPlayModeTests`/`TerrockPresentationPlayModeTests`
laden sie direkt und bleiben gültig).

**Angepasste Tests** — jeweils erst geprüft, dass nichts Echtes verlorengeht:

| Test | vorher | nachher |
| --- | --- | --- |
| `TierTwoGameplayPrefabs_UseTheirOwnActorIdentity` | Sprite + `ActorId` Pflicht | 3D-Gegner: keine Sprite-Darstellung; 2D-Gegner: weiter `ActorId`-Pflicht |
| `Garon2D_LoadsAllBossStatesAtProductionScale` | 9 Zustände als Sprite-Kacheln am lebenden Boss | **`Garon3D_…`**: 9 Stämme → gespielte Clips (`CurrentClip`) am lebenden Boss |
| `TheMarshUsesCanonicalNoctarionSpriteVisuals` | 2D-Prefab-Pflicht am Wild | **`…Noctarion3DVisuals`**: Wrapper-Pflicht, keine Sprite-Darstellung |
| `ReusableStyleProofAssets_ArePresentAndCompatible` | jeder Renderer = `M_EidrenWorld_VertexLit` | `ContactShadow`-Decals (G-004-Erdung, 14 von 16 Requisiten) ausgenommen — der Test hinkte der abgenommenen Erdung hinterher |

Zwei Testnamen haben sich dabei geändert (`Garon2D_…`→`Garon3D_…`,
`…NoctarionSpriteVisuals`→`…Noctarion3DVisuals`) — im Namensdiff verschwinden
die alten und kommen die neuen; beide alten waren grün.

## Der Rückseiten-Befund (13.08.2026) — gefunden durch die Sichtprüfung

Die erste Sichtprüfung in Unity zeigte den Riftling **ohne Kopf**. Ursache:
Der Helfer `platte()` baute in allen Quelldateien vier Dreiecke gegen den
vorderen Gratpunkt — **ohne Rückfläche**. Der Quell-Viewer zeichnet Backfaces
und verbarg das; Unity cullt sie. Freistehende Platten (Kammdornen,
Panzerschuppen, Mähnen) fehlten deshalb im Spiel von hinten; bei fünf Figuren
sichtbar bis hin zum Blick in den offenen Rumpf (Noctarion).

Fix: zwei Rückflächen-Dreiecke je Platte (invertierte Windung, dunkleres AO)
in **allen 14 Quellen**, Export, Windungsbilanz-Prüfung, Prefab-Neubau,
Rückansichts-Captures. Kosten +3–16 % Dreiecke (je Figur im README
nachgetragen). Wanderer und Wildling-Vorlage haben kein `platte()` und waren
nicht betroffen.

**Nachtrag zum Ablauf (13.08. nachmittags):** Die GLB-Exporte vom Vormittag
(12:13) liefen noch **vor** dem Quellen-Fix — die Windungsbilanz zeigte den
Riftling danach weiterhin einseitig (24:0 im Kronenband). Erst der Neuexport
aller 14 am Nachmittag hat den Fix in die GLB gebracht; seitdem sind 13 von 14
ausgewogen, alle `verify_gltf`-Läufe grün, Prefabs neu gebaut und die
Rückansichten erneut gerendert (Riftling-Kamm und Noctarion-Rumpf geschlossen).

**„Dunkler Rückendeckel = Stil" hielt nur teilweise:** Beim **GraniteShell**
war die fast schwarze Heckkappe (`capBackCol: P.fuge`) KEIN Stil, sondern ein
Fehlgriff — der Sprite-Atlas zeigt den Panzer **rundum steinfarben**. Die
Kappe ist auf `P.stein` korrigiert (das ao 0.66 des Endrings dunkelt sie als
Schattenseite); die Rückansicht zeigt jetzt beschatteten Fels statt eines
scheinbaren Lochs. Die verbleibende Nord/Süd-Asymmetrie im Kuppelband
(8:37) ist die legitime, nach hinten abfallende Scheitelform.

**Nachprüfung EmberEater und Garon (13.08. abends): dieselbe Abweichung,
derselbe Fix.** Beide Atlanten widerlegen die dunklen Deckel — der
Garon-Panzer ist in der Vorlage RUNDUM goldbraun gemustert, der
EmberEater-Rücken schwarzgraue Kohleschuppen mit zentraler Lavaader, nicht
braun. Beide `capBackCol: P.schatten` sind auf die jeweilige
Panzer-Grundfarbe umgestellt (`P.stein` bzw. `P.basis`), Export, Rebuild
und Rückansichts-Captures belegen den Wechsel. Damit ist die
„Schattenfarben-Caps sind abgenommener Stil"-These in allen drei Fällen
widerlegt. Offen als Feinabstimmung: das goldbraune MUSTER des
Garon-Hecks und die Schuppen-/Aderstruktur des EmberEater-Rückens sind
Modellierarbeit, ebenso der braune `P.haut`-Mantelton der
EmberEater-Hinterhand. Da die Spielkamera steil ist, sind Rücken und
Deckel die meistgesehenen Flächen — diese Feinarbeit lohnt.

**Regel daraus:** Sichtprüfung neuer Figuren immer AUCH von hinten und in
Unity, nie nur im Quell-Viewer — der Viewer verbirgt einseitige Flächen, und
er kann selbst der Ursprung einer Abweichung sein.

## Kupfer-Abbaufeedback wiederhergestellt (13.08.2026)

Beifang der Regressionprüfung: `CopperMining_SynchronizesTwoVisibleImpactBursts`
(PlayMode) war **auch isoliert rot** — kein Ordnungseffekt, sondern ein echter
Bruch aus dem Stilumbau. Der `T1ResourceVisualBuilder` hatte
`CopperVein_Active.prefab` neu erzeugt und die
`CopperMiningVisualFeedback`-Komponente der alten Geometry_A14-Hierarchie
ersatzlos fallen lassen (belegt im Kommentar von
`VisualAssetTests.CopperVeinUsesDetailedThreeDimensionalHeroGeometry`); der
EditMode-Strukturtest wurde damals nachgezogen, der Verhaltenstest nicht. Im
Spiel fehlte seitdem jedes Schlag-Feedback am Kupfer.

`CopperMiningFeedbackRebuilder` (`Editor/CopperMiningFeedbackRebuilder.cs`,
Menü `Eidren/V0.2/Kupfer-Abbaufeedback wiederherstellen`) verdrahtet die
Komponente idempotent auf den Fabrik-Vertrag: die Wurzel wackelt (früher die
OreCrown), die `OreShard_00..04` sind Funkenpunkte und Flash-Ziele, zwei neue
Unlit-Materialien (`M_CopperMining_Stone/…_Spark`) färben die
Laufzeit-Partikel. Ins Prefab kommen weder Renderer noch Collider — die
Strukturtests der Fabrik blieben unberührt (nachgeprüft: beide grün).

## Endstand-Läufe (13.08.2026, nach allen Eingriffen)

Nach Neuexport, Prefab-Neubau, GraniteShell-Kappe und Kupfer-Fix, als
Namensdiff gegen die jeweilige Baseline. Belege:
`TestResults-k3d-endstand-editmode.xml`, `TestResults-k3d-endstand-playmode.xml`.

| Suite | Fälle | rot | neu gegenüber Baseline |
| --- | ---: | ---: | --- |
| EditMode | 1019 | 69 | **keiner** — exakt die 69 bekannten |
| PlayMode | 117 | 5 | nur `FloatingText_IsResetAndReused` |

`FloatingText_IsResetAndReused` ist zweifach als **ordnungsabhängig** belegt
(isoliert grün am 06.08. und 13.08., im Suitenlauf rot) — kein Schaden aus
dieser Arbeit, als eigene Aufgabe ausgegliedert. `CopperMining…` und
`CurrentWallPrefab…` sind wieder grün; die vier übrigen Roten sind die
bekannten Altfälle der Baseline (Building, Consumables, zweimal Loot).

## Navigationsradius — entschieden (13.08.2026)

Stichprobe am Extremfall RootCharger: die breiteste Alpha-Zeile seiner
Idle-Kachel misst 211 px ≙ **2,11 m Sichtbreite** — das 2D-Bild im Spiel war
also sogar 5 cm **breiter** als das 3D-Modell (2,061 m). Das Bild ragte schon
immer weit über den Agenten (1,23 m) hinaus; der 3D-Umbau ändert die
Sichtbreite nicht. **`AgentRadius` bleibt unangetastet** — Sichtbild breiter
als Kollisionsradius ist zudem übliche Praxis, damit Gegner nicht an
Engstellen hängen.

## Noch offen

**KORRIGIERT (13.08. abends) — zwei Behauptungen dieses Abschnitts waren
falsch, und die Prüfmethode dahinter war wertlos:**

1. *„Kein Gegner-Prefab steht in einer Szene"* stützte sich auf Textsuche
   nach GUIDs in den `.unity`-Dateien — **die Szenen sind aber binär
   serialisiert** (365 kB bei 609 „Zeilen"), Textsuche beweist dort gar
   nichts. Das Unity-Inventar (`WildlingSceneRewire.Inventar`) fand
   **27 szenenplatzierte Gegner**: 12 Wildlinge und 15 **ausgerollte
   2D-Kopien** von Riftling, RootCharger, MoorThrower und GraniteShell in
   den TierTwo-Zonen (Szenennamen sind die displayName-Werte: Rissling,
   Wurzelstürmer, Moorwerfer, Granitpanzer). Ausgerollt heißt: keine
   Prefab-Instanzen — **der Prefab-Umbau hatte sie nicht erreicht**, sie
   liefen weiter in 2D.
2. *„Der Wildling hat kein 3D-Modell"* war schlicht falsch:
   `Art/Actors/Wildling/Wildling3D/Wildling.glb` existiert seit dem
   06.08.2026, mit Abnahme-Renders und README — er ist die **Ur-Figur** der
   Serie. Er fehlte nur in der Einbau-Kette.

## Der Wildling und der Szenen-Umbau (13.08.2026)

**Wildling in die Kette geholt:**

- `Erscheinen` nachgerüstet (aus `riftling.html` portiert — gleiches
  17-Knochen-Rig; Export-Liste in `export_gltf.js` war hart kodiert und
  musste mitwachsen). Jetzt 8 Clips wie die Serie; bodencheck des neuen
  Clips: tiefster Punkt +10 mm.
- In `CreatureMeshBuilder.Creatures` (jetzt **15** Figuren) und in die
  Erwartungslisten der Tests aufgenommen. `Wildling_3D.prefab` gebaut.
- **Höhenentscheid:** das Modell misst 1,645 m — die am 06.08. gegen die
  Sprite-Vorlage abgenommene Sichthöhe. `worldHeight` am Prefab ist 1,90
  (VisualScaleTable `visual.wildling`), derselbe Raycast-Wert, den auch das
  2D-Quad trug. Das Modell wurde bewusst NICHT skaliert.

**Szenen-Umbau** (`Editor/WildlingSceneRewire.cs`, Menü
`Eidren/V0.2/Wildling-Szenen …`): öffnet die zwölf Szenen und stellt jeden
`WildlingController`-Träger um — gleiche Schritte wie `CreatureEnemyRewire`,
plus eine **Definition-Id→Modell-Tabelle**, damit jede ausgerollte Kopie IHR
Modell bekommt (unbekannte Id bricht ab, statt still das falsche
einzuhängen). Ergebnis: **27 umgestellt** — 12× Wildling_3D, 4× Riftling_3D,
4× GraniteShell_3D, 4× MoorThrower_3D, 3× RootCharger_3D; sieben Szenen
gespeichert, Sicherungen aller zwölf im Sitzungs-Scratchpad.

Die PlayMode-Wache `OutdoorScenesInitializeExactWildlingCounts` verlangt
seitdem die **Kreaturenschicht** statt der Sprite-Darstellung (Locator
liefert `CreatureMeshPresentation`, keine `SpriteActorPresentation` daneben,
`SpriteActorAnimator` bleibt Pflicht).

**Wildling-Bodenkontakt — behoben bzw. entschieden (13.08. abends):**

- **`Gehen` ist repariert: −31 mm → −3,5 mm.** Die Ursache lag NICHT im
  Wurzel-Wippen des Clips (ein erster Fix dort war messbar wirkungslos,
  weil `plantFeet` die Wurzelhöhe ohnehin normalisiert), sondern in der
  `SOLE`-Probeliste: sie kannte nur Ballen und Ferse — kippte der Fuß in
  der Schwungphase, tauchte die **Zehenkrallenspitze** (z 0,195) unter den
  Boden, ohne dass eine Probe es sah. Je Fuß eine Krallenspitzen-Probe
  nachgerüstet (die Serienfiguren tragen sie von Anfang an); kein anderer
  Clip hat sich dadurch verschlechtert.
- **`Tod` bleibt bewusst bei −244 mm.** Der Kollaps schaltet die
  Fußsetzung ab (`plantAmount = 1 − fall`), die gefalteten Beine sinken
  mit der abgesenkten Wurzel ein. Aus der steilen Spielkamera verdeckt der
  liegende Körper die versunkenen Beine vollständig; ein Fix hieße, die
  abgenommene Kollaps-Pose neu abzustimmen — Aufwand ohne sichtbaren
  Gewinn. Entschieden als Ausnahme, nicht als Restpunkt.

## In-Welt-Abnahme (13.08.2026, abends)

Die Studio-Captures zeigen die Figur — erst das Spielbild zeigt das Spiel.
`Tests/PlayMode/KreaturenInWeltCaptureTests.cs` lädt echte Zonen mit voller
Komposition und rendert die **echte Spielkamera** (RenderTexture-Weg; läuft
nur auf Anforderung: `[Explicit]` plus `EIDREN_CAPTURE_ORDNER`-Pflicht,
Aufruf per `-testFilter KreaturenInWeltCaptureTests` OHNE `-nographics`).

Neun Bilder aus vier Zonen, Befund:

- **Es fügt sich.** Vertexfarben treffen die Zonenpaletten, Bodenschatten
  liegen unter den Figuren, die Größen stimmen gegen den Wanderer.
- **Verhalten läuft in 3D:** der Wurzelstürmer telegrafiert im Bild einen
  Angriff (rotes Quad) — Kampflogik und Darstellung greifen ineinander.
- **Eidra-Tönung sichtbar:** der Marsh-Noctarion trägt den bläulichen
  UiAccent-Schimmer über `SetTint`.
- **Die Spielkamera ist steiler als jede Studio-Ansicht** — gesehen wird
  vor allem die Oberseite. Der Rückseiten-/Deckflächen-Befund war damit
  spielentscheidend; künftige Figuren zuerst von OBEN beurteilen.
- **Terrock ist im Steinbruch kaum lesbar** (steinfarben auf Steinboden,
  klein, im Schatten) — funktional grün, aber als Sichtbarkeits-Frage
  notiert: evtl. braucht er im Spielkontext einen Akzent.

**Die Prefabs sind nicht von git verfolgt** (`.gitignore` ist `*`). Vor einem
Umbau eine Kopie ablegen; `git checkout` hilft hier nicht. Sicherungen aller
elf Gegner-Prefabs, der 14 Quelldateien und der 14 GLB vor dem heutigen
Eingriff liegen im Sitzungs-Scratchpad.

## Die 2D-Altlast — aufgeräumt (13.08.2026, nachts)

Nach dem Vollausbau wurde die tote 2D-Schicht beweisgetrieben entfernt.
Grundlage: eine Verbrauchskarte aus DREI Kanälen — Code-Ladepfade
(`Resources.Load`-Strings), serialisierte String-Pfade in Text-Assets, und
`AltlastInventar` (Editor) für GUID-Referenzen inklusive der **binär**
serialisierten Szenen plus deren String-Felder. Textsuche allein hätte hier
wieder gelogen.

**Gelöscht: 398 Dateien, 545 MB** (Sicherung samt Metas im
Sitzungs-Scratchpad, `AltlastLoescher` mit Liste aus
`altlast-loeschliste.json`): alle Zustands-Atlanten von Garon (27), Spieler
(80), Wildling (21) und den elf V02-Kreaturen (264) sowie die sechs toten
Forge-2D-Prefabs.

**Geblieben ist die lebendige Schutzschicht (66 Dateien):** die
Bodenschatten- und Partikeltexturen aller Figuren (per String-Pfad in 17
Gegner-Prefabs, dem Boss und den Szenen referenziert!) und die komplette
Noctarion-/Terrock-2D-Kette (Atlanten + `Noctarion_2D`/`Terrock_2D`) — sie
dient weiter `StyleProofEidraCompanion` und den grünen Presentation-Tests.

**Tests dazu ausgemustert:** `V02ActorVisualTests`,
`WildlingSpriteVisualTests`, `GaronSpriteVisualTests` (alle Fälle prüften
Gelöschtes bzw. nie Gebautes) und die Prefab-Methode in
`WildlingDefinitionTests`; die Ein-Pixel-Dimensionsfälle
(Noctarion 1279/1280, Terrock 1023/1024) sind auf die Ist-Malerei gesetzt.
**EditMode-Baseline: 69 → 61 bekannte Fehlschläge, null neue** —
Referenz `TestResults-altlast-editmode2.xml`.

`V02ActorVisualBuilder` und `WildlingContentBuilder` bleiben als
kompilierende Werkzeuge erhalten (Code-Verweise aus anderen Buildern), sind
aber endgültig funktionslos: ihre Quell-Atlanten existieren nicht mehr.
`Player_2D.prefab` und `Garon_2D.prefab` in der `VisualScaleAssetMap`
zeigen weiterhin ins Leere — unverändert folgenlos.

**Die Notiz zum SmithingMark ist überholt.** Kein Quelltext verweist mehr auf
`ITEM_TMP_SmithingMark.png`; `IgnivarContentBuilder` lädt
`ITEM_SmithingMark.png`, und die Datei ist vorhanden.

## Restbaseline-Triage: 61 → 0 (14.08.2026)

Die komplette Testlandschaft ist erstmals grün: **EditMode 1009/1009**
(`TestResults-nachzug4-editmode.xml`), **PlayMode 120/120**
(`TestResults-nachzug2-playmode.xml`). Ältere Baselines liegen in
`TestResults-Archiv/` (163 Dateien).

**Produktfixe (nicht Testkosmetik):**

- Elf WeaponData-Assets aus dem Dekompilat-Ordner `Assets/MonoBehaviour/`
  GUID-erhaltend nach `Data/Weapons/` umgezogen (heilte SaveGame ×21 u. a.)
- `ItemContentTable.Material()` reichte den `icon`-Parameter nicht durch
  (fünf EidraForge-Fälle)
- `Wanderer3DPlayerBuilder`: Werkzeug-Renderer (statische MeshRenderer)
  bekamen das Figurenmaterial nie — `GetComponentsInChildren<Renderer>`
  statt `SkinnedMeshRenderer`; die Axt trug den glTF-Importshader
- `CombatHUD.prefab` hing mit dem Interaktions-Fallback-Icon noch am
  dekompilierten Ripper-Sprite (`Assets/Resources/art/ui/…asset`) statt an
  der gepflegten PNG
- Die EidraForge-Szene fehlte in den Build Settings (hätte den Tester-Build
  verkrüppelt); `WorldMapContentBuilder`/`EidraForgeLootAssetBuilder`/
  `IgnivarContentBuilder`/`UiVisualAssetBuilder` (PhaseE) waren nie gelaufen
- `BLD_FarmPlot_L01` hatte keine Zustands-Visuals mehr (leer/bepflanzt/
  erntereif alle null) — `FarmPlotVisualRebuilder` baut sie im
  CopperMining-Muster (Unlit/Color, Collider-frei unter `Visual_A20`)
- Terrock-/Noctarion-Begleitkarten waren vollflächig opak —
  `KreaturenMapsAlphaStanzer` überträgt das Albedo-Alpha in Normal/Emission
  (40 Karten)
- Platzhalter-Item-Icons: zwölf Familien-Gruppen waren byte-identisch —
  `ItemIconFamilienToenung` tönt die Partner semantisch (Eisen stahlblau,
  Hartholz dunkler, Stein heller, …). WICHTIG: Ableitungs-Icons müssen in
  `ItemContentTable` auf ihre EIGENE Datei als Quelle zeigen, sonst zieht
  `EnsureMirroredSprite` die Tönung in den Partner nach (Granit/Behauener
  Granit/Kupferspeer-Bauplan waren so gefangen)
- 14 WorldMap-Marker-Metas trugen PPU 20,41 statt 100

**Testfixe mit Datenbeleg:** Voll-Erstattung beim Abriss (StorageChest
wood:10/stone:4; Wand-Ersatz +2−4), Gebäudepfade `Level01/BLD_*_L01`,
Wanderer-Clips 12→24, Maps-Formatfamilien nach Messung (8 Frames: alte
Vollbreite, 10: −1 px, 12: schmal mit 4096-Kappung; Geometrie-Scanner nur
noch im exakten Alt-Layout), Catalog-Fall „gesperrte Zeile" ausgemustert
(Menü entfernt Gesperrtes seit `RemoveAll`).

## Testhygiene: die drei Flackerer (14.08.2026)

Alle drei ordnungsabhängigen PlayMode-Flackerer sind mechanisch entschärft
(Volllauf 120/120 grün):

- `CombatFeedbackPooling.FloatingText`: der Pool ist ein LIFO-Stack; lief
  ein fremder Alt-Text während der Wartezeit ab, lag er beim zweiten Spawn
  obenauf. Fix: aktive Alt-Texte vor Testbeginn ablaufen lassen
- `ZoneStructure.LoadZone`: entgiftet die DontDestroyOnLoad-Dienste NUR im
  vergifteten Fall (`StartNewGame` bei totem Spieler; `enabled`-Zyklus des
  `SceneFlowService` nutzt dessen `OnDisable`-Aufräumpfad gegen hängende
  Eingabesperren)
- `InteractionIntegrationTests`: räumt per `[OneTimeTearDown]` den
  ServiceRoot ab — die Klasse tötet den Spieler absichtlich und vergiftete
  damit die Nachfolger

## Kür: Sichtpolitur der Serie (14.08.2026)

Quelle → `export_gltf.js` → GLB → Unity-Reimport; alle drei
`verify_gltf`-Prüfungen bestanden (Skinning-Abweichung 0,000 mm),
Studio-Captures im Sitzungs-Scratchpad abgenommen:

- **Garon**: Heck-Goldornament — schmales Abschlussband (z −0,435) plus
  zweilagige Goldraute auf der Steinkappe. Negatives s dreht den
  `platte()`-Wölbungs-Lift nach außen (−z); Bauweise wie `rune()`, nur im
  Goldsatz
- **EmberEater**: durchgehende Glut-Rückenader — Kruppen-Glied (hips) und
  Widerrist-Glied (chest) verlängern den isolierten Spine-Längsriss zum
  Faden über den First
- **Terrock**: Moosflächen auf der Kuppel-Oberseite (Draufsicht liest
  Grünanteile — Befund „steinfarben auf Steinboden") und Gold-Smaragd-Raute
  auf der Kruppenkappe. Achtung: `smaragd()` versetzt die Kernlage entlang
  (nx,nz) — für Heckflächen direkt mit zwei `platte()`-Lagen bauen

**In-Welt-Abnahme der Kür (14.08.2026):** `KreaturenInWeltCaptureTests`
mit `EIDREN_CAPTURE_ORDNER` gefahren (8/8 bestanden). `quarry_terrock.png`
belegt den Sichtbarkeits-Gewinn im Steinbruch: der Terrock hebt sich mit
hellem Panzer und Moosgrün vom Steinboden ab, und die Heckansicht ist
allein am grünen Kruppen-Smaragd identifizierbar — der Befund „steinfarben
auf Steinboden" ist damit geschlossen. Die sechs Kernbilder (Garon-Heck,
EmberEater-Ader Seite/Heck, Terrock Studio Heck/Seite, Terrock in-Welt)
liegen dauerhaft unter `Documentation/Etappen/Kreaturenserie/Belege_Kuer_20260814/`.
Der EmberEater-Hautton bleibt unverändert: `#0e0b0a` ist laut
`MESSWERTE.md` der dominante gemessene Vorlagenton (41,8 %) — die im
Quellkommentar notierte „offene Feinabstimmung" braucht ein Geschmacks-
urteil im Spielkontext, keine weitere Messung.

## Aufräumaktion Projektordner (14.08.2026, nach dem Release)

Freigegeben und ausgeführt in drei Kategorien: (A) Reproduzierbares
gelöscht — alter visual-fix-Build, beide Staging-Baureste, TempReview
(vollständig in der 05.08-Sicherung), die Dekompilier-Werkzeuge unter
`.tools/` sowie 515 lose Etappen-Laufprotokolle aus dem Projektstamm
(als Zip in der Meilenstein-Sicherung). (B) Archivgut verschoben nach
`Eidren-Sicherungen/archiv-*`: G001-Captures und die Release-Zips v0.1.0
und v0.2.0-dev vom 06.08. (C) Ripper-Sammelordner per Verbrauchskarte
(`RipperRestInventar`: GetDependencies projektweit inkl. Binärszenen PLUS
ProjectSettings-GUID-Scan) auf 301 Kandidaten geprüft: **208 beweisbar
unreferenzierte gelöscht** (`RipperRestRaeumung` + AltlastLoescher,
Komplettsicherung vorab), 38 leere Ordnerskelette entfernt —
ComputeShader/Font/GameObject/NavMeshData/Texture2D sind ganz weg, die
93 nachweislich genutzten Dateien (URP-Renderer, Input-Actions,
UI-Sprites …) bleiben. Beleg: `TestResults-Archiv/ripper-rest-inventar.txt`.

Regressionsbeweis: EditMode alle 1009 Bestandsfälle grün, PlayMode
120/120 grün (`TestResults-ripper2-*.xml`). Stolperstein für kommende
Läufe: Die Inventar-Ausgabe hat CRLF-Zeilenenden — wer Pfade mit
split("\n") zieht, muss das \r strippen, sonst überspringt der Löscher
alles als „nicht vorhanden". Hinweis: Parallel zur Räumung entstand
fremdes Werk `EidrenMeshFactoryTests` (17:52, vermutlich codex-Agent)
mit einem roten Wickelungs-Fall — nicht Teil dieser Aktion.
