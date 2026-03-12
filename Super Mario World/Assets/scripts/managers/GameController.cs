using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 
using System.Collections; // Necessário para a Coroutine das Estrelas!

public class GameController : MonoBehaviour
{
    public static GameController instance;

    [Header("Status do Jogo")]
    public int totalLives = 5;
    public int totalCoins;
    public int totalScore;

    [Header("Bonus Stars (Fim de Fase)")]
    public int bonusStars = 0;

    [Header("Referências de UI")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI starText; 
    
    [Header("Efeitos Visuais e Fim de Fase")]
    public GameObject floatingTextPrefab; // ✨ O Prefab do texto flutuante
    public TextMeshProUGUI centerStarText; // ✨ O Texto gigante no meio da tela

    // --- CONFIGURAÇÃO DE COMBO ---
    private int[] comboPoints = { 200, 400, 800, 1000, 2000, 4000, 8000 };
    private int currentComboIndex = 0;

    // ==========================================
    // 🚩 SISTEMA DE CHECKPOINT
    // ==========================================
    public static bool hasCheckpoint = false;
    public static Vector3 checkpointPosition;

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
        starText = GameObject.FindWithTag("StarText")?.GetComponent<TextMeshProUGUI>(); 
        
        // ✨ Tenta achar o texto central caso tenha a tag
        centerStarText = GameObject.FindWithTag("CenterStarText")?.GetComponent<TextMeshProUGUI>();
        if (centerStarText != null) centerStarText.gameObject.SetActive(false); // Esconde no início da fase

        UpdateAllUI();
    }

    void UpdateAllUI()
    {
        UpdateCoinText();
        UpdateLivesText();
        UpdateScoreText();
        UpdateStarText(); 
    }

    // ==========================================
    // 🧠 LÓGICA DE COMBO E PONTUAÇÃO FLUTUANTE
    // ==========================================
    
    // Agora aceita a posição do inimigo!
    public void RegisterComboHit(Vector3? spawnPosition = null)
    {
        int scoreToAdd = 0;

        if (currentComboIndex < comboPoints.Length)
        {
            scoreToAdd = comboPoints[currentComboIndex];
            AddScore(scoreToAdd, spawnPosition);
        }
        else
        {
            AddLife(spawnPosition);
        }

        currentComboIndex++;
    }

    public void ResetCombo()
    {
        currentComboIndex = 0;
    }

    // ==========================================
    // 💾 GERENCIAMENTO DE DADOS
    // ==========================================

    public void AddCoin(int amount = 1, Vector3? spawnPosition = null)
    {
        totalCoins += amount;
        if (totalCoins >= 100)
        {
            totalCoins -= 100;
            AddLife(spawnPosition);
        }
        UpdateCoinText();
    }

    public void AddLife(Vector3? spawnPosition = null)
    {
        totalLives++;
        UpdateLivesText();
        
        // ✨ Cria o texto vermelho "1-UP" no local
        if (spawnPosition.HasValue)
        {
            SpawnFloatingText("1-UP", spawnPosition.Value, Color.red);
        }
    }

    public void AddScore(int points, Vector3? spawnPosition = null)
    {
        totalScore += points;
        UpdateScoreText();
        
        // ✨ Cria o texto branco com os pontos no local
        if (spawnPosition.HasValue)
        {
            SpawnFloatingText(points.ToString(), spawnPosition.Value, Color.white);
        }
    }

    private void SpawnFloatingText(string text, Vector3 position, Color color)
    {
        if (floatingTextPrefab != null)
        {
            // Instancia levemente acima do inimigo/bloco
            GameObject floater = Instantiate(floatingTextPrefab, position + (Vector3.up * 0.8f), Quaternion.identity);
            if (floater.TryGetComponent<FloatingText>(out FloatingText ft))
            {
                ft.Setup(text, color);
            }
        }
    }

    // ==========================================
    // ⭐ ANIMAÇÃO DOS STARPOINTS (FIM DE FASE)
    // ==========================================
    public void AddBonusStars(int stars)
    {
        // Inicia o teatro de contagem!
        StartCoroutine(StarCountdownRoutine(stars));
    }

    private IEnumerator StarCountdownRoutine(int collectedStars)
    {
        // 1. Liga o texto gigante no meio da tela
        if (centerStarText != null)
        {
            centerStarText.gameObject.SetActive(true);
            centerStarText.text = collectedStars.ToString();
        }

        // Dá um tempinho dramático antes de começar a contar
        yield return new WaitForSeconds(0.5f);

        // 2. O Loop da contagem regressiva
        while (collectedStars > 0)
        {
            collectedStars--; // Tira da fita
            bonusStars++;     // Coloca na HUD

            // Atualiza os textos
            if (centerStarText != null) centerStarText.text = collectedStars.ToString();
            UpdateStarText();

            // Regra do SMW: 100 Estrelas = 1 Vida (Opcionalmente, toque um som aqui!)
            if (bonusStars >= 100)
            {
                bonusStars -= 100;
                AddLife(Mario.instance.transform.position); // Spawna o 1-UP na cabeça do Mario!
            }
            
            // Espera 0.05 segundos antes de passar o próximo número (Faz a contagem ser bem rápida!)
            yield return new WaitForSeconds(0.05f);
        }

        // 3. Espera meio segundo e esconde o número central
        yield return new WaitForSeconds(0.5f);
        if (centerStarText != null) centerStarText.gameObject.SetActive(false);
    }

    // ... (Mantenha o resto dos seus métodos: PlayerDied, SetCheckpoint, etc. exatamente iguais)
    
    public void PlayerDied()
    {
        totalLives--;
        ResetCombo();
        UpdateLivesText();

        if (totalLives > 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Debug.Log("Game Over!");
            ClearCheckpoint(); 
        }
    }

    public void SetCheckpoint(Vector3 pos) { hasCheckpoint = true; checkpointPosition = pos; }
    public void ClearCheckpoint() { hasCheckpoint = false; }
    public void UpdateCoinText() { if (coinText != null) coinText.text = totalCoins.ToString("0"); }
    public void UpdateScoreText() { if (scoreText != null) scoreText.text = totalScore.ToString("0"); }
    public void UpdateLivesText() { if (livesText != null) livesText.text = totalLives.ToString(); }
    public void UpdateStarText() { if (starText != null) starText.text = bonusStars.ToString(); }
}