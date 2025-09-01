using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(MarioAnimator))]
public class mario : MonoBehaviour
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
    private float slideDecay = 15f; // quanto maior, mais rápido ele para
    private float skidDuration = 0.5f;
    private float skidTimer = 0f;
    private int lastDirection = 1; // 1 = direita, -1 = esquerda
    private int currentDirection = 1;
    private bool isSliding = false;
    private bool isSkidding = false;
    private bool wasRunning = false;


    // Valores padrão (armazenados para voltar ao normal)
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

    [SerializeField] private LayerMask deathZoneLayer;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rig;
    private BoxCollider2D boxCollider;
    private MarioAnimator marioAnim;
    private float deltaY;

    #region Inicialização
    void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        marioAnim = GetComponent<MarioAnimator>();

        float standingHalf = standingColliderSize.y / 2f;
        float lowHalf = lowColliderSize.y / 2f;
        deltaY = standingHalf - lowHalf;

        baseSpeed = Speed;   
        baseJumpForce = JumpForce; 
    }
    #endregion

    void Update()
    {
        HandleMove();
        HandleJump();
        HandleLow();
    }

    #region Movimento Horizontal
    void HandleMove()
    {
        if (isSkidding)
        {
            skidTimer -= Time.deltaTime;
        }
        

        if (isLowered && !isSliding)
        {
            marioAnim.SetRun(false);
            marioAnim.SetSkid(false);
        }

        float h = Input.GetAxis("Horizontal");

        // --- BLOCO DE PRIORIDADE: Movimento durante o SKID ATIVO ---
        if (isSkidding && skidTimer > 0f)
        {
            marioAnim.SetSkid(true); // Ativa animação
            marioAnim.SetRun(false);
            marioAnim.SetWalk(false);

            slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDecay * Time.deltaTime * 1.5f); // Decaimento da velocidade
            transform.position += new Vector3(lastDirection, 0f, 0f) * slideSpeed * Time.deltaTime; // MOVIMENTO DO SKID

            transform.eulerAngles = (lastDirection > 0) ? Vector3.zero : new Vector3(0, 180f, 0);

            if (skidTimer <= 0f)
            {
                isSkidding = false;
                slideSpeed = 0f;
                marioAnim.SetSkid(false);

                if (Mathf.Abs(h) > 0.01f) // Se ainda houver input horizontal
                {
                    lastDirection = h > 0 ? 1 : -1;
                }
            }
            return; // IMPORTANTE: Sai da função. O movimento deste frame foi controlado pelo skid.
        }

        // --- MOVIMENTO ATIVO (jogador segurando tecla) ---
        if (Mathf.Abs(h) > 0.01f && !isLowered)
        {
            if (isSkidding && (skidTimer <= 0f))
            {
                isSkidding = false;
                marioAnim.SetSkid(false);
                // Atualiza lastDirection aqui também, caso o término do skid não tenha sido pego no bloco acima
                lastDirection = h > 0 ? 1 : -1;
            }
            // Direção e rotação
            currentDirection = h > 0 ? 1 : -1;
            // Detecta mudança brusca de direção enquanto correndo
            // Se mudou a direção...
            if (currentDirection != lastDirection)
            {
                // ... e ainda está correndo, inicia derrapagem
                if (Speed > baseSpeed + 2)
                {
                    isSliding = true;
                    isSkidding = true;
                    slideSpeed = Speed;
                    skidTimer = skidDuration; // Inicia o cronômetro
                    marioAnim.SetSkid(true);
                    marioAnim.SetRun(false);
                    marioAnim.SetWalk(false);
                    return; // ← impede que ele ande na direção oposta no mesmo frame
                }
                else
                {
                    // Mudou a direção mas não está mais correndo → só atualiza a direção
                    lastDirection = currentDirection;
                }
            }
            transform.position += new Vector3(h, 0f, 0f) * Speed * Time.deltaTime;
            transform.eulerAngles = (h > 0) ? Vector3.zero : new Vector3(0, 180f, 0);

            // Seta animações
            bool isRunning = Speed > baseSpeed + 2f;
            marioAnim.SetWalk(true);
            marioAnim.SetRun(isRunning);

            // Registra se ele chegou a correr
            if (isRunning && !isSliding)
                wasRunning = true;

            // Enquanto estiver andando, deslize está desativado
            isSliding = false;
        }

        // --- NENHUMA TECLA DE MOVIMENTO (possível deslize) ---
        else
        {
            marioAnim.SetWalk(false);
            marioAnim.SetRun(false);

            // Se estava correndo antes e ainda não deslizou, inicia deslize
            if (wasRunning && !isSkidding)
            {
                isSliding = true;
                slideSpeed = Speed;
                wasRunning = false; // importante! evita repetir deslize
            }

            // Executa deslize
            if (isSliding)
            {
                slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDecay * Time.deltaTime);
                transform.position += new Vector3(lastDirection, 0f, 0f) * slideSpeed * Time.deltaTime;

                // Ativa animações durante deslize:
                marioAnim.SetWalk(true);          // sempre ativa o "walk" pra efeito de deslize
                marioAnim.SetLow(isLowered);      // ativa "low" se estiver abaixado, desativa se não
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

    #region Colisão com chão
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = true;
            marioAnim.SetJump(false);
            marioAnim.SetFall(false);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;

            var state = marioAnim.GetCurrentState();
            float delay = state.IsName("jumping") ? 0.4f : 0.1f;
            StartCoroutine(SetFallingAfterDelay(delay));
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
        boxCollider.offset = lowColliderOffset;
        transform.position += Vector3.down * deltaY;
    }

    private void ExitLow()
    {
        isLowered = false;
        marioAnim.SetLow(false);

        boxCollider.size = standingColliderSize;
        boxCollider.offset = standingColliderOffset;
        transform.position += Vector3.up * deltaY;
    }
    #endregion

    void DieFromFall()
    {
        StartCoroutine(RespawnAfterDelay(0.5f));
    }
    
    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameController.instance.PlayerDied();
    }

}