# Wanderer 3D — Einbau ins Spiel (Design)

Datum: 06.08.2026 · Branch: codex/v0.2-release · Status: vom Auftraggeber freigegeben (Chat)

Ergänzt `WILDLING3D_ENTWURF.md`: dort entsteht die erste Gegnerfigur als
GLB (mit echten Treffer-/Taumeln-/Tod-Clips). Die hier geschaffene
Schnittstellen-Struktur ist der Andockpunkt für deren späteren Einbau —
`MeshActorPresentation` nutzt echte Clips, wo vorhanden, und fällt sonst
auf die prozeduralen Darstellungen zurück.

## Ziel

Die neue Low-Poly-3D-Spielfigur (`Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb`)
ersetzt die Sprite-Darstellung des Spielers vollständig. Rüstungs- und
Waffenoptik kommen aus derselben GLB (16 Meshes an einem Skelett, 12 Clips).
Waffen sind materialneutral: eine Optik je Familie (Speer/Dolche/Hammer),
Kupfer-/Eisen-Varianten unterscheiden sich nur in Werten (`WeaponData`).
Gegner behalten vorerst ihre Sprite-Optik, werden aber strukturell so
vorbereitet, dass ein späterer 3D-Tausch nur noch ein anderes Prefab-Kind ist.

## Nicht-Ziele

- Keine 3D-Modelle für Gegner, Bosse oder Eidra (Folgeaufträge).
- Keine neuen Animationsclips: Hit/Stagger/Death/Appear werden prozedural
  dargestellt, echte Clips können später nachgerüstet werden.
- Keine Änderung an Kampfwerten, Item-Daten oder Balancing.

## Voraussetzungen

1. **glTFast**: `com.unity.cloud.gltfast` in `Packages/manifest.json`
   ergänzen. Braucht beim ersten Resolve Internet. Danach importiert Unity
   die GLB wie jedes Modell (Meshes, Skelett, Clips).
2. **Shader**: Neuer URP-Shader `Eidren/VertexColorLit` — gibt Vertexfarben
   (sRGB, AO und Augenleuchten bereits eingebacken) multipliziert mit der
   URP-Beleuchtung aus. Projekt rendert Gamma+LDR (G-003): kein Tonemapping,
   keine HDR-Annahmen. Flat Shading kommt aus den Normalen der GLB.

## Architektur

### Schnittstellen-Refactoring (Vorbereitung für Gegner)

`SpriteActorAnimator`, `PlayerVisualAnimator`, `PlayerWeaponVisual`,
`PlayerPrefabBindings` und `ActorOcclusionTransparency` greifen heute konkret
auf `SpriteActorPresentation` zu. Sie werden auf Schnittstellen umgestellt:

- `IActorPresentation` (besteht): Facing, VisualState, Tint.
- **Neu** `ILocomotionPresentation`: `SetLocomotion(Vector3 worldDirection, bool running)`.
- **Neu** `IAuthoredStatePresentation`: `SetAuthoredState(string stem, float time, bool loop, bool restart)`.

`SpriteActorPresentation` implementiert alle drei (tut es faktisch schon,
nur ohne Interface-Deklaration). `ActorOcclusionTransparency` sammelt Akteure
künftig über `IActorPresentation` statt `FindObjectsByType<SpriteActorPresentation>`.
Gegner-Prefabs bleiben unverändert lauffähig.

### Neue Komponenten

**`MeshActorPresentation`** (`Assets/_Game/Scripts/Presentation/`)
implementiert `IActorPresentation` + `ILocomotionPresentation`:

- Animator mit Controller aus den 12 GLB-Clips.
- Zustands-Mapping: Idle → `Ruhe_<Familie>`, Move → `Gehen_<Familie>` /
  `Laufen_<Familie>` (Tempo-Schwelle aus `SetLocomotion`), Ability1/2 →
  `Angriff_<Familie>` mit Abspielgeschwindigkeit skaliert auf die vom
  Kampf-Controller gemeldete Dauer.
- Prozedurale Zustände: Hit = Tint-Blitz (MaterialPropertyBlock),
  Stagger = kurzes Wanken (Wurzel-Neigung), Death = Umkippen + Ausblenden,
  Appear = Einblenden.
- Facing: `ActorFacing8` → Ziel-Yaw, gedämpfte Drehung (flüssig statt
  8 Sprünge). Blickrichtung der GLB ist +Z, 1 Einheit = 1 m, Figur 1,80 m.
- Waffenfamilien-Umschaltung: Clip-Satz wechselt mit der Familie;
  unbewaffnet nutzt die `_Dolche`-Clips ohne Waffenmesh.

**`WandererEquipmentVisual`** (ersetzt `PlayerArmorVisual` am Spieler):

