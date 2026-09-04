# MIDPOLY-000 – Phase 3 ForgeGuardian-Produktion

**Stand:** 24.08.2026  
**Status:** visuell freigegeben, produktiv integriert und technisch abgenommen  
**Familie:** 17-Bone-Standardkreaturen / gerüsteter Wächter

## Gestaltung

Der ForgeGuardian ist klar vom gewachsenen RiftGuardian getrennt: ein gebauter, dunkler Panzerträger mit Werkzeugwaffen statt Stein und Kristall. Beim Guardian-Audit wurde der in der alten Mid-Poly-Fassung mitgeführte Legacy-Körper entfernt.

- 2,50 m hohe, aufrechte und blockige Rüstungssilhouette
- geschlossener Helm mit dunklem schmalem Sehschlitz, Kamm und Wangenschutz
- eine zusammenhängende geschmiedete Anatomie mit wenigen großen Eisenplatten, Nieten, Bändern und Bronzebeschlägen
- fast körperbreiter, asymmetrisch geschmiedeter Schild am linken Arm
- lange Axt am rechten Arm mit deutlich aufgefächertem kantigem Blatt
- warme Schmiedezeichen an Brust, Schild und Axt als Albedo, ohne Emission
- schwere Beinschienen und breite Panzerstiefel
- natürliche hoch aufgelöste Metallfacetten statt einer quadratischen Kachelfigur
- einhändiger Axthieb bei aktivem Schild statt kopiertem Zweiarmschlag

## Produktionsstand

- LOD0: 23.264 Tris
- LOD1: 12.562 Tris
- LOD2: 5.112 Tris
- 3 Materialien: Eisen, Bronze/Beschläge, Schmiedezeichen
- keine Emission
- 17 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach frischem GLB-Import in Ruhepose: 2,548 m bei 2,50 m Soll
- normierte Skinweights für alle drei LODs
- GLB-Roundtrip für LOD0/1/2: PASS
- produktiver Visual-Prefab GUID-erhaltend migriert: `10f04432d55e7f244927f4ed06e922e2`
- produktiver Gegner-Prefab GUID-erhaltend migriert: `169a7ce4cc9d1294ca15af296e4a4ea3`
- LODGroup: 3 Stufen
- eingebetteter Legacy-Kern: nicht vorhanden
- spezialisierte EditMode-Tests einschließlich Axthieb und stabiler Waffenhand: 4/4 PASS
- CapsuleCollider: Radius 0,5856 m, Höhe 2,44 m
- Legacy-Fallback: `Assets/_Game/Prefabs/Actors/3D/Fallback/ForgeGuardian_3D_Legacy.prefab`

## Dateien

- Blender-Produktion: `Assets/_Game/Art/MidPoly/Creatures/ForgeGuardian/Source~/CRE_ForgeGuardian_Mid_Phase3_Production.blend`
- Runtime-Produktion: `Assets/_Game/Art/MidPoly/Creatures/ForgeGuardian/Runtime/CRE_ForgeGuardian_Mid_Production.glb`
- Produktionsmanifest: `Assets/_Game/Art/MidPoly/Creatures/ForgeGuardian/FORGEGUARDIAN_PRODUCTION_MANIFEST.json`
- technischer Bericht: `FORGEGUARDIAN_TECHNICAL_REPORT.json`
- Low-vs-Mid: `FORGEGUARDIAN_LOW_VS_MID.png`
- 8-Punkt-Showcase: `FORGEGUARDIAN_MID_8POINT.png`
- Front: `FORGEGUARDIAN_MID_FRONT.png`
- Dreiviertelansicht: `FORGEGUARDIAN_MID_THREEQUARTER.png`
- Spielkamera: `FORGEGUARDIAN_MID_GAMECAM.png`
- Angriffspose: `FORGEGUARDIAN_MID_ATTACK.png`
- Angriffsanimation: `FORGEGUARDIAN_SINGLE_GENERATION_ATTACK.mp4`
- In-World-Abnahme: `InWorld/forge_forgeguardian.png`

## Produktionsabnahme

- Migrationslauf und technischer Bericht: PASS
- gezielte EditMode-Regression: 213/213 PASS
- kompletter PlayMode-Lauf: 11/12 PASS; einzig die bekannte reihenfolgeabhängige NavMesh-Prüfung schlug im Verbund fehl
- dieselbe NavMesh-Prüfung isoliert: 1/1 PASS
- In-World-Capture-Test in `EidraForge`: 1/1 PASS
- Collider-, Gegner- und Szenenverträge blieben erhalten

Der freigegebene Sichtkandidat ist ohne Änderung des Rigs, der Clipnamen oder der Laufzeitverträge in die Produktion überführt. Der ForgeGuardian ist damit für Phase 3 abgeschlossen.
