using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MarioAnimator))]
[RequireComponent(typeof(MarioMovement))]
public class Mario : CharacterBase
{
    public static Mario instance;

    // ==========================================
    // ⚙️ CONFIGURAÇÕES
    // ==========================================
    [Header("Estados do Mario")]
    public int powerUpState = 1; 
    public bool isHoldingObject = false;

    [Header("Colisores (Auto-Mapeados)")]
    public Collider2D bodyCollider; 
    public Collider2D feetCollider; 

    // 🏆 VARIÁVEIS DE VITÓRIA
    private bool isVictorySequence = false;
    private bool isVictoryLanded = false;
    private float victoryTimer = 0f;
    private Yoshi currentYoshi;

    [Header("Sistema de Pegar")]
    public Transform holdPoint;
    public float grabRange = 0.5f;
    public LayerMask grabLayer;
    private GameObject heldObject;

    [Header("Efeitos")]
    public float invincibleFlashInterval = 0.05f; 
    
    private SpriteRenderer[] allRenderers; 

    // 🔨 VARIÁVEIS DA MARRETA DO PISCAR
    private bool isFlashing = false;
    private float currentFlashAlpha = 1f;

    // Componentes Internos
    private MarioAnimator marioAnim;
    private MarioMovement movement; 
    
    private Vector3 initialScale;
    private List<Collider2D> ignoredColliders = new List<Collider2D>();

