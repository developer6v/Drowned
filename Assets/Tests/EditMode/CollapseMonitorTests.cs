using System.Collections.Generic;
using Drowned.Core;
using Drowned.Resources;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class CollapseMonitorTests
    {
        private static ResourceManager Resources(float energy, float oxygen, float food)
            => new ResourceManager(new ResourceBundle(energy, oxygen, food), 24);

        [Test]
        public void ParametrosInvalidosLancam()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CollapseMonitor(0, Resources(1, 1, 1)));
            Assert.Throws<System.ArgumentNullException>(() => new CollapseMonitor(3, null));
        }

        [Test]
        public void ContaDiasConsecutivosEColapsaUmaVez()
        {
            var rm = Resources(0f, 10f, 10f);
            var monitor = new CollapseMonitor(3, rm);
            int collapses = 0;
            var danger = new List<int>();
            monitor.OnCollapse += () => collapses++;
            monitor.OnDangerChanged += danger.Add;

            Assert.IsTrue(monitor.InDanger);
            monitor.EvaluateDay();
            Assert.AreEqual(2, monitor.DaysRemaining);
            monitor.EvaluateDay();
            Assert.AreEqual(0, collapses);
            monitor.EvaluateDay();
            Assert.AreEqual(1, collapses);
            Assert.IsTrue(monitor.Collapsed);
            Assert.AreEqual(0, monitor.DaysRemaining);

            monitor.EvaluateDay();
            Assert.AreEqual(1, collapses);
            Assert.AreEqual(new[] { 1, 2, 3 }, danger);
        }

        [Test]
        public void RecuperarZeraAContagem()
        {
            var rm = Resources(0f, 10f, 10f);
            var monitor = new CollapseMonitor(3, rm);
            var danger = new List<int>();
            monitor.OnDangerChanged += danger.Add;

            monitor.EvaluateDay();
            monitor.EvaluateDay();
            rm.Add(new ResourceBundle(5f, 0f, 0f));
            monitor.EvaluateDay();
            Assert.AreEqual(0, monitor.ConsecutiveDays);
            Assert.IsFalse(monitor.Collapsed);
            monitor.EvaluateDay(); // continua 0, sem evento repetido
            Assert.AreEqual(new[] { 1, 2, 0 }, danger);
        }

        [Test]
        public void OxigenioZeradoTambemConta()
        {
            var monitor = new CollapseMonitor(1, Resources(10f, 0f, 10f));
            bool collapsed = false;
            monitor.OnCollapse += () => collapsed = true;
            monitor.EvaluateDay();
            Assert.IsTrue(collapsed);
        }

        [Test]
        public void AlimentoZeradoNaoConta()
        {
            var monitor = new CollapseMonitor(1, Resources(10f, 10f, 0f));
            Assert.IsFalse(monitor.InDanger);
            monitor.EvaluateDay();
            Assert.IsFalse(monitor.Collapsed);
            Assert.AreEqual(0, monitor.ConsecutiveDays);
        }
    }
}
