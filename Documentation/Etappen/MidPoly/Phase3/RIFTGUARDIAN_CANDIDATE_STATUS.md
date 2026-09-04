# MIDPOLY-000 – Phase 3 RiftGuardian-Produktion

**Stand:** 24.08.2026  
**Status:** visuell freigegeben, technisch geprüft und produktiv integriert  
**Familie:** 17-Bone-Standardkreaturen / Wächter

## Gestaltung

Der RiftGuardian behält seine Rolle als drei Meter hoher Rift-Steinwächter. Nach dem Guardian-Audit wurde die frühere Überlagerung aus Legacy-Körper und zusätzlicher Plattenfigur vollständig entfernt. Die Produktionsfassung besteht jetzt aus genau einer gemeinsamen Mid-Poly-Formensprache:

- durchgehende facettierte Felsanatomie statt eines runden Kerns unter einer separaten Plattenhülle
- eingewachsene, unregelmäßige Steinlagen an Brust und Gelenken statt aufgesetzter Mauerblöcke
- halslos zwischen den Schultern sitzender Felskopf
- durchgehender violetter Augenschlitz und zwei seitliche Kristallhörner
- breite Schulterbauten mit jeweils drei klaren Riftkristallen
- schwere anatomische Arme, große Fäuste und organisch eingewachsene Steinknöchel
- breite plantigrade Beine und natürliche Felszehen
- verzweigter, eingelassener Riftbruch statt horizontaler Trennstreifen
- hoch aufgelöste, aber klar lesbare Facetten ohne kugelig geglättete Rundflächen
- asymmetrischer massengetriebener Bodenschlag statt der kopierten Zweiarmschlag-Choreografie

## Produktionsstand

- LOD0: 19.084 Tris
- LOD1: 10.304 Tris
- LOD2: 4.191 Tris
- 3 Materialien: Stein, Riftkristall, Rune
- zurückhaltende Materialemission nur für Kristall und Rune
- 17 Bones mit unveränderten Namen
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- Höhe nach frischem GLB-Import in Ruhepose: rund 3,00 m
- normierte Skinweights für alle drei LODs
- GLB-Roundtrip für LOD0/1/2: PASS
- produktives Visual-Prefab GUID-erhaltend ersetzt: PASS
- TierTwo-Gegner-Prefab und bestehende Szenenreferenzen unverändert gültig: PASS
- LODGroup: 0,55 / 0,25 / 0,07
- bestehender CapsuleCollider 0,806 m / 3,255 m unverändert: PASS
- Renderer-Footprint 2,121 × 3,035 × 1,212 m: PASS
- eingebetteter Legacy-Kern: nicht vorhanden
- spezialisierte EditMode-Tests einschließlich Angriffssymmetrie: 4/4 PASS

## Dateien

- Blender-Quelle: `Assets/_Game/Art/MidPoly/Creatures/RiftGuardian/Source~/CRE_RiftGuardian_Mid_Phase3_Production.blend`
- Runtime-GLBs: `Assets/_Game/Art/MidPoly/Creatures/RiftGuardian/Runtime/`
- Manifest: `Assets/_Game/Art/MidPoly/Creatures/RiftGuardian/RIFTGUARDIAN_PRODUCTION_MANIFEST.json`
- Technischer Bericht: `RIFTGUARDIAN_TECHNICAL_REPORT.json`
- GreyRifts-Capture: `InWorld/greyrifts_rift_guardian.png`
- Low-vs-Mid: `RIFTGUARDIAN_LOW_VS_MID.png`
- 8-Punkt-Showcase: `RIFTGUARDIAN_MID_8POINT.png`
- Front: `RIFTGUARDIAN_MID_FRONT.png`
- Dreiviertelansicht: `RIFTGUARDIAN_MID_THREEQUARTER.png`
- Spielkamera: `RIFTGUARDIAN_MID_GAMECAM.png`
- Angriffspose: `RIFTGUARDIAN_MID_ATTACK.png`
- Angriffsanimation: `RIFTGUARDIAN_SINGLE_GENERATION_ATTACK.mp4`

## Abnahme und Regression

Die korrigierte Ein-Generationen-Fassung wurde am 24.08.2026 GUID-erhaltend in `RiftGuardian_3D.prefab` eingebunden. Der separate Legacy-Fallback unter `Assets/_Game/Prefabs/Actors/3D/Fallback/RiftGuardian_3D_Legacy.prefab` bleibt nur als bewusst getrennte Rückfallkopie erhalten und ist nicht Bestandteil des neuen Meshes.

- Produktionsvertrag, Rig, Clips, LODs, Skinweights und Footprint: PASS
- Mesh-Präsentation, Bodenkontakt, Gegnerframework, Status/Feedback und Population: 213/213 PASS
- relevante PlayMode-Spawn-/Kampf-/Szenentests: 11/11 PASS
- isolierter NavMesh-Hindernistest: 1/1 PASS; der Sammellauf zeigt die bekannte reihenfolgeabhängige Fluktuation
- expliziter GreyRifts-Capture-Lauf: 1/1 PASS

Collider, NavMesh-Agent, Gegnerdaten und TierTwo-Population blieben unverändert. Die verbleibenden Standardkreaturen werden fortgesetzt; der CoreGuardian bleibt wegen seiner Bossrolle für den späteren Bossdurchgang reserviert.
