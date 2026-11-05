"""
SCRUM-184 - Ícones finais da HUD: recursos, estruturas, especialistas, tecnologias e alertas.

Gera PNGs 128x128 com fundo transparente, no mesmo estilo flat dos placeholders da SCRUM-127
(disco azul-escuro + símbolo colorido; tecnologias em hexágono para diferenciar). Rodar:

    python -I tools/icons/make_game_icons.py

Saída: Assets/Sprites/Icons/Resources/Icons/<Categoria>/<id>.png
O jogo carrega por id com Resources.Load (Drowned.UI.GameIcons), então os ids aqui precisam ser
os mesmos de docs/balanceamento/*.csv. Para mudar cores, edite a paleta abaixo e rode de novo.
"""
import math
import os
from PIL import Image, ImageDraw

SIZE = 128
SS = 4  # desenha em 4x e reduz: antialias
S = SIZE * SS
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..",
                    "Assets", "Sprites", "Icons", "Resources", "Icons")

# Paleta (mesma família de UiTheme.cs e DemoArt.cs)
AMBER = (245, 166, 35, 255)
AMBER_DARK = (200, 120, 10, 255)
BLUE = (64, 160, 230, 255)
BLUE_DARK = (30, 90, 160, 255)
GREEN = (80, 200, 120, 255)
GREEN_DARK = (30, 130, 70, 255)
PURPLE = (175, 130, 240, 255)
PURPLE_DARK = (110, 70, 180, 255)
SAND = (230, 190, 115, 255)
SAND_DARK = (150, 105, 55, 255)
METAL = (150, 165, 185, 255)
METAL_DARK = (85, 100, 120, 255)
GLASS = (190, 230, 255, 255)
RED = (240, 80, 65, 255)
RED_DARK = (160, 40, 30, 255)
WHITE = (235, 245, 255, 255)
DISC = (18, 42, 72, 255)
DISC_RIM = (60, 120, 170, 255)
HEX = (24, 36, 70, 255)
HEX_RIM = (175, 130, 240, 255)

W = int(S * 0.018)  # espessura padrão de contorno


def P(x, y):
    return (x * S, y * S)


def box(cx, cy, r):
    return [(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S]


def new():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def save(img, category, name):
    img = img.resize((SIZE, SIZE), Image.LANCZOS)
    folder = os.path.join(ROOT, category)
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, f"{name}.png")
    img.save(path)
    print("ok", os.path.relpath(path))


# ---------------------------------------------------------------- fundos
def disc(d, rim=DISC_RIM):
    d.ellipse(box(0.5, 0.5, 0.45), fill=DISC, outline=rim, width=int(S * 0.025))


def hexagon(d):
    pts = [P(0.5 + 0.45 * math.cos(math.radians(a)), 0.5 + 0.45 * math.sin(math.radians(a))) for a in range(30, 390, 60)]
    d.polygon(pts, fill=HEX, outline=HEX_RIM, width=int(S * 0.025))


# ---------------------------------------------------------------- símbolos (coordenadas 0..1 dentro do ícone)
def bolt(d, ox=0.0, oy=0.0, k=1.0, fill=AMBER, outline=AMBER_DARK):
    pts = [(0.56, 0.14), (0.30, 0.54), (0.48, 0.54), (0.42, 0.88), (0.70, 0.44), (0.52, 0.44)]
    d.polygon([P(0.5 + (x - 0.5) * k + ox, 0.5 + (y - 0.5) * k + oy) for x, y in pts], fill=fill, outline=outline, width=W)


def bubbles(d, ox=0.0, oy=0.0, k=1.0):
    for cx, cy, r in [(0.40, 0.62, 0.20), (0.66, 0.40, 0.13), (0.60, 0.72, 0.08)]:
        cx, cy, r = 0.5 + (cx - 0.5) * k + ox, 0.5 + (cy - 0.5) * k + oy, r * k
        d.ellipse(box(cx, cy, r), fill=BLUE, outline=BLUE_DARK, width=W)
        d.ellipse(box(cx - r * 0.45, cy - r * 0.45, r * 0.3), fill=WHITE)


def leaf(d, ox=0.0, oy=0.0, k=1.0):
    # folha de alga: duas curvas aproximadas por polígono
    pts = []
    for i in range(0, 181, 10):
        t = math.radians(i)
        pts.append((0.5 + 0.20 * math.sin(t), 0.82 - 0.64 * i / 180))
    for i in range(180, -1, -10):
        t = math.radians(i)
        pts.append((0.5 - 0.20 * math.sin(t) * 0.8, 0.82 - 0.64 * i / 180))
    d.polygon([P(0.5 + (x - 0.5) * k + ox, 0.5 + (y - 0.5) * k + oy) for x, y in pts], fill=GREEN, outline=GREEN_DARK, width=W)
    d.line([P(0.5 + ox, 0.5 + (0.80 - 0.5) * k + oy), P(0.5 + ox, 0.5 + (0.24 - 0.5) * k + oy)], fill=GREEN_DARK, width=W)


