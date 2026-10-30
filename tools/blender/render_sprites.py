"""
Drowned - projeto 3D auxiliar: renderiza modelos em sprites isometricos.

Roda sem interface:

    blender -b --python tools/blender/render_sprites.py -- --test-cube
    blender -b --python tools/blender/render_sprites.py -- --model modelos/moradia.blend --name Moradia
    blender -b --python tools/blender/render_sprites.py -- --model modelos/habitante.fbx --name Habitante --size 128 --frames 1-16

O que o script faz:
  1. Abre uma cena vazia e monta a cena de render (SCRUM-116): camera ortografica
     isometrica, luz principal + luz de preenchimento, fundo transparente.
  2. Importa o modelo (.blend, .fbx, .obj, .glb/.gltf) ou cria um cubo de teste (SCRUM-117).
  3. Gira o modelo nas N direcoes (padrao 4: S, W, N, E) e renderiza um PNG por direcao
     e por frame (SCRUM-154).
  4. Monta um spritesheet horizontal (linhas = direcoes, colunas = frames) e grava
     um .json com o layout para o importador do Unity.

Saida padrao: tools/blender/out/<Nome>/
  <Nome>_dir0_f000.png ...   frames individuais
  <Nome>_sheet.png            spritesheet
  <Nome>_sheet.json           {"frameSize":[w,h],"directions":4,"frames":n,"order":["S","W","N","E"]}

Depois copie a pasta para Assets/Sprites/<Categoria>/ no projeto Unity e importe o sheet
como Sprite (2D and UI) com Sprite Mode = Multiple, Pixels Per Unit conforme o README.
"""

import argparse
import json
import math
import os
import sys

import bpy
import numpy as np

# ---------------------------------------------------------------------------
# Configuracao padrao da camera isometrica (SCRUM-115)
# ---------------------------------------------------------------------------
# Elevacao de 30 graus acima do horizonte (rotacao X = 60) + giro de 45 graus em Z
# produz a projecao "2:1" classica de city-builders (losango duas vezes mais largo
# que alto). Para isometrico verdadeiro use ISO_ELEVATION_DEG = 35.264.
ISO_ELEVATION_DEG = 30.0
ISO_AZIMUTH_DEG = 45.0
DIRECTION_NAMES = {4: ["S", "W", "N", "E"], 8: ["S", "SW", "W", "NW", "N", "NE", "E", "SE"]}


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1 :] if "--" in argv else []
    p = argparse.ArgumentParser(description="Renderiza um modelo em sprites isometricos.")
    p.add_argument("--model", help="Caminho do modelo (.blend, .fbx, .obj, .glb, .gltf)")
    p.add_argument("--name", help="Nome base dos arquivos de saida (padrao: nome do modelo)")
    p.add_argument("--test-cube", action="store_true", help="Ignora --model e renderiza um cubo de teste")
    p.add_argument("--out", default=None, help="Pasta de saida (padrao: tools/blender/out)")
    p.add_argument("--size", type=int, default=256, help="Lado do frame em pixels (quadrado)")
    p.add_argument("--directions", type=int, default=4, choices=[4, 8], help="Numero de direcoes")
    p.add_argument("--frames", default="1", help="Frames de animacao, ex.: '1' ou '1-16'")
    p.add_argument("--ortho-scale", type=float, default=None,
                   help="Escala ortografica da camera. Padrao: ajusta ao tamanho do modelo")
    p.add_argument("--samples", type=int, default=32, help="Samples do EEVEE")
    p.add_argument("--elevation", type=float, default=ISO_ELEVATION_DEG)
    p.add_argument("--azimuth", type=float, default=ISO_AZIMUTH_DEG)
    return p.parse_args(argv)


# ---------------------------------------------------------------------------
# Cena de render (SCRUM-116)
# ---------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    return bpy.context.scene


def setup_render(scene, size, samples):
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = samples
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.image_settings.compression = 15
    # Visual limpo para sprite: sem filtro de cor cinematografico
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    # Mundo neutro e escuro para nao vazar luz ambiente colorida nos sprites
    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.05, 0.08, 0.12, 1.0)
        bg.inputs[1].default_value = 0.3
    scene.world = world


