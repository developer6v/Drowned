using System.Collections.Generic;
using UnityEngine;

namespace Drowned.Research
{
    /// <summary>
    /// Dados de uma tecnologia (SCRUM-146). Um asset por tecnologia em <c>Assets/Data/Technologies</c>,
    /// a partir da árvore da SCRUM-149 na aba Tecnologias da planilha.
    /// </summary>
    [CreateAssetMenu(fileName = "NovaTecnologia", menuName = "Drowned/Technology", order = 3)]
    public sealed class TechnologyData : ScriptableObject
    {
        [Tooltip("Id estável usado no save (ex.: 'hydroponics').")]
        [SerializeField] private string _id;

        [Tooltip("Nome mostrado no painel, em português.")]
        [SerializeField] private string _displayName;

        [SerializeField, TextArea] private string _description;

        [Tooltip("Pontos de pesquisa necessários.")]
        [SerializeField, Min(1f)] private float _cost = 50f;

        [SerializeField] private List<TechnologyData> _prerequisites = new List<TechnologyData>();
        [SerializeField] private List<TechnologyEffect> _effects = new List<TechnologyEffect>();
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float Cost => _cost;
        public IReadOnlyList<TechnologyData> Prerequisites => _prerequisites;
        public IReadOnlyList<TechnologyEffect> Effects => _effects;
        public Sprite Icon => _icon;

        /// <summary>Cria uma instância em memória sem asset. Para testes e para o gerador da planilha.</summary>
        public static TechnologyData Create(string id, string displayName, float cost, IEnumerable<TechnologyData> prerequisites, params TechnologyEffect[] effects)
        {
            var data = CreateInstance<TechnologyData>();
            data._id = id;
            data._displayName = displayName;
            data._cost = Mathf.Max(1f, cost);
            data._prerequisites = prerequisites != null ? new List<TechnologyData>(prerequisites) : new List<TechnologyData>();
            data._effects = new List<TechnologyEffect>(effects);
            data._prerequisites.RemoveAll(p => p == null || p == data);
            return data;
        }

        private void OnValidate()
        {
            if (_cost < 1f) _cost = 1f;
            if (string.IsNullOrWhiteSpace(_id))
                Debug.LogWarning($"TechnologyData '{name}': o campo Id está vazio.", this);
            if (_prerequisites.Contains(this))
                Debug.LogWarning($"TechnologyData '{name}': não pode ser pré-requisito de si mesma.", this);
        }
    }
}
