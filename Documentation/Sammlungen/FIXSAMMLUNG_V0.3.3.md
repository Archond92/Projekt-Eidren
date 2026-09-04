# Fixsammlung v0.3.3

**Status: abgeschlossen am 26.08.2026.** Die Meldungen F33-001 bis F33-006
sind umgesetzt und im neuen Max-Loadout-Windows-Paket enthalten.

## F33-001 — Gebäude verschwinden direkt nach dem Bauen

**Bereich:** Basisbau, Mid-Poly-Gebäude, LOD

**Beobachtung (25.08.2026):** Die Bauvorschau lässt sich bestätigen, aber das
fertige Gebäude wird anschließend nicht angezeigt.

**Befund:** Der Bauvorgang selbst funktioniert. Der gespielte Teststand enthält
29 persistierte Gebäude und der Player-Log keine Ausnahme. Die neue
Mid-Poly-LOD-Gruppe blendete kleine Gebäude jedoch schon bei der normalen
orthografischen Kameragröße 7,4 vollständig aus: Boden (`size = 1`) erreichte
nur rund 0,068 Bildschirmhöhe bei einer Ausblendgrenze von 0,08; beim
Kochtopf lag dieselbe Größe unter der Grenze 0,07. Eine Katalogprüfung aller
elf Gebäude fand denselben Fehler an der Lagerkiste (0,066 bei Grenze 0,08)
und nur minimale Reserve an der Werkbank (0,074 bei Grenze 0,07). Darum
verschwanden gerade kleine Gebäude unmittelbar nach der Vorschau.

**Fix:** Die letzte LOD-Stufe aller elf baubaren Prefabs bleibt bis 0,01
sichtbar. Die vier zuständigen Mid-Poly-Builder schreiben diesen Wert
ebenfalls, damit ein späterer Neuaufbau den Fehler nicht zurückbringt.

**Nachweis:** Die Produktionsprüfungen für Basismodule und Stationen verlangen
jetzt für jedes Prefab, dass die letzte LOD-Grenze unter seiner projizierten
Größe bei der echten 7,4er-Spielkamera liegt. Der bestehende vollständige
Bau-und-Neulade-Test prüft zusätzlich am wirklich platzierten Boden mit einem
kontrollierten Renderdurchlauf der Spielkamera, dass mindestens ein Renderer
sichtbar ist.

**Status: umgesetzt und am 26.08.2026 abgesichert.** Gezielte EditMode-Suite
21/21, echte PlayMode-Bauabläufe 2/2, vollständige Katalogprüfung 11/11
sichtbar. Der Fix ist im am 26.08.2026 neu gebauten Max-Loadout-Paket
`0.3.3-maxloadout` enthalten.

## F33-002 — Mid-Poly-Wanderer hat keinen nackten Grundkörper

**Bereich:** Wanderer, Mid-Poly-Kandidat, Rüstung, Skinning

**Beobachtung (26.08.2026):** Ohne Rüstung zeigt der Mid-Poly-Wanderer keinen
vollständigen Körper. Die Low-Poly-Ausführung besitzt dagegen unter `Basis`
den erwarteten unbekleideten Grundzustand aus sichtbarer Haut und schwarzer
Hose.

**Befund:** Das Mid-Poly-Modul `Basis` enthält in allen drei LOD-Stufen nur
Kopf, Bart und Augen. Seine Geometrie endet ungefähr am Hals; Rumpf, Arme und
Beine fehlen vollständig. Der Low- und der Mid-Poly-Wanderer verwenden
dieselben 20 Knochen mit übereinstimmenden Bind-Posen. Der vorhandene
Low-Poly-Grundkörper kann deshalb ohne neues Rig oder Änderungen an den 25
Animationen weiterverwendet werden.

