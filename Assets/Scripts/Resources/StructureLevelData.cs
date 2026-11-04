using System;
using UnityEngine;

namespace Drowned.Resources
{
    /// <summary>
    /// Um nível de uma estrutura (SCRUM-128). O nível 1 é a construção; os seguintes são upgrades (SCRUM-134).
    /// Produção e consumo são declarados <b>por dia</b>, como na planilha. A conversão para tick
    /// é feita pelo ResourceManager com o TicksPerDay do GameClock.
    /// </summary>
    [Serializable]
    public class StructureLevelData
    {
        [Tooltip("Custo para construir (nível 1) ou para subir até este nível (nível 2+).")]
        [SerializeField] private ResourceBundle _cost;

        [Tooltip("Quanto esta estrutura produz de cada recurso por dia simulado.")]
        [SerializeField] private ResourceBundle _productionPerDay;

        [Tooltip("Quanto esta estrutura consome de cada recurso por dia simulado (ex.: energia para funcionar).")]
        [SerializeField] private ResourceBundle _consumptionPerDay;

        [Tooltip("Quantos habitantes esta estrutura abriga. Zero para tudo que não é moradia.")]
        [SerializeField, Min(0)] private int _housingCapacity;

        [Tooltip("Pontos de pesquisa por dia (laboratório). Zero para as outras estruturas.")]
        [SerializeField, Min(0f)] private float _researchPerDay;

        [Tooltip("Sprite renderizado no Blender para este nível (direção padrão).")]
        [SerializeField] private Sprite _sprite;

        public StructureLevelData() { }

        /// <summary>Construtor para testes e para o gerador a partir da planilha.</summary>
        public StructureLevelData(ResourceBundle cost, ResourceBundle productionPerDay, ResourceBundle consumptionPerDay, int housingCapacity = 0, Sprite sprite = null, float researchPerDay = 0f)
        {
            if (housingCapacity < 0) throw new ArgumentOutOfRangeException(nameof(housingCapacity));
            if (researchPerDay < 0f) throw new ArgumentOutOfRangeException(nameof(researchPerDay));
            _cost = cost;
            _productionPerDay = productionPerDay;
            _consumptionPerDay = consumptionPerDay;
            _housingCapacity = housingCapacity;
            _sprite = sprite;
            _researchPerDay = researchPerDay;
        }

        public ResourceBundle Cost => _cost;
        public ResourceBundle ProductionPerDay => _productionPerDay;
        public ResourceBundle ConsumptionPerDay => _consumptionPerDay;
        public int HousingCapacity => _housingCapacity;
        public float ResearchPerDay => _researchPerDay;
        public Sprite Sprite => _sprite;

        /// <summary>Saldo por dia (produção menos consumo). Útil para a HUD mostrar o efeito de construir.</summary>
        public ResourceBundle NetPerDay => _productionPerDay - _consumptionPerDay;
    }
}
