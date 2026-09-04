# Wanderer – vollständiges High-Detail-Referenzupgrade

**Stand:** 25.08.2026 (Animationsrevision V4.4)  
**Status:** High-Detail-Modell und Animationsrevision V4.4 aktiv in Unity 6000.3 integriert und geprüft

## Animationsrevision V4.4 (25.08.2026)

Befund aus der Sichtprüfung: In den Aktions-Clips **fehlte optisch der rechte Arm** —
die Zweithand-IK streckte ihn quer vor der Brust zur links geführten Schaftbahn und
begrub ihn dabei im Panzer. V4.4 blendet die Stützhand **reichweitenabhängig**: Liegt
der Zweitgriffpunkt außer Reichweite oder jenseits der Körpermitte, geht der rechte
Arm stattdessen in eine sichtbare seitliche **Balance-Pose** (`blend = reach_score *
cross_score`, stetig, kein Umspringen). Der Zweitgriff ist in der Griffprüfung seither
informativ statt hart. Zusätzlich greift die Axt jetzt wie Hacke/Hammer am Stielende
(`ANCHOR_SLIDE −0,30` statt −0,18) — der lange Reststiel unter der Faust lehnte beim
Überkopf-Heben an der linken Schulter (letzter Überlappungsrest, Abbau_Axt F7).

Ergebnis: Aktions-Überlappung 6/6 PASS, Griffkontakt 24/24 PASS (Primär ≤ 0,031 m),
Kerndurchstoß 30/30 PASS. Sichttafeln: `TempReview/Wanderer3D-AnimationBefund/ARM_CHECK.png`
(rechter Arm frontal sichtbar) und `V42_AKTIONEN_CHECK.png` (Stand V4.4).
Rig-Tag: `v4.4-balanced-arms`, Manifest `animationRevision: V4.4`.

## Animationsrevision V4.3 (25.08.2026)

Die interaktive Sichtprüfung zeigte weiterhin Waffen, die in Ausholphasen durch den
Körper schnitten — die V3-Schwungrotationen sind mit dem Faust-Anker grundsätzlich
unvereinbar. V4.3 ersetzt sie in den sechs Aktions-Clips durch **prozedurale,
körperfreie Choreografie**: seitlich am Panzer vorbeigeführte Grundbahnen
(Hacken/Hammer diagonal, Sense als tiefer Mähzug am Stielende gegriffen, Speer als
seitlich geführter Stoß aus dem hinteren Schaftdrittel) plus ein **Torso-Repulsor**,
der pro Frame Schaftsegment und Werkzeugkopf-Ring gegen die **aus dem Mesh
gemessene Panzerhülle** prüft und den Griff innerhalb der Armreichweite herausdrückt
(notfalls giert die Schaftrichtung weg). Körperbewegung (Ausfallschritt,
Gewichtsverlagerung) bleibt original.

Neu in der Prüfkette: `WANDERER_ACTION_OVERLAP_REPORT_V43.json` — in Aktions-Clips
null BVH-Schnittpaare Waffe↔Körper/Rüstung (Faustzonen über Dreiecks-ECKEN
ausgenommen; Zentroidfilter versagen bei den großen Low-Poly-Dreiecken). Ergebnis:
6/6 Clips PASS; Griffkontakt 24/24 PASS (Primär ≤ 0,001 m; Zweithand ≤ 0,30 m —
die seitlichen Bahnen liegen teils außer Reichweite der Stützhand, die sich dann
streckt); Kerndurchstoß 30/30 PASS. Unity EditMode 114/114, PlayMode 3/3.

Wichtige Werkzeuglehren: Reparaturen IMMER vom erhaltenen V3-Publishkandidaten
(`TempReview/Wanderer3D-ReferenceUpgrade/Wanderer_Complete_AnimationCandidate_V3.blend`)
ausgehen — der Publish überschreibt die Source~-Blend, eine Reparatur von dort
wendet Abduktion & Co. doppelt an. Sichttafeln: `Befund20260824/06_REPARATUR_AKTIONEN_V43.png`
und `07_REPARATUR_TRAGEN_V43.png`.

## Animationsrevision V4 (24.08.2026)

Der Animationsbefund vom 24.08. hatte gezeigt, dass in V3 keine Waffe in der Hand lag:
Die Waffenbones wurden von den Clips 0,19–0,60 m von der Hand weg „geparkt", der
V3-Durchdringungsbericht war trivial grün, WEIL die Waffen frei schwebten. V4 behebt das:

- **Waffen an die Hände gekoppelt.** Die Bind-Pose bindet alle Waffen korrekt an die
  Fäuste (der Kopf des `staff`-/`dag`-Bones IST der Faustpunkt). Ruhe/Gehen/Laufen
  verwenden jetzt die Bind-relative Kopplung; der Speer trägt 14 cm höher (Bodenfreiheit),
  der Hammer ruht 15 cm tiefer und 12° nach außen geneigt auf der Schulter (Helmfreiheit).
