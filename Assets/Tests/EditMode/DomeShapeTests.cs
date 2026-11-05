using System;
using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class DomeShapeTests
    {
        [Test]
        public void Centro_esta_dentro_e_cantos_fora()
        {
            Assert.Less(DomeShape.Edge(0f, 0f, 0.5f, 0.6f), 1f);
            Assert.Greater(DomeShape.Edge(1f, 0.6f, 0.5f, 0.6f), 1f);
            Assert.Greater(DomeShape.Edge(-1f, -0.5f, 0.5f, 0.6f), 1f);
        }

        [Test]
        public void Topo_e_base_ficam_na_borda()
        {
            Assert.AreEqual(1f, DomeShape.Edge(0f, 0.6f, 0.5f, 0.6f), 1e-5f, "topo da cúpula");
            Assert.AreEqual(1f, DomeShape.Edge(0f, -0.5f, 0.5f, 0.6f), 1e-5f, "frente da base");
            Assert.AreEqual(1f, DomeShape.Edge(1f, 0f, 0.5f, 0.6f), 1e-5f, "lateral");
        }

        [Test]
        public void Alpha_e_zero_fora_e_mais_forte_na_borda_que_no_miolo()
        {
            Assert.AreEqual(0f, DomeShape.Alpha(1.1f, 0f, 0.5f, 0.6f, 0.04f, 0.35f, 0.12f));
            float center = DomeShape.Alpha(0f, 0.1f, 0.5f, 0.6f, 0.04f, 0.35f, 0.12f);
            float rim = DomeShape.Alpha(0.99f, 0f, 0.5f, 0.6f, 0.04f, 0.35f, 0.12f);
            Assert.AreEqual(0.04f, center, 1e-4f);
            Assert.Greater(rim, center + 0.2f);
            Assert.LessOrEqual(rim, 1f);
        }

        [Test]
        public void Medidas_invalidas_lancam()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DomeShape.Edge(0f, 0f, 0f, 0.6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => DomeShape.Edge(0f, 0f, 0.5f, 0f));
        }
    }
}