    protected override void Awake()
    {
        if (GameController.hasCheckpoint) transform.position = GameController.checkpointPosition;
        
        base.Awake(); 
        instance = this;

        marioAnim = GetComponent<MarioAnimator>();
        movement = GetComponent<MarioMovement>();
        
        Collider2D[] allCols = GetComponents<Collider2D>();
        foreach(Collider2D c in allCols)
        {
            if (!c.isTrigger && bodyCollider == null) bodyCollider = c;
            if (c.isTrigger && feetCollider == null) feetCollider = c;
        }
        
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        
        initialScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void Update()
    {
        if (isVictorySequence) { HandleVictoryBehavior(); return; }
        if (marioAnim.IsRiding()) return;
        HandleGameplayInputs();
    }

    void LateUpdate()
    {
        if (isFlashing && allRenderers != null)
        {
            foreach(SpriteRenderer sr in allRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, currentFlashAlpha);
                }
            }
        }
    }

    public override void OnLevelComplete()
    {
        base.OnLevelComplete(); 
        Time.timeScale = 1.0f; 

        isVictorySequence = true;
        isVictoryLanded = false; 
        IsInvincible = true; 
        victoryTimer = 0f;

        if (movement != null) movement.enabled = false;
        
        currentYoshi = null;

        if (marioAnim.IsRiding())
        {
            currentYoshi = GetComponentInParent<Yoshi>();
            if (currentYoshi != null) currentYoshi.OnLevelComplete(); 
        }

        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
    }

    private void HandleVictoryBehavior()
    {
        if (!isVictoryLanded)
        {
            bool isGroundedNow = (currentYoshi != null) ? currentYoshi.IsGrounded : IsGrounded;
            float fallSpeed = (currentYoshi != null && currentYoshi.GetComponent<Rigidbody2D>() != null) 
                              ? currentYoshi.GetComponent<Rigidbody2D>().linearVelocity.y 
                              : (rb != null ? rb.linearVelocity.y : 0);

            if (!isGroundedNow || Mathf.Abs(fallSpeed) > 0.1f)
            {
                if (currentYoshi != null) { marioAnim.SetFall(true); } 
                else { marioAnim.SetFall(true); marioAnim.SetJump(false); marioAnim.SetWalk(false); }
                return; 
            }

            isVictoryLanded = true; 

            if (rb != null && currentYoshi == null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector2.zero;
            }
        }

        IsGrounded = true; 
        victoryTimer += Time.deltaTime;
        Transform targetTransform = (currentYoshi != null) ? currentYoshi.transform : transform;

        if (victoryTimer < 4.0f) 
        {
            float victoryWalkSpeed = 3.5f; 
            float step = victoryWalkSpeed * Time.deltaTime;
            targetTransform.Translate(Vector3.right * step, Space.World);
            
            if (currentYoshi != null)
            {
                currentYoshi.SetVictoryWalk(true);
                marioAnim.SetRiding(true); marioAnim.SetFall(false); marioAnim.SetJump(false);
            }
            else
            {
                marioAnim.SetFall(false); marioAnim.SetJump(false); marioAnim.SetWalk(true); marioAnim.SetRun(false); 
            }
        }
        else if (victoryTimer < 6.0f) 
        {
            if (currentYoshi != null) { currentYoshi.SetVictoryWalk(false); currentYoshi.SetVictoryPose(true); }
            marioAnim.SetWalk(false); marioAnim.SetVictory(); 
        }
        else 
        {
            if (LevelTransition.instance != null && !LevelTransition.instance.IsTransitioning()) 
                LevelTransition.instance.PlayIrisOut(targetTransform);
        }
    }

    private void HandleGameplayInputs()
    {
        if (isHoldingObject && heldObject == null) { isHoldingObject = false; marioAnim.SetHolding(0f); }
        if ((Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.LeftShift)) && !isHoldingObject) CheckForGrab();
        if ((Input.GetKeyUp(KeyCode.X) || Input.GetKeyUp(KeyCode.LeftShift)) && isHoldingObject) ThrowObject();
        
        if (!IsGrounded && rb.linearVelocity.y < -0.1f) { marioAnim.SetFall(true); marioAnim.SetJump(false); }
        else if (IsGrounded) { marioAnim.SetFall(false); }

        marioAnim.SetPowerUpState(powerUpState);
        marioAnim.SetHolding(isHoldingObject ? 1.0f : 0.0f);

        if (IsGrounded && Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f)
        {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0f, 15f * Time.deltaTime), rb.linearVelocity.y);
        }
    }
    
    public new int GetFacingDirection() { return Mathf.Approximately(transform.eulerAngles.y, 180f) ? -1 : 1; }
    
    private void CheckForGrab()
    {
        int facingDir = GetFacingDirection();
        
        // 1. Abaixamos o radar no eixo Y (-0.8f) para garantir que ele varra o chão!
        Vector2 checkPos = (Vector2)transform.position + new Vector2(facingDir * 0.6f, -0.8f);
        
        // 2. Trocamos para OverlapCircleAll! Ele agora pega TODOS os itens na área e filtra o correto.
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, grabRange, grabLayer);
        
        foreach (Collider2D hit in hits)
        {
            // Procura quem é o HoldableObject no meio do que ele encontrou
            if (hit.TryGetComponent<HoldableObject>(out HoldableObject item)) 
            {
                if (item.CanBePickedUp()) 
                {
                    Physics2D.IgnoreCollision(bodyCollider != null ? bodyCollider : GetComponent<Collider2D>(), hit, true);
                    item.GetPickedUp(this); 
                    StartHolding(hit.gameObject); 
                    
                    return; // Achou, pegou, cancela a busca!
                }
            }
        }
    }

    private void ThrowObject()
    {
        if (heldObject == null) { isHoldingObject = false; return; }
        HoldableObject item = heldObject.GetComponent<HoldableObject>();
        if (heldObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D itemRb)) itemRb.simulated = true;
        isHoldingObject = false; heldObject = null;
        int facingDir = GetFacingDirection();
        Vector2 throwDir = new Vector2(facingDir, 0);
        if (Input.GetKey(KeyCode.UpArrow)) { throwDir = Vector2.up; if(marioAnim) marioAnim.SetThrowUpTrigger(); }
        if (item != null) item.Throw(throwDir, 12f, bodyCollider != null ? bodyCollider : GetComponent<Collider2D>());
    }

    public void StartHolding(GameObject item)
    {
        isHoldingObject = true; heldObject = item;
        if (item.TryGetComponent<Rigidbody2D>(out Rigidbody2D itemRb)) { itemRb.simulated = false; itemRb.linearVelocity = Vector2.zero; }
        item.transform.SetParent(holdPoint); item.transform.localPosition = Vector3.zero; item.transform.localRotation = Quaternion.identity; 
        if (item.transform.localScale.x < 0) { Vector3 s = item.transform.localScale; item.transform.localScale = new Vector3(Mathf.Abs(s.x), s.y, s.z); }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isHoldingObject && collision.gameObject == heldObject) return;
        
        if (((1 << collision.gameObject.layer) & groundLayer) != 0) 
        {
            if (rb.linearVelocity.y > 0.1f) return;

            foreach(ContactPoint2D contact in collision.contacts) 
            {
                if(contact.normal.y > 0.5f) 
                { 
                    IsGrounded = true; 
                    marioAnim.SetJump(false); 
                    marioAnim.SetFall(false); 
                    if (GameController.instance) GameController.instance.ResetCombo(); 
                    break; 
                }
            }
        } 
        else if (collision.gameObject.TryGetComponent<EnemyBase>(out EnemyBase hitEnemy)) 
        {
            if (!IsGrounded && transform.position.y > hitEnemy.transform.position.y + 0.15f)
            {
                hitEnemy.OnStomped(this.gameObject); 
                PerformStompJump(); 
                if (GameController.instance) GameController.instance.RegisterComboHit(); 
                return;
            }

            if (!IsInvincible) TakeDamage();
        } 
        else if (collision.gameObject.TryGetComponent<ShellBase>(out ShellBase shell)) 
        {
            if (!IsGrounded && transform.position.y > shell.transform.position.y + 0.15f)
            {
                shell.StompedByPlayer(this.gameObject); 
                PerformStompJump(); 
                if (GameController.instance) GameController.instance.RegisterComboHit();
                return; 
            }

            if (shell.IsIdle()) {
                if ((Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.LeftShift)) && shell.CanBePickedUp() && !isHoldingObject) {
                    Physics2D.IgnoreCollision(bodyCollider != null ? bodyCollider : GetComponent<Collider2D>(), collision.collider, true); shell.GetPickedUp(this); StartHolding(shell.gameObject); return; 
                }
                float relativeX = shell.transform.position.x - transform.position.x; int kickDir = (relativeX >= 0) ? 1 : -1; shell.Kick(kickDir);
                Physics2D.IgnoreCollision(bodyCollider != null ? bodyCollider : GetComponent<Collider2D>(), collision.collider, true); StartCoroutine(ReEnableCollision(collision.collider));
            } else if (shell.IsMovingHorizontally()) {
                float dirToMario = transform.position.x - shell.transform.position.x; if (Mathf.Sign(shell.GetDirection()) == Mathf.Sign(dirToMario)) if (!IsInvincible) TakeDamage();
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (rb.linearVelocity.y > 0.1f) return;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) { IsGrounded = true; if (marioAnim != null) { marioAnim.SetFall(false); marioAnim.SetJump(false); } return; }
            }
        }
    }

    // ==========================================
    // 🥾 SISTEMA DE STOMP BLINDADO E DEATH ZONE
    // ==========================================
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        TryStomp(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryStomp(other); 
    }

    private void TryStomp(Collider2D other)
    {
        if (rb.linearVelocity.y > -0.1f) return;

        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            if (transform.position.y > other.transform.position.y)
            {
                if (other.TryGetComponent<EnemyBase>(out EnemyBase hitEnemy))
                {
                    hitEnemy.OnStomped(this.gameObject); 
                    PerformStompJump(); 
                    if (GameController.instance) GameController.instance.RegisterComboHit(); 
                }
                else if (other.TryGetComponent<ShellBase>(out ShellBase shell))
                {
                    shell.StompedByPlayer(this.gameObject); 
                    PerformStompJump(); 
                    if (GameController.instance) GameController.instance.RegisterComboHit();
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision) { if (((1 << collision.gameObject.layer) & groundLayer) != 0) IsGrounded = false; }
    
    private void PerformStompJump() { rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); BounceUp(1f); if (marioAnim != null) { marioAnim.SetTrigger("stomping"); marioAnim.SetJump(true); } }
    
    public void ChangePowerUpState(int s) { powerUpState = s; }
    
    public override void TakeDamage() 
    { 
        if (!IsInvincible) 
        { 
            if (GameController.instance) GameController.instance.PlayerDied(); 
            gameObject.SetActive(false); 
        } 
    }
    
    public void TriggerInvulnerability(float d) { if(!IsInvincible) StartCoroutine(InvulnerabilityRoutine(d)); }
    
    private IEnumerator InvulnerabilityRoutine(float duration) 
    { 
        IsInvincible = true; 
        isFlashing = true; 

        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in allEnemies)
        {
            Collider2D[] enemyCols = enemy.GetComponents<Collider2D>();
            foreach(Collider2D eCol in enemyCols)
            {
                if (bodyCollider != null && eCol != null)
                {
                    ignoredColliders.Add(eCol);
                    Physics2D.IgnoreCollision(bodyCollider, eCol, true);
                }
            }
        }

        float endTime = Time.time + duration;
        
        while (Time.time < endTime)
        {
            currentFlashAlpha = 0.3f;
            yield return new WaitForSeconds(invincibleFlashInterval);
            
            currentFlashAlpha = 1f;
            yield return new WaitForSeconds(invincibleFlashInterval);
        }
        
        currentFlashAlpha = 1f;
        isFlashing = false; 
        
        if (allRenderers != null)
        {
            foreach(SpriteRenderer sr in allRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, 1f);
                }
            }
        }
        
        foreach (Collider2D col in ignoredColliders)
        {
            if (col != null && bodyCollider != null) Physics2D.IgnoreCollision(bodyCollider, col, false);
        }
        ignoredColliders.Clear();
        
        IsInvincible = false; 
    }

    // --- MONTARIA ---
    public void StartRiding(Transform t) 
    { 
        rb.simulated = false; 
        if (bodyCollider != null) bodyCollider.enabled = false; 
        if (feetCollider != null) feetCollider.enabled = false; 
        
        transform.SetParent(t); 
        transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity; transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1, 1); 
        movement.enabled = false; marioAnim.SetRiding(true); 
    }
    
    public void StopRiding(bool isDamage = false) 
    { 
        this.enabled = true; 
        
        Rigidbody2D parentRb = transform.parent != null ? transform.parent.GetComponent<Rigidbody2D>() : null;
        bool yoshiWasJumping = parentRb != null && parentRb.linearVelocity.y > 0.1f;

        transform.SetParent(null); 
        float d = Mathf.Sign(transform.localScale.x); 
        transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * d, initialScale.y, initialScale.z); 
        
        rb.simulated = true; 
        if (bodyCollider != null) bodyCollider.enabled = true; 
        if (feetCollider != null) feetCollider.enabled = true; 
        
        marioAnim.SetRiding(false); 
        marioAnim.SetJump(true); 
        IsGrounded = false; 

        if (isDamage)
        {
            movement.enabled = false; 
            rb.linearVelocity = new Vector2(-d * 5f, 5f); 
            
            TriggerInvulnerability(2f);
            StartCoroutine(DamageKnockbackRoutine());
        }
        else
        {
            movement.enabled = true; 
            
            float dismountForce = yoshiWasJumping ? jumpForce * 1.2f : jumpForce * 0.8f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, dismountForce); 
        }
    }

    private IEnumerator DamageKnockbackRoutine()
    {
        yield return new WaitForSeconds(0.35f);
        if (movement != null) movement.enabled = true;
    }
    
    private IEnumerator ReEnableCollision(Collider2D c) { yield return new WaitForSeconds(0.2f); if(c!=null && bodyCollider!=null) Physics2D.IgnoreCollision(bodyCollider, c, false); }
}