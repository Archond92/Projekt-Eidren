# G-004 — Bodenschatten und Objektverankerung: Entwurf

**Stand:** 10. August 2026 · **Status:** Entwurf, dem Auftraggeber zur Prüfung
vorgelegt

**Bezug:** [GRAFIKAUFTRAEGE_V0.2.1.md](GRAFIKAUFTRAEGE_V0.2.1.md), Auftrag G-004.
Voraussetzung G-003 ist abgeschlossen ([G003_ABNAHME.md](G003_ABNAHME.md)).

**Regressionsmaßstab:** Namensdiff gegen `TestResults-stil-e5-final.xml`
(831 Tests, 760 bestanden, 71 bekannte Fehlschläge), nicht `failed="0"`.

---

## 1. Ausgangslage — gemessen, nicht vermutet

Der Auftragstext von G-004 wurde formuliert, als die Welt aus gemalten Sprites
vor flacher Geometrie bestand. Seither haben der Stilumbau (Etappen 1–5,
abgeschlossen am 10.08.2026) und der Wanderer3D-Einbau die Voraussetzungen
verändert. Der Befund vor Beginn:

**Echter Schattenwurf läuft bereits.**

- `ZoneLightingBuilder:54–55` — Sonne auf `LightShadows.Hard`, Stärke `0,55`.
- `ZoneLightingBuilder:43` — `SunEuler = (71.7, 156.2, 0)`.
- `EidrenWorldVertexLit.shader` und `WandererVertexLit.shader` besitzen beide
  einen ShadowCaster-Pass.
- Alle 81 Renderer der StyleProof-Props und alle 135 Renderer der
  Ressourcen-Visuals stehen auf `m_CastShadows: 1`.
- `Wanderer3DPlayerBuilder:150–151` — die Spielfigur wirft und empfängt
  Schatten.

**Weiche Schatten sind abgeschaltet.** `Eidren_URP.asset:66` —
`m_SoftShadowsSupported: 0`. Die vorhandenen Schatten sind hartkantig.
Schattenweite 50, eine Kaskade, Schattenkarte 2048, Tiefen- und Normalenbias
je 1.

**Die Spielfigur hat bewusst keinen Blob-Schatten mehr.** Der Wanderer3D-Einbau
hat ihn entfernt und per PlayMode-Test festgeschrieben
(`GroundShadowPlayModeTests.PlayerGroundShadow_SpielerHatKeinenBlobSchattenMehr`,
bestanden).

**Die Kontaktdecals aus G-003 sind verloren.** `WorldContactShadowBuilder` hatte
sie in die Prefabs gebaut. Der Stilumbau hat dieselben Prefabs regeneriert und
die `ContactShadow`-Kinder dabei überschrieben:

| Ordner | Prefabs mit `ContactShadow` |
| --- | ---: |
| `Prefabs/Resources/Visuals` | 0 von 18 |
| `Prefabs/Environment/AreaArtVariants` | 0 von 24 |
| `Prefabs/Environment/StyleProof` | 0 von 16 |
| `Prefabs/Buildings/Level01` | 0 von 9 |

Erhalten sind sie ausschließlich eingebacken in den acht Zonenszenen
(12 Instanzen in `Zone_Greenwood`). Material und Textur liegen unberührt unter
`Assets/_Game/Art/World/`.

**Der Boden ist eben.** `EidrenSceneStructureBuilder:242–246` erzeugt
`WalkableGround` als `PrimitiveType.Cube` bei `y = −0,5` mit Skalierung
`(size, 1, size)`. Die Oberkante liegt über die gesamte Zone exakt auf `y = 0`.
Weder der Szenenbau noch `AreaArtBlend.shader` erzeugen Höhenvariation.
`BuildingPlacementController` rechnet folgerichtig mit
`WalkableGround.bounds.max.y` als **einer** Bodenhöhe.

---

## 2. Zielbild — drei Mechanismen mit klarer Zuständigkeit

