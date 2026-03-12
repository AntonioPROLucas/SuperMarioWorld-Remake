using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class JumpingPiranha : EnemyBase
{
    [Header("Configurações do Salto")]
    public float jumpForce = 12f;      
    public float gravityGoingUp = 2f;  
    public float gravityFalling = 0.5f;
    public float waitTimeInside = 2f;  
    
    [Header("Regras de Spawn")]
    public float safeZoneRadius = 3.0f; 

    private Rigidbody2D rbPlant;
    private Vector3 startPos;
    private bool isJumping = false;

    // 🔧 CORREÇÃO 1: Removemos o 'override' e o 'base.Awake()'
    // Como EnemyBase não tem um Awake virtual, usamos o Awake padrão da Unity.
    protected void Awake()
    {
        // Se EnemyBase tiver inicializações importantes, copie elas aqui ou mude EnemyBase para 'virtual'.
        // Geralmente inicializamos o RB aqui mesmo:
        rbPlant = GetComponent<Rigidbody2D>();
        
        // Configuração inicial da física
        startPos = transform.position;
        rbPlant.gravityScale = 0; 
        rbPlant.freezeRotation = true; 
        
        // Garante Trigger
        if(GetComponent<Collider2D>()) GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        StartCoroutine(JumpRoutine());
    }

    private void FixedUpdate()
    {
        if (isJumping)
        {
            if (rbPlant.velocity.y > 0) rbPlant.gravityScale = gravityGoingUp;
            else rbPlant.gravityScale = gravityFalling;
        }
    }

    IEnumerator JumpRoutine()
    {
        while (true)
        {
            // 1. Espera dentro
            rbPlant.velocity = Vector2.zero;
            rbPlant.gravityScale = 0;
            transform.position = startPos; 
            isJumping = false;

            yield return new WaitForSeconds(waitTimeInside);

            // 2. Safe Zone
            if (Mario.instance != null)
            {
                float dist = Vector2.Distance(transform.position, Mario.instance.transform.position);
                while (dist < safeZoneRadius)
                {
                    dist = Vector2.Distance(transform.position, Mario.instance.transform.position);
                    yield return new WaitForSeconds(0.5f); 
                }
            }

            // 3. PULO
            isJumping = true;
            rbPlant.velocity = Vector2.up * jumpForce;

            yield return new WaitForSeconds(0.5f); 
            
            while (transform.position.y > startPos.y)
            {
                yield return null; 
            }
        }
    }
    
    // Dano ao contato (Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Mario mario = other.GetComponent<Mario>();
            if (mario != null && !mario.IsInvincible)
            {
                mario.TakeDamage();
            }
        }
    }

    // 🔧 CORREÇÃO 2: Implementando OnStomped (Obrigatório pelo EnemyBase)
    public override void OnStomped(GameObject player)
    {
        // Piranha Plant tem espinhos! Se o Mario pular nela, ELE toma dano.
        Mario mario = player.GetComponent<Mario>();
        if (mario != null)
        {
            // Empurra o Mario para cima levemente (feedback de erro)
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if(playerRb) playerRb.velocity = new Vector2(playerRb.velocity.x, 5f);
            
            mario.TakeDamage();
        }
    }

    // 🔧 CORREÇÃO 3: Implementando HitByShell (Obrigatório pelo EnemyBase)
    public override void HitByShell()
    {
        // Se levar cascopada, morre na hora!
        // Aqui você pode instanciar uma partícula de fumaça ou som
        Destroy(gameObject);
    }
}