"""
Drowned - render em lote (SCRUM-154): manifesto -> spritesheets em Assets/Sprites.

    blender -b --python tools/blender/render_batch.py -- --manifest tools/blender/manifest.json
    blender -b --python tools/blender/render_batch.py -- --only Housing_L1
    blender -b --python tools/blender/render_batch.py -- --skip-existing

Cada item do manifesto vira `Assets/Sprites/<category>/<name>/<name>_sheet.png` + `.json`.
No Unity, `SpriteSheetPostprocessor` le o .json e configura o import (fatiamento, pivo, PPU)
sem ninguem abrir o Sprite Editor.

Formato do manifesto (tools/blender/manifest.json):

{
  "defaults": { "size": 256, "directions": 4, "frames": "1", "orthoScale": 3.2,
                "groundAnchor": 0.25, "samples": 32 },
  "items": [
    { "name": "Housing_L1", "category": "Structures", "generator": "placeholder:housing_1" },
    { "name": "Moradia_N1", "category": "Structures", "model": "modelos/moradia_n1.blend" },
    { "name": "Inhabitant", "category": "Characters", "generator": "placeholder:inhabitant",
      "size": 128, "orthoScale": 1.6 }
  ]
}

Um item usa `model` (arquivo .blend/.fbx/.obj/.glb) OU `generator` (placeholder:<id> de
placeholder_models.py). Campos opcionais sobrescrevem `defaults`. Caminhos relativos sao
resolvidos a partir da pasta do manifesto.

Uma falha em um item nao derruba o lote: o resumo no fim lista o que deu certo e o que falhou,
e o processo sai com codigo 1 se algo falhou.
"""

import argparse
import math
import json
import os
import shutil
import sys
import traceback

import bpy

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

import render_sprites  # noqa: E402
import placeholder_models  # noqa: E402

DEFAULTS = {
    "size": 256,
    "directions": 4,
    "frames": "1",
    "orthoScale": None,
    "groundAnchor": 0.25,
    "samples": 32,
    "elevation": render_sprites.ISO_ELEVATION_DEG,
    "azimuth": render_sprites.ISO_AZIMUTH_DEG,
}


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser(description="Renderiza todos os modelos do manifesto.")
    p.add_argument("--manifest", default=os.path.join(SCRIPT_DIR, "manifest.json"))
    p.add_argument("--only", action="append", default=[], help="Renderiza so este nome (pode repetir)")
    p.add_argument("--skip-existing", action="store_true", help="Pula itens que ja tem sheet em Assets/Sprites")
    p.add_argument("--out", default=None, help="Pasta temporaria de frames (padrao: tools/blender/out)")
    p.add_argument("--assets", default=None, help="Pasta Assets/Sprites de destino (padrao: <repo>/Assets/Sprites)")
    return p.parse_args(argv)


def repo_root():
    return os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))


def load_manifest(path):
    with open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)
    defaults = dict(DEFAULTS)
    defaults.update(data.get("defaults", {}))
    items = []
    for raw in data.get("items", []):
        item = dict(defaults)
        item.update(raw)
        if "name" not in item or "category" not in item:
            raise ValueError(f"Item sem name/category: {raw}")
        if not item.get("model") and not item.get("generator") and not item.get("parts"):
            raise ValueError(f"Item {item['name']} precisa de 'model', 'generator' ou 'parts'")
        items.append(item)
    return items


def load_objects(item, manifest_dir):
    """Carrega o item: uma peça só (model/generator) ou várias (parts). Depois aplica rotate/fit."""
    if item.get("parts"):
        objects = []
        for i, part in enumerate(item["parts"]):
            objects += place_part(load_single(part, manifest_dir), part, f"Part{i}")
    else:
        objects = load_single(item, manifest_dir)
    return normalize(objects, item)


def top_level(objects):
    return [o for o in objects if o.parent is None or o.parent not in objects]


def place_part(objects, part, name):
    """
    Agrupa a peça sob um Empty no centro da base dela e aplica scale e rotate (graus em Z) em torno desse ponto,
    depois desloca por offset [x, y, z]. Assim escalar uma peça não a tira do lugar.
    glassMaterials: nomes de material desta peça que viram vidro translúcido (ex.: ["dark"] no domo da Kenney).
    """
    if part.get("glassMaterials"):
        render_sprites.glass_slots(objects, part["glassMaterials"])
    b = mesh_bounds(objects)
    cx, cy, cz = (((b[0][0] + b[1][0]) * 0.5, (b[0][1] + b[1][1]) * 0.5, b[0][2]) if b else (0.0, 0.0, 0.0))
    empty = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(empty)
    empty.location = (cx, cy, cz)
    bpy.context.view_layer.update()
    inverse = empty.matrix_world.inverted()
    for o in top_level(objects):
        o.parent = empty
        o.matrix_parent_inverse = inverse
    off = part.get("offset", [0.0, 0.0, 0.0])
    empty.location = (cx + float(off[0]), cy + float(off[1]), cz + float(off[2]))
    empty.rotation_euler = (0.0, 0.0, math.radians(float(part.get("rotate", 0.0))))
    sc = float(part.get("scale", 1.0))
    empty.scale = (sc, sc, sc)
    return objects + [empty]


def mesh_bounds(objects):
    bpy.context.view_layer.update()
    from mathutils import Vector
    lo = [float("inf")] * 3
    hi = [float("-inf")] * 3
    for o in objects:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            for k in range(3):
                lo[k] = min(lo[k], w[k])
                hi[k] = max(hi[k], w[k])
    if lo[0] == float("inf"):
        return None
    return lo, hi


