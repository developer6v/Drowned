using Drowned.World;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class BlobShadowTests
    {
        [Test]
        public void Mais_escura_no_centro_e_zero_na_borda()
        {
            Assert.AreEqual(1f, BlobShadow.Falloff(0f), 1e-6f);
            Assert.AreEqual(0f, BlobShadow.Falloff(1f));
            Assert.AreEqual(0f, BlobShadow.Falloff(1.5f));
            Assert.Greater(BlobShadow.Falloff(0.3f), BlobShadow.Falloff(0.7f));
        }
    }
}
