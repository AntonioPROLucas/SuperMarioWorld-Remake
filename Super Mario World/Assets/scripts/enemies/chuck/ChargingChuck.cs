using UnityEngine;
using System.Collections;

public class CharginChuck : EnemyBase
{
    [Header("Status do Chuck")]
    public int hp = 5;
    public float chargeSpeed = 6f;
    public float detectionRange = 8f; // Distância de visão para ativar
    public float brakeDuration = 0.5f; // Tempo que ele fica parado olhando antes de virar
    public float hurtDuration = 0.5f; // Tempo atordoado após o stomp

    [Header("Nomes das Animações exatas no Animator")]
    public string animIdle = "idle";
    public string animRunLeft = "chargingLeft";
    public string animRunRight = "chargingRight";
    public string animHurt = "hit";

    [Header("Efeitos e Combos")]
    public GameObject hitEffectPrefab;

    // Máquina de Estados
    private enum ChuckState { Idle, Charging, Braking, Hurt }
    private ChuckState currentState = ChuckState.Idle;

    private int facingDir = -1; // -1 Esquerda, 1 Direita
    private Transform playerTarget;
    private Rigidbody2D rbChuck;
    private Animator animChuck;

    private void Awake()
    {
        // Pega as referências
        rbChuck = GetComponent<Rigidbody2D>();
        animChuck = GetComponent<Animator>();
        // Removemos o 'base.Awake();' porque ele não existe na classe pai
    }

    protected override void Start()
    {
        base.Start();
        // Acha o Mario na cena (Supondo que você tenha Mario.instance, senão usa FindObject)
        if (Mario.instance != null)
            playerTarget = Mario.instance.transform;
    }

    private void Update()
    {
        if (!isActivated || playerTarget == null) return;

        switch (currentState)
        {
            case ChuckState.Idle:
                // Toca a animação de procurar (Idle)
                animChuck.Play(animIdle);
                CheckForPlayer();
                break;

            case ChuckState.Charging:
                HandleCharge();
                break;

            case ChuckState.Braking:
            case ChuckState.Hurt:
                // Se está freando ou machucado, zera a velocidade horizontal
                rbChuck.linearVelocity = new Vector2(0, rbChuck.linearVelocity.y);
                break;
        }
    }

    // ==========================================
    // 👁️ VISÃO E PERSEGUIÇÃO
    // ==========================================
    private void CheckForPlayer()
    {
        float dist = Vector2.Distance(transform.position, playerTarget.position);
        
        // Se o Mario entrou no campo de visão, começa a correr!
        if (dist <= detectionRange)
        {
            StartCharge();
        }
    }

    private void StartCharge()
    {
        currentState = ChuckState.Charging;
        facingDir = playerTarget.position.x > transform.position.x ? 1 : -1;
        
        // 🔄 AJUSTE DE VECTOR: 
        // Muda a escala apenas na hora de correr. 
        // (NOTA: Se ele ficar invertido, troque o -Mathf.Abs por Mathf.Abs e vice-versa)
        Vector3 scale = transform.localScale;
        scale.x = facingDir == 1 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x); 
        transform.localScale = scale;

