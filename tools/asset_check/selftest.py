"""Tự kiểm tra check_asset.py: dựng asset tốt và asset có lỗi biết trước, xác nhận script bắt đúng lỗi.

  blender -b --factory-startup --python tools/asset_check/selftest.py   -> exit 0 nếu đạt
"""
import json, os, sys, tempfile

import bpy, bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import check_asset as ca  # noqa: E402

TMP = tempfile.mkdtemp(prefix="asset_selftest_")
RULES = {
    "units": {"system": "METRIC", "scale_length": 1.0},
    "materials": {"shader": "BSDF_PRINCIPLED", "max_per_object": 2, "texture_max_px": 2048, "power_of_two": True},
    "rig": {"max_bones": 10, "max_influences": 4, "required_actions": ["idle", "walk"]},
    "export": {"formats": ["fbx", "glb"], "size_tol": 0.01,
               "fbx_options": {"apply_scale_options": "FBX_SCALE_UNITS", "axis_forward": "-Z", "axis_up": "Y",
                               "bake_space_transform": True, "mesh_smooth_type": "FACE", "add_leaf_bones": False}},
    "classes": {
        "st": {"tris_max": 1000, "height": [1.4, 1.6], "footprint_max": [1.0, 1.0], "pivot": "feet",
               "transform_applied": True, "rig": "none", "topology": {"closed": True, "ngons_max": 0},
               "uv": {"required": True, "inside_0_1": True}},
        "st_rig": {"tris_max": 5000, "height": [1.4, 1.6], "footprint_max": [1.0, 1.0], "pivot": "feet",
                   "transform_applied": True, "rig": "required", "topology": {"closed": True, "ngons_max": 0},
                   "uv": {"required": True, "inside_0_1": True}},
    },
}


def body(name, apply=True):
    """Hộp 0,6 × 0,6 × 1,5, gốc ở giữa đáy, có UV và vật liệu Principled."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.75))
    o = bpy.context.active_object
    o.name = name
    o.scale = (0.6, 0.6, 1.5)
    if apply:
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    m = bpy.data.materials.new(name + "_mat")
    m.use_nodes = True
    o.data.materials.append(m)
    return o


def save(name):
    path = os.path.join(TMP, name + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=path)
    return path


def make_good():
    ca.clear_scene()
    body("Good")
    return save("good")


def make_hidden():
    """Asset tốt nhưng đang ẩn trong file — trước đây làm file xuất rỗng."""
    ca.clear_scene()
    o = body("Hidden")
    o.hide_set(True); o.hide_viewport = True
    return save("hidden")


def make_glb():
    """Asset tốt xuất ra GLB (đỉnh bị tách ở đường nối) — topology vẫn phải đạt sau khi hàn."""
    ca.clear_scene()
    o = body("Glb")
    o.select_set(True)
    path = os.path.join(TMP, "good.glb")
    bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB")
    return path


def make_bad():
    ca.clear_scene()
    o = body("Bad")
    o.scale = (1.0, 1.0, 2.0)                      # scale chưa apply, cao 3 m
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    bm.faces[0].normal_flip()                       # pháp tuyến không nhất quán
    bm.faces.remove(bm.faces[1])                    # mesh hở
    bm.verts.new((0.0, 0.0, 0.5))                   # đỉnh rời
    bm.to_mesh(me); bm.free()
    while me.uv_layers:
        me.uv_layers.remove(me.uv_layers[0])         # không có UV
    o.data.materials.append(None)                    # ô vật liệu trống
    return save("bad")


def make_rig(weighted=True, actions=("Idle", "Walk")):
    ca.clear_scene()
    o = body("Rigged")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.subdivide(number_cuts=3)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.armature_add(location=(0, 0, 0))
    arm = bpy.context.active_object
    arm.data.bones[0].name = "Root"
    bpy.ops.object.mode_set(mode="EDIT")
    arm.data.edit_bones["Root"].tail = (0, 0, 1.5)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True); arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    if not weighted:
        g = o.vertex_groups["Root"]
        g.remove([v.index for v in o.data.vertices if v.co.z > 1.0])
    for a in actions:
        act = bpy.data.actions.new(a)
        act.use_fake_user = True
    return save("rig_ok" if weighted else "rig_bad")


def ids(result):
    return {i["check"] for i in result["items"] if i["level"] == "error"}


def main():
    rules_path = os.path.join(TMP, "rules.json")
    with open(rules_path, "w", encoding="utf-8") as f:
        json.dump(RULES, f)
    cases = [
        ("tốt", make_good(), "st", set(), True),
        ("tốt nhưng đang ẩn", make_hidden(), "st", set(), True),
        ("tốt, nhập từ GLB", make_glb(), "st", set(), True),
        ("lỗi", make_bad(), "st",
         {"transform_applied", "height", "boundary_edges", "loose_verts", "inconsistent_normals",
          "uv_missing", "material_empty_slot"}, False),
        ("rig tốt", make_rig(True), "st_rig", set(), True),
        ("rig thiếu weight + animation", make_rig(False, actions=("Idle",)), "st_rig",
         {"unweighted_verts", "action_missing"}, False),
    ]
    failed = 0
    for label, path, cls, expect, exact in cases:
        res = ca.run({"input": path, "class": cls, "rules": rules_path, "export_dir": os.path.join(TMP, "export")})
        got = ids(res)
        ok = (got == expect) if exact else expect <= got
        failed += not ok
        print("%s %-30s mong đợi %s | thấy %s" % ("OK  " if ok else "FAIL", label, sorted(expect), sorted(got)))
        if not ok:
            for it in res["items"]:
                print("     ", it)
    print("SELFTEST:", "ĐẠT" if not failed else "%d ca KHÔNG ĐẠT" % failed)
    sys.exit(1 if failed else 0)


main()