**Fix:** Die Mid-Poly-Erzeugung gewinnt aus dem freigegebenen Low-Poly-Modul
`Basis` eine eigene animierte Körperlage. Kopf und Haare werden an der
Halsgrenze von 1,62 m entfernt, damit sie sich nicht mit dem detaillierten
Mid-Poly-Kopf überlagern. Übrig bleiben 628 Dreiecke für Rumpf, Arme, Beine
und Hose. Vertexfarben, Gewichte und Bind-Posen bleiben erhalten; die 20
Knochen werden nach Namen auf das Mid-Poly-Rig abgebildet. Diese Körperlage
wird in LOD0, LOD1 und LOD2 dauerhaft dem Modul `Basis` zugeordnet, während
Rüstung weiterhin darüber ein- und ausgeblendet werden kann.

**Sicherheitsgrenze:** Die aktuell produktive Spielerfigur bleibt wie
angefordert vollständig Low Poly. F33-002 verändert nur den Mid-Poly-Kandidaten
und dessen reproduzierbaren Builder; ein späterer Mid-Poly-Aufbau enthält den
Grundkörper automatisch.

**Viewer-Nachprüfung (26.08.2026):** Der vorhandene HTML-Viewer V4 enthält
noch den Stand vor diesem Fix. Dort besteht `Basis` weiterhin nur aus sechs
Kopf-Renderern; der neu erzeugte Grundkörper ist noch nicht eingebettet.

**Viewer-Fix:** V5 bettet den aus derselben Low-Poly-Quelle gewonnenen
628-Dreiecke-Grundkörper separat in LOD0, LOD1 und LOD2 ein. Der Zustand
`Ohne` zeigt damit wieder Kopf, Körper, Arme, Beine und Hose.

**Status: umgesetzt und am 26.08.2026 abgesichert.** Das neue Körper-Asset
besteht seine drei gezielten Prüfungen für Geometrie, Vertexfarben,
Hautgewichte, 20-Knochen-Kompatibilität und die Einbindung in alle drei
LOD-Stufen. Zusätzlich bleiben alle sechs Produktionsprüfungen des
Low-Poly-Wanderers grün, einschließlich 256 Rüstungskombinationen, aller
Werkzeug- und Waffenfamilien sowie aller 25 Animationen. Der Mid-Poly-Fix ist
im Kandidaten und Viewer V5 enthalten; die spielbare F33-Ausgabe verwendet
weiterhin ausdrücklich den vollständigen Low-Poly-Wanderer.

## F33-003 — Laufanimationen springen an der Schleifengrenze

**Bereich:** Wanderer, Animation, Laufen

**Beobachtung (26.08.2026):** Beim endlosen Abspielen der Laufanimation ist
am Übergang vom letzten zurück zum ersten Frame ein Posensprung sichtbar.

**Befund:** Alle sechs `Laufen`-Varianten besitzen dieselbe offene
Schleifengrenze. Zwischen Anfang und Ende liegen maximal 0,04224 m
Translation und 15,122 Grad Rotation. Die sechs Ruhe- und sechs Gehclips
schließen dagegen exakt.

**Fix:** Der Low-Poly-Animationsgenerator schließt Ruhe-, Geh- und Laufzyklen
am letzten Exportframe ausdrücklich auf die Startpose. Dieselben korrigierten
25 Clips werden reproduzierbar in Low Poly, Mid-Poly-LOD0, LOD1, LOD2 und die
kombinierte Produktionsdatei geschrieben.

**Status: umgesetzt und am 26.08.2026 abgesichert.** Die Rohdatenprüfung aller
fünf GLBs misst an sämtlichen 18 Schleifen 0,000000 m Translation und
0,000000 Grad Rotation zwischen Anfang und Ende. Die Unity-Importprüfung
bestätigt dieselben Grenzen für Low- und Mid-Poly.

## F33-004 — `Oeffnen` ist eine nahezu statische falsche Pose

**Bereich:** Wanderer, Animation, Interaktion

**Beobachtung (26.08.2026):** `Oeffnen` beginnt bereits in einer stark
zurückgelehnten, knienden Haltung und zeigt über 1,25 Sekunden keine klar
erkennbare Öffnungsbewegung.

**Befund:** Der Clip besitzt weder einen Übergang aus dem Stand noch eine
sichtbare Greif-, Öffnungs- oder Rückkehrphase. Nur kleine Teilbewegungen
verändern die ansonsten fast unveränderte Pose.

