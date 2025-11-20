using UnityEngine;

[RequireComponent(typeof(Animator))] 
public class ShellBase : MonoBehaviour
{
    public ShellType shellType;
    public float moveSpeed = 8f;
    public float throwUpForce = 10f;
    public float stompBounceForce = 10f;
    public LayerMask groundLayer; 
    public LayerMask wallLayer; 

    [Header("Efeitos")]
    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim; 
    private int isMovingAnimHash;

    private enum ShellState { Idle, Moving, VerticallyBouncing, Held }
    private ShellState state = ShellState.Idle;
    private int direction = 1;

    // Variável de Combo
    private int shellComboIndex = 0; 
    private int[] comboPointSequence = { 200, 400, 800, 1000, 2000, 4000, 8000 };
    
    private bool isBroken = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>(); 
        isMovingAnimHash = Animator.StringToHash("isMoving");
        EnterIdle();
    }
    
    void FixedUpdate()
    {
        if (isBroken) return; 

        if (state == ShellState.Moving) 
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken) return; 

        // --- 1. LÓGICA DE ATERRAGEM (Prioridade #1) ---
        // (Se bater no chão, para tudo e checa se deve ficar Idle)
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (state == ShellState.Moving && Mathf.Abs(rb.linearVelocity.x) < 0.1f) { EnterIdle(); }
            if (state == ShellState.VerticallyBouncing) { EnterIdle(); }
            return; // Aterrou, não faz mais nada este frame.
        }

        // --- 2. LÓGICA DE QUANDO O CASCO ESTÁ A GIRAR (MOVING) ---
        if (state == ShellState.Moving)
        {
            // Tenta pegar o script do outro casco (procurando no objeto-pai, por segurança)
            ShellBase otherShell = collision.gameObject.GetComponentInParent<ShellBase>();

            // 2a. Bater em OUTRO CASCO (Prioridade #2 - Mais Específico)
            if (otherShell != null && otherShell != this)
            {
                // Se o outro casco também está se movendo, destrua os dois.
                if (otherShell.IsMovingHorizontally()) 
                {
                    // Ambos se destroem
                    this.BreakAndDie();
                    otherShell.BreakAndDie();

                    // (Opcional) Efeito de fumaça
                    if (hitEffectPrefab != null)
                    {
                        Vector2 effectPosition = collision.contacts[0].point;
                        GameObject effect = Instantiate(hitEffectPrefab, effectPosition, Quaternion.identity);
                        Destroy(effect, hitEffectDuration);
                    }
                }
                // (Se o outro casco não estava se movendo, a física normal (empurrar)
                // será aplicada, o que está correto)
            }
            // 2b. Bater em Inimigos (Prioridade #3)
            else if (collision.gameObject.GetComponentInParent<EnemyBase>() is EnemyBase enemy)
            {
                // Se entramos aqui, É UM INIMIGO (porque tem o script EnemyBase)
                
                enemy.HitByShell(); // 💥 MATA O INIMIGO
                // (Lógica de combo...)
                if (GameController.instance != null)
                {
                    // Verifica se ainda tem pontos no array de combo
                    if (shellComboIndex < comboPointSequence.Length)
                    {
                        GameController.instance.AddScore(comboPointSequence[shellComboIndex]);
                        // (Opcional) Instanciar texto flutuante com os pontos aqui
                    }
                    else
                    {
                        // Se estourou o combo (ex: 8 hits), ganha vida
                        GameController.instance.AddLife();
                        // (Opcional) Tocar som de 1-UP
                    }
                    
                    shellComboIndex++; // Avança o combo
                }

                // (Efeito de Hit)
                if (hitEffectPrefab != null)
                {
                    Vector2 effectPosition = collision.contacts[0].point;
                    GameObject effect = Instantiate(hitEffectPrefab, effectPosition, Quaternion.identity);
                    Destroy(effect, hitEffectDuration);
                }
            }
            // 2c. Bater em Paredes (Prioridade #4 - Genérico)
            else if (((1 << collision.gameObject.layer) & wallLayer) != 0)
            {
                direction *= -1; 
            }
        }
        
        // --- 3. LÓGICA: QUANDO O CASCO ESTÁ A QUICAR (BOUNCING) ---
        if (state == ShellState.VerticallyBouncing)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                if (collision.gameObject.TryGetComponent<EnemyBase>(out EnemyBase enemy))
                {
                    enemy.HitByShell();
                    rb.linearVelocity = new Vector2(0, throwUpForce * 0.5f); 
                }

                // (Efeito de Hit)
                if (hitEffectPrefab != null)
                {
                    Vector2 effectPosition = collision.contacts[0].point;
                    GameObject effect = Instantiate(hitEffectPrefab, effectPosition, Quaternion.identity);
                    Destroy(effect, hitEffectDuration);
                }
            }
        }
}

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;

        // Lógica: "EU sou um casco MOVENDO (sólido) e bati em um casco PARADO (trigger)?"
        if (this.state == ShellState.Moving)
        {
            if (other.TryGetComponent<ShellBase>(out ShellBase otherShell))
            {
                if (otherShell.IsIdle())
                {
                    // 1. O casco parado (Idle) é destruído
                    otherShell.BreakAndDie();
                    
                    // 2. Eu (o casco em movimento) NÃO FAÇO NADA.
                    //    Eu não ricocheteio, apenas continuo reto.
                }
            }
        }
    }
    
    public void BreakAndDie()
    {
        if (isBroken) return;
        isBroken = true;
        state = ShellState.Idle; 
        if(rb != null) rb.linearVelocity = Vector2.zero; 
        transform.SetParent(null); 
        if(col != null)
        {
            col.enabled = false;
        }
        if(rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f; 
        }
        transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
        
        // ⬇️⬇️⬇️ A CORREÇÃO ESTÁ AQUI ⬇️⬇️⬇️
        // Vamos dar uma força X aleatória para ele voar para os lados
        float randomXForce = Random.Range(-3f, 3f);
        float randomYForce = Random.Range(8f, 12f); // Um pouco de variação no pulo
        
        rb.AddForce(new Vector2(randomXForce, randomYForce), ForceMode2D.Impulse);
        // ⬆️⬆️⬆️ FIM DA CORREÇÃO ⬆️⬆️⬆️

        Destroy(gameObject, 3f);
    }

    public bool IsIdle() => state == ShellState.Idle;
    public bool IsMovingHorizontally() => state == ShellState.Moving;

    public void EnterIdle()
    {
        if (isBroken) return; 
        state = ShellState.Idle;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = true;
        col.isTrigger = true;
        anim.SetBool(isMovingAnimHash, false); 
        shellComboIndex = 0; 
    }

    public void EnterMoving(int dir)
    {
        if (isBroken) return; 
        state = ShellState.Moving;
        direction = dir;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.enabled = true;
        col.isTrigger = false;
        anim.SetBool(isMovingAnimHash, true); 
    }

    public void EnterHeld()
    {
        if (isBroken) return; 
        state = ShellState.Held;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = true;
        col.isTrigger = true;
        anim.SetBool(isMovingAnimHash, false); 
        shellComboIndex = 0; 
    }

    public void ExitHeld(Vector2 throwDirection)
    {
        if (isBroken) return; 
        transform.SetParent(null);
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.enabled = true;
        
        if (throwDirection == Vector2.up)
        {
            state = ShellState.VerticallyBouncing;
            col.isTrigger = false; 
            anim.SetBool(isMovingAnimHash, false); 
            direction = 0;
            rb.linearVelocity = new Vector2(0, throwUpForce);
            shellComboIndex = 0; 
        }
        else
        {
            direction = throwDirection.x > 0 ? 1 : -1;
            EnterMoving(direction); 
        }
    }
    
    public void StompedByPlayer(GameObject stomper)
    {
        if (isBroken) return;

        // --- 1. Lógica de Combo ---
        if (GameController.instance != null)
        {
            // Sincroniza o combo do jogo com o do casco
            int gameStompIndex = GameController.instance.GetStompComboIndex();
            if (shellComboIndex > gameStompIndex)
            {
                GameController.instance.SetStompComboIndex(shellComboIndex);
            }
            
            GameController.instance.IncrementStompCombo();
        }

        // --- 2. Efeito Visual ---
        if (hitEffectPrefab != null)
        {
            // Cria o efeito no ponto de contato (ou na posição do casco)
            Vector2 effectPos = stomper != null ? (Vector2)stomper.transform.position : (Vector2)transform.position;
            // Tenta ajustar para ficar entre o Mario e o Casco
            if(stomper != null) effectPos = (stomper.transform.position + transform.position) / 2f;

            GameObject effect = Instantiate(hitEffectPrefab, effectPos, Quaternion.identity);
            Destroy(effect, hitEffectDuration);
        }

        // --- 3. Lógica de Estado (Stop vs Kick) ---
        if (state == ShellState.Moving)
        {
            // Se estava se movendo, PARA
            EnterIdle();
        }
        else if (state == ShellState.Idle)
        {
            // Se estava parado, CHUTA
            int kickDirection = (stomper.transform.position.x < transform.position.x) ? 1 : -1;
            EnterMoving(kickDirection);
        }

        // --- 4. Aplica o Impulso no Mario ---
        if (stomper != null && stomper.TryGetComponent<Rigidbody2D>(out Rigidbody2D stomperRb))
        {
            stomperRb.linearVelocity = new Vector2(stomperRb.linearVelocity.x, stompBounceForce);
        }
    }

    public int GetShellComboIndex()
    {
        return shellComboIndex;
    }
    
    public int GetDirection()
    {
        return direction;
    }
}