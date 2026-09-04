# MIDPOLY-000 – Phase 3 GraniteShell Golden Master

**Stand:** 20.08.2026  
**Status:** visuell freigegeben, technisch geprüft und produktiv integriert  
**Rolle:** gepanzerte Kreaturenreferenz

## Gestaltung

Der Granitpanzer ist als langsamer, extrem widerstandsfähiger „Amboss“ neu interpretiert. Er teilt Rig und Gameplaymaße mit dem vorhandenen Vierbeiner, grenzt sich gestalterisch aber klar vom RootCharger ab:

- geschlossene, nach oben verjüngte Felskuppel statt Schulterbuckel und Wurzelmähne
- neun gegeneinander versetzte Reihen gebrochener Panzerplatten
- zentrale Keilsteinreihe und als Geometrie ausgeführte dunkle Bruchfugen
- tiefer, kurzer Amboss-Schädel unter dem Kuppelrand
- schwere Blockgelenke mit klar abgesetzten beweglichen Fugen
- kurze, tragende Läufe und breite Pranken für eine massive Standfläche
- sparsame Moospolster ausschließlich in geschützten oberen Fugen

Die Oberfläche arbeitet mit großen ebenen Flächen, ungleichen Bruchkanten und Flat Shading. Es wurden weder Subdivision-Smoothing noch rundliche Kieselformen als Endform verwendet.

## Produktionsstand

- LOD0: 16.868 Tris
- LOD1: 9.108 Tris
- LOD2: 3.681 Tris
- 2 PBR-Materialien
- 19 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach frischem GLB-Roundtrip: 2,4001 m bei 2,40 m Soll
- Skinweights je Vertex normiert
- Vertexfarben, Armature Modifier, Bone- und Clipvertrag auf allen drei LODs geprüft: PASS
- produktives Visual-Prefab GUID-erhaltend ersetzt: PASS
- Gegner-Prefab und bestehende Szenenreferenzen unverändert gültig: PASS
- LODGroup: 0,55 / 0,25 / 0,07
- bestehender CapsuleCollider 0,676 m / 2,73 m unverändert: PASS

## Dateien

- Blender-Quelle: `Assets/_Game/Art/MidPoly/GoldenMasters/GraniteShell/Source~/CRE_GraniteShell_Mid_Phase3_Production.blend`
- Runtime-GLBs: `Assets/_Game/Art/MidPoly/GoldenMasters/GraniteShell/Runtime/`
- Manifest: `Assets/_Game/Art/MidPoly/GoldenMasters/GraniteShell/GRANITESHELL_PRODUCTION_MANIFEST.json`
- Technischer Bericht: `GRANITESHELL_TECHNICAL_REPORT.json`
- In-World-Abnahme: `InWorld/greyrifts_granite_shell.png`
- Low-vs-Mid: `GRANITESHELL_LOW_VS_MID.png`
- 8-Punkt-Showcase: `GRANITESHELL_MID_8POINT.png`
- Spielkamera: `GRANITESHELL_MID_GAMECAM.png`
- Angriffspose: `GRANITESHELL_MID_ATTACK.png`
- Angriffsanimation: `GRANITESHELL_MID_ANGRIFF.mp4`

## Abnahme und Regression

Die visuelle Freigabe erfolgte am 20.08.2026. Das Modell ist GUID-erhaltend in `GraniteShell_3D.prefab` eingebunden; die vorherige Fassung liegt als Legacy-Fallback unter `Assets/_Game/Prefabs/Actors/3D/Fallback/GraniteShell_3D_Legacy.prefab`.

- Produktionsvertrag, Rig, Clips, LODs, Skinweights und Footprint: 3/3 fokussierte Tests PASS
- Mesh-Präsentation, Bodenkontakt, Gegnerframework, Status/Feedback, Tier Two und Population: 213/213 PASS
- relevante PlayMode-Spawn-/Kampf-/Szenentests: 11/11 PASS
- isolierter NavMesh-Hindernistest: 1/1 PASS; der Sammellauf zeigt dieselbe bekannte reihenfolgeabhängige Fluktuation wie beim RootCharger
- automatisierte In-World-Aufnahme in den Graurissen: 1/1 PASS

Damit ist der GraniteShell als gepanzerter Golden Master abgeschlossen und für die Migration verwandter Panzerkreaturen freigegeben.
