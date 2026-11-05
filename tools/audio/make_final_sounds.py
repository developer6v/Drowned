"""
Sons finais do Drowned (SCRUM-158/159/172/173/186).

1. Copia os efeitos escolhidos dos pacotes CC0 da Kenney (Interface Sounds, Impact Sounds, Music Jingles)
   para Assets/Audio/Resources/Final/<nome>.ogg.
2. Gera o loop de ambiente subaquático (ambient_loop.ogg) por síntese: ronco grave filtrado, ondas lentas,
   bolhas e tons distantes, 40 s, com as pontas cruzadas para o loop não ter emenda.

O AudioManager procura primeiro em Resources/Final e cai em Resources/Placeholder se faltar algum.

Uso (precisa de numpy e de ffmpeg no PATH):

    python -I tools/audio/make_final_sounds.py --kenney <pasta com os zips extraídos>

<pasta> deve ter interface-sounds/, impact-sounds/ e music-jingles/ (cada um com o conteúdo do zip).
Os pacotes estão em https://kenney.nl/assets (licença CC0).
"""
import argparse
import os
import shutil
import subprocess

import numpy as np

RATE = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Audio", "Resources", "Final")

# nome no AudioManager -> (pacote, caminho dentro do pacote). Escolhas: nomes descritivos da Kenney e, nos
# jingles, direção melódica medida (sobe = vitória, desce = derrota).
PICKS = {
    "click": ("interface-sounds", "Audio/click_002.ogg"),
    "build_confirm": ("interface-sounds", "Audio/confirmation_002.ogg"),
    "error": ("interface-sounds", "Audio/error_006.ogg"),
    "unlock": ("interface-sounds", "Audio/maximize_008.ogg"),
    "construction": ("impact-sounds", "Audio/impactMetal_medium_001.ogg"),
    "monument_stage": ("impact-sounds", "Audio/impactBell_heavy_001.ogg"),
    "victory": ("music-jingles", "Audio/Hit jingles/jingles_HIT15.ogg"),
    "defeat": ("music-jingles", "Audio/Steel jingles/jingles_STEEL06.ogg"),
}


def find(base, pack, rel):
    """Acha o arquivo aceitando o conteúdo do zip direto na pasta do pacote ou dentro de x/."""
    for root in (os.path.join(base, pack), os.path.join(base, pack, "x")):
        p = os.path.join(root, rel)
        if os.path.exists(p):
            return p
    raise FileNotFoundError(f"{pack}/{rel}")


def lowpass(x, cutoff):
    """Passa-baixa de 1 polo, aplicado duas vezes (12 dB/oitava)."""
    a = np.exp(-2.0 * np.pi * cutoff / RATE)
    for _ in range(2):
        y = np.empty_like(x)
        acc = 0.0
        for i in range(len(x)):
            acc = (1 - a) * x[i] + a * acc
            y[i] = acc
        x = y
    return x


def ambient(seconds=40.0, seed=7):
    rng = np.random.default_rng(seed)
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    # ronco: ruído browniano filtrado, com ondas lentas de volume (loop-safe: períodos dividem a duração)
    noise = np.cumsum(rng.standard_normal(n))
    noise -= np.convolve(noise, np.ones(4410) / 4410, mode="same")  # tira a deriva
    rumble = lowpass(noise / (np.abs(noise).max() + 1e-9), 220.0)
    swell = 0.55 + 0.45 * np.sin(2 * np.pi * t * (2 / seconds)) * np.sin(2 * np.pi * t * (5 / seconds) + 1.3)
    out = rumble * swell * 0.9

    # tons distantes (graves, lentos, com vibrato)
    for k, (f0, start, dur) in enumerate([(98.0, 4.0, 7.0), (82.4, 17.0, 8.0), (110.0, 29.0, 6.0)]):
        i0, i1 = int(start * RATE), int(min(seconds, start + dur) * RATE)
        tt = t[i0:i1] - start
        env = np.sin(np.pi * tt / dur) ** 2
        vib = 1 + 0.004 * np.sin(2 * np.pi * 0.7 * tt + k)
        out[i0:i1] += 0.10 * env * np.sin(2 * np.pi * f0 * vib * tt)

    # bolhas: chirps curtos subindo de tom
    for _ in range(26):
        start = rng.uniform(0.5, seconds - 0.5)
        dur = rng.uniform(0.04, 0.12)
        f0 = rng.uniform(500, 1100)
        i0 = int(start * RATE)
        tt = np.arange(int(dur * RATE)) / RATE
        env = np.exp(-tt / (dur * 0.35))
        chirp = np.sin(2 * np.pi * (f0 * tt + f0 * 1.8 * tt * tt / dur))
        out[i0:i0 + len(tt)] += rng.uniform(0.04, 0.1) * env * chirp

    # emenda: cruza os últimos 2 s com os primeiros 2 s
    fade = int(2.0 * RATE)
    w = np.linspace(0, 1, fade)
    head = out[:fade].copy()
    out[:fade] = head * w + out[-fade:] * (1 - w)
    out = out[:-fade]

    out /= np.abs(out).max() + 1e-9
    return (out * 0.7).astype(np.float32)


def encode_ogg(samples, path):
    proc = subprocess.run(
        ["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(RATE), "-ac", "1", "-i", "-", "-c:a", "libvorbis", "-q:a", "4", path],
        input=samples.tobytes(), capture_output=True)
    if proc.returncode != 0:
        raise RuntimeError(proc.stderr.decode(errors="replace"))


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--kenney", required=True, help="Pasta com interface-sounds/, impact-sounds/ e music-jingles/")
    p.add_argument("--out", default=OUT)
    args = p.parse_args()
    os.makedirs(args.out, exist_ok=True)
    for name, (pack, rel) in PICKS.items():
        src = find(args.kenney, pack, rel)
        dst = os.path.join(args.out, name + ".ogg")
        shutil.copyfile(src, dst)
        print("ok", name, "<-", pack, rel)
    amb = os.path.join(args.out, "ambient_loop.ogg")
    encode_ogg(ambient(), amb)
    print("ok ambient_loop (gerado)")


if __name__ == "__main__":
    main()
