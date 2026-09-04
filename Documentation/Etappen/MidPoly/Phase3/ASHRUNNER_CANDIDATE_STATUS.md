# MIDPOLY-000 – Phase 3 AshRunner-Produktion

**Stand:** 21.08.2026  
**Status:** visuell freigegeben, geprüft und produktiv integriert  
**Familie:** 22-Bone-Vierbeiner mit dreigliedrigem Schwanz

## Gestaltung

Der AshRunner bleibt der schnelle, leichte Aschenläufer der Schmiede. Gegenüber dem alten glatten Low-Poly-Körper besitzt er eine schmale, kantige Konstruktion mit klar getrennten Ascheplatten und viel sichtbarer Luft zwischen den langen Läufen.

- gestreckter, hochbeiniger Vierbeiner statt gedrungener Panzerträger
- schmaler Schädel mit kleinen diagonalen Glutschlitzen und gezahntem Maul
- nach hinten gefegter Klingenkamm über Kopf, Nacken und Rücken
- gestaffelte Ascherippen und unregelmäßige Flankenplatten
- lange freistehende Läufe mit sichtbaren Gelenken und gespreizten Krallen
- dreigliedrige gepanzerte Schwanzkette mit kantiger heißer Spitze
- orange Glutfugen ausschließlich über Albedo; keine Emission
- flache Facettenschattierung ohne geglättete Rundflächen

## Technischer Produktionsstand

- LOD0: 24.470 Tris
- LOD1: 13.458 Tris
- LOD2: 5.580 Tris
- 3 Materialien: Aschepanzer, Kupfer-/Glutfugen, Knochen/Krallen
- exakt 22 Bones einschließlich `tail1`, `tail2` und `tail3`
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- tatsächliche Höhe nach GLB-Roundtrip: 1,807 m bei 1,80 m Sollklasse
- Länge: 2,904 m; schlanke Breite: 1,107 m
- normierte Skinweights für alle drei LODs
- GLB-Roundtrip für LOD0/1/2: PASS
- vorhandener schneller Biss und Schwanznachlauf ohne Timingänderung übernommen
- produktiver Visual-Prefab GUID-erhaltend aktualisiert: `58ed61fa5de0ed34e99d3961ba794a15`
- produktiver Gegner-Prefab GUID-erhaltend aktualisiert: `1a9416c05bcee034ea73b147a57cc176`
- LODGroup: 0,55 / 0,25 / 0,07
- bestehender CapsuleCollider unverändert: Radius 0,504 m, Höhe 2,10 m
- Legacy-Fallback vor der Migration gesichert
- vier spezialisierte EditMode-Tests: PASS
- vollständiger In-World-Kreaturenlauf und Forge-Capture: PASS

## Dateien

- Blender-Kandidat: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/Source~/CRE_AshRunner_Mid_Phase3_Candidate.blend`
- kombinierter Runtime-Kandidat: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/Runtime/CRE_AshRunner_Mid_Candidate.glb`
- LOD-Dateien: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/Runtime/CRE_AshRunner_Mid_LOD0.glb` bis `LOD2.glb`
- Manifest: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/ASHRUNNER_CANDIDATE_MANIFEST.json`
- technischer Bericht: `ASHRUNNER_CANDIDATE_TECHNICAL_REPORT.json`
- produktive Blenderquelle: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/Source~/CRE_AshRunner_Mid_Phase3_Production.blend`
- produktives Runtime-Asset: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/Runtime/CRE_AshRunner_Mid_Production.glb`
- Produktionsmanifest: `Assets/_Game/Art/MidPoly/Creatures/AshRunner/ASHRUNNER_PRODUCTION_MANIFEST.json`
- Unity-Produktionsbericht: `ASHRUNNER_TECHNICAL_REPORT.json`
- Forge-In-World-Capture: `ASHRUNNER_INWORLD_FORGE.png`
- Low-vs-Mid: `ASHRUNNER_LOW_VS_MID.png`
- 8-Punkt-Showcase: `ASHRUNNER_MID_8POINT.png`
- Front: `ASHRUNNER_MID_FRONT.png`
- Dreiviertelansicht: `ASHRUNNER_MID_THREEQUARTER.png`
- Seitenansicht: `ASHRUNNER_MID_SIDE.png`
- Spielkamera: `ASHRUNNER_MID_GAMECAM.png`
- Angriffspose: `ASHRUNNER_MID_ATTACK.png`
- Bissanimation: `ASHRUNNER_MID_ATTACK.mp4`

## Abschluss

Der freigegebene Kandidat ist ohne Änderung von Rig, Clipnamen, Gameplay-Timing, Collider, NavMesh-Agent oder Gegnerdaten in die Produktion überführt. Die vier Pfoten-Bones und der dreigliedrige Schwanz werden in den Regressionstests explizit geprüft; der schnelle Biss behält seinen sichtbaren Schwanznachlauf. Der AshRunner ist damit für Phase 3 abgeschlossen. Als Nächstes folgt der zweite Vertreter der 22-Bone-Familie, Ignivar.
