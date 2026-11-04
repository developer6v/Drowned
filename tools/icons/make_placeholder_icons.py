"""
SCRUM-127 - Icones placeholder dos 3 recursos (energia, oxigenio, alimento).
Gera PNGs 128x128 com fundo transparente em Assets/Sprites/Placeholders/Icons/.
Flat design, paleta azul do tema com destaque ambar. Rodar:

    python -I tools/icons/make_placeholder_icons.py
"""
import math
import os
import sys
from PIL import Image, ImageDraw

SIZE = 128
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Sprites", "Placeholders", "Icons")

AMBER = (245, 166, 35, 255)
AMBER_DARK = (200, 120, 10, 255)
BLUE = (64, 160, 230, 255)
BLUE_DARK = (30, 90, 160, 255)
GREEN = (80, 200, 120, 255)
GREEN_DARK = (30, 130, 70, 255)
DISC = (18, 42, 72, 255)


def canvas():
    img = Image.new("RGBA", (SIZE * 4, SIZE * 4), (0, 0, 0, 0))  # desenha 4x e reduz: antialias
    return img, ImageDraw.Draw(img)


def finish(img, name):
    img = img.resize((SIZE, SIZE), Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"icon_{name}.png")
    img.save(path)
    print("ok", os.path.relpath(path))


def disc(d, s):
    pad = s * 0.06
    d.ellipse([pad, pad, s - pad, s - pad], fill=DISC)


def energia():
    img, d = canvas(); s = SIZE * 4
    disc(d, s)
    # raio
    pts = [(0.56, 0.14), (0.30, 0.54), (0.48, 0.54), (0.42, 0.88), (0.70, 0.44), (0.52, 0.44)]
    d.polygon([(x * s, y * s) for x, y in pts], fill=AMBER, outline=AMBER_DARK, width=int(s * 0.015))
    finish(img, "energia")


def oxigenio():
    img, d = canvas(); s = SIZE * 4
    disc(d, s)
    # tres bolhas
    for cx, cy, r in [(0.40, 0.62, 0.20), (0.66, 0.40, 0.13), (0.60, 0.72, 0.08)]:
        d.ellipse([(cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s], fill=BLUE, outline=BLUE_DARK, width=int(s * 0.015))
        # brilho
        hr = r * 0.3
        d.ellipse([(cx - r * 0.45 - hr) * s, (cy - r * 0.45 - hr) * s, (cx - r * 0.45 + hr) * s, (cy - r * 0.45 + hr) * s], fill=(220, 240, 255, 255))
    finish(img, "oxigenio")


def alimento():
    img, d = canvas(); s = SIZE * 4
    disc(d, s)
    # folha de alga: elipse inclinada + nervura
    leaf = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    ld = ImageDraw.Draw(leaf)
    ld.ellipse([s * 0.30, s * 0.18, s * 0.70, s * 0.84], fill=GREEN, outline=GREEN_DARK, width=int(s * 0.015))
    ld.line([(s * 0.50, s * 0.22), (s * 0.50, s * 0.82)], fill=GREEN_DARK, width=int(s * 0.02))
    for t in (0.35, 0.5, 0.65):
        y = s * (0.22 + t * 0.6)
        ld.line([(s * 0.50, y), (s * 0.36, y - s * 0.08)], fill=GREEN_DARK, width=int(s * 0.015))
        ld.line([(s * 0.50, y), (s * 0.64, y - s * 0.08)], fill=GREEN_DARK, width=int(s * 0.015))
    leaf = leaf.rotate(-25, resample=Image.BICUBIC, center=(s / 2, s / 2))
    img.alpha_composite(leaf)
    finish(img, "alimento")


if __name__ == "__main__":
    energia(); oxigenio(); alimento()
