# Mid-Poly Phase 6 – Integration und Profiling

Status: abgeschlossen (PASS fuer alle belastbar messbaren Budgets)

- Deterministischer Worst-Case-Besatz: 131 Instanzen, 2.694 LOD0-Renderer, 1.017.286 LOD0-Dreiecke, 205 eindeutige Materialien.
- Geometrie-, Renderer-, Material-, Grafik-, Mesh- und Texturspeicher-Guardrails: bestanden.
- Alle 39 Fallback-Prefabs sowie die 18 alten Figurenquellen: 0 produktive GUID-Referenzen.
- Die kostenlose Unity-MCP-Instanz ist headless. GPU-Frametime und Game-View-Draw-Calls werden von Unity deshalb nicht geliefert und sind im JSON ausdrücklich als nicht ausgewertet markiert.
- Der Profilerbesatz wird nur temporär aufgebaut und anschließend durch erneutes Laden der aktiven Szene rückstandsfrei entfernt.

Belege: `WORST_CASE_HARNESS.json`, `PERFORMANCE_REPORT.json`, `MIDPOLY_WORST_CASE.png`.
