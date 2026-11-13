"""
Drowned - quadros da animação de introdução (SCRUM-198).

Monta 5 cenas com os modelos Kenney do jogo (modelos/kenney) e renderiza cada uma em 1920x1080:

    1. A cidade prospera sob a grande cúpula, com o Monumento Central aceso.
    2. Um tremor racha a cúpula: luzes vermelhas, destroços e bolhas.
    3. A cidade engolida; o Monumento vira ruína.
    4. Cinco sobreviventes numa pequena cúpula (Moradia, Gerador, Planta de O2, Fazenda).
    5. A colônia olha para a ruína do Monumento ao longe: "cabe a você".

Uso (sem interface):

    blender -b --python tools/blender/render_intro.py --
    blender -b --python tools/blender/render_intro.py -- --only 2 --percent 25 --samples 16

Saída: Assets/Sprites/Intro/Resources/Intro/intro_1.png ... intro_5.png (o IntroView carrega por Resources).
Com --preview grava em tools/blender/out/Intro/ em vez de Assets.
"""

import argparse
import math
import os
import random
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
KENNEY = os.path.join(HERE, "modelos", "kenney")
REPO = os.path.dirname(os.path.dirname(HERE))
ASSETS_OUT = os.path.join(REPO, "Assets", "Sprites", "Intro", "Resources", "Intro")
PREVIEW_OUT = os.path.join(HERE, "out", "Intro")

sys.path.insert(0, HERE)
import render_sprites  # noqa: E402  (import_model, fix_glass, make_glass)

# Paleta (mesma família do jogo: azul profundo + âmbar)
WATER = (0.03, 0.16, 0.26)
WATER_DARK = (0.01, 0.05, 0.09)
AMBER = (1.0, 0.62, 0.18)
WARM = (1.0, 0.78, 0.5)
RED = (1.0, 0.12, 0.08)
COLD = (0.45, 0.75, 1.0)


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    p = argparse.ArgumentParser(description="Renderiza os quadros da introdução.")
    p.add_argument("--only", type=int, action="append", help="Renderiza só a cena N (1 a 5). Pode repetir.")
    p.add_argument("--percent", type=int, default=100, help="Porcentagem da resolução (prévia rápida: 25)")
    # 16 samples: com 32+ em 1920x1080 a GPU desta máquina estoura o tempo e o quadro sai preto (06/11/2026)
    p.add_argument("--samples", type=int, default=16, help="Samples do EEVEE")
    p.add_argument("--preview", action="store_true", help="Grava em tools/blender/out/Intro em vez de Assets")
    return p.parse_args(argv)


# ---------------------------------------------------------------------------
# Cena, mundo e render
# ---------------------------------------------------------------------------
def reset(samples, percent, water, fog_density):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    ee = scene.eevee
    for attr, value in (("taa_render_samples", samples), ("volumetric_start", 0.5), ("volumetric_end", 140.0),
                        ("volumetric_tile_size", "4"), ("volumetric_samples", 96),
                        ("use_volumetric_shadows", True), ("use_bloom", True), ("use_gtao", True)):
        if hasattr(ee, attr):
            try:
                setattr(ee, attr, value)
            except (TypeError, ValueError):
                pass
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = percent
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 30
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        scene.view_settings.view_transform = "Filmic"

    world = bpy.data.worlds.new("Water")
    world.use_nodes = True
    nt = world.node_tree
    bg = nt.nodes.get("Background")
    bg.inputs[0].default_value = (*water, 1.0)
    bg.inputs[1].default_value = 1.0
    scene.world = world
    scene["fog_density"] = fog_density
    scene["fog_color"] = water
    random.seed(7)
    return scene


