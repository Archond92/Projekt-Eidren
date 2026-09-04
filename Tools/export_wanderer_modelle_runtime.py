"""Export the approved Wanderer_Modelle.blend comparison scene as one Unity runtime GLB.

Run with Blender, not regular Python:
  blender --background Wanderer_Modelle.blend --python Tools/export_wanderer_modelle_runtime.py -- <output.glb>

The comparison scene deliberately contains many copies of the character.  This exporter
selects one canonical body, one copy of each armour set and hand item, then copies the
approved actions onto a single shared rig.  It also bakes the Blender material colours
into COLOR_0 because Unity replaces all imported materials with the Wanderer vertex-
colour material.
"""

from __future__ import annotations

import json
import math
import re
import sys
import unicodedata
from collections import OrderedDict, defaultdict
from pathlib import Path

import bpy
from mathutils import Matrix


BASE_COLLECTION = "MP_Z26_Ruhe_Grundkoerper_Set"
ARMOUR_COLLECTIONS = OrderedDict(
    (
        ("Stoff", "MP_Z26_Stoff_Ruhe_Grundkoerper_Set"),
        ("Kupfer", "MP_Z26_Kupfer_Ruhe_Grundkoerper_Set"),
        ("Eisen", "MP_Z26_Eisen_Ruhe_Grundkoerper_Set"),
    )
)

HAND_ITEM_SOURCES = OrderedDict(
    (
        ("Waffe_Axt", ("MP_Z8_Ruhe_Axt_Set", "Waffe_Axt")),
        ("Waffe_Sense", ("MP_Z9_Ruhe_Sense_Set", "Werkzeug_Sense")),
        ("Waffe_Spitzhacke", ("MP_Z10_Ruhe_Spitzhacke_Set", "Werkzeug_Spitzhacke")),
        ("Waffe_Dolche", ("MP_Z11_Ruhe_Dolche_Set", "Dolch_")),
        # The cloth carrying copy contains the final collision-corrected hammer mesh.
        ("Waffe_Hammer", ("MP_Z12_Stoff_Ruhe_Hammer_Set", "Waffe_Hammer")),
        ("Waffe_Speer", ("MP_Z13_Ruhe_Speer_Set", "Waffe_Speer")),
    )
)

ACTION_SOURCES = OrderedDict(
    (
        ("Abbau_Axt", "MP_Z1_Axt_Set"),
        ("Abbau_Sense", "MP_Z2_Sense_Set"),
        ("Abbau_Spitzhacke", "MP_Z3_Spitzhacke_Set"),
        ("Angriff_Dolche", "MP_Z4_Dolche_Set"),
        ("Angriff_Hammer", "MP_Z5_Hammer_Set"),
        ("Angriff_Speer", "MP_Z6_Speer_Set"),
        ("Oeffnen", "MP_Z7_Oeffnen_Set"),
        ("Ruhe_Axt", "MP_Z8_Ruhe_Axt_Set"),
        ("Ruhe_Sense", "MP_Z9_Ruhe_Sense_Set"),
        ("Ruhe_Spitzhacke", "MP_Z10_Ruhe_Spitzhacke_Set"),
        ("Ruhe_Dolche", "MP_Z11_Ruhe_Dolche_Set"),
        ("Ruhe_Hammer", "MP_Z12_Ruhe_Hammer_Set"),
        ("Ruhe_Speer", "MP_Z13_Ruhe_Speer_Set"),
        ("Gehen_Axt", "MP_Z14_Gehen_Axt_Set"),
        ("Gehen_Sense", "MP_Z15_Gehen_Sense_Set"),
        ("Gehen_Spitzhacke", "MP_Z16_Gehen_Spitzhacke_Set"),
        ("Gehen_Dolche", "MP_Z17_Gehen_Dolche_Set"),
        ("Gehen_Hammer", "MP_Z18_Gehen_Hammer_Set"),
        ("Gehen_Speer", "MP_Z19_Gehen_Speer_Set"),
        ("Laufen_Axt", "MP_Z20_Laufen_Axt_Set"),
        ("Laufen_Sense", "MP_Z21_Laufen_Sense_Set"),
        ("Laufen_Spitzhacke", "MP_Z22_Laufen_Spitzhacke_Set"),
        ("Laufen_Dolche", "MP_Z23_Laufen_Dolche_Set"),
        ("Laufen_Hammer", "MP_Z24_Laufen_Hammer_Set"),
        ("Laufen_Speer", "MP_Z25_Laufen_Speer_Set"),
        ("Ruhe_Ohne", "MP_Z26_Ruhe_Grundkoerper_Set"),
        # Z27 is accidentally a static pose in the authored comparison file.
        # The dagger walk has the natural independent arm swing and becomes the
        # correct unarmed walk when its two dagger meshes are hidden at runtime.
        ("Gehen_Ohne", "MP_Z17_Gehen_Dolche_Set"),
        ("Laufen_Ohne", "MP_Z28_Laufen_Grundkoerper_Set"),
    )
)

