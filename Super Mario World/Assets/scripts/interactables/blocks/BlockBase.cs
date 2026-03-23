using UnityEngine;
using System.Collections;

public abstract class BlockBase : MonoBehaviour
{
    private Vector3 initialPosition;

    // Ação principal a ser implementada por subclasses (para hit de Mario)
    // 🔄 MUDANÇA: Aceita CharacterBase em vez de Mario
    public abstract void OnHitFromBelow(CharacterBase player); 

    public void HitByShellAttack() 
    {
        OnHitFromBelow(null); 
    }
    
    protected void Start()
    {
        initialPosition = transform.position;
    }

    // Coroutine para a animação de "bump" (salto)
    protected IEnumerator BumpAnimation()
    {
        // 1. Mover para cima
        Vector3 targetUp = initialPosition + new Vector3(0, 0.2f, 0); // Exemplo de offset
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(initialPosition, targetUp, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Mover para baixo
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(targetUp, initialPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition; // Garante que volte exatamente ao ponto inicial
        yield break; 
    }

    // 🟢 Detecção de Colisão Sólida
    protected void OnCollisionEnter2D(Collision2D collision)
    {
        // LÓGICA 1: MARIO (Confia no ângulo relaxado)
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 hitNormal = collision.contacts[0].normal;
            if (Vector2.Dot(hitNormal, Vector2.up) > 0.6f) 
            {
                if (collision.gameObject.TryGetComponent<CharacterBase>(out CharacterBase player))
                {
                    OnHitFromBelow(player);
                    AudioManager.instance.PlaySFX("Block");
                }
            }
        }

        // LÓGICA 2: SHELL / CASCO (Solução por Velocidade Pura)
        else if (collision.gameObject.CompareTag("Shell"))
        {
            Rigidbody2D shellRb = collision.gameObject.GetComponent<Rigidbody2D>();
            
            // 1. Checamos se a Casca está subindo (a única prova de ataque)
            bool isMovingUp = shellRb != null && shellRb.linearVelocity.y > 0.1f;
            
            // 🎯 CONDIÇÃO FINAL: Ativa se a Casca estiver subindo (Ignora o centro Y).
            if (isMovingUp) 
            {
                HitByShellAttack();
            }
            else
            {
                HitByShellAttack();
            }
        }
    }
}