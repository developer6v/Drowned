"""
Drowned - modelos placeholder low-poly gerados so com primitivas (SCRUM-154).

Servem para o render em lote rodar de ponta a ponta antes da arte final (SCRUM-126, 135 a 138,
151, 164, 165). Quando o modelo real existir, troque `"generator": "placeholder:<id>"` por
`"model": "caminho/arquivo.blend"` no manifesto e rode o lote de novo.

Convencoes (iguais ao README): 1 unidade = 1 celula do grid, origem no centro da base,
frente apontando para -Y.

Uso dentro do Blender (chamado por render_batch.py):

    import placeholder_models
    objects = placeholder_models.build("housing_1")

Ou, para gravar um .blend e abrir no Blender:

    blender -b --python tools/blender/placeholder_models.py -- --id housing_1 --save out/housing_1.blend
"""

import math
import sys

import bpy

# Cores do tema (mesmas de DemoArt.cs)
HOUSING = (0.90, 0.75, 0.45)
FOOD = (0.40, 0.80, 0.45)
ENERGY = (0.98, 0.80, 0.25)
OXYGEN = (0.45, 0.80, 0.98)
RESEARCH = (0.75, 0.55, 0.95)
MONUMENT = (0.85, 0.85, 0.90)
RUIN = (0.45, 0.45, 0.50)
METAL = (0.35, 0.40, 0.48)
GLASS = (0.70, 0.90, 1.00)
SKIN = (0.85, 0.70, 0.60)
SUIT = (0.25, 0.45, 0.70)

_materials = {}


def material(color, roughness=0.6, emission=0.0):
    key = (color, roughness, emission)
    if key in _materials:
        return _materials[key]
    mat = bpy.data.materials.new(f"PH_{len(_materials)}")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        if emission > 0.0 and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*color, 1.0)
            bsdf.inputs["Emission Strength"].default_value = emission
    _materials[key] = mat
    return mat


def _finish(obj, name, color, **mat_kw):
    obj.name = name
    obj.data.materials.append(material(color, **mat_kw))
    return obj


def box(name, color, size=(1.0, 1.0, 1.0), at=(0.0, 0.0, 0.0), rot=(0.0, 0.0, 0.0), **mat_kw):
    """Caixa com a base em at.z (at e o centro da base)."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(at[0], at[1], at[2] + size[2] / 2.0),
                                    rotation=rot)
    o = bpy.context.active_object
    o.scale = size
    return _finish(o, name, color, **mat_kw)


def cylinder(name, color, radius=0.5, depth=1.0, at=(0.0, 0.0, 0.0), verts=16, rot=(0.0, 0.0, 0.0), **mat_kw):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth,
                                        location=(at[0], at[1], at[2] + depth / 2.0), rotation=rot)
    return _finish(bpy.context.active_object, name, color, **mat_kw)


def cone(name, color, radius=0.5, depth=1.0, at=(0.0, 0.0, 0.0), verts=4, **mat_kw):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=0.0, depth=depth,
                                    location=(at[0], at[1], at[2] + depth / 2.0),
                                    rotation=(0.0, 0.0, math.radians(45.0) if verts == 4 else 0.0))
    return _finish(bpy.context.active_object, name, color, **mat_kw)


def sphere(name, color, radius=0.5, at=(0.0, 0.0, 0.0), **mat_kw):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=at)
    return _finish(bpy.context.active_object, name, color, **mat_kw)


def dome(name, color, radius=0.5, at=(0.0, 0.0, 0.0), **mat_kw):
    """Meia esfera com a base em at.z."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=radius, location=at)
    o = bpy.context.active_object
    # Corta a metade de baixo
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.bisect(plane_co=at, plane_no=(0.0, 0.0, 1.0), clear_inner=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    return _finish(o, name, color, **mat_kw)


def torus(name, color, major=0.5, minor=0.08, at=(0.0, 0.0, 0.0), rot=(0.0, 0.0, 0.0), **mat_kw):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=at, rotation=rot,
                                     major_segments=24, minor_segments=8)
    return _finish(bpy.context.active_object, name, color, **mat_kw)


