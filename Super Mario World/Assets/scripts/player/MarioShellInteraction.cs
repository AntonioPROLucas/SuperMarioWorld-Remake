using UnityEngine;
using System.Collections;

public class MarioShellInteraction : MonoBehaviour
{
    [Header("Referências")]
    public Transform holdPoint;
    public KeyCode pickUpKey = KeyCode.LeftShift;
    public KeyCode upKey = KeyCode.UpArrow;

    [Header("Configurações")]
    public float throwUpForce = 10f;
    public float throwForwardForce = 8f;
    public float collisionReenableDelay = 0.2f;

    [Header("Interações")]
    public float shellStompBounceForce = 8f; // Força do pulo ao pisar no casco

    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 1f;
    private ShellBase heldShell;
    private bool isHoldingShell = false;
    private Animator marioAnimator;
    private Mario mario;
    private Rigidbody2D marioRb;

    void Start()
    {
        mario = GetComponent<Mario>();
        marioRb = GetComponent<Rigidbody2D>(); 
        marioAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleShellPickupAndThrow();
    }

    void HandleShellPickupAndThrow()
    {
        if (isHoldingShell && heldShell != null)
        {
            heldShell.transform.position = holdPoint.position;
            if (!Input.GetKey(pickUpKey))
            {
                Vector2 throwDir = Input.GetKey(upKey) ? Vector2.up : transform.right;
                ThrowShell(throwDir);
            }
        }
    }

   void OnTriggerEnter2D(Collider2D other)
    {
        // Tenta pegar o componente ShellBase do objeto tocado
        if (other.TryGetComponent<ShellBase>(out ShellBase shell))
        {
            // --- CASO 1: JÁ ESTOU SEGURANDO UM CASCO? ---
            if (isHoldingShell && heldShell != null)
            {
                // Se o casco tocado não for o mesmo que estou segurando...
                if (shell != heldShell)
                {
                    // ...e se o casco tocado estiver PARADO (Idle/Trigger)
                    // (Se estiver se movendo, o OnCollisionEnter2D cuida disso)
                    if (shell.IsIdle())
                    {
                        // 💥 DESTRUIÇÃO MÚTUA (Como no SMW)
                        heldShell.BreakAndDie(); // O da mão
                        shell.BreakAndDie();     // O do chão

                        // Reseta o estado do Mario
                        mario.isHoldingObject = false;
                        isHoldingShell = false;
                        heldShell = null;
                        
                        // (Opcional) Adicione som de impacto aqui
                    }
                }
                return; // Sai da função para não tentar pegar/chutar
            }

            // --- CASO 2: NÃO ESTOU SEGURANDO NADA (Comportamento Padrão) ---
            
            // Só interage se o casco estiver PARADO
            if (shell.IsIdle())
            {
                // 1. Verifica se é um Pisão (Stomp)
                bool isStomp = !mario.isGrounded && marioRb.linearVelocity.y <= 0.1f;

                if (isStomp)
                {
                    // --- AÇÃO DE "STOMP-KICK" ---
                    if (GameController.instance != null)
                    {
                        int stompIndex = GameController.instance.GetStompComboIndex();
                        int shellIndex = shell.GetShellComboIndex();

                        if (shellIndex > stompIndex)
                            GameController.instance.SetStompComboIndex(shellIndex);
                        
                        GameController.instance.IncrementStompCombo();
                    }

                    int kickDirection = mario.GetFacingDirection();
                    shell.EnterMoving(kickDirection);
                    
                    marioRb.linearVelocity = new Vector2(marioRb.linearVelocity.x, shellStompBounceForce);
                    
                    if (marioAnimator != null) marioAnimator.SetTrigger("stomping");
                    
                    Physics2D.IgnoreCollision(shell.GetComponent<Collider2D>(), GetComponent<Collider2D>(), true);
                    StartCoroutine(ReEnableCollisionAfterDelay(shell));
                    
                    if (hitEffectPrefab != null)
                    {
                        Vector2 effectPosition = other.ClosestPoint(mario.transform.position);
                        GameObject effect = Instantiate(hitEffectPrefab, effectPosition, Quaternion.identity);
                        Destroy(effect, hitEffectDuration); 
                    }
                }
                else 
                {
                    // Se NÃO foi um pisão, pega ou chuta
                    if (Input.GetKey(pickUpKey))
                    {
                        PickUpShell(shell);
                    }
                    else
                    {
                        KickShell(shell);
                    }
                }
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // --- CASO 1: COLISÃO COM OUTRO CASCO (Shell vs Shell) ---
        if (collision.gameObject.TryGetComponent<ShellBase>(out ShellBase otherShell))
        {
            // Se estamos segurando um casco...
            if (isHoldingShell && heldShell != null)
            {
                // ...e batemos em qualquer outro casco que não seja o nosso
                if (otherShell != heldShell)
                {
                    // ⬇️⬇️ NOVA LÓGICA DIRECIONAL ⬇️⬇️
                    
                    // 1. Calcula a direção do outro casco em relação ao Mario
                    float directionToTarget = otherShell.transform.position.x - transform.position.x;
                    int marioFacingDir = mario.GetFacingDirection();

                    // 2. Verifica se é uma colisão FRONTAL (Escudo Funciona)
                    if (Mathf.Sign(directionToTarget) == Mathf.Sign(marioFacingDir))
                    {
                        // 💥 DESTRUIÇÃO MÚTUA
                        heldShell.BreakAndDie(); // O casco da mão quebra
                        otherShell.BreakAndDie(); // O casco do chão quebra
                        
                        // Solta o objeto logicamente
                        mario.isHoldingObject = false;
                        isHoldingShell = false;
                        heldShell = null;
                        return; 
                    }
                    else
                    {
                        // 3. Colisão pelas COSTAS (Escudo Falha)
                        
                        // Se o outro casco estiver se movendo, ele machuca o Mario!
                        if (otherShell.IsMovingHorizontally())
                        {
                            mario.TakeDamage();
                            // O casco da mão continua segurado (ou é solto pela lógica de dano do Mario)
                        }
                        // Se o outro casco estiver parado e atrás, nada acontece (apenas colisão física)
                    }
                }
            }
            
            // Se NÃO estamos segurando (lógica antiga de dano/stomp no casco em movimento)
            else if (otherShell.IsMovingHorizontally()) 
            {
                 bool isStomp = false;
                 Vector2 stompContactPoint = Vector2.zero;

                 if (!mario.isGrounded && marioRb.linearVelocity.y <= 0.1f) 
                 {
                     foreach (ContactPoint2D contact in collision.contacts)
                     {
                         if (contact.normal.y > 0.2f) 
                         {
                             isStomp = true;
                             stompContactPoint = contact.point;
                             break; 
                         }
                     }
                 }

                 if (isStomp)
                 {
                     otherShell.StompedByPlayer(this.gameObject); 
                     if (marioAnimator != null) marioAnimator.SetTrigger("stomping");
                 }
                 else
                    {
                        // 1. Descobre onde o casco está em relação ao Mario
                        float directionToShell = otherShell.transform.position.x - transform.position.x;
                        
                        // 2. Descobre para onde o casco está indo
                        int shellVelocityDir = otherShell.GetDirection();

                        // 3. Verifica: O casco está se movendo PARA LONGE do Mario?
                        // (Ex: Casco à direita (+) se movendo para direita (+))
                        if (Mathf.Sign(directionToShell) == Mathf.Sign(shellVelocityDir))
                        {
                            // Situação Segura: Mario está "empurrando" ou perseguindo o casco.
                            // Apenas "chuta" novamente para garantir que continue movendo.
                            KickShell(otherShell);
                        }
                        else
                        {
                            // Situação de Perigo: O casco está vindo NA DIREÇÃO do Mario.
                            mario.TakeDamage();
                        }
                    }
            }
        }
        
        // --- CASO 2: COLISÃO COM INIMIGO (Escudo) ---
        else if (isHoldingShell && heldShell != null && collision.gameObject.CompareTag("Enemy"))
        {
            bool isStompPosition = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.2f) 
                {
                    isStompPosition = true;
                    break;
                }
            }

            if (!isStompPosition)
            {
                float directionToEnemy = collision.transform.position.x - transform.position.x;
                int marioFacingDir = mario.GetFacingDirection(); 

                if (Mathf.Sign(directionToEnemy) == Mathf.Sign(marioFacingDir))
                {
                    if (collision.gameObject.TryGetComponent<EnemyBase>(out EnemyBase enemy))
                    {
                        enemy.HitByShell(); 
                    }

                    if (GameController.instance != null) GameController.instance.AddScore(1000); 

                    heldShell.BreakAndDie(); 

                    mario.isHoldingObject = false;
                    isHoldingShell = false;
                    heldShell = null;
                }
            }
        }
    }


