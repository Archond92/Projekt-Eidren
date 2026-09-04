# MIDPOLY-000 – Phase 3 Status

**Stand:** 24.08.2026  
**Phase:** Kreaturen und Bosse  
**Status:** abgeschlossen und produktiv integriert

## Golden Masters

| Familie | Referenz | Status |
|---|---|---|
| humanoide Standardkreatur | Wildling | abgeschlossen |
| vierbeinig/gedrungen | RootCharger | visuell freigegeben, geprüft und produktiv integriert |
| gepanzert | GraniteShell | visuell freigegeben, geprüft und produktiv integriert |
| Fernkampf | MoorThrower | visuell freigegeben, geprüft und produktiv integriert |

## RootCharger-Abschluss

Der RootCharger ist mit editierbarer Blenderquelle, drei geprüften LODs, zwei PBR-Materialien, unverändertem 19-Bone-Rig und sämtlichen acht Gameplayclips produktiv eingebunden. Prefab- und Gegner-GUID blieben erhalten. Technische Regression, PlayMode-Verhalten und In-World-Darstellung sind geprüft.

## GraniteShell-Abschluss

Der GraniteShell ist mit editierbarer Blenderquelle, drei geprüften LODs, zwei PBR-Materialien, unverändertem 19-Bone-Rig und sämtlichen acht Gameplayclips produktiv eingebunden. Visual- und Gegner-GUID blieben erhalten. Technische Regression, PlayMode-Verhalten und In-World-Darstellung sind geprüft.

## MoorThrower-Abschluss

Der MoorThrower ist mit drei geprüften LODs, unverändertem 17-Bone-Rig, sämtlichen acht Gameplayclips und einem stabil an `handR` gebundenen `ProjectileSocket` produktiv eingebunden. Visual- und Gegner-GUID blieben erhalten. Technische Regression, PlayMode-Verhalten und In-World-Darstellung sind geprüft.

Alle geforderten Kreaturen-Golden-Master sind damit abgeschlossen. Die Serienmigration beginnt mit der 17-Bone-Standardfamilie und dem Riftling als erstem Familienvertreter.

## Serienmigration – Riftling

Der Riftling ist als erster Vertreter der 17-Bone-Standardfamilie visuell freigegeben und produktiv integriert. Drei LODs, alle acht Bestandsclips, das unveränderte Rig, normierte Skinweights, GUID-Erhalt, Collidervertrag, technische Regression und In-World-Darstellung sind geprüft. Die Serienmigration wird mit dem EmberEater fortgesetzt.

## Serienmigration – EmberEater

Der EmberEater ist mit schwerer Knöchelgänger-Silhouette visuell freigegeben und produktiv integriert. Drei LODs, alle acht Bestandsclips, das unveränderte 17-Bone-Rig, 1,600 m Höhe, normierte Skinweights, GUID-Erhalt, Collidervertrag, Regression und Forge-Capture sind geprüft. Collider-, Agent- und Gegnerdaten blieben unverändert. Als Nächstes folgt die verbleibende 17-Bone-Wächterfamilie; der CoreGuardian wird als Boss im späteren Bossdurchgang behandelt.

## Serienmigration – RiftGuardian

Der 3-m-RiftGuardian ist nach dem Guardian-Audit als durchgehend einteilige Modellgeneration neu aufgebaut und produktiv integriert. Der eingebettete Legacy-Körper sowie die optisch getrennte Plattenhülle wurden vollständig entfernt; facettierte Felsanatomie, eingewachsene Steinlagen, Riftbrüche und Kristalle folgen nun einer gemeinsamen Formensprache. Drei LODs, alle acht Bestandsclips, das unveränderte 17-Bone-Rig, normierte Skinweights, GUID-Erhalt, Collidervertrag und vier spezialisierte Unity-Tests sind geprüft. Collider-, Agent- und Gegnerdaten blieben unverändert.

## Serienmigration – ForgeGuardian