def make_fog_cards(scene, cam, color, density, near=6.0, far=110.0, count=18):
    """
    Névoa da água: cartões transparentes perpendiculares à câmera, do perto ao longe. Cada um soma um pouco
    de cor da água, então o que está longe some no azul. Volume de verdade (mundo ou caixa) deixa o quadro preto
    no EEVEE do Blender 5.2, e o Cycles na CPU levaria dezenas de minutos.
    """
    fwd = cam.matrix_world.to_quaternion() @ Vector((0, 0, -1))
    fov = cam.data.angle
    # alpha por cartão tal que a soma ao longe chegue perto de 1 - exp(-density * far)
    alpha = 1.0 - math.exp(-density * (far - near) / count)
    m = bpy.data.materials.new("FogCard")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (0, 0, 0, 1)
    b.inputs["Roughness"].default_value = 1.0
    b.inputs["Specular IOR Level"].default_value = 0.0
    b.inputs["Emission Color"].default_value = (*color, 1.0)
    b.inputs["Emission Strength"].default_value = 1.0
    b.inputs["Alpha"].default_value = alpha
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    m.use_backface_culling = False
    for i in range(count):
        t = i / max(1, count - 1)
        d = near + (far - near) * (t ** 1.4)
        size = 2.0 * d * math.tan(fov / 2.0) * 1.6
        bpy.ops.mesh.primitive_plane_add(size=size, location=cam.location + fwd * d)
        card = bpy.context.active_object
        card.name = f"Fog{i}"
        card.rotation_euler = cam.rotation_euler
        card.data.materials.append(m)
        card.visible_shadow = False
        if hasattr(card, "visible_volume_scatter"):
            card.visible_volume_scatter = False


def light_shaft(at, top_radius=1.0, bottom_radius=5.0, height=40.0, color=COLD, alpha=0.07):
    """Feixe de luz falso: cone aberto, emissivo e translúcido, descendo da superfície."""
    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=bottom_radius, radius2=top_radius, depth=height,
                                    end_fill_type="NOTHING",
                                    location=(at[0], at[1], at[2] + height / 2.0))
    cone = bpy.context.active_object
    cone.name = "Shaft"
    m = bpy.data.materials.new("Shaft")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (0, 0, 0, 1)
    b.inputs["Specular IOR Level"].default_value = 0.0
    b.inputs["Emission Color"].default_value = (*color, 1.0)
    b.inputs["Emission Strength"].default_value = 2.0
    # Some nas bordas do cone (olhando de lado) e embaixo: gradiente pelo ângulo com a câmera e pela altura
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.35
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(lw.outputs["Facing"], inv.inputs[1])
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Position"], sep.inputs[0])
    ramp = nt.nodes.new("ShaderNodeMapRange")
    ramp.inputs["From Min"].default_value = at[2]
    ramp.inputs["From Max"].default_value = at[2] + height * 0.6
    nt.links.new(sep.outputs["Z"], ramp.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    nt.links.new(inv.outputs[0], mul.inputs[0])
    nt.links.new(ramp.outputs["Result"], mul.inputs[1])
    k = nt.nodes.new("ShaderNodeMath")
    k.operation = "MULTIPLY"
    k.inputs[1].default_value = alpha
    nt.links.new(mul.outputs[0], k.inputs[0])
    nt.links.new(k.outputs[0], b.inputs["Alpha"])
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    m.use_backface_culling = False
    cone.data.materials.append(m)
    cone.visible_shadow = False
    return cone


def camera(scene, loc, target, lens=32.0, roll=0.0):
    data = bpy.data.cameras.new("Cam")
    data.lens = lens
    data.clip_end = 400.0
    cam = bpy.data.objects.new("Cam", data)
    scene.collection.objects.link(cam)
    cam.location = loc
    direction = Vector(target) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam.rotation_euler.rotate_axis("Z", math.radians(roll))
    scene.camera = cam
    bpy.context.view_layer.update()
    if scene.get("fog_density", 0.0) > 0.0:
        make_fog_cards(scene, cam, tuple(scene["fog_color"]), scene["fog_density"])
    return cam


def light(scene, kind, loc, energy, color=(1, 1, 1), rot=(0, 0, 0), size=None, spot_deg=None, volume=1.0):
    data = bpy.data.lights.new(kind, type=kind)
    data.energy = energy
    data.color = color
    if hasattr(data, "volume_factor"):
        data.volume_factor = volume
    if size is not None:
        if kind == "SUN":
            data.angle = math.radians(size)
        else:
            data.shadow_soft_size = size
    if spot_deg is not None:
        data.spot_size = math.radians(spot_deg)
        data.spot_blend = 0.6
    obj = bpy.data.objects.new(kind, data)
    obj.location = loc
    obj.rotation_euler = [math.radians(a) for a in rot]
    scene.collection.objects.link(obj)
    return obj


def god_ray(scene, x, y, energy=6000.0, deg=9.0, color=COLD):
    """Spot apontado para baixo, bem alto: com o volume da água vira um feixe de luz."""
    light_shaft((x, y, 0.0), top_radius=40.0 * math.tan(math.radians(deg) / 8.0) + 0.5,
                bottom_radius=40.0 * math.tan(math.radians(deg) / 2.0), color=color)
    return light(scene, "SPOT", (x, y, 40.0), energy, color, rot=(0, 0, 0), size=0.3, spot_deg=deg, volume=3.0)


# ---------------------------------------------------------------------------
# Materiais e primitivas
# ---------------------------------------------------------------------------
def mat(name, color, rough=0.6, emission=0.0, alpha=1.0, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*color, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emission > 0.0:
        b.inputs["Emission Color"].default_value = (*color, 1.0)
        b.inputs["Emission Strength"].default_value = emission
    if alpha < 1.0:
        b.inputs["Alpha"].default_value = alpha
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "BLENDED"
        if hasattr(m, "use_backface_culling"):
            m.use_backface_culling = False
    return m


def sand_material():
    m = bpy.data.materials.new("Sand")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (0.42, 0.40, 0.32, 1.0)
    b.inputs["Roughness"].default_value = 0.95
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.0
    noise.inputs["Detail"].default_value = 8.0
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    nt.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], b.inputs["Normal"])
    return m