# ---------------------------------------------------------------------------
# Estruturas (1 unidade = 1 celula)
# ---------------------------------------------------------------------------
def housing_1():
    body = box("Housing_Body", HOUSING, size=(0.8, 0.8, 0.6))
    roof = cone("Housing_Roof", (0.55, 0.40, 0.30), radius=0.62, depth=0.35, at=(0, 0, 0.6))
    door = box("Housing_Door", (0.35, 0.25, 0.20), size=(0.2, 0.05, 0.3), at=(0.0, -0.41, 0.0))
    return [body, roof, door]


def housing_2():
    base = box("Housing_Base", HOUSING, size=(0.85, 0.85, 0.55))
    top = box("Housing_Top", HOUSING, size=(0.65, 0.65, 0.5), at=(0, 0, 0.55))
    roof = cone("Housing_Roof", (0.55, 0.40, 0.30), radius=0.5, depth=0.3, at=(0, 0, 1.05))
    door = box("Housing_Door", (0.35, 0.25, 0.20), size=(0.2, 0.05, 0.3), at=(0.0, -0.44, 0.0))
    win = box("Housing_Window", GLASS, size=(0.25, 0.05, 0.2), at=(0.0, -0.34, 0.7), emission=0.5)
    return [base, top, roof, door, win]


def algae_farm(level=1):
    parts = [box("Farm_Base", METAL, size=(1.9, 1.9, 0.15))]
    rows = 3 if level == 1 else 4
    for i in range(rows):
        y = -0.65 + i * (1.3 / (rows - 1))
        parts.append(box(f"Farm_Row{i}", FOOD, size=(1.6, 0.25, 0.3 + 0.1 * level), at=(0.0, y, 0.15)))
    parts.append(dome("Farm_Dome", GLASS, radius=1.0, at=(0, 0, 0.15), roughness=0.1))
    return parts


def generator(level=1):
    body = cylinder("Gen_Body", METAL, radius=0.35, depth=0.5)
    core = cylinder("Gen_Core", ENERGY, radius=0.25, depth=0.35, at=(0, 0, 0.5), emission=2.0)
    ring = torus("Gen_Ring", ENERGY, major=0.38, minor=0.05, at=(0, 0, 0.55), emission=1.0)
    stack = cylinder("Gen_Stack", METAL, radius=0.08, depth=0.5 + 0.3 * level, at=(0.25, 0.25, 0.5), verts=8)
    parts = [body, core, ring, stack]
    if level >= 2:
        parts.append(torus("Gen_Ring2", ENERGY, major=0.38, minor=0.05, at=(0, 0, 0.8), emission=1.0))
    return parts


def oxygen_plant(level=1):
    tank = cylinder("O2_Tank", OXYGEN, radius=0.3, depth=0.7 + 0.2 * level, verts=16)
    cap = dome("O2_Cap", GLASS, radius=0.3, at=(0, 0, 0.7 + 0.2 * level), roughness=0.1)
    pipe = cylinder("O2_Pipe", METAL, radius=0.06, depth=0.5, at=(0.0, -0.35, 0.15), verts=8,
                    rot=(math.radians(90.0), 0.0, 0.0))
    valve = sphere("O2_Valve", OXYGEN, radius=0.1, at=(0.0, -0.4, 0.4), emission=1.5)
    return [tank, cap, pipe, valve]


def lab(level=1):
    base = box("Lab_Base", RESEARCH, size=(1.8, 1.8, 0.6))
    d = dome("Lab_Dome", GLASS, radius=0.7, at=(0, 0, 0.6), roughness=0.1)
    antenna = cylinder("Lab_Antenna", METAL, radius=0.04, depth=0.8 + 0.3 * level, at=(0.7, 0.7, 0.6), verts=6)
    tip = sphere("Lab_Tip", RESEARCH, radius=0.08, at=(0.7, 0.7, 1.45 + 0.3 * level), emission=2.0)
    return [base, d, antenna, tip]


# ---------------------------------------------------------------------------
# Habitante (ocupa ~0.4 celula, 0.9 de altura)
# ---------------------------------------------------------------------------
def inhabitant():
    legs = box("Hab_Legs", SUIT, size=(0.25, 0.18, 0.35))
    body = box("Hab_Body", SUIT, size=(0.32, 0.22, 0.35), at=(0, 0, 0.35))
    head = sphere("Hab_Head", SKIN, radius=0.17, at=(0, 0, 0.87))
    visor = box("Hab_Visor", GLASS, size=(0.22, 0.05, 0.09), at=(0.0, -0.16, 0.85), emission=0.3)
    return [legs, body, head, visor]