- **Aktions-Clips behalten den Schwungbogen** der V3-Rotationen; der sichtbare
  Schaftanker wird pro Frame exakt in die linke Faust gelegt.
- **Echte Zweihandführung** für Abbau (Spitzhacke, Axt, Sense), Hammer- und Speerangriff:
  beide Arme greifen per analytischer 2-Bone-IK (Segmentrichtungen — das rekonstruierte
  Rig ist ein Loose-Joint-Rig, alle Bone-Y-Achsen zeigen in Rest nach +Z). Die rechte
  Hand greift Hand-über-Hand die Schulterprojektion auf der Schaftlinie; der Griff
  bleibt vor der Brustpanzerlinie.
- **Skalierungsfehler behoben:** `Abbau_Spitzhacke` hatte Arm-Skalierungen bis 1,59;
  sämtliche Scale-Keys aller Clips sind auf 1,0 geglättet.
- **Neue Prüfkette:** `WANDERER_GRIP_CONTACT_REPORT_V4.json` (Waffenmesh muss den
  Handschuh berühren, Oberflächenabstand per BVH; 24/24 Clips PASS, max. 0,002 m) und
  `WANDERER_CORE_IMPALEMENT_REPORT_V4.json` (Azimut-Kriterium: Waffennahpunkte dürfen
  die Rumpf-/Kopfachse nicht umschließen; 30/30 Fälle PASS). Das alte
  0-Overlap-Kriterium stammte aus der Park-Ära und ist damit abgelöst — getragene
  Waffen liegen konstruktionsbedingt an der Rüstung an.
- Werkzeuge: Reparatur `TempReview/Wanderer3D-AnimationBefund/repair_wanderer_animation_v4.py`,
  Prüfungen `verify_wanderer_grip_contact.py` und `audit_wanderer_core_impalement.py` (ebenda),
  Veröffentlichung `publish_wanderer_animation_v4.py`. Sichtprüfungstafeln unter
  `Befund20260824/05_REPARATUR_*.png` und `06_REPARATUR_GRENZFAELLE.png`.

## Ergebnis

Der freigegebene Kupfer-Wanderer wurde zu einem vollständigen modularen Wanderer-Paket ausgebaut. Die neue Quelle enthält den Basiskörper, drei eigenständige Rüstungsfamilien, alle geforderten Waffen und Werkzeuge, das bestehende Rig sowie sämtliche 25 verbindlichen Animationen.

Die Stoffrüstung wurde als V2 vollständig neu aufgebaut und orientiert sich wieder klar an der freigegebenen Low-Poly-Silhouette: spitze Kapuze, dunkles Braun/Anthrazit, warme dreieckige Brustfelder, gegabelte Rockschöße, Gürtel, Armschienen und schmale dunkle Beinkleidung. Die vier Knöpfe sind flache, in die Knopfleiste eingelassene Scheiben statt frei stehender Kugeln. Ober- und Unterschenkel sind getrennt modelliert, verjüngt und durch Kniewickel sowie eigene Stiefelschäfte gegliedert; die frühere kastenförmige Hose ist damit ersetzt. Die Kupferrüstung behält die freigegebene Detailfassung mit Rautensteppung, Gurten, Schließen, Nieten und vollständiger Rückseite. Die Eisenrüstung besitzt eigene Geometriekopien und eine klar getrennte Materialfamilie; sie ist keine bloße Farbumschaltung der Kupfervariante.

Die Animationsrevision V3 passt sämtliche Waffenhaltungen an die neuen Silhouetten an. Spitzhacke, Axt und Sense besitzen nun voneinander unterscheidbare Abbaubewegungen; Hammer, Speer und Doppeldolche verwenden eigene Angriffsabläufe. Auch die jeweils sechs Ruhe-, Geh- und Laufvarianten wurden für alle Waffenfamilien korrigiert. Die Dolchhaltung wurde nach der Unity-Kameraprüfung zusätzlich überarbeitet: Die Griffe liegen jetzt an den Händen, die Schneiden zeigen vom Körper weg und alle vier Dolchclips bleiben frei von Kernkörper-Durchdringungen.

Der Kandidat ist mit Unity `6000.3.0f1` importiert und in `Player_3D.prefab` sowie `Player.prefab` aktiv geschaltet. Die vorhandenen Prefab-GUIDs und der Legacy-Fallback wurden erhalten. Beim ersten Unity-Sichttest entdeckte weiße prozedurale Kupfer- und Gambesonmaterialien wurden auf portable glTF-PBR-Grundfarben korrigiert; Kupfer, dunkles Blau und die geometrische Rautensteppung werden nun auch im Laufzeitimport dargestellt.

## Vollständiger Lieferumfang

- Basiskörper und Gesicht
- 12 Rüstungsmodule: Helm, Harnisch, Hände und Beine in Stoff, Kupfer und Eisen
- 20 konkrete Waffen-/Werkzeugmodule:
  - Hammer: Basis, Kupfer, Eisen, Sealbreaker
  - Dolche: Basis, Kupfer, Eisen, Ash Fangs
  - Speer: Kupfer, Eisen, Ember Thorn
  - Axt: Basis, Kupfer, Eisen
  - Spitzhacke: Basis, Kupfer, Eisen
  - Sense: Basis, Kupfer, Eisen