**Echter Schattenwurf führt.** Er liefert Formmodellierung, Bewegung, Sprung,
Ausweichrolle und Bodenkontakt ohne eigenen Code. Änderung: weiche Schatten
anschalten.

**Das Kontaktdecal erdet.** Bei 71,7° Sonnenhöhe wirft ein 5 m hoher Baum nur
rund 1,65 m Schatten; der liegt eng am Fuß und verschwindet teilweise unter dem
Objekt selbst. Genau dort trägt eine weiche Abdunklung unter dem Objekt die
Standposition. Sie ergänzt den Wurf, sie ersetzt ihn nicht.

**Blob-Schatten bleiben Übergang.** Die Sprite-Gegner und Eidra behalten
`DynamicActorGroundShadow` unverändert, bis die Figuren-Pipeline sie auf 3D
umstellt. Ein Billboard-Quad wirft keinen brauchbaren echten Schatten.

---

## 3. Änderungen im Einzelnen

### 3.1 Weiche Schatten anschalten

- `Eidren_URP.asset`: `m_SoftShadowsSupported: 0 → 1`.
- `ZoneLightingBuilder.SunShadows`: `LightShadows.Hard → LightShadows.Soft`.

**Unangetastet bleiben** `SunEuler = (71.7, 156.2, 0)` und
`SunShadowStrength = 0,55`. Der Winkel ist G-003-Invariante: Bildwinkel 113,5°
trifft die gemalte Lichtrichtung auf 0,0° genau, und der Tiefenanteil −0,678 ist
zugleich das `N·L` der Figurenbeleuchtung. Eine Änderung dort bräche G-003.

### 3.2 Das Kontaktdecal entsteht im Bauweg

`WorldContactShadowBuilder` wird vom Nachlauf-Werkzeug zum gemeinsamen Helfer.
Jeder erzeugende Builder ruft ihn als letzten Schritt auf — eine Zeile je
Builder:

| Gruppe | Ordner | Prefabs | Builder |
| --- | --- | ---: | --- |
| Ressourcen-Visuals | `Prefabs/Resources/Visuals` | 18 | `T1ResourceVisualBuilder`, `T2ResourceVisualBuilder` |
| Gebietsvarianten | `Prefabs/Environment/AreaArtVariants` | 24 | `AreaArtVariantPrefabBuilder` |
| StyleProof-Props | `Prefabs/Environment/StyleProof` | 16 | `StyleProofPropBuilder` |
| Gebäude | `Prefabs/Buildings/Level01` | 9 | `EidrenBuildingVisualBuilder` |
| Weltkisten | `Prefabs/Loot/WorldChests` | 3 | `WorldChestContentBuilder` |
| Forge-Behälter | `Prefabs/Containers/Forge` | 8 | `ForgeContainerVisualBuilder` |
| Stationen | `Prefabs/Stations` | 2 | `StorageContentBuilder` |

Dazu unverändert die eingebackenen Dekorations-Instanzen über
`ZoneLightingBuilder.Apply` → `WorldContactShadowBuilder.AddToDecorations`.

**Das ist die eigentliche Lehre aus G-003.** Dort scheiterte die Erdung nicht an
ihrer Gestalt, sondern an ihrer Befestigung: Sie hing als nachträgliche
Dekoration an Prefabs, die der nächste Builder-Lauf neu geschrieben hat. Als
Bauteil im Bauweg kann das nicht mehr passieren — und die Projektregel „kein
Prefab wird manuell editiert, jeder Stand ist per Builder reproduzierbar" gilt
dann auch für die Erdung.

**Gestalt und Tonwerte bleiben aus G-003 unverändert:** weiche Ellipse,
Spitzendeckung `111/255`, Farbton `(0.11, 0.15, 0.18)` bei Alpha `0,38`,
Breitenfaktor `1,1`, Tiefenfaktor `0,55`, Bodenabstand `0,018`. Diese Werte sind
gemessen und abgenommen; sie werden nicht neu erfunden.

### 3.3 Aufnahmeregel statt Pflegeliste

