"""Gera sons placeholder (WAV mono 22050 Hz) para o AudioManager (SCRUM-187).

Uso:
    python tools/audio/generate_placeholders.py [--out Assets/Audio/Placeholder]

Sons simples por síntese (senos, ruído), só para ouvir os eventos até os sons finais
(SCRUM-158/159/172/173/186) existirem. Não depende de biblioteca externa.
Ficam em Assets/Audio/Resources/Placeholder para o AudioManager.LoadPlaceholders() achar via Resources.Load.
"""
import argparse
import math
import os
import random
import struct
import wave

RATE = 22050


def _write(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in samples))


def _env(i, n, attack=0.01, release=0.2):
    t = i / RATE
    total = n / RATE
    a = min(1.0, t / attack) if attack > 0 else 1.0
    r = min(1.0, (total - t) / release) if release > 0 else 1.0
    return a * r


def tone(freqs, seconds, volume=0.5, attack=0.01, release=0.2, wave_fn=math.sin):
    n = int(seconds * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        s = sum(wave_fn(2 * math.pi * f * t) for f in freqs) / len(freqs)
        out.append(s * volume * _env(i, n, attack, release))
    return out


def sweep(f0, f1, seconds, volume=0.5):
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        f = f0 + (f1 - f0) * i / n
        phase += 2 * math.pi * f / RATE
        out.append(math.sin(phase) * volume * _env(i, n, 0.005, 0.1))
    return out


def noise(seconds, volume=0.3, lowpass=0.2):
    n = int(seconds * RATE)
    out, prev = [], 0.0
    rnd = random.Random(7)
    for i in range(n):
        v = rnd.uniform(-1, 1)
        prev = prev + lowpass * (v - prev)
        out.append(prev * volume * _env(i, n, 0.005, 0.15))
    return out


def concat(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


def mix(a, b):
    n = max(len(a), len(b))
    return [(a[i] if i < len(a) else 0) + (b[i] if i < len(b) else 0) for i in range(n)]


def ambient(seconds=8.0):
    """Loop de ambiente: ruído grave filtrado com bolhas ocasionais."""
    n = int(seconds * RATE)
    rnd = random.Random(3)
    out, prev = [], 0.0
    for i in range(n):
        v = rnd.uniform(-1, 1)
        prev = prev + 0.03 * (v - prev)
        t = i / RATE
        swell = 0.6 + 0.4 * math.sin(2 * math.pi * t / 4.0)
        out.append(prev * 0.5 * swell)
    for k in range(10):
        start = int(rnd.uniform(0, seconds - 0.4) * RATE)
        b = sweep(300 + rnd.uniform(0, 300), 900 + rnd.uniform(0, 400), 0.25, 0.08)
        for j, s in enumerate(b):
            if start + j < n:
                out[start + j] += s
    # suaviza as pontas para o loop não estalar
    fade = int(0.05 * RATE)
    for i in range(fade):
        out[i] *= i / fade
        out[n - 1 - i] *= i / fade
    return out


SOUNDS = {
    "click": lambda: tone([1200], 0.05, 0.4, 0.002, 0.03),
    "build_confirm": lambda: concat(tone([660], 0.08, 0.4, 0.005, 0.03), tone([990], 0.14, 0.4, 0.005, 0.08)),
    "error": lambda: concat(tone([220, 233], 0.12, 0.45, 0.005, 0.05), tone([180, 190], 0.2, 0.45, 0.005, 0.1)),
    "construction": lambda: concat(noise(0.08, 0.5), noise(0.08, 0.4), noise(0.1, 0.5)),
    "unlock": lambda: concat(tone([523], 0.1, 0.4, 0.005, 0.04), tone([659], 0.1, 0.4, 0.005, 0.04), tone([784], 0.1, 0.4, 0.005, 0.04), tone([1047], 0.35, 0.4, 0.005, 0.25)),
    "crisis_alarm": lambda: concat(*([tone([880], 0.18, 0.45, 0.005, 0.05), tone([660], 0.18, 0.45, 0.005, 0.05)] * 3)),
    "monument_stage": lambda: mix(concat(tone([392], 0.2, 0.35), tone([523], 0.2, 0.35), tone([659], 0.6, 0.35, 0.01, 0.4)), sweep(200, 100, 1.0, 0.15)),
    "victory": lambda: concat(tone([523, 659], 0.25, 0.4), tone([659, 784], 0.25, 0.4), tone([784, 1047], 0.9, 0.4, 0.01, 0.6)),
    "defeat": lambda: concat(tone([440, 445], 0.4, 0.4), tone([349, 352], 0.4, 0.4), tone([262, 264], 1.2, 0.4, 0.01, 0.9)),
    "ambient_loop": ambient,
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default=os.path.join("Assets", "Audio", "Resources", "Placeholder"))
    args = parser.parse_args()
    for name, fn in SOUNDS.items():
        path = os.path.join(args.out, f"{name}.wav")
        _write(path, fn())
        print("ok", path)


if __name__ == "__main__":
    main()
