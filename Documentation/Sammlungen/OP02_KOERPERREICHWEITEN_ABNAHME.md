# OP-02 – Abnahme der Körperreichweiten

**Stand:** 25.08.2026  
**Ergebnis:** **PASS**  
**Erledigt zugleich:** OP-03 (einheitliche Interaktionsreichweiten) und OP-07
(gefühlte Waffenreichweite bei großen Zielen)

## Verbindliche Regel

- Interaktionen sind bis **1,5 m horizontaler Abstand von der physischen
  Zieloberfläche** erlaubt.
- Als Oberfläche gilt zuerst der nächste nicht auslösende Collider, danach die
  Renderergrenze, danach ein Trigger und nur ohne Geometrie der
  Transform-Ursprung.
- Ein Kontaktband bis **0,55 m** beeinflusst ausschließlich die Zielauswahl:
  Das berührte Objekt schlägt einen nur rechnerisch bevorzugten Nachbarn. Die
  zulässige Interaktionsdistanz bleibt 1,5 m.
- Kampfreichweiten behalten ihre bestehenden Waffenwerte, messen diese aber
  bis zum Gegnerkörper statt zu dessen Mittelpunkt.

## Umgestellte Systeme

- Interaktionszielwahl, Haltesitzung und Reichweitenabbruch;
- Lager- und Werkbankfenster;
- Gebäude, Acker, Ressourcen, Welt- und Schmiedekisten, Weltbeute,
  Gegnerbeute, Eidra-Fangziele und Portale;
- Nahkampf-Hitbox, Kampfzielsuche, Zielhilfe und Speer-Distanzfenster;
- produktive Prefabs, Ressourcendaten und die betroffenen Content-Builder.

Die Collider-/Renderer-Suche verwendet zurückgesetzte, wiederverwendete
Puffer. Damit erzeugt die häufig aufgerufene Körpermessung nicht pro Abfrage
neue Listenobjekte und bleibt mit deaktiviertem Domain Reload korrekt.

## Prüfbelege

- vollständiger EditMode-Lauf: **1.379/1.379 bestanden**, 0 fehlgeschlagen;
- vollständiger PlayMode-Lauf: **148 bestanden**, 0 fehlgeschlagen,
  3 bewusst übersprungen (151 gesamt);
- gezielte Nachprüfung: Körperdistanz am Collider, 1,5-m-Grenze,
  Kontaktpriorität, großer Gegner im Treffersektor und Kampfzielsuche über die
  Körperoberfläche bestanden;
- Architekturwächter für zurückgesetzten veränderlichen statischen Zustand:
  bestanden;
- Unity-Konsole nach Kompilierung: 0 Fehler.

## Release-Artefakt

- Paket: `Releases/Eidren-v0.3.2-windows-x64.zip`
- Größe: **293.913.247 Byte**
- SHA-256:
  `55d9f51c0439b0fa135db739938c0f14921cb13f7e0f197844dea9f659bb38c8`
- ZIP, Manifest und Prüfsummendatei stimmen überein: 101/101 Dateien,
  0 Größenabweichungen, 0 unerwünschte Debug-/Logeinträge.
- 15-Sekunden-Player-Smoke mit Direct3D 11 bestanden; keine Ausnahme und kein
  Fatalfehler. Beleg:
  `smoke-v032-midpoly-op02-20260825-161031.log`.
