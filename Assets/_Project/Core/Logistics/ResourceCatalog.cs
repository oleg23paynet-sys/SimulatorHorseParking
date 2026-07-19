#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;

namespace HorseParking.Core.Logistics
{
    /// <summary>Stable, language-independent identifier of a material or resource.</summary>
    public readonly struct ResourceId : IEquatable<ResourceId>, IComparable<ResourceId>
    {
        private readonly string? value;

        public ResourceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Resource id is required.", nameof(value));
            }

            this.value = value.Trim();
        }

        public string Value => value ?? string.Empty;
        public bool Equals(ResourceId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is ResourceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(ResourceId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => Value;
        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
    }

    /// <summary>Data-driven rules for one resource. Player-facing text remains a localization key.</summary>
    public sealed class ResourceDefinition
    {
        public ResourceDefinition(ResourceId id, LocalizationKey displayNameKey, int capacityPerUnit)
        {
            if (capacityPerUnit <= 0) throw new ArgumentOutOfRangeException(nameof(capacityPerUnit));
            Id = id;
            DisplayNameKey = displayNameKey;
            CapacityPerUnit = capacityPerUnit;
        }

        public ResourceId Id { get; }
        public LocalizationKey DisplayNameKey { get; }
        public int CapacityPerUnit { get; }
    }

    /// <summary>Immutable runtime catalog assembled from external configuration.</summary>
    public sealed class ResourceCatalog
    {
        private readonly Dictionary<ResourceId, ResourceDefinition> definitionsById;
        private readonly IReadOnlyList<ResourceDefinition> definitions;

        public ResourceCatalog(IEnumerable<ResourceDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            definitionsById = new Dictionary<ResourceId, ResourceDefinition>();
            var orderedDefinitions = new List<ResourceDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Resource definition cannot be null.", nameof(definitions));
                if (!definitionsById.TryAdd(definition.Id, definition))
                {
                    throw new ArgumentException("Duplicate resource id: " + definition.Id.Value, nameof(definitions));
                }

                orderedDefinitions.Add(definition);
            }

            orderedDefinitions.Sort((left, right) => left.Id.CompareTo(right.Id));
            this.definitions = orderedDefinitions.AsReadOnly();
        }

        public IReadOnlyList<ResourceDefinition> Definitions => definitions;

        public bool TryGet(ResourceId id, out ResourceDefinition definition) => definitionsById.TryGetValue(id, out definition!);

        public ResourceDefinition GetRequired(ResourceId id)
        {
            if (TryGet(id, out var definition)) return definition;
            throw new KeyNotFoundException("Unknown resource: " + id.Value);
        }
    }
}