def setup_camera(scene, elevation_deg, azimuth_deg, ortho_scale):
    cam_data = bpy.data.cameras.new("IsoCamera")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_scale
    cam_data.clip_start = 0.01
    cam_data.clip_end = 1000.0
    cam = bpy.data.objects.new("IsoCamera", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    # Camera olhando para a origem: rotacao X = 90 - elevacao, Z = azimute
    rot_x = math.radians(90.0 - elevation_deg)
    rot_z = math.radians(azimuth_deg)
    cam.rotation_euler = (rot_x, 0.0, rot_z)
    dist = 50.0
    # Posicao = origem - direcao de visao * dist
    dir_x = math.sin(rot_z) * math.cos(math.radians(elevation_deg))
    dir_y = -math.cos(rot_z) * math.cos(math.radians(elevation_deg))
    dir_z = math.sin(math.radians(elevation_deg))
    cam.location = (dir_x * dist, dir_y * dist, dir_z * dist)
    return cam


def setup_lights(scene):
    # Luz principal (sol) vindo de cima-esquerda-frente, sombras suaves
    key_data = bpy.data.lights.new("KeyLight", type="SUN")
    key_data.energy = 4.0
    key_data.angle = math.radians(10.0)
    key = bpy.data.objects.new("KeyLight", key_data)
    key.rotation_euler = (math.radians(50.0), math.radians(-15.0), math.radians(70.0))
    scene.collection.objects.link(key)
    # Luz de preenchimento azulada (ambiente subaquatico)
    fill_data = bpy.data.lights.new("FillLight", type="SUN")
    fill_data.energy = 1.2
    fill_data.color = (0.6, 0.8, 1.0)
    fill = bpy.data.objects.new("FillLight", fill_data)
    fill.rotation_euler = (math.radians(60.0), math.radians(20.0), math.radians(-120.0))
    scene.collection.objects.link(fill)


# ---------------------------------------------------------------------------
# Modelo
# ---------------------------------------------------------------------------
def make_test_cube(scene):
    bpy.ops.mesh.primitive_cube_add(size=2.0, location=(0.0, 0.0, 1.0))
    cube = bpy.context.active_object
    cube.name = "TestCube"
    mat = bpy.data.materials.new("TestCubeMat")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.95, 0.65, 0.2, 1.0)  # ambar do tema
        bsdf.inputs["Roughness"].default_value = 0.6
    cube.data.materials.append(mat)
    # Um "marcador" na face frontal para dar para ver a direcao no sprite
    bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=0.2, location=(0.0, -1.05, 1.0),
                                        rotation=(math.radians(90.0), 0.0, 0.0))
    marker = bpy.context.active_object
    marker.name = "TestCubeMarker"
    mat2 = bpy.data.materials.new("MarkerMat")
    mat2.use_nodes = True
    bsdf2 = mat2.node_tree.nodes.get("Principled BSDF")
    if bsdf2:
        bsdf2.inputs["Base Color"].default_value = (0.1, 0.4, 0.9, 1.0)
    marker.data.materials.append(mat2)
    return [cube, marker]


def import_model(path):
    before = set(bpy.data.objects)
    ext = os.path.splitext(path)[1].lower()
    if ext == ".blend":
        with bpy.data.libraries.load(path, link=False) as (src, dst):
            dst.objects = [n for n in src.objects]
        for obj in dst.objects:
            if obj is not None:
                bpy.context.scene.collection.objects.link(obj)
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=path)
    elif ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        raise SystemExit(f"Formato nao suportado: {ext}")
    imported = [o for o in bpy.data.objects if o not in before]
    # Remove cameras/luzes que vieram junto com o arquivo
    keep = []
    for o in imported:
        if o.type in ("CAMERA", "LIGHT"):
            bpy.data.objects.remove(o, do_unlink=True)
        else:
            keep.append(o)
    if not keep:
        raise SystemExit("Nenhum objeto importado.")
    return keep


def group_under_pivot(objects):
    """Cria um Empty na origem e faz dele o pai de todos os objetos de topo.
    Girar o pivo gira o modelo inteiro em torno do eixo Z."""
    pivot = bpy.data.objects.new("SpritePivot", None)
    bpy.context.scene.collection.objects.link(pivot)
    for o in objects:
        if o.parent is None or o.parent not in objects:
            o.parent = pivot
            o.matrix_parent_inverse = pivot.matrix_world.inverted()
    return pivot


