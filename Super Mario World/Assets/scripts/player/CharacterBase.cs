using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Configurações Base")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;

    [Header("Layers Globais (Configurar Aqui)")]
    // protected permite que Mario e Yoshi acessem essas variáveis
    [SerializeField] protected LayerMask groundLayer; 
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected LayerMask deathZoneLayer;

    // Componentes
    protected Rigidbody2D rb;
    protected Animator anim;

    // Estados Universais
    public bool IsGrounded { get; protected set; }
    public int FacingDirection { get; protected set; } = 1;
    public bool IsInvincible { get; protected set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // --- CONTRATOS (MÉTODOS VIRTUAIS) ---

    public virtual void TakeDamage()
    {
        Debug.Log($"{gameObject.name} sofreu dano.");
    }

    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} morreu.");
        if (GameController.instance != null) GameController.instance.PlayerDied();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public virtual void CollectCoin()
    {
        if (GameController.instance != null) GameController.instance.AddCoin();
    }

    public virtual void BounceUp(float forceMultiplier = 1.0f)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        rb.AddForce(Vector2.up * jumpForce * forceMultiplier, ForceMode2D.Impulse);
    }
    
    public virtual void SetFacingDirection(int dir)
    {
        FacingDirection = dir;
    }
    
    public int GetFacingDirection()
    {
        return FacingDirection;
    }

    // --- LÓGICA UNIVERSAL DE MORTE (TRIGGER) ---
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // Se bater na DeathZone, morre.
        if (((1 << other.gameObject.layer) & deathZoneLayer) != 0)
        {
            Die();
        }
    }

    // Variável para travar inputs
    protected bool isInputDisabled = false; 

    // Método chamado pelo GoalGate
    public virtual void OnLevelComplete()
    {
        isInputDisabled = true; // Trava controles do jogador
        // NÃO ativamos isKinematic aqui! Deixamos a gravidade agir.
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Zera velocidade horizontal, mantém vertical
    }
}