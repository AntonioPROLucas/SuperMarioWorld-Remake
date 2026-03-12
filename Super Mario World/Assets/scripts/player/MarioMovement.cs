using UnityEngine;

[RequireComponent(typeof(Mario))] 
public class MarioMovement : MonoBehaviour
{
    [Header("Configurações de Corrida")]
    public KeyCode runKey = KeyCode.LeftShift;
    public float maxSpeed = 9f;
    public float acceleration = 0.8f;
    public float deceleration = 8f;
    
    [Header("Configurações de Pulo/Abaixar")]
    public KeyCode lowKey = KeyCode.DownArrow;
    public KeyCode upKey = KeyCode.UpArrow;
    public Vector2 standingSize = new(1.2f, 3.27f);
    public Vector2 standingOffset = new(0.03f, -0.03f);
    public Vector2 lowSize = new(1f, 1.8f);
    public Vector2 lowOffset = new(0.03f, -0.9f);

    [Header("Efeitos de Movimento")]
    public GameObject smokePrefab;
    public Transform smokeSpawnPoint;
    public float smokeInterval = 0.05f; 
    private float lastSmokeTime;

    // ✨ NOVAS VARIÁVEIS DA MONTARIA
    [Header("Montaria (Yoshi)")]
    [Header("Montaria (Yoshi)")]
    public KeyCode mountKey = KeyCode.LeftAlt; 
    public float yoshiMountRange = 2.5f;
    private float mountCooldown = 0f; // 🛑 Cooldown para evitar duplo input
    private bool wasRiding = false;

    // Estados Internos 
    private float currentSpeed;
    private float currentJumpForce;
    private float slideSpeed = 0f;
    private float slideDecay = 15f; 
    private float skidDuration = 0.5f;
    private float skidTimer = 0f;
    
    private bool isSliding = false;
    private bool isSkidding = false;
    private bool wasRunning = false;
    
    private bool isLowered;
    private bool isLookingUp;

    // Referências
    private Mario mario; 
    private Rigidbody2D rb;
    private MarioAnimator anim;
    private BoxCollider2D col;

    void Awake()
    {
        mario = GetComponent<Mario>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<MarioAnimator>();
        col = GetComponent<BoxCollider2D>();
        
        currentSpeed = mario.moveSpeed; 
        currentJumpForce = mario.jumpForce;
    }

    void Update()
    {
        // 🛑 Lógica do Cooldown
        if (mountCooldown > 0) mountCooldown -= Time.deltaTime;

        bool currentlyRiding = anim.IsRiding();
        if (wasRiding && !currentlyRiding) 
        {
            mountCooldown = 0.5f; // Aplica meio segundo de trava ao sair do Yoshi
        }
        wasRiding = currentlyRiding;

        HandleMove();
        HandleJump();
        HandleLow();
        HandleLookUp();
        HandleAcceleration();
        HandleSmokeEffects();
        HandleMountInput();
    }

    // ==========================================
    // 🕹️ LÓGICA DE MOVIMENTO
    // ==========================================
    void HandleMove()
    {
        if (mario.isHoldingObject)
        {
            ResetSkidFlags();
            if (!isLowered)
            {
                float h = Input.GetAxis("Horizontal");
                if (Mathf.Abs(h) > 0.01f)
                {
                    transform.position += new Vector3(h, 0f, 0f) * currentSpeed * Time.deltaTime;
                    anim.SetWalk(true);
                    Flip(h > 0 ? 1 : -1);
                }
                else
                {
                    anim.SetWalk(false);
                }
            }
            return; 
        }
        
        if (isSkidding) skidTimer -= Time.deltaTime;

        if (isLowered && !isSliding)
        {
            anim.SetRun(false);
            anim.SetSkid(false);
        }

        float h_original = Input.GetAxis("Horizontal");

        if (isSkidding && skidTimer > 0f)
        {
            ExecuteSkid();
            return; 
        }

        if (Mathf.Abs(h_original) > 0.01f && !isLowered)
        {
            if (isSkidding && (skidTimer <= 0f))
            {
                isSkidding = false;
                anim.SetSkid(false);
                Flip(h_original > 0 ? 1 : -1);
            }
            
            int inputDirection = h_original > 0 ? 1 : -1;
           
            if (inputDirection != mario.FacingDirection)
            {
                if (currentSpeed > mario.moveSpeed + 2)
                {
                    StartSkid();
                    return; 
                }
                else
                {
                    Flip(inputDirection);
                }
            }

            transform.position += new Vector3(h_original, 0f, 0f) * currentSpeed * Time.deltaTime;
            Flip(inputDirection);

            bool isRunning = currentSpeed > mario.moveSpeed + 2f;
            anim.SetWalk(true);
            anim.SetRun(isRunning);

            if (isRunning && !isSliding) wasRunning = true;
            isSliding = false;
        }
        else 
        {
            HandleNoInput();
        }
    }

    private void StartSkid()
    {
        isSliding = true;
        isSkidding = true;
        slideSpeed = currentSpeed;
        skidTimer = skidDuration; 
        anim.SetSkid(true);
        anim.SetRun(false);
        anim.SetWalk(false);
    }

    private void ExecuteSkid()
    {
        anim.SetSkid(true); 
        anim.SetRun(false);
        anim.SetWalk(false);

        slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDecay * Time.deltaTime * 1.5f);
        transform.position += new Vector3(mario.FacingDirection, 0f, 0f) * slideSpeed * Time.deltaTime; 

