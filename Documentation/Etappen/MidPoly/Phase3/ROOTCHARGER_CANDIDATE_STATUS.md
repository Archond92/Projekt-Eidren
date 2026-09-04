# MIDPOLY-000 – Phase 3 RootCharger Golden Master

**Stand:** 20.08.2026  
**Status:** visuell freigegeben, technisch geprüft und produktiv integriert  
**Rolle:** vierbeinige/gedrungene Kreaturenreferenz

## Gestaltung

Der Wurzelstürmer übernimmt Größe, Grundsilhouette und Identität des vorhandenen Vierbeiners, wurde aber durch eine eigenständige kantige Mid-Poly-Oberfläche erweitert:

- asymmetrische, gebrochene Rindenplatten statt runder Auflagen
- schwere Wurzelmähne über Schulter und Rücken
- klare Stirn-, Brauen-, Wangen- und Kieferflächen
- funktional getrennte Kieferzähne und leuchtende Augen
- kantige Gelenkpanzer an allen vier Läufen
- vier gespreizte Klauen pro Pranke für lesbaren Bodenkontakt
- Moos- und Blattakzente auf der Oberseite für die steile Spielkamera

Die Form verwendet große ebene Flächen und zweistufig gebrochene Kanten. Es wurden keine glättende Subdivision und kein runder Pebble-Look als Endform verwendet.

## Produktionsstand

- LOD0: 25.172 Tris
- LOD1: 13.592 Tris
- LOD2: 5.528 Tris
- 2 PBR-Materialien
- 19 Bones, unveränderte Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach GLB-Roundtrip: 2,1997 m bei 2,20 m Soll
- Skinweights je Vertex normiert
- LOD0/LOD1/LOD2 nach frischem Blender-GLB-Import vollständig geprüft: PASS
- produktives Visual-Prefab GUID-erhaltend ersetzt: PASS
- Gegner-Prefab und bestehende Szenenreferenzen unverändert gültig: PASS
- LODGroup: 0,55 / 0,25 / 0,07
- Collider und Gameplay-Hülle gegen das neue Visual abgeglichen: PASS

## Dateien

- Blender-Quelle: `Assets/_Game/Art/MidPoly/GoldenMasters/RootCharger/Source~/CRE_RootCharger_Mid_Phase3_Production.blend`
- Runtime-GLBs: `Assets/_Game/Art/MidPoly/GoldenMasters/RootCharger/Runtime/`
- Manifest: `Assets/_Game/Art/MidPoly/GoldenMasters/RootCharger/ROOTCHARGER_PRODUCTION_MANIFEST.json`
- Technischer Bericht: `ROOTCHARGER_TECHNICAL_REPORT.json`
- In-World-Abnahme: `InWorld/greyrifts_root_charger.png`
- 8-Punkt-Showcase: `ROOTCHARGER_MID_8POINT.png`
- Spielkamera: `ROOTCHARGER_MID_GAMECAM.png`
- Angriffsanimation: `ROOTCHARGER_MID_ANGRIFF.mp4`

## Abnahme und Regression

Die visuelle Freigabe erfolgte am 20.08.2026. Das Modell ist GUID-erhaltend in `RootCharger_3D.prefab` eingebunden; die vorherige Fassung liegt als Legacy-Fallback unter `Assets/_Game/Prefabs/Actors/3D/Fallback/RootCharger_3D_Legacy.prefab`.

- Produktions-, Rig-, Clip-, LOD-, Material-, Collider- und Ground-Contact-Tests: 191/191 PASS
- Gegnerframework-, Tier-Two-, Status-, Feedback- und Populationstests: 24/24 PASS
- relevante PlayMode-Spawn-/Kampf-/Szenentests: 11/11 PASS
- isolierter NavMesh-Hindernistest: 1/1 PASS; ein vorheriger Sammellauf zeigte hier ausschließlich eine bekannte reihenfolgeabhängige Testfluktuation ohne RootCharger-Bezug
- automatisierte In-World-Aufnahme in den Graurissen: 1/1 PASS

Damit ist der RootCharger als vierbeiniger/gedrungener Golden Master abgeschlossen und für die Migration verwandter Kreaturen freigegeben.
