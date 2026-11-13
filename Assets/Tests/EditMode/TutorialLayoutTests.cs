using Drowned.UI;
using Drowned.World;
using NUnit.Framework;
using UnityEngine;

namespace Drowned.Tests
{
    public class TutorialLayoutTests
    {
        private static readonly Rect Screen = new Rect(0f, 0f, 1920f, 1080f);
        private static readonly Vector2 Bubble = new Vector2(600f, 180f);

        [Test]
        public void DimRects_cobrem_a_tela_menos_o_buraco()
        {
            var hole = new Rect(800f, 400f, 200f, 100f);
            var parts = TutorialLayout.DimRects(Screen, hole, 10f);
            Assert.AreEqual(4, parts.Length);
            float area = 0f;
            foreach (var p in parts) area += p.width * p.height;
            float holeArea = 220f * 120f;
            Assert.AreEqual(Screen.width * Screen.height - holeArea, area, 0.5f, "as 4 partes somam a tela menos o buraco");
            // Nenhuma parte invade o buraco
            var padded = new Rect(790f, 390f, 220f, 120f);
            foreach (var p in parts)
                Assert.IsFalse(p.Overlaps(new Rect(padded.x + 1, padded.y + 1, padded.width - 2, padded.height - 2)));
        }

        [Test]
        public void DimRects_cortam_buraco_fora_da_tela()
        {
            var hole = new Rect(-100f, 1000f, 300f, 200f); // canto de cima à esquerda, saindo da tela
            var parts = TutorialLayout.DimRects(Screen, hole, 0f);
            foreach (var p in parts)
            {
                Assert.GreaterOrEqual(p.width, 0f);
                Assert.GreaterOrEqual(p.height, 0f);
                Assert.GreaterOrEqual(p.xMin, Screen.xMin);
                Assert.LessOrEqual(p.yMax, Screen.yMax);
            }
        }

        [Test]
        public void Alvo_embaixo_poe_seta_acima_apontando_para_baixo()
        {
            var target = new Rect(100f, 10f, 190f, 68f); // botão da barra de construção
            var place = TutorialLayout.PlaceArrow(Screen, target, Bubble, 100f, 12f, 16f);
            Assert.AreEqual(ArrowSide.Above, place.Side);
            Assert.AreEqual(0f, place.Angle);
            Assert.AreEqual(target.yMax + 12f, place.Tip.y, 0.01f);
            Assert.Greater(place.BubbleCenter.y, place.Tip.y + 100f, "balão depois da seta");
        }

        [Test]
        public void Alvo_em_cima_poe_seta_abaixo_apontando_para_cima()
        {
            var target = new Rect(16f, 1022f, 640f, 52f); // recursos na barra de cima
            var place = TutorialLayout.PlaceArrow(Screen, target, Bubble, 100f, 12f, 16f);
            Assert.AreEqual(ArrowSide.Below, place.Side);
            Assert.AreEqual(180f, place.Angle);
            Assert.AreEqual(target.yMin - 12f, place.Tip.y, 0.01f);
            Assert.Less(place.BubbleCenter.y, place.Tip.y - 100f);
        }

        [Test]
        public void Balao_fica_dentro_da_tela_mesmo_com_alvo_no_canto()
        {
            var target = new Rect(1880f, 1040f, 40f, 40f);
            var place = TutorialLayout.PlaceArrow(Screen, target, Bubble, 100f, 12f, 16f);
            Assert.LessOrEqual(place.BubbleCenter.x + Bubble.x / 2f, Screen.xMax - 16f + 0.01f);
            Assert.GreaterOrEqual(place.BubbleCenter.y - Bubble.y / 2f, Screen.yMin + 16f - 0.01f);
            Assert.LessOrEqual(place.Tip.x, Screen.xMax - 16f + 0.01f, "ponta da seta também não sai da tela");
        }

        [Test]
        public void Union_junta_retangulos()
        {
            var u = TutorialLayout.Union(new[] { new Rect(0f, 0f, 10f, 10f), new Rect(20f, 5f, 10f, 20f) });
            Assert.AreEqual(Rect.MinMaxRect(0f, 0f, 30f, 25f), u);
            Assert.AreEqual(Rect.zero, TutorialLayout.Union(new Rect[0]));
        }

        [Test]
        public void Bob_vai_de_zero_a_amplitude()
        {
            Assert.AreEqual(0f, TutorialLayout.Bob(0f, 18f, 1f), 0.001f);
            Assert.AreEqual(18f, TutorialLayout.Bob(0.5f, 18f, 1f), 0.001f);
            Assert.AreEqual(0f, TutorialLayout.Bob(1f, 18f, 1f), 0.001f);
            for (float t = 0f; t < 2f; t += 0.07f)
            {
                float b = TutorialLayout.Bob(t, 18f, 1.4f);
                Assert.GreaterOrEqual(b, -0.001f);
                Assert.LessOrEqual(b, 18.001f);
            }
        }

        [Test]
        public void Pulse_fica_entre_minimo_e_um()
        {
            for (float t = 0f; t < 3f; t += 0.05f)
            {
                float p = TutorialLayout.Pulse(t, 1.2f, 0.35f);
                Assert.GreaterOrEqual(p, 0.35f - 0.001f);
                Assert.LessOrEqual(p, 1.001f);
            }
        }

        [Test]
        public void BuildSpot_acha_o_lugar_livre_mais_perto_do_centro()
        {
            var grid = new GridMap(10, 10);
            grid.SetBuildableRect(0, 0, 10, 10, true);
            grid.SetBuildableRect(3, 3, 4, 4, false); // "Monumento" no meio
            Assert.IsTrue(TutorialLayout.TryFindBuildSpot(grid, 1, 1, 5, 5, out var a));
            Assert.IsTrue(grid.CanPlace(a.x, a.y));
            int dist = Mathf.Max(Mathf.Abs(a.x - 5), Mathf.Abs(a.y - 5));
            // Bloco ocupa 3..6: do centro (5,5), x = 7 já está livre, a 2 células
            Assert.AreEqual(2, dist, "o anel livre mais próximo fica logo fora do bloco 4x4");
        }

        [Test]
        public void BuildSpot_respeita_pegada_maior_e_ocupacao()
        {
            var grid = new GridMap(6, 6);
            grid.SetBuildableRect(0, 0, 6, 6, true);
            grid.Place(2, 2, 2, 2);
            Assert.IsTrue(TutorialLayout.TryFindBuildSpot(grid, 2, 2, 2, 2, out var a));
            Assert.IsTrue(grid.CanPlace(a.x, a.y, 2, 2));
        }

        [Test]
        public void BuildSpot_falso_sem_lugar()
        {
            var grid = new GridMap(4, 4); // nada construível
            Assert.IsFalse(TutorialLayout.TryFindBuildSpot(grid, 1, 1, 2, 2, out _));
        }

        [Test]
        public void Camera_movida_por_distancia_ou_zoom()
        {
            Assert.IsFalse(TutorialLayout.CameraMoved(Vector2.zero, 8f, new Vector2(0.5f, 0.5f), 8f, 1.5f, 0.6f));
            Assert.IsTrue(TutorialLayout.CameraMoved(Vector2.zero, 8f, new Vector2(1.5f, 0f), 8f, 1.5f, 0.6f));
            Assert.IsTrue(TutorialLayout.CameraMoved(Vector2.zero, 8f, Vector2.zero, 7.2f, 1.5f, 0.6f));
        }
    }
}
