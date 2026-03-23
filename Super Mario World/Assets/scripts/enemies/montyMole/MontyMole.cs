using UnityEngine;
using System.Collections; 

public class MontyMole : EnemyBase 
{
    [Header("Configurações de Movimento")]
    public float jumpForce = 5f;
    public float patrolSpeed = 2f;
    public bool spriteFacesLeft = true;
    
    [Tooltip("Tempo que o castor fica parado APÓS virar de direção")]
    public float turnDelay = 0.5f; 

    [Header("Efeitos e Combos")]
    public GameObject hitEffectPrefab;

    private enum State { Emerging, Patrol, Turning, Dead }
    private State currentState = State.Emerging;

    private Animator anim;
    private Rigidbody2D rb;
    private int moveDirection = 1;
    
    // ✨ Cronômetro de segurança para o pulo
    private float emergeTimer = 0f; 

    private int isEmergingHash = Animator.StringToHash("isEmerging");
    private int isPatrollingHash = Animator.StringToHash("isPatrolling");

    protected void Awake() 
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 1. Aplica a força do pulo para sair da terra
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        
        // 2. MATADOR DE MOONWALK: Já nasce olhando para o Mario instantaneamente!
        if (Mario.instance != null)
        {
            moveDirection = (Mario.instance.transform.position.x < transform.position.x) ? -1 : 1;
            int visualDirection = spriteFacesLeft ? -moveDirection : moveDirection;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * visualDirection, transform.localScale.y, transform.localScale.z);
        }

        if (anim) anim.SetBool(isEmergingHash, true);
    }

    void Update()
    {
        if (currentState == State.Dead || currentState == State.Turning) return; 

        if (currentState == State.Emerging)
        {
            // Apenas conta o tempo de vida. A mudança de animação foi TOTALMENTE removida daqui!
            emergeTimer += Time.deltaTime;
        }
        else if (currentState == State.Patrol)
        {
            if (Mario.instance != null)
            {
                int desiredDirection = (Mario.instance.transform.position.x < transform.position.x) ? -1 : 1;
                
                if (desiredDirection != moveDirection)
                {
                    StartCoroutine(TurnRoutine(desiredDirection));
                    return; 
                }
            }

            rb.linearVelocity = new Vector2(moveDirection * patrolSpeed, rb.linearVelocity.y); 
        }
    }

    // ==========================================
    // 🛬 SISTEMA DE ATERRISSAGEM BLINDADO
    // ==========================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckLanding(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckLanding(collision);
    }

    private void CheckLanding(Collision2D collision)
    {
        // 1. Só verifica se já passou de 0.2s de vida (já saiu de dentro do chão)
        if (currentState == State.Emerging && emergeTimer > 0.2f)
        {
            // 2. Só aterrissa se estiver CAINDO (velocidade <= 0.1f) E tocando no topo de algo (normal > 0.5f)
            if (rb.linearVelocity.y <= 0.1f && collision.contacts[0].normal.y > 0.5f)
            {
                currentState = State.Patrol;
                if (anim) 
                {
                    anim.SetBool(isEmergingHash, false);
                    anim.SetBool(isPatrollingHash, true);
                }
            }
        }
    }

    // ==========================================
    // 🔄 ROTINA DE VIRAR (MOONWALK E DELAY)
    // ==========================================
    private IEnumerator TurnRoutine(int newDirection)
    {
        currentState = State.Turning;
        
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        moveDirection = newDirection;
        int visualDirection = spriteFacesLeft ? -moveDirection : moveDirection;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * visualDirection, transform.localScale.y, transform.localScale.z);
        
        yield return new WaitForSeconds(turnDelay);

        if (currentState == State.Dead) yield break;

        currentState = State.Patrol;
    }

    // ==========================================
    // 💀 SISTEMA DE DANO E MORTE
    // ==========================================
    public override void OnStomped(GameObject stomper)
    {
        if (currentState == State.Dead) return;

        if (stomper.GetComponent<Yoshi>() != null)
        {
            // Lógica do Yoshi (Esmaga instantaneamente)
            if (smokeEffectPrefab != null)
            {
                GameObject smokeFx = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
                Destroy(smokeFx, 0.5f); // Se o prefab não tiver AutoDestroy, forçamos aqui!
            }
            Destroy(gameObject); 
            return; 
        }
        else 
        {
            // Lógica do Mario (Bate e joga pra fora da tela)
            if (hitEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + (Vector3.up * 0.5f);
                GameObject hitFx = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
                Destroy(hitFx, 0.4f); // Força a destruição do efeito visual
            }
            Die(); 
        }
    }

    public override void HitByShell()
    {
        Die();
    }

    private void Die()
    {
        if (currentState == State.Dead) return; 
        currentState = State.Dead;

        Collider2D[] cols = GetComponents<Collider2D>();
        foreach(Collider2D c in cols) c.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 2.5f; 
        rb.AddForce(Vector2.up * 8f, ForceMode2D.Impulse);

        Vector3 s = transform.localScale;
        s.y = -Mathf.Abs(s.y);
        transform.localScale = s;

        if (anim) anim.speed = 0; 

        Destroy(gameObject, 3f);
    }
}