def normalize(objects, item):
    """
    rotate: gira o item inteiro em Z (graus), para a frente do modelo apontar para -Y (convenção do README).
    fit: [largura, profundidade] em células; escala para caber com folga e centraliza na origem, base em Z = 0.
    fitHeight: altura em unidades (para personagens); usado se não houver fit.
    scale + anchor: escala fixa e ponto [x, y] (nas coordenadas do modelo) que vai para a origem; para itens que
    precisam da mesma escala entre si (etapas do Monumento), em vez de cada um ser esticado até a pegada.
    """
    if not any(item.get(k) for k in ("rotate", "fit", "fitHeight", "scale")):
        return objects
    root = bpy.data.objects.new("ItemRoot", None)
    bpy.context.scene.collection.objects.link(root)
    for o in top_level(objects):
        o.parent = root
    root.rotation_euler = (0.0, 0.0, math.radians(float(item.get("rotate", 0.0))))
    bounds = mesh_bounds(objects)
    if bounds is None:
        return objects + [root]
    lo, hi = bounds
    s = 1.0
    if item.get("fit"):
        fw, fd = float(item["fit"][0]), float(item["fit"][1])
        margin = float(item.get("fitMargin", 0.9))
        s = min(fw * margin / max(hi[0] - lo[0], 1e-6), fd * margin / max(hi[1] - lo[1], 1e-6))
        if item.get("maxHeight"):
            s = min(s, float(item["maxHeight"]) / max(hi[2] - lo[2], 1e-6))
    elif item.get("fitHeight"):
        s = float(item["fitHeight"]) / max(hi[2] - lo[2], 1e-6)
    elif item.get("scale"):
        s = float(item["scale"])
    cx, cy = (lo[0] + hi[0]) * 0.5, (lo[1] + hi[1]) * 0.5
    if item.get("anchor"):
        cx, cy = float(item["anchor"][0]), float(item["anchor"][1])
    root.scale = (s, s, s)
    root.location = (-cx * s, -cy * s, -lo[2] * s)
    return objects + [root]


def load_single(item, manifest_dir):
    if item.get("generator"):
        kind, _, gen_id = item["generator"].partition(":")
        if kind != "placeholder":
            raise ValueError(f"Gerador desconhecido: {item['generator']}")
        return placeholder_models.build(gen_id)
    model = item["model"]
    if not os.path.isabs(model):
        model = os.path.join(manifest_dir, model)
    if not os.path.exists(model):
        raise FileNotFoundError(model)
    return render_sprites.import_model(os.path.abspath(model))


def render_item(item, manifest_dir, out_root):
    name = item["name"]
    scene = render_sprites.reset_scene()
    placeholder_models._materials.clear()
    render_sprites.setup_render(scene, int(item["size"]), int(item["samples"]))
    render_sprites.setup_lights(scene)
    objects = load_objects(item, manifest_dir)
    frames = render_sprites.parse_frames(str(item["frames"]))
    out_dir = os.path.join(out_root, name)
    return render_sprites.render_objects(
        scene, objects, out_dir, name,
        size=int(item["size"]), directions=int(item["directions"]), frames=frames,
        ortho_scale=item.get("orthoScale"), elevation=float(item["elevation"]),
        azimuth=float(item["azimuth"]), ground_anchor=item.get("groundAnchor"))


def copy_to_assets(sheet_path, item, assets_root):
    name = item["name"]
    dest_dir = os.path.join(assets_root, item["category"], name)
    os.makedirs(dest_dir, exist_ok=True)
    src_dir = os.path.dirname(sheet_path)
    for ext in (".png", ".json"):
        shutil.copyfile(os.path.join(src_dir, f"{name}_sheet{ext}"), os.path.join(dest_dir, f"{name}_sheet{ext}"))
    return dest_dir


def main():
    args = parse_args()
    manifest_path = os.path.abspath(args.manifest)
    manifest_dir = os.path.dirname(manifest_path)
    out_root = args.out or os.path.join(SCRIPT_DIR, "out")
    assets_root = args.assets or os.path.join(repo_root(), "Assets", "Sprites")

    items = load_manifest(manifest_path)
    if args.only:
        wanted = set(args.only)
        items = [i for i in items if i["name"] in wanted]
        missing = wanted - {i["name"] for i in items}
        if missing:
            print(f"[render_batch] AVISO: nomes nao encontrados no manifesto: {', '.join(sorted(missing))}")

    ok, skipped, failed = [], [], []
    for item in items:
        name = item["name"]
        dest = os.path.join(assets_root, item["category"], name, f"{name}_sheet.png")
        if args.skip_existing and os.path.exists(dest):
            skipped.append(name)
            print(f"[render_batch] pulando {name} (ja existe)")
            continue
        print(f"[render_batch] >>> {name}")
        try:
            sheet = render_item(item, manifest_dir, out_root)
            dest_dir = copy_to_assets(sheet, item, assets_root)
            ok.append(name)
            print(f"[render_batch] OK {name} -> {dest_dir}")
        except Exception as exc:  # noqa: BLE001 - queremos seguir o lote
            failed.append((name, f"{type(exc).__name__}: {exc}"))
            traceback.print_exc()
            print(f"[render_batch] FALHOU {name}: {exc}")

    print("\n[render_batch] ===== resumo =====")
    print(f"  ok:      {len(ok)}  {', '.join(ok)}")
    print(f"  pulados: {len(skipped)}  {', '.join(skipped)}")
    print(f"  falhas:  {len(failed)}")
    for name, err in failed:
        print(f"    - {name}: {err}")
    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