Ein Kontaktdecal bekommt jedes Prefab, dessen zusammengefasste Rendererbounds
in der Höhe (`bounds.size.y`) **0,3 m übersteigen**. Maßgeblich ist dieselbe
Bounds-Berechnung, die `WorldContactShadowBuilder` schon heute für die
Decalgröße verwendet — eine Rechnung, nicht zwei.

Der Schwellenwert 0,3 m ist ein Vorschlag und beim Schreiben des Plans gegen die
gemessenen Ist-Höhen aller 80 Prefabs zu prüfen (siehe Abschnitt 9.2). Er muss
so liegen, dass Bodenplatten und flacher Bewuchs sicher darunter und die
niedrigsten stehenden Objekte sicher darüber fallen.

Damit fallen Böden, Wege und flacher Bodenbewuchs automatisch heraus, ohne dass
jemand eine Ausnahmeliste pflegen muss — und die Regel ist als Test formulierbar.
Der Bodenbewuchs `{Zone}_Cover_*` bleibt zusätzlich über seinen Namen
ausgeschlossen, wie schon in G-003 begründet.

### 3.4 Was ausdrücklich nicht geändert wird

- `DynamicActorGroundShadow` und die sechs Akteur-Schattentexturen.
- Der Sonnenwinkel und die Schattenstärke.
- Collider, Interaktionsreichweiten, Abbau-Erträge, Bauraster-Footprints.
- Die Prefabpfade und -namen sämtlicher Gruppen.

---

## 4. Abweichungen vom Auftragstext

Beide Abweichungen betreffen das Mittel, nicht das Ziel.

**1. Echter Schattenwurf statt reiner Schattenflecken.** Der Auftrag verlangt
ausdrücklich, „bewusst auf einen weichen Schattenfleck pro Objekt statt auf
vollen Schattenwurf" zu setzen, mit der Begründung, das trage bei gemalten
2D-Figuren besser und sei günstiger. Die Begründung ist entfallen: Spielfigur
und sämtliche Weltobjekte sind seit dem Stilumbau und dem Wanderer3D-Einbau
echte 3D-Geometrie, und der Schattenwurf ist bereits aktiv und bezahlt. Ihn
abzuschalten, um ihn durch Flecken zu ersetzen, wäre ein Rückschritt. Die
Sprite-Gegner, für die das Argument weiter gilt, behalten ihre Flecken.

**2. Keine Hangausrichtung.** Der Auftrag verlangt in Sollverhalten und
Kriterium 5, der Schatten möge der Bodenneigung folgen. Geneigten Untergrund
gibt es in Eidren nicht: `WalkableGround` ist ein flacher Würfel
(`EidrenSceneStructureBuilder:242–246`), seine Oberkante liegt über die ganze
Zone auf `y = 0`. Kriterium 5 wird deshalb als **gegenstandslos** ausgewiesen,
nicht als erfüllt. Eine Raycast- und Normalenmechanik dafür zu bauen, hieße
Code für einen Fall zu pflegen, den das Spiel nicht kennt.

---

## 5. Abgleich mit den acht Abnahmekriterien

| # | Kriterium | Wie erreicht |
| --- | --- | --- |
| 1 | Figur, Gegner, Eidra und alle stehenden Weltobjekte besitzen einen Bodenschatten | Wurf für Figur und 80 Weltprefabs, Decal zusätzlich, Blob für Sprite-Gegner |
| 2 | Schatten sitzt an der Standposition, nicht versetzt | Decal am Fußpunkt der Rendererbounds; Wurf per Definition |
| 3 | Folgt Bewegung ohne sichtbare Verzögerung | echter Wurf, keine nachlaufende Logik |
| 4 | Bei Rolle und Sprung verkleinert sich der Schatten, Figur löst sich sichtbar | echter Wurf leistet das ohne Zutun — Nachweis per Capture |
| 5 | Auf geneigtem Untergrund liegt der Schatten auf der Oberfläche | **gegenstandslos**, siehe Abschnitt 4.2, mit Codebeleg |
| 6 | Schattenrichtung stimmt mit der Lichtrichtung aus G-003 überein | dieselbe Sonne; Decalversatz aus `SunEuler` gerechnet |
| 7 | Auf dem Boden aus G-002 sichtbar, ohne als harter Fleck zu wirken | weiche Schatten an; Decal-Falloff aus G-003 unverändert |
| 8 | Bildrate sinkt bei voller Gegnerzahl nicht messbar ab | Messung an denselben Positionen, siehe Abschnitt 7 |