**Fix:** `Oeffnen` wurde als Einmalaktion aus sicherem Stand, Vorlehnen,
beidhändigem Greifen, Öffnungsbewegung und Rückkehr neu erzeugt. Der alte
Knie-Teleport und die rückwärts gebogene Dauerpose sind entfernt.

**Status: umgesetzt und am 26.08.2026 abgesichert.** Die Mitte des Clips liegt
6,43 cm von der Startwurzel entfernt und bewegt den führenden Oberarm um
51,45 Grad. Start und Ende schließen wieder exakt. Rohdatenprüfung und beide
Unity-Importvarianten bestehen.

## F33-005 — Zweite Hand greift Abbauwerkzeuge nicht

**Bereich:** Wanderer, Animation, Werkzeuge

**Beobachtung (26.08.2026):** Bei `Abbau_Axt`, `Abbau_Spitzhacke` und
`Abbau_Sense` folgt das Werkzeug korrekt der führenden Hand, die zweite Hand
schwebt jedoch neben Körper oder Stiel.

**Befund:** Die in der älteren Abnahme beanstandete frei schwebende Ausrüstung
ist behoben. Für die glaubwürdige Zweihandführung fehlt aber weiterhin der
Kontakt der freien Hand zum Werkzeugstiel.

**Fix:** Die Zweithand-IK gilt jetzt für Hammer, Axt, Spitzhacke und Sense.
Der Griffpunkt darf realistisch am Stiel gleiten; beide Armketten werden
gemeinsam gelöst, damit weder die führende noch die freie Hand den Schaft
verliert. Die Lösung wird direkt in die exportierten Schlüsselbilder gebacken.

**Status: umgesetzt und am 26.08.2026 abgesichert.** Über Low Poly und alle
vier Mid-Poly-Dateien beträgt der größte gemessene Zweithandabstand 1,23 cm;
bei Axt 0,15 cm, Spitzhacke 0,55 cm und Sense 0,04 cm. Die Bewegungen wurden
zusätzlich im Viewer an Aushol-, Treffer- und Rückkehrphase sichtbar geprüft.

## F33-006 — Wanderer-Viewer prüft einen veralteten und unvollständigen Stand

**Bereich:** Prüfwerkzeug, Wanderer, LOD, Rendering

**Beobachtung (26.08.2026):** V4 enthält den neuen Grundkörper aus F33-002
nicht und bietet nur die höchste Detailstufe an.

**Befund:** Der eingebettete GLB-Stand besitzt ausschließlich LOD0. LOD1 und
LOD2 sind nicht auswählbar. Backface-Culling ist deaktiviert, wodurch fehlende
oder falsch ausgerichtete Rückseiten bei der Modellprüfung verdeckt werden
können. Eine sichtbare Quellversion oder Prüfsumme fehlt, sodass ein alter
Export nicht unmittelbar erkennbar ist.

**Fix:** Viewer V5 wird reproduzierbar aus dem aktuellen Kandidaten gebaut. Er
enthält LOD0, LOD1 und LOD2 einschließlich Grundkörper, startet mit aktivierter
Rückseitenprüfung und erlaubt deren gezieltes Abschalten. Erstellzeitpunkt und
SHA-256-Kurzprüfsumme der gewählten LOD-Datei stehen direkt im Bedienfeld. Die
fehlerhafte automatische Familienerkennung der V4 wurde ebenfalls korrigiert.

**Status: umgesetzt und am 26.08.2026 abgesichert.** Das Viewer-Audit prüft
alle drei eingebetteten LODs auf je einen 628-Dreiecke-Grundkörper, 25 Clips
und culling-fähiges Material. Alle drei Stufen wurden im Browser umgeschaltet
und sichtbar geprüft. `http://127.0.0.1:8749/` öffnet jetzt direkt V5.

Die vollständige Viewer-Abnahme liegt unter
`MidPoly_Phase2/ReferenceUpgrade/CompleteWanderer/Befund20260826/`.