SEMANTIC_MODULES = (
    "Basis",
    "Haare",
    "Helm_Stoff",
    "Helm_Kupfer",
    "Helm_Eisen",
    "Harnisch_Stoff",
    "Harnisch_Kupfer",
    "Harnisch_Eisen",
    "Beine_Stoff",
    "Beine_Kupfer",
    "Beine_Eisen",
    "Haende_Stoff",
    "Haende_Kupfer",
    "Haende_Eisen",
    "Waffe_Speer",
    "Waffe_Dolche",
    "Waffe_Hammer",
    "Waffe_Axt",
    "Waffe_Spitzhacke",
    "Waffe_Sense",
)


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def collection(name: str) -> bpy.types.Collection:
    result = bpy.data.collections.get(name)
    require(result is not None, f"Collection fehlt: {name}")
    return result


def collection_rig(name: str) -> bpy.types.Object:
    rigs = [obj for obj in collection(name).all_objects if obj.type == "ARMATURE"]
    require(len(rigs) == 1, f"Genau ein Rig erwartet in {name}, erhalten: {len(rigs)}")
    return rigs[0]


def ascii_token(value: str) -> str:
    normalized = unicodedata.normalize("NFKD", value).encode("ascii", "ignore").decode("ascii")
    normalized = re.sub(r"[^A-Za-z0-9]+", "_", normalized).strip("_")
    return normalized or "Teil"


def normalized(value: str) -> str:
    return ascii_token(value).casefold()


def armour_module(tier: str, suffix: str) -> str:
    token = normalized(suffix)
    if tier == "Stoff":
        if token.startswith("kapuzenhaube"):
            return "Helm_Stoff"
        if "handschuh" in token:
            return "Haende_Stoff"
        if "sohle" in token:
            return "Beine_Stoff"
        return "Harnisch_Stoff"

    if token.startswith("helm") or token.startswith("wangen"):
        return f"Helm_{tier}"
    if "handschuh" in token:
        return f"Haende_{tier}"
    if any(part in token for part in ("huftplatte", "beinschiene", "knie", "fupanzer")):
        return f"Beine_{tier}"
    return f"Harnisch_{tier}"


