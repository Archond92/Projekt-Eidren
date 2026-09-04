# MIDPOLY-000 – Phase 5 Arbeitsstand

**Stand:** 25.08.2026  
**Phase:** 5 – Ressourcen, Vegetation und Umgebung  
**Status:** abgeschlossen (PASS)  
**Abgeschlossen:** alle neun Gameplay-Ressourcen sowie das vollständige 16-teilige Umgebungs-/Zonenprop-Kit

## HardwoodTree und SwampHemp – produktiv integriert

Hartholzbaum und Sumpfhanf wurden aus den freigegebenen Tree-/FiberPlant-Konstruktionskits als eigenständige Tier-2-Familie ausgearbeitet. Hartholz besitzt einen breiteren, leicht gedrehten Altstamm, kräftigen Wurzelanlauf und eine dunklere, ausladende Krone. Sumpfhanf ist höher, breiter und leicht geneigt; die hellen Köpfe bleiben auch aus der Spielkamera klar lesbar. Active und Exhausted unterscheiden sich durch volle Silhouette beziehungsweise Stumpf-/Schnittreste.

Produktionsdaten LOD0 / LOD1 / LOD2 / Exhausted:

- HardwoodTree: 7.856 / 4.320 / 1.368 / 1.236 Dreiecke, Zielhöhen 6,650 / 0,840 m;
- SwampHemp: 3.836 / 2.108 / 692 / 1.290 Dreiecke, Zielhöhen 1,372 / 0,228 m;
- `Idle_Sway` und `Harvest_Recoil` sind in beiden Active-LOD0-Modellen erhalten und im Runtime-Treiber verdrahtet;
- drei ImpactPoints je Active-Visual;
- keine Collider in der Grafik-Hierarchie.

Integration und Verträge:

- alle vier Active-/Exhausted-Prefab-GUIDs erhalten;
- ResourceNode, Interaktionstrigger, Hartholz-Blocker, Werkzeuganforderung, Dauer, Ertrag und Zonenbudgets unverändert;
- TwilightGrove-Varianten für Hartholz und Sumpfhanf sowie VeilMarsh-Varianten für Sumpfhanf neu aus den produktiven Visuals geklont;
- `T2ResourceVisualBuilder` schützt jetzt Vegetation sowie Granit/Eisen vor Low-Poly-Rückbau.

Verifikation:

- Blender-5.2-Quellen und acht Runtime-GLBs erzeugt;
- Unity-Import und Technikreport: PASS für beide Assets und alle sechs Zonen-Zustände;
- fokussierter Vegetationsvertrag vor und nach explizitem T2-Builder-Lauf: jeweils 3/3 PASS;
- finale projektweite EditMode-Regression nach Behebung der Werkbank-Altschulden: 1.375/1.375 PASS;
- Unity-D3D11-Sichtprüfung Active/Exhausted: PASS; Abnahmebilder liegen unter `Candidate/VegetationResources`.

## GraniteDeposit und IronVein – produktiv integriert

Granit und Eisen wurden als zweite Mid-Poly-Ressourcenfamilie auf den freigegebenen Stone-/Copper-Konstruktionskits aufgebaut. Granit verwendet eine hellere, breit geschichtete Schieferform mit Mica-Flächen; Eisen eine dunkle Wirtsform mit rostig-metallischen Erzeinschlüssen. Active und Exhausted bleiben über Höhe, Masse und Restform klar unterscheidbar.

Produktionsdaten LOD0 / LOD1 / LOD2 / Exhausted:

- GraniteDeposit: 1.022 / 562 / 223 / 1.114 Dreiecke, Zielhöhen 1,266 / 0,749 m;
- IronVein: 1.042 / 572 / 227 / 1.134 Dreiecke, Zielhöhen 1,408 / 0,749 m;
- `Harvest_Recoil` bleibt auf beiden Active-LOD0-Modellen erhalten;
- drei ImpactPoints je Active-Visual;
- keine Collider in der Grafik-Hierarchie.

Integration und Verträge:

- alle vier Active-/Exhausted-Prefab-GUIDs erhalten;
- ResourceNode, Interaktionstrigger, Blocker-Capsule, Werkzeuganforderung, Dauer, Ertrag und Zonenbudgets unverändert;
- GreyRifts-Varianten für Granit und Eisen sowie VeilMarsh-Varianten für Eisen neu aus den produktiven Visuals geklont;
- `T2ResourceVisualBuilder` schützt Granit und Eisen gemeinsam mit HardwoodTree und SwampHemp vor Low-Poly-Rückbau.

Verifikation:

- Blender-5.2-Quellen und acht Runtime-GLBs erzeugt;
- GLB-Roundtrip: exakte Zielhöhen, Dreieckszahlen, Materialien und `Harvest_Recoil` PASS;
- Unity-Import und technische Migration mit Returncode 0;
- Phase-5-Technikreport: PASS für beide Assets und alle sechs Zonen-Zustände;
- Ressourcen-, Zonen-, Node- und Visualregression: 38/38 PASS;
- expliziter T2-Builder-Rückbauversuch mit anschließendem Produktionsvertrag: 3/3 PASS;
- VisualScale einschließlich Werkbankbreite und -höhe im finalen projektweiten Lauf: PASS;
- Unity-D3D11-Sichtprüfung Active/Exhausted: PASS.

## Umgebungskit – produktiv integriert

Das Phase-5-Umgebungskit umfasst 16 freigegebene Produktionsprefabs: drei Bäume, Busch, Farn, Blumen, Gras, Moos, Leuchtpilze, drei Felsgrößen, zwei Ruinenwände, Ruinenmonument und Eidren-Rune. Alle Assets besitzen LOD0/LOD1/LOD2 mit streng fallenden Dreieckszahlen, zentralisierte `MP_ENV_`-Materialien, erhaltene Prefab-GUIDs und erhaltene Collider-Verträge. Der Unity-Technikreport ist vollständig PASS.

Die Phase-0-Migrationsmatrix ist synchronisiert: 150 von 150 Zeilen stehen auf `abgenommen`.

## Übergabe

Phase 5 ist abgeschlossen und an die ebenfalls abgeschlossene szenenweite Integrations-, Profiling- und Gesamt-QA-Runde aus Phase 6/7 übergeben.
