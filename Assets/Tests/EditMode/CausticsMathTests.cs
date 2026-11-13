using System;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class CausticsMathTests
    {
        [Test]
        public void Fica_entre_zero_e_um()
        {
            for (int i = 0; i < 40; i++)
                for (int j = 0; j < 40; j++)
                    Assert.That(CausticsMath.Intensity(i / 40f, j / 40f), Is.InRange(0f, 1f));
        }

        [Test]
        public void Repete_sem_emenda_com_periodo_um()
        {
            for (int i = 0; i < 20; i++)
            {
                float t = i / 20f;
                Assert.AreEqual(CausticsMath.Intensity(0f, t), CausticsMath.Intensity(1f, t), 1e-4f, "emenda em u");
                Assert.AreEqual(CausticsMath.Intensity(t, 0f), CausticsMath.Intensity(t, 1f), 1e-4f, "emenda em v");
            }
        }

        [Test]
        public void Tem_linhas_claras_e_fundo_escuro()
        {
            float min = 1f, max = 0f;
            for (int i = 0; i < 64; i++)
                for (int j = 0; j < 64; j++)
                {
                    float v = CausticsMath.Intensity(i / 64f, j / 64f);
                    min = Math.Min(min, v);
                    max = Math.Max(max, v);
                }
            Assert.Less(min, 0.15f);
            Assert.Greater(max, 0.6f);
        }

        [Test]
        public void Nitidez_invalida_lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CausticsMath.Intensity(0f, 0f, 0f));
        }
    }
}
