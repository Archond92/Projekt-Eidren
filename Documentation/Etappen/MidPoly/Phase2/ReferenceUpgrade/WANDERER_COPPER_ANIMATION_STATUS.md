# Wanderer in Kupferrüstung – Animationsstatus

Stand: Animierter High-Poly-Kandidat, produktionsnahe LODs und Unity-Review-Prefab fertig. Der aktive Spieler-Prefab ist noch nicht umgestellt.

## Ergebnis

- Das neue High-Poly-Modell ist auf das bestehende Wanderer-Skelett übertragen.
- Die Rüstung und die gesteppten Stoffflächen folgen den Bewegungen über überblendete Gewichte.
- Die Kupferspitzhacke ist an den validierten Werkzeugknochen gebunden.
- Alle 25 vorhandenen Wanderer-Clips sind in der animierten GLB-Datei enthalten.
- Die technische Rückprüfung der exportierten GLB-Datei ist bestanden.

## Umfang

- 73.316 animierte Dreiecke
- 312 gewichtete Modellteile
- 20 Knochen
- 25 Animationsclips
- Vorschauen: Ruhe, Gehen, Laufen, Spitzhackenabbau, Hammerangriff und Speerangriff

## Bewegungsprüfung

- Spitzhackenabbau: maximale Werkzeugänderung 66,707 Grad pro Bild – bestanden
- Hammerangriff: maximale Werkzeugänderung 70,137 Grad pro Bild – bestanden
- Speerangriff: maximale Werkzeugänderung 20,385 Grad pro Bild – bestanden
- Grenzwert: 75 Grad pro Bild

Damit treten in den geprüften Bewegungen keine ungewollten vollständigen Werkzeugüberschläge auf. Hammer- und Speerangriff behalten unterschiedliche Bewegungsverläufe.

## Dateien

- Blender-Arbeitsdatei: `TempReview/Wanderer3D-ReferenceUpgrade/Wanderer_Copper_ReferenceUpgrade_HighPoly_Animated.blend`
- Animierter Austausch-Export: `TempReview/Wanderer3D-ReferenceUpgrade/Wanderer_Copper_ReferenceUpgrade_HighPoly_Animated.glb`
- Technischer Prüfbericht: `Documentation/Etappen/MidPoly/Phase2/ReferenceUpgrade/WANDERER_COPPER_ANIMATION_TECHNICAL_REPORT.json`
- Vorschauordner: `Documentation/Etappen/MidPoly/Phase2/ReferenceUpgrade/Animation`

## Noch offen vor Produktionsfreigabe

- Zusammenführung mit den weiterhin benötigten Stoff- und Eisenmodulen des produktiven Wanderers
- Umschaltung des aktiven Spieler-Prefabs mit erhaltenem Legacy-Fallback
- Prüfung von Bodenkontakt, Handkontakt und Übergängen im finalen Spieler-Animator
- Performanceprüfung im Zielspiel

## Produktionskandidat und Unity-Prüfung

- LOD0: 64.062 Dreiecke
- LOD1: 36.656 Dreiecke beziehungsweise 57,22 Prozent von LOD0
- LOD2: 14.459 Dreiecke beziehungsweise 22,57 Prozent von LOD0
- Vier gebündelte Materialfamilien statt 15 Einzelmaterialien
- 13 Renderer je LOD
- GLB-Roundtrip: bestanden
- Unity-Import: bestanden
- Unity-Review-Prefab mit drei LOD-Stufen: bestanden
- Produktiver Spieler-Prefab: bewusst noch unverändert

Review-Prefab: `Assets/_Game/Prefabs/Actors/3D/Review/Player_CopperReference_Review.prefab`

Unity-Bericht: `Documentation/Etappen/MidPoly/Phase2/ReferenceUpgrade/WANDERER_COPPER_UNITY_IMPORT_REPORT.json`
