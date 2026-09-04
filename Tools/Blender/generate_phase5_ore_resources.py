"""Build the Phase-5 granite and iron resource production files.

The approved StoneDeposit and CopperVein sources are used as construction
families.  Geometry, dimensions, material identity and collider references are
specialized per resource before deterministic GLB export.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import bpy


ASSETS = {
    "GraniteDeposit": {
        "template": "Assets/_Game/Art/MidPoly/Resources/StoneDeposit/Source~/StoneDeposit_Animated_MidPoly.blend",
        "output_root": "Assets/_Game/Art/MidPoly/Resources/GraniteDeposit",
        "replace": {
            "StoneDeposit": "GraniteDeposit",
            "RES_Stone_Slate": "RES_Granite_Host",
        },
        "heights": {"Active": 1.266, "Exhausted": 0.749},
        "collider": (1.9, 1.9, 1.35),
        "materials": {
            "RES_Granite_Host": ((0.235, 0.275, 0.325, 1.0), 0.03, 0.82),
            "RES_Granite_Mica": ((0.54, 0.61, 0.67, 1.0), 0.32, 0.42),
        },
    },
    "IronVein": {
        "template": "Assets/_Game/Art/MidPoly/Resources/CopperVein/Source~/CopperVein_Animated_MidPoly.blend",
        "output_root": "Assets/_Game/Art/MidPoly/Resources/IronVein",
        "replace": {
            "CopperVein": "IronVein",
            "RES_Copper_HostRock": "RES_Iron_HostRock",
            "RES_Copper_Ore": "RES_Iron_Ore",
        },
        "heights": {"Active": 1.408, "Exhausted": 0.749},
        "collider": (1.9, 1.9, 1.35),
        "materials": {
            "RES_Iron_HostRock": ((0.075, 0.095, 0.125, 1.0), 0.08, 0.9),
            "RES_Iron_Ore": ((0.34, 0.24, 0.20, 1.0), 0.78, 0.33),
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
        specular.default_value = 0.36
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


def transform_resource_mesh(obj: bpy.types.Object, asset: str, target_height: float) -> None:
    mesh = obj.data
    z_values = [vertex.co.z for vertex in mesh.vertices]
    minimum = min(z_values)
    maximum = max(z_values)
    height = maximum - minimum
    if height <= 0.0001:
        raise RuntimeError(f"Degenerate mesh height: {obj.name}")

    for vertex in mesh.vertices:
        normalized = (vertex.co.z - minimum) / height
        if asset == "GraniteDeposit":
            vertex.co.x *= 0.91 + 0.09 * normalized
            vertex.co.y *= 1.02 - 0.045 * normalized
            vertex.co.x += vertex.co.y * 0.035 * normalized * normalized
        else:
            vertex.co.x *= 0.95 + 0.075 * normalized
            vertex.co.y *= 0.985 + 0.035 * normalized
            vertex.co.x += vertex.co.y * 0.025 * normalized
        vertex.co.z = minimum + normalized * target_height
    mesh.update()


def add_granite_mica(obj: bpy.types.Object, material: bpy.types.Material) -> None:
    mesh = obj.data
    if mesh.materials.get(material.name) is None:
        mesh.materials.append(material)
    material_index = list(mesh.materials).index(material)
    for polygon in mesh.polygons:
        if polygon.normal.z > 0.18 and polygon.index % 13 in (0, 1):
            polygon.material_index = material_index


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

    styled = {
        name: style_material(name, *settings)
        for name, settings in config["materials"].items()
    }
    mica = styled.get("RES_Granite_Mica")

    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith(f"RES_{asset}_"):
            continue
        state = "Exhausted" if "Exhausted" in obj.name else "Active"
        transform_resource_mesh(obj, asset, config["heights"][state])
        if mica is not None:
            add_granite_mica(obj, mica)

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

    print(f"EIDREN_PHASE5_EXPORT_OK asset={asset} blend={blend_path}")


if __name__ == "__main__":
    main()
