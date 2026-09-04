"""Generate the Phase-5 environment production kit for Eidren.

The kit replaces the remaining procedural StyleProof props with editable
Blender sources and deterministic GLB LODs while retaining their established
Unity dimensions, pivots and gameplay collider contracts.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


ASSETS = {
    "SP_Tree_A": (7.5, 3.57, 2.736, "tree_a"),
    "SP_Tree_B": (6.0, 4.024, 3.096, "tree_b"),
    "SP_Tree_C": (4.0, 2.715, 2.028, "tree_c"),
    "SP_Plant_Bush": (0.9, 1.501, 1.176, "bush"),
    "SP_Plant_Fern": (0.7, 1.305, 1.037, "fern"),
    "SP_Plant_Flowers": (0.4, 0.493, 0.459, "flowers"),
    "SP_GroundCover_Grass": (0.15, 1.188, 1.2, "grass"),
    "SP_GroundCover_Moss": (0.12, 1.2, 1.137, "moss"),
    "SP_Accent_GlowMushrooms": (0.35, 0.54, 0.484, "mushrooms"),
    "SP_Rock_Small": (0.503, 1.826, 1.684, "rock_small"),
    "SP_Rock_Medium": (1.106, 2.506, 1.819, "rock_medium"),
    "SP_Rock_Large": (2.403, 2.646, 2.064, "rock_large"),
    "SP_RuinWall_A": (2.6, 2.772, 1.506, "ruin_a"),
    "SP_RuinWall_B": (2.6, 2.168, 1.24, "ruin_b"),
    "SP_RuinMonument": (5.0, 1.795, 1.436, "monument"),
    "SP_EidrenRune": (0.8, 0.721, 0.44, "rune"),
}


def args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    values = []
    import sys
    if "--" in sys.argv:
        values = sys.argv[sys.argv.index("--") + 1 :]
    return parser.parse_args(values)


def material(name: str, color, roughness: float, metallic: float = 0.0, emission=None):
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    bsdf = value.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if emission:
        bsdf.inputs["Emission Color"].default_value = emission
        bsdf.inputs["Emission Strength"].default_value = 2.2
    value.diffuse_color = color
    return value


def create_materials():
    return {
        "bark": material("MP_ENV_Bark", (0.16, 0.065, 0.028, 1), 0.88),
        "leaf_dark": material("MP_ENV_LeafDark", (0.035, 0.18, 0.07, 1), 0.76),
        "leaf_light": material("MP_ENV_LeafLight", (0.11, 0.32, 0.12, 1), 0.7),
        "plant": material("MP_ENV_Plant", (0.13, 0.34, 0.08, 1), 0.75),
        "flower_yellow": material("MP_ENV_FlowerYellow", (0.82, 0.61, 0.12, 1), 0.6),
        "flower_pink": material("MP_ENV_FlowerPink", (0.68, 0.16, 0.32, 1), 0.62),
        "moss": material("MP_ENV_Moss", (0.07, 0.22, 0.045, 1), 0.92),
        "mushroom": material("MP_ENV_MushroomStem", (0.42, 0.32, 0.21, 1), 0.76),
        "glow": material("MP_ENV_MushroomGlow", (0.13, 0.62, 0.55, 1), 0.48, emission=(0.08, 0.85, 0.72, 1)),
        "stone": material("MP_ENV_Stone", (0.31, 0.34, 0.37, 1), 0.9),
        "stone_light": material("MP_ENV_StoneLight", (0.48, 0.51, 0.53, 1), 0.86),
        "ruin": material("MP_ENV_RuinStone", (0.39, 0.37, 0.33, 1), 0.92),
        "ruin_accent": material("MP_ENV_RuinAccent", (0.24, 0.17, 0.09, 1), 0.84),
        "rune_plate": material("MP_ENV_RunePlate", (0.19, 0.21, 0.25, 1), 0.82),
        "rune_glow": material("MP_ENV_RuneGlow", (0.08, 0.52, 0.72, 1), 0.42, emission=(0.04, 0.8, 1.0, 1)),
    }


def root(name: str):
    obj = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def attach(obj, parent, mat, name: str):
    obj.name = name
    obj.parent = parent
    if obj.type == "MESH":
        obj.data.materials.append(mat)
    return obj


def apply(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def cone(parent, name, center, depth, radii, mat, vertices=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radii[0],
        radius2=radii[1],
        depth=depth,
        location=(center[0], center[1], center[2] + depth * 0.5),
        rotation=rotation,
    )
    obj = attach(bpy.context.object, parent, mat, name)
    bevel = obj.modifiers.new("EdgeWear", "BEVEL")
    bevel.width = min(depth, radii[0] * 2) * 0.025
    bevel.segments = 2
    apply_modifier(obj, bevel.name)
    return obj


def cube(parent, name, center, size, mat, bevel_width=0.03, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=center, rotation=rotation)
    obj = attach(bpy.context.object, parent, mat, name)
    obj.scale = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    apply(obj)
    bevel = obj.modifiers.new("WornEdges", "BEVEL")
    bevel.width = min(size) * bevel_width
    bevel.segments = 2
    apply_modifier(obj, bevel.name)
    return obj


def ico(parent, name, center, size, mat, subdivisions=4, twist=0.0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=center)
    obj = attach(bpy.context.object, parent, mat, name)
    obj.scale = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    apply(obj)
    for vertex in obj.data.vertices:
        z = vertex.co.z / max(0.001, size[2] * 0.5)
        vertex.co.x += math.sin(vertex.co.y * 5.7 + z * 2.3 + twist) * size[0] * 0.035
        vertex.co.y += math.sin(vertex.co.x * 4.9 - z * 1.7 + twist) * size[1] * 0.028
    obj.data.update()
    return obj


def branch(parent, name, start, end, radius, mat):
    start_v, end_v = Vector(start), Vector(end)
    direction = end_v - start_v
    midpoint = (start_v + end_v) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=radius, depth=direction.length, location=midpoint)
    obj = attach(bpy.context.object, parent, mat, name)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    apply(obj)
    return obj


def ribbon(parent, name, angle, length, width, mat, segments=16, tilt=0.0):
    verts, faces = [], []
    for i in range(segments + 1):
        t = i / segments
        half = width * (1.0 - t * 0.84) * 0.5
        bend = math.sin(t * math.pi) * length * 0.12
        verts.extend([(-half, bend, t * length), (half, bend, t * length)])
        if i:
            base = i * 2
            faces.append((base - 2, base - 1, base + 1, base))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    attach(obj, parent, mat, name)
    obj.rotation_euler = (tilt, 0.0, angle)
    return obj


def apply_modifier(obj, name):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=name)
    obj.select_set(False)


def build_tree(parent, variant: str, mats):
    if variant == "tree_a":
        trunk_h, crown_z, spread = 4.45, 4.25, 1.0
        centers = [(0, 0, crown_z), (0.08, -0.05, 5.55), (-0.08, 0.05, 6.65)]
    elif variant == "tree_b":
        trunk_h, crown_z, spread = 3.25, 3.15, 1.18
        centers = [(0, 0, crown_z), (0.18, -0.08, 4.25), (-0.12, 0.08, 5.15)]
    else:
        trunk_h, crown_z, spread = 2.55, 2.5, 0.9
        centers = [(0.25, 0, crown_z), (0.46, -0.05, 3.34)]
    cone(parent, "Trunk", (0, 0, 0), trunk_h, (0.56 * spread, 0.22 * spread), mats["bark"])
    branch(parent, "RootA", (0, 0, 0.18), (0.7 * spread, 0.15, 0.02), 0.14 * spread, mats["bark"])
    branch(parent, "RootB", (0, 0, 0.15), (-0.48 * spread, -0.34, 0.02), 0.12 * spread, mats["bark"])
    branch(parent, "BranchA", (0, 0, trunk_h * 0.62), (0.68 * spread, 0.08, trunk_h * 0.78), 0.12 * spread, mats["bark"])
    branch(parent, "BranchB", (0, 0, trunk_h * 0.7), (-0.5 * spread, -0.18, trunk_h * 0.88), 0.1 * spread, mats["bark"])
    for index, center in enumerate(centers):
        size = (2.9 * spread * (1 - index * 0.16), 2.35 * spread * (1 - index * 0.14), 2.05 - index * 0.22)
        ico(parent, f"Crown_{index:02}", center, size, mats["leaf_dark" if index % 2 == 0 else "leaf_light"], twist=index * 0.8)
        if index == 0:
            ico(parent, "Crown_Accent", (center[0] + 0.45 * spread, center[1] - 0.24, center[2] + 0.14), (1.25 * spread, 1.05 * spread, 1.1), mats["leaf_light"], twist=2.4)


def build_bush(parent, mats):
    ico(parent, "Bush_Core", (0, 0, 0.43), (1.25, 0.96, 0.8), mats["leaf_dark"], 4)
    ico(parent, "Bush_Left", (-0.42, 0.03, 0.38), (0.72, 0.68, 0.64), mats["leaf_light"], 4, 0.7)
    ico(parent, "Bush_Right", (0.4, -0.05, 0.36), (0.7, 0.64, 0.6), mats["leaf_light"], 4, 1.4)


def build_fern(parent, mats):
    for i in range(14):
        ribbon(parent, f"Frond_{i:02}", i / 14 * math.tau, 0.74 + 0.05 * (i % 3), 0.14, mats["plant"], 18, math.radians(26 + i % 5 * 5))


def build_flowers(parent, mats):
    for i in range(7):
        angle = i / 7 * math.tau
        radius = 0.13 + 0.035 * (i % 2)
        x, y = math.cos(angle) * radius, math.sin(angle) * radius
        height = 0.29 + 0.025 * (i % 3)
        cone(parent, f"Stalk_{i:02}", (x, y, 0), height, (0.017, 0.01), mats["plant"], vertices=8)
        ico(parent, f"Bloom_{i:02}", (x, y, height + 0.035), (0.13, 0.13, 0.09), mats["flower_yellow" if i % 2 == 0 else "flower_pink"], 3, i)


def build_grass(parent, mats):
    for i in range(20):
        angle = i / 20 * math.tau
        blade = ribbon(parent, f"Blade_{i:02}", angle, 0.15 * (0.8 + 0.2 * (i % 4) / 3), 0.055, mats["plant"], 16, math.radians(8 + i % 4 * 3))
        blade.location.x = math.cos(angle) * (0.25 + 0.24 * (i % 3) / 2)
        blade.location.y = math.sin(angle) * (0.25 + 0.24 * (i % 3) / 2)


def build_moss(parent, mats):
    for i in range(6):
        angle = i / 6 * math.tau
        ico(parent, f"Moss_{i:02}", (math.cos(angle) * 0.34, math.sin(angle) * 0.31, 0.055), (0.54, 0.46, 0.12), mats["moss"], 3, i)


def build_mushrooms(parent, mats):
    for i in range(6):
        angle = i / 6 * math.tau
        x, y = math.cos(angle) * 0.16, math.sin(angle) * 0.14
        height = 0.19 + 0.035 * (i % 3)
        cone(parent, f"Stem_{i:02}", (x, y, 0), height, (0.035, 0.025), mats["mushroom"], vertices=10)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, location=(x, y, height + 0.035))
        cap = attach(bpy.context.object, parent, mats["glow"], f"Cap_{i:02}")
        cap.scale = (0.095, 0.09, 0.045)
        apply(cap)


def build_rocks(parent, count: int, mats):
    for i in range(count):
        angle = i / max(1, count) * math.tau + 0.35
        radius = 0.3 + 0.11 * count
        size = 0.62 + 0.13 * (i % 3)
        rock = ico(parent, f"Rock_{i:02}", (math.cos(angle) * radius * 0.55, math.sin(angle) * radius * 0.45, size * 0.38), (size * 1.25, size, size * 0.82), mats["stone" if i % 3 else "stone_light"], 4, i * 1.2)
        rock.rotation_euler.z = angle * 0.37


def build_wall(parent, variant: str, mats):
    rows = 6
    for row in range(rows):
        columns = 5 if variant == "ruin_a" else 4
        for column in range(columns):
            if (row + column + (0 if variant == "ruin_a" else 1)) % 11 == 0 and row > 2:
                continue
            width = 0.52 + 0.04 * ((row + column) % 2)
            x = (column - (columns - 1) * 0.5) * 0.5 + (0.14 if row % 2 else 0)
            z = 0.2 + row * 0.4
            cube(parent, f"Block_{row:02}_{column:02}", (x, 0, z), (width, 0.7 + 0.05 * (column % 2), 0.38), mats["ruin" if (row + column) % 4 else "ruin_accent"], 0.08, rotation=(0.02 * (column % 2), 0.03 * (row % 2), 0.018 * (column - 2)))


def build_monument(parent, mats):
    for row in range(9):
        scale = 1.0 - row * 0.045
        for side in range(4):
            angle = side / 4 * math.tau + (row % 2) * 0.22
            x, y = math.cos(angle) * 0.42 * scale, math.sin(angle) * 0.34 * scale
            cube(parent, f"Monument_{row:02}_{side:02}", (x, y, 0.25 + row * 0.52), (0.6 * scale, 0.5 * scale, 0.49), mats["ruin" if side != 1 else "ruin_accent"], 0.08, rotation=(0, 0, angle + 0.2))
    ico(parent, "BrokenCrown", (0.08, -0.03, 4.72), (0.9, 0.75, 0.58), mats["ruin"], 3, 2.2)


def build_rune(parent, mats):
    cube(parent, "RunePlate", (0, 0, 0.37), (0.68, 0.36, 0.74), mats["rune_plate"], 0.1, rotation=(0.02, -0.05, 0.0))
    for index, scale in enumerate((0.19, 0.12)):
        bpy.ops.mesh.primitive_torus_add(major_radius=scale, minor_radius=0.018, major_segments=32, minor_segments=8, location=(0, -0.19, 0.43 + index * 0.03), rotation=(math.pi * 0.5, 0, 0))
        torus = attach(bpy.context.object, parent, mats["rune_glow"], f"RuneRing_{index:02}")
        torus.scale.z = 1.22
        apply(torus)
    cube(parent, "RuneSpine", (0, -0.205, 0.42), (0.045, 0.035, 0.46), mats["rune_glow"], 0.25, rotation=(0, 0.12, 0.0))


def descendants(value):
    yield value
    for child in value.children:
        yield from descendants(child)


def bounds(value):
    points = []
    for obj in descendants(value):
        if obj.type == "MESH":
            points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def normalize(value, target_height: float, target_x: float, target_y: float):
    # Several generators assign their final rotations/offsets after mesh creation.
    # Force Blender to evaluate those transforms before deriving the production
    # scale, otherwise ribbon-based foliage can be normalized from stale bounds.
    bpy.context.view_layer.update()
    minimum, maximum = bounds(value)
    extent = maximum - minimum
    value.scale = (target_x / extent.x, target_y / extent.y, target_height / extent.z)
    bpy.context.view_layer.update()
    minimum, _ = bounds(value)
    value.location.z -= minimum.z
    bpy.context.view_layer.update()


def duplicate_lod(source, name: str, ratio: float):
    target = root(name)
    for child in source.children:
        duplicate = child.copy()
        duplicate.data = child.data.copy()
        bpy.context.scene.collection.objects.link(duplicate)
        duplicate.parent = target
        duplicate.matrix_world = child.matrix_world.copy()
        if duplicate.type == "MESH" and len(duplicate.data.polygons) > 2:
            modifier = duplicate.modifiers.new("ProductionDecimate", "DECIMATE")
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            apply_modifier(duplicate, modifier.name)
    return target


def triangles(value) -> int:
    total = 0
    for obj in descendants(value):
        if obj.type == "MESH":
            obj.data.calc_loop_triangles()
            total += len(obj.data.loop_triangles)
    return total


def export(value, path: Path):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in descendants(value):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = value
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
        export_animations=False,
        export_extras=True,
    )


def build_asset(name: str, kind: str, mats):
    value = root(f"PRP_{name}_LOD0_Root")
    if kind.startswith("tree"):
        build_tree(value, kind, mats)
    elif kind == "bush":
        build_bush(value, mats)
    elif kind == "fern":
        build_fern(value, mats)
    elif kind == "flowers":
        build_flowers(value, mats)
    elif kind == "grass":
        build_grass(value, mats)
    elif kind == "moss":
        build_moss(value, mats)
    elif kind == "mushrooms":
        build_mushrooms(value, mats)
    elif kind.startswith("rock"):
        build_rocks(value, {"rock_small": 2, "rock_medium": 3, "rock_large": 4}[kind], mats)
    elif kind in {"ruin_a", "ruin_b"}:
        build_wall(value, kind, mats)
    elif kind == "monument":
        build_monument(value, mats)
    elif kind == "rune":
        build_rune(value, mats)
    else:
        raise ValueError(kind)
    return value


def main():
    project = Path(args().project_root).resolve()
    source = project / "Assets/_Game/Art/MidPoly/Environment/Phase5/Source~"
    runtime = project / "Assets/_Game/Art/MidPoly/Environment/Phase5/Runtime"
    source.mkdir(parents=True, exist_ok=True)
    runtime.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = create_materials()
    manifest = {"schema": 1, "phase": 5, "status": "production-candidate", "assets": []}

    for name, (height, width, depth, kind) in ASSETS.items():
        lod0 = build_asset(name, kind, mats)
        normalize(lod0, height, width, depth)
        lod1 = duplicate_lod(lod0, f"PRP_{name}_LOD1_Root", 0.52)
        lod2 = duplicate_lod(lod0, f"PRP_{name}_LOD2_Root", 0.21)
        paths = []
        for index, lod in enumerate((lod0, lod1, lod2)):
            path = runtime / f"PRP_{name}_Mid_LOD{index}.glb"
            export(lod, path)
            paths.append(str(path.relative_to(project)).replace("\\", "/"))
        minimum, maximum = bounds(lod0)
        manifest["assets"].append(
            {
                "asset": name,
                "targetSize": [width, height, depth],
                "exportedSize": [round(maximum.x - minimum.x, 6), round(maximum.z - minimum.z, 6), round(maximum.y - minimum.y, 6)],
                "triangles": {"lod0": triangles(lod0), "lod1": triangles(lod1), "lod2": triangles(lod2)},
                "materials": sorted({slot.material.name for obj in descendants(lod0) if obj.type == "MESH" for slot in obj.material_slots}),
                "runtime": paths,
            }
        )

    blend = source / "PRP_EnvironmentKit_Mid_Phase5.blend"
    bpy.context.scene["eidren_phase"] = 5
    bpy.context.scene["eidren_kit"] = "Environment"
    bpy.context.scene["eidren_status"] = "production-candidate"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend), check_existing=False)
    (source / "ENVIRONMENT_KIT_PRODUCTION_MANIFEST.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"EIDREN_PHASE5_ENVIRONMENT_OK assets={len(ASSETS)} blend={blend}")


if __name__ == "__main__":
    main()