---

## 6. Tests und Absicherung

**EditMode, neu — `GroundContactTests`:**

- Jedes Prefab der sieben Gruppen mit Rendererhöhe über 0,3 m besitzt genau ein
  `ContactShadow`-Kind mit dem Material `M_World_ContactShadow`.
- Prefabs unterhalb der Höhenregel besitzen keins.
- Die Decalgröße liegt im Korridor `Breite × 1,1` und `Tiefe × 0,55` der
  Rendererbounds, gedeckelt wie in G-003.
- Genau ein Decal je Prefab — ein zweiter Builder-Lauf erzeugt kein zweites.

Dieser Test ist der eigentliche Regressionsschutz: Er wird rot, sobald ein
künftiger Builder-Lauf die Erdung wieder wegwirft. Damit ist die Kopplung ein
Vertrag statt einer Konvention.

**Bestehende Tests**, die mitlaufen müssen: `V02ContainerVisualTests`,
`VisualAssetTests`, `VisualScaleTests`, `EidrenWorldStyleTests`,
`StyleProofContentTests`, `GroundShadowPlayModeTests`.

**Regression:** Namensdiff gegen `TestResults-stil-e5-final.xml`. Erwartet sind
0 neue Fehlschläge; die 71 bekannten bleiben unverändert.

---

## 7. Abnahme

- **Vorher/Nachher-Captures** über die G-001-Strecke aus positionsgleichen
  Kamerapositionen, mindestens die Bodenposition und zwei Objektpositionen.
- **Bewegungsnachweis** für Kriterium 4: je ein Capture stehend, in Bewegung und
  während der Ausweichrolle.
- **Stichprobe je Gruppe**: ein Bild pro Objektklasse mit sichtbarer Erdung.
- **Bildrate** an denselben Positionen bei voller Gegnerzahl, vorher gegen
  nachher, mit Zahlen.
- Kein Nachweis wird behauptet, der nicht als Bild oder Messwert vorliegt.

---

## 8. Risiken

| Risiko | Umgang |
| --- | --- |
| Weiche Schatten kosten Bildrate | Kriterium 8 misst es; fällt die Rate, bleibt Hard und das Decal trägt allein |
| Schattenakne bei Bias 1/1 mit weichen Schatten | Sichtprüfung an Kanten; Bias nur bei Befund und dann dokumentiert nachziehen |
| Decal unter sehr breiten Objekten wirkt als Platte | Deckel aus G-003 (`4,5 × 3,5`) bleibt in Kraft |
| Paralleler codex-Agent schreibt gleichzeitig | Lockfile-Wache vor jedem Unity-Lauf, niemals löschen |
| Doppel-Decals bei wiederholtem Builder-Lauf | Idempotenz per Namensprüfung, durch Test abgesichert |

---

## 9. Offene Punkte

1. **Freigabe des Entwurfs durch den Auftraggeber** steht aus.
2. Ob Weltkisten, Forge-Behälter und Stationen die Höhenregel durchgängig
   erfüllen, ist am Bestand zu messen, sobald der Plan geschrieben wird — die
   Regel steht, die Zuordnung je Prefab noch nicht.
3. Die Wechselwirkung mit den noch offenen Abschlussarbeiten A1–A4 aus
   Etappe 5 (insbesondere A2, Alt-Material-Rückbau) ist vor dem ersten
   Builder-Lauf zu klären, damit sich beide Arbeiten nicht überschreiben.
