using System.Collections.Generic;
using UnityEngine;

namespace Drowned.Monument
{
    /// <summary>
    /// Dados do Monumento Central (SCRUM-160): sprite da ruína e as etapas de restauração.
    /// Um único asset em <c>Assets/Data/Monument.asset</c>, a partir da aba Monumento da planilha (SCRUM-163).
    /// </summary>
    [CreateAssetMenu(fileName = "Monument", menuName = "Drowned/Monument", order = 4)]
    public sealed class MonumentData : ScriptableObject
    {
        [Tooltip("Sprite do Monumento em ruína, antes de qualquer etapa.")]
        [SerializeField] private Sprite _ruinSprite;

        [Tooltip("Etapas em ordem: Fundação, Estrutura, Sistemas, Acabamento.")]
        [SerializeField] private List<MonumentStage> _stages = new List<MonumentStage>();

        public Sprite RuinSprite => _ruinSprite;
        public IReadOnlyList<MonumentStage> Stages => _stages;
        public int StageCount => _stages.Count;

        /// <summary>Cria em memória sem asset. Para testes e para o gerador da planilha.</summary>
        public static MonumentData Create(params MonumentStage[] stages)
        {
            var data = CreateInstance<MonumentData>();
            data._stages = new List<MonumentStage>(stages);
            return data;
        }

        private void OnValidate()
        {
            if (_stages.Count == 0)
                Debug.LogWarning($"MonumentData '{name}': precisa de pelo menos uma etapa.", this);
        }
    }
}