- 6 kompatible generische Waffen-Fallbacks
- 20 Bones und exakt 25 Animationsclips
- LOD0, LOD1 und LOD2 sowie eine kombinierte GLB-Datei

## Geometriebudget

Die Werte „alle Varianten“ enthalten bewusst sämtliche austauschbaren Rüstungen und Waffen in einer Quelldatei. Im Spiel wird jeweils nur eine gültige Konfiguration angezeigt.

| Detailstufe | Alle Varianten | Aktive Stoff-Konfiguration | Aktive Kupfer-Konfiguration | Aktive Eisen-Konfiguration |
|---|---:|---:|---:|---:|
| LOD0 | 159.620 Tris | 25.368 Tris | 73.316 Tris | 73.316 Tris |
| LOD1 | 116.450 Tris | 18.534 Tris | 53.582 Tris | 53.582 Tris |
| LOD2 | 49.534 Tris | 10.186 Tris | 23.638 Tris | 23.638 Tris |

## Prüfung

- Blender-/GLB-Roundtrip: PASS für LOD0, LOD1, LOD2 und die kombinierte Datei
- ein Rig mit 20 Bones: PASS
- exakt 25 Clipnamen: PASS
- alle 39 semantischen Module vorhanden: PASS
- alle Spielflächen geriggt und vollständig gewichtet: PASS
- Werkzeuge fest am `staff`-Bone; Dolche ausschließlich an `dagL`/`dagR`: PASS
- Sprungprüfung der kritischen Werkzeugrotationen: PASS
- Vorder-, Rückseiten-, LOD- und gemischte Rüstungssichtprüfung: PASS
- Kupferrückseite bleibt kupferfarben; blaue Rautensteppung bleibt sichtbar: PASS
- Stoff-V2 mit 44 geriggten Einzelteilen, eingelassenen Knöpfen und gegliederten Beinen: PASS
- Waffen-/Körper-Schnittprüfung über 24 bewaffnete Clips und 946 Animationsframes: PASS, 0 Kernkörper-Durchdringungen
- Unity-Import mit 159.620 / 116.450 / 49.534 Tris und 34 Laufzeitmaterialien: PASS
- aktive LODGroup mit drei Stufen und vollständiger Modulschaltung: PASS
- Prefab-GUIDs für `Player_3D` und `Player` unverändert: PASS
- 30 gezielte EditMode-Produktionsprüfungen: PASS
- 3 PlayMode-Prüfungen für Rüstungswechsel, Angriff und Abbau: PASS
- Unity-Kameraaufnahmen für Kupferrüstung, Spitzhacke, Dolche und Speer: PASS

Die Revision umfasst die sechs Aktionsclips sowie alle 18 bewaffneten Ruhe-, Geh- und Laufclips. Der automatisierte Bericht steht unter `AnimationAudit/WANDERER_WEAPON_BODY_INTERSECTION_REPORT_V3.json`. Die Unity-Aufnahmen des tatsächlich aktiven Prefabs liegen unter `UnityAudit/`.

Maschinenlesbare Berichte:

- `WANDERER_COMPLETE_HIGHDETAIL_V1_TECHNICAL_REPORT.json`
- `ClothV2/WANDERER_STOFF_V2_TECHNICAL_REPORT.json`
- `WANDERER_COMPLETE_PRODUCTION_LOD_REPORT.json`
- `AnimationAudit/WANDERER_WEAPON_BODY_INTERSECTION_REPORT_V3.json`
- `WANDERER_UNITY_INTEGRATION_REPORT.json`
- `UnityAudit/*.png`

## Produktionsdateien

- Blenderquelle: `Assets/_Game/Art/MidPoly/GoldenMasters/Wanderer/Candidate/ReferenceUpgradeComplete/Source~/CHR_Wanderer_CompleteReference_HighDetail_Production.blend`
- kombinierte Laufzeitdatei: `Assets/_Game/Art/MidPoly/GoldenMasters/Wanderer/Candidate/ReferenceUpgradeComplete/Runtime/CHR_Wanderer_CompleteReference_HighDetail_Production.glb`
- einzelne Laufzeit-LODs: im selben `Runtime`-Ordner
- Manifest: `Assets/_Game/Art/MidPoly/GoldenMasters/Wanderer/Candidate/ReferenceUpgradeComplete/WANDERER_COMPLETE_REFERENCE_PRODUCTION_MANIFEST.json`

## Noch offen

Der Wanderer-Referenzumbau selbst ist abgeschlossen und aktiv. Im übergeordneten Grafikupgrade bleiben lediglich die projektweiten Gesamtregressionen und weitere noch nicht umgestellte Assetgruppen außerhalb des Wanderers zu bearbeiten. Der bisherige Wanderer bleibt als `Player_3D_Legacy.prefab` rückfallfähig erhalten.
