using UnityEngine;
using System.Collections;

public class GoalGate : MonoBehaviour
{
    [Header("Configuração")]
    public Transform movingTape;
    public float moveSpeed = 3f;
    public float height = 3f;
    
    [Header("Efeitos Visuais")]
    public GameObject sparkEffectPrefab; // ⚡ Efeito ao cortar a fita
    public GameObject puffEffectPrefab;  // ☁️ Efeito de fumaça (fita ignorada)

    [Header("Recompensas")]
    public int minStars = 5;
    public int maxStars = 50;

    private Vector3 initialPos;
    private bool levelFinished = false;
    private Coroutine gateRoutine;

    void Start()
    {
        if (movingTape != null) initialPos = movingTape.localPosition;
    }

    void Update()
    {
        if (levelFinished || movingTape == null) return;
        float newY = initialPos.y + Mathf.PingPong(Time.time * moveSpeed, height);
        movingTape.localPosition = new Vector3(initialPos.x, newY, initialPos.z);
    }

    // ==========================================
    // 🧠 DETETOR INTELIGENTE DE JOGADOR
    // ==========================================
    private Mario GetValidMario(Collider2D other)
    {
        Mario m = other.GetComponentInParent<Mario>();
        if (m != null) return m;

        Yoshi y = other.GetComponentInParent<Yoshi>();
        if (y != null) return y.GetComponentInChildren<Mario>();

        return null; 
    }

    // ==========================================
    // 1. MARIO PASSOU POR BAIXO (IGNOROU A FITA)
    // ==========================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (levelFinished) return;
        
        Mario mario = GetValidMario(other);
        if (mario != null)
        {
            gateRoutine = StartCoroutine(ProcessGatePassDelay(mario));
        }
    }

    private IEnumerator ProcessGatePassDelay(Mario mario)
    {
        yield return new WaitForSeconds(0.05f); 
        if (levelFinished) yield break; 
        
        // ☁️ A FITA VIRA FUMAÇA E UMA MOEDA AUTOMÁTICA
        if (movingTape != null)
        {
            if (puffEffectPrefab != null) 
            {
                GameObject puff = Instantiate(puffEffectPrefab, movingTape.position, Quaternion.identity);
                Destroy(puff, 0.5f); // ⏱️ A fumaça some depois de meio segundo!
            }
            
            if (GameController.instance != null)
            {
                GameController.instance.AddCoin(); 
                GameController.instance.AddScore(10);
            }
            Destroy(movingTape.gameObject);
        }

        ProcessGoal(mario, 1);
    }

    // ==========================================
    // 2. MARIO CORTOU A FITA 
    // ==========================================
    public void TapeTouched(Collider2D other)
    {
        if (levelFinished) return;
        
        Mario mario = GetValidMario(other);
        if (mario == null) return;

        if (gateRoutine != null) StopCoroutine(gateRoutine);

        float relativeHeight = movingTape.localPosition.y - initialPos.y;
        float percentage = relativeHeight / height;
        int starsAwarded = Mathf.RoundToInt(Mathf.Lerp(minStars, maxStars, percentage));
        
        // ⚡ A FITA SOFRE O CORTE (SPARK)
        if (movingTape != null)
        {
            if (sparkEffectPrefab != null) 
            {
                GameObject spark = Instantiate(sparkEffectPrefab, movingTape.position, Quaternion.identity);
                Destroy(spark, 0.5f); // ⏱️ A faísca some depois de meio segundo!
            }
            Destroy(movingTape.gameObject);
        }

        ProcessGoal(mario, starsAwarded);
    }

    // ==========================================
    // FINALIZAÇÃO UNIFICADA E FAXINA
    // ==========================================
    private void ProcessGoal(Mario mario, int starsAwarded)
    {
        levelFinished = true;

        if (GameController.instance) 
        {
            GameController.instance.AddBonusStars(starsAwarded);
            
            // 🛑 A SOLUÇÃO DO PROBLEMA 2: Limpa o checkpoint ao vencer a fase!
            GameController.instance.ClearCheckpoint(); 
        }

        HandleEnemiesOnScreen();
        mario.OnLevelComplete(); 
    }

   private void HandleEnemiesOnScreen()
    {
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        int visibleCount = 0;

        foreach (EnemyBase enemy in allEnemies)
        {
            // 1. Verifica se está na tela para dar pontos
            if (IsVisible(enemy.transform))
            {
                visibleCount++;
                if (GameController.instance != null) GameController.instance.AddScore(200);
            }
            
            // 2. ☁️ CRIA A FUMAÇA NA POSIÇÃO DO INIMIGO
            if (puffEffectPrefab != null)
            {
                Instantiate(puffEffectPrefab, enemy.transform.position, Quaternion.identity);
            }
            
            // 3. DESTRÓI O INIMIGO IMEDIATAMENTE (Sem deixar cascos para trás!)
            Destroy(enemy.gameObject); 
        }

        // Regra do 1-UP para 5 ou mais inimigos
        if (visibleCount >= 5 && GameController.instance != null)
        {
            GameController.instance.AddLife();
        }
    }

    private bool IsVisible(Transform target)
    {
        if (Camera.main == null) return false;
        Vector3 viewPos = Camera.main.WorldToViewportPoint(target.position);
        return (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1);
    }
}