# MIDPOLY-000 – Phase 1 Arbeitsstand

**Stand:** 20.08.2026  
**Phase 0:** abgeschlossen  
**Phase 1:** abgeschlossen, alle drei Golden Masters produktionsfreigegeben  
**Freigegebener Ressourcenpilot:** integriert

> **Fortschreibung vom 20.08.2026:** Phase 2 ist abgeschlossen. Die folgenden Wanderer-Daten wurden auf den aktuellen vollständigen Ausrüstungsstand aktualisiert; der Detailbericht liegt unter `../MidPoly_Phase2/PHASE2_STATUS.md`.

## Produktiv integriert

Die vom Nutzer visuell freigegebenen und per GLB-Roundtrip geprüften V2-Modelle wurden als vorgezogene Pilotfamilie integriert:

- Tree
- StoneDeposit
- CopperVein
- BerryBush
- FiberPlant

Je Ressource sind Active und Exhausted am bestehenden Prefabpfad verdrahtet. Active besitzt LOD0/LOD1/LOD2. Die importierten Clips `Idle_Sway` und/oder `Harvest_Recoil` werden über `MidpolyResourceAnimationDriver` abgespielt. Alle bestehenden Node-Prefab-GUIDs, Collider, Interaktionswerte und Datenreferenzen bleiben erhalten.

Der T1-Builder verwendet bei aktivierter `MIDPOLY_MIGRATION_REGISTRY.json` die freigegebenen Runtime-GLBs. Fehlen Freigabe oder Runtime-Dateien, bleibt der alte prozedurale Builder als Fallback verfügbar. Dadurch kann ein Content-Rebuild die Mid-Poly-Prefabs nicht stillschweigend überschreiben.

Zusätzlich wurden zwölf echte Gebietsvarianten für Greenwood, Marsh, Quarry und EmberRuins aus den neuen Basisvisuals neu gebacken. Die Kupferader in EmberRuins referenziert vertragsgemäß direkt das Basisvisual.

## Golden-Master-Eingangsaudit

Wildling, Wanderer und Workbench liegen unter `Assets/_Game/Art/MidPoly/GoldenMasters` und sind produktiv integriert.

- **Wildling:** produktionsbereit und am bestehenden `Wildling_3D.prefab`-GUID integriert. LOD0/1/2 besitzen 32.594/18.520/7.408 Tris. Vier PBR-Materialien verwenden eine kleine Paletten-UV statt problematischer Vertex-Streams. Alle 17 Bones und acht Clips bleiben exakt erhalten. Bounds (1,043 × 1,894 × 1,025 m), bestehender CapsuleCollider (Radius 0,52 m, Höhe 2,10 m), Skinweights, Root-Motion-Grenzen und Bodenkontakt sind validiert. Der alte GLB-Pfad bleibt als deaktivierbarer Fallback erhalten.
- **Wanderer:** produktionsbereit an den bestehenden `Player_3D.prefab`- und `Player.prefab`-GUIDs integriert. LOD0/1/2 besitzen aktuell 37.018/20.919/9.105 Tris. Basiskörper, alle zwölf Stoff-/Kupfer-/Eisen-Rüstungsteile, 20 konkrete Waffen-/Werkzeugvarianten und sechs generische Fallbacks bleiben über insgesamt 39 semantische Schaltmodule getrennt. Fünf Paletten-PBR-Materialien ersetzen die Testmaterialien und alten Vertex-Farbstreams. Alle 20 Bones, 25 Clips, Skinweights, LOD-übergreifenden Schaltungen und der CharacterController (Höhe 2 m, Radius 0,48 m) sind validiert. Der alte `Player_3D` wurde als Legacy-Fallback gesichert.
- **Workbench:** produktionsbereit am bestehenden `Workbench.prefab`-GUID integriert. LOD0/1/2 besitzen 18.876/9.438/3.764 Tris; die neun Testmaterialien wurden auf fünf PBR-Materialien konsolidiert. Bounds (1,269 × 1,290 × 0,974 m), Bedienseite, Trigger (1 × 1,5 × 1 m), NavMeshObstacle, Controller, Kontaktschatten und fünf semantische Funktionsanker sind validiert. Vor dem Umbau wurde `Fallback/Workbench_Legacy.prefab` gesichert.

Der maschinenlesbare Befund steht in `GOLDEN_MASTER_CANDIDATE_REPORT.json`.

## Verifikation

- Ressourcenintegrationsreport: PASS, 5/5 Familien und 12/12 Gebietsvarianten
- Mid-Poly-Pilot-EditMode: 2/2 PASS
- VisualAssetTests: 11/11 PASS
- VisualScaleTests: 8/8 PASS
- InteractionIntegrationTests PlayMode: 9/9 PASS
- AreaArtTests: 6/6 PASS
- GroundContactTests: 160/160 PASS
- Wildling-Produktionsvertrag: 3/3 PASS
- CreatureMeshPresentationTests: 28/28 PASS
- WildlingDefinitionTests: 6/6 PASS
- Wildling-relevante Gesamtsuite: 197/197 PASS
- Golden-Master-Audit: Wildling `importPass=true`, `productionReady=true`
- Werkbank-Produktionsvertrag: 3/3 PASS
- Werkbank-Regression (Visuals, Bauphysik, Icons, Produktionscontent, Bodenkontakt): 213/213 PASS
- Golden-Master-Audit: Werkbank `importPass=true`, `productionReady=true`
- Wanderer-Produktions-/Präsentationsvertrag inklusive 256 Rüstungskombinationen und 20 Itemvarianten: 101/101 PASS
- Wanderer-Regression (PlayerPrefab, Asset, Präsentation, Bodenkontakt): 267/267 PASS
- Ausrüstung, Waffenwechsel und Death Bag: 72/72 PASS
- Player-/Interaktions-PlayMode: 12/12 PASS
- glTFast-Wanderer-Import: 0 Vertex-Stream-Warnungen
- Golden-Master-Audit: Wanderer `importPass=true`, `productionReady=true`
- Golden-Master-Gesamtaudit: `allImported=true`, `allProductionReady=true`

## Abschluss und nächster Schritt

Phase 1 bleibt mit drei technisch und produktiv freigegebenen Golden Masters sowie der verbindlichen `STYLE_BIBLE.md` abgeschlossen. Phase 2 hat den Wanderer inzwischen vollständig auf Mid-Poly-Rüstung und konkrete Waffen-/Werkzeugvarianten umgestellt. Als Nächstes beginnt Phase 3 mit Kreaturen und Bossen.