def activate(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.hide_render = False
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def ensure_armature_last(obj: bpy.types.Object) -> None:
    armature_indices = [index for index, modifier in enumerate(obj.modifiers) if modifier.type == "ARMATURE"]
    require(len(armature_indices) <= 1, f"Mehrere Armature-Modifier an {obj.name}")
    if armature_indices:
        index = armature_indices[0]
        while index < len(obj.modifiers) - 1:
            obj.modifiers.move(index, index + 1)
            index += 1


def apply_static_modifiers(obj: bpy.types.Object) -> None:
    ensure_armature_last(obj)
    for modifier in list(obj.modifiers):
        if modifier.type == "ARMATURE":
            continue
        activate(obj)
        bpy.ops.object.modifier_apply(modifier=modifier.name)


def material_colour(obj: bpy.types.Object, material_index: int) -> tuple[float, float, float, float]:
    if 0 <= material_index < len(obj.data.materials):
        material = obj.data.materials[material_index]
        if material is not None:
            return tuple(float(component) for component in material.diffuse_color)
    return (1.0, 1.0, 1.0, 1.0)


def bake_material_colours(obj: bpy.types.Object) -> None:
    mesh = obj.data
    old = mesh.color_attributes.get("Color")
    if old is not None:
        mesh.color_attributes.remove(old)
    colour = mesh.color_attributes.new(name="Color", type="BYTE_COLOR", domain="CORNER")
    for polygon in mesh.polygons:
        rgba = material_colour(obj, polygon.material_index)
        for loop_index in polygon.loop_indices:
            colour.data[loop_index].color_srgb = rgba
    try:
        mesh.color_attributes.active_color = colour
    except (AttributeError, TypeError):
        mesh.color_attributes.active_color_index = list(mesh.color_attributes).index(colour)
    try:
        mesh.color_attributes.render_color_index = list(mesh.color_attributes).index(colour)
    except AttributeError:
        pass
    mesh.update()


def remap_armature(obj: bpy.types.Object, export_rig: bpy.types.Object) -> None:
    modifiers = [modifier for modifier in obj.modifiers if modifier.type == "ARMATURE"]
    require(modifiers, f"Armature-Modifier fehlt: {obj.name}")
    for modifier in modifiers:
        modifier.object = export_rig


def copy_mesh(
    source: bpy.types.Object,
    source_rig: bpy.types.Object,
    export_rig: bpy.types.Object,
    parent: bpy.types.Object,
    module: str,
    index: int,
    suffix: str,
) -> bpy.types.Object:
    result = source.copy()
    result.data = source.data.copy()
    result.name = f"LOD0_{module}_{index:02d}_{ascii_token(suffix)}"
    bpy.context.scene.collection.objects.link(result)
    # Every comparison copy is translated to a different grid cell.  Remove that
    # cell transform while preserving the authored local position around its rig.
    result.matrix_world = source_rig.matrix_world.inverted() @ source.matrix_world
    result.parent = parent
    result.matrix_parent_inverse = Matrix.Identity(4)
    result.hide_set(False)
    result.hide_viewport = False
    result.hide_render = False
    result["eidrenModule"] = module
    result["eidrenSourceObject"] = source.name
    result["eidrenRuntime"] = "MidPoly"
    remap_armature(result, export_rig)
    return result


def create_runtime_hierarchy(source_rig: bpy.types.Object):
    root = bpy.data.objects.new("Wanderer", None)
    root["eidrenRuntime"] = "MidPoly"
    bpy.context.scene.collection.objects.link(root)

    rig = source_rig.copy()
    rig.data = source_rig.data.copy()
    rig.name = "Armature"
    bpy.context.scene.collection.objects.link(rig)
    rig.matrix_world = Matrix.Identity(4)
    rig.parent = root
    rig.matrix_parent_inverse = Matrix.Identity(4)
    rig.hide_set(False)
    rig.hide_viewport = False
    rig.hide_render = False
    rig.data.pose_position = "REST"
    rig.animation_data_clear()

    modules = {}
    for module_name in SEMANTIC_MODULES:
        node = bpy.data.objects.new(module_name, None)
        node["eidrenModule"] = module_name
        bpy.context.scene.collection.objects.link(node)
        node.parent = root
        node.matrix_parent_inverse = Matrix.Identity(4)
        modules[module_name] = node
    return root, rig, modules


def assert_compatible_rig(candidate: bpy.types.Object, canonical: bpy.types.Object, context: str) -> None:
    candidate_bones = {bone.name: bone for bone in candidate.data.bones}
    canonical_bones = {bone.name: bone for bone in canonical.data.bones}
    require(
        set(candidate_bones) == set(canonical_bones),
        f"Abweichende Knochen in {context}",
    )
    # The old action rigs intentionally parented `staff` to root, while the final
    # carrying rig parents it to handL.  Retargeting below operates in armature
    # space and therefore supports that difference safely.


def retarget_action(
    source_rig: bpy.types.Object,
    source_action: bpy.types.Action,
    export_rig: bpy.types.Object,
    clip_name: str,
) -> bpy.types.Action:
    """Bake identical skinning deltas from a source rig onto the canonical rig.

    Z1-Z7 were authored before the final arm bone rolls were normalized.  Copying
    their quaternion curves verbatim would twist both arms.  The skinning matrix
    `pose @ inverse(rest)` is independent of bone roll, so transfer that matrix at
    every authored frame and key the canonical pose.
    """
    source_rig.data.pose_position = "POSE"
    export_rig.data.pose_position = "POSE"
    source_rig.hide_viewport = False
    source_rig.hide_render = False
    source_rig.hide_set(False)
    source_rig.animation_data_create()
    source_rig.animation_data.action = source_action
    start, end = (float(value) for value in source_action.frame_range)
    source_samples = []
    for sample_frame in (start, (start + end) * 0.5):
        whole_frame = math.floor(sample_frame)
        bpy.context.scene.frame_set(whole_frame, subframe=sample_frame - whole_frame)
        bpy.context.view_layer.update()
        source_samples.append({bone.name: bone.matrix.copy() for bone in source_rig.pose.bones})
    source_motion_delta = max(
        abs(source_samples[1][bone_name][row][column] - source_samples[0][bone_name][row][column])
        for bone_name in source_samples[0] for row in range(4) for column in range(4)
    )
    require(source_motion_delta > 0.0001, f"Quellclip ist im Exportkontext statisch: {clip_name}")

    export_rig.animation_data_create()
    action = bpy.data.actions.new(name=f"__runtime__{clip_name}")
    slot = action.slots.new(id_type="OBJECT", name=export_rig.name)
    export_rig.animation_data.action = None
    export_rig.animation_data.action = action
    export_rig.animation_data.action_slot = slot
    require(export_rig.animation_data.action_slot == slot, f"Action-Slot nicht gebunden: {clip_name}")
    for pose_bone in export_rig.pose.bones:
        pose_bone.rotation_mode = "QUATERNION"

    frames = [float(frame) for frame in range(int(start), int(end) + 1)]
    if not frames or abs(frames[0] - start) > 0.0001:
        frames.insert(0, start)
    if abs(frames[-1] - end) > 0.0001:
        frames.append(end)

    source_bones = source_rig.data.bones
    target_bones = export_rig.data.bones
    ordered_names = sorted(
        (bone.name for bone in target_bones),
        key=lambda name: len(target_bones[name].parent_recursive),
    )
    for frame in frames:
        whole_frame = math.floor(frame)
        bpy.context.scene.frame_set(whole_frame, subframe=frame - whole_frame)
        bpy.context.view_layer.update()
        target_matrices = {}
        for bone_name in ordered_names:
            source_pose = source_rig.pose.bones[bone_name].matrix.copy()
            skin_delta = source_pose @ source_bones[bone_name].matrix_local.inverted_safe()
            target_matrices[bone_name] = skin_delta @ target_bones[bone_name].matrix_local
        # F34-006: Die lokale Pose wird analytisch aus den Zielmatrizen berechnet.
        # Der Setter `PoseBone.matrix` rechnet gegen die zuletzt EVALUIERTE
        # Elternpose (Stand des vorigen Frames bzw. der Ruhepose). Damit hinkte
        # jede Knochenebene einen Frame hinterher; die ersten Frames jedes Clips
        # trugen Spruenge von bis zu 130 Grad pro Frame (Speer ruckelt) und die
        # Laufschleifen schlossen nicht mehr.
        for bone_name in ordered_names:
            bone = target_bones[bone_name]
            target = target_matrices[bone_name]
            if bone.parent is not None:
                parent_pose = target_matrices[bone.parent.name]
                rest_offset = bone.parent.matrix_local.inverted_safe() @ bone.matrix_local
                basis = (parent_pose @ rest_offset).inverted_safe() @ target
            else:
                basis = bone.matrix_local.inverted_safe() @ target
            export_rig.pose.bones[bone_name].matrix_basis = basis
        # Do not evaluate the depsgraph here: the target action is already active
        # and an evaluation before keying would restore its previous frame, turning
        # every newly baked clip into a constant pose.
        for bone_name in ordered_names:
            pose_bone = export_rig.pose.bones[bone_name]
            pose_bone.keyframe_insert("location", frame=frame, group=bone_name)
            pose_bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone_name)
            pose_bone.keyframe_insert("scale", frame=frame, group=bone_name)

    action["eidrenSourceAction"] = source_action.name
    action["eidrenRetargetedBySkinDelta"] = True
    # Immediate bake audit: compare the complete target pose at the first and
    # middle frame before the source rigs are removed or glTF optimization runs.
    export_rig.animation_data.action = action
    sample_matrices = []
    for sample_frame in (start, (start + end) * 0.5):
        whole_frame = math.floor(sample_frame)
        bpy.context.scene.frame_set(whole_frame, subframe=sample_frame - whole_frame)
        bpy.context.view_layer.update()
        sample_matrices.append({bone.name: bone.matrix.copy() for bone in export_rig.pose.bones})
    motion_delta = max(
        abs(sample_matrices[1][bone_name][row][column] - sample_matrices[0][bone_name][row][column])
        for bone_name in sample_matrices[0] for row in range(4) for column in range(4)
    )
    require(motion_delta > 0.0001, f"Zielclip wurde statisch gebacken: {clip_name}")
    action["eidrenMotionDelta"] = motion_delta
    return action


