"""Build the Phase-5 hardwood and swamp-hemp production files.

The approved Tree and FiberPlant productions provide the animation and LOD
contracts. Geometry, proportions and materials are re-authored per Tier-2
resource before deterministic GLB export.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import bpy


ASSETS = {
    "HardwoodTree": {
        "template": "Assets/_Game/Art/MidPoly/Resources/Tree/Source~/Tree_Animated_MidPoly.blend",
        "output_root": "Assets/_Game/Art/MidPoly/Resources/HardwoodTree",
        "replace": {
            "Tree": "HardwoodTree",
            "RES_HardwoodTree_Wood": "RES_Hardwood_Bark",
            "RES_HardwoodTree_Foliage": "RES_Hardwood_Foliage",
        },
        "heights": {"Active": 6.65, "Exhausted": 0.84},
        "collider": (1.5, 1.5, 6.0),
        "materials": {
            "RES_Hardwood_Bark": ((0.115, 0.052, 0.028, 1.0), 0.0, 0.86),
            "RES_Hardwood_Foliage": ((0.035, 0.145, 0.072, 1.0), 0.0, 0.74),
        },
    },
    "SwampHemp": {
        "template": "Assets/_Game/Art/MidPoly/Resources/FiberPlant/Source~/FiberPlant_Animated_MidPoly.blend",
        "output_root": "Assets/_Game/Art/MidPoly/Resources/SwampHemp",
        "replace": {
            "FiberPlant": "SwampHemp",
            "RES_Fiber_Green": "RES_SwampHemp_Stalk",
            "RES_Fiber_Cream": "RES_SwampHemp_Head",
        },
        "heights": {"Active": 1.372, "Exhausted": 0.228},
        "collider": (2.43, 2.43, 2.43),
        "materials": {
            "RES_SwampHemp_Stalk": ((0.125, 0.245, 0.082, 1.0), 0.0, 0.76),
            "RES_SwampHemp_Head": ((0.53, 0.43, 0.14, 1.0), 0.0, 0.64),
        },
    },
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--asset", required=True, choices=sorted(ASSETS))
    argv = []
    if "--" in __import__("sys").argv:
        argv = __import__("sys").argv[__import__("sys").argv.index("--") + 1 :]
    return parser.parse_args(argv)


def principled(material: bpy.types.Material):
    material.use_nodes = True
    return material.node_tree.nodes.get("Principled BSDF")


def style_material(name: str, base_color, metallic: float, roughness: float):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    node = principled(material)
    node.inputs["Base Color"].default_value = base_color
    node.inputs["Metallic"].default_value = metallic
    node.inputs["Roughness"].default_value = roughness
    specular = node.inputs.get("Specular IOR Level")
    if specular is not None:
        specular.default_value = 0.32
    material.diffuse_color = base_color
    return material


def rename_datablocks(replacements: dict[str, str]) -> None:
    groups = (
        bpy.data.objects,
        bpy.data.meshes,
        bpy.data.collections,
        bpy.data.materials,
    )
    for group in groups:
        for datablock in list(group):
            name = datablock.name
            for old, new in replacements.items():
                name = name.replace(old, new)
            datablock.name = name


def rotate_xy(x: float, y: float, angle: float) -> tuple[float, float]:
    cosine = math.cos(angle)
    sine = math.sin(angle)
    return x * cosine - y * sine, x * sine + y * cosine


def transform_hardwood(obj: bpy.types.Object, target_height: float, active: bool) -> None:
    mesh = obj.data
    minimum = min(vertex.co.z for vertex in mesh.vertices)
    maximum = max(vertex.co.z for vertex in mesh.vertices)
    height = maximum - minimum
    if height <= 0.0001:
        raise RuntimeError(f"Degenerate mesh height: {obj.name}")

    foliage_vertices: set[int] = set()
    if active and len(mesh.materials) > 1:
        for polygon in mesh.polygons:
            if polygon.material_index == 1:
                foliage_vertices.update(polygon.vertices)

    for vertex in mesh.vertices:
        normalized = (vertex.co.z - minimum) / height
        x = vertex.co.x
        y = vertex.co.y
        if vertex.index in foliage_vertices:
            crown_scale = 1.12 + 0.05 * math.sin(normalized * math.pi * 3.0)
            x *= crown_scale
            y *= crown_scale * 1.08
            x += 0.09 * normalized
            y -= 0.04 * normalized
        else:
            trunk_scale = (1.22 - 0.18 * normalized) if active else 1.18
            x *= trunk_scale
            y *= trunk_scale * 1.03
            x += 0.055 * math.sin(normalized * math.pi * 1.7)
        x, y = rotate_xy(x, y, 0.075 * normalized)
        vertex.co.x = x
        vertex.co.y = y
        vertex.co.z = minimum + normalized * target_height
    mesh.update()


def transform_swamp_hemp(obj: bpy.types.Object, target_height: float, active: bool) -> None:
    mesh = obj.data
    minimum = min(vertex.co.z for vertex in mesh.vertices)
    maximum = max(vertex.co.z for vertex in mesh.vertices)
    height = maximum - minimum
    if height <= 0.0001:
        raise RuntimeError(f"Degenerate mesh height: {obj.name}")

    for vertex in mesh.vertices:
        normalized = (vertex.co.z - minimum) / height
        x = vertex.co.x * (1.34 if active else 1.24)
        y = vertex.co.y * (1.26 if active else 1.18)
        if active:
            x += math.copysign(0.055 * normalized * normalized, x or 1.0)
            y += math.copysign(0.04 * normalized * normalized, y or 1.0)
            x, y = rotate_xy(x, y, 0.115 * normalized)
            x += 0.035 * normalized * normalized
            y -= 0.02 * normalized * normalized
        vertex.co.x = x
        vertex.co.y = y
        vertex.co.z = minimum + normalized * target_height
    mesh.update()


def fit_collider(obj: bpy.types.Object, dimensions: tuple[float, float, float]) -> None:
    current = obj.dimensions.copy()
    obj.scale.x *= dimensions[0] / current.x
    obj.scale.y *= dimensions[1] / current.y
    obj.scale.z *= dimensions[2] / current.z
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def descendants(root: bpy.types.Object):
    yield root
    for child in root.children:
        yield from descendants(child)


def export_root(root_name: str, path: Path, animations: bool) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    root = bpy.data.objects.get(root_name)
    if root is None:
        raise RuntimeError(f"Export root missing: {root_name}")
    for obj in descendants(root):
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        check_existing=False,
        export_format="GLB",
        use_selection=True,
        export_yup=True,
        export_apply=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
        export_animations=animations,
        export_animation_mode="ACTIONS",
        export_frame_range=False,
        export_force_sampling=True,
        export_nla_strips=False,
        export_extras=True,
    )


def triangle_count(root_name: str) -> int:
    root = bpy.data.objects[root_name]
    total = 0
    for obj in descendants(root):
        if obj.type != "MESH":
            continue
        obj.data.calc_loop_triangles()
        total += len(obj.data.loop_triangles)
    return total


def write_manifest(asset: str, output_root: Path, heights: dict[str, float]) -> None:
    manifest = {
        "asset": asset,
        "status": "Phase 5 production candidate",
        "unityScale": "1 unit = 1 meter",
        "activeHeight": heights["Active"],
        "exhaustedHeight": heights["Exhausted"],
        "animations": ["Idle_Sway", "Harvest_Recoil"],
        "triangles": {
            "lod0": triangle_count(f"RES_{asset}_Active_Root"),
            "lod1": triangle_count(f"RES_{asset}_Active_Lod1_Root"),
            "lod2": triangle_count(f"RES_{asset}_Active_Lod2_Root"),
            "exhausted": triangle_count(f"RES_{asset}_Exhausted_Root"),
        },
        "sourceTemplate": "Tree" if asset == "HardwoodTree" else "FiberPlant",
        "geometryRevision": "Tier-2 proportions, silhouette and material identity",
    }
    path = output_root / "Source~" / "AssetManifest.json"
    path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    args = parse_args()
    project_root = Path(args.project_root).resolve()
    asset = args.asset
    config = ASSETS[asset]
    template = project_root / config["template"]
    output_root = project_root / config["output_root"]
    source_dir = output_root / "Source~"
    runtime_dir = output_root / "Runtime"
    source_dir.mkdir(parents=True, exist_ok=True)
    runtime_dir.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.open_mainfile(filepath=str(template))
    rename_datablocks(config["replace"])
    for name, settings in config["materials"].items():
        style_material(name, *settings)

    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith(f"RES_{asset}_"):
            continue
        state = "Exhausted" if "Exhausted" in obj.name else "Active"
        if asset == "HardwoodTree":
            transform_hardwood(obj, config["heights"][state], state == "Active")
        else:
            transform_swamp_hemp(obj, config["heights"][state], state == "Active")

    collider = bpy.data.objects.get(f"COL_{asset}_Reference")
    if collider is None:
        raise RuntimeError(f"Collider reference missing for {asset}")
    fit_collider(collider, config["collider"])

    scene = bpy.context.scene
    scene["eidren_asset"] = asset
    scene["eidren_phase"] = 5
    scene["eidren_status"] = "production-candidate"
    blend_path = source_dir / f"{asset}_Animated_MidPoly.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), check_existing=False)

    exports = (
        (f"RES_{asset}_Active_Root", runtime_dir / f"RES_{asset}_Active_Lod0_Animated.glb", True),
        (f"RES_{asset}_Active_Lod1_Root", runtime_dir / f"RES_{asset}_Active_Lod1.glb", False),
        (f"RES_{asset}_Active_Lod2_Root", runtime_dir / f"RES_{asset}_Active_Lod2.glb", False),
        (f"RES_{asset}_Exhausted_Root", runtime_dir / f"RES_{asset}_Exhausted_Lod0.glb", False),
        (f"COL_{asset}_Root", source_dir / f"COL_{asset}_Reference.glb", False),
    )
    for root_name, output_path, animations in exports:
        export_root(root_name, output_path, animations)

    write_manifest(asset, output_root, config["heights"])
    print(f"EIDREN_PHASE5_EXPORT_OK asset={asset} blend={blend_path}")


if __name__ == "__main__":
    main()
