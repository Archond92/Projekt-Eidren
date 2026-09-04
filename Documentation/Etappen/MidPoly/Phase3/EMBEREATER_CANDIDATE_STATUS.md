# MIDPOLY-000 – Phase 3 EmberEater-Produktion

**Stand:** 20.08.2026  
**Status:** visuell freigegeben, technisch geprüft und produktiv integriert  
**Familie:** 17-Bone-Standardkreaturen / Knöchelgänger

## Gestaltung

Der Glutzehrer bleibt der kleinste und gedrungenste Gegner der Serie, erhält aber eine klarere schwere Mid-Poly-Anatomie:

- waagerechter, tief über dem Boden geführter Obsidianleib
- vier tragende Kontaktpunkte mit massiven vorderen Knöchelfäusten
- kurzer, breiter Schädel mit vorgelagerten orangefarbenen Augen
- aufgerissenes dunkles Maul mit gestaffelten Glutzähnen und sichtbarem Rachen
- gebrochene Rücken-, Flanken-, Schulter- und Gliedmaßenplatten
- stufige orange Lavarisse entlang des Rückens ohne echte Emission
- schmale Hinterhand und deutlich schwererer Schulterbereich

## Produktionsstand

- LOD0: 18.682 Tris
- LOD1: 10.088 Tris
- LOD2: 4.110 Tris
- 3 Materialien, keine Emission
- 17 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach GLB-Roundtrip: 1,600 m für alle drei LODs bei 1,60 m Soll
- normierte Skinweights für alle drei LODs
- alle drei LODs nach frischem Blender-GLB-Import: PASS
- produktives Visual-Prefab GUID-erhaltend ersetzt: PASS
- Forge-Gegner-Prefab und bestehende Szenenreferenzen unverändert gültig: PASS
- LODGroup: 0,55 / 0,25 / 0,07
- bestehender CapsuleCollider 0,4128 m / 1,720 m unverändert: PASS
- Renderer-Footprint 1,359 × 1,600 × 1,787 m: PASS

## Dateien

- Blender-Quelle: `Assets/_Game/Art/MidPoly/Creatures/EmberEater/Source~/CRE_EmberEater_Mid_Phase3_Production.blend`
- Runtime-GLBs: `Assets/_Game/Art/MidPoly/Creatures/EmberEater/Runtime/`
- Manifest: `Assets/_Game/Art/MidPoly/Creatures/EmberEater/EMBEREATER_PRODUCTION_MANIFEST.json`
- Technischer Bericht: `EMBEREATER_TECHNICAL_REPORT.json`
- Forge-Capture: `InWorld/forge_embereater.png`
- Low-vs-Mid: `EMBEREATER_LOW_VS_MID.png`
- 8-Punkt-Showcase: `EMBEREATER_MID_8POINT.png`
- Front: `EMBEREATER_MID_FRONT.png`
- Dreiviertelansicht: `EMBEREATER_MID_THREEQUARTER.png`
- Spielkamera: `EMBEREATER_MID_GAMECAM.png`
- Angriffspose: `EMBEREATER_MID_ATTACK.png`
- Angriffsanimation: `EMBEREATER_MID_ANGRIFF.mp4`

## Abnahme und Regression

Die visuelle Freigabe erfolgte am 20.08.2026. Das Modell ist GUID-erhaltend in `EmberEater_3D.prefab` eingebunden; die vorherige Fassung liegt als Legacy-Fallback unter `Assets/_Game/Prefabs/Actors/3D/Fallback/EmberEater_3D_Legacy.prefab`.

- Produktionsvertrag, Rig, Clips, LODs, Skinweights und Footprint: PASS
- Mesh-Präsentation, Bodenkontakt, Gegnerframework, Status/Feedback und Population: 213/213 PASS
- relevante PlayMode-Spawn-/Kampf-/Szenentests: 11/11 PASS
- isolierter NavMesh-Hindernistest: 1/1 PASS; der Sammellauf zeigt die bekannte reihenfolgeabhängige Fluktuation
- expliziter Forge-Capture-Lauf: 1/1 PASS

Der dokumentierte Unterschied zwischen Modellbreite und Navigationsradius wurde bewusst nicht durch eine Gameplayänderung kaschiert. Collider, NavMesh-Agent und Gegnerdaten blieben unverändert. Als Nächstes wird die verbleibende 17-Bone-Wächterfamilie fortgesetzt; der CoreGuardian bleibt wegen seiner Bossrolle für den späteren Bossdurchgang reserviert.