def seabed(scene, size=260.0, dunes=0.6):
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=120, y_subdivisions=120, size=size)
    g = bpy.context.active_object
    g.name = "Seabed"
    for v in g.data.vertices:
        x, y = v.co.x, v.co.y
        v.co.z = dunes * (math.sin(x * 0.11) * math.cos(y * 0.09) + 0.5 * math.sin(x * 0.31 + y * 0.17))
        # achata o centro para as construções ficarem no chão
        d = math.hypot(x, y)
        v.co.z *= min(1.0, d / 25.0)
    g.data.materials.append(sand_material())
    bpy.ops.object.shade_smooth()
    return g


def hemisphere(name, radius, at=(0, 0, 0), material=None, holes=0.0, seed=1):
    """Meia esfera (cúpula). holes > 0 apaga essa fração das faces de cima: cúpula rachada."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=radius, location=at)
    o = bpy.context.active_object
    o.name = name
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(o.data)
    rnd = random.Random(seed)
    doomed = [v for v in bm.verts if v.co.z < -0.001]
    bmesh.ops.delete(bm, geom=doomed, context="VERTS")
    if holes > 0.0:
        # Rachadura: um rasgo concentrado num lado do topo
        faces = [f for f in bm.faces if f.calc_center_median().z > radius * 0.35
                 and f.calc_center_median().x > -radius * 0.2 and rnd.random() < holes]
        bmesh.ops.delete(bm, geom=faces, context="FACES")
    bm.to_mesh(o.data)
    bm.free()
    if material:
        o.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return o


def glass(name="DomeGlass", alpha=0.18):
    m = mat(name, (0.55, 0.85, 1.0), rough=0.05, alpha=alpha)
    return m


def dome_frame(radius, at=(0, 0, 0), ribs=10, color=(0.55, 0.6, 0.68)):
    """Arcos metálicos sobre a cúpula: dá para ler o vidro."""
    m = mat("Frame", color, rough=0.4, metal=0.7)
    for i in range(ribs):
        bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=radius * 0.008, major_segments=64,
                                         minor_segments=6, location=at,
                                         rotation=(math.radians(90), 0, math.radians(i * 180.0 / ribs)))
        t = bpy.context.active_object
        t.data.materials.append(m)
    bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=radius * 0.02, major_segments=64,
                                     minor_segments=8, location=at)
    bpy.context.active_object.data.materials.append(m)


def bubbles(count, center, spread, height, size=(0.05, 0.22), seed=3, glow=0.4):
    m = mat("Bubble", (0.8, 0.95, 1.0), rough=0.05, emission=glow, alpha=0.55)
    rnd = random.Random(seed)
    for _ in range(count):
        r = rnd.uniform(*size)
        loc = (center[0] + rnd.uniform(-spread, spread), center[1] + rnd.uniform(-spread, spread),
               center[2] + rnd.uniform(0, height))
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=r, location=loc)
        bpy.context.active_object.data.materials.append(m)


def fish_school(count, center, spread, seed=5, color=(0.02, 0.06, 0.1)):
    """Silhuetas de peixe (elipsoides achatados): vida ao fundo sem modelo novo."""
    m = mat("Fish", color, rough=0.8)
    rnd = random.Random(seed)
    heading = rnd.uniform(0, math.tau)
    for _ in range(count):
        loc = (center[0] + rnd.uniform(-spread, spread), center[1] + rnd.uniform(-spread, spread),
               center[2] + rnd.uniform(-spread * 0.3, spread * 0.3))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.35, location=loc)
        f = bpy.context.active_object
        f.scale = (1.0, 0.25, 0.4)
        f.rotation_euler = (0, 0, heading + rnd.uniform(-0.2, 0.2))
        f.data.materials.append(m)
        bpy.ops.mesh.primitive_cone_add(vertices=3, radius1=0.18, depth=0.3,
                                        location=(loc[0] - 0.4 * math.cos(f.rotation_euler.z),
                                                  loc[1] - 0.4 * math.sin(f.rotation_euler.z), loc[2]),
                                        rotation=(0, math.radians(90), f.rotation_euler.z + math.pi))
        bpy.context.active_object.data.materials.append(m)


# ---------------------------------------------------------------------------
# Modelos Kenney
# ---------------------------------------------------------------------------
def kenney(rel):
    return os.path.join(KENNEY, rel.replace("/", os.sep))


def place(rel, at=(0, 0, 0), height=None, scale=None, rot=0.0, tilt=(0.0, 0.0), sink=0.0):
    """
    Importa um GLB da Kenney, agrupa sob um Empty com a base em at e escala para a altura pedida
    (ou fator fixo). rot = giro em Z (graus); tilt = (x, y) em graus, para estruturas tombadas; sink afunda na areia.
    """
    objects = render_sprites.import_model(kenney(rel))
    bpy.context.view_layer.update()
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in objects:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector((min(lo.x, w.x), min(lo.y, w.y), min(lo.z, w.z)))
            hi = Vector((max(hi.x, w.x), max(hi.y, w.y), max(hi.z, w.z)))
    root = bpy.data.objects.new("Part", None)
    bpy.context.scene.collection.objects.link(root)
    root.location = ((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z)
    bpy.context.view_layer.update()
    for o in objects:
        if o.parent is None or o.parent not in objects:
            o.parent = root
            o.matrix_parent_inverse = root.matrix_world.inverted()
    s = scale if scale is not None else (height / max(hi.z - lo.z, 1e-6) if height else 1.0)
    root.scale = (s, s, s)
    root.location = (at[0], at[1], at[2] - sink)
    root.rotation_euler = (math.radians(tilt[0]), math.radians(tilt[1]), math.radians(rot))
    return root


def glow_orb(at, radius, color=AMBER, strength=12.0, light_energy=1500.0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=radius, location=at)
    bpy.context.active_object.data.materials.append(mat("Orb", color, emission=strength))
    light(bpy.context.scene, "POINT", at, light_energy, color, size=radius)


def monument_complete(at=(0, 0, 0), s=1.0):
    place("space/platform_large.glb", at, height=0.6 * s)
    place("space/hangar_roundA.glb", (at[0], at[1], at[2] + 0.6 * s), height=2.6 * s)
    place("space/structure_closed.glb", (at[0] + 2.2 * s, at[1] - 1.2 * s, at[2] + 0.6 * s), height=1.8 * s, rot=20)
    place("space/satelliteDish_large.glb", (at[0] - 2.0 * s, at[1] + 1.4 * s, at[2] + 0.6 * s), height=2.4 * s, rot=-30)
    place("space/machine_wireless.glb", (at[0] + 1.6 * s, at[1] + 1.8 * s, at[2] + 0.6 * s), height=2.0 * s)
    glow_orb((at[0], at[1], at[2] + 4.4 * s), 0.7 * s)


def monument_ruin(at=(0, 0, 0), s=1.0, glow=0.0):
    place("space/platform_large.glb", at, height=0.5 * s, tilt=(4, -3), sink=0.2 * s)
    place("space/rock_largeA.glb", (at[0] - 1.6 * s, at[1] + 0.8 * s, at[2]), height=2.0 * s, rot=40)
    place("space/rock_largeB.glb", (at[0] + 1.8 * s, at[1] - 0.6 * s, at[2]), height=1.6 * s, rot=-20)
    place("space/meteor_half.glb", (at[0] + 0.2 * s, at[1] + 1.6 * s, at[2]), height=1.2 * s)
    place("space/supports_low.glb", (at[0] + 0.4 * s, at[1] - 1.4 * s, at[2] + 0.3 * s), height=1.4 * s, tilt=(25, 10))
    place("space/pipe_straight.glb", (at[0] - 0.6 * s, at[1] - 0.4 * s, at[2] + 0.4 * s), height=0.6 * s,
          rot=60, tilt=(0, 70))
    place("space/hangar_roundA.glb", (at[0] - 0.2 * s, at[1] + 0.2 * s, at[2] + 0.2 * s), height=2.0 * s,
          tilt=(28, -14), sink=0.8 * s)
    if glow > 0.0:
        # Brilho fraco no núcleo: ainda há algo vivo ali
        glow_orb((at[0], at[1], at[2] + 3.0 * s), 0.3 * s, strength=glow, light_energy=300.0 * glow)


CITY = [
    # (modelo, x, y, altura, rot)
    ("space/hangar_largeA.glb", -7.0, 3.0, 3.2, 30),
    ("space/hangar_smallA.glb", -4.5, -4.5, 2.0, 10),
    ("space/hangar_smallA.glb", -8.5, -1.5, 2.0, -40),
    ("space/structure_detailed.glb", 6.5, 3.5, 3.0, -20),
    ("space/structure_closed.glb", 7.5, -2.5, 2.6, 15),
    ("space/hangar_roundGlass.glb", 3.5, -6.5, 2.4, 0),
    ("space/machine_generatorLarge.glb", -2.0, 7.0, 2.2, 45),
    ("space/machine_barrelLarge.glb", 3.0, 7.5, 2.0, 0),
    ("space/satelliteDish_detailed.glb", 9.5, 1.0, 2.6, -60),
    ("space/hangar_largeA.glb", 1.5, -9.0, 3.0, -15),
    ("space/structure.glb", -9.5, 6.0, 2.6, 20),
    ("space/machine_generator.glb", -1.0, -7.5, 1.6, 0),
    ("space/corridor_end.glb", 5.0, 0.0, 1.6, 90),
    ("space/corridor_end.glb", -5.0, 0.5, 1.6, -90),
]


def city(damage=0.0, seed=11):
    """Construções da cidade grande. damage 0 = intactas; 1 = tombadas e meio enterradas."""
    rnd = random.Random(seed)
    for rel, x, y, h, rot in CITY:
        tilt = (rnd.uniform(-25, 25) * damage, rnd.uniform(-25, 25) * damage)
        place(rel, (x, y, 0), height=h * (1.0 - 0.15 * damage), rot=rot + rnd.uniform(-30, 30) * damage,
              tilt=tilt, sink=h * 0.35 * damage)


def city_lights(scene, count=10, radius=9.0, energy=250.0, color=WARM, seed=13):
    rnd = random.Random(seed)
    for _ in range(count):
        a = rnd.uniform(0, math.tau)
        r = rnd.uniform(2.5, radius)
        light(scene, "POINT", (r * math.cos(a), r * math.sin(a), rnd.uniform(1.0, 2.5)), energy, color, size=0.5)


def scatter_rocks(count, radius_min, radius_max, seed=17, height=(0.6, 2.2)):
    models = ["nature/rock_largeC.glb", "nature/rock_tallB.glb", "space/rock_largeB.glb", "nature/stone_largeC.glb",
              "nature/stone_tallE.glb", "space/rock_crystals.glb"]
    rnd = random.Random(seed)
    for _ in range(count):
        a = rnd.uniform(0, math.tau)
        r = rnd.uniform(radius_min, radius_max)
        place(rnd.choice(models), (r * math.cos(a), r * math.sin(a), 0), height=rnd.uniform(*height),
              rot=rnd.uniform(0, 360), sink=0.1)


def scatter_plants(count, radius_min, radius_max, seed=19):
    models = ["nature/plant_flatTall.glb", "nature/grass_large.glb", "nature/crops_leafsStageB.glb",
              "nature/plant_bushLarge.glb", "space/rock_crystalsLargeA.glb"]
    rnd = random.Random(seed)
    for _ in range(count):
        a = rnd.uniform(0, math.tau)
        r = rnd.uniform(radius_min, radius_max)
        place(rnd.choice(models), (r * math.cos(a), r * math.sin(a), 0), height=rnd.uniform(0.6, 1.8),
              rot=rnd.uniform(0, 360))


def debris(count, center, spread, height, seed=23):
    """Pedras e pedaços no meio da água (caindo)."""
    rnd = random.Random(seed)
    models = ["space/rock_largeA.glb", "space/rock_largeB.glb", "space/pipe_straight.glb", "space/barrels.glb"]
    for _ in range(count):
        place(rnd.choice(models), (center[0] + rnd.uniform(-spread, spread), center[1] + rnd.uniform(-spread, spread),
                                   center[2] + rnd.uniform(0, height)),
              height=rnd.uniform(0.4, 1.4), rot=rnd.uniform(0, 360), tilt=(rnd.uniform(0, 90), rnd.uniform(0, 90)))


def survivors(center, count=5, radius=1.6, seed=29):
    rnd = random.Random(seed)
    for i in range(count):
        a = math.radians(200 + i * 35) + rnd.uniform(-0.1, 0.1)
        x, y = center[0] + radius * math.cos(a), center[1] + radius * math.sin(a)
        # de frente para o centro da cúpula
        face = math.degrees(math.atan2(center[1] - y, center[0] - x)) + 90
        place("space/astronautA.glb", (x, y, 0), height=0.9, rot=face)


def survivors_facing(points, look_from):
    """Sobreviventes em pontos fixos, virados para a câmera (para os cinco aparecerem)."""
    for x, y in points:
        face = math.degrees(math.atan2(look_from[1] - y, look_from[0] - x)) + 90
        place("space/astronautA.glb", (x, y, 0), height=0.9, rot=face)


def small_colony(at=(0, 0, 0), dome_radius=5.0):
    x, y = at[0], at[1]
    place("space/hangar_smallA.glb", (x - 1.6, y + 1.2, 0), height=1.5, rot=20)          # Moradia
    place("space/machine_generator.glb", (x + 1.8, y + 1.4, 0), height=1.2, rot=-30)     # Gerador
    place("space/machine_barrel.glb", (x + 2.3, y - 1.2, 0), height=1.1)                 # Planta de O2
    place("space/platform_large.glb", (x - 1.8, y - 1.8, 0), height=0.15)                # Fazenda
    for dx, dy in ((-2.3, -2.2), (-1.4, -2.3), (-2.2, -1.4), (-1.3, -1.4)):
        place("nature/crops_leafsStageB.glb", (x + dx, y + dy, 0.15), height=0.5, rot=dx * 50)
    hemisphere("SmallDome", dome_radius, at, glass("SmallGlass", 0.14))
    dome_frame(dome_radius, at, ribs=6)


# ---------------------------------------------------------------------------
# As 5 cenas
# ---------------------------------------------------------------------------
def scene_1(a):
    """A cidade prospera."""
    scene = reset(a.samples, a.percent, WATER, 0.018)
    seabed(scene)
    city()
    monument_complete((0, 0, 0), 1.0)
    hemisphere("BigDome", 14.0, (0, 0, 0), glass())
    dome_frame(14.0, ribs=12)
    city_lights(scene, count=12, energy=300.0)
    scatter_rocks(26, 17, 60)
    scatter_plants(30, 15, 45)
    fish_school(14, (-14, 10, 12), 5)
    fish_school(9, (18, 18, 16), 4, seed=8)
    bubbles(40, (0, 0, 14), 10, 12, glow=0.6)
    light(scene, "SUN", (0, 0, 50), 2.2, COLD, rot=(20, 10, 30), size=8)
    god_ray(scene, 0, 0, 9000, 14, (0.7, 0.9, 1.0))
    god_ray(scene, -16, 12, 5000, 8)
    god_ray(scene, 20, -8, 4000, 7)
    camera(scene, (22, -30, 13), (0, 0, 4), lens=30)


def scene_2(a):
    """O tremor racha a cúpula."""
    scene = reset(a.samples, a.percent, (0.025, 0.09, 0.15), 0.03)
    seabed(scene, dunes=0.9)
    city(damage=0.15)
    monument_complete((0, 0, 0), 1.0)
    hemisphere("BigDome", 14.0, (0, 0, 0), glass(alpha=0.16), holes=0.45, seed=4)
    dome_frame(14.0, ribs=12)
    # Cacos de vidro caindo
    shard = glass("Shard", 0.35)
    rnd = random.Random(31)
    for _ in range(26):
        bpy.ops.mesh.primitive_plane_add(size=rnd.uniform(0.6, 1.8),
                                         location=(rnd.uniform(2, 12), rnd.uniform(-8, 8), rnd.uniform(4, 15)),
                                         rotation=(rnd.uniform(0, 3), rnd.uniform(0, 3), rnd.uniform(0, 3)))
        bpy.context.active_object.data.materials.append(shard)
    debris(16, (6, 0, 6), 7, 10)
    bubbles(160, (7, 0, 9), 6, 12, size=(0.06, 0.35), glow=0.8)
    # Alarme: luzes vermelhas espalhadas
    for x, y in ((-6, 2), (5, -4), (2, 6), (-3, -6), (8, 2)):
        light(scene, "POINT", (x, y, 2.5), 900.0, RED, size=0.4)
    city_lights(scene, count=5, energy=120.0)
    scatter_rocks(26, 17, 60)
    light(scene, "SUN", (0, 0, 50), 0.8, COLD, rot=(30, -10, 60), size=6)
    god_ray(scene, 9, 0, 9000, 10, (0.8, 0.9, 1.0))
    camera(scene, (-20, -24, 6), (2, 0, 6), lens=28, roll=-7)


def scene_3(a):
    """A cidade engolida; o Monumento vira ruína."""
    scene = reset(a.samples, a.percent, WATER_DARK, 0.022)
    seabed(scene, dunes=1.0)
    city(damage=1.0)
    monument_ruin((0, 0, 0), 2.0, glow=1.2)
    # Restos da cúpula: só o anel de base e arcos quebrados
    dome_frame(14.0, ribs=3)
    scatter_rocks(30, 16, 60, seed=41)
    scatter_plants(18, 6, 30, seed=43)
    debris(10, (0, 0, 0), 10, 0.3, seed=47)
    fish_school(6, (-8, 10, 8), 3, seed=9)
    light(scene, "SUN", (0, 0, 50), 0.9, COLD, rot=(15, 5, 0), size=4)
    god_ray(scene, 0, 0, 7000, 8, COLD)
    camera(scene, (17, -27, 11), (0, 0, 1.0), lens=32, roll=2)


def scene_4(a):
    """Cinco sobreviventes numa pequena cúpula."""
    scene = reset(a.samples, a.percent, WATER_DARK, 0.035)
    seabed(scene, dunes=0.8)
    small_colony((0, 0, 0), 5.0)
    survivors_facing([(-0.6, -2.9), (0.15, -3.2), (0.9, -3.0), (1.6, -2.6), (-1.2, -3.4)], (6.5, -9.5))
    # Luz quente dentro da cúpula: abrigo
    light(scene, "POINT", (0, 0, 3.2), 900.0, WARM, size=1.5)
    light(scene, "POINT", (1.8, 1.4, 1.5), 150.0, AMBER, size=0.3)
    scatter_rocks(30, 7, 50, seed=51)
    scatter_plants(20, 6, 25, seed=53)
    bubbles(30, (0, 0, 5), 3, 10, glow=0.6)
    light(scene, "SUN", (0, 0, 50), 0.4, COLD, rot=(25, 0, 40), size=4)
    god_ray(scene, 0, 0, 3500, 10, COLD)
    camera(scene, (6.5, -9.5, 3.2), (0, 0, 1.1), lens=30)


def scene_5(a):
    """A colônia olha para a ruína do Monumento ao longe."""
    scene = reset(a.samples, a.percent, WATER, 0.022)
    seabed(scene, dunes=0.8)
    small_colony((0, 0, 0), 5.0)
    survivors((0, 0, 0), 5, 1.4)
    light(scene, "POINT", (0, 0, 3.2), 700.0, WARM, size=1.5)
    # Ruína ao longe, com um brilho âmbar fraco no núcleo e um feixe de luz sobre ela
    ruin = (0, 30, 0)
    monument_ruin(ruin, 3.2, glow=9.0)
    god_ray(scene, ruin[0], ruin[1], 14000, 12, (1.0, 0.8, 0.5))
    for rel, x, y, h, rot in CITY[:8]:
        place(rel, (ruin[0] + x * 1.3, ruin[1] + y * 1.3 + 4, 0), height=h * 0.85, rot=rot + 30,
              tilt=(random.uniform(-20, 20), random.uniform(-20, 20)), sink=h * 0.4)
    scatter_rocks(36, 7, 70, seed=61)
    scatter_plants(24, 6, 35, seed=63)
    fish_school(10, (8, 20, 9), 4, seed=12)
    light(scene, "SUN", (0, 0, 50), 1.0, COLD, rot=(20, 0, 160), size=6)
    god_ray(scene, -10, 14, 4000, 7)
    camera(scene, (3.0, -12.5, 2.6), (0, 30, 5.5), lens=30)


SCENES = {1: scene_1, 2: scene_2, 3: scene_3, 4: scene_4, 5: scene_5}


def main():
    a = parse_args()
    out_dir = PREVIEW_OUT if a.preview else ASSETS_OUT
    os.makedirs(out_dir, exist_ok=True)
    for n in (a.only or sorted(SCENES)):
        SCENES[n](a)
        path = os.path.join(out_dir, f"intro_{n}.png")
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print(f"[intro] cena {n} -> {path}")


if __name__ == "__main__":
    main()
