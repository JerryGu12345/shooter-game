using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Transform gunListContainer;
    [SerializeField] private GameObject gunItemPrefab;
    [SerializeField] private Button closeButton;
    
    [Header("Buy New Gun Panel")]
    [SerializeField] private GameObject buyGunPanel;
    [SerializeField] private Button buyTier1Button;
    [SerializeField] private Button buyTier2Button;
    [SerializeField] private TextMeshProUGUI buyTier1Text;
    [SerializeField] private TextMeshProUGUI buyTier2Text;
    
    [Header("Gun Detail Panel")]
    [SerializeField] private GameObject gunDetailPanel;
    [SerializeField] private TextMeshProUGUI gunTitleText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI bulletSpeedText;
    
    [Header("Stat Upgrade Buttons")]
    [SerializeField] private Button propulsionUpgradeButton;
    [SerializeField] private Button bulletMassUpgradeButton;
    [SerializeField] private Button magSizeUpgradeButton;
    [SerializeField] private Button fireRateUpgradeButton;
    [SerializeField] private TextMeshProUGUI propulsionText;
    [SerializeField] private TextMeshProUGUI bulletMassText;
    [SerializeField] private TextMeshProUGUI magSizeText;
    [SerializeField] private TextMeshProUGUI fireRateText;
    
    [Header("Action Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button evolveButton;
    [SerializeField] private TextMeshProUGUI evolveButtonText;
    
    [Header("Prestige Panel")]
    [SerializeField] private GameObject prestigePanel;
    [SerializeField] private TextMeshProUGUI prestigeText;
    [SerializeField] private TextMeshProUGUI sacrificeValueText;
    [SerializeField] private TextMeshProUGUI newPrestigeText;
    [SerializeField] private Button isekaiButton;
    
    private GunData selectedGun;
    private List<GameObject> gunItems = new List<GameObject>();
    
    private void Start()
    {
        gunDetailPanel.SetActive(false);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
        
        buyTier1Button.onClick.AddListener(() => OnBuyGunClicked(PlayerProgression.Instance.GetAvailableTier1()));
        buyTier2Button.onClick.AddListener(() => OnBuyGunClicked(PlayerProgression.Instance.GetAvailableTier2()));
        
        propulsionUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("propulsion"));
        bulletMassUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("bulletMass"));
        magSizeUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("magSize"));
        fireRateUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("fireRate"));
        
        equipButton.onClick.AddListener(OnEquipClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
        evolveButton.onClick.AddListener(OnEvolveClicked);
        isekaiButton.onClick.AddListener(OnIsekaiClicked);
    }
    
    public void OpenShop()
    {
        // Called by UIManager when shop panel is activated
        RefreshShop();
    }
    
    public void CloseShop()
    {
        // Hide detail panel and go back
        gunDetailPanel.SetActive(false);
        UIManager.Instance.GoBack();
    }
    
    private void OnEnable()
    {
        // Refresh shop every time the panel is shown
        if (PlayerProgression.Instance != null)
        {
            Debug.Log("ShopPanel enabled, refreshing shop...");
            RefreshShop();
        }
        else
        {
            Debug.LogError("PlayerProgression.Instance is null when ShopPanel was enabled!");
        }
    }
    
    private void RefreshShop()
    {
        if (PlayerProgression.Instance == null)
        {
            Debug.LogError("PlayerProgression.Instance is null!");
            return;
        }
        
        if (gunListContainer == null)
        {
            Debug.LogError("GunListContainer is not assigned in Inspector!");
            return;
        }
        
        if (gunItemPrefab == null)
        {
            Debug.LogError("GunItemPrefab is not assigned in Inspector!");
            return;
        }
        
        // Update coin display with scientific notation for large numbers
        long coins = PlayerProgression.Instance.GetCoins();
        coinsText.text = $"Coins: {FormatLargeNumber(coins)}";
        
        // Update prestige panel
        UpdatePrestigePanel();
        
        // Update buy gun panel
        UpdateBuyGunPanel();
        
        // Clear existing gun items
        foreach (var item in gunItems)
        {
            Destroy(item);
        }
        gunItems.Clear();
        
        // Create gun items
        var ownedGuns = PlayerProgression.Instance.GetOwnedGuns();
        Debug.Log($"Found {ownedGuns.Count} guns in PlayerProgression");
        
        foreach (var gun in ownedGuns)
        {
            GameObject item = Instantiate(gunItemPrefab, gunListContainer);
            gunItems.Add(item);
            
            // Set up gun item
            var nameText = item.transform.Find("GunName").GetComponent<TextMeshProUGUI>();
            var statusText = item.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
            var selectButton = item.GetComponent<Button>();
            
            nameText.text = $"Tier {gun.tier} Gun #{gun.gunId}";
            
            if (gun.isEquipped)
            {
                statusText.text = "EQUIPPED";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = $"DMG: {gun.GetDamage():F1}";
                statusText.color = Color.white;
            }
            
            // Add click handler
            GunData gunRef = gun;
            selectButton.onClick.AddListener(() => {
                Debug.Log($"Gun clicked: Tier {gunRef.tier} #{gunRef.gunId}");
                OnGunSelected(gunRef);
            });
        }
        
        Debug.Log($"Created {gunItems.Count} gun items");
    }
    
    private void UpdateBuyGunPanel()
    {
        bool canBuyMore = PlayerProgression.Instance.CanBuyNewGun();
        buyGunPanel.SetActive(canBuyMore);
        
        if (canBuyMore)
        {
            int tier1 = PlayerProgression.Instance.GetAvailableTier1();
            int tier2 = PlayerProgression.Instance.GetAvailableTier2();
            
            long cost1 = (long)Mathf.Pow(100000, tier1 - 1);
            long cost2 = (long)Mathf.Pow(100000, tier2 - 1);
            
            buyTier1Text.text = $"Buy Tier {tier1}\n{FormatLargeNumber(cost1)}";
            buyTier2Text.text = $"Buy Tier {tier2}\n{FormatLargeNumber(cost2)}";
            
            buyTier1Button.interactable = PlayerProgression.Instance.GetCoins() >= cost1;
            buyTier2Button.interactable = PlayerProgression.Instance.GetCoins() >= cost2;
        }
    }
    
    private void UpdatePrestigePanel()
    {
        if (prestigePanel == null) return;
        
        int prestige = PlayerProgression.Instance.GetPrestige();
        prestigeText.text = $"Prestige: {prestige}";
        
        long totalValue = PlayerProgression.Instance.CalculateTotalValue();
        sacrificeValueText.text = $"Total Value: {FormatLargeNumber(totalValue)}";
        
        int newPrestige = PlayerProgression.Instance.CalculateNewPrestige(totalValue);
        newPrestigeText.text = $"New Prestige: {newPrestige}";
        
        bool canIsekai = PlayerProgression.Instance.CanIsekai();
        isekaiButton.interactable = canIsekai;
        
        if (!canIsekai)
        {
            isekaiButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"Isekai (Need Prestige {prestige + 1}+)";
        }
        else
        {
            isekaiButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"Isekai → Prestige {newPrestige}";
        }
    }
    
    private void OnBuyGunClicked(int tier)
    {
        if (PlayerProgression.Instance.BuyGun(tier))
        {
            RefreshShop();
        }
    }
    
    private void OnGunSelected(GunData gun)
    {
        Debug.Log($"OnGunSelected called for: Tier {gun.tier} #{gun.gunId}");
        selectedGun = gun;
        ShowGunDetails();
    }
    
    private void ShowGunDetails()
    {
        if (selectedGun == null)
        {
            Debug.LogError("selectedGun is null!");
            return;
        }
        
        Debug.Log($"Showing details for: Tier {selectedGun.tier} #{selectedGun.gunId}");
        gunDetailPanel.SetActive(true);
        
        gunTitleText.text = $"Tier {selectedGun.tier} Gun #{selectedGun.gunId}";
        damageText.text = $"Damage: {selectedGun.GetDamage():F2}";
        bulletSpeedText.text = $"Bullet Speed: {selectedGun.stats.GetBulletSpeed():F2}";
        
        // Update stat displays and buttons
        UpdateStatButton("propulsion", selectedGun.stats.propulsionLevel, 
            selectedGun.stats.GetPropulsion(), propulsionUpgradeButton, propulsionText);
        UpdateStatButton("bulletMass", selectedGun.stats.bulletMassLevel, 
            selectedGun.stats.GetBulletMass(), bulletMassUpgradeButton, bulletMassText);
        UpdateStatButton("magSize", selectedGun.stats.magSizeLevel, 
            selectedGun.stats.GetMagSize(), magSizeUpgradeButton, magSizeText);
        UpdateStatButton("fireRate", selectedGun.stats.fireRateLevel, 
            selectedGun.stats.GetFireRateRPS(), fireRateUpgradeButton, fireRateText);
        
        // Update action buttons
        equipButton.interactable = !selectedGun.isEquipped;
        equipButton.GetComponentInChildren<TextMeshProUGUI>().text = 
            selectedGun.isEquipped ? "Equipped" : "Equip";
        
        deleteButton.interactable = !selectedGun.isEquipped;
        
        // Update evolve button
        bool canEvolve = selectedGun.CanEvolve();
        bool canSuperEvolve = selectedGun.CanSuperEvolve();
        evolveButton.interactable = canEvolve;
        
        if (canSuperEvolve)
        {
            evolveButtonText.text = $"Super Evolve (+2 Tiers)\n{selectedGun.stats.GetMaxedStatsCount()}/4 Stats Maxed";
        }
        else if (canEvolve)
        {
            evolveButtonText.text = $"Evolve (+1 Tier)\n{selectedGun.stats.GetMaxedStatsCount()}/4 Stats Maxed";
        }
        else
        {
            evolveButtonText.text = $"Need 2+ Maxed Stats\n{selectedGun.stats.GetMaxedStatsCount()}/4 Stats Maxed";
        }
    }
    
    private void UpdateStatButton(string statName, int level, float value, Button button, TextMeshProUGUI text)
    {
        text.text = $"{statName}: Lv.{level} ({value:F2})";
        
        long upgradeCost = selectedGun.GetStatUpgradeCost(statName);
        
        if (upgradeCost == -1)
        {
            button.interactable = false;
            button.GetComponentInChildren<TextMeshProUGUI>().text = "MAX";
        }
        else
        {
            button.interactable = PlayerProgression.Instance.GetCoins() >= upgradeCost;
            button.GetComponentInChildren<TextMeshProUGUI>().text = FormatLargeNumber(upgradeCost);
        }
    }
    
    private void OnUpgradeStatClicked(string statName)
    {
        if (selectedGun == null) return;
        
        if (PlayerProgression.Instance.UpgradeStat(selectedGun.gunId, statName))
        {
            RefreshShop();
            ShowGunDetails();
        }
    }
    
    private void OnEquipClicked()
    {
        if (selectedGun == null) return;
        
        PlayerProgression.Instance.EquipGun(selectedGun.gunId);
        RefreshShop();
        ShowGunDetails();
    }
    
    private void OnDeleteClicked()
    {
        if (selectedGun == null) return;
        
        if (PlayerProgression.Instance.DeleteGun(selectedGun.gunId))
        {
            selectedGun = null;
            gunDetailPanel.SetActive(false);
            RefreshShop();
        }
    }
    
    private void OnEvolveClicked()
    {
        if (selectedGun == null) return;
        
        if (PlayerProgression.Instance.EvolveGun(selectedGun.gunId))
        {
            RefreshShop();
            ShowGunDetails();
        }
    }
    
    private void OnIsekaiClicked()
    {
        // Add confirmation dialog here if you want
        if (PlayerProgression.Instance.Isekai())
        {
            selectedGun = null;
            gunDetailPanel.SetActive(false);
            RefreshShop();
        }
    }
    
    private string FormatLargeNumber(long number)
    {
        if (number < 1000) return number.ToString();
        if (number < 1000000) return $"{number / 1000f:F1}K";
        if (number < 1000000000) return $"{number / 1000000f:F1}M";
        if (number < 1000000000000) return $"{number / 1000000000f:F1}B";
        return $"{number / 1000000000000f:F1}T";
    }
}