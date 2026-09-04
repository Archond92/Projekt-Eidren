---
name: eidren-release
description: Eidren-Version veröffentlichen — Exe bauen, GitHub-Release mit Assets, Discord-Posts, Aufräumen. Verwenden, wenn eine Version fertig ist und "wie gehabt" veröffentlicht werden soll.
---

# Eidren-Release veröffentlichen

Verifizierter Ablauf (zuletzt v0.3.1, 18.08.2026). Reihenfolge einhalten:
Build zuerst, denn Release-Notes und README brauchen die finalen Zahlen
(Bytes, SHA-256) aus dem Build.

## 0. Voraussetzungen prüfen

- Volle Suiten grün (EditMode + PlayMode) — Stand gehört in die Notes.
- `git status` sauber genug; Repo trackt NUR `.gitignore`, `README.md`,
  `RELEASE_NOTES_*.md`, `TESTING_NOTICE.md` (CHANGELOG.md bleibt bewusst
  lokal/ungetrackt).

## 1. Version setzen

- `Assets/_Game/Editor/WindowsReleaseBuilder.cs`: Konstanten
  `DevelopmentVersion` und `DevelopmentArtifactName` auf die neue Version
  (z. B. `"0.3.2"` / `"Eidren-v0.3.2-windows-x64"`). `BuildDevelopment()`
  nutzt die Konstanten.
- `CHANGELOG.md`: Abschnitt „Unveröffentlicht" in „## X.Y.Z – JJJJ-MM-TT"
  umbenennen + kurzen Einordnungssatz. Prüfen, ob seit dem letzten Release
  entstandene FEATURES überhaupt im CHANGELOG stehen (v0.3.1-Lektion: die
  Minimap fehlte komplett, weil sie nach dem Release-Schnitt entstand).

## 2. Exe bauen (detached!)

VORHER laufende Spielinstanzen beenden — eine offene Staging-Exe sperrt
`Eidren.exe` und der Builder bricht mit UnauthorizedAccessException ab:

```powershell
Get-Process -Name "Eidren" -ErrorAction SilentlyContinue | Stop-Process -Force -Confirm:$false
```

Der Build dauert länger als das 10-Minuten-Tool-Timeout — Unity DETACHED
starten, sonst killt das Timeout den Build:

```powershell
Start-Process -FilePath "<projekt>\.unity-editor\Editor\Unity.exe" -ArgumentList '-batchmode','-projectPath','"<projekt>"','-executeMethod','Eidren.Editor.WindowsReleaseBuilder.BuildDevelopment','-quit','-logFile','"<projekt>\release-vXYZ-build.log"' -PassThru
```

Daneben eine Bash-Hintergrund-Wache auf das Log (`grep "release created\|
Aborting batchmode\|error CS"` in einer sleep-Schleife). Ergebnis liegt in
`Releases/`: `<Artefakt>.zip`, `.sha256`, `.manifest.json` + entpackte Exe
unter `Releases/Staging/<Artefakt>/`. Der Builder nimmt die Szenen aus den
EditorBuildSettings (EidraForge steht seit F31-009 fest in
`OrderedScenePaths`).

## 3. Smoke-Test

Exe aus `Releases/Staging/<Artefakt>/Eidren.exe` mit ERZWUNGENEM Log
starten: `-logFile <pfad>` — der Release-Build schreibt sonst KEIN Log
(alte Player.log-Dateien täuschen einen grünen Smoke vor!). Prüfen:
1. `[Eidren] Fade-Zwilling geladen` MUSS erscheinen (Shader-Bundling-
   Beweis; die Fehlervariante heißt „Fade-Zwilling fehlt im Build").
2. `Exception|Failed to load` → 0 Treffer.

## 4. Release-Notes + README

- `RELEASE_NOTES_vX.Y.Z.md` neu, Muster der Vorgängerdatei: Titel
  „# Eidren vX.Y.Z – Windows-Testversion", Einordnung, „Neu"/„Behoben",
  „Empfohlener Testpfad", „Abnahme" (Suitenstände), „Technische Daten",
  „Bekannte Einordnung". Zahlen: Zip-Bytes aus dem Build-Log; entpackte
  Größe = Summe der `bytes` im Manifest (`files`-Liste); Dateizahl =
  `len(files)`; SHA aus `.sha256`. Deutsche Dezimalkommas („253,36 MiB").
- `README.md`: Titelzeile, Einordnungsabsatz, Releases-Link
  (`releases/tag/vX.Y.Z`), Zip-Name (2×), SHA-256-Block, Notes-Link.
- **`.gitignore`**: neue Zeile `!RELEASE_NOTES_vX.Y.Z.md` — das Repo
  ignoriert `*` mit namentlichen Ausnahmen; ohne die Zeile scheitert
  `git add`.

## 5. GitHub

```bash
git add .gitignore README.md RELEASE_NOTES_vX.Y.Z.md
git commit -m "release: publish Eidren vX.Y.Z ..."
git tag vX.Y.Z
git push origin <branch> && git push origin vX.Y.Z
gh release create vX.Y.Z --prerelease --title "Eidren vX.Y.Z – Windows-Testversion" --notes-file RELEASE_NOTES_vX.Y.Z.md "Releases/<Artefakt>.zip" "Releases/<Artefakt>.sha256" "Releases/<Artefakt>.manifest.json"
```

Titel-Muster wie die Vorgänger („– Windows-Testversion", Pre-release).

## 6. Discord (Desktop-App per Computersteuerung)

`request_access(["Discord"], clipboardWrite: true)` — bei Ablehnung die
Texte als Datei übergeben statt posten. Je Kanal EINE Nachricht (Einfügen
per Zwischenablage; Shift+Enter für Zeilenumbrüche in einer Nachricht):

| Kanal | Inhalt |
|---|---|
| #ankuendigungen | „🚀 Eidren vX.Y.Z ist da!" + Release-Link (GitHub-Karte lädt selbst) |
| #changelog | Changelog gegliedert, mit Abnahmezeile |
| #builds-downloads | „Aktueller Testbuild" + ZIP-Direktlink (`releases/download/vX.Y.Z/<Artefakt>.zip`), Größen, SHA-256, Entpack-/SmartScreen-Hinweis |
| #bekannte-probleme | „Bekannte Einordnung von vX.Y.Z" — die offenen Punkte |
| #erste-schritte | „Empfohlener vX.Y.Z-Testpfad" aus den Notes |

Danach die stehenden Kanäle GEGENLESEN und nur bei faktischen Fehlern
BEARBEITEN (nicht doppeln): #faq-spielhilfe, #willkommen, #roadmap.

## 7. Aufräumen

In `Releases/` und `Releases/Staging/` alles löschen außer: aktuelles
Zip/.sha256/.manifest, aktueller Staging-Ordner (frisch getestete Exe),
Vorgänger-Zip (+.sha256/.manifest) als lokale Kopie. Ältere Stände sind
als GitHub-Release-Assets gesichert.

## 8. Nachlauf

- Memory `eidren-fixsammlung-*`/Projektstand aktualisieren (Version
  veröffentlicht, Exe-Stand = Code-Stand — die Memory-Regel „Beobachtung
  gegen Exe-Fassung prüfen" bezieht sich ab jetzt auf diese Version).
- Nutzer-Bericht: Release-Link, Zahlen, was gelöscht wurde.
