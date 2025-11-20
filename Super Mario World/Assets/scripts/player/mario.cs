using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(MarioAnimator))]
public class Mario : MonoBehaviour
{
    [Header("Movimento e Salto")]
    public float Speed = 5f;
    public float JumpForce = 18f;
    

    [Header("Corrida")]
    public KeyCode runKey = KeyCode.LeftShift;
    public float maxSpeed = 9f;
    public float maxJumpForce = 22f;
    public float acceleration = 0.8f;
    public float deceleration = 8f;
    private float slideSpeed = 0f;
    private float slideDecay = 15f; 
    private float skidDuration = 0.5f;
    private float skidTimer = 0f;
    // public float enemyStompBounce = 10f; // <-- Não é mais necessário aqui
    private int lastDirection = 1; 
    private int currentDirection = 1;
    private bool isSliding = false;
    private bool isSkidding = false;
    private bool wasRunning = false;

    private float baseSpeed;
    private float baseJumpForce;

    [Header("Collider - Em Pé")]
    public Vector2 standingColliderSize = new(1.2f, 3.27f);
    public Vector2 standingColliderOffset = new(0.03f, -0.03f);

    [Header("Collider - Abaixado")]
    public Vector2 lowColliderSize = new(1f, 1.8f);
    public Vector2 lowColliderOffset = new(0.03f, -0.03f); 

    [Header("Tecla de Abaixar")]
    public KeyCode lowKey = KeyCode.DownArrow;

    [HideInInspector] public bool isGrounded = false;
    [HideInInspector] public bool isLowered = false;
    [HideInInspector] public bool isHoldingObject = false;

    [SerializeField] private LayerMask enemyLayer; 
    [SerializeField] private LayerMask deathZoneLayer;
    [SerializeField] private LayerMask groundLayer;
    
    private int powerUpState = 1;
    private Rigidbody2D rig;
    private BoxCollider2D boxCollider;
    private MarioAnimator marioAnim;
    
    private float deltaY; 
    private Vector2 calculatedLowColliderOffset; 
    
    // ⬇️⬇️ 1. ADICIONE ESTA NOVA VARIÁVEL ⬇️⬇️
    private float lastYVelocity; 

    #region Inicialização
    void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        marioAnim = GetComponent<MarioAnimator>();

        deltaY = (standingColliderSize.y - lowColliderSize.y) / 2f;
        calculatedLowColliderOffset = new Vector2(standingColliderOffset.x, standingColliderOffset.y - deltaY);

