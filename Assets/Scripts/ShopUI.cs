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
    [SerializeField] private TMP_InputField tierInputField;
    [SerializeField] private TextMeshProUGUI buyCostText;
    [SerializeField] private Button buyNewGunButton;
    
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
    
    private GunData selectedGun;
    private List<GameObject> gunItems = new List<GameObject>();
    
    private void Start()
    {
        shopPanel.SetActive(false);
        gunDetailPanel.SetActive(false);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
        
        buyNewGunButton.onClick.AddListener(OnBuyNewGunClicked);
        tierInputField.onValueChanged.AddListener(OnTierInputChanged);
        
        propulsionUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("propulsion"));
        bulletMassUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("bulletMass"));
        magSizeUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("magSize"));
        fireRateUpgradeButton.onClick.AddListener(() => OnUpgradeStatClicked("fireRate"));
        
        equipButton.onClick.AddListener(OnEquipClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
    }
    
    public void OpenShop()
    {
        shopPanel.SetActive(true);
        RefreshShop();
    }
    
    public void CloseShop()
    {
        shopPanel.SetActive(false);
        gunDetailPanel.SetActive(false);
    }
    
    private void RefreshShop()
    {
        if (PlayerProgression.Instance == null)
        {
            Debug.LogError("PlayerProgression.Instance is null!");
            return;
        }
        
        // Update coin display with scientific notation for large numbers
        long coins = PlayerProgression.Instance.GetCoins();
        coinsText.text = $"Coins: {FormatLargeNumber(coins)}";
        
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
        Debug.Log($"Found {ownedGuns.Count} guns");
        
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
            OnTierInputChanged(tierInputField.text);
        }
    }
    
    private void OnTierInputChanged(string tierStr)
    {
        if (int.TryParse(tierStr, out int tier) && tier >= 1)
        {
            long cost = (long)Mathf.Pow(100000, tier - 1);
            buyCostText.text = $"Cost: {FormatLargeNumber(cost)}";
            buyNewGunButton.interactable = PlayerProgression.Instance.GetCoins() >= cost;
        }
        else
        {
            buyCostText.text = "Invalid tier";
            buyNewGunButton.interactable = false;
        }
    }
    
    private void OnBuyNewGunClicked()
    {
        if (int.TryParse(tierInputField.text, out int tier) && tier >= 1)
        {
            if (PlayerProgression.Instance.BuyGun(tier))
            {
                RefreshShop();
            }
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
    
    private string FormatLargeNumber(long number)
    {
        if (number < 1000) return number.ToString();
        if (number < 1000000) return $"{number / 1000f:F1}K";
        if (number < 1000000000) return $"{number / 1000000f:F1}M";
        if (number < 1000000000000) return $"{number / 1000000000f:F1}B";
        return $"{number / 1000000000000f:F1}T";
    }
}