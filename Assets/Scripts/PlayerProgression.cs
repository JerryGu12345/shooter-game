using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponData
{
    public string weaponId;
    public string weaponName;
    public int baseDamage;
    public float baseFireRate;
    public int baseCost;
    public int currentLevel;
    public bool isOwned;
    
    // Upgrade costs increase per level
    public int GetUpgradeCost()
    {
        return baseCost + (currentLevel * 100);
    }
    
    public int GetCurrentDamage()
    {
        return baseDamage + (currentLevel * 5);
    }
    
    public float GetCurrentFireRate()
    {
        return Mathf.Max(0.1f, baseFireRate - (currentLevel * 0.1f));
    }
}

[Serializable]
public class PlayerProgressionData
{
    public int coins;
    public string equippedWeaponId;
    public List<WeaponData> ownedWeapons;
    
    public PlayerProgressionData()
    {
        coins = 0;
        equippedWeaponId = "pistol"; // Default weapon
        ownedWeapons = new List<WeaponData>();
    }
}

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }
    
    private PlayerProgressionData progressionData;
    private List<WeaponData> availableWeapons;
    
    // Rewards
    public int coinsPerMatch = 50;
    public int coinsPerWin = 150;
    public int coinsPerKill = 20;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeWeapons();
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeWeapons()
    {
        availableWeapons = new List<WeaponData>
        {
            new WeaponData
            {
                weaponId = "pistol",
                weaponName = "Pistol",
                baseDamage = 10,
                baseFireRate = 0.5f,
                baseCost = 0, // Starting weapon
                currentLevel = 1,
                isOwned = true
            },
            new WeaponData
            {
                weaponId = "rifle",
                weaponName = "Rifle",
                baseDamage = 15,
                baseFireRate = 0.3f,
                baseCost = 500,
                currentLevel = 1,
                isOwned = false
            },
            new WeaponData
            {
                weaponId = "shotgun",
                weaponName = "Shotgun",
                baseDamage = 25,
                baseFireRate = 1.0f,
                baseCost = 800,
                currentLevel = 1,
                isOwned = false
            },
            new WeaponData
            {
                weaponId = "sniper",
                weaponName = "Sniper",
                baseDamage = 50,
                baseFireRate = 2.0f,
                baseCost = 1500,
                currentLevel = 1,
                isOwned = false
            }
        };
    }
    
    public void LoadProgress()
    {
        string json = PlayerPrefs.GetString("PlayerProgression", "");
        
        if (string.IsNullOrEmpty(json))
        {
            // New player
            progressionData = new PlayerProgressionData();
            progressionData.ownedWeapons.Add(GetWeaponData("pistol"));
            SaveProgress();
        }
        else
        {
            progressionData = JsonUtility.FromJson<PlayerProgressionData>(json);
            Debug.Log($"Loaded progress: {progressionData.coins} coins");
        }
    }
    
    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(progressionData);
        PlayerPrefs.SetString("PlayerProgression", json);
        PlayerPrefs.Save();
        Debug.Log($"Saved progress: {progressionData.coins} coins");
    }
    
    public void AddCoins(int amount)
    {
        progressionData.coins += amount;
        SaveProgress();
        Debug.Log($"Added {amount} coins. Total: {progressionData.coins}");
    }
    
    public bool SpendCoins(int amount)
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
    
    public int GetCoins()
    {
        return progressionData.coins;
    }
    
    public WeaponData GetWeaponData(string weaponId)
    {
        return availableWeapons.Find(w => w.weaponId == weaponId);
    }
    
    public WeaponData GetEquippedWeapon()
    {
        var weapon = progressionData.ownedWeapons.Find(w => w.weaponId == progressionData.equippedWeaponId);
        if (weapon == null)
        {
            // Fallback to pistol
            weapon = progressionData.ownedWeapons.Find(w => w.weaponId == "pistol");
        }
        return weapon;
    }
    
    public List<WeaponData> GetAllWeapons()
    {
        return availableWeapons;
    }
    
    public List<WeaponData> GetOwnedWeapons()
    {
        return progressionData.ownedWeapons;
    }
    
    public bool BuyWeapon(string weaponId)
    {
        var weaponTemplate = GetWeaponData(weaponId);
        if (weaponTemplate == null)
        {
            Debug.LogError($"Weapon {weaponId} not found!");
            return false;
        }
        
        if (IsWeaponOwned(weaponId))
        {
            Debug.Log("Already own this weapon!");
            return false;
        }
        
        if (SpendCoins(weaponTemplate.baseCost))
        {
            var newWeapon = new WeaponData
            {
                weaponId = weaponTemplate.weaponId,
                weaponName = weaponTemplate.weaponName,
                baseDamage = weaponTemplate.baseDamage,
                baseFireRate = weaponTemplate.baseFireRate,
                baseCost = weaponTemplate.baseCost,
                currentLevel = 1,
                isOwned = true
            };
            
            progressionData.ownedWeapons.Add(newWeapon);
            SaveProgress();
            Debug.Log($"Bought {weaponTemplate.weaponName}!");
            return true;
        }
        
        return false;
    }
    
    public bool UpgradeWeapon(string weaponId)
    {
        var weapon = progressionData.ownedWeapons.Find(w => w.weaponId == weaponId);
        if (weapon == null)
        {
            Debug.LogError("Don't own this weapon!");
            return false;
        }
        
        int upgradeCost = weapon.GetUpgradeCost();
        if (SpendCoins(upgradeCost))
        {
            weapon.currentLevel++;
            SaveProgress();
            Debug.Log($"Upgraded {weapon.weaponName} to level {weapon.currentLevel}!");
            return true;
        }
        
        return false;
    }
    
    public void EquipWeapon(string weaponId)
    {
        if (IsWeaponOwned(weaponId))
        {
            progressionData.equippedWeaponId = weaponId;
            SaveProgress();
            Debug.Log($"Equipped {weaponId}");
        }
    }
    
    public bool IsWeaponOwned(string weaponId)
    {
        return progressionData.ownedWeapons.Exists(w => w.weaponId == weaponId);
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
}