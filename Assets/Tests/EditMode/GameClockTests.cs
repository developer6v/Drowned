using System.Collections.Generic;
using Drowned.Core;
using NUnit.Framework;

namespace Drowned.Tests
{
    public class GameClockTests
    {
        [Test]
        public void ComecaNoDia1TickZeroVelocidadeNormal()
        {
            var clock = new GameClock(60f, 24);
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(0, clock.TickOfDay);
            Assert.AreEqual(0, clock.TotalTicks);
            Assert.AreEqual(GameSpeed.Normal, clock.Speed);
            Assert.IsFalse(clock.IsPaused);
        }

        [Test]
        public void ParametrosInvalidosLancam()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GameClock(0f, 24));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GameClock(60f, 0));
        }

        [Test]
        public void UmTickDisparaDepoisDeSecondsPerTick()
        {
            var clock = new GameClock(24f, 24); // 1 segundo por tick
            var ticks = new List<int>();
            clock.OnTick += ticks.Add;

            clock.Advance(0.5f);
            Assert.IsEmpty(ticks);

            clock.Advance(0.5f);
            Assert.AreEqual(new[] { 0 }, ticks);
            Assert.AreEqual(1, clock.TickOfDay);
        }

        [Test]
        public void UmDiaCompletoDispara24TicksEUmNovoDia()
        {
            var clock = new GameClock(24f, 24);
            int ticks = 0, days = 0, lastDay = 0;
            clock.OnTick += _ => ticks++;
            clock.OnNewDay += d => { days++; lastDay = d; };

            for (int i = 0; i < 24; i++) clock.Advance(1f);

            Assert.AreEqual(24, ticks);
            Assert.AreEqual(1, days);
            Assert.AreEqual(2, lastDay);
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(0, clock.TickOfDay);
        }

        [Test]
        public void FrameLongoDisparaVariosTicksNaOrdem()
        {
            var clock = new GameClock(24f, 24);
            var ticks = new List<int>();
            clock.OnTick += ticks.Add;

            clock.Advance(3.5f);

            Assert.AreEqual(new[] { 0, 1, 2 }, ticks);
            Assert.AreEqual(3, clock.TickOfDay);
        }

        [Test]
        public void FrameMaiorQueUmDiaViraODiaSemPerderTicks()
        {
            var clock = new GameClock(10f, 10);
            int ticks = 0;
            var days = new List<int>();
            clock.OnTick += _ => ticks++;
            clock.OnNewDay += days.Add;

            clock.Advance(25f); // 2 dias e meio

            Assert.AreEqual(25, ticks);
            Assert.AreEqual(new[] { 2, 3 }, days);
            Assert.AreEqual(3, clock.Day);
            Assert.AreEqual(5, clock.TickOfDay);
        }

        [Test]
        public void VelocidadeMultiplicaOTempo()
        {
            var clock = new GameClock(24f, 24);
            int ticks = 0;
            clock.OnTick += _ => ticks++;

            clock.SetSpeed(GameSpeed.Fastest);
            clock.Advance(1f);

            Assert.AreEqual(3, ticks);
            Assert.AreEqual(1.0, clock.PlayedSeconds, 1e-6);
            Assert.AreEqual(3.0, clock.SimulatedSeconds, 1e-6);
        }

        [Test]
        public void PausadoNaoAvancaNemContaTempoJogado()
        {
            var clock = new GameClock(24f, 24);
            int ticks = 0;
            clock.OnTick += _ => ticks++;

            clock.SetSpeed(GameSpeed.Paused);
            clock.Advance(100f);

            Assert.AreEqual(0, ticks);
            Assert.AreEqual(0.0, clock.PlayedSeconds);
            Assert.AreEqual(1, clock.Day);
        }

        [Test]
        public void TogglePauseVoltaParaAVelocidadeAnterior()
        {
            var clock = new GameClock();
            clock.SetSpeed(GameSpeed.Fast);

            clock.TogglePause();
            Assert.IsTrue(clock.IsPaused);

            clock.TogglePause();
            Assert.AreEqual(GameSpeed.Fast, clock.Speed);
        }

        [Test]
        public void OnSpeedChangedSoDisparaQuandoMuda()
        {
            var clock = new GameClock();
            var changes = new List<GameSpeed>();
            clock.OnSpeedChanged += changes.Add;

            clock.SetSpeed(GameSpeed.Normal); // já era Normal
            clock.SetSpeed(GameSpeed.Fast);
            clock.SetSpeed(GameSpeed.Fast);
            clock.SetSpeed(GameSpeed.Paused);

            Assert.AreEqual(new[] { GameSpeed.Fast, GameSpeed.Paused }, changes);
        }

        [Test]
        public void DayProgressVaiDeZeroAUm()
        {
            var clock = new GameClock(10f, 10);
            Assert.AreEqual(0f, clock.DayProgress, 1e-5f);
            clock.Advance(5f);
            Assert.AreEqual(0.5f, clock.DayProgress, 1e-5f);
            clock.Advance(5f);
            Assert.AreEqual(0f, clock.DayProgress, 1e-5f); // virou o dia
            Assert.AreEqual(2, clock.Day);
        }

        [Test]
        public void RestoreRecolocaDiaETickEContinuaDoPontoCerto()
        {
            var clock = new GameClock(24f, 24);
            clock.Restore(day: 5, tickOfDay: 23, simulatedSeconds: 1000, playedSeconds: 500);
            Assert.AreEqual(5, clock.Day);
            Assert.AreEqual(23, clock.TickOfDay);
            Assert.AreEqual(4 * 24 + 23, clock.TotalTicks);

            int newDay = 0;
            clock.OnNewDay += d => newDay = d;
            clock.Advance(1f); // completa o último tick e vira o dia
            Assert.AreEqual(6, newDay);
            Assert.AreEqual(0, clock.TickOfDay);
        }

        [Test]
        public void MultiplicadoresDeVelocidade()
        {
            Assert.AreEqual(0f, GameSpeed.Paused.Multiplier());
            Assert.AreEqual(1f, GameSpeed.Normal.Multiplier());
            Assert.AreEqual(2f, GameSpeed.Fast.Multiplier());
            Assert.AreEqual(3f, GameSpeed.Fastest.Multiplier());
        }
    }
}
