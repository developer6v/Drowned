using System;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class ParallaxMathTests
    {
        [Test]
        public void Fator_zero_fica_parado_no_mundo()
        {
            Assert.AreEqual(0f, ParallaxMath.LayerOrigin(37f, 0f), 1e-5f);
        }

        [Test]
        public void Fator_um_anda_com_a_camera()
        {
            Assert.AreEqual(37f, ParallaxMath.LayerOrigin(37f, 1f), 1e-5f);
        }

        [Test]
        public void Fator_fora_de_zero_a_um_e_limitado()
        {
            Assert.AreEqual(10f, ParallaxMath.LayerOrigin(10f, 3f), 1e-5f);
            Assert.AreEqual(0f, ParallaxMath.LayerOrigin(10f, -1f), 1e-5f);
        }

        [Test]
        public void Bloco_base_sempre_contem_a_camera()
        {
            const float w = 90f;
            foreach (var cam in new[] { -500f, -91f, -45f, 0f, 12.3f, 89.9f, 90f, 1234.5f })
                foreach (var f in new[] { 0f, 0.3f, 0.6f, 0.85f, 1f })
                {
                    float b = ParallaxMath.TileBase(cam, f, w);
                    Assert.LessOrEqual(b, cam + 1e-3f, $"cam={cam} f={f}");
                    Assert.Greater(b + w, cam - 1e-3f, $"cam={cam} f={f}");
                }
        }

        [Test]
        public void Bloco_base_fica_alinhado_a_origem_da_camada()
        {
            const float w = 90f;
            float cam = 200f, f = 0.5f;
            float origin = ParallaxMath.LayerOrigin(cam, f);
            float b = ParallaxMath.TileBase(cam, f, w);
            float k = (b - origin) / w;
            Assert.AreEqual(Math.Round(k), k, 1e-4f, "o bloco precisa estar a um número inteiro de larguras da origem");
        }

        [Test]
        public void Largura_invalida_lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallaxMath.TileBase(0f, 0.5f, 0f));
        }

        [Test]
        public void Wrap_da_a_volta_nos_dois_sentidos()
        {
            Assert.AreEqual(1f, ParallaxMath.Wrap(11f, 0f, 10f), 1e-5f);
            Assert.AreEqual(9f, ParallaxMath.Wrap(-1f, 0f, 10f), 1e-5f);
            Assert.AreEqual(5f, ParallaxMath.Wrap(5f, 0f, 10f), 1e-5f);
            Assert.AreEqual(-3f, ParallaxMath.Wrap(-3f, -5f, 5f), 1e-5f);
        }

        [Test]
        public void Wrap_com_intervalo_vazio_devolve_o_minimo()
        {
            Assert.AreEqual(2f, ParallaxMath.Wrap(100f, 2f, 2f), 1e-5f);
        }
    }
}
