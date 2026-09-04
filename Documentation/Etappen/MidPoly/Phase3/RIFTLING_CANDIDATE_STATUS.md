# MIDPOLY-000 – Phase 3 Riftling-Produktion

**Stand:** 20.08.2026  
**Status:** visuell freigegeben, technisch geprüft und produktiv integriert  
**Familie:** 17-Bone-Standardkreaturen

## Gestaltung

Der Riftling behält die Identität der bisherigen Spielfigur, wird aber als kantiges Mid-Poly-Wesen neu aufgebaut:

- schmaler, leicht vorgebeugter digitigrader Körper
- außergewöhnlich lange, dünne Arme mit drei tief hängenden Klauen
- schmaler Schädel mit dunklen Augenhöhlen, seitlichen Wangenhörnern und fünf nach hinten gezogenen Kammspitzen
- gestaffelte Schulter-, Rücken-, Arm- und Oberschenkelplatten statt runder Volumen
- gebrochene, triangulierte Oberflächen mit harter Facettenschattierung
- dunkles Violett/Anthrazit mit kontrolliertem Grauviolett für Knochenplatten und Klauen
- nur sehr schwache violette Rift-Nähte; keine dominante Leuchtwirkung

## Produktionsstand

- LOD0: 15.986 Tris
- LOD1: 8.632 Tris
- LOD2: 3.509 Tris
- 3 Materialien
- 17 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach GLB-Roundtrip: 1,7224 / 1,7224 / 1,7181 m bei 1,70 m Soll
- normierte Skinweights für alle drei LODs
- alle drei LODs nach frischem Blender-GLB-Import: PASS
- produktives Visual-Prefab GUID-erhaltend ersetzt: PASS
- Gegner-Prefab und bestehende Szenenreferenzen unverändert gültig: PASS
- LODGroup: 0,55 / 0,25 / 0,07
- bestehender CapsuleCollider 0,4576 m / 1,848 m unverändert: PASS

## Dateien

- Blender-Quelle: `Assets/_Game/Art/MidPoly/Creatures/Riftling/Source~/CRE_Riftling_Mid_Phase3_Production.blend`
- Runtime-GLBs: `Assets/_Game/Art/MidPoly/Creatures/Riftling/Runtime/`
- Manifest: `Assets/_Game/Art/MidPoly/Creatures/Riftling/RIFTLING_PRODUCTION_MANIFEST.json`
- Technischer Bericht: `RIFTLING_TECHNICAL_REPORT.json`
- In-World-Abnahme: `InWorld/greyrifts_riftling.png`
- Low-vs-Mid: `RIFTLING_LOW_VS_MID.png`
- 8-Punkt-Showcase: `RIFTLING_MID_8POINT.png`
- Front: `RIFTLING_MID_FRONT.png`
- Dreiviertelansicht: `RIFTLING_MID_THREEQUARTER.png`
- Spielkamera: `RIFTLING_MID_GAMECAM.png`
- Angriffspose: `RIFTLING_MID_ATTACK.png`
- Angriffsanimation: `RIFTLING_MID_ANGRIFF.mp4`

## Abnahme und Regression

Die visuelle Freigabe erfolgte am 20.08.2026. Das Modell ist GUID-erhaltend in `Riftling_3D.prefab` eingebunden; die vorherige Fassung liegt als Legacy-Fallback unter `Assets/_Game/Prefabs/Actors/3D/Fallback/Riftling_3D_Legacy.prefab`.

- Produktionsvertrag, Rig, Clips, LODs, Skinweights und Footprint: PASS
- Mesh-Präsentation, Bodenkontakt, Gegnerframework, Status/Feedback, Tier Two und Population: 213/213 PASS
- relevante PlayMode-Spawn-/Kampf-/Szenentests: 11/11 PASS
- isolierter NavMesh-Hindernistest: 1/1 PASS; der Sammellauf zeigt die bekannte reihenfolgeabhängige Fluktuation
- automatisierte In-World-Aufnahme in den Graurissen: 1/1 PASS

Damit ist der Riftling als erster produktiver Vertreter der 17-Bone-Standardfamilie abgeschlossen. Als nächster Standardgegner folgt der EmberEater.
