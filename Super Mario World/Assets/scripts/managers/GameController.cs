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
    public GameObject floatingTextPrefab; // O Prefab do texto flutuante
    public TextMeshProUGUI centerStarText; // O Texto gigante no meio da tela

    // --- CONFIGURAÇÃO DE COMBO ---
    private int[] comboPoints = { 200, 400, 800, 1000, 2000, 4000, 8000 };
    private int currentComboIndex = 0;
    private float lastTimeCombo = 0f;

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
            transform.SetParent(null);
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

        if (AudioManager.instance != null) 
        {
            AudioManager.instance.RestartBGM();
            AudioManager.instance.SetYoshiDrums(false); // Trava de segurança ativada!
        }
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
        if (Time.time < lastTimeCombo + 0.1f) return;
        lastTimeCombo = Time.time;

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
        AudioManager.instance.PlaySFX("Coin");

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
        AudioManager.instance.PlaySFX("oneUp");
        
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
    
   public void PlayerDied()
    {
        totalLives--;
        ResetCombo();
        UpdateLivesText();

        // 1. Para a música alegre imediatamente!
        if (AudioManager.instance != null) AudioManager.instance.StopBGM();

        if (totalLives > 0)
        {
            // 2. Toca o som de Morte
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Death");
            
            // 3. Inicia a contagem de tempo para o som tocar antes de recarregar a fase
            StartCoroutine(DeathRoutine());
        }
        else
        {
            // 4. Toca o som de Game Over
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX("GameOver");
            
            ClearCheckpoint(); 
            totalLives = 5; 
            bonusStars = 0;
            totalCoins = 0; 
            totalScore = 0; 
            
            GameOverManager telaMorte = FindObjectOfType<GameOverManager>();
            if (telaMorte != null) telaMorte.ActivateGameOver();
        }
    }

    // Espera o tempo do "jingle" de morte antes de reiniciar a tela
    private IEnumerator DeathRoutine()
    {
        // Espera 3 segundos (Ajuste esse tempo de acordo com a duração do seu áudio de morte)
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetCheckpoint(Vector3 pos) { hasCheckpoint = true; checkpointPosition = pos; }
    public void ClearCheckpoint() { hasCheckpoint = false; }
    public void UpdateCoinText() { if (coinText != null) coinText.text = totalCoins.ToString("0"); }
    public void UpdateScoreText() { if (scoreText != null) scoreText.text = totalScore.ToString("0"); }
    public void UpdateLivesText() { if (livesText != null) livesText.text = totalLives.ToString(); }
    public void UpdateStarText() { if (starText != null) starText.text = bonusStars.ToString(); }
}