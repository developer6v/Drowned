using System;
using Drowned.Resources;
using UnityEngine;

namespace Drowned.Research
{
    /// <summary>O que uma tecnologia faz ao ser concluída (SCRUM-146 / SCRUM-148).</summary>
    public enum TechnologyEffectType
    {
        /// <summary>Libera a construção de uma estrutura com RequiresTechnology.</summary>
        UnlockStructure = 0,
        /// <summary>Libera upgrades de uma estrutura até o nível indicado.</summary>
        UnlockLevel = 1,
        /// <summary>Soma uma fração ao multiplicador de produção de um recurso.</summary>
        ProductionBonus = 2,
        /// <summary>Soma uma fração à velocidade de pesquisa.</summary>
        ResearchBonus = 3,
        /// <summary>Soma uma fração à capacidade de moradia total.</summary>
        HousingBonus = 4
    }

    [Serializable]
    public class TechnologyEffect
    {
        [SerializeField] private TechnologyEffectType _type;

        [Tooltip("Id da estrutura (UnlockStructure / UnlockLevel).")]
        [SerializeField] private string _structureId;

        [Tooltip("Nível máximo liberado (UnlockLevel).")]
        [SerializeField, Min(1)] private int _level = 2;

        [Tooltip("Recurso afetado (ProductionBonus).")]
        [SerializeField] private ResourceType _resource;

        [Tooltip("Fração do bônus: 0.2 = +20% (ProductionBonus, ResearchBonus, HousingBonus).")]
        [SerializeField, Min(0f)] private float _value = 0.2f;

        public TechnologyEffect() { }

        private TechnologyEffect(TechnologyEffectType type, string structureId, int level, ResourceType resource, float value)
        {
            _type = type;
            _structureId = structureId;
            _level = level;
            _resource = resource;
            _value = value;
        }

        public static TechnologyEffect UnlockStructure(string structureId) =>
            new TechnologyEffect(TechnologyEffectType.UnlockStructure, structureId, 1, ResourceType.Energy, 0f);

        public static TechnologyEffect UnlockLevel(string structureId, int level) =>
            new TechnologyEffect(TechnologyEffectType.UnlockLevel, structureId, Mathf.Max(1, level), ResourceType.Energy, 0f);

        public static TechnologyEffect ProductionBonus(ResourceType resource, float value) =>
            new TechnologyEffect(TechnologyEffectType.ProductionBonus, null, 1, resource, Mathf.Max(0f, value));

        public static TechnologyEffect ResearchBonus(float value) =>
            new TechnologyEffect(TechnologyEffectType.ResearchBonus, null, 1, ResourceType.Energy, Mathf.Max(0f, value));

        public static TechnologyEffect HousingBonus(float value) =>
            new TechnologyEffect(TechnologyEffectType.HousingBonus, null, 1, ResourceType.Energy, Mathf.Max(0f, value));

        public TechnologyEffectType Type => _type;
        public string StructureId => _structureId;
        public int Level => _level;
        public ResourceType Resource => _resource;
        public float Value => _value;

        /// <summary>Descrição curta em português para o painel de pesquisa.</summary>
        public string Describe(Func<string, string> structureName = null)
        {
            string name = structureName != null ? structureName(_structureId) : _structureId;
            int pct = Mathf.RoundToInt(_value * 100f);
            return _type switch
            {
                TechnologyEffectType.UnlockStructure => $"Libera {name}",
                TechnologyEffectType.UnlockLevel => $"Libera nível {_level} de {name}",
                TechnologyEffectType.ProductionBonus => $"+{pct}% de produção de {_resource.Label().ToLowerInvariant()}",
                TechnologyEffectType.ResearchBonus => $"+{pct}% de velocidade de pesquisa",
                TechnologyEffectType.HousingBonus => $"+{pct}% de capacidade de moradia",
                _ => "?"
            };
        }
    }
}