# ---------------------------------------------------------------------------
# Monumento (4x4 celulas, 5 visuais: ruina + 4 etapas)
# ---------------------------------------------------------------------------
def _monument_platform(color):
    return [box("Mon_Platform", color, size=(3.6, 3.6, 0.3)),
            box("Mon_Step", color, size=(2.8, 2.8, 0.25), at=(0, 0, 0.3))]


def _monument_columns(color, height, count=4, broken=False):
    parts = []
    for i in range(count):
        a = math.radians(45.0 + i * 90.0)
        x, y = 1.1 * math.cos(a), 1.1 * math.sin(a)
        h = height * (0.4 + 0.15 * i) if broken else height
        parts.append(cylinder(f"Mon_Col{i}", color, radius=0.15, depth=h, at=(x, y, 0.55), verts=10))
    return parts


def monument_ruin():
    parts = _monument_platform(RUIN)
    parts += _monument_columns(RUIN, 1.6, broken=True)
    parts.append(box("Mon_Rubble1", RUIN, size=(0.6, 0.4, 0.3), at=(0.3, -0.6, 0.55), rot=(0, 0, 0.4)))
    parts.append(box("Mon_Rubble2", RUIN, size=(0.4, 0.4, 0.25), at=(-0.5, 0.4, 0.55), rot=(0, 0, -0.7)))
    return parts


def monument_stage1():  # Fundacao
    parts = _monument_platform(MONUMENT)
    parts += _monument_columns(METAL, 0.6)
    return parts


def monument_stage2():  # Estrutura
    parts = _monument_platform(MONUMENT)
    parts += _monument_columns(METAL, 2.0)
    parts.append(torus("Mon_Ring", METAL, major=1.1, minor=0.08, at=(0, 0, 2.55)))
    return parts


def monument_stage3():  # Sistemas
    parts = monument_stage2()
    parts.append(cylinder("Mon_Core", METAL, radius=0.5, depth=1.8, at=(0, 0, 0.55), verts=12))
    for i in range(4):
        a = math.radians(i * 90.0)
        parts.append(sphere(f"Mon_Light{i}", OXYGEN, radius=0.12, at=(1.1 * math.cos(a), 1.1 * math.sin(a), 2.55),
                            emission=2.0))
    return parts


def monument_stage4():  # Acabamento
    parts = monument_stage3()
    parts.append(sphere("Mon_Orb", ENERGY, radius=0.55, at=(0, 0, 3.1), emission=3.0))
    parts.append(torus("Mon_Ring2", ENERGY, major=0.9, minor=0.06, at=(0, 0, 3.1),
                       rot=(math.radians(90.0), 0.0, 0.0), emission=1.0))
    return parts


# ---------------------------------------------------------------------------
# Chão do fundo do mar (SCRUM-167): tile 1x1 de areia com pedrinhas e conchas
# ---------------------------------------------------------------------------
def _sand_material(seed):
    mat = bpy.data.materials.new(f"Sand{seed}")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 14.0 + seed * 3.0
    noise.inputs["Detail"].default_value = 6.0
    if "W" in noise.inputs:
        noise.noise_dimensions = "4D"
        noise.inputs["W"].default_value = seed * 1.7
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    # areia molhada vista através da água: escura, puxando para o azul-esverdeado; cada variação um pouco diferente
    k = 1.0 + (seed - 2) * 0.06
    ramp.color_ramp.elements[0].color = (0.15 * k, 0.22 * k, 0.26 * k, 1.0)
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[1].color = (0.26 * k, 0.33 * k, 0.34 * k, 1.0)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    if bsdf:
        nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = 0.95
    return mat


