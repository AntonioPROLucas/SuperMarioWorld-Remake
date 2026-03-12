using UnityEngine;
using System.Collections;

public class Yoshi : CharacterBase
{
    // ==========================================
    // ⚙️ CONFIGURAÇÕES
    // ==========================================
    [Header("Movement Stats")]
    public float baseSpeed = 5f;
    public float maxSpeed = 9f;
    public float acceleration = 20f;
    public float deceleration = 20f;
    public float turnDuration = 0.2f;
    public float panicSpeed = 7f;

    [Header("Wall Sensor")]
    public float wallSensorHeight = -0.5f; 
    public float wallSensorDistance = 1.5f;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public float effectDuration = 0.5f;
    
    [Header("References")]
    public Transform saddlePoint; 
    public Transform mouthPoint;  
    public GameObject tongueObject; 
    public GameObject greenShellPrefab;
    public GameObject redShellPrefab;
    public GameObject fireballPrefab;

    [Header("Tongue Settings")]
    public float delayAntesDaLingua = 0.05f; 
    public float duracaoDaLingua = 0.2f;    

    [Header("Inputs")]
    public KeyCode dismountKey = KeyCode.LeftControl;
    public KeyCode tongueKey = KeyCode.X;

    // Estados
    public enum YoshiState { Idle, Ridden, Panic }
    private YoshiState currentState = YoshiState.Idle;
    
    // 🏆 VITÓRIA (Variáveis de controle do Mario)
    private bool isVictory = false;
    private bool victoryWalking = false;
    private bool victoryPosing = false;

    // Movimento Interno
    private float currentSpeed = 0f;
    private bool isTurning = false;

    // Montaria & Combate
    private GameObject rider;
    private MarioAnimator riderAnim;
    private Vector3 storedRiderScale;
    public bool hasMouthFull = false; 
    private ShellType heldShellType;

    // Hashes do Animator
    private int animWalk, animJump, animFall, animTurn, animPanic, animRidden, animEat, animLick, animSpit, animHold, animBirth;

    // Layers & Combo
    public LayerMask wallLayer;
    private int fruitLayer;
    private int defaultLayer;
    private int ghostLayer;
    private int deathZoneLayerID; 
    private int yoshiComboIndex = 0;
    private int[] comboPointSequence = { 200, 400, 800, 1000, 2000, 4000, 8000 };

    protected override void Awake()
    {
        base.Awake();
        // Inicializa Hashes
        animWalk = Animator.StringToHash("walk");
        animJump = Animator.StringToHash("jump");
        animFall = Animator.StringToHash("fall");
        animTurn = Animator.StringToHash("turn");
        animPanic = Animator.StringToHash("panic");
        animRidden = Animator.StringToHash("ridden");
        animEat = Animator.StringToHash("eat");
        animLick = Animator.StringToHash("lick");
        animSpit = Animator.StringToHash("spit");
        animHold = Animator.StringToHash("holding");
        animBirth = Animator.StringToHash("birth");
        
        anim.SetFloat("holding_blend", 0f);

        // Inicializa Layers
        defaultLayer = gameObject.layer;
        fruitLayer = LayerMask.NameToLayer("Fruit");
        ghostLayer = LayerMask.NameToLayer("LayerGhost");
        deathZoneLayerID = LayerMask.NameToLayer("DeathZone"); 
        if (ghostLayer == -1) ghostLayer = defaultLayer;

        if (currentState != YoshiState.Ridden) gameObject.layer = ghostLayer;
        if (tongueObject != null) tongueObject.SetActive(false);
    }

    // 🏆 QUANDO A FASE ACABA
    public override void OnLevelComplete()
    {
        base.OnLevelComplete();
        isVictory = true;
        
        // Mantém a gravidade (Y) mas zera movimento lateral (X)
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        // Limpa ações
        StopAllCoroutines();
        if (tongueObject) tongueObject.SetActive(false);
        if (riderAnim) riderAnim.SetRidePoint(false);
    }