Der 2,50-m-ForgeGuardian ist nach dem Guardian-Audit ohne eingebetteten Legacy-Körper neu aufgebaut und produktiv integriert. Eine zusammenhängende geschmiedete Anatomie trägt wenige große Brust-, Arm- und Beinpanzer; Schild und Axt bestehen jeweils aus genau einer Waffenbaugruppe. Der individuelle einhändige Axthieb hält den Schild aktiv. Drei LODs, alle acht Clips, das unveränderte 17-Bone-Rig, normierte Skinweights, GUID-Erhalt, Collidervertrag und vier spezialisierte Unity-Tests sind geprüft.

## Serienmigration – SealGuardian

Der 3,00-m-SealGuardian ist nach dem Guardian-Audit ohne eingebetteten Legacy-Körper neu aufgebaut und produktiv integriert. Eine durchgehende facettierte Robe mit breiten natürlichen Falten ersetzt die frühere gekachelte Robenhülle; Schulter, Helm, Siegel und der nicht-emissive Kristallstab sind als kontrollierte Akzente integriert. Sein einhändiger diagonaler Stabschlag behält die stabilisierte Waffenführung. Drei LODs, alle acht Clips, das unveränderte 17-Bone-Rig, normierte Skinweights, GUID-Erhalt, Collidervertrag und vier spezialisierte EditMode-Tests sind geprüft.

## Guardian-Audit – CoreGuardian

Auch beim 4,50-m-CoreGuardian wurde der eingebettete Legacy-Körper entfernt. Die vorhandene vollständige Ofen-, Panzer- und Kernkonstruktion bildet nun allein das produktive Mesh; der bewusst separate Legacy-Fallback bleibt außerhalb des Modells erhalten. LOD0/1/2 mit 54.360 / 30.440 / 13.046 Dreiecken, 17 Bones, acht Clips, normierte Skinweights, GLB-Roundtrip und GUID-Erhalt sind geprüft. Der individuelle Bossangriff ist als zweistufiger, wechselnder Slam mit rechtem und linkem Einschlag produktiv integriert. Clipname, 25,6-Frame-Quellbereich, Actor-Root, Collider und Controllervertrag bleiben unverändert; die erweiterte Unity-Produktionssuite besteht mit 5/5 Tests.

## Serienmigration – AshRunner

Der 1,80-m-AshRunner ist visuell freigegeben und produktiv integriert. Die zuvor zu breite erste Iteration wurde verworfen; die Produktionsfassung besitzt einen schmalen hochbeinigen Körper, gestreckten Schädel, nach hinten gefegten Klingenkamm, gestaffelte Ascheplatten, lange freistehende Läufe und eine gepanzerte dreigliedrige Schwanzkette. Drei LODs, alle acht Clips, das unveränderte 22-Bone-Rig, normierte Skinweights, GUID-Erhalt, Collidervertrag, GLB-Roundtrip, vier spezialisierte EditMode-Tests und der vollständige In-World-Lauf sind geprüft. Der schnelle Biss behält seinen sichtbaren dreigliedrigen Schwanznachlauf. Die Serienmigration wird mit Ignivar fortgesetzt.

## Serienmigration – Ignivar

Der 1,00-m-Ignivar ist als kompakter Feuerfuchs visuell freigegeben und produktiv integriert. Geschichtete Obsidian-Fellfacetten, keilförmige Schnauze, geteilte Ohren, kurze kräftige Läufe, geometrische Flammenmähne und die hochgezogene siebenfach aufgefächerte Schwanzkrone bilden eine eigenständige Anatomie innerhalb der Wildling-Formensprache. LOD0/1/2 mit 25.446 / 14.248 / 5.996 Dreiecken, drei Materialien, alle acht Clips, das unveränderte 22-Bone-Rig, normierte Skinweights, Bissanimation und GLB-Roundtrip sind geprüft. Visual- und Präsentations-Prefab-GUID blieben erhalten; die Unity-Produktionssuite besteht mit 3/3 Tests. Vorder-, Seiten- und Rückansicht wurden am tatsächlichen Unity-Produktionswrapper in Ruhe und Bewegung geprüft.

