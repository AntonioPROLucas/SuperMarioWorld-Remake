using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class ShellBase : HoldableObject
{
    [Header("Configurações do Casco")]
    public ShellType shellType;
    private Coroutine destroyOffScreenRoutine;
    public float moveSpeed = 8f;
    public float throwUpForce = 12f;
    public float stompBounceForce = 10f;

    [Header("Efeitos e Combos")]
    public GameObject hitEffectPrefab;
    public GameObject smokeEffectPrefab;

    [Header("Camadas")]
    public LayerMask wallLayer;
    public LayerMask groundLayer;

    // Estados
    private enum ShellState { Idle, Moving, VerticallyBouncing, Held }
    private ShellState state = ShellState.Idle;

    private Animator anim;
    private int isMovingAnimHash;
    private int direction = 1;
    private bool isBroken = false;

    protected override void Awake()
    {
        base.Awake(); // Chama o Awake do pai para pegar RB e Collider
        anim = GetComponent<Animator>();
        isMovingAnimHash = Animator.StringToHash("isMoving");
    }

    void Start()
    {
        if (state == ShellState.Idle) EnterIdle();
    }

    void FixedUpdate()
    {
        if (isBroken || state == ShellState.Held) return;

        if (state == ShellState.Moving)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
    }

    // --- IMPLEMENTAÇÃO DO PAI ---

    public override bool CanBePickedUp()
    {
        // Só pega se estiver Parado
        return state == ShellState.Idle;
    }

    public override void GetPickedUp(Mario mario)
    {
        if (state == ShellState.Held) return;
        EnterHeld();
        base.GetPickedUp(mario); // Chama a lógica física do pai
    }

    public override void Throw(Vector2 throwDirection, float force, Collider2D throwerCollider = null)
    {
        // Define o estado específico do Casco ANTES da física
        if (throwDirection == Vector2.up)
        {
            EnterVerticalBounce();
            // Chama o pai para lidar com a física, passando a força vertical específica
            base.Throw(Vector2.up, throwUpForce, throwerCollider);
        }
        else
        {
            int dir = throwDirection.x > 0 ? 1 : -1;
            EnterMoving(dir);
            // Chama o pai para lidar com a física horizontal
            base.Throw(new Vector2(dir, 0), force, throwerCollider);
        }
    }

   public void Kick(int dir)
    {
        EnterMoving(dir);
    }

    public void StompedByPlayer(GameObject stomper)
    {
        if (isBroken) return;
        SpawnEffect(hitEffectPrefab, transform.position);

        if (state == ShellState.Moving || state == ShellState.VerticallyBouncing)
        {
            EnterIdle();
            
            // O Mario pisou em um casco perigoso, ganha pontos de combo!
            if (GameController.instance) GameController.instance.RegisterComboHit(transform.position);
        }
        else if (state == ShellState.Idle)
        {
            int kickDir = (stomper.transform.position.x < transform.position.x) ? 1 : -1;
            EnterMoving(kickDir);
        }
        
        // Empurra o Mario pra cima
        if (stomper != null && stomper.TryGetComponent<Rigidbody2D>(out Rigidbody2D pRb))
        {
            pRb.linearVelocity = new Vector2(pRb.linearVelocity.x, stompBounceForce);
        }
    }

    public void DieByCrush() 
    {
        if (isBroken) return;
        isBroken = true;
        
        if(smokeEffectPrefab) 
        {
            GameObject smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
            Destroy(smoke, 1.0f);
        }
        Destroy(gameObject);
    }
    
    public void DieByImpact(Vector2? position = null) 
    {
        if (isBroken) return;
        isBroken = true;
        
        transform.SetParent(null); 
        
        // Se 'position' for nulo, usa a própria posição do objeto
        Vector2 posToSpawn = position ?? (Vector2)transform.position;
        SpawnEffect(hitEffectPrefab, posToSpawn);

        if (col) col.enabled = false;
        
        // ... restante da sua lógica original de DieByImpact (rb setup, scale, destroy)
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2.5f;
        rb.isKinematic = false;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * 8f, ForceMode2D.Impulse);

        Vector3 s = transform.localScale;
        s.y = -Mathf.Abs(s.y);
        transform.localScale = s;

        Destroy(gameObject, 3f);
    }

    // --- ESTADOS ---
    public bool IsIdle() => state == ShellState.Idle;
    public bool IsMovingHorizontally() => state == ShellState.Moving;
    public int GetDirection() => direction;

    private void EnterMoving(int dir)
    {
        state = ShellState.Moving;
        direction = dir;
        col.isTrigger = false;
        anim.SetBool(isMovingAnimHash, true);
    }

    private void EnterIdle()
    {
        state = ShellState.Idle;
        rb.linearVelocity = Vector2.zero;
        col.isTrigger = false;
        anim.SetBool(isMovingAnimHash, false);
    }

    private void EnterHeld()
    {
        state = ShellState.Held;
        anim.SetBool(isMovingAnimHash, false);
    }

    private void EnterVerticalBounce()
    {
        state = ShellState.VerticallyBouncing;
        col.isTrigger = false;
        anim.SetBool(isMovingAnimHash, false);
    }

    // --- COLISÕES ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken || state == ShellState.Held) return;

        // Declaração ÚNICA do ponto de contato
        Vector2 contactPoint = collision.GetContact(0).point;

        // 1. CHÃO
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (state == ShellState.VerticallyBouncing) EnterIdle();
        }
        // 2. PAREDES
        else if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            if (state == ShellState.Moving) direction *= -1;
        }
        // 3. INIMIGOS
        else if (collision.gameObject.TryGetComponent<EnemyBase>(out EnemyBase enemy))
        {
            if (state == ShellState.Moving || state == ShellState.VerticallyBouncing)
            {
                SpawnEffect(hitEffectPrefab, contactPoint); 
                enemy.HitByShell();
                
                // ✨ A MÁGICA DA UNIFICAÇÃO: O casco alimenta o combo global do Mario!
                if (GameController.instance) GameController.instance.RegisterComboHit(contactPoint);
            }
        }
        // 4. OUTROS CASCOS
        else if (collision.gameObject.TryGetComponent<ShellBase>(out ShellBase otherShell))
        {
            if (this.IsMovingHorizontally() && otherShell.IsMovingHorizontally())
            {
                // Dois cascos correndo batem de frente. Ambos dão pontos na mesma cadeia de combo!
                if (GameController.instance) 
                {
                    GameController.instance.RegisterComboHit(contactPoint);
                    GameController.instance.RegisterComboHit(contactPoint); 
                }
                
                this.DieByImpact(contactPoint);
                otherShell.DieByImpact(contactPoint);
            }
            else if (this.IsMovingHorizontally() && otherShell.IsIdle())
            {
                // Este casco destruiu um casco parado
                if (GameController.instance) GameController.instance.RegisterComboHit(contactPoint);
                otherShell.DieByImpact(contactPoint);
            }
        }
    }

    private void SpawnEffect(GameObject prefab, Vector2 pos)
    {
        if (prefab) Destroy(Instantiate(prefab, pos, Quaternion.identity), 1.0f);
    }
    
    void OnBecameInvisible()
    {
        if (isBroken) return; // Se já quebrou, ignora (DieByImpact cuida dele)

        // Inicia a contagem de 3 segundos antes de destruir
        if (gameObject.activeInHierarchy) // Segurança extra
        {
            destroyOffScreenRoutine = StartCoroutine(DestroyOffScreenDelay(3f));
        }
    }

    // 2. Voltou para a Tela: Cancela a destruição!
    void OnBecameVisible()
    {
        if (destroyOffScreenRoutine != null)
        {
            StopCoroutine(destroyOffScreenRoutine);
            destroyOffScreenRoutine = null;
        }
    }

    // A Corrotina de espera
    private IEnumerator DestroyOffScreenDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Se ainda não voltou pra tela após o tempo, tchau!
        Destroy(gameObject);
    }

}