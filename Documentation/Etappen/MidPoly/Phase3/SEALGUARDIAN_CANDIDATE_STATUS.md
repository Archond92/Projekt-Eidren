# MIDPOLY-000 – Phase 3 SealGuardian-Produktion

**Stand:** 24.08.2026  
**Status:** visuell freigegeben, produktiv integriert und technisch abgenommen  
**Familie:** 17-Bone-Standardkreaturen / Siegelwächter

## Gestaltung

Der SealGuardian bleibt die hohe, schmale Gegenfigur zum ForgeGuardian. Beim Guardian-Audit wurden der eingebettete Legacy-Körper und die darüberliegende gekachelte Robenhülle gemeinsam entfernt und durch genau eine Modellgeneration ersetzt.

- 3,00-m-Klasse mit langem, bodennahem Robenkörper
- durchgehender facettierter Robenkörper mit breiten natürlichen Längsfalten
- wenige eingewachsene Brustlagen statt einer zweiten Plattenfigur
- geschlossener Helm mit dunklem Sehschlitz, hohem Kamm und Wangenblechen
- deutlich zugespitzte Schulterplatten
- kupferne Siegelrauten entlang der Robenfront
- langer Stab rechts mit sechseckigem Kristallkopf als helle Albedo
- keine Emission
- natürliche hoch aufgelöste Stoff- und Metallfacetten

## Technischer Produktionsstand

- LOD0: 18.784 Tris
- LOD1: 10.330 Tris
- LOD2: 4.508 Tris
- 3 Materialien: Robe/Eisen, Bronze, Siegel/Stablicht
- 17 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- individueller Angriffskandidat: einhändiger diagonaler Stabschlag mit Oberkörperdrehung und freiem Ausgleichsarm
- stabilisierte Waffenführung: Der Stab folgt Schulter und Unterarm ohne zusätzliche Handgelenk-Rollbewegung; lineare Quaternion-Interpolation verhindert Überschwingen zwischen den Schlüsselposen
- Clipname, Frameumfang und Gameplay-Timing des Angriffs bleiben kompatibel
- Höhe im Ruhe-GLB: 3,006 m inklusive Stabspitze bei 3,00 m Sollklasse
- normierte Skinweights für alle drei LODs
- GLB-Roundtrip für LOD0/1/2: PASS
- eingebetteter Legacy-Kern: nicht vorhanden
- 8-Punkt-Rundumprüfung: anliegende Robensiegel, keine schwebenden Frontdetails und keine zweite Robenhülle
- stabilisierte Waffenführung über 33 Abtastpunkte ohne Handgelenk-Roll: PASS
- produktives Visual-Prefab GUID-erhaltend migriert: `92c0b93beea767a4eb5c1f267903da03`
- produktives Gegner-Prefab unverändert: `5134989611299da499f7ee70aeb2ca48`
- Collidervertrag unverändert: Radius 0,7104 m, Höhe 2,96 m
- EditMode-Produktionssuite: 4/4 PASS
- vollständiger In-World-Kreaturenlauf in echter Spielkamera: PASS
- EidraForge-Capture: PASS

## Dateien

- Blender-Produktion: `Assets/_Game/Art/MidPoly/Creatures/SealGuardian/Source~/CRE_SealGuardian_Mid_Phase3_Production.blend`
- kombinierte Runtime-Produktion: `Assets/_Game/Art/MidPoly/Creatures/SealGuardian/Runtime/CRE_SealGuardian_Mid_Production.glb`
- LOD-Dateien: `Assets/_Game/Art/MidPoly/Creatures/SealGuardian/Runtime/CRE_SealGuardian_Mid_LOD0.glb` bis `LOD2.glb`
- Produktionsmanifest: `Assets/_Game/Art/MidPoly/Creatures/SealGuardian/SEALGUARDIAN_PRODUCTION_MANIFEST.json`
- technischer Produktionsbericht: `SEALGUARDIAN_TECHNICAL_REPORT.json`
- Low-vs-Mid: `SEALGUARDIAN_LOW_VS_MID.png`
- 8-Punkt-Showcase: `SEALGUARDIAN_MID_8POINT.png`
- Front: `SEALGUARDIAN_MID_FRONT.png`
- Dreiviertelansicht: `SEALGUARDIAN_MID_THREEQUARTER.png`
- Spielkamera: `SEALGUARDIAN_MID_GAMECAM.png`
- Angriffspose: `SEALGUARDIAN_MID_ATTACK.png`
- Telegraph-Animation: `SEALGUARDIAN_MID_TELEGRAPH.mp4`
- individueller Stabangriff: `SEALGUARDIAN_SINGLE_GENERATION_ATTACK.mp4`
- In-World-Capture: `SEALGUARDIAN_INWORLD_FORGE.png`

## Abschluss

Der freigegebene Sicht- und Angriffskandidat ist GUID-erhaltend in die Produktion überführt. Rig, Clipnamen, Gameplay-Timing, Collider, NavMesh- und Gegnerdaten blieben unverändert. Die stabilisierte individuelle Angriffsanimation ist im produktiven GLB enthalten und durch einen eigenen Regressionstest gegen erneutes Waffenrollen geschützt. Der SealGuardian ist damit für Phase 3 abgeschlossen.
