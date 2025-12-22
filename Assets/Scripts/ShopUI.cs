using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Transform weaponListContainer;
    [SerializeField] private GameObject weaponItemPrefab;
    [SerializeField] private Button closeButton;
    
    [Header("Weapon Detail Panel")]
    [SerializeField] private GameObject weaponDetailPanel;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponStatsText;
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    
    private WeaponData selectedWeapon;
    private List<GameObject> weaponItems = new List<GameObject>();
    
    private void Start()
    {
        shopPanel.SetActive(false);
        weaponDetailPanel.SetActive(false);
        
        buyButton.onClick.AddListener(OnBuyClicked);
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        equipButton.onClick.AddListener(OnEquipClicked);
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
    }
    
    public void OpenShop()
    {
        shopPanel.SetActive(true);
        RefreshShop();
    }
    
    public void CloseShop()
    {
        shopPanel.SetActive(false);
        weaponDetailPanel.SetActive(false);
    }
    
    private void RefreshShop()
    {
        // Update coin display
        coinsText.text = $"Coins: {PlayerProgression.Instance.GetCoins()}";
        
        // Clear existing weapon items
        foreach (var item in weaponItems)
        {
            Destroy(item);
        }
        weaponItems.Clear();
        
        // Create weapon items
        var allWeapons = PlayerProgression.Instance.GetAllWeapons();
        foreach (var weapon in allWeapons)
        {
            GameObject item = Instantiate(weaponItemPrefab, weaponListContainer);
            weaponItems.Add(item);
            
            // Set up weapon item
            var nameText = item.transform.Find("WeaponName").GetComponent<TextMeshProUGUI>();
            var statusText = item.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
            var selectButton = item.GetComponent<Button>();
            
            nameText.text = weapon.weaponName;
            
            bool isOwned = PlayerProgression.Instance.IsWeaponOwned(weapon.weaponId);
            bool isEquipped = PlayerProgression.Instance.GetEquippedWeapon()?.weaponId == weapon.weaponId;
            
            if (isEquipped)
            {
                statusText.text = "EQUIPPED";
                statusText.color = Color.green;
            }
            else if (isOwned)
            {
                statusText.text = "OWNED";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = $"{weapon.baseCost} coins";
                statusText.color = Color.yellow;
            }
            
            // Add click handler
            WeaponData weaponRef = weapon; // Capture for lambda
            selectButton.onClick.AddListener(() => OnWeaponSelected(weaponRef));
        }
    }
    
    private void OnWeaponSelected(WeaponData weapon)
    {
        selectedWeapon = weapon;
        ShowWeaponDetails();
    }
    
    private void ShowWeaponDetails()
    {
        if (selectedWeapon == null) return;
        
        weaponDetailPanel.SetActive(true);
        
        // Get the owned version if player owns it
        WeaponData ownedWeapon = null;
        if (PlayerProgression.Instance.IsWeaponOwned(selectedWeapon.weaponId))
        {
            ownedWeapon = PlayerProgression.Instance.GetOwnedWeapons()
                .Find(w => w.weaponId == selectedWeapon.weaponId);
        }
        
        bool isOwned = ownedWeapon != null;
        bool isEquipped = PlayerProgression.Instance.GetEquippedWeapon()?.weaponId == selectedWeapon.weaponId;
        
        WeaponData displayWeapon = isOwned ? ownedWeapon : selectedWeapon;
        
        // Update display
        weaponNameText.text = displayWeapon.weaponName;
        weaponLevelText.text = isOwned ? $"Level {displayWeapon.currentLevel}" : "Not Owned";
        
        weaponStatsText.text = $"Damage: {displayWeapon.GetCurrentDamage()}\n" +
                               $"Fire Rate: {displayWeapon.GetCurrentFireRate():F2}s\n" +
                               $"DPS: {(displayWeapon.GetCurrentDamage() / displayWeapon.GetCurrentFireRate()):F1}";
        
        // Update buttons
        if (isOwned)
        {
            buyButton.gameObject.SetActive(false);
            upgradeButton.gameObject.SetActive(true);
            equipButton.gameObject.SetActive(true);
            
            int upgradeCost = displayWeapon.GetUpgradeCost();
            upgradeButtonText.text = $"Upgrade ({upgradeCost} coins)";
            upgradeButton.interactable = PlayerProgression.Instance.GetCoins() >= upgradeCost;
            
            equipButton.interactable = !isEquipped;
            equipButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                isEquipped ? "Equipped" : "Equip";
        }
        else
        {
            buyButton.gameObject.SetActive(true);
            upgradeButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(false);
            
            buyButtonText.text = $"Buy ({selectedWeapon.baseCost} coins)";
            buyButton.interactable = PlayerProgression.Instance.GetCoins() >= selectedWeapon.baseCost;
        }
    }
    
    private void OnBuyClicked()
    {
        if (selectedWeapon == null) return;
        
        if (PlayerProgression.Instance.BuyWeapon(selectedWeapon.weaponId))
        {
            RefreshShop();
            ShowWeaponDetails();
        }
    }
    
    private void OnUpgradeClicked()
    {
        if (selectedWeapon == null) return;
        
        if (PlayerProgression.Instance.UpgradeWeapon(selectedWeapon.weaponId))
        {
            RefreshShop();
            ShowWeaponDetails();
        }
    }
    
    private void OnEquipClicked()
    {
        if (selectedWeapon == null) return;
        
        PlayerProgression.Instance.EquipWeapon(selectedWeapon.weaponId);
        RefreshShop();
        ShowWeaponDetails();
    }
}