# MIDPOLY-000 – Phase 3 Ignivar-Produktion

**Stand:** 24.08.2026  
**Status:** visuell freigegeben, produktiv integriert und technisch abgenommen  
**Familie:** 22-Bone-Vierbeiner mit aufgerichtetem dreigliedrigem Schwanz

## Gestaltung

Ignivar bleibt die kleinste, schnellste und fangbare Eidra der Serie. Die neue Fassung überträgt den Feuerfuchs aus der Spritevorlage in eine klar kantige Mid-Poly-Silhouette, ohne ihn mit dem langen, schmalen AshRunner zu verwechseln.

- kompakter, niedriger Fuchskörper mit breitem Brustkorb
- kurze kräftige Läufe und deutlich getrennte Pfoten mit gespreizten Krallen
- keilförmige Schnauze, harte Brauenplatten und geteilte hohe Ohren
- geschichtete Obsidian-Fellfacetten statt glatter Körperflächen
- gebrochene orange Flanken- und Beinzeichen ausschließlich über Albedo
- geometrische Flammenmähne in zwei Temperaturstufen
- hochgezogener dreigliedriger Schwanz mit siebenfach aufgefächerter Flammenkrone
- vollständig flache Facettenschattierung; keine Emission und keine Partikelflammen

## Produktionsstand

- LOD0: 25.446 Tris
- LOD1: 14.248 Tris
- LOD2: 5.996 Tris
- 3 Materialien: Obsidianfell, Flammenglut, heißer Flammenkern
- exakt 22 Bones einschließlich `tail1`, `tail2` und `tail3`
- exakt 8 vorhandene Clips: Ruhe, Gehen, Telegraph, Angriff, Treffer, Taumeln, Tod, Erscheinen
- tatsächliche Höhe nach GLB-Roundtrip: 1,040 m bei 1,00 m Sollklasse
- Breite: 0,693 m; Länge: 1,577 m
- normierte Skinweights für alle drei LODs
- GLB-Roundtrip für LOD0/1/2: PASS
- bestehender sehr schneller Biss und die aufgerichtete Schwanzbewegung unverändert übernommen
- produktive Prefab- und Präsentationsintegration: PASS, beide GUIDs erhalten
- Unity-Produktionssuite: 3/3 PASS
- Unity-Sichtprüfung aus Vorder-, Seiten- und Rückansicht in Ruhe und Bewegung: PASS

## Dateien

- Blender-Produktion: `Assets/_Game/Art/MidPoly/Creatures/Ignivar/Source~/CRE_Ignivar_Mid_Phase3_Production.blend`
- kombinierte Runtime-Produktion: `Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_Production.glb`
- LOD-Dateien: `Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_LOD0.glb` bis `LOD2.glb`
- Manifest: `Assets/_Game/Art/MidPoly/Creatures/Ignivar/IGNIVAR_PRODUCTION_MANIFEST.json`
- GLB-Bericht: `IGNIVAR_PRODUCTION_GLB_REPORT.json`
- Unity-Bericht: `IGNIVAR_TECHNICAL_REPORT.json`
- Low-vs-Mid: `IGNIVAR_LOW_VS_MID.png`
- 8-Punkt-Showcase: `IGNIVAR_MID_8POINT.png`
- Front: `IGNIVAR_MID_FRONT.png`
- Dreiviertelansicht: `IGNIVAR_MID_THREEQUARTER.png`
- Seitenansicht: `IGNIVAR_MID_SIDE.png`
- Spielkamera: `IGNIVAR_MID_GAMECAM.png`
- Angriffspose: `IGNIVAR_MID_ATTACK.png`
- Bissanimation: `IGNIVAR_MID_ATTACK.mp4`
- Unity-Produktionscaptures: `InWorld/IgnivarFinal/`

## Abschluss

Ignivar ist GUID-erhaltend in den produktiven Visual-Prefab und den zur Laufzeit geladenen Eidra-Präsentationswrapper integriert. Eidra-Daten, WildEidra-Laufzeitpfad, Collider, NavMesh-Agent, Fanglogik und sämtliche Gameplayzeiten bleiben unverändert. LODGroup, Clipset, Skinweights, dreigliedrige Schwanzkette, Runtime-Wrapper und Unity-Darstellung sind geprüft. Damit ist Ignivar als letzte offene Kreatur der Phase 3 abgeschlossen.