    // 🏆 LOOP DE ATUALIZAÇÃO (CORRIGIDO)
    private void Update()
    {
        // 1. Lógica de Vitória (Prioridade)
        if (isVictory)
        {
            if (anim != null)
            {
                if (victoryPosing)
                {
                    anim.Play("idle"); // Força Idle imediato
                }
                else if (victoryWalking)
                {
                    anim.Play("walking"); 
                }
                else
                {
                    anim.Play("idle");
                }
                
                // Limpa parâmetros para não dar conflito
                anim.SetBool(animWalk, victoryWalking);
                anim.SetBool(animJump, false);
                anim.SetBool(animFall, false);
            }
            return; // 🛑 Impede que o resto do código rode
        }

        // 2. Lógica Normal (Agora dentro do Update corretamente)
        switch (currentState)
        {
            case YoshiState.Ridden:
                if (!isTurning)
                {
                    HandleMovePhysics();
                    HandleJump();
                    HandleTongueInput(); 
                    if (Input.GetKeyDown(dismountKey)) Dismount(false);
                }
                break;
            case YoshiState.Panic: break;
            case YoshiState.Idle:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                currentSpeed = 0f;
                break;
        }
        UpdateAnimations();
    } // <--- A CHAVE DO UPDATE FECHA AQUI

    private void FixedUpdate()
    {
        if (isVictory) return; // Não processa física na vitória

        if (currentState == YoshiState.Panic)
        {
            HandlePanicMovement();
        }
    }