        if (skidTimer <= 0f)
        {
            isSkidding = false;
            slideSpeed = 0f;
            anim.SetSkid(false);
        }
    }

    private void HandleNoInput()
    {
        anim.SetWalk(false);
        anim.SetRun(false);

        if (wasRunning && !isSkidding)
        {
            isSliding = true;
            slideSpeed = currentSpeed;
            wasRunning = false; 
        }

        if (isSliding)
        {
            slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDecay * Time.deltaTime);
            transform.position += new Vector3(mario.FacingDirection, 0f, 0f) * slideSpeed * Time.deltaTime;

            anim.SetWalk(true);       
            anim.SetLow(isLowered);     
            anim.SetSkid(isSkidding);

            if (skidTimer <= 0f && slideSpeed <= 0.1f)
            {
                isSliding = false;
                isSkidding = false;
                slideSpeed = 0f;
                anim.SetWalk(false);
                anim.SetSkid(false);
            }
        }
    }

    private void ResetSkidFlags()
    {
        isSkidding = false;
        isSliding = false;
        wasRunning = false;
        slideSpeed = 0f;
        anim.SetSkid(false);
        anim.SetRun(false);
    }

    // ==========================================
    // 🧊 FÍSICA E ESTADOS AUXILIARES
    // ==========================================
    void HandleAcceleration()
    {
        if (Input.GetKey(runKey))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
            currentJumpForce = Mathf.MoveTowards(currentJumpForce, mario.jumpForce * 1.3f, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, mario.moveSpeed, deceleration * Time.deltaTime);
            currentJumpForce = Mathf.MoveTowards(currentJumpForce, mario.jumpForce, deceleration * Time.deltaTime);
        }

        if (currentSpeed <= 6) wasRunning = false;
    }

    void Flip(int direction)
    {
        mario.SetFacingDirection(direction); 
        if (transform.parent != null) return; 
        
        if (direction > 0) transform.eulerAngles = Vector3.zero;
        else transform.eulerAngles = new Vector3(0, 180f, 0);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && mario.IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
            rb.AddForce(Vector2.up * currentJumpForce, ForceMode2D.Impulse);
            anim.SetJump(true);
        }
    }

    // ==========================================
    // 🔽 ABAIXAR E OLHAR CIMA
    // ==========================================
    void HandleLow()
    {
        if (!mario.IsGrounded)
        {
            if (isLowered) ExitLow();
            return;
        }

        if (Input.GetKey(lowKey))
        {
            if (!isLowered) EnterLow();
        }
        else if (isLowered) ExitLow();
    }

    void EnterLow()
    {
        isLowered = true;
        anim.SetLow(true);
        col.size = lowSize;
        col.offset = lowOffset;
    }

    void ExitLow()
    {
        isLowered = false;
        anim.SetLow(false);
        col.size = standingSize;
        col.offset = standingOffset;
    }

    void HandleLookUp()
    {
        bool moving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f;
        if (mario.IsGrounded && !isLowered && !moving && Input.GetKey(upKey))
        {
            if (!isLookingUp)
            {
                isLookingUp = true;
                anim.SetLookUp(true);
            }
        }
        else if (isLookingUp)
        {
            isLookingUp = false;
            anim.SetLookUp(false);
        }
    }

    private void HandleSmokeEffects()
    {
        if (mario.isHoldingObject || !mario.IsGrounded) return;

        bool isRunning = currentSpeed > mario.moveSpeed + 2f;
        bool shouldEmit = isRunning || isSkidding;

        if (shouldEmit)
        {
            if (Time.time >= lastSmokeTime + smokeInterval)
            {
                Instantiate(smokePrefab, smokeSpawnPoint.position, Quaternion.identity);
                lastSmokeTime = Time.time;
            }
        }
    }

    // ==========================================
    // 🦖 SISTEMA DE MONTARIA DE EMERGÊNCIA
    // ==========================================
    private void HandleMountInput()
    {
        if (mountCooldown > 0f) return; // 🛑 Impede a montaria se o cooldown estiver ativo!

        // Aceita a tecla oficial, Z, e ambos os botões ALT do teclado
        if (Input.GetKeyDown(mountKey) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            if (!mario.isHoldingObject && !anim.IsRiding())
            {
                TryMountYoshi();
            }
        }
    }

    private void TryMountYoshi()
    {
        Yoshi[] allYoshis = FindObjectsByType<Yoshi>(FindObjectsSortMode.None);
        Yoshi closestYoshi = null;
        float closestDistance = yoshiMountRange;

        // Procura o Yoshi mais próximo dentro do raio de 2.5f
        foreach (Yoshi foundYoshi in allYoshis)
        {
            if (!foundYoshi.gameObject.activeInHierarchy) continue;

            float distance = Vector2.Distance(transform.position, foundYoshi.transform.position);
            
            // Se encontrou um Yoshi perto e ele estiver vazio
            if (distance <= closestDistance && !foundYoshi.IsMouthFull()) // Opcional: checagem de estado
            {
                closestYoshi = foundYoshi;
                closestDistance = distance;
            }
        }

        // Se achou o Yoshi com sucesso, monta nele!
        if (closestYoshi != null)
        {
            transform.position = closestYoshi.transform.position;
            closestYoshi.MountPlayer(mario.gameObject);
            ResetSkidFlags(); 
        }
    }
}