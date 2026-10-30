using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class CameraBoundsTests
    {
        private const float Aspect16x9 = 16f / 9f;

        [Test]
        public void LimitesInvertidosLancam()
        {
            Assert.Throws<System.ArgumentException>(() => new CameraBounds(10, 0, 0, 10));
        }

        [Test]
        public void MaxOrthoSizeRespeitaOEixoMaisApertado()
        {
            // Mapa 32 de largura x 18 de altura: a 16:9 cabe exatamente com orthoSize 9
            var b = new CameraBounds(0, 0, 32, 18);
            Assert.AreEqual(9f, b.MaxOrthoSizeFor(Aspect16x9), 1e-4f);

            // Mapa estreito: a largura manda
            var estreito = new CameraBounds(0, 0, 16, 100);
            Assert.AreEqual(8f / Aspect16x9, estreito.MaxOrthoSizeFor(Aspect16x9), 1e-4f);
        }

        [Test]
        public void ClampOrthoSizeFicaEntreMinEOQueCabe()
        {
            var b = new CameraBounds(0, 0, 32, 18);
            Assert.AreEqual(3f, b.ClampOrthoSize(1f, 3f, 20f, Aspect16x9));
            Assert.AreEqual(9f, b.ClampOrthoSize(50f, 3f, 20f, Aspect16x9)); // limitado pelo mapa
            Assert.AreEqual(5f, b.ClampOrthoSize(5f, 3f, 20f, Aspect16x9));
            Assert.AreEqual(6f, b.ClampOrthoSize(50f, 3f, 6f, Aspect16x9));  // limitado pelo max
        }

        [Test]
        public void ClampOrthoSizeNuncaFicaAbaixoDoMinimoMesmoEmMapaMinusculo()
        {
            var b = new CameraBounds(0, 0, 2, 2);
            Assert.AreEqual(3f, b.ClampOrthoSize(10f, 3f, 20f, Aspect16x9));
        }

        [Test]
        public void ClampPositionMantemAAreaVisivelDentroDoMapa()
        {
            var b = new CameraBounds(0, 0, 100, 100);
            float size = 10f; // meia altura 10, meia largura 17.78
            var (x, y) = b.ClampPosition(-50, 200, size, Aspect16x9);
            Assert.AreEqual(size * Aspect16x9, x, 1e-4f);
            Assert.AreEqual(100 - size, y, 1e-4f);

            var (x2, y2) = b.ClampPosition(50, 50, size, Aspect16x9);
            Assert.AreEqual(50f, x2, 1e-4f);
            Assert.AreEqual(50f, y2, 1e-4f);
        }

        [Test]
        public void ClampPositionCentralizaQuandoOMapaEMenorQueATela()
        {
            var b = new CameraBounds(0, 0, 10, 10);
            var (x, y) = b.ClampPosition(999, -999, 20f, Aspect16x9);
            Assert.AreEqual(5f, x, 1e-4f);
            Assert.AreEqual(5f, y, 1e-4f);
        }

        [Test]
        public void ExpandCresceNasQuatroDirecoes()
        {
            var b = new CameraBounds(0, 0, 10, 10).Expand(2f);
            Assert.AreEqual(-2f, b.MinX);
            Assert.AreEqual(-2f, b.MinY);
            Assert.AreEqual(12f, b.MaxX);
            Assert.AreEqual(12f, b.MaxY);
            Assert.AreEqual(14f, b.Width);
        }
    }
}
