using System;

namespace Drowned.World
{
    /// <summary>Curvas das animações de estrutura (SCRUM-166, SCRUM-153), separadas para teste.</summary>
    public static class StructureAnimMath
    {
        /// <summary>
        /// "Montagem": escala de 0 a 1 com um leve exagero antes de assentar (ease-out-back).
        /// <paramref name="t"/> vai de 0 a 1; <paramref name="overshoot"/> 0 = sem exagero, ~1.7 = ~10% de exagero.
        /// </summary>
        public static float BuildPop(float t, float overshoot)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float s = Math.Max(0f, overshoot);
            float u = t - 1f;
            return 1f + (s + 1f) * u * u * u + s * u * u;
        }

        /// <summary>Respiração: 1 ± amplitude, com período de 1/speed segundos e fase própria por instância.</summary>
        public static float Breathe(float time, float phase, float amplitude, float speed)
        {
            return 1f + amplitude * (float)Math.Sin((time * speed) * Math.PI * 2.0 + phase);
        }

        /// <summary>Pulso entre 0 e 1 (para brilho), com período de 1/speed segundos.</summary>
        public static float Pulse01(float time, float phase, float speed)
        {
            return 0.5f + 0.5f * (float)Math.Sin((time * speed) * Math.PI * 2.0 + phase);
        }

        /// <summary>Fase estável a partir do id da estrutura, para instâncias vizinhas não pulsarem juntas.</summary>
        public static float PhaseFor(int id)
        {
            unchecked
            {
                uint h = (uint)id * 2654435761u;
                return (h % 6283u) / 1000f; // 0..2π
            }
        }
    }
}
