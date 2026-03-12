using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Fireball : MonoBehaviour
{
    [Header("Configurações")]
    public float speed = 10f;
    public float lifeTime = 3f; // Tempo até sumir sozinha
    public GameObject smokeEffectPrefab; // Efeito ao sumir/bater em parede

    //[Header("Sons")]
    //public AudioClip fireSound; // Som ao nascer
    //public AudioClip hitSound;  // Som ao transformar inimigo

    private Rigidbody2D rb;
    private Vector2 direction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Garante que não sofra gravidade para não cair
        rb.gravityScale = 0f; 
    }

    void Start()
    {
        // Toca o som de fogo ao nascer
        //if (fireSound != null && AudioManager.instance != null)
            //AudioManager.instance.PlaySound(fireSound);

        // Autodestruição após o tempo de vida
        Destroy(gameObject, lifeTime);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        // Define a velocidade baseada na direção
        rb.linearVelocity = direction * speed;
    }

    // =================================================================
    // 🎯 LÓGICA DE ATRAVESSAR E TRANSFORMAR
    // ==========================================
    // Usamos OnTrigger para ela passar por dentro do inimigo
    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. ATINGIU UM INIMIGO
        if (other.CompareTag("Enemy"))
        {
            // Tenta pegar o script do inimigo (usando InParent para garantir)
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

            if (enemy != null)
            {
                // 🔥 A NOVA LÓGICA:
                // 1. Manda o inimigo virar moeda
                enemy.TurnIntoCoinByFire(); 

                // 2. Toca som de acerto (se tiver)
                //if (hitSound != null && AudioManager.instance != null)
                    //AudioManager.instance.PlaySound(hitSound);

                // 3. 🛑 NÃO DESTRUÍMOS A BOLA DE FOGO!
                // Ela continua viagem com a mesma velocidade.
                
                return; // Sai para não checar parede
            }
        }   
    }

    private void Explode()
    {
        // Cria efeito de fumaça (se tiver)
        if (smokeEffectPrefab != null)
        {
            Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destrói a bola de fogo
        Destroy(gameObject);
    }
}