        if (facingDir == 1) animChuck.Play(animRunRight);
        else animChuck.Play(animRunLeft);
    }

    private void HandleCharge()
    {
        // Corre na direção definida
        rbChuck.linearVelocity = new Vector2(facingDir * chargeSpeed, rbChuck.linearVelocity.y);

        // 🧠 IA DE ESQUIVA (FREIO):
        // Verifica de que lado o Mario está AGORA
        int currentDirToPlayer = playerTarget.position.x > transform.position.x ? 1 : -1;
        
        // Se a direção do Mario for diferente de onde o Chuck está correndo (Mario pulou por cima)
        if (currentDirToPlayer != facingDir)
        {
            StartCoroutine(BrakeRoutine());
        }
    }

    private IEnumerator BrakeRoutine()
    {
        currentState = ChuckState.Braking;
        animChuck.Play(animIdle); // Volta a olhar para os lados confuso
        
        yield return new WaitForSeconds(brakeDuration);
        
        // Recalcula e volta a correr atrás do Mario
        StartCharge(); 
    }

    // ==========================================
    // 💥 DANO E COLISÃO
    // ==========================================
    public override void OnStomped(GameObject stomper)
    {
        // 1. Instancia o efeito de impacto (onomatopeia) a CADA pulo!
        if (hitEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + (Vector3.up * 0.5f);
            Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
        }
        
        if (currentState == ChuckState.Hurt) return; // Evita hits duplos rápidos

        // 1. Lógica de Dano Diferenciado (Mario a pé vs Yoshi)
        int damageTaken = 1; // Pulo normal tira 1 de HP

        // Verifica se quem pulou foi o Yoshi (ou se o Mario está montado)
        if (stomper.GetComponent<Yoshi>() != null) 
        {
            damageTaken = 2; // Yoshi dá hit duplo (3 pulos matam os 5 HP)
        }

        hp -= damageTaken;
        
        // Faz o Mario/Yoshi quicar para cima
        ApplyStompBounce(stomper);

        // 2. Verifica se morreu
        if (hp <= 0)
        {
            DieByImpact(); // Vira de ponta cabeça e cai ou usa o DieByCrush
        }
        else
        {
            StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        currentState = ChuckState.Hurt;
        animChuck.Play(animHurt); // Toca a animação dele encolhido/piscando
        
        yield return new WaitForSeconds(hurtDuration);
        
        // Após o atordoamento, volta furioso na direção do jogador
        StartCharge(); 
    }

    public override void HitByShell()
    {
        // Casco mata o Chuck na hora!
        DieByImpact(); 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Colisão com o Jogador
        if (collision.gameObject.CompareTag("Player"))
        {
            // Trava de dano: Se o Chuck está tomando dano (Hurt), ele não machuca o Mario
            if (currentState == ChuckState.Hurt) return;

            ContactPoint2D contact = collision.contacts[0];
            
            // Se o Mário bateu de lado (não veio de cima) e o Chuck está correndo
            if (contact.normal.y > -0.5f && currentState == ChuckState.Charging)
            {
                /* * Quando tiver as animações do Mario pequeno, descomente a linha abaixo:
                 * OnLateralHit(collision.gameObject);
                 */
            }
        }
        // 2. 🐢 Colisão direta com o Casco (NOVO)
        else if (collision.gameObject.CompareTag("Shell"))
        {
            if (collision.gameObject.TryGetComponent<ShellBase>(out ShellBase shell))
            {
                // Se o casco NÃO está parado (ou seja, foi arremessado voando ou chutado no chão)
                if (!shell.IsIdle())
                {
                    HitByShell(); // Chuck executa sua morte caindo da tela
                    
                    // Opcional mas recomendado: Faz o casco quebrar ao bater no capacete duro do Chuck!
                    shell.DieByImpact(); 
                }
            }
        }
    }

    // ==========================================
    // 💀 SISTEMA DE MORTE UNIFICADO
    // ==========================================
    private void FallOffScreen()
    {
        isActivated = false; // Desliga a IA
        currentState = ChuckState.Hurt; // Trava o estado para não se mexer nem dar dano
        
        // Toca a animação de dano enquanto cai
        if(animChuck != null) animChuck.Play(animHurt);

        // Desliga o colisor para ele atravessar o chão e as paredes
        if (TryGetComponent<Collider2D>(out Collider2D col)) col.enabled = false;

        // Dá um "pulinho" para trás e deixa a gravidade puxar para o abismo
        rbChuck.linearVelocity = new Vector2(-facingDir * 3f, 7f);

        // Destrói o objeto após 3 segundos (tempo suficiente para sumir da tela)
        Destroy(gameObject, 3f);
    }

    // Sobrescreve as 3 mortes base para usarem o MESMO comportamento de queda:
    public override void DieByImpact() 
    { 
        FallOffScreen(); 
    }
    
    public override void DieByCrush() 
    { 
        FallOffScreen(); 
    }

    public override void TurnIntoCoinByFire() 
    { 
        FallOffScreen(); 
    }
}