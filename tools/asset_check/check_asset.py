"""Kiểm tra chất lượng asset 3D (topology, tỉ lệ, UV, vật liệu, rig, export) — chạy trong Blender headless.

  blender -b --factory-startup --python tools/asset_check/check_asset.py -- \
      --input <file.blend|.fbx|.glb> --class <tên nhóm trong rules.json> \
      [--objects A,B] [--rules tools/asset_check/rules.json] [--report report.json] [--export-dir DIR]

-> exit 0 nếu không có lỗi (error), exit 1 nếu có lỗi, exit 2 nếu sai tham số.
Ngưỡng nằm trong rules.json (chỉnh được, không sửa code). Quyết định: design/decisions/0006-cong-cu-kiem-tra-asset.md.
Không dựa vào MCP: cùng một file + cùng rules luôn cho cùng kết quả.
"""
import json, math, os, shutil, subprocess, sys, tempfile

import bpy, bmesh
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
EPS_AREA = 1e-10


class Report:
    def __init__(self):
        self.items = []

    def add(self, level, check, message, obj=None, value=None, limit=None):
        self.items.append({"level": level, "check": check, "object": obj,
                           "message": message, "value": value, "limit": limit})

    def count(self, level):
        return sum(1 for i in self.items if i["level"] == level)


# ---------- nạp file ----------

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def load_input(path):
    ext = os.path.splitext(path)[1].lower()
    if ext == ".blend":
        bpy.ops.wm.open_mainfile(filepath=path)
    else:
        clear_scene()
        if ext == ".fbx":
            bpy.ops.import_scene.fbx(filepath=path)
        elif ext in (".glb", ".gltf"):
            bpy.ops.import_scene.gltf(filepath=path)
        else:
            raise ValueError("định dạng không hỗ trợ: " + ext)


def target_objects(names):
    if names:
        missing = [n for n in names if n not in bpy.data.objects]
        if missing:
            raise ValueError("không thấy object: " + ", ".join(missing))
        return [bpy.data.objects[n] for n in names if bpy.data.objects[n].type == "MESH"]
    return [o for o in bpy.data.objects if o.type == "MESH" and not o.hide_render]


