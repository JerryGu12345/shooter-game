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
    
    public float GetFireRateCooldown() => 1f / GetFireRateRPS(); // Convert RPS to seconds between shots
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
}

[Serializable]
public class PlayerProgressionData
{
    public long coins;
    public int equippedGunId;
    public List<GunData> ownedGuns;
    public int nextGunId; // For generating unique gun IDs
    
    public PlayerProgressionData()
    {
        coins = 0;
        equippedGunId = 1;
        ownedGuns = new List<GunData>();
        nextGunId = 2;
        
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
    
    // Rewards
    public long coinsPerMatch = 1000;
    public long coinsPerWin = 5000;
    public long coinsPerKill = 500;
    
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
            Debug.Log($"Loaded progress: {progressionData.coins} coins, {progressionData.ownedGuns.Count} guns");
        }
    }
    
    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(progressionData);
        PlayerPrefs.SetString("PlayerProgression", json);
        PlayerPrefs.Save();
        Debug.Log($"Saved progress: {progressionData.coins} coins");
    }
    
    public void AddCoins(long amount)
    {
        progressionData.coins += amount;
        SaveProgress();
        Debug.Log($"Added {amount} coins. Total: {progressionData.coins}");
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