def house(d, ox=0.0, oy=0.0, k=1.0, fill=SAND, outline=SAND_DARK):
    def q(x, y):
        return P(0.5 + (x - 0.5) * k + ox, 0.5 + (y - 0.5) * k + oy)
    d.polygon([q(0.24, 0.48), q(0.50, 0.24), q(0.76, 0.48)], fill=AMBER, outline=AMBER_DARK, width=W)
    d.rectangle([*q(0.30, 0.48), *q(0.70, 0.78)], fill=fill, outline=outline, width=W)
    d.rectangle([*q(0.45, 0.60), *q(0.55, 0.78)], fill=outline)


def dome(d, ox=0.0, oy=0.0, k=1.0):
    def q(x, y):
        return P(0.5 + (x - 0.5) * k + ox, 0.5 + (y - 0.5) * k + oy)
    d.rectangle([*q(0.20, 0.66), *q(0.80, 0.76)], fill=METAL, outline=METAL_DARK, width=W)
    d.pieslice([*q(0.24, 0.30), *q(0.76, 1.02)], 180, 360, fill=GLASS, outline=BLUE_DARK, width=W)


def tank(d):
    d.rounded_rectangle([*P(0.36, 0.22), *P(0.64, 0.80)], radius=int(S * 0.12), fill=BLUE, outline=BLUE_DARK, width=W)
    d.rectangle([*P(0.42, 0.30), *P(0.48, 0.70)], fill=(140, 200, 245, 255))


def flask(d, ox=0.0, oy=0.0, k=1.0, fill=PURPLE, outline=PURPLE_DARK):
    def q(x, y):
        return P(0.5 + (x - 0.5) * k + ox, 0.5 + (y - 0.5) * k + oy)
    d.polygon([q(0.42, 0.18), q(0.58, 0.18), q(0.58, 0.42), q(0.76, 0.80), q(0.24, 0.80), q(0.42, 0.42)],
              fill=GLASS, outline=outline, width=W)
    d.polygon([q(0.33, 0.62), q(0.67, 0.62), q(0.76, 0.80), q(0.24, 0.80)], fill=fill)


def gear(d, cx, cy, r, fill=METAL, outline=METAL_DARK):
    teeth = 8
    pts = []
    for i in range(teeth * 2):
        a = math.pi * i / teeth
        rr = r if i % 2 == 0 else r * 0.75
        pts.append(P(cx + rr * math.cos(a), cy + rr * math.sin(a)))
    d.polygon(pts, fill=fill, outline=outline, width=W)
    d.ellipse(box(cx, cy, r * 0.32), fill=DISC)


def chip(d):
    d.rectangle([*P(0.32, 0.32), *P(0.68, 0.68)], fill=PURPLE, outline=PURPLE_DARK, width=W)
    for i in range(4):
        t = 0.38 + i * 0.08
        for a, b in [((t, 0.22), (t, 0.32)), ((t, 0.68), (t, 0.78)), ((0.22, t), (0.32, t)), ((0.68, t), (0.78, t))]:
            d.line([P(*a), P(*b)], fill=METAL, width=W)
    d.rectangle([*P(0.43, 0.43), *P(0.57, 0.57)], fill=GLASS)


def columns(d, fill=WHITE, outline=METAL_DARK):
    d.polygon([P(0.22, 0.32), P(0.50, 0.18), P(0.78, 0.32)], fill=AMBER, outline=AMBER_DARK, width=W)
    for x in (0.30, 0.46, 0.62):
        d.rectangle([*P(x, 0.34), *P(x + 0.08, 0.72)], fill=fill, outline=outline, width=W)
    d.rectangle([*P(0.22, 0.72), *P(0.78, 0.80)], fill=fill, outline=outline, width=W)


def person(d, color, dark):
    d.ellipse(box(0.5, 0.36, 0.13), fill=(235, 200, 175, 255), outline=(150, 110, 90, 255), width=W)
    d.pieslice([*P(0.24, 0.52), *P(0.76, 1.04)], 180, 360, fill=color, outline=dark, width=W)


def badge(d, kind):
    """Selo no canto inferior direito: 'up' (bônus), 'plus' (desbloqueio)."""
    cx, cy, r = 0.76, 0.76, 0.15
    d.ellipse(box(cx, cy, r), fill=AMBER, outline=AMBER_DARK, width=W)
    if kind == "up":
        d.polygon([P(cx, cy - 0.09), P(cx + 0.08, cy + 0.01), P(cx - 0.08, cy + 0.01)], fill=DISC)
        d.rectangle([*P(cx - 0.03, cy), *P(cx + 0.03, cy + 0.08)], fill=DISC)
    else:
        d.rectangle([*P(cx - 0.08, cy - 0.025), *P(cx + 0.08, cy + 0.025)], fill=DISC)
        d.rectangle([*P(cx - 0.025, cy - 0.08), *P(cx + 0.025, cy + 0.08)], fill=DISC)


