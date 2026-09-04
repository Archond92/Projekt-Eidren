# MIDPOLY-000 – Phase 3 MoorThrower Golden Master

**Stand:** 20.08.2026  
**Status:** visuell freigegeben, technisch geprüft und produktiv integriert  
**Rolle:** Fernkampf-/Werferreferenz

## Gestaltung

Der Moorwerfer bleibt ein stark gebückter, plantigrader Moorkörper, erhält aber eine aus der Spielkamera eindeutig lesbare Fernkampf-Silhouette:

- asymmetrischer, sehr langer rechter Wurfarm
- großer, aus dreizehn gebrochenen Teilkörpern aufgebauter Wurfbrocken
- stabiler `ProjectileSocket` an der rechten Hand
- heller kantiger Totenschädel mit tiefen, nicht leuchtenden Augenhöhlen
- versetzte Moos-, Wurzel- und Trockenlagen statt runder Bewuchskugeln
- breite Platschfüße und lange Greifhände für klaren Bodenkontakt
- sichtbarer Überkopfwurf mit Ausholen, Rumpfdrehung und Nachschwingen

## Produktionsstand

- LOD0: 15.680 Tris
- LOD1: 8.466 Tris
- LOD2: 3.436 Tris
- 3 PBR-Materialien, keine Emission
- 17 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach GLB-Roundtrip: 1,9485 m bei 1,90 m Soll; innerhalb der Animations-/Silhouettentoleranz
- normierte Skinweights und geprüfter Armature Modifier
- `ProjectileSocket` in allen drei Runtime-GLBs vorhanden
- alle drei LODs nach frischem Blender-GLB-Import: PASS
- produktives Visual-Prefab GUID-erhaltend ersetzt: PASS
- Gegner-Prefab und bestehende Szenenreferenzen unverändert gültig: PASS
- LODGroup: 0,55 / 0,25 / 0,07
- bestehender CapsuleCollider 0,4992 m / 2,016 m unverändert: PASS

## Dateien

- Blender-Quelle: `Assets/_Game/Art/MidPoly/GoldenMasters/MoorThrower/Source~/CRE_MoorThrower_Mid_Phase3_Production.blend`
- Runtime-GLBs: `Assets/_Game/Art/MidPoly/GoldenMasters/MoorThrower/Runtime/`
- Manifest: `Assets/_Game/Art/MidPoly/GoldenMasters/MoorThrower/MOORTHROWER_PRODUCTION_MANIFEST.json`
- Technischer Bericht: `MOORTHROWER_TECHNICAL_REPORT.json`
- In-World-Abnahme: `InWorld/greyrifts_moor_thrower.png`
- Low-vs-Mid: `MOORTHROWER_LOW_VS_MID.png`
- 8-Punkt-Showcase: `MOORTHROWER_MID_8POINT.png`
- Spielkamera: `MOORTHROWER_MID_GAMECAM.png`
- Wurfpose: `MOORTHROWER_MID_THROW.png`
- Wurfanimation: `MOORTHROWER_MID_WURF.mp4`

## Abnahme und Regression

Die visuelle Freigabe erfolgte am 20.08.2026. Das Modell ist GUID-erhaltend in `MoorThrower_3D.prefab` eingebunden; die vorherige Fassung liegt als Legacy-Fallback unter `Assets/_Game/Prefabs/Actors/3D/Fallback/MoorThrower_3D_Legacy.prefab`.

- Produktionsvertrag, Rig, Clips, LODs, Skinweights, Socket und Footprint: PASS
- Mesh-Präsentation, Bodenkontakt, Gegnerframework, Status/Feedback, Tier Two und Population: 213/213 PASS
- relevante PlayMode-Spawn-/Kampf-/Szenentests: 11/11 PASS
- isolierter NavMesh-Hindernistest: 1/1 PASS; der Sammellauf zeigt die bekannte reihenfolgeabhängige Fluktuation
- automatisierte In-World-Aufnahme in den Graurissen: 1/1 PASS

Damit ist der MoorThrower als Fernkampf-Golden-Master abgeschlossen und für die Migration verwandter Fernkampfgegner freigegeben.
