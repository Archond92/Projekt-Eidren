# MIDPOLY-000 – Phase 0 Abschlussbericht

**Status:** abgeschlossen
**Baseline:** 2026-08-20 14:41 UTC · Unity 6000.3.0f1
**Produktive Prefabs verändert:** nein

## Ergebnis

- 150 sichtbare Asset-Roots inventarisiert (134 Prefabs, 16 importierte Modelle).
- 645 technische Mesh-Subassets mit Local ID, Vertices, Tris und Bounds gesichert.
- 12 produktive Szenen inventarisiert; 9 Szenen mit 3D-Renderern besitzen eine Übersicht, 3 reine Bootstrap/UI-Szenen sind als nicht anwendbar markiert.
- 48 visuelle Roots enthalten ein Skinned-Mesh-Rig.
- 150 visuelle Roots besitzen mindestens einen reproduzierbar ermittelten Builder-Kandidaten.
- 77 Editor-Skripte schreiben oder erzeugen nach statischem Indikator Assets.
- Summierte gerenderte LOD-/Instanzgeometrie der Asset-Roots: 117.926 Tris (keine Szenen-Performancezahl).

## Phase-0-Prüfpunkte

| Prüfpunkt | Ergebnis | Nachweis |
| --- | --- | --- |
| Vollständiges Assetregister | PASS | `ASSET_REGISTER.csv/json`, 150 visuelle Roots; `MESH_COMPONENTS.csv`, 645 Mesh-Subassets |
| Standard- und Gameplay-Baseline je sichtbarem Asset | PASS | `Captures/Standard_Low`, `Captures/Gameplay_Low`; Fehler: 0 |
| GUIDs, Bounds, Pivots, Collider, Rig, Clips und Controller gesichert | PASS | Register plus `BASELINE_CHECKSUMS.csv` |
| Produktive Szenen dokumentiert | PASS | `SCENE_BASELINE.csv`, `Captures/Scenes`; Fehler: 0 |
| Migrationsstatus initialisiert | PASS | `MIGRATION_STATUS.csv`; alle Zeilen `ungeprueft` |
| Builder-/Rebuild-Risiko eingefroren | PASS | `BUILDER_SOURCES.csv`, Builder-Kandidaten im Register und Quell-Hashes |

## Bestandsbefunde

- Der im Auftrag genannte Bestand von 611 erzeugten Komponenten-Meshassets ist bestätigt: 611 native `.asset`-Meshzeilen plus 34 GLB-Mesh-Subassets wurden erfasst.
- Unity meldet bei allen 16 importierten Figurenmodellen jeweils 80 gerenderte Dreiecke weniger als die Vergleichswerte in MIDPOLY-000. Das Register hält den tatsächlich importierten Unity-Stand fest; die konstante Differenz ist vor der ersten Retarget-Abnahme gegen die GLB-Primitive zu klären.
- Die bestehende Editor-Testassembly kompiliert wegen fünf Verweisen auf das nicht mehr vorhandene `ZoneStateService.DefaultMasterSeed` derzeit nicht. Für den reinen Auditlauf wurde nur die Testassembly temporär ausgeschlossen und danach unverändert wieder aktiviert; Produktivcode und produktive Assets blieben unangetastet.

## Auslegung der Captures

`Standard_Low` ist die unveränderte Vorher-Hälfte des späteren Low-vs-Mid-Vergleichs. `Gameplay_Low` zeigt dasselbe Asset isoliert mit einer festen isometrischen Spielkameraperspektive. Die Szenenübersichten sichern zusätzlich den räumlichen Projektkontext. Echte laufzeitabhängige Zustände und Animationsframes werden in den nachfolgenden Familien-QAs gegen diese Baseline ergänzt.

## Abgrenzung und Nachvollziehbarkeit

Technische Teilmeshes sind separat inventarisiert, aber nicht einzeln gerendert, weil sie keine eigenständigen Motive darstellen. Gerendert werden ihre sichtbaren Prefab-/Modell-Roots. Builder-Zuordnungen sind ausdrücklich Kandidaten: exakte Pfad-/Dateinennungen sowie eng begrenzte Familienheuristiken. Dadurch bleiben indirekt zusammengesetzte Bake-Ketten sichtbar, ohne eine nicht belegte Alleinzuständigkeit zu behaupten.

Phase 1 darf auf dieser Baseline aufsetzen. Produktive Assetwechsel bleiben bis zur Golden-Master-Freigabe gesperrt.
