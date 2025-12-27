using Unity.Entities;
using Unity.NetCode;

// Player equipment slots
public enum EquipmentSlot
{
    Knife = 0,
    Gun1 = 1,
    Gun2 = 2,
    MedKit = 3
}

// Component to track what's equipped
public struct PlayerEquipment : IComponentData
{
    [GhostField] public EquipmentSlot currentSlot;
    [GhostField] public int gun1Id; // Gun ID from PlayerProgression
    [GhostField] public int gun2Id; // -1 if no second gun
    [GhostField] public float swapTimer; // Time until swap completes
    [GhostField] public bool isSwapping;
    [GhostField] public EquipmentSlot pendingSlot; // What we're swapping to
    [GhostField] public float currentItemSize;
}

// Updated Player component
public struct PlayerStats : IComponentData
{
    [GhostField] public float health;
    [GhostField] public float maxHealth;
    [GhostField] public int currentAmmo;
    [GhostField] public int maxAmmo;
    [GhostField] public float reloadTimer;
    [GhostField] public bool isReloading;
    [GhostField] public float medKitUseTimer;
    [GhostField] public bool isUsingMedKit;
    [GhostField] public float medKitHealPerSecond;
    [GhostField] public float medKitAccumulatedHeal;
}

// Item size component for weapons
public struct ItemSize : IComponentData
{
    [GhostField] public float size;
}