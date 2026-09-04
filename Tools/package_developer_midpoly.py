"""Paketiert den Entwickler-MidPoly-Build nach dem Muster von v0.3.3.

Aufruf:
  python Tools/package_developer_midpoly.py <version> <editmode> <playmode> <smoke>
  z. B. python Tools/package_developer_midpoly.py 0.3.4 "1357/1357 passed" "149 passed, 0 failed, 3 skipped" "Direct3D 11 start and tester save confirmed"

Liest Releases/Eidren-Developer-MidPoly-windows-x64/, schreibt dort
BUILD_MANIFEST.json, SHA256SUMS.txt und START_HERE.txt, packt alles nach
Releases/Eidren-v<version>-windows-x64.zip und legt .sha256 sowie
.manifest.json daneben.
"""
import datetime
import hashlib
import json
import pathlib
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parent.parent
BUILD = ROOT / "Releases" / "Eidren-Developer-MidPoly-windows-x64"
EXE = "Eidren-Developer-MidPoly.exe"
GLB = ROOT / "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb"


def sha256(path: pathlib.Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def main() -> None:
    version, editmode, playmode, smoke = sys.argv[1:5]
    artifact = f"Eidren-v{version}-windows-x64"
    assert (BUILD / EXE).exists(), f"Build fehlt: {BUILD / EXE}"
    for stale in ("BUILD_MANIFEST.json", "SHA256SUMS.txt", "START_HERE.txt"):
        (BUILD / stale).unlink(missing_ok=True)

    player_files = [p for p in BUILD.rglob("*") if p.is_file()]
    player_bytes = sum(p.stat().st_size for p in player_files)
    key_files = [EXE, "Eidren-Developer-MidPoly_Data/resources.assets",
                 "Eidren-Developer-MidPoly_Data/Managed/Eidren.Gameplay.dll",
                 "Eidren-Developer-MidPoly_Data/Managed/Eidren.Presentation.dll"]
    key_sha = {k: sha256(BUILD / k) for k in key_files if (BUILD / k).exists()}

    build_manifest = {
        "schema": 1,
        "builtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00", "Z"),
        "version": f"{version}-dev-midpoly",
        "productName": "Eidren Developer MidPoly",
        "executable": EXE,
        "target": "Windows x64",
        "unityVersion": "6000.3.0f1",
        "graphicsApi": "Direct3D 11",
        "developmentBuild": True,
        "wandererProductionStyle": "MidPoly",
        "wandererGlbSha256": sha256(GLB),
        "maxLoadout": {"stage": 2, "level": 40, "unlockedTechnologies": 28, "visitedAreas": 8,
                       "armor": "complete iron", "weapons": ["iron_spear", "iron_daggers"],
                       "eidra": ["terrock", "noctarion", "ignivar"], "buildingMaterialsIncluded": True},
        "verification": {"editModeTests": editmode, "playModeTests": playmode, "build": "succeeded",
                         "smokeTest": smoke, "freshFirstStartRestoredAfterSmokeTest": True},
        "playerFilesBeforeDocumentation": len(player_files),
        "playerBytesBeforeDocumentation": player_bytes,
        "sha256": key_sha,
    }
    (BUILD / "BUILD_MANIFEST.json").write_text(json.dumps(build_manifest, indent=2), encoding="utf-8")
    (BUILD / "SHA256SUMS.txt").write_text("".join(f"{v}  {k}\n" for k, v in key_sha.items()), encoding="utf-8")
    (BUILD / "START_HERE.txt").write_text(
        "EIDREN ENTWICKLERSTAND - MID POLY (v" + version + ")\n\n"
        "Start:\n  Eidren-Developer-MidPoly.exe doppelklicken.\n\n"
        "Wichtig:\n  Die EXE, Eidren-Developer-MidPoly_Data, MonoBleedingEdge und die DLL-Dateien\n"
        "  muessen gemeinsam in diesem Ordner bleiben.\n\n"
        "Dieser Stand enthaelt:\n"
        "  - Unity Development Build fuer Windows x64 / Direct3D 11\n"
        "  - die Fixrunde v0.3.4 (Truhen, Fasern, Buesche, Verschieben, Speer, Abbau)\n"
        "  - den Mid-Poly-Wanderer mit neu exportierten Animationen\n"
        "  - Maximallevel 40 und Stufe 2, alle 28 Technologien, alle 8 Gebiete\n"
        "  - Terrock, Noctarion und Ignivar, komplette Eisenruestung\n"
        "  - Eisenspeer und Eisendolche, Materialien fuer alle Gebaeude\n\n"
        "Der Vollausbau-Spielstand wird beim ersten Start automatisch angelegt.\n"
        "Der Produktname ist von der regulaeren Eidren-Version getrennt; regulaere\n"
        "Spielstaende werden dadurch nicht ueberschrieben.\n", encoding="utf-8")

    zip_path = ROOT / "Releases" / f"{artifact}.zip"
    zip_path.unlink(missing_ok=True)
    all_files = sorted(p for p in BUILD.rglob("*") if p.is_file())
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for p in all_files:
            z.write(p, p.relative_to(BUILD).as_posix())
    with zipfile.ZipFile(zip_path) as z:
        assert z.testzip() is None, "Zip defekt"
        names = set(z.namelist())
    matches = names == {p.relative_to(BUILD).as_posix() for p in all_files}
    zip_sha = sha256(zip_path)
    (ROOT / "Releases" / f"{artifact}.sha256").write_text(f"{zip_sha}  {artifact}.zip\n", encoding="utf-8")
    outer = {
        "version": version, "unityVersion": "6000.3.0f1", "platform": "Windows x64", "buildType": "Development",
        "graphicsApi": "Direct3D 11", "artifact": f"{artifact}.zip", "artifactBytes": zip_path.stat().st_size,
        "archiveFiles": len(names), "archiveMatchesBuildDirectory": matches, "sha256": zip_sha,
        "unpackedBytes": sum(p.stat().st_size for p in all_files),
        "wanderer": {"productionStyle": "MidPoly", "bones": 20, "animations": 28, "runtimeGlbSha256": build_manifest["wandererGlbSha256"]},
        "testerSave": {"included": True, "level": 40, "unlockedTechnologies": 28, "visitedAreas": 8, "eidra": 3},
        "verification": {"editMode": editmode, "playMode": playmode, "playerSmoke": smoke},
    }
    (ROOT / "Releases" / f"{artifact}.manifest.json").write_text(json.dumps(outer, indent=2), encoding="utf-8")
    print(json.dumps({k: outer[k] for k in ("artifact", "artifactBytes", "archiveFiles", "archiveMatchesBuildDirectory", "sha256", "unpackedBytes")}, indent=1))


if __name__ == "__main__":
    main()