def copy_actions(export_rig: bpy.types.Object) -> list[bpy.types.Action]:
    copied = []
    for clip_name, collection_name in ACTION_SOURCES.items():
        source_rig = collection_rig(collection_name)
        assert_compatible_rig(source_rig, export_rig, collection_name)
        action = source_rig.animation_data.action if source_rig.animation_data else None
        require(action is not None, f"Action fehlt am Rig in {collection_name}")
        # Bake every clip into one quaternion representation.  Mixing the source
        # rigs' Euler and quaternion rotation modes in one glTF armature makes the
        # exporter choose only one mode and can silently flatten rotations.
        new_action = retarget_action(source_rig, action, export_rig, clip_name)
        copied.append((clip_name, new_action))

    # Remove comparison-scene actions so ACTIONS exports exactly the runtime set.
    copied_set = {action for _, action in copied}
    for obj in bpy.data.objects:
        if obj != export_rig and obj.animation_data is not None:
            obj.animation_data_clear()
    for action in list(bpy.data.actions):
        if action not in copied_set:
            bpy.data.actions.remove(action)

    for clip_name, action in copied:
        action.name = clip_name
        action.use_fake_user = True
        action["eidrenRuntimeClip"] = True
    export_rig.animation_data_create()
    export_rig.animation_data.action = copied[0][1]
    return [action for _, action in copied]


