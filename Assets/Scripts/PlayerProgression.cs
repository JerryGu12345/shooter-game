using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GunStats
{
    public int propulsionLevel = 1;
    public int bulletMassLevel = 1;
    public int magSizeLevel = 1;
    public int fireRateLevel = 1;
    
    // Track upgrade order for cost calculation
    public int propulsionUpgradeOrder = 0;
    public int bulletMassUpgradeOrder = 0;
    public int magSizeUpgradeOrder = 0;
    public int fireRateUpgradeOrder = 0;
    
    // Stat value tables
    private static readonly float[] propulsionValues = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static readonly float[] bulletMassValues = { 1, 1.25f, 1.5f, 1.75f, 2, 2.25f, 2.5f, 2.75f, 3 };
    private static readonly int[] magSizeValues = { 4, 6, 9, 13, 20, 30, 45, 67, 100 };
    private static readonly float[] fireRateValues = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    
    public float GetPropulsion() => propulsionValues[propulsionLevel - 1];
    public float GetBulletMass() => bulletMassValues[bulletMassLevel - 1];
    public int GetMagSize() => magSizeValues[magSizeLevel - 1];
    public float GetFireRateRPS() => fireRateValues[fireRateLevel - 1];
    
    public float GetBulletSpeed() => GetPropulsion() / GetBulletMass();
    
    public float GetFireRateCooldown() => 1f / GetFireRateRPS();
    
    public int GetMaxedStatsCount()
    {
        int count = 0;
        if (propulsionLevel >= 9) count++;
        if (bulletMassLevel >= 9) count++;
        if (magSizeLevel >= 9) count++;
        if (fireRateLevel >= 9) count++;
        return count;
    }
    
    public void ResetAllLevels()
    {
        propulsionLevel = 1;
        bulletMassLevel = 1;
        magSizeLevel = 1;
        fireRateLevel = 1;
        propulsionUpgradeOrder = 0;
        bulletMassUpgradeOrder = 0;
        magSizeUpgradeOrder = 0;
        fireRateUpgradeOrder = 0;
    }
}

[Serializable]
public class GunData
{
    public int gunId;
    public int tier;
    public GunStats stats;
    public bool isEquipped;
    
    public GunData(int id, int gunTier)
    {
        gunId = id;
        tier = gunTier;
        stats = new GunStats();
        isEquipped = false;
    }
    
    public long GetBaseCost()
    {
        return (long)Mathf.Pow(100000, tier - 1);
    }
    
    public long GetStatUpgradeCost(string statName)
    {
        int currentLevel = 0;
        int upgradeOrder = 0;
        
        switch (statName)
        {
            case "propulsion":
                currentLevel = stats.propulsionLevel;
                upgradeOrder = stats.propulsionUpgradeOrder;
                break;
            case "bulletMass":
                currentLevel = stats.bulletMassLevel;
                upgradeOrder = stats.bulletMassUpgradeOrder;
                break;
            case "magSize":
                currentLevel = stats.magSizeLevel;
                upgradeOrder = stats.magSizeUpgradeOrder;
                break;
            case "fireRate":
                currentLevel = stats.fireRateLevel;
                upgradeOrder = stats.fireRateUpgradeOrder;
                break;
        }
        
        if (currentLevel >= 9) return -1; // Max level
        
        long baseCost = GetBaseCost();
        long cost = (long)(baseCost * Mathf.Pow(2, currentLevel * upgradeOrder));
        return cost;
    }
    
    public float GetDamage()
    {
        float bulletSpeed = stats.GetBulletSpeed();
        float speedMultiplier = Mathf.Min(bulletSpeed, 1f);
        float tierMultiplier = Mathf.Pow(4, tier - 1);
        return tierMultiplier * speedMultiplier * stats.GetBulletMass();
    }
    
    public bool CanEvolve()
    {
        return stats.GetMaxedStatsCount() >= 2;
    }
    
