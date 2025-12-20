using System.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;

    private bool isReturningToMenu = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowGameOver(bool isWinner)
    {
        if (isReturningToMenu) return;
        
        Debug.Log($"ShowGameOver called: isWinner = {isWinner}");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("Game over panel activated");
        }
        else
        {
            Debug.LogError("Game Over Panel is not assigned!");
        }

        if (gameOverText != null)
        {
            gameOverText.text = isWinner ? "YOU WIN!" : "YOU LOSE!";
            gameOverText.color = isWinner ? Color.green : Color.red;
            Debug.Log($"Text set to: {gameOverText.text}");
        }
        else
        {
            Debug.LogError("Game Over Text is not assigned!");
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ReturnToMenuAfterDelay(3f));
    }

    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        isReturningToMenu = true;
        Debug.Log($"Waiting {delay} seconds before returning to menu...");
        yield return new WaitForSeconds(delay);

        Debug.Log("Time's up! Starting cleanup...");
        
        // Collect all worlds that exist right now
        var worldsToDispose = new System.Collections.Generic.List<World>();
        Debug.Log($"Found {World.All.Count} worlds:");
        foreach (var world in World.All)
        {
            Debug.Log($"  - {world.Name} (Flags: {world.Flags})");
            worldsToDispose.Add(world);
        }
        
        // Dispose ALL worlds immediately
        foreach (var world in worldsToDispose)
        {
            if (world != null && world.IsCreated)
            {
                Debug.Log($"Disposing world: {world.Name}");
                world.Dispose();
            }
        }

        // Reset DefaultGameObjectInjectionWorld
        World.DefaultGameObjectInjectionWorld = null;
        
        Debug.Log($"All worlds disposed. Remaining worlds: {World.All.Count}");

        // Now load the menu scene (Single mode to clean everything)
        Debug.Log("Loading MainMenu scene...");
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        
        // Note: This GameObject will be destroyed automatically by LoadScene(Single)
    }
}