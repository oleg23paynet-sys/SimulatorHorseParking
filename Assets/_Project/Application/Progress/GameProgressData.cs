#nullable enable

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HorseParking.Application.Progress
{
    [DataContract]
    public sealed class ResourceQuantityProgress
    {
        [DataMember(Order = 0)] public string ResourceId { get; set; } = string.Empty;
        [DataMember(Order = 1)] public int Quantity { get; set; }
    }

    [DataContract]
    public sealed class InventoryProgress
    {
        [DataMember(Order = 0)] public int CapacityUnits { get; set; }
        [DataMember(Order = 1)] public List<ResourceQuantityProgress> Items { get; set; } = new();
    }

    [DataContract]
    public sealed class UpgradeLevelProgress
    {
        [DataMember(Order = 0)] public int UpgradeId { get; set; }
        [DataMember(Order = 1)] public int Level { get; set; }
    }

    /// <summary>Versioned, Unity-independent data written into one save slot.</summary>
    [DataContract]
    public sealed class GameProgressData
    {
        public const int CurrentVersion = 1;

        [DataMember(Order = 0)] public int Version { get; set; } = CurrentVersion;
        [DataMember(Order = 1)] public long SavedAtUtcTicks { get; set; }
        [DataMember(Order = 2)] public int Gold { get; set; }
        [DataMember(Order = 3)] public InventoryProgress Warehouse { get; set; } = new();
        [DataMember(Order = 4)] public InventoryProgress Cart { get; set; } = new();
        [DataMember(Order = 5)] public List<UpgradeLevelProgress> Upgrades { get; set; } = new();
        [DataMember(Order = 6)] public double SecondsUntilExpenses { get; set; }
        [DataMember(Order = 7)] public int ConstructionState { get; set; }
        [DataMember(Order = 8)] public double ConstructionProgress { get; set; }
        [DataMember(Order = 9)] public int CartJourneyState { get; set; }
        [DataMember(Order = 10)] public string? CartDestinationId { get; set; }
        [DataMember(Order = 11)] public int TutorialStep { get; set; }
    }

    public interface IGameProgressRepository
    {
        bool Exists { get; }
        void Save(GameProgressData progress);
        bool TryLoad(out GameProgressData? progress);
    }
}
