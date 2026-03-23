using UnityEngine;
using System.Collections;

public enum KoopaState 
{ 
    WithShell = 0, 
    ShellLess = 1 
}

public class KoopaTroopa : EnemyBase 
{
    [Header("Comportamento")]
    public float speed = 2f;
    public ShellType shellType;

    [Header("Estado Inicial")]
    public KoopaState currentState = KoopaState.WithShell;

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
    public float deathAnimationTime = 1.0f; // Tempo para destruir após achatado
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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        animator.SetInteger("State", (int)currentState);
    }

    void FixedUpdate()
    {
        // Se a otimização estiver ligada e ele ainda não foi ativado, não faz nada.
        if (activateOnVisible && !isActivated) return;

        if (isTurning || isDead || isKnockedBack) return;

        // --- INÍCIO DO DEBUG DA PAREDE ---
        
        // Em vez de retornar apenas true/false, vamos pegar O QUE ele bateu
        Collider2D hit = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        if (hit != null)
        {
            // SE ELE ENTRAR AQUI, O SENSOR FUNCIONA
            Debug.Log($"🚨 SENSOR ATIVADO! Bati em: {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
            
            // Se aparecer essa mensagem no console e ele NÃO virar, 
            // o problema está na Coroutine TurnRoutine.
        }
        else
        {
            // SE ELE ENTRAR AQUI ENQUANTO BATE NA PAREDE, O SENSOR ESTÁ CEGO
            // (Descomente a linha abaixo se quiser ver no console, mas vai spammar muito)
            // Debug.Log("👀 Não vejo nada na Layer Wall..."); 
        }

        // --- FIM DO DEBUG ---
        
        bool wallAhead = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);
        if (wallAhead) { StartCoroutine(TurnRoutine()); return; }
        
        if (shellType != ShellType.Green)
        {
            bool groundAhead = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
            if (!groundAhead) { StartCoroutine(TurnRoutine()); return; }
        }
        
        float moveDirection = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocity.y);
    }

    public override void OnStomped(GameObject stomper)
    {
        if (isDead) return;

        // 1. Efeito Visual de Hit
        if (hitEffectPrefab != null)
        {
            Vector2 spawnPos = new Vector2(col.bounds.center.x, col.bounds.max.y);
            GameObject effect = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, hitEffectDuration);
        }

        // 2. Verifica se foi o Yoshi (Morte por Esmagamento/Fumaça)
        bool isYoshi = stomper.GetComponent<Yoshi>() != null || stomper.GetComponentInParent<Yoshi>() != null;

        if (isYoshi)
        {
            ApplyStompBounce(stomper);
            DieByCrush(); 
            return;
        }

        // 3. Foi o Mario (Lógica Clássica)
        if (currentState == KoopaState.WithShell) 
        {
            animator.SetTrigger("Stomped"); 
            
            // Spawn do Casco
            if (shellPrefab != null)
            {
                Vector2 rayOrigin = new Vector2(transform.position.x, col.bounds.min.y);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 2f, groundLayer);
                Vector2 spawnPos = hit.collider != null ? hit.point : (Vector2)transform.position;
                
                GameObject shellObject = Instantiate(shellPrefab, spawnPos, Quaternion.identity);

                if (hit.collider != null && shellObject.TryGetComponent<Collider2D>(out Collider2D shellCol))
                {
                    shellObject.transform.position = new Vector2(spawnPos.x, spawnPos.y + shellCol.bounds.extents.y);
                }
            }
            
            isKnockedBack = true;
            float dir = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * knockbackForce, rb.linearVelocity.y);
            
            ApplyStompBounce(stomper);
            currentState = KoopaState.ShellLess; 
        }
        else // Sem Casco
        {
            ApplyStompBounce(stomper);
            DieFlattened(); // Morre achatado
        }
    }

    // --- TIPOS DE MORTE ---

    // 1. Esmagado pelo Yoshi (Fumaça)
    public override void DieByCrush()
    {
        if (isDead) return;
        isDead = true;
        base.DieByCrush(); 
    }

    // 2. Achatado pelo Mario (Sem Casco)
    private void DieFlattened()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger("Die"); // Animação de achatado

        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true; 
        if (col != null) col.enabled = false;

        Destroy(gameObject, deathAnimationTime); 
    }

    // 3. Impacto Físico (Atingido por Casco)
    public override void DieByImpact()
    {
        if (isDead) return;
        isDead = true;
        
        // Se estiver DENTRO do casco, toca a animação (para virar sprite de casco fechado).
        // Se estiver FORA (pelado), NÃO toca trigger (usa o sprite atual virado).
        if (currentState == KoopaState.WithShell)
        {
            animator.SetTrigger("Die");
        }
        
        if (col != null) col.enabled = false;
        
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f; 
        
        // Inverte Sprite (Flip Y) para ficar de ponta cabeça
        transform.localScale = new Vector3(transform.localScale.x, -Mathf.Abs(transform.localScale.y), transform.localScale.z);
        
        // Lança para fora da tela
        float deathForceX = facingRight ? -3f : 3f; 
        rb.AddForce(new Vector2(deathForceX, 12f), ForceMode2D.Impulse); 
        
        Destroy(gameObject, 3f); 
    }

    public void OnKnockbackComplete() { isKnockedBack = false; }

    public override void HitByShell()
    {
        if (isDead) return;
        DieByImpact(); 
    }
    
    public override void OnLateralHit(GameObject player)
    {
        if (isDead) return;
        if (isKnockedBack)
        {
            DieByCrush(); 
            if (GameController.instance != null) GameController.instance.AddScore(200);
            return; 
        }
        base.OnLateralHit(player);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isTurning) return;

        // Se eu sou ShellLess (Pelado)
        if (currentState == KoopaState.ShellLess && !isKnockedBack)
        {
            // Verifico se bati num Casco
            if (collision.gameObject.TryGetComponent<ShellBase>(out ShellBase shell))
            {
                TryReShell(shell);
            }
        }
        
        // Colisão com outros Koopas (virar)
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (collision.gameObject != this.gameObject && 
                collision.gameObject.TryGetComponent<KoopaTroopa>(out KoopaTroopa other))
            {
                if (this.facingRight != other.IsFacingRight()) StartCoroutine(TurnRoutine());
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other); // Chama lógica de morte por zona
        
        if (currentState == KoopaState.ShellLess && !isKnockedBack)
        {
            if (other.TryGetComponent<ShellBase>(out ShellBase shell))
            {
                TryReShell(shell);
            }
        }
    }

    // Lógica Centralizada de ReShell
    private void TryReShell(ShellBase shell)
    {
        // 1. O casco precisa ser da mesma cor
        // 2. O casco precisa estar PARADO (Idle)
        if (shell.shellType == this.shellType && shell.IsIdle())
        {
            Destroy(shell.gameObject); // Consome o casco do chão
            animator.SetTrigger("ReShell"); // Toca animação
            
            // Trava lógica para não tentar pegar 2 vezes
            currentState = KoopaState.WithShell; 
            
            // Para o movimento enquanto entra no casco
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    // Helper para Animação (Animation Events geralmente pedem INT, então aqui ok)
    public void SetInternalState(int stateIndex) 
    { 
        currentState = (KoopaState)stateIndex; 
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