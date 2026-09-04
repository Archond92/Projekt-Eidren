# MIDPOLY-000 – Phase 2 abgeschlossen

**Stand:** 20.08.2026  
**Status:** abgeschlossen und produktiv integriert  
**Gegenstand:** Wanderer und vollständiges Ausrüstungssystem

## Ergebnis

Der Wanderer wurde auf den freigegebenen kantigen Mid-Poly-Stil umgestellt. Basiskörper, Gesicht und Haare der Phase-1-Referenz bleiben erhalten. Die noch provisorischen Stoff- und Eisenmodule sowie sämtliche Waffen- und Werkzeugfamilien wurden durch eigenständige, facettierte Mid-Poly-Geometrie ersetzt.

Die Integration verwendet weiterhin die bestehenden produktiven Prefabpfade und GUIDs. Der CharacterController, Gameplay-Sockets, Skinweights, alle 20 Bones und sämtliche 25 verbindlichen Clips bleiben erhalten. Der alte Wanderer liegt weiterhin als deaktivierbarer Legacy-Fallback vor.

## Lieferumfang

- 12 Rüstungsmodule: Helm, Harnisch, Hände und Beine jeweils in Stoff, Kupfer und Eisen
- 20 konkrete Waffen-/Werkzeugmodule:
  - Hammer: Basis, Kupfer, Eisen, Sealbreaker
  - Dolche: Basis, Kupfer, Eisen, Ash Fangs
  - Speer: Kupfer, Eisen, Ember Thorn
  - Axt: Basis, Kupfer, Eisen
  - Spitzhacke: Basis, Kupfer, Eisen
  - Sense: Basis, Kupfer, Eisen
- 6 generische Waffen-/Werkzeugmodule als kompatible Fallbacks
- Basiskörper und Rüstung ergeben zusammen mit den Waffen insgesamt 39 semantisch schaltbare Module
- 5 Paletten-PBR-Materialien: Gesicht/Organik, Stoff, Leder/Holz, Kupfer und Eisen
- LOD0/LOD1/LOD2 mit 37.018 / 20.919 / 9.105 Tris
- 20 Bones und exakt 25 Clips

## Schalt- und Gameplayvertrag

Das Präsentationssystem wählt nun anhand der konkreten Item-ID das passende Mid-Poly-Modul. Basis-, Kupfer-, Eisen- und benannte Spezialvarianten werden exklusiv geschaltet. Nicht zugeordnete oder ältere Aufrufer bleiben über die generischen Module kompatibel.

Geprüft wurden:

- alle 256 gültigen Kombinationen der vier Rüstungsslots
- alle 20 konkreten Item-Varianten auf exklusive Sichtbarkeit
- Anlegen, Ablegen, Wechseln, Zerbrechen und Death-Bag-/Drop-Semantik
- Basis-Spitzhacke im echten Kupferader-Abbauablauf inklusive Ein- und Ausblenden
- alle drei LOD-Stufen mit identischer Modulsemantik
- sämtliche 25 Clipnamen, Bones, Materialien, Bounds, Bodenkontakt und Controllermaße
- Erhalt der produktiven Prefab-GUIDs

## Verifikation

- Blender-GLB-Roundtrip: PASS für LOD0, LOD1 und LOD2
- Unity-Migrationsbericht: PASS, 39 Module, 3 LODs, keine Fehler
- fokussierter Produktions-/Präsentationsvertrag: 101/101 PASS
- Wanderer-/Player-/Bodenkontakt-Regression: 267/267 PASS
- Ausrüstung, Waffenwechsel und Death Bag: 72/72 PASS
- Player-/Interaktions-PlayMode: 12/12 PASS
- Golden-Master-Gesamtaudit: `allImported=true`, `allProductionReady=true`

Der maschinenlesbare technische Befund steht in `WANDERER_TECHNICAL_REPORT.json`. Die aktuelle Blender-Quelldatei liegt unter `Assets/_Game/Art/MidPoly/GoldenMasters/Wanderer/Source~/CHR_Wanderer_Mid_Phase2_Production.blend`; die drei Runtime-LODs liegen im benachbarten `Runtime`-Ordner.

## Visuelle Abnahmebilder

- `WANDERER_STOFF_ASHFANGS.png`
- `WANDERER_EISEN_HAMMER.png`
- `WANDERER_MIXED_EMBERTHORN.png`
- `WANDERER_KUPFER_PICKAXE_REFERENCE.png`

## Nächster Auftragsschritt

Phase 3 beginnt mit den Kreaturen und Bossen. Der bereits freigegebene Wildling dient dabei als Standardkreatur-Referenz; anschließend folgen die geforderten Golden Master für vierbeinige/gedrungene, gerüstete und Fernkampf-Kreaturen.

## Nachgelagertes High-Detail-Referenzupgrade

Am 23.08.2026 wurde zusätzlich ein vollständiger High-Detail-Produktionskandidat für den Wanderer erstellt. Er ersetzt den hier dokumentierten produktiven Stand noch nicht. Umfang, Prüfungen und der ausstehende Unity-6000.3-Schritt stehen unter `ReferenceUpgrade/CompleteWanderer/WANDERER_COMPLETE_REFERENCE_STATUS.md`.
