"""Print aggregate module bounds and triangle counts for the Wanderer source."""

import json
import re

import bpy


MODULE_PATTERN = re.compile(
    r"^(LOD[0-2])_(Basis|Helm_(?:Stoff|Kupfer|Eisen)|Harnisch_(?:Stoff|Kupfer|Eisen)|"
    r"Haende_(?:Stoff|Kupfer|Eisen)|Beine_(?:Stoff|Kupfer|Eisen)|"
    r"Waffe_(?:Hammer_(?:Base|Kupfer|Eisen|Sealbreaker)|Dolche_(?:Base|Kupfer|Eisen|AshFangs)|"
    r"Speer_(?:Kupfer|Eisen|EmberThorn)|Axt_(?:Base|Kupfer|Eisen)|"
    r"Spitzhacke_(?:Base|Kupfer|Eisen)|Sense_(?:Base|Kupfer|Eisen)))_"
)


def extend_bounds(bounds, coordinate):
    for axis in range(3):
        bounds[0][axis] = min(bounds[0][axis], coordinate[axis])
        bounds[1][axis] = max(bounds[1][axis], coordinate[axis])


summary = {}
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    match = MODULE_PATTERN.match(obj.name)
    if not match:
        continue
    key = f"{match.group(1)}:{match.group(2)}"
    item = summary.setdefault(
        key,
        {
            "lod": match.group(1),
            "module": match.group(2),
            "objects": 0,
            "vertices": 0,
            "triangles": 0,
            "bounds": [[float("inf")] * 3, [float("-inf")] * 3],
            "materials": set(),
        },
    )
    obj.data.calc_loop_triangles()
    item["objects"] += 1
    item["vertices"] += len(obj.data.vertices)
    item["triangles"] += len(obj.data.loop_triangles)
    item["materials"].update(slot.material.name for slot in obj.material_slots if slot.material)
    for vertex in obj.data.vertices:
        extend_bounds(item["bounds"], obj.matrix_world @ vertex.co)

for item in summary.values():
    item["materials"] = sorted(item["materials"])
    item["bounds"] = [[round(value, 5) for value in coordinate] for coordinate in item["bounds"]]

print("WANDERER_BLEND_SUMMARY_BEGIN")
print(json.dumps({
    "file": bpy.data.filepath,
    "modules": sorted(summary.values(), key=lambda item: (item["lod"], item["module"])),
    "actions": sorted(action.name for action in bpy.data.actions),
}, ensure_ascii=False))
print("WANDERER_BLEND_SUMMARY_END")
