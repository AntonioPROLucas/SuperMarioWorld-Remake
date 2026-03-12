using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Configurações Base do Inimigo")]
    public float stompBounceForce = 10f;
    
    [Header("Efeitos Base")]
    public GameObject smokeEffectPrefab; // ☁️ Fumaça (Adicionado aqui para todos usarem)

    [Header("Fogo e Recompensas")]
    public GameObject coinFromFirePrefab;

    [Header("Otimização de Ativação")]
    public bool activateOnVisible = true; // Se marcado, espera a câmera ver para andar
    protected bool isActivated = false;   // Controle interno

    public abstract void OnStomped(GameObject stomper);
    public abstract void HitByShell();

    protected virtual void OnBecameVisible()
    {
        if (activateOnVisible)
        {
            isActivated = true;
            // Se tiver Rigidbody, acorda ele
            if (TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.simulated = true; // Garante que a física volte a rodar
            }
        }
    }

    protected virtual void Start()
    {
        // Se a opção estiver ligada, começa desativado
        if (activateOnVisible)
        {
            isActivated = false;
            
        }
        else
        {
            isActivated = true; // Se não usar otimização, já nasce ativo
        }
    }

    public virtual void OnLateralHit(GameObject player)
    {
        if (player.TryGetComponent<Mario>(out Mario mario))
        {
            mario.TakeDamage();
        }
    }

    // 💀 CENÁRIO A: MORTE FÍSICA (Impacto)
    // O Koopa vai sobrescrever isso para fazer a lógica de virar e cair.
    public virtual void DieByImpact()
    {
        Destroy(gameObject);
    }

    // ☁️ CENÁRIO B: MORTE POR ESMAGAMENTO (Yoshi/Spin)
    // O padrão é soltar fumaça e sumir.
    public virtual void DieByCrush()
    {
        // Solta a Fumaça "Puff"
        if (smokeEffectPrefab != null)
        {
            Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    protected void ApplyStompBounce(GameObject stomper)
    {
        if (stomper.TryGetComponent<Rigidbody2D>(out Rigidbody2D stomperRb))
        {
            stomperRb.linearVelocity = new Vector2(stomperRb.linearVelocity.x, stompBounceForce);
        }
    }
    
    // Método chamado pelo Mario quando ele está invencível (Starman/Dano)
    public virtual void TakeDamageByInvinciblePlayer()
    {
        // Usa a morte por esmagamento (fumaça), que é mais satisfatória
        DieByCrush(); 
        
        // Se quiser som, adicione aqui. Ex: AudioManager.Play("Kick");
    }

    public virtual void TurnIntoCoinByFire()
    {
        // 1. SEGURANÇA: Se já estiver morto, não faz nada
        // (Se tiver uma variável isDead no seu script base)
        // if (isDead) return;
        // isDead = true;

        // 2. SPAWN DA MOEDA
        if (coinFromFirePrefab != null)
        {
            // Instancia a moeda exatamente onde o inimigo estava
            GameObject coin = Instantiate(coinFromFirePrefab, transform.position, Quaternion.identity);
            
            // Se a moeda nascida de bloco ganha impulso, as de inimigo também podem:
            if(coin.TryGetComponent<Coin>(out Coin coinScript))
            {
                // Opcional: Toca uma força para cima para ficar bonito
                coinScript.LaunchItem(); 
            }
        }

        // 3. SUMIR IMEDIATAMENTE
        // Não toca animação de morte, não solta fumaça, apenas some
        // (como se tivesse sido transmutado instantaneamente)
        Destroy(gameObject);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se tocou na camada "DeadZone"
        if (other.gameObject.layer == LayerMask.NameToLayer("DeathZone"))
        {
            Destroy(gameObject); // Some sem efeito, sem som, apenas desaparece
        }
    }
}