    // ==========================================
    // MÉTODOS DE COLISÃO E FÍSICA
    // ==========================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            IsGrounded = true;
            anim.SetBool(animJump, false);
            anim.SetBool(animFall, false);
        }

        if (currentState != YoshiState.Ridden) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (collision.gameObject.TryGetComponent<EnemyBase>(out EnemyBase enemy))
            {
                if (IsStomp(collision))
                {
                    enemy.OnStomped(this.gameObject);
                    HandleCombo();
                    BounceUp(0.4f);
                }
                else TakeDamage();
            }
        }
        else if (collision.gameObject.CompareTag("Shell"))
        {
            if (collision.gameObject.TryGetComponent<ShellBase>(out ShellBase shell))
            {
                if (IsStomp(collision))
                {
                    shell.DieByCrush();
                    BounceUp(0.3f);
                }
                else if (shell.IsIdle()) 
                {
                    SpawnHitEffect(collision.contacts[0].point);
                    shell.DieByCrush();
                }
                else 
                {
                    TakeDamage();
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // 🛑 TRAVA DE SEGURANÇA NA VITÓRIA
        if (isVictory) return; 

        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            IsGrounded = false;
            // Só roda coroutine se o objeto estiver ativo e válido
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SetFallingAfterDelay(rb.linearVelocity.y > 0 ? 0.1f : 0.05f));
            }
        }
    }

    IEnumerator SetFallingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsGrounded && rb.linearVelocity.y <= 0.01f)
            anim.SetBool(animFall, true);
    }

    // ==========================================
    // MOVIMENTO & COMBATE
    // ==========================================
    private void HandleMovePhysics()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.01f)
        {
            int inputDirection = h > 0 ? 1 : -1;
            if (inputDirection != FacingDirection)
            {
                StartCoroutine(FlipRoutine(inputDirection));
                return;
            }
            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? maxSpeed : baseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            transform.position += new Vector3(inputDirection * currentSpeed * Time.deltaTime, 0f, 0f);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            if (currentSpeed > 0.1f) transform.position += new Vector3(FacingDirection * currentSpeed * Time.deltaTime, 0f, 0f);
            else currentSpeed = 0f;
        }
    }

    private IEnumerator FlipRoutine(int newDirection)
    {
        isTurning = true;
        currentSpeed = 0f;
        anim.SetTrigger(animTurn);
        if (riderAnim != null) riderAnim.SetRideTurning(true);
        yield return new WaitForSeconds(turnDuration);
        if (riderAnim != null) riderAnim.SetRideTurning(false);
        FlipInstant(newDirection);
        isTurning = false;
    }

    private void FlipInstant(int direction)
    {
        SetFacingDirection(direction);
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
        if (rider != null) rider.transform.localRotation = Quaternion.identity;
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            anim.SetBool(animJump, true);
            anim.SetBool(animWalk, false);
        }
    }

    private void HandleTongueInput()
    {
        if (Input.GetKeyDown(tongueKey))
        {
            if (hasMouthFull) SpitOut();
            else StartCoroutine(LickRoutine());
        }
    }

    private IEnumerator LickRoutine()
    {
        if (riderAnim != null) riderAnim.SetRidePoint(true);
        yield return new WaitForSeconds(0.2f); 
        anim.SetTrigger(animLick);
        yield return new WaitForSeconds(delayAntesDaLingua); 
        if (tongueObject != null) tongueObject.SetActive(true);
        yield return new WaitForSeconds(duracaoDaLingua); 
        if (tongueObject != null) tongueObject.SetActive(false);
        if (riderAnim != null) riderAnim.SetRidePoint(false);
    }

    public void OnTongueHit(GameObject target)
    {
        if (target.CompareTag("Coin")) {
            if (target.TryGetComponent<Coin>(out Coin coin)) coin.CollectDirectly(this); 
            else Destroy(target); 
            return;
        }
        
        if (hasMouthFull) return;

        if (target.TryGetComponent<ShellBase>(out ShellBase shell)) { 
            EatShell(shell.shellType); 
            Destroy(target); 
            return; 
        }

        if (target.CompareTag("Enemy")) {
            if (target.TryGetComponent<EnemyBase>(out EnemyBase enemy)) {
                
                // 🛑 REGRA DE BALANCEAMENTO: Inimigos pesados que o Yoshi NÃO pode comer
                if (target.TryGetComponent<CharginChuck>(out CharginChuck chuck))
                {
                    // A língua apenas bate nele e volta, sem engolir e sem dar dano!
                    return; 
                }

                // Lógica normal para os outros inimigos
                if (target.TryGetComponent<KoopaTroopa>(out KoopaTroopa koopa)) {
                    if (koopa.currentState == KoopaState.ShellLess) SwallowEnemy(target); 
                    else { EatShell(koopa.shellType); Destroy(target); }
                } 
                else 
                {
                    SwallowEnemy(target);
                }
                return;
            }
        }
        
        if (target.CompareTag("Fruit") || target.gameObject.layer == fruitLayer) CollectFruit(target);
    }

    private void CollectFruit(GameObject target)
    {
        anim.SetTrigger(animEat); 
        if (target.TryGetComponent<Coin>(out Coin fruitScript)) fruitScript.CollectDirectly(this); else Destroy(target);
    }

    private void EatShell(ShellType type) 
    { 
        hasMouthFull = true; heldShellType = type; anim.SetBool(animHold, true); 
    }
    
    private void SwallowEnemy(GameObject target)
    {
        anim.SetTrigger(animEat); 
        if (GameController.instance != null) { GameController.instance.AddCoin(); GameController.instance.AddScore(200); }
        Destroy(target);
    }

    private void SpitOut()
    {
        anim.SetTrigger(animSpit);
        hasMouthFull = false;
        anim.SetBool(animHold, false);
        Vector3 spawnPos = mouthPoint.position + (Vector3.right * FacingDirection * 0.5f);
        if (heldShellType == ShellType.Green) {
            if (greenShellPrefab != null) {
                GameObject shellObj = Instantiate(greenShellPrefab, spawnPos, Quaternion.identity);
                ShellBase shellScript = shellObj.GetComponent<ShellBase>();
                Rigidbody2D shellRb = shellObj.GetComponent<Rigidbody2D>();
                if (shellScript != null && shellRb != null) { shellRb.simulated = true; shellScript.Kick(FacingDirection); }
            }
        } else if (heldShellType == ShellType.Red) {
            SpawnFireball(new Vector2(FacingDirection, 0.5f)); SpawnFireball(new Vector2(FacingDirection, 0f)); SpawnFireball(new Vector2(FacingDirection, -0.5f)); 
        }
    }

    private void SpawnFireball(Vector2 dir)
    {
        if (fireballPrefab != null) {
            GameObject fire = Instantiate(fireballPrefab, mouthPoint.position, Quaternion.identity);
            Vector3 originalScale = fireballPrefab.transform.localScale;
            float scaleX = (dir.x < 0) ? -Mathf.Abs(originalScale.x) : Mathf.Abs(originalScale.x);
            fire.transform.localScale = new Vector3(scaleX, Mathf.Abs(originalScale.y), originalScale.z);
            fire.transform.rotation = Quaternion.identity;
            if (fire.TryGetComponent<Fireball>(out Fireball fb)) { fb.SetDirection(dir); fire.transform.rotation = Quaternion.identity; }
        }
    }
    
    public bool IsMouthFull() => hasMouthFull;

    // --- MONTARIA ---
    public void MountPlayer(GameObject player)
    {
        if (player.TryGetComponent<Mario>(out Mario marioScript))
        {
            storedRiderScale = player.transform.localScale;
            storedRiderScale.x = Mathf.Abs(storedRiderScale.x); 
            rider = player;
            currentState = YoshiState.Ridden;
            riderAnim = player.GetComponent<MarioAnimator>();
            currentSpeed = 0f;
            rb.linearVelocity = Vector2.zero;
            marioScript.StartRiding(saddlePoint);
            rider.transform.localRotation = Quaternion.identity;
            float parentScaleX = Mathf.Abs(saddlePoint.lossyScale.x);
            float parentScaleY = Mathf.Abs(saddlePoint.lossyScale.y);
            if (parentScaleX == 0) parentScaleX = 1;
            if (parentScaleY == 0) parentScaleY = 1;
            Vector3 compensatedScale = new Vector3(storedRiderScale.x / parentScaleX, storedRiderScale.y / parentScaleY, storedRiderScale.z);
            rider.transform.localScale = compensatedScale; 
            gameObject.layer = defaultLayer;
        }
    }

    public void Dismount(bool enterPanic = true)
    {
        if (rider == null) return;
        Collider2D marioCol = rider.GetComponent<Collider2D>();
        Collider2D yoshiCol = GetComponent<Collider2D>();
        if (rider.TryGetComponent<Mario>(out Mario marioScript)) {
            rider.transform.SetParent(null);
            rider.transform.rotation = Quaternion.identity;
            int exitDirection = (int)Mathf.Sign(transform.localScale.x);
            SetFacingDirection(exitDirection);
            marioScript.SetFacingDirection(exitDirection);
            Vector3 finalScale = storedRiderScale; 
            finalScale.x = Mathf.Abs(finalScale.x); 
            rider.transform.localScale = finalScale;
            marioScript.StopRiding();
            if (enterPanic) marioScript.TriggerInvulnerability(1.0f);
        }
        if (marioCol != null && yoshiCol != null) {
            Physics2D.IgnoreCollision(marioCol, yoshiCol, true);
            StartCoroutine(ReenableCollision(marioCol, yoshiCol));
        }
        rider = null;
        riderAnim = null;
        currentState = enterPanic ? YoshiState.Panic : YoshiState.Idle;
        if (!enterPanic) { currentSpeed = 0f; rb.linearVelocity = Vector2.zero; }
        gameObject.layer = ghostLayer;
    }
    
    private IEnumerator ReenableCollision(Collider2D c1, Collider2D c2)
    {
        yield return new WaitForSeconds(1.0f);
        if (c1 != null && c2 != null) Physics2D.IgnoreCollision(c1, c2, false);
    }

    private void HandlePanicMovement()
    {
        rb.linearVelocity = new Vector2(FacingDirection * panicSpeed, rb.linearVelocity.y);
        Vector2 origin = (Vector2)transform.position + new Vector2(0, wallSensorHeight);
        RaycastHit2D wallHit = Physics2D.Raycast(origin, Vector2.right * FacingDirection, wallSensorDistance, wallLayer);
        if (wallHit.collider != null) FlipInstant(FacingDirection * -1);
    }

    public override void TakeDamage() 
    { 
        if (currentState == YoshiState.Ridden) Dismount(true); else currentState = YoshiState.Panic; 
    }

    public override void Die() { if (rider != null) rider.GetComponent<CharacterBase>().Die(); Destroy(gameObject); }

    // --- MÉTODOS PÚBLICOS ---
    public void SetVictoryWalk(bool walking)
    {
        victoryWalking = walking;
        if (walking) victoryPosing = false;
    }

    public void SetVictoryPose(bool posing = true)
    {
        victoryWalking = false;
        victoryPosing = posing;

        if (posing && anim != null)
        {
            anim.Play("idle"); 
        }
    }

    private void UpdateAnimations()
    {
        bool isMovingPhysics = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        bool isWalking = currentSpeed > 0.1f || isMovingPhysics;
        bool isJumping = !IsGrounded && rb.linearVelocity.y > 0.1f;
        bool isFalling = !IsGrounded && rb.linearVelocity.y <= 0.1f;
        bool isPanicking = (currentState == YoshiState.Panic);
        anim.SetBool(animWalk, isWalking);
        anim.SetBool(animJump, isJumping);
        anim.SetBool(animFall, isFalling);
        anim.SetBool(animPanic, isPanicking);
        anim.SetFloat(animRidden, currentState == YoshiState.Ridden ? 1.0f : 0.0f);
        anim.SetFloat("holding_blend", hasMouthFull ? 1.0f : 0.0f);
        
        if (riderAnim != null) 
        { 
            riderAnim.SetWalk(isWalking); 
            riderAnim.SetJump(isJumping); 
            riderAnim.SetFall(isFalling); 
        }
    }

    private void SpawnHitEffect(Vector3 position) { if (hitEffectPrefab != null) { GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity); Destroy(effect, effectDuration); } }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (!this.enabled) return;

        // 1. Zonas de Morte e Buracos
        if (other.gameObject.layer == deathZoneLayerID) 
        { 
            if (currentState == YoshiState.Ridden) Dismount(false); 
            Destroy(gameObject); 
            return; 
        }

        // 2. Moedas (Coins) - Yoshi pode pegar moeda mesmo de boca cheia? Geralmente sim.
        // Se suas frutas estão marcadas como Coin, mude a Tag delas para "Fruit" no Unity!
        if (other.CompareTag("Coin")) 
        { 
            if (other.TryGetComponent<Coin>(out Coin coin)) coin.CollectDirectly(this); 
            else Destroy(other.gameObject); 
            return; 
        }

        // 3. Montar no Yoshi
        if (currentState != YoshiState.Ridden && other.CompareTag("Player")) 
        {
            if (other.TryGetComponent<Mario>(out Mario marioScript)) 
            {
                // 🛑 REGRA DE OURO: Se o Mario estiver segurando item ou COM O PÉ NO CHÃO, ignora!
                // Isso mata completamente o bug do Unity reativar a colisão e montar ele parado.
                if (marioScript.isHoldingObject || marioScript.IsGrounded) return;

                Rigidbody2D marioRb = other.GetComponent<Rigidbody2D>();
                
                // Exige uma velocidade de queda real (-0.5f) para ignorar os "tremores" da física
                bool isFalling = marioRb != null && marioRb.linearVelocity.y < -0.5f;
                bool isAbove = other.transform.position.y > transform.position.y + 0.2f;
                
                if (isFalling && isAbove) 
                {
                    MountPlayer(other.gameObject);
                }
            }
            return;
        }

        // Se não tiver montado, para por aqui (não come nada sozinho)
        if (currentState != YoshiState.Ridden) return;

        // 4. Cascos (Shells)
        if (other.CompareTag("Shell")) 
        {
            if (other.TryGetComponent<ShellBase>(out ShellBase shell)) 
            {
                if (shell.IsIdle()) 
                { 
                    SpawnHitEffect(other.ClosestPoint(transform.position)); 
                    shell.DieByCrush(); 
                    BounceUp(0.6f); 
                    return; 
                }
            }
        }

        // 5. Frutas (Fruit) - AQUI ESTÁ A CORREÇÃO
        // Adicionamos parênteses extras e verificamos explicitamente a Tag E o Layer
        bool isFruit = other.gameObject.layer == fruitLayer || other.CompareTag("Fruit");
        
        if (isFruit) 
        { 
            // 🛑 SÓ COME SE A BOCA ESTIVER VAZIA
            if (!hasMouthFull) 
            {
                CollectFruit(other.gameObject); 
            }
        }
    }

    public void PlayBirthAnimation(float duration) 
    { 
        if (anim != null) anim.SetTrigger(animBirth); 
        StartCoroutine(BirthRoutine(duration)); 
    }

    private IEnumerator BirthRoutine(float duration) 
    { 
        if (rb != null) rb.simulated = false; 
        yield return new WaitForSeconds(duration); 

        if (rb != null) 
        { 
            rb.simulated = true; 
            rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse); 
        } 
    }
    private void HandleCombo() 
    { 
        if (GameController.instance != null) 
        { 
            if (yoshiComboIndex < comboPointSequence.Length) GameController.instance.AddScore(comboPointSequence[yoshiComboIndex]); 
            else GameController.instance.AddLife(); yoshiComboIndex++; 
        } 
    }

    private bool IsStomp(Collision2D collision) 
    { 
        ContactPoint2D contact = collision.contacts[0]; 
        if (contact.normal.y > 0.3f) return true; 
        if (rb.linearVelocity.y < -0.1f && transform.position.y > collision.transform.position.y + 0.1f) return true; return false; 
    }
}