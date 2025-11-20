using UnityEngine;
using System.Collections;

public class KoopaTroopa : EnemyBase 
{
    [Header("Comportamento")]
    public float speed = 2f;
    public ShellType shellType;

    [Header("Camadas")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask shellLayer; 

    [Header("Verificações")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.1f;

    [Header("Lógica de Morte e Casco")]
    public GameObject shellPrefab; 
    public float deathAnimationTime = 1.0f;
    public float knockbackForce = 15f; 

    [Header("Efeitos")]
    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D col;
    private bool facingRight = false;
    private bool isTurning = false;
    private bool isDead = false;
    private bool isKnockedBack = false;

    // 0 = With_Shell, 1 = Shell_Less
    private int currentState = 0; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        animator.SetInteger("State", currentState);
    }

    void FixedUpdate()
    {
        if (isTurning || isDead || isKnockedBack) return;
        
        // --- LÓGICA DE IA (NÃO MUDA) ---
        bool wallAhead = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);
        if (wallAhead)
        {
            StartCoroutine(TurnRoutine());
            return;
        }
        if (shellType != ShellType.Green)
        {
            bool groundAhead = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
            if (!groundAhead)
            {
                StartCoroutine(TurnRoutine());
                return;
            }
        }
        float moveDirection = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocity.y);
    }

    // ⬇️⬇️⬇️ AQUI ESTÁ A CORREÇÃO PRINCIPAL ⬇️⬇️⬇️
    public override void OnStomped(GameObject stomper)
    {
        if (isDead) return;

        // ⬇️⬇️ LÓGICA DE EFEITOS (EFFECTS) ⬇️⬇️
        if (hitEffectPrefab != null)
        {
            // Spawna o efeito no topo do collider do Koopa
            Vector2 spawnPos = new Vector2(col.bounds.center.x, col.bounds.max.y);
            GameObject effect = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, hitEffectDuration);
        }
        // ⬆️⬆️ FIM DA LÓGICA DE EFEITOS ⬆️⬆️

        // USA O ESTADO INTERNO DO SCRIPT, NÃO O PARÂMETRO DO ANIMATOR
        // (Isso corrige o bug do "casco infinito")
        if (currentState == 0) // Se o SCRIPT acha que está com casco
        {
            // 1. Dispara a animação "pulled"
            animator.SetTrigger("Stomped"); 
            
            // ⬇️⬇️ INÍCIO DA NOVA LÓGICA DE SPAWN ⬇️⬇️
            if (shellPrefab != null)
            {
                // 1. Define uma origem para o Raycast (os pés do Koopa)
                // (Usamos o colisor 'col' que já temos)
                Vector2 rayOrigin = new Vector2(transform.position.x, col.bounds.min.y);
                
                // 2. Dispara um Raycast curto para baixo para encontrar o chão
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 2f, groundLayer);
                
                Vector2 spawnPos;
                
                if (hit.collider != null)
                {
                    // 3. SUCESSO! Encontramos o chão em 'hit.point'.
                    // Vamos spawnar o casco lá.
                    spawnPos = hit.point;
                }
                else
                {
                    // 4. FALHA. (Ex: Koopa foi pisado no ar).
                    // Apenas spawna o casco onde o Koopa está.
                    // (O 'ShellBase' vai ser Kinematic, então não vai cair)
                    spawnPos = transform.position;
                }

                // 5. Cria o casco na posição calculada
                GameObject shellObject = Instantiate(shellPrefab, spawnPos, Quaternion.identity);

                // 6. [BÔNUS DE POLIMENTO]
                // Move o casco para CIMA para ele ficar *sobre* o chão
                if (hit.collider != null && shellObject.TryGetComponent<Collider2D>(out Collider2D shellCol))
                {
                    // Pega a "metade da altura" (extents) do casco
                    float shellHalfHeight = shellCol.bounds.extents.y;
                    
                    // Move o casco para cima para ele ficar perfeitamente no chão
                    shellObject.transform.position = new Vector2(spawnPos.x, spawnPos.y + shellHalfHeight);
                }
            }
            // ⬆️⬆️ FIM DA NOVA LÓGICA DE SPAWN ⬆️⬆️
            
            // --- LÓGICA DE KNOCKBACK ---
            isKnockedBack = true; // ⬅️ PAUSA A IA
            float dir = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(dir * knockbackForce, rb.velocity.y);
            // --- FIM DA LÓGICA ---

            // 4. Aplica o pulo no Mario
            ApplyStompBounce(stomper);
            currentState = 1; // ⬅️ MUDANÇA CRÍTICA!
        }
        else // Se o SCRIPT acha que está SEM casco (currentState == 1)
        {
            ApplyStompBounce(stomper);
            DieSmashed(); // Chama a morte correta
        }
    }

    // ⬇️ ESTA FUNÇÃO É CHAMADA PELO SCRIPT AUXILIAR ⬇️
    public void SetInternalState(int state)
    {
        // O StateMachineBehaviour vai chamar isso no final
        // da animação "pulled".
        currentState = state;
    }

    public void OnKnockbackComplete()
    {
        isKnockedBack = false; // ⬅️ REATIVA A IA
    }

    public override void HitByShell()
    {
        if (isDead) return;
        
        // Pega o estado ATUAL do SCRIPT
        // (que já foi definido como 1 no OnStomped)
        if (currentState == 0) // Se tinha casco
        {
            DieByShell(); // Chama a morte com física!
        }
        else // Se estava ShellLess
        {
            DieSmashed(); // Chama a morte esmagada
        }
    }
    
    public override void OnLateralHit(GameObject player)
    {
        if (isDead) return;
        
        // Se o Koopa estiver sendo arremessado (estado "pulled"), ele morre ao contato!
        if (isKnockedBack)
        {
            DieSmashed(); // Morre instantaneamente
            
            // (Opcional) Adicionar pontos aqui
            if (GameController.instance != null) GameController.instance.AddScore(200);
            
            // IMPORTANTE: Damos um return aqui para NÃO chamar o base.OnLateralHit
            // Isso impede que o Mario tome dano.
            return; 
        }

        // Comportamento padrão (Mario toma dano)
        base.OnLateralHit(player);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isTurning) return;

        // --- Lógica de colisão com outros Koopas (não muda) ---
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (collision.gameObject == this.gameObject) return;
            if (collision.gameObject.TryGetComponent<KoopaTroopa>(out KoopaTroopa otherKoopa))
            {
                if (this.facingRight != otherKoopa.IsFacingRight())
                {
                    StartCoroutine(TurnRoutine());
                }
            }
        }  
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || isTurning) return;

        if (currentState == 1 && !isKnockedBack) // Se estiver "ShellLess" e fora da animação pulled
        {
            // NOTA: A checagem de "other.gameObject.layer" é um pouco diferente
            if (((1 << other.gameObject.layer) & shellLayer) != 0)
            {
                if (other.TryGetComponent<ShellBase>(out ShellBase shell))
                {
                    // 1. ADICIONA A CHECAGEM DE TIPO (como você pediu)
                    if (shell.IsIdle() && shell.shellType == this.shellType) 
                    {
                        Debug.Log("RESHELL (TRIGGER)! CONDIÇÕES CORRETAS!");
                        Destroy(shell.gameObject); // Destrói o casco

                        // 2. APENAS dispara o gatilho.
                        animator.SetTrigger("ReShell"); 
                    }
                }
            }
        }

    }
    private void DieSmashed()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("Die"); 
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true; 
        if (col != null) col.enabled = false;
        Destroy(gameObject, deathAnimationTime);
    }
    
    private void DieByShell()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("Die"); 
        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f; 
        transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
        float deathForceX = facingRight ? -3f : 3f; 
        rb.AddForce(new Vector2(deathForceX, 12f), ForceMode2D.Impulse); 
        Destroy(gameObject, 3f); 
    }
    
    #region Funções de Movimento
    System.Collections.IEnumerator TurnRoutine()
    {
        isTurning = true;
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("isTurning", true);
        yield return new WaitForSeconds(0.2f); 
        Flip();
        if (animator != null) animator.SetBool("isTurning", false);
        isTurning = false;
    }
    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    public bool IsFacingRight() { return facingRight; }
    void OnDrawGizmos()
    {
        if (groundCheck != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(groundCheck.position, checkRadius); }
        if (wallCheck != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(wallCheck.position, checkRadius); }
    }
    #endregion
}