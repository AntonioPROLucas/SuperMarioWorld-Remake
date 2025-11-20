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
    // O índice do combo de pulos
    private int stompComboIndex = 0; 
    
    // O vetor com a sequência de pontos
    private int[] comboPointSequence = { 200, 400, 800, 1000, 2000, 4000, 8000 };

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

    public void AddLife()
    {
        totalLives++;
        UpdateLivesText();
    }

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreText();
    }

    public void IncrementStompCombo()
    {
        if (stompComboIndex < comboPointSequence.Length)
        {
            AddScore(comboPointSequence[stompComboIndex]);
        }
        else
        {
            AddLife();
        }
        stompComboIndex++;
    }

    public void ResetStompCombo()
    {
        stompComboIndex = 0;
    }

    /// Retorna o índice atual do combo de pulos (Stomp Combo).
    /// </summary>
    public int GetStompComboIndex()
    {
        return stompComboIndex;
    }

    /// <summary>
    /// Define manualmente o índice do combo de pulos.
    /// </summary>
    public void SetStompComboIndex(int newIndex)
    {
        stompComboIndex = newIndex;
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
