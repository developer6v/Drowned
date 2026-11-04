using System.Collections.Generic;
using UnityEngine;

namespace Drowned.Resources
{
    /// <summary>
    /// Dados de um tipo de estrutura (SCRUM-128): Moradia, Fazenda de algas, Gerador, Planta de oxigênio, Laboratório.
    /// Um asset por estrutura em <c>Assets/Data/Structures</c>. Os números vêm da planilha de balanceamento
    /// (SCRUM-118/143); ninguém digita valor no inspetor sem passar por ela.
    /// </summary>
    [CreateAssetMenu(fileName = "NovaEstrutura", menuName = "Drowned/Structure", order = 0)]
    public sealed class StructureData : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Id estável usado no save e na planilha (ex.: 'housing', 'algae_farm'). Não mude depois de publicado.")]
        [SerializeField] private string _id;

        [Tooltip("Nome mostrado na HUD, em português.")]
        [SerializeField] private string _displayName;

        [SerializeField] private StructureCategory _category;

        [Header("Grid")]
        [Tooltip("Quantas células a estrutura ocupa no GridMap.")]
        [SerializeField, Min(1)] private int _footprintWidth = 1;
        [SerializeField, Min(1)] private int _footprintHeight = 1;

        [Header("Desbloqueio (SCRUM-148)")]
        [Tooltip("Se verdadeiro, só pode ser construída depois de uma tecnologia com efeito 'Libera estrutura' para este id.")]
        [SerializeField] private bool _requiresTechnology;

        [Tooltip("Nível máximo sem tecnologia. Níveis acima exigem uma tecnologia com efeito 'Libera nível'.")]
        [SerializeField, Min(1)] private int _baseMaxLevel = 1;

        [Header("Níveis (índice 0 = nível 1 = construção)")]
        [SerializeField] private List<StructureLevelData> _levels = new List<StructureLevelData>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public StructureCategory Category => _category;
        public int FootprintWidth => _footprintWidth;
        public int FootprintHeight => _footprintHeight;
        public bool RequiresTechnology => _requiresTechnology;
        public int BaseMaxLevel => _baseMaxLevel;

        /// <summary>Define as exigências de desbloqueio em memória. Para testes e para o gerador da planilha.</summary>
        public void SetUnlockRequirements(bool requiresTechnology, int baseMaxLevel)
        {
            _requiresTechnology = requiresTechnology;
            _baseMaxLevel = Mathf.Max(1, baseMaxLevel);
        }

        /// <summary>Quantidade de níveis. O nível máximo é igual a este valor.</summary>
        public int LevelCount => _levels.Count;

        /// <summary>Dados do nível (1-based). Lança se o nível não existir.</summary>
        public StructureLevelData GetLevel(int level)
        {
            if (level < 1 || level > _levels.Count)
                throw new System.ArgumentOutOfRangeException(nameof(level), $"'{_id}' tem {_levels.Count} nível(is); pedido {level}.");
            return _levels[level - 1];
        }

        public bool HasLevel(int level) => level >= 1 && level <= _levels.Count;

        /// <summary>Custo de construção (nível 1).</summary>
        public ResourceBundle BuildCost => GetLevel(1).Cost;

        /// <summary>
        /// Cria uma instância em memória sem asset. Usado pelos testes e pelo gerador da planilha.
        /// </summary>
        public static StructureData Create(string id, string displayName, StructureCategory category,
            int footprintWidth, int footprintHeight, params StructureLevelData[] levels)
        {
            var data = CreateInstance<StructureData>();
            data._id = id;
            data._displayName = displayName;
            data._category = category;
            data._footprintWidth = footprintWidth;
            data._footprintHeight = footprintHeight;
            data._levels = new List<StructureLevelData>(levels);
            data.Validate(logWarnings: false);
            return data;
        }

        private void OnValidate() => Validate(logWarnings: true);

        private void Validate(bool logWarnings)
        {
            if (_footprintWidth < 1) _footprintWidth = 1;
            if (_footprintHeight < 1) _footprintHeight = 1;
            if (!logWarnings) return;
            if (string.IsNullOrWhiteSpace(_id))
                Debug.LogWarning($"StructureData '{name}': o campo Id está vazio.", this);
            if (_levels.Count == 0)
                Debug.LogWarning($"StructureData '{name}': precisa de pelo menos um nível.", this);
        }
    }
}