    public bool CanSuperEvolve()
    {
        return stats.GetMaxedStatsCount() >= 4;
    }
    
    public long GetGunValue()
    {
        long value = GetBaseCost();
        
        // Add value of all upgrades
        for (int i = 1; i < stats.propulsionLevel; i++)
        {
            value += (long)(GetBaseCost() * Mathf.Pow(2, i * stats.propulsionUpgradeOrder));
        }
        for (int i = 1; i < stats.bulletMassLevel; i++)
        {
            value += (long)(GetBaseCost() * Mathf.Pow(2, i * stats.bulletMassUpgradeOrder));
        }
        for (int i = 1; i < stats.magSizeLevel; i++)
        {
            value += (long)(GetBaseCost() * Mathf.Pow(2, i * stats.magSizeUpgradeOrder));
        }
        for (int i = 1; i < stats.fireRateLevel; i++)
        {
            value += (long)(GetBaseCost() * Mathf.Pow(2, i * stats.fireRateUpgradeOrder));
        }
        
        return value;
    }
}

[Serializable]
public class PlayerProgressionData
{
    public long coins;
    public int equippedGunId;
    public List<GunData> ownedGuns;
    public int nextGunId;
    public int prestige;
    public int maxTierEverAfforded;
    
    public PlayerProgressionData()
    {
        coins = 0;
        equippedGunId = 1;
        ownedGuns = new List<GunData>();
        nextGunId = 2;
        prestige = 0;
        maxTierEverAfforded = 1;
        
        // Start with one tier 1 gun
        GunData starterGun = new GunData(1, 1);
        starterGun.isEquipped = true;
        ownedGuns.Add(starterGun);
    }
}

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }
    
    private PlayerProgressionData progressionData;
    
    public const int MAX_GUNS = 12;
    public const int MAX_STAT_LEVEL = 9;
    
    // Base rewards (multiplied by prestige multiplier)
    public long baseCoinsPerMatch = 1000;
    public long baseCoinsPerWin = 5000;
    public long baseCoinsPerKill = 500;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void LoadProgress()
    {
        string json = PlayerPrefs.GetString("PlayerProgression", "");
        
        if (string.IsNullOrEmpty(json))
        {
            progressionData = new PlayerProgressionData();
            SaveProgress();
            Debug.Log("New player created with tier 1 gun");
        }
        else
        {
            progressionData = JsonUtility.FromJson<PlayerProgressionData>(json);
            Debug.Log($"Loaded progress: {progressionData.coins} coins, {progressionData.ownedGuns.Count} guns, Prestige {progressionData.prestige}");
        }
    }
    
    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(progressionData);
        PlayerPrefs.SetString("PlayerProgression", json);
        PlayerPrefs.Save();
        Debug.Log($"Saved progress: {progressionData.coins} coins");
    }
    
    public float GetPrestigeMultiplier()
    {
        return Mathf.Pow(100000, progressionData.prestige);
    }
    
    public long coinsPerMatch => (long)(baseCoinsPerMatch * GetPrestigeMultiplier());
    public long coinsPerWin => (long)(baseCoinsPerWin * GetPrestigeMultiplier());
    public long coinsPerKill => (long)(baseCoinsPerKill * GetPrestigeMultiplier());
    
    public void AddCoins(long amount)
    {
        progressionData.coins += amount;
        
        // Update max tier ever afforded
        UpdateMaxTierAfforded();
        
        SaveProgress();
        Debug.Log($"Added {amount} coins. Total: {progressionData.coins}");
    }
    
    private void UpdateMaxTierAfforded()
    {
        // Calculate the highest tier the player can afford
        int tier = 1;
        while (true)
        {
            long cost = (long)Mathf.Pow(100000, tier - 1);
            if (progressionData.coins >= cost)
            {
                tier++;
            }
            else
            {
                break;
            }
        }
        tier--; // Go back to last affordable tier
        
        if (tier > progressionData.maxTierEverAfforded)
        {
            progressionData.maxTierEverAfforded = tier;
            Debug.Log($"Max tier ever afforded increased to: {tier}");
        }
    }
    
    public bool SpendCoins(long amount)
    {
        if (progressionData.coins >= amount)
        {
            progressionData.coins -= amount;
            SaveProgress();
            Debug.Log($"Spent {amount} coins. Remaining: {progressionData.coins}");
            return true;
        }
        Debug.Log($"Not enough coins! Need {amount}, have {progressionData.coins}");
        return false;
    }
    
    public long GetCoins()
    {
        return progressionData.coins;
    }
    
    public int GetPrestige()
    {
        return progressionData.prestige;
    }
    
    public int GetMaxTierAfforded()
    {
        return progressionData.maxTierEverAfforded;
    }
    
    public int GetAvailableTier1()
    {
        return Mathf.Max(2, progressionData.maxTierEverAfforded);
    }
    
    public int GetAvailableTier2()
    {
        return Mathf.Max(2, progressionData.maxTierEverAfforded) - 1;
    }
    
    public List<GunData> GetOwnedGuns()
    {
        return progressionData.ownedGuns;
    }
    
    public GunData GetEquippedGun()
    {
        return progressionData.ownedGuns.Find(g => g.gunId == progressionData.equippedGunId);
    }
    
    public GunData GetGunById(int gunId)
    {
        return progressionData.ownedGuns.Find(g => g.gunId == gunId);
    }
    
    public bool CanBuyNewGun()
    {
        return progressionData.ownedGuns.Count < MAX_GUNS;
    }
    
    public bool BuyGun(int tier)
    {
        if (!CanBuyNewGun())
        {
            Debug.Log("Gun inventory full! Maximum 12 guns.");
            return false;
        }
        
        long cost = (long)Mathf.Pow(100000, tier - 1);
        
        if (SpendCoins(cost))
        {
            GunData newGun = new GunData(progressionData.nextGunId, tier);
            progressionData.nextGunId++;
            progressionData.ownedGuns.Add(newGun);
            SaveProgress();
            Debug.Log($"Bought tier {tier} gun for {cost} coins!");
            return true;
        }
        
        return false;
    }
    
    public bool UpgradeStat(int gunId, string statName)
    {
        GunData gun = GetGunById(gunId);
        if (gun == null)
        {
            Debug.LogError("Gun not found!");
            return false;
        }
        
        long cost = gun.GetStatUpgradeCost(statName);
        if (cost == -1)
        {
            Debug.Log("Stat already at max level!");
            return false;
        }
        
        if (SpendCoins(cost))
        {
            // Get the current total upgrade count to determine next upgrade order
            int totalUpgrades = gun.stats.propulsionUpgradeOrder + 
                               gun.stats.bulletMassUpgradeOrder + 
                               gun.stats.magSizeUpgradeOrder + 
                               gun.stats.fireRateUpgradeOrder;
            
            switch (statName)
            {
                case "propulsion":
                    gun.stats.propulsionLevel++;
                    if (gun.stats.propulsionUpgradeOrder == 0)
                        gun.stats.propulsionUpgradeOrder = totalUpgrades + 1;
                    break;
                case "bulletMass":
                    gun.stats.bulletMassLevel++;
                    if (gun.stats.bulletMassUpgradeOrder == 0)
                        gun.stats.bulletMassUpgradeOrder = totalUpgrades + 1;
                    break;
                case "magSize":
                    gun.stats.magSizeLevel++;
                    if (gun.stats.magSizeUpgradeOrder == 0)
                        gun.stats.magSizeUpgradeOrder = totalUpgrades + 1;
                    break;
                case "fireRate":
                    gun.stats.fireRateLevel++;
                    if (gun.stats.fireRateUpgradeOrder == 0)
                        gun.stats.fireRateUpgradeOrder = totalUpgrades + 1;
                    break;
            }
            
            SaveProgress();
            Debug.Log($"Upgraded {statName} to level {gun.stats.propulsionLevel}!");
            return true;
        }
        
        return false;
    }
    
    public bool EvolveGun(int gunId)
    {
        GunData gun = GetGunById(gunId);
        if (gun == null) return false;
        
        if (!gun.CanEvolve())
        {
            Debug.Log("Gun needs at least 2 maxed stats to evolve!");
            return false;
        }
        
        int tierIncrease = gun.CanSuperEvolve() ? 2 : 1;
        gun.tier += tierIncrease;
        gun.stats.ResetAllLevels();
        
        SaveProgress();
        Debug.Log($"Gun evolved! New tier: {gun.tier} (Super Evolve: {gun.CanSuperEvolve()})");
        return true;
    }
    
    public long CalculateTotalValue()
    {
        long total = progressionData.coins;
        
        foreach (var gun in progressionData.ownedGuns)
        {
            total += gun.GetGunValue();
        }
        
        return total;
    }
    
    public int CalculateNewPrestige(long sacrificeValue)
    {
        // New prestige = log(sacrifice value) / (10 * log(10))
        if (sacrificeValue <= 0) return 0;
        
        double logValue = Math.Log10(sacrificeValue);
        double newPrestige = logValue / (10.0 * Math.Log10(10));
        return (int)Math.Floor(newPrestige);
    }
    
    public bool CanIsekai()
    {
        long totalValue = CalculateTotalValue();
        int newPrestige = CalculateNewPrestige(totalValue);
        return newPrestige > progressionData.prestige;
    }
    
    public bool Isekai()
    {
        long totalValue = CalculateTotalValue();
        int newPrestige = CalculateNewPrestige(totalValue);
        
        if (newPrestige <= progressionData.prestige)
        {
            Debug.Log($"Cannot isekai! New prestige ({newPrestige}) must be higher than current ({progressionData.prestige})");
            return false;
        }
        
        // Reset everything except prestige
        progressionData.coins = 0;
        progressionData.ownedGuns.Clear();
        progressionData.nextGunId = 2;
        progressionData.maxTierEverAfforded = 1;
        progressionData.prestige = newPrestige;
        
        // Give starter gun
        GunData starterGun = new GunData(1, 1);
        starterGun.isEquipped = true;
        progressionData.ownedGuns.Add(starterGun);
        progressionData.equippedGunId = 1;
        
        SaveProgress();
        Debug.Log($"Isekai successful! New prestige: {newPrestige}. Sacrifice value was: {totalValue}");
        return true;
    }
    
    public void EquipGun(int gunId)
    {
        GunData gun = GetGunById(gunId);
        if (gun != null)
        {
            // Unequip all guns
            foreach (var g in progressionData.ownedGuns)
            {
                g.isEquipped = false;
            }
            
            // Equip selected gun
            gun.isEquipped = true;
            progressionData.equippedGunId = gunId;
            SaveProgress();
            Debug.Log($"Equipped gun ID {gunId}");
        }
    }
    
    public bool DeleteGun(int gunId)
    {
        GunData gun = GetGunById(gunId);
        if (gun == null) return false;
        
        if (gun.isEquipped)
        {
            Debug.Log("Cannot delete equipped gun!");
            return false;
        }
        
        progressionData.ownedGuns.Remove(gun);
        SaveProgress();
        Debug.Log($"Deleted gun ID {gunId}");
        return true;
    }
    
    public void RewardMatchParticipation()
    {
        AddCoins(coinsPerMatch);
    }
    
    public void RewardWin()
    {
        AddCoins(coinsPerWin);
    }
    
    public void RewardKill()
    {
        AddCoins(coinsPerKill);
    }
    
    // Debug: Reset progress
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("PlayerProgression");
        LoadProgress();
        Debug.Log("Progress reset!");
    }
    
    // Debug: Add coins for testing
    public void AddTestCoins(long amount)
    {
        AddCoins(amount);
    }
}