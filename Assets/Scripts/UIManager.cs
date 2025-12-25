using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject matchmakingPanel;
    
    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private GameObject currentPanel;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize - show main menu, hide everything else
        ShowPanel(mainMenuPanel, false); // false = don't add to history
    }
    
    private void Update()
    {
        // Handle back button (Escape key or Android back button)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }
    
    public void ShowMainMenu()
    {
        ShowPanel(mainMenuPanel);
    }
    
    public void ShowShop()
    {
        ShowPanel(shopPanel);
    }
    
    public void ShowProfile()
    {
        ShowPanel(profilePanel);
    }
    
    public void ShowSettings()
    {
        ShowPanel(settingsPanel);
    }
    
    public void ShowMatchmaking()
    {
        ShowPanel(matchmakingPanel);
    }
    
    private void ShowPanel(GameObject panel, bool addToHistory = true)
    {
        if (panel == null)
        {
            Debug.LogError("Trying to show null panel!");
            return;
        }
        
        // Add current panel to history before switching
        if (addToHistory && currentPanel != null && currentPanel != panel)
        {
            panelHistory.Push(currentPanel);
        }
        
        // Hide all panels
        HideAllPanels();
        
        // Show requested panel
        panel.SetActive(true);
        currentPanel = panel;
        
        Debug.Log($"Showing panel: {panel.name}");
    }
    
    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
    }
    
    public void GoBack()
    {
        if (panelHistory.Count > 0)
        {
            GameObject previousPanel = panelHistory.Pop();
            ShowPanel(previousPanel, false); // Don't add to history when going back
            Debug.Log($"Going back to: {previousPanel.name}");
        }
        else
        {
            // No history - go to main menu
            ShowPanel(mainMenuPanel, false);
            Debug.Log("No history - returning to main menu");
        }
    }
    
    public void ClearHistory()
    {
        panelHistory.Clear();
    }
}