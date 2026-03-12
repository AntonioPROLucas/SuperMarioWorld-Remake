using UnityEngine;

public class OneUpMushroom : MonoBehaviour
{
    public float speed = 3f;
    public int direction = 1;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Movimento simples (igual Goomba/Cogumelo Vermelho)
        rb.velocity = new Vector2(speed * direction, rb.velocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Se bater na parede, vira
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || 
            collision.gameObject.layer == LayerMask.NameToLayer("Wall")) 
        {
            foreach(ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    direction *= -1;
                    return;
                }
            }
        }

        // Se bater no Mario, dá vida
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameController.instance)
            {
                // ✨ A MÁGICA AQUI: Passamos a posição do cogumelo para spawnar o texto!
                GameController.instance.AddLife(transform.position);
                // Tocar som de 1-UP aqui
            }
            Destroy(gameObject);
        }
    }
}