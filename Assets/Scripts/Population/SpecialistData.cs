using UnityEngine;

namespace Drowned.Population
{
    /// <summary>
    /// Dados de um especialista (SCRUM-144): Engenheiro, Cientista, Biólogo, Arquiteto.
    /// Um asset por especialista em <c>Assets/Data/Specialists</c>. Thresholds e bônus vêm da aba População da planilha (SCRUM-150).
    /// </summary>
    [CreateAssetMenu(fileName = "NovoEspecialista", menuName = "Drowned/Specialist", order = 2)]
    public sealed class SpecialistData : ScriptableObject
    {
        [Tooltip("Id estável usado no save (ex.: 'engineer').")]
        [SerializeField] private string _id;

        [Tooltip("Nome mostrado na HUD, em português.")]
        [SerializeField] private string _displayName;

        [Tooltip("Frase curta para o painel (ex.: 'Otimiza os geradores da cidade').")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("População necessária para desbloquear.")]
        [SerializeField, Min(1)] private int _populationThreshold = 10;

        [SerializeField] private SpecialistBonus _bonus;

        [Tooltip("Fração do bônus: 0.2 = +20% de produção ou -20% de custo.")]
        [SerializeField, Min(0f)] private float _bonusValue = 0.2f;

        [Tooltip("Ícone para o painel de especialistas.")]
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int PopulationThreshold => _populationThreshold;
        public SpecialistBonus Bonus => _bonus;
        public float BonusValue => _bonusValue;
        public Sprite Icon => _icon;

        /// <summary>Texto do bônus em português.</summary>
        public string BonusText => _bonus.Describe(_bonusValue);

        /// <summary>Cria uma instância em memória sem asset. Para testes e para o gerador da planilha.</summary>
        public static SpecialistData Create(string id, string displayName, int populationThreshold, SpecialistBonus bonus, float bonusValue, string description = "")
        {
            var data = CreateInstance<SpecialistData>();
            data._id = id;
            data._displayName = displayName;
            data._description = description;
            data._populationThreshold = Mathf.Max(1, populationThreshold);
            data._bonus = bonus;
            data._bonusValue = Mathf.Max(0f, bonusValue);
            return data;
        }
    }
}
