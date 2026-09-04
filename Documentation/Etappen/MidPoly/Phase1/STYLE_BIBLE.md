# Eidren Mid-Poly Style Bible – Golden-Master-Freigabe

**Version:** 1.0  
**Stand:** 20.08.2026  
**Verbindliche Referenzen:** Wildling, Wanderer, Werkbank

## Formensprache

- Silhouetten werden zuerst über große, klar lesbare Massen gebaut; Kleinteile dürfen die Fernlesbarkeit nicht ersetzen.
- Organische und technische Formen bleiben sichtbar facettiert. Große Rundungen benötigen bewusst gesetzte Ebenen, Richtungswechsel und asymmetrische Brüche.
- Kanten dürfen schmal gefast sein, aber nicht wie durchgehend weich geschliffenes Spielzeug wirken. Breite Rundbevels sind zu vermeiden.
- Konstruktion muss glaubwürdig bleiben: Lasten werden über Beine, Rahmen, Gelenke und Beschläge geführt; Rüstung folgt Körper und Bewegungsachsen.
- Pro Asset sind ein bis drei charakteristische Akzente erlaubt. Wiederholte Kleindetails bleiben untergeordnet.

## Oberflächen und Materialien

- Stilisiertes PBR ist Pflicht. Farbe allein darf Holz, Leder, Stoff, Stein und Metall nicht unterscheiden; Roughness, Metallizität, Kantenbild und Konstruktion tragen die Materiallesbarkeit mit.
- Paletten-UVs oder gebackene Texturen ersetzen produktiv instabile Vertex-Farbstreams.
- Kupfer: warme dunkle Grundfläche, hellere belastete Kanten, sparsame Patina in Vertiefungen.
- Holz: gerichtete Planken-/Faserwirkung, matte Fläche, sichtbare Verbindungspunkte.
- Stoff/Leder: weicheres Lichtbild als Metall, aber weiterhin kantige Falten- und Plattenführung.
- Emission bleibt kleinflächig und gameplayrelevant; sie darf keine Silhouette überstrahlen.
- Zielbudgets: Charaktere höchstens fünf produktive Materialfamilien pro Golden-Master-Ausprägung, Standardkreaturen höchstens vier, komplexe Stationen höchstens fünf.

## Geometriedichte und LOD

- Detail wird dort konzentriert, wo Silhouette, Gesicht, Hände, Gelenke, Werkzeuge oder Interaktionsfunktion gelesen werden.
- LOD0 folgt den Budgets aus `AUFTRAG_MIDPOLY_GESAMTUMSTELLUNG.md`.
- LOD1 zielt auf etwa 45–60 % von LOD0, LOD2 auf etwa 15–25 %.
- Alle LODs behalten Pivot, Rig, Modulnamen und sichtbare Hauptfunktion. LOD-Wechsel dürfen keine Ausrüstungszustände reaktivieren.
- Freigegebene Referenzen: Wildling 32.594/18.520/7.408, Wanderer nach Phase 2 37.018/20.919/9.105, Werkbank 18.876/9.438/3.764 Tris.

## Figuren, Rig und Animation

- Bestehende Bone-, Clip-, Root-Motion-, Timing- und Gameplayevent-Verträge bleiben erhalten.
- Gesichter erhalten erkennbare Augen-, Brauen-, Nasen-, Mund- und Kieferformen; kugelige Köpfe ohne Ebenen sind nicht zulässig.
- Hände greifen Werkzeuge an definiertem Haupt- und Nebengriff. Zweihandwerkzeuge werden in Kontakt- und Ausholphase geprüft.
- Rüstung bleibt slotweise modular. Ein Slot schaltet alle seine LOD-Renderer gemeinsam.
- Skinweights müssen endlich und pro Vertex normalisiert sein. Gelenke werden in Extremposes auf Durchdringung und Volumenverlust geprüft.

## Gameplay-Lesbarkeit und Integration

- Gameplaymaße, Collider, Trigger, NavMesh-Hindernisse, Interaktionsseite und Datenreferenzen werden nicht aus optischen Gründen verändert.
- Produktive Prefabpfade und GUIDs bleiben erhalten. Vor einem Golden-Master-Austausch wird ein Legacy-Fallback gesichert oder der alte GLB-Pfad bleibt über die Registry verfügbar.
- Ressourcen behalten Active/Exhausted, Stationen ihre Bedienseite, Figuren ihre Ausrüstungs- und Waffenlogik.
- Kontaktschatten unterstützen die Bodenhaftung, ersetzen aber weder korrekte Pivots noch echte Lichtschatten.

## Licht und Abnahme

- Reviews verwenden eine warme Hauptlichtquelle, kühles Füll-/Kantenlicht und eine dunkle neutrale Umgebung. Das Modell muss auch unter produktiver URP-Beleuchtung lesbar bleiben.
- Jede Freigabe umfasst Blenderquelle, GLB-Roundtrip, LODs, Materialzählung, Bounds/Pivot, Collider oder Rig, Animationen, Unity-Prefab und Regressionstests.
- Ein Asset gilt erst als produktionsbereit, wenn Importprobleme und Produktionsblocker im maschinenlesbaren Audit leer sind.
- Form, Material, Lichtreaktion, Dichte und Gameplay-Lesbarkeit müssen zu allen drei Golden Masters passen; rein runde Low-Poly- oder überglättete Kunststoffoptik wird abgelehnt.