# ---------------------------------------------------------------- recursos (ResourceType)
def resources():
    for name, draw in [("Energy", lambda d: bolt(d)), ("Oxygen", lambda d: bubbles(d)), ("Food", lambda d: leaf(d))]:
        img, d = new(); disc(d); draw(d); save(img, "ResourceTypes", name)


# ---------------------------------------------------------------- estruturas (ids de Estruturas.csv)
def structures():
    img, d = new(); disc(d); house(d, k=1.05); save(img, "Structures", "housing")
    img, d = new(); disc(d); dome(d, oy=0.02); leaf(d, oy=0.02, k=0.45); save(img, "Structures", "algae_farm")
    img, d = new(); disc(d)
    d.rounded_rectangle([*P(0.32, 0.30), *P(0.68, 0.80)], radius=int(S * 0.05), fill=METAL, outline=METAL_DARK, width=W)
    bolt(d, k=0.6, oy=0.04)
    save(img, "Structures", "generator")
    img, d = new(); disc(d); tank(d); bubbles(d, ox=0.16, oy=-0.12, k=0.45); save(img, "Structures", "oxygen_plant")
    img, d = new(); disc(d); flask(d); save(img, "Structures", "lab")


# ---------------------------------------------------------------- especialistas (ids de Especialistas.csv)
def specialists():
    def make(name, color, dark, accessory):
        img, d = new(); disc(d); person(d, color, dark); accessory(d); save(img, "Specialists", name)

    def helmet(d):
        d.pieslice([*P(0.36, 0.20), *P(0.64, 0.46)], 180, 360, fill=AMBER, outline=AMBER_DARK, width=W)
        gear(d, 0.76, 0.76, 0.13)

    def leaf_badge(d):
        d.ellipse(box(0.76, 0.76, 0.15), fill=DISC, outline=GREEN_DARK, width=W)
        leaf(d, ox=0.26, oy=0.26, k=0.32)

    def flask_badge(d):
        d.ellipse(box(0.76, 0.76, 0.15), fill=DISC, outline=PURPLE_DARK, width=W)
        flask(d, ox=0.26, oy=0.26, k=0.30)

    def ruler(d):
        d.polygon([P(0.62, 0.90), P(0.92, 0.90), P(0.92, 0.60)], fill=AMBER, outline=AMBER_DARK, width=W)

    make("engineer", (70, 120, 190, 255), BLUE_DARK, helmet)
    make("biologist", GREEN, GREEN_DARK, leaf_badge)
    make("scientist", WHITE, METAL_DARK, flask_badge)
    make("architect", (90, 150, 210, 255), BLUE_DARK, ruler)


# ---------------------------------------------------------------- tecnologias (ids de Tecnologias.csv)
def technologies():
    def make(name, draw, kind=None):
        img, d = new(); hexagon(d); draw(d)
        if kind: badge(d, kind)
        save(img, "Technologies", name)

    make("algae", lambda d: leaf(d, k=0.85, ox=-0.04, oy=-0.04), "up")
    make("turbines", lambda d: bolt(d, k=0.85, ox=-0.04, oy=-0.04), "up")
    make("filters", lambda d: bubbles(d, k=0.85, ox=-0.04, oy=-0.04), "up")
    make("modular", lambda d: house(d, k=0.85, ox=-0.04, oy=-0.04), "plus")
    make("hydroponics", lambda d: (dome(d, k=0.8, ox=-0.04, oy=-0.02), leaf(d, k=0.38, ox=-0.04, oy=0.0)), "plus")
    make("geothermal", lambda d: (gear(d, 0.46, 0.46, 0.22, fill=AMBER, outline=AMBER_DARK), bolt(d, k=0.35, ox=-0.04, oy=-0.04, fill=WHITE, outline=METAL_DARK)), "plus")
    make("photosynthesis", lambda d: (leaf(d, k=0.75, ox=-0.08, oy=-0.04), bubbles(d, k=0.4, ox=0.12, oy=-0.16)), "plus")
    make("compact_housing", lambda d: house(d, k=0.85, ox=-0.04, oy=-0.04, fill=GLASS, outline=BLUE_DARK), "up")
    make("computing", chip, "up")
    make("monumental", lambda d: columns(d))


# ---------------------------------------------------------------- interface e alertas
def ui():
    img, d = new()
    d.polygon([P(0.50, 0.10), P(0.92, 0.86), P(0.08, 0.86)], fill=RED, outline=RED_DARK, width=int(S * 0.03))
    d.rectangle([*P(0.46, 0.34), *P(0.54, 0.62)], fill=WHITE)
    d.ellipse(box(0.50, 0.73, 0.045), fill=WHITE)
    save(img, "Ui", "alert")
    img, d = new(); disc(d, rim=AMBER_DARK); columns(d); save(img, "Ui", "monument")
    img, d = new(); disc(d); person(d, (70, 120, 190, 255), BLUE_DARK); save(img, "Ui", "population")


if __name__ == "__main__":
    resources()
    structures()
    specialists()
    technologies()
    ui()
