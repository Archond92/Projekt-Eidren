"""Structural gate for the Mid-Poly Wanderer runtime GLB."""

from __future__ import annotations

import json
import struct
import sys
from pathlib import Path


EXPECTED_NODES = {
    "Basis",
    "Haare",
    "Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
    "Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
    "Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
    "Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
    "Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer",
    "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense",
}

EXPECTED_ANIMATIONS = {
    "Abbau_Axt", "Abbau_Sense", "Abbau_Spitzhacke",
    "Angriff_Dolche", "Angriff_Hammer", "Angriff_Speer", "Oeffnen",
    *(f"{motion}_{stance}" for motion in ("Ruhe", "Gehen", "Laufen")
      for stance in ("Axt", "Sense", "Spitzhacke", "Dolche", "Hammer", "Speer", "Ohne")),
}


def fail(message: str) -> None:
    raise RuntimeError(message)


def read_glb(path: Path) -> dict:
    raw = path.read_bytes()
    if len(raw) < 20 or raw[:4] != b"glTF":
        fail(f"Keine gueltige GLB: {path}")
    magic, version, total_length = struct.unpack_from("<III", raw, 0)
    if magic != 0x46546C67 or version != 2 or total_length != len(raw):
        fail("GLB-Header ist inkonsistent")
    offset = 12
    document = None
    while offset < len(raw):
        chunk_length, chunk_type = struct.unpack_from("<II", raw, offset)
        chunk = raw[offset + 8: offset + 8 + chunk_length]
        if chunk_type == 0x4E4F534A:
            document = json.loads(chunk.decode("utf-8").rstrip(" \t\r\n\0"))
        offset += 8 + chunk_length
    if document is None:
        fail("JSON-Chunk fehlt")
    return document


def verify(path: Path) -> dict:
    gltf = read_glb(path)
    node_names = {node.get("name") for node in gltf.get("nodes", [])}
    missing_nodes = sorted(EXPECTED_NODES - node_names)
    if missing_nodes:
        fail("Semantische Knoten fehlen: " + ", ".join(missing_nodes))

    animation_names = [animation.get("name") for animation in gltf.get("animations", [])]
    missing_animations = sorted(EXPECTED_ANIMATIONS - set(animation_names))
    unexpected_animations = sorted(set(animation_names) - EXPECTED_ANIMATIONS)
    if missing_animations or unexpected_animations or len(animation_names) != len(EXPECTED_ANIMATIONS):
        fail(
            f"Animationssatz falsch; fehlt={missing_animations}, unerwartet={unexpected_animations}, "
            f"Anzahl={len(animation_names)}"
        )

    accessors = gltf.get("accessors", [])
    primitives = [primitive for mesh in gltf.get("meshes", []) for primitive in mesh.get("primitives", [])]
    missing_colour = [index for index, primitive in enumerate(primitives) if "COLOR_0" not in primitive.get("attributes", {})]
    if missing_colour:
        fail(f"COLOR_0 fehlt an {len(missing_colour)} von {len(primitives)} Primitives")
    unskinned_nodes = [
        node.get("name") for node in gltf.get("nodes", [])
        if "mesh" in node and "skin" not in node
    ]
    if unskinned_nodes:
        fail("Ungeskinnte Runtime-Meshes: " + ", ".join(str(name) for name in unskinned_nodes))
    skins = gltf.get("skins", [])
    if len(skins) != 1 or len(skins[0].get("joints", [])) != 20:
        fail(f"Ein Skin mit 20 Knochen erwartet, erhalten: {[len(s.get('joints', [])) for s in skins]}")

    triangles = 0
    for primitive in primitives:
        if "indices" in primitive:
            triangles += accessors[primitive["indices"]]["count"] // 3
        else:
            triangles += accessors[primitive["attributes"]["POSITION"]]["count"] // 3
    if triangles <= 6506:
        fail(f"Mid-Poly muss detaillierter als der archivierte Low-Poly-Wanderer sein: {triangles} Dreiecke")

    # A structurally present clip can still be accidentally baked as one constant
    # pose. Blender's optimizer collapses such output channels to two boundary
    # samples, while these authored/baked clips contain intermediate samples.
    static_animations = []
    for animation in gltf.get("animations", []):
        animated = any(accessors[sampler["output"]]["count"] > 2
                       for sampler in animation.get("samplers", []))
        if not animated:
            static_animations.append(animation.get("name"))
    if static_animations:
        fail("Konstant gebackene Animationen: " + ", ".join(static_animations))

    return {
        "file": str(path.resolve()),
        "bytes": path.stat().st_size,
        "nodes": len(gltf.get("nodes", [])),
        "meshes": len(gltf.get("meshes", [])),
        "primitives": len(primitives),
        "triangles": triangles,
        "skins": len(skins),
        "bones": len(skins[0]["joints"]),
        "animations": animation_names,
    }


if __name__ == "__main__":
    if len(sys.argv) != 2:
        fail("Aufruf: python Tools/verify_wanderer_runtime_glb.py <Wanderer.glb>")
    print(json.dumps(verify(Path(sys.argv[1])), indent=2, ensure_ascii=False))