def seabed(seed, pebbles, shells):
    import random
    rnd = random.Random(seed)
    # 1.02 de lado: cobre a célula com folga para os tiles vizinhos se sobreporem sem fresta
    bpy.ops.mesh.primitive_plane_add(size=1.02, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = f"Seabed{seed}"
    ground.data.materials.append(_sand_material(seed))
    parts = [ground]
    stone_colors = [(0.20, 0.24, 0.28), (0.16, 0.20, 0.24), (0.26, 0.28, 0.30)]
    for i in range(pebbles):
        r = rnd.uniform(0.015, 0.04)
        x, y = rnd.uniform(-0.42, 0.42), rnd.uniform(-0.42, 0.42)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=r, location=(x, y, r * 0.3))
        o = bpy.context.active_object
        o.scale = (1.0, rnd.uniform(0.7, 1.0), 0.45)
        parts.append(_finish(o, f"Pebble{i}", rnd.choice(stone_colors)))
    for i in range(shells):
        r = rnd.uniform(0.02, 0.035)
        x, y = rnd.uniform(-0.38, 0.38), rnd.uniform(-0.38, 0.38)
        bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=r, radius2=0.0, depth=r * 0.8, location=(x, y, r * 0.4))
        parts.append(_finish(bpy.context.active_object, f"Shell{i}", (0.62, 0.55, 0.50), roughness=0.5))
    return parts


# ---------------------------------------------------------------------------
# Acessorios dos especialistas (SCRUM-152)
# ---------------------------------------------------------------------------
# Modelados nas coordenadas do astronautA da Kenney (sem girar): centro em (2.0, -1.5), cabeca de z 0.48 a 0.79,
# maos perto de z 0.14 (direita em x ~2.19, esquerda em x ~1.81), rosto em +Y (y ~ -1.37), mochila em -Y.
# No manifest, cada especialista = astronautA + acc_<id>, girados juntos.

ACC_X, ACC_Y = 2.0, -1.5
HEAD_TOP = 0.793
FACE_Y = -1.37
HAT_YELLOW = (0.98, 0.78, 0.12)
TOOL_GREY = (0.62, 0.66, 0.72)
SAMPLE_GREEN = (0.30, 0.85, 0.45)
COAT_WHITE = (0.95, 0.96, 0.98)
LENS = (0.35, 0.85, 1.00)
BOARD_BROWN = (0.55, 0.36, 0.20)
PLAN_BLUE = (0.25, 0.50, 0.95)


def acc_engineer():
    """Capacete de obra com aba + chave de boca na mao direita."""
    shell = box("Eng_Hat", HAT_YELLOW, size=(0.30, 0.30, 0.07), at=(ACC_X, ACC_Y, HEAD_TOP - 0.01), roughness=0.4)
    crown = box("Eng_HatTop", HAT_YELLOW, size=(0.22, 0.24, 0.06), at=(ACC_X, ACC_Y, HEAD_TOP + 0.06), roughness=0.4)
    brim = box("Eng_Brim", HAT_YELLOW, size=(0.30, 0.09, 0.025), at=(ACC_X, FACE_Y + 0.04, HEAD_TOP - 0.01), roughness=0.4)
    handle = box("Eng_Wrench", TOOL_GREY, size=(0.035, 0.035, 0.15), at=(2.21, -1.42, 0.03), roughness=0.3)
    jaw = box("Eng_WrenchHead", TOOL_GREY, size=(0.08, 0.04, 0.04), at=(2.21, -1.42, 0.0), roughness=0.3)
    return [shell, crown, brim, handle, jaw]


def acc_biologist():
    """Cilindro de amostras verde nas costas + puca (vara e aro) na mao esquerda."""
    tank = cylinder("Bio_Tank", SAMPLE_GREEN, radius=0.06, depth=0.26, at=(ACC_X, -1.77, 0.24), emission=0.6)
    cap_top = cylinder("Bio_TankCap", TOOL_GREY, radius=0.065, depth=0.03, at=(ACC_X, -1.77, 0.50))
    cap_bot = cylinder("Bio_TankBase", TOOL_GREY, radius=0.065, depth=0.03, at=(ACC_X, -1.77, 0.22))
    pole = cylinder("Bio_NetPole", BOARD_BROWN, radius=0.012, depth=0.55, at=(1.74, -1.42, 0.06), verts=8)
    ring = torus("Bio_NetRing", TOOL_GREY, major=0.075, minor=0.012, at=(1.74, -1.42, 0.68),
                 rot=(math.radians(90.0), 0.0, 0.0))
    return [tank, cap_top, cap_bot, pole, ring]


