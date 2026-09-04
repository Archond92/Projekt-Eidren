# MIDPOLY-000 – Abschlussbericht Phase 7

**Stand:** 25.08.2026  
**Status:** abgeschlossen (PASS)

## Ergebnis

- 110 Produktionsprefabs und 12 Szenen im finalen Asset-Audit geprüft;
- 150 von 150 Einträgen der Migrationsmatrix `abgenommen`;
- 19 technische Teilberichte PASS;
- 16 Figuren-, 16 Umgebungs- und 18 Ressourcen-Zustandsassets im Familienaudit PASS;
- 0 Legacy-Assets und 0 produktive Legacy-Abhängigkeiten im Unity-Runtime-Baum;
- 1.375 von 1.375 EditMode-Tests bestanden, 0 fehlgeschlagen, 0 übersprungen;
- 151 PlayMode-Tests ausgeführt: 148 bestanden, 0 fehlgeschlagen, 3 bewusst übersprungen (reine Diagnose-/Capture-Helfer);
- Windows-x64-Entwicklungsbuild 0.3.2 erfolgreich gebaut, Archiv und Manifest gegengeprüft und Player-Start auf D3D11 erfolgreich;
- Unity-Konsole nach dem finalen Reload ohne Fehler.

## Behobene Abschlussabweichungen

- Werkbank-Geometrie auf exakt 1,0 × 1,1 × 1,0 m normalisiert, ohne GUID-, Collider-, NavMesh- oder Interaktionsvertrag zu verändern;
- erschöpfte Kupferader vollständig auf Wirtsfelsmaterial neutralisiert;
- historische 2D-, Low-Poly- und Runtime-Fallback-Tests auf den freigegebenen Mid-Poly-/Archivvertrag nachgezogen;
- doppelte Unity-Serialisierungsfelder in `WildlingController` und `EidraWildController` beseitigt;
- Wanderer-Slotprüfung auf die reale GLB-Struktur aus leeren Schaltknoten und zugeordneten `LOD0_<Slot>_*`-Skinned-Meshes ausgerichtet;
- zentraler Archivwächter in allen betroffenen Migrationsbuildern ergänzt.
- veraltete Terrock-/Noctarion-PlayMode-Prüfungen auf die kanonischen 3D-Prefabs und deren vollständige Mid-Poly-Clipzustände umgestellt;
- Sichtverdeckungsprüfung auf den shaderunabhängigen Mid-Poly-Fade-Vertrag (`(Occlusion Fade)`, `_BaseColor`/`_Color`) aktualisiert.

## PlayMode- und Build-Nachweis

Der vollständige PlayMode-Lauf dauerte 133,37 Sekunden. Von 151 Tests bestanden 148; es gab 0 Fehler. Die drei übersprungenen Tests sind zwei explizite Diagnose-Logger sowie ein manueller PNG-Capture, die laut Testdefinition keine Regel prüfen. Ein gezielter Vorlauf der vier zuvor abweichenden Präsentations-/Occlusion-Tests bestand zusätzlich 4 von 4.

Der vorgesehene Entwicklungsbuilder erzeugte mit Unity 6000.3.0f1 einen Windows-x64-/D3D11-Player aus allen 12 produktiven Szenen. Das ZIP enthält 101 Einträge und ist 293.916.651 Bytes groß. Datei-Hash, Manifest-Hash und Manifest-Größe stimmen überein:

`7273bea133cfcaa83fcf24b4c278255d2af9ca8020c106ae1de696f9fe6090d9`

Der gebaute Player wurde anschließend 15 Sekunden gestartet. Er initialisierte Direct3D 11, Assemblies, PhysX und den eingebauten Occlusion-Fade-Zwilling ohne Exception oder Fehler. Die Unity-Editor-Konsole enthielt nach Abschluss 0 Fehler.

## Legacy-Archiv

57 unreferenzierte Altassets wurden zusammen mit ihren 57 `.meta`-Dateien aus `Assets` verschoben. Ein während der Abschlussprüfung einmalig neu erzeugter Werkbank-Fallback wurde zusätzlich abgefangen. Der vollständige, wiederherstellbare Bestand umfasst damit 58 Assetdateien und 58 `.meta`-Dateien. Es wurde nichts gelöscht.

## Performance-Nachweis

Der deterministische Worst-Case-Besatz umfasst 131 Instanzen, 2.694 erzwungene LOD0-Renderer, 1.017.286 LOD0-Dreiecke, 205 eindeutige Materialien, 146 Skinned Meshes und 2.871 Bone-Referenzen. Geometrie-, Renderer-, Material- und Speicherbudgets sind PASS.

Die kostenlose Unity-MCP-Instanz läuft headless. GPU-Frametime und Game-View-Draw-Calls stehen deshalb technisch nicht zur Verfügung und sind im Performancebericht ausdrücklich als nicht ausgewertet markiert; sie wurden nicht fälschlich als Nullmessung gewertet.

## Belege

- `MIDPOLY_FINAL_ASSET_REPORT.json` – finaler Asset- und Berichtsaudit;
- `../MidPoly_Phase6/PERFORMANCE_REPORT.json` – Worst-Case- und Speicherbudgets;
- `../MidPoly_Phase6/MIDPOLY_WORST_CASE.png` – Sichtnachweis des Profilerbesatzes;
- `../MidPoly_LegacyArchive/README.md` – Umfang und Wiederherstellung des Archivs.
- `PLAYMODE_BUILD_VALIDATION.md` – Testnamen, Skip-Begründungen, Build-, Hash- und Startnachweis.
