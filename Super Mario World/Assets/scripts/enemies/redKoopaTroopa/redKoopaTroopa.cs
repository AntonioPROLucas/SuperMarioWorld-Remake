using UnityEngine;

public class redKoopaTroopa : MonoBehaviour
{
    public float speed = 2f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.1f;

    private Rigidbody2D rb;
    private Animator animator;
    private bool facingRight = false;
    private bool isTurning = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (isTurning) return; // não anda enquanto está virando

        bool groundAhead = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        bool wallAhead = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        if (!groundAhead || wallAhead)
        {
            StartCoroutine(TurnRoutine());
        }

        float moveDirection = facingRight ? 1f : -1f;
        rb.velocity = new Vector2(moveDirection * speed, rb.velocity.y);

        if (isTurning)
        {
            Debug.Log("Koopa está virando, movimento bloqueado");
            return;
        }

    }

    System.Collections.IEnumerator TurnRoutine()
    {
        Debug.Log("Iniciando rotina de virada...");
        isTurning = true;
        rb.velocity = Vector2.zero; // para completamente

        if (animator != null)
        {
            animator.SetBool("isTurning", true);
            Debug.Log("Trigger Turn enviado");
        }
        else
        {
            Debug.LogWarning("Animator não encontrado!");
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log("Finalizando rotina de virada...");
        Flip();
        animator.SetBool("isTurning", false);
        isTurning = false;
    }


    void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    #region Visualização de Debug (Gizmos)
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
        }
    }
    #endregion
}