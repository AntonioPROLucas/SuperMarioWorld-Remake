using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    [Header("Status do Jogo")]
    public int totalLives = 5;
    public int totalCoins;
    public int totalScore;

    [Header("Referências de UI")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        coinText = GameObject.FindWithTag("CoinText")?.GetComponent<TextMeshProUGUI>();
        scoreText = GameObject.FindWithTag("ScoreText")?.GetComponent<TextMeshProUGUI>();
        livesText = GameObject.FindWithTag("LivesText")?.GetComponent<TextMeshProUGUI>();

        UpdateAllUI();
    }

    void UpdateAllUI()
    {
        UpdateCoinText();
        UpdateLivesText();
        UpdateScoreText();
    }

    public void UpdateCoinText()
    {
        if (coinText != null)
            coinText.text = totalCoins.ToString();
    }

    public void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = totalScore.ToString();
    }

    public void UpdateLivesText()
    {
        if (livesText != null)
            livesText.text = totalLives.ToString();
    }

    public void PlayerDied()
    {
        totalLives--;
        UpdateLivesText();

        if (totalLives > 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Debug.Log("Game Over!");
            // SceneManager.LoadScene("GameOver");
        }
    }
}