        baseSpeed = Speed;   
        baseJumpForce = JumpForce; 
    }
    #endregion

    void Update()
    {
        marioAnim.SetPowerUpState(powerUpState); 
        marioAnim.SetHolding(isHoldingObject ? 1.0f : 0.0f);

        HandleMove();
        HandleJump();
        HandleLow();
    }

    void FixedUpdate()
    {
        lastYVelocity = rig.velocity.y;
    }


    #region Movimento Horizontal
    void HandleMove()
    {
        // =================================================================
        // 🔹 BLOCO DE ALTA PRIORIDADE: MOVIMENTO AO SEGURAR OBJETO 🔹
        // =================================================================
        if (isHoldingObject)
        {
            isSkidding = false;
            isSliding = false;
            wasRunning = false;
            slideSpeed = 0f;
            marioAnim.SetSkid(false);
            marioAnim.SetRun(false);

            // Só permite mover e virar se NÃO estiver agachado
            if (!isLowered)
            {
                float h = Input.GetAxis("Horizontal");

                if (Mathf.Abs(h) > 0.01f)
                {
                    transform.position += new Vector3(h, 0f, 0f) * Speed * Time.deltaTime;
                    marioAnim.SetWalk(true);
                }
                else
                {
                    marioAnim.SetWalk(false);
                }
                
                // Só vira se não estiver agachado
                if (h > 0) { transform.eulerAngles = Vector3.zero; lastDirection = 1; }
                else if (h < 0) { transform.eulerAngles = new Vector3(0, 180f, 0); lastDirection = -1; }
            }
            else
            {
                // Se estiver agachado, FORÇA a parada da animação
                marioAnim.SetWalk(false);
            }
                return; 
            }
        // =================================================================
        // FIM DO BLOCO DE SEGURAR OBJETO
        // =================================================================
        
        if (isSkidding)
        {
            skidTimer -= Time.deltaTime;
        }
        

        if (isLowered && !isSliding)
        {
            marioAnim.SetRun(false);
            marioAnim.SetSkid(false);
        }

        float h_original = Input.GetAxis("Horizontal");

        // --- BLOCO DE PRIORIDADE: Movimento durante o SKID ATIVO ---
        if (isSkidding && skidTimer > 0f)
        {
            marioAnim.SetSkid(true); 
            marioAnim.SetRun(false);
            marioAnim.SetWalk(false);

            slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDecay * Time.deltaTime * 1.5f);
            transform.position += new Vector3(lastDirection, 0f, 0f) * slideSpeed * Time.deltaTime; 

            transform.eulerAngles = (lastDirection > 0) ? Vector3.zero : new Vector3(0, 180f, 0);

            if (skidTimer <= 0f)
            {
                isSkidding = false;
                slideSpeed = 0f;
                marioAnim.SetSkid(false);

                if (Mathf.Abs(h_original) > 0.01f) 
                {
                    lastDirection = h_original > 0 ? 1 : -1;
                }
            }
            return; 
        }

        // --- MOVIMENTO ATIVO (jogador segurando tecla) ---
        if (Mathf.Abs(h_original) > 0.01f && !isLowered)
        {
            if (isSkidding && (skidTimer <= 0f))
            {
                isSkidding = false;
                marioAnim.SetSkid(false);
                lastDirection = h_original > 0 ? 1 : -1;
            }
            
            currentDirection = h_original > 0 ? 1 : -1;
           
            if (currentDirection != lastDirection)
            {
                if (Speed > baseSpeed + 2)
                {
                    isSliding = true;
                    isSkidding = true;
                    slideSpeed = Speed;
                    skidTimer = skidDuration; 
                    marioAnim.SetSkid(true);
                    marioAnim.SetRun(false);
                    marioAnim.SetWalk(false);
                    return; 
                }
                else
                {
                    lastDirection = currentDirection;
                }
            }
            transform.position += new Vector3(h_original, 0f, 0f) * Speed * Time.deltaTime;
            transform.eulerAngles = (h_original > 0) ? Vector3.zero : new Vector3(0, 180f, 0);

            bool isRunning = Speed > baseSpeed + 2f;
            marioAnim.SetWalk(true);
            marioAnim.SetRun(isRunning);

            if (isRunning && !isSliding)
                wasRunning = true;

            isSliding = false;
        }

        // --- NENHUMA TECLA DE MOVIMENTO (possível deslize) ---
        else
        {
            marioAnim.SetWalk(false);
            marioAnim.SetRun(false);

            if (wasRunning && !isSkidding)
            {
                isSliding = true;
                slideSpeed = Speed;
                wasRunning = false; 
            }

            if (isSliding)
            {
                slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDecay * Time.deltaTime);
                transform.position += new Vector3(lastDirection, 0f, 0f) * slideSpeed * Time.deltaTime;

                marioAnim.SetWalk(true);       
                marioAnim.SetLow(isLowered);     
                marioAnim.SetSkid(isSkidding);

                if (skidTimer <= 0f && slideSpeed <= 0.1f)
                {
                    isSliding = false;
                    isSkidding = false;
                    slideSpeed = 0f;
                    marioAnim.SetWalk(false);
                    marioAnim.SetSkid(false);
                }
            }
        }
        
        // Aumenta velocidade se estiver correndo
        if (Input.GetKey(runKey))
        {
            Speed = Mathf.MoveTowards(Speed, maxSpeed, acceleration * Time.deltaTime);
            JumpForce = Mathf.MoveTowards(JumpForce, maxJumpForce, acceleration * Time.deltaTime);
        }
        else
        {
            Speed = Mathf.MoveTowards(Speed, baseSpeed, deceleration * Time.deltaTime);
            JumpForce = Mathf.MoveTowards(JumpForce, baseJumpForce, deceleration * Time.deltaTime);
        }

        if (Speed <= 6)
            wasRunning = false;
    }
    #endregion


    #region Salto
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rig.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            marioAnim.SetJump(true);
        }
    }

    IEnumerator SetFallingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isGrounded)
        {
            marioAnim.SetFall(true);
        }
    }
    #endregion

    #region Colisão com chão e Inimigos (MÉTODO ATUALIZADO)

    void OnCollisionEnter2D(Collision2D collision)
    {
        // --- 1. VERIFICA SE ATERROU NO CHÃO ---
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = true;
            marioAnim.SetJump(false);
            marioAnim.SetFall(false);

            if (GameController.instance != null)
            {
                GameController.instance.ResetStompCombo();
            }
        }

        // --- 2. VERIFICA SE COLIDIU COM UM INIMIGO ---
        else if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            if (collision.gameObject.TryGetComponent<EnemyBase>(out EnemyBase enemy))
            {
                bool isStomp = false;

                // Usa a velocidade cacheada (lastYVelocity) para evitar bugs de "pulo duplo"
                if (lastYVelocity <= 0.1f) 
                {
                    foreach (ContactPoint2D contact in collision.contacts)
                    {
                        if (contact.normal.y > 0.2f) 
                        {
                            isStomp = true;
                            break;
                        }
                    }
                }

                if (isStomp)
                {
                    // --- AÇÃO DE PISAR (STOMP) ---
                    enemy.OnStomped(this.gameObject); 
                    
                    if (GameController.instance != null)
                    {
                        GameController.instance.IncrementStompCombo();
                    }
                    
                    marioAnim.SetTrigger("stomping");
                }
                else
                {
                    // ⬇️⬇️ NOVA LÓGICA DE ESCUDO / DANO DIRECIONAL ⬇️⬇️
                    
                    bool isDefending = false;

                    // Se estiver segurando algo, checa se o inimigo está NA FRENTE
                    if (isHoldingObject)
                    {
                        // Calcula a direção do inimigo em relação ao Mario
                        float directionToEnemy = enemy.transform.position.x - transform.position.x;
                        
                        // Se o sinal da direção do inimigo for igual ao sinal para onde o Mario olha...
                        if (Mathf.Sign(directionToEnemy) == Mathf.Sign(lastDirection))
                        {
                            isDefending = true; // ...então o casco protege!
                        }
                    }

                    // Só toma dano se NÃO estiver defendendo
                    if (!isDefending)
                    {
                        enemy.OnLateralHit(this.gameObject);
                    }
                    // Se estiver defendendo (else implícito), não faz nada aqui no Mario.cs.
                    // O script MarioShellInteraction.cs vai detectar a colisão e matar o inimigo/quebrar o casco.
                    
                    // ⬆️⬆️ FIM DA NOVA LÓGICA ⬆️⬆️
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;

            if (gameObject.activeInHierarchy) 
            {
                var state = marioAnim.GetCurrentState();
                float delay = state.IsName("jumping") ? 0.4f : 0.1f;
                StartCoroutine(SetFallingAfterDelay(delay));
            }
        }
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & deathZoneLayer) != 0)
        {
            DieFromFall();
        }
    }
    
    #region Abaixar
    void HandleLow()
    {
        // ... (Sua lógica de HandleLow() não muda) ...
        // (Copie e cole a sua lógica de abaixar aqui)
        if (!isGrounded)
        {
            if (isLowered) ExitLow();
            return;
        }

        if (Input.GetKey(lowKey))
        {
            if (!isLowered) EnterLow();
        }
        else
        {
            if (isLowered) ExitLow();
        }
    }

    private void EnterLow()
    {
        isLowered = true;
        marioAnim.SetLow(true);
        boxCollider.size = lowColliderSize;
        boxCollider.offset = calculatedLowColliderOffset;
    }

    private void ExitLow()
    {
        isLowered = false;
        marioAnim.SetLow(false);
        boxCollider.size = standingColliderSize;
        boxCollider.offset = standingColliderOffset;
    }
    #endregion

    public int GetFacingDirection()
    {
        return lastDirection;
    }

    public void ChangePowerUpState(int newState)
    {
        powerUpState = newState;
    }

    // === Sistema de dano ===
    public void TakeDamage()
    {
        // ... (Sua lógica de dano aqui) ...
        Debug.Log("Mario tomou dano!");
        StartCoroutine(RespawnAfterDelay(0.2f));
    }

    void DieFromFall()
    {
        // ... (Sua lógica de morte por queda aqui) ...
        Debug.Log("Mario caiu no buraco!");
        StartCoroutine(RespawnAfterDelay(0.5f));
    }
    
    IEnumerator RespawnAfterDelay(float delay)
    {
        // ... (Sua lógica de respawn aqui) ...
        this.enabled = false;
        rig.velocity = Vector2.zero;
        rig.isKinematic = true;
        
        yield return new WaitForSeconds(delay);

        if (GameController.instance != null)
        {
            GameController.instance.PlayerDied();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}