def world_bmesh(obj, weld=False):
    """bmesh của mesh sau modifier, đã đổi sang toạ độ thế giới.
    weld=True: hàn đỉnh trùng vị trí (FBX/glTF tách đỉnh ở đường nối UV/vật liệu) — trả thêm số đỉnh đã hàn."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.transform(obj.matrix_world)
    ev.to_mesh_clear()
    if not weld:
        return bm
    n = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    return bm, n - len(bm.verts)


def bbox_world(objs):
    lo = Vector((math.inf,) * 3); hi = Vector((-math.inf,) * 3)
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    return lo, hi


# ---------- từng nhóm kiểm tra ----------

def check_scene_units(rep, rules):
    u = bpy.context.scene.unit_settings
    want = rules["units"]
    if u.system != want["system"] or abs(u.scale_length - want["scale_length"]) > 1e-6:
        rep.add("error", "units", "đơn vị cảnh khác quy ước (1 đơn vị = 1 m)",
                value=[u.system, u.scale_length], limit=[want["system"], want["scale_length"]])


def check_topology(rep, obj, bm, cls):
    t = cls["topology"]
    tris = sum(len(f.verts) - 2 for f in bm.faces)
    if tris > cls["tris_max"]:
        rep.add("error", "tris_max", "quá ngân sách tam giác", obj.name, tris, cls["tris_max"])
    non_manifold = sum(1 for e in bm.edges if not e.is_manifold and not e.is_boundary)
    boundary = sum(1 for e in bm.edges if e.is_boundary)
    loose_verts = sum(1 for v in bm.verts if not v.link_edges)
    wire_edges = sum(1 for e in bm.edges if not e.link_faces)
    ngons = sum(1 for f in bm.faces if len(f.verts) > 4)
    degenerate = sum(1 for f in bm.faces if f.calc_area() < EPS_AREA)
    # pháp tuyến không nhất quán: hai mặt kề đi qua cạnh chung cùng một chiều
    flipped = 0
    for e in bm.edges:
        if len(e.link_loops) == 2:
            a, b = e.link_loops
            if a.vert == b.vert:
                flipped += 1
    for key, val, msg in (("non_manifold", non_manifold, "cạnh non-manifold (>2 mặt)"),
                          ("loose_verts", loose_verts, "đỉnh rời"),
                          ("wire_edges", wire_edges, "cạnh không thuộc mặt nào"),
                          ("degenerate_faces", degenerate, "mặt diện tích ~0"),
                          ("inconsistent_normals", flipped, "cạnh có pháp tuyến hai bên ngược nhau")):
        if val > t.get(key + "_max", 0):
            rep.add("error", key, msg, obj.name, val, t.get(key + "_max", 0))
    if t.get("closed") and boundary:
        rep.add("error", "boundary_edges", "mesh phải kín nhưng có cạnh hở", obj.name, boundary, 0)
    if ngons > t.get("ngons_max", 0):
        rep.add("warn", "ngons", "có n-gon (>4 cạnh) — khi xuất sẽ bị chia tam giác tự động", obj.name, ngons, t.get("ngons_max", 0))
    if t.get("closed") and not boundary and not non_manifold and not flipped:
        vol = bm.calc_volume(signed=True)
        if vol < 0:
            rep.add("error", "normals_inward", "pháp tuyến quay vào trong", obj.name, round(vol, 4), "> 0")
    return tris


def check_transform(rep, obj, cls, is_blend):
    """Chỉ chặt với file nguồn .blend. FBX/glTF: trình nhập tự thêm xoay đổi trục vào object -> chỉ ghi info;
    sai tỉ lệ thật vẫn bị bắt ở kiểm tra kích thước (toạ độ thế giới)."""
    if not cls.get("transform_applied", True):
        return
    r = obj.rotation_euler; s = obj.scale
    if any(abs(a) > 1e-4 for a in r) or any(abs(a - 1) > 1e-4 for a in s):
        rep.add("error" if is_blend else "info", "transform_applied", "rotation/scale chưa apply (phải là 0 / 1)", obj.name,
                [round(math.degrees(a), 2) for a in r] + [round(a, 4) for a in s], "rot 0, scale 1")


def check_size_pivot(rep, objs, cls):
    lo, hi = bbox_world(objs)
    size = hi - lo
    h = cls.get("height")
    if h and not (h[0] <= size.z <= h[1]):
        rep.add("error", "height", "chiều cao ngoài khoảng của nhóm", None, round(size.z, 3), h)
    fp = cls.get("footprint_max")
    if fp and (size.x > fp[0] + 1e-4 or size.y > fp[1] + 1e-4):
        rep.add("error", "footprint", "rộng/sâu vượt khung chiếm chỗ", None, [round(size.x, 3), round(size.y, 3)], fp)
    if cls.get("pivot") == "feet":
        tol = cls.get("pivot_tol", 0.02)
        centre = (lo + hi) / 2
        for o in objs:
            p = o.matrix_world.translation
            off = [round(p.x - centre.x, 3), round(p.y - centre.y, 3), round(p.z - lo.z, 3)]
            if any(abs(v) > tol for v in off):
                rep.add("error", "pivot", "gốc object phải ở giữa đáy (chân)", o.name, off, tol)
    return [round(v, 4) for v in size]


def check_uv(rep, obj, bm, cls):
    u = cls["uv"]
    lay = bm.loops.layers.uv.active
    if not lay:
        if u.get("required"):
            rep.add("error", "uv_missing", "không có UV map", obj.name)
        return
    outside = total = zero = 0
    uv_area = world_area = 0.0
    for f in bm.faces:
        pts = [l[lay].uv.copy() for l in f.loops]
        for p in pts:
            total += 1
            if not (-1e-4 <= p.x <= 1 + 1e-4 and -1e-4 <= p.y <= 1 + 1e-4):
                outside += 1
        a = 0.0
        for i in range(len(pts)):
            x1, y1 = pts[i]; x2, y2 = pts[(i + 1) % len(pts)]
            a += x1 * y2 - x2 * y1
        a = abs(a) / 2
        if a < EPS_AREA:
            zero += 1
        uv_area += a; world_area += f.calc_area()
    if u.get("inside_0_1") and outside:
        rep.add("error", "uv_outside", "toạ độ UV ngoài 0–1", obj.name, outside, 0)
    if zero > u.get("zero_area_faces_max", 0):
        rep.add("warn", "uv_zero_area", "mặt có UV diện tích ~0 (texture bị kéo)", obj.name, zero, u.get("zero_area_faces_max", 0))
    td = u.get("texel_density")
    if td and world_area > 0 and uv_area > 0:
        dens = math.sqrt(uv_area / world_area) * td["texture_px"]
        if not (td["min"] <= dens <= td["max"]):
            rep.add("warn", "texel_density", "mật độ texel (px/m) ngoài khoảng", obj.name, round(dens, 1), [td["min"], td["max"]])


def check_materials(rep, obj, rules):
    m = rules["materials"]
    slots = obj.material_slots
    if len(slots) > m["max_per_object"]:
        rep.add("error", "material_count", "quá nhiều vật liệu", obj.name, len(slots), m["max_per_object"])
    if not slots:
        rep.add("error", "material_missing", "không có vật liệu", obj.name)
    for s in slots:
        mat = s.material
        if mat is None:
            rep.add("error", "material_empty_slot", "ô vật liệu trống", obj.name)
            continue
        if not mat.use_nodes or not any(n.type == m["shader"] for n in mat.node_tree.nodes):
            rep.add("error", "material_shader", "vật liệu không dùng shader quy ước", obj.name, mat.name, m["shader"])
            continue
        for n in mat.node_tree.nodes:
            if n.type != "TEX_IMAGE":
                continue
            img = n.image
            if img is None or img.size[0] == 0:
                rep.add("error", "texture_missing", "texture thiếu file/ảnh", obj.name, mat.name)
                continue
            w, h = img.size
            if max(w, h) > m["texture_max_px"]:
                rep.add("error", "texture_size", "texture quá lớn", obj.name, [w, h], m["texture_max_px"])
            if m.get("power_of_two") and (w & (w - 1) or h & (h - 1)):
                rep.add("warn", "texture_pot", "kích thước texture không phải luỹ thừa của 2", obj.name, [w, h])


def check_rig(rep, objs, cls, rules):
    want = cls.get("rig", "none")
    arms = {o.find_armature() for o in objs} - {None}
    if want == "none":
        return 0
    if not arms:
        rep.add("error", "rig_missing", "nhóm này cần rig nhưng không có armature", None)
        return 0
    r = rules["rig"]
    bones = 0
    for arm in arms:
        deform = {b.name for b in arm.data.bones if b.use_deform}
        bones += len(deform)
        if len(deform) > r["max_bones"]:
            rep.add("error", "bones_max", "quá nhiều xương deform", arm.name, len(deform), r["max_bones"])
        for o in objs:
            if o.find_armature() is not arm:
                continue
            gi = {g.index for g in o.vertex_groups if g.name in deform}
            unweighted = over = 0
            for v in o.data.vertices:
                n = sum(1 for g in v.groups if g.group in gi and g.weight > 1e-4)
                if n == 0:
                    unweighted += 1
                if n > r["max_influences"]:
                    over += 1
            if unweighted:
                rep.add("error", "unweighted_verts", "đỉnh không có xương nào điều khiển", o.name, unweighted, 0)
            if over:
                rep.add("error", "max_influences", "đỉnh chịu ảnh hưởng của quá nhiều xương", o.name, over, r["max_influences"])
    names = [a.name.lower() for a in bpy.data.actions]
    for req in r.get("required_actions", []):
        if not any(req in n for n in names):
            rep.add("error", "action_missing", "thiếu animation", None, req, [a.name for a in bpy.data.actions])
    return bones


def stats(objs):
    tris = 0
    for o in objs:
        bm = world_bmesh(o)
        tris += sum(len(f.verts) - 2 for f in bm.faces)
        bm.free()
    lo, hi = bbox_world(objs) if objs else (Vector(), Vector())
    arms = {o.find_armature() for o in objs} - {None}
    return {"tris": tris, "size": [round(v, 4) for v in (hi - lo)],
            "bones": sum(len(a.data.bones) for a in arms),
            "actions": sorted(a.name for a in bpy.data.actions)}


def check_export(rep, args, names, cls, rules, before):
    e = rules["export"]
    out_dir = args.get("export_dir") or tempfile.mkdtemp(prefix="asset_check_")
    os.makedirs(out_dir, exist_ok=True)
    base = os.path.splitext(os.path.basename(args["input"]))[0]
    results = {}
    for fmt in e["formats"]:
        load_input(args["input"])
        objs = target_objects(names)
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs + [a for a in {o.find_armature() for o in objs} if a]:
            o.hide_viewport = False          # object ẩn thì không chọn được -> file xuất rỗng
            o.hide_set(False)
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        path = os.path.join(out_dir, base + "." + fmt)
        if fmt == "fbx":
            bpy.ops.export_scene.fbx(filepath=path, use_selection=True, **e["fbx_options"])
        else:
            bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB")
        clear_scene()
        if fmt == "fbx":
            bpy.ops.import_scene.fbx(filepath=path)
        else:
            bpy.ops.import_scene.gltf(filepath=path)
        # trình nhập glTF tạo mesh làm hình hiển thị xương (custom shape) — không phải asset
        shapes = {pb.custom_shape for o in bpy.data.objects if o.type == "ARMATURE"
                  for pb in o.pose.bones if pb.custom_shape}
        after = stats([o for o in bpy.data.objects if o.type == "MESH" and o not in shapes])
        results[fmt] = {"file": path, "bytes": os.path.getsize(path), "after": after}
        if after["tris"] == 0:
            rep.add("error", "export_empty", fmt + ": file xuất không có mesh", None, 0, before["tris"])
        elif after["tris"] != before["tris"]:
            rep.add("error", "export_tris", fmt + ": số tam giác đổi sau khi xuất–nhập lại", None, after["tris"], before["tris"])
        if any(abs(a - b) > e["size_tol"] for a, b in zip(after["size"], before["size"])):
            rep.add("error", "export_size", fmt + ": kích thước/trục đổi sau khi xuất–nhập lại", None, after["size"], before["size"])
        if cls.get("rig", "none") != "none" and after["bones"] < before["bones"]:
            rep.add("error", "export_bones", fmt + ": mất xương sau khi xuất", None, after["bones"], before["bones"])
        if fmt == "glb":
            exe = shutil.which("gltf_validator")
            if exe:
                subprocess.run([exe, "-o", path], capture_output=True)
                rpt = path + ".report.json"
                if os.path.exists(rpt):
                    with open(rpt, encoding="utf-8") as f:
                        issues = json.load(f).get("issues", {})
                    results[fmt]["gltf_validator"] = {k: issues.get(k) for k in ("numErrors", "numWarnings")}
                    if issues.get("numErrors"):
                        rep.add("error", "gltf_validator", "glTF-Validator báo lỗi", None, issues.get("numErrors"), 0)
            else:
                rep.add("info", "gltf_validator", "bỏ qua: chưa cài gltf_validator (Khronos)")
    return results


# ---------- chạy ----------

def run(args):
    with open(args.get("rules") or os.path.join(HERE, "rules.json"), encoding="utf-8") as f:
        rules = json.load(f)
    if args["class"] not in rules["classes"]:
        raise ValueError("không có nhóm '%s' trong rules (có: %s)" % (args["class"], ", ".join(rules["classes"])))
    cls = rules["classes"][args["class"]]
    names = [n for n in (args.get("objects") or "").split(",") if n]
    rep = Report()
    load_input(args["input"])
    objs = target_objects(names)
    if not objs:
        raise ValueError("không có mesh nào để kiểm tra")
    check_scene_units(rep, rules)
    per_obj = {}
    is_blend = args["input"].lower().endswith(".blend")
    for o in objs:
        bm, doubles = world_bmesh(o, weld=True)
        if is_blend and doubles:
            rep.add("warn", "duplicate_verts", "đỉnh trùng vị trí (chưa merge)", o.name, doubles, 0)
        tris = check_topology(rep, o, bm, cls)
        check_uv(rep, o, bm, cls)
        bm.free()
        check_transform(rep, o, cls, is_blend)
        check_materials(rep, o, rules)
        per_obj[o.name] = {"tris": tris}
    size = check_size_pivot(rep, objs, cls)
    bones = check_rig(rep, objs, cls, rules)
    before = stats(objs)
    export = check_export(rep, args, [o.name for o in objs], cls, rules, before) if not args.get("no_export") else {}
    return {"input": args["input"], "class": args["class"], "objects": per_obj, "size": size, "bones": bones,
            "stats": before, "export": export,
            "summary": {"errors": rep.count("error"), "warnings": rep.count("warn")}, "items": rep.items}


def parse(argv):
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    out = {}
    i = 0
    while i < len(argv):
        k = argv[i].lstrip("-").replace("-", "_")
        if k == "no_export":
            out[k] = True; i += 1
        else:
            out[k] = argv[i + 1]; i += 2
    return out


def main():
    try:
        args = parse(sys.argv)
        if "input" not in args or "class" not in args:
            print(__doc__); sys.exit(2)
        args["input"] = os.path.abspath(args["input"])
        result = run(args)
    except ValueError as e:
        print("LỖI THAM SỐ:", e); sys.exit(2)
    if args.get("report"):
        with open(args["report"], "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
    for it in result["items"]:
        print("[%s] %s%s: %s (giá trị %s, giới hạn %s)" % (it["level"], it["check"],
              " @" + it["object"] if it["object"] else "", it["message"], it["value"], it["limit"]))
    s = result["summary"]
    print("KẾT QUẢ: %d lỗi, %d cảnh báo — %s" % (s["errors"], s["warnings"], "KHÔNG ĐẠT" if s["errors"] else "ĐẠT"))
    sys.exit(1 if s["errors"] else 0)


if __name__ == "__main__":
    main()
