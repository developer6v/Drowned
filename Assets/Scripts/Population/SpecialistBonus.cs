namespace Drowned.Population
{
    /// <summary>Tipo de bônus que um especialista aplica ao ser desbloqueado (SCRUM-145).</summary>
    public enum SpecialistBonus
    {
        /// <summary>Engenheiro: +X% de produção de energia.</summary>
        EnergyProduction = 0,
        /// <summary>Biólogo: +X% de produção de alimento.</summary>
        FoodProduction = 1,
        /// <summary>Cientista: +X% de pontos de pesquisa.</summary>
        ResearchSpeed = 2,
        /// <summary>Arquiteto: -X% no custo de construção e upgrade.</summary>
        BuildCostDiscount = 3
    }

    public static class SpecialistBonusExtensions
    {
        /// <summary>Descrição curta em português para o painel de especialistas.</summary>
        public static string Describe(this SpecialistBonus bonus, float value)
        {
            int pct = UnityEngine.Mathf.RoundToInt(value * 100f);
            return bonus switch
            {
                SpecialistBonus.EnergyProduction => $"+{pct}% de produção de energia",
                SpecialistBonus.FoodProduction => $"+{pct}% de produção de alimento",
                SpecialistBonus.ResearchSpeed => $"+{pct}% de velocidade de pesquisa",
                SpecialistBonus.BuildCostDiscount => $"-{pct}% no custo de construção",
                _ => "?"
            };
        }
    }
}