def acc_scientist():
    """Jaleco branco sobre o corpo + oculos de protecao no rosto."""
    coat = box("Sci_Coat", COAT_WHITE, size=(0.29, 0.31, 0.37), at=(ACC_X, -1.515, 0.12), roughness=0.8)
    collar = box("Sci_Collar", COAT_WHITE, size=(0.22, 0.05, 0.05), at=(ACC_X, FACE_Y - 0.005, 0.47), roughness=0.8)
    frame = box("Sci_Goggles", (0.12, 0.13, 0.16), size=(0.27, 0.03, 0.06), at=(ACC_X, FACE_Y + 0.01, 0.63))
    lens_l = box("Sci_LensL", LENS, size=(0.08, 0.02, 0.045), at=(ACC_X - 0.06, FACE_Y + 0.025, 0.637), emission=0.8)
    lens_r = box("Sci_LensR", LENS, size=(0.08, 0.02, 0.045), at=(ACC_X + 0.06, FACE_Y + 0.025, 0.637), emission=0.8)
    return [coat, collar, frame, lens_l, lens_r]


def acc_architect():
    """Prancheta com planta azul na frente do peito + lapis atras da orelha."""
    tilt = (math.radians(-20.0), 0.0, 0.0)
    board = box("Arc_Board", BOARD_BROWN, size=(0.17, 0.02, 0.21), at=(ACC_X + 0.04, FACE_Y + 0.05, 0.20), rot=tilt)
    sheet = box("Arc_Plan", PLAN_BLUE, size=(0.14, 0.012, 0.16), at=(ACC_X + 0.04, FACE_Y + 0.066, 0.215), rot=tilt,
                emission=0.3)
    clip = box("Arc_Clip", TOOL_GREY, size=(0.06, 0.03, 0.025), at=(ACC_X + 0.04, FACE_Y + 0.02, 0.40), rot=tilt)
    pencil = cylinder("Arc_Pencil", HAT_YELLOW, radius=0.012, depth=0.16, at=(2.2, ACC_Y, 0.56), verts=6,
                      rot=(math.radians(90.0), 0.0, 0.0))
    return [board, sheet, clip, pencil]


GENERATORS = {
    "seabed_a": lambda: seabed(1, pebbles=3, shells=0),
    "seabed_b": lambda: seabed(2, pebbles=6, shells=1),
    "seabed_c": lambda: seabed(3, pebbles=1, shells=2),
    "housing_1": housing_1,
    "housing_2": housing_2,
    "algae_farm_1": lambda: algae_farm(1),
    "algae_farm_2": lambda: algae_farm(2),
    "generator_1": lambda: generator(1),
    "generator_2": lambda: generator(2),
    "oxygen_plant_1": lambda: oxygen_plant(1),
    "oxygen_plant_2": lambda: oxygen_plant(2),
    "lab_1": lambda: lab(1),
    "lab_2": lambda: lab(2),
    "inhabitant": inhabitant,
    "monument_ruin": monument_ruin,
    "monument_1": monument_stage1,
    "monument_2": monument_stage2,
    "monument_3": monument_stage3,
    "monument_4": monument_stage4,
    "acc_engineer": acc_engineer,
    "acc_biologist": acc_biologist,
    "acc_scientist": acc_scientist,
    "acc_architect": acc_architect,
}


def build(generator_id):
    """Cria o modelo placeholder na cena atual e devolve a lista de objetos."""
    if generator_id not in GENERATORS:
        raise KeyError(f"Placeholder desconhecido: {generator_id}. Opcoes: {', '.join(sorted(GENERATORS))}")
    return GENERATORS[generator_id]()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    p = argparse.ArgumentParser()
    p.add_argument("--id", required=True, choices=sorted(GENERATORS))
    p.add_argument("--save", required=True, help="Caminho do .blend de saida")
    args = p.parse_args(argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    build(args.id)
    bpy.ops.wm.save_as_mainfile(filepath=args.save)
    print(f"[placeholder_models] salvo em {args.save}")


if __name__ == "__main__":
    main()