## Serienmigration – Terrock, Noctarion und Garon

Die verbleibenden Spezialkreaturen und der Boss Garon sind produktiv integriert. Terrock besitzt 30.872 / 17.288 / 7.409 Dreiecke, 22 Bones und zehn Clips; Noctarion 24.150 / 13.524 / 5.778 Dreiecke, 22 Bones und zehn Clips; Garon 53.660 / 30.048 / 12.878 Dreiecke, 22 Bones und neun Clips. GLB-Roundtrip, normierte Skinweights, vollständige Clipsets, erhaltene Visual- und Consumer-GUIDs sowie die gemeinsame Unity-Produktionssuite sind geprüft.

## Abschluss – Animationsdiversität der Zweibeiner

Der Audit hatte bestätigt, dass Wildling, Riftling, EmberEater, RiftGuardian, ForgeGuardian, SealGuardian und CoreGuardian ursprünglich exakt dieselbe Angriffs-Choreografie besaßen. Alle sieben Figuren verfügen nun über ein eigenständiges, zu Silhouette, Anatomie und Ausrüstung passendes Angriffsprofil.

Verbindliche Zielprofile:

| Figur | neuer visueller Angriff | Status |
|---|---|---|
| Wildling | asymmetrischer Klauen-/Prankenhieb | produktiv integriert; Front-, Seiten- und Spielkamera-Kontaktpruefung bestanden |
| Riftling | aggressiv versetzter Risshieb | Wildling-Formensprache und individueller Risshieb produktiv integriert; GLB- und Unity-Pruefung bestanden |
| EmberEater | gebückter kurzer Körper-/Biss-/Prankenstoß | Wildling-Formensprache und individueller Knöchel-/Prankenstoß produktiv integriert; GLB- und Unity-Pruefung bestanden |
| RiftGuardian | asymmetrischer massengetriebener Bodenschlag | einteilige Felsanatomie und individueller Angriff produktiv integriert; GLB- und Unity-Pruefung bestanden |
| ForgeGuardian | einhändiger Axthieb bei aktivem Schild | einteilige Schmiedeanatomie und individueller Angriff produktiv integriert; GLB- und Unity-Pruefung bestanden |
| SealGuardian | einhändiger diagonaler Stabschlag | visuell freigegeben, produktiv integriert und in Unity geprüft |
| CoreGuardian | mehrstufiger Boss-Slam | zweistufiger wechselnder Boss-Slam produktiv integriert; GLB-Roundtrip und Unity-Pruefung bestanden |

Gameplay-Timing, Events, Reichweite, Root-Motion-Semantik, Collider und Controllerzustände bleiben unverändert. Die sieben Angriffe sind im direkten Vergleich unterscheidbar und in Unity geprüft; der Pflichtpunkt Animationsdiversität ist damit abgeschlossen.

## Phase-3-Abschluss

Alle 16 Figuren aus dem verbindlichen Produktionsauftrag – Wanderer, 14 Standard-/Spezialkreaturen und Garon – besitzen produktive Mid-Poly-Modelle mit vollständigen vorhandenen Clipsets. Sämtliche Kreaturen- und Bossmodelle der Phase 3 sind mit LODs, Rig-/Skin-Verträgen, erhaltenen Prefabreferenzen und Unity-Prüfungen integriert. Die abschließende gemeinsame Mid-Poly-Produktionsregression besteht mit 80/80 EditMode-Tests. Das Phase-0-Assetregister ist für alle Figuren auf `abgenommen` beziehungsweise `fallback` nachgeführt; kein Figureneintrag bleibt `ungeprueft`. Die nächste Produktionsphase ist Phase 4 mit Gebäuden, Stationen und Containern; die Werkbank bleibt deren Golden Master.