- Pro Rüstungsslot (Head/Chest/Hands/Legs) höchstens ein Mesh aktiv,
  `Basis` immer an. Ein leerer Slot zeigt kein Slotmesh (nur Basis);
  `_Stoff` erscheint erst mit angelegtem Wanderer-Set.
- Waffenslot: aktive `WeaponFamily` schaltet `Waffe_Speer/_Dolche/_Hammer`.
- Bezieht Slot/Stufe aus dem `WandererStyleCatalog` (unten), angebunden an
  dieselbe Equipment-Quelle, die heute `PlayerArmorVisual.Show` speist.

**`WandererStyleCatalog`** (ScriptableObject, `Assets/_Game/Scripts/Data/`):

- Einträge: ItemId → WearableSlot + Stufe (`Stoff`/`Kupfer`/`Eisen`)
  bzw. WaffenId → `WeaponFamily` (redundant zu `WeaponIdentityData`, dient
  als Validierungsquelle).
- Vorbelegung: `armor_wanderer_*` = Stoff, `armor_copper_*` = Kupfer,
  `armor_iron_*` = Eisen; Sealbreaker → Hammer, AshFangs → Dolche,
  EmberThorn → Speer.
- `ValidateDefinition()` im Stil von `ItemDefinition`: jedes Rüstungs- und
  Waffen-Item aus dem Item-Katalog muss gemappt sein, jeder Mesh-Name muss
  in der GLB existieren.

### Prefab und Beleuchtung

- `Player.prefab`: Sprite-Kind entfernen, Wanderer-Rig (GLB-Instanz)
  einhängen, glTFast-Materialien durch `Eidren/VertexColorLit` ersetzen,
  `PlayerArmorVisual` durch `WandererEquipmentVisual` ersetzen,
  `PlayerPrefabBindings` auf die neuen Komponenten verdrahten.
- Die 3D-Figur wirft echte URP-Schatten (`CastShadows` an);
  `DynamicActorGroundShadow` entfällt am Spieler.
- Licht-Invarianten aus G-003 bleiben unangetastet: Sonnentiefe −0,678 fix,
  Bildwinkel 113,5° ist Messwert.

## Datenfluss

1. Equipment-Änderung → `PlayerPrefabBindings.RefreshEquippedWeapons()` /
   Rüstungs-Refresh → `WandererEquipmentVisual` schaltet Meshes,
   `MeshActorPresentation` wechselt Clip-Satz.
2. Bewegung → `PlayerVisualAnimator` → `ILocomotionPresentation.SetLocomotion`
   → Gehen/Laufen-Clip + Dreh-Yaw.
3. Angriff → `PlayerCombatController.AttackStarted(duration, combo, family)`
   → `PlayerVisualAnimator` → `SetVisualState(Ability1/2, …)` → Angriffs-Clip
   zeitskaliert. Trefferzeitpunkte bleiben Gameplay-Sache (`MeleeWeaponHitbox`),
   die Optik ist rein kosmetisch.

## Fehlerfälle

- ItemId ohne Catalog-Eintrag → Warnung im Log, Slot bleibt leer (kein Wurf).
- Fehlender Clip/Mesh-Name → Validierungsfehler im EditMode-Test, zur
  Laufzeit Fallback auf `Ruhe_Dolche` bzw. kein Mesh.
- glTFast nicht installiert → Import schlägt sichtbar fehl; Compile-Log
  prüfen (bekannte Falle: Exit 0 lügt bei Compile-Fehlern).

## Tests

- **EditMode**: Catalog-Vollständigkeit gegen den Item-Katalog; Clip- und
  Mesh-Namen der GLB vorhanden; Zustands-/Familien-Mapping der
  `MeshActorPresentation`; Schnittstellen-Refactoring bricht Gegner nicht
  (bestehende Tests bleiben grün).
- **PlayMode**: Spieler spawnt mit 3D-Rig (kein `SpriteActorPresentation`
  am Spieler); Rüstungswechsel schaltet genau ein Slotmesh; Waffenwechsel
  schaltet Waffenmesh + Clip-Satz; Angriff spielt Angriffs-Clip.
- **Abnahme**: G-001-Referenz-Captures des Spielers werden ungültig und
  werden als letzter Schritt neu aufgenommen.

## Risiken

- glTFast-Installation braucht einmalig Netz.
- Paralleler codex-Agent: Unity-Lockfile prüfen, nie löschen; fremde
  Dateien können Phantom-Compilefehler erzeugen.
- glTFast erzeugt beim Import eigene Materialien — Ersetzung durch den
  Vertex-Color-Shader muss verifiziert werden (Sichtprüfung + Capture).