def build_runtime_scene():
    base_collection = collection(BASE_COLLECTION)
    base_rig = collection_rig(BASE_COLLECTION)
    base_rig.data.pose_position = "REST"
    root, export_rig, modules = create_runtime_hierarchy(base_rig)
    module_counts = defaultdict(int)
    runtime_objects = {root, export_rig, *modules.values()}

    # Snapshot first: linking runtime duplicates updates Blender's collection view
    # and may otherwise invalidate this iterator mid-loop.
    for source in list(base_collection.all_objects):
        if source.type != "MESH":
            continue
        is_hair = source.name.endswith("_D_Haare") or source.name.endswith("_D_Haarpony")
        module = "Haare" if is_hair else "Basis"
        suffix = source.name.split("_D_", 1)[-1] if "_D_" in source.name else "Koerper"
        module_counts[module] += 1
        runtime_objects.add(
            copy_mesh(source, base_rig, export_rig, modules[module], module, module_counts[module], suffix)
        )

    for tier, collection_name in ARMOUR_COLLECTIONS.items():
        source_collection = collection(collection_name)
        source_rig = collection_rig(collection_name)
        source_rig.data.pose_position = "REST"
        assert_compatible_rig(source_rig, export_rig, collection_name)
        pieces = [obj for obj in source_collection.all_objects if obj.type == "MESH" and "_Mid_" in obj.name]
        require(pieces, f"Keine Mid-Poly-Ruestungsteile in {collection_name}")
        for source in pieces:
            suffix = source.name.split("_Mid_", 1)[1]
            module = armour_module(tier, suffix)
            module_counts[module] += 1
            runtime_objects.add(
                copy_mesh(source, source_rig, export_rig, modules[module], module, module_counts[module], suffix)
            )

    for module, (collection_name, name_fragment) in HAND_ITEM_SOURCES.items():
        source_collection = collection(collection_name)
        source_rig = collection_rig(collection_name)
        source_rig.data.pose_position = "REST"
        assert_compatible_rig(source_rig, export_rig, collection_name)
        pieces = [
            obj for obj in source_collection.all_objects
            if obj.type == "MESH" and name_fragment in obj.name and "_Mid_" not in obj.name
        ]
        require(pieces, f"Handobjekt fehlt: {module} aus {collection_name}")
        for source in pieces:
            suffix = source.name.rsplit("_", 1)[-1]
            module_counts[module] += 1
            runtime_objects.add(
                copy_mesh(source, source_rig, export_rig, modules[module], module, module_counts[module], suffix)
            )

    # Retargeting evaluates animation frames.  Remove the thousands of comparison
    # meshes before that work, retaining only the 28 source rigs and runtime copies.
    action_source_rigs = {collection_rig(name) for name in ACTION_SOURCES.values()}
    for obj in list(bpy.data.objects):
        if obj not in runtime_objects and obj not in action_source_rigs:
            bpy.data.objects.remove(obj, do_unlink=True)

    actions = copy_actions(export_rig)

    # Delete all comparison copies.  Data used by runtime duplicates remains owned by
    # their copied objects/actions and therefore survives this cleanup.
    for obj in list(bpy.data.objects):
        if obj not in runtime_objects:
            bpy.data.objects.remove(obj, do_unlink=True)

    # Some semantic names already existed in hidden comparison/reference objects,
    # so Blender temporarily suffixed the runtime empties with .001.  Their names
    # are free after cleanup and must exactly match the Unity equipment contract.
    for module_name, node in modules.items():
        node.name = module_name

    # Applying modifiers in the enormous comparison scene makes Blender rebuild all
    # 100+ visible character copies after every piece.  Isolate the runtime scene
    # first, then bake geometry and colours once per retained mesh.
    for obj in list(runtime_objects):
        if obj.type == "MESH":
            apply_static_modifiers(obj)
            obj.data.validate(verbose=False, clean_customdata=False)
            obj.data.update()
            bake_material_colours(obj)

    for module in SEMANTIC_MODULES:
        require(module_counts[module] > 0, f"Runtime-Modul ohne Mesh: {module}")
    return root, export_rig, modules, actions, module_counts


