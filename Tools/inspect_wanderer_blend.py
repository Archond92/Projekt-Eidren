"""Print a compact JSON inventory for a Wanderer Blender source file.

Run with Blender in background mode. The inventory intentionally avoids
changing or saving the open file.
"""

import json

import bpy


def mesh_triangles(mesh):
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


objects = []
for obj in bpy.data.objects:
    entry = {
        "name": obj.name,
        "type": obj.type,
        "parent": obj.parent.name if obj.parent else None,
        "hiddenViewport": obj.hide_viewport,
        "hiddenRender": obj.hide_render,
        "collections": sorted(collection.name for collection in obj.users_collection),
    }
    if obj.type == "MESH":
        entry.update(
            {
                "vertices": len(obj.data.vertices),
                "triangles": mesh_triangles(obj.data),
                "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
                "armatureModifiers": [modifier.object.name if modifier.object else None for modifier in obj.modifiers if modifier.type == "ARMATURE"],
                "vertexGroups": [group.name for group in obj.vertex_groups],
            }
        )
    elif obj.type == "ARMATURE":
        entry["bones"] = [bone.name for bone in obj.data.bones]
    objects.append(entry)

payload = {
    "file": bpy.data.filepath,
    "scene": bpy.context.scene.name,
    "frameRange": [bpy.context.scene.frame_start, bpy.context.scene.frame_end],
    "objects": objects,
    "materials": [material.name for material in bpy.data.materials],
    "actions": [
        {
            "name": action.name,
            "frameRange": list(action.frame_range),
            "slots": [slot.identifier for slot in action.slots] if hasattr(action, "slots") else [],
        }
        for action in bpy.data.actions
    ],
    "collections": [
        {
            "name": collection.name,
            "hiddenViewport": collection.hide_viewport,
            "hiddenRender": collection.hide_render,
            "objects": [obj.name for obj in collection.objects],
        }
        for collection in bpy.data.collections
    ],
}

print("WANDERER_BLEND_INVENTORY_BEGIN")
print(json.dumps(payload, ensure_ascii=False))
print("WANDERER_BLEND_INVENTORY_END")