    void PickUpShell(ShellBase shell)
    {
        mario.isHoldingObject = true;
        isHoldingShell = true;
        heldShell = shell;
        shell.EnterHeld();
        
        Physics2D.IgnoreCollision(shell.GetComponent<Collider2D>(), GetComponent<Collider2D>(), true);
    }

    void ThrowShell(Vector2 direction)
    {
        if (heldShell == null) return;

        mario.isHoldingObject = false;

        ShellBase shellToThrow = heldShell;
        heldShell = null;
        isHoldingShell = false;
        
        shellToThrow.transform.SetParent(null);
        shellToThrow.ExitHeld(direction);
        
        StartCoroutine(ReEnableCollisionAfterDelay(shellToThrow));
    }

    void KickShell(ShellBase shell)
    {
        float kickDirection = Mathf.Sign(shell.transform.position.x - transform.position.x);
        if (kickDirection == 0)
        {
            kickDirection = Mathf.Sign(transform.localScale.x);
        }
        
        shell.EnterMoving((int)kickDirection);
        
        Physics2D.IgnoreCollision(shell.GetComponent<Collider2D>(), GetComponent<Collider2D>(), true);
        StartCoroutine(ReEnableCollisionAfterDelay(shell));
    }

    private IEnumerator ReEnableCollisionAfterDelay(ShellBase shell)
    {
        yield return new WaitForSeconds(collisionReenableDelay); 

        if (this != null && shell != null) 
        {
            Physics2D.IgnoreCollision(shell.GetComponent<Collider2D>(), GetComponent<Collider2D>(), false);
        }
    }
}