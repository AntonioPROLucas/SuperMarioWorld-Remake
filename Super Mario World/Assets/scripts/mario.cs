using System.Collections;
using UnityEngine;

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
    public float acceleration = 1f;
    public float deceleration = 4f;

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

    private Rigidbody2D rig;
    private BoxCollider2D boxCollider;
    private MarioAnimator marioAnim;
    private float deltaY;

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

    void Update()
    {
        HandleMove();
        HandleJump();
        HandleLow();
    }

    #region Movimento Horizontal
    void HandleMove()
    {
        if (isLowered)
        {
            marioAnim.SetWalk(false);
            return;
        }

        float h = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(h, 0f, 0f);
        transform.position += movement * Time.deltaTime * Speed;
        if (h > 0)
        {
            marioAnim.SetWalk(true);
            transform.eulerAngles = Vector3.zero;
        }
        else if (h < 0)
        {
            marioAnim.SetWalk(true);
            transform.eulerAngles = new Vector3(0, 180f, 0);
        }
        if(h != 0)
        {
            if (Input.GetKey(runKey))
            {
                // Aumenta gradualmente
                Speed = Mathf.MoveTowards(Speed, maxSpeed, acceleration * Time.deltaTime);
                JumpForce = Mathf.MoveTowards(JumpForce, maxJumpForce, acceleration * Time.deltaTime);
            }
            else{
                Speed = Mathf.MoveTowards(Speed, baseSpeed, deceleration * Time.deltaTime);
                JumpForce = Mathf.MoveTowards(JumpForce, baseJumpForce, deceleration * Time.deltaTime);
                marioAnim.SetRun(false);
            }

        // Ativa animação de corrida se velocidade for alta o suficiente
        bool isRunning = Speed > baseSpeed + 1.5f; // ← ajuste o threshold se quiser
        marioAnim.SetRun(isRunning);
        }

        else
        {
            // Diminui gradualmente até o valor padrão
            Speed = Mathf.MoveTowards(Speed, baseSpeed, deceleration * Time.deltaTime);
            JumpForce = Mathf.MoveTowards(JumpForce, baseJumpForce, deceleration * Time.deltaTime);
            marioAnim.SetWalk(false);
            marioAnim.SetRun(false);
        }
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
        if (collision.gameObject.layer == 6)
        {
            isGrounded = true;
            marioAnim.SetJump(false);
            marioAnim.SetFall(false);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 6)
        {
            isGrounded = false;

            var state = marioAnim.GetCurrentState();
            float delay = state.IsName("jumping") ? 0.4f : 0.1f;
            StartCoroutine(SetFallingAfterDelay(delay));
        }
    }
    #endregion

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
}