def export_runtime(output: Path) -> dict:
    root, rig, modules, actions, module_counts = build_runtime_scene()
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 150
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    rig.data.pose_position = "POSE"
    rig.animation_data.action = actions[0]
    bpy.ops.export_scene.gltf(
        filepath=str(output),
        check_existing=False,
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_force_sampling=True,
        export_frame_step=1,
        export_skins=True,
        export_def_bones=False,
        export_leaf_bone=False,
        export_materials="EXPORT",
        export_vertex_color="ACTIVE",
        export_all_vertex_colors=False,
        export_extras=True,
        export_yup=True,
        export_cameras=False,
        export_lights=False,
        export_reset_pose_bones=True,
        export_optimize_animation_size=True,
        export_optimize_animation_keep_anim_armature=True,
    )
    return {
        "output": str(output),
        "bytes": output.stat().st_size,
        "actions": [action.name for action in actions],
        "actionCount": len(actions),
        "boneCount": len(rig.data.bones),
        "modules": dict(module_counts),
        "meshObjects": sum(module_counts.values()),
    }


def main() -> None:
    separator = sys.argv.index("--") if "--" in sys.argv else -1
    require(separator >= 0 and separator + 1 < len(sys.argv), "Ausgabepfad nach '--' fehlt")
    output = Path(sys.argv[separator + 1]).resolve()
    report = export_runtime(output)
    print("WANDERER_RUNTIME_EXPORT=" + json.dumps(report, ensure_ascii=False, sort_keys=True))


if __name__ == "__main__":
    main()