def bounds_radius(objects):
    """Raio do menor circulo (em XY) e altura que contem todos os objetos, no mundo."""
    xs, ys, zs = [], [], []
    for o in objects:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            wx, wy, wz = o.matrix_world @ __import__("mathutils").Vector(c)
            xs.append(wx); ys.append(wy); zs.append(wz)
    if not xs:
        return 1.0, 2.0
    r = max(math.hypot(x, y) for x, y in zip(xs, ys))
    h = max(zs) - min(min(zs), 0.0)
    return r, h


# ---------------------------------------------------------------------------
# Render e spritesheet (SCRUM-154)
# ---------------------------------------------------------------------------
def parse_frames(spec):
    if "-" in spec:
        a, b = spec.split("-")
        return list(range(int(a), int(b) + 1))
    return [int(spec)]


def render_all(scene, pivot, out_dir, name, directions, frames):
    os.makedirs(out_dir, exist_ok=True)
    files = []
    step = 2.0 * math.pi / directions
    for d in range(directions):
        pivot.rotation_euler = (0.0, 0.0, d * step)
        row = []
        for i, f in enumerate(frames):
            scene.frame_set(f)
            path = os.path.join(out_dir, f"{name}_dir{d}_f{i:03d}.png")
            scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            row.append(path)
        files.append(row)
    return files


def build_sheet(files, out_dir, name, size, directions):
    rows = len(files)
    cols = len(files[0])
    sheet = np.zeros((rows * size, cols * size, 4), dtype=np.float32)
    for r, row in enumerate(files):
        for c, path in enumerate(row):
            img = bpy.data.images.load(path)
            px = np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)
            # Blender guarda a imagem de baixo para cima; o sheet e de cima para baixo
            px = np.flipud(px)
            y0 = r * size
            x0 = c * size
            sheet[y0:y0 + size, x0:x0 + size, :] = px
            bpy.data.images.remove(img)
    out_img = bpy.data.images.new(f"{name}_sheet", width=cols * size, height=rows * size, alpha=True)
    out_img.pixels = np.flipud(sheet).ravel().tolist()
    sheet_path = os.path.join(out_dir, f"{name}_sheet.png")
    out_img.filepath_raw = sheet_path
    out_img.file_format = "PNG"
    out_img.save()
    meta = {
        "name": name,
        "frameSize": [size, size],
        "directions": directions,
        "directionOrder": DIRECTION_NAMES[directions],
        "frames": cols,
        "layout": "linhas = direcoes, colunas = frames",
    }
    with open(os.path.join(out_dir, f"{name}_sheet.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)
    return sheet_path


def main():
    args = parse_args()
    if not args.test_cube and not args.model:
        raise SystemExit("Informe --model <arquivo> ou --test-cube")

    script_dir = os.path.dirname(os.path.abspath(__file__))
    out_root = args.out or os.path.join(script_dir, "out")
    name = args.name or ("TestCube" if args.test_cube else os.path.splitext(os.path.basename(args.model))[0])
    out_dir = os.path.join(out_root, name)

    scene = reset_scene()
    setup_render(scene, args.size, args.samples)
    setup_lights(scene)

    objects = make_test_cube(scene) if args.test_cube else import_model(os.path.abspath(args.model))
    pivot = group_under_pivot(objects)

    if args.ortho_scale is None:
        r, h = bounds_radius(objects)
        # Losango isometrico: largura visivel ~ 2r*sqrt(2); altura ~ h*cos(elev) + 2r*sin(elev)
        width = 2.0 * r * math.sqrt(2.0)
        height = h * math.cos(math.radians(args.elevation)) + 2.0 * r * math.sin(math.radians(args.elevation))
        ortho = max(width, height) * 1.15
    else:
        ortho = args.ortho_scale
    cam = setup_camera(scene, args.elevation, args.azimuth, ortho)
    # Centraliza verticalmente: mira no meio da altura do modelo
    _, h = bounds_radius(objects)
    cam.location = (cam.location.x, cam.location.y, cam.location.z + h * 0.5)

    frames = parse_frames(args.frames)
    files = render_all(scene, pivot, out_dir, name, args.directions, frames)
    sheet = build_sheet(files, out_dir, name, args.size, args.directions)
    print(f"[render_sprites] OK: {len(files) * len(files[0])} frames, sheet em {sheet}")


if __name__ == "__main__":
    main()
