using System;
using UnityEngine;

namespace Drowned.Resources
{
    /// <summary>
    /// Uma quantidade de cada recurso (SCRUM-128). Serve para custo, produção, consumo e estoque.
    /// É uma struct imutável: as operações devolvem um novo valor.
    /// Serializável para aparecer no inspetor dentro de <see cref="StructureLevelData"/>.
    /// </summary>
    [Serializable]
    public struct ResourceBundle : IEquatable<ResourceBundle>
    {
        [SerializeField] private float _energy;
        [SerializeField] private float _oxygen;
        [SerializeField] private float _food;

        public static readonly ResourceBundle Zero = new ResourceBundle(0f, 0f, 0f);
        public static readonly ResourceBundle One = new ResourceBundle(1f, 1f, 1f);

        public ResourceBundle(float energy, float oxygen, float food)
        {
            _energy = energy;
            _oxygen = oxygen;
            _food = food;
        }

        public float Energy => _energy;
        public float Oxygen => _oxygen;
        public float Food => _food;

        public float this[ResourceType type] => type switch
        {
            ResourceType.Energy => _energy,
            ResourceType.Oxygen => _oxygen,
            ResourceType.Food => _food,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        /// <summary>Devolve uma cópia com o valor de um recurso trocado.</summary>
        public ResourceBundle With(ResourceType type, float value) => type switch
        {
            ResourceType.Energy => new ResourceBundle(value, _oxygen, _food),
            ResourceType.Oxygen => new ResourceBundle(_energy, value, _food),
            ResourceType.Food => new ResourceBundle(_energy, _oxygen, value),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        public bool IsZero => _energy == 0f && _oxygen == 0f && _food == 0f;

        /// <summary>Verdadeiro se este pacote tem pelo menos <paramref name="cost"/> de cada recurso.</summary>
        public bool Covers(ResourceBundle cost) =>
            _energy >= cost._energy && _oxygen >= cost._oxygen && _food >= cost._food;

        /// <summary>Quanto falta em cada recurso para cobrir <paramref name="cost"/>. Nunca negativo.</summary>
        public ResourceBundle Missing(ResourceBundle cost) => new ResourceBundle(
            Mathf.Max(0f, cost._energy - _energy),
            Mathf.Max(0f, cost._oxygen - _oxygen),
            Mathf.Max(0f, cost._food - _food));

        /// <summary>Multiplicação componente a componente. Usado pelos multiplicadores de produção dos especialistas.</summary>
        public ResourceBundle Scale(ResourceBundle factors) => new ResourceBundle(
            _energy * factors._energy, _oxygen * factors._oxygen, _food * factors._food);

        /// <summary>Cópia sem valores negativos. O estoque usa isso para nunca ficar abaixo de zero.</summary>
        public ResourceBundle ClampedToZero() => new ResourceBundle(
            Mathf.Max(0f, _energy), Mathf.Max(0f, _oxygen), Mathf.Max(0f, _food));

        public static ResourceBundle operator +(ResourceBundle a, ResourceBundle b) =>
            new ResourceBundle(a._energy + b._energy, a._oxygen + b._oxygen, a._food + b._food);

        public static ResourceBundle operator -(ResourceBundle a, ResourceBundle b) =>
            new ResourceBundle(a._energy - b._energy, a._oxygen - b._oxygen, a._food - b._food);

        public static ResourceBundle operator *(ResourceBundle a, float k) =>
            new ResourceBundle(a._energy * k, a._oxygen * k, a._food * k);

        public static ResourceBundle operator *(float k, ResourceBundle a) => a * k;

        public static ResourceBundle operator /(ResourceBundle a, float k) =>
            new ResourceBundle(a._energy / k, a._oxygen / k, a._food / k);

        public bool Equals(ResourceBundle other) =>
            _energy == other._energy && _oxygen == other._oxygen && _food == other._food;

        public override bool Equals(object obj) => obj is ResourceBundle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_energy, _oxygen, _food);
        public static bool operator ==(ResourceBundle a, ResourceBundle b) => a.Equals(b);
        public static bool operator !=(ResourceBundle a, ResourceBundle b) => !a.Equals(b);

        public override string ToString() => $"(E {_energy:0.##}, O {_oxygen:0.##}, A {_food:0.##})";
    }
}
