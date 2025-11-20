using UnityEngine;

/// <summary>
/// A classe "contrato" para todos os inimigos.
/// Define os métodos que Mario e os Cascos podem chamar.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Configurações Base do Inimigo")]
    [Tooltip("A força do pulo que o Mario ganha ao pisar neste inimigo.")]
    public float stompBounceForce = 10f;

    /// <summary>
    /// Chamado quando um jogador pisa neste inimigo.
    /// </summary>
    /// <param name="stomper">O GameObject que pisou (ex: Mario).</param>
    public abstract void OnStomped(GameObject stomper);

    /// <summary>
    /// Chamado quando um casco (ou projétil) atinge este inimigo.
    /// </summary>
    public abstract void HitByShell();

    /// <summary>
    /// Chamado quando o jogador colide lateralmente com este inimigo.
    /// </summary>
    /// <param name="player">O GameObject do jogador.</param>
    public virtual void OnLateralHit(GameObject player)
    {
        // A implementação padrão é aplicar dano ao jogador.
        if (player.TryGetComponent<Mario>(out Mario mario))
        {
            mario.TakeDamage();
        }
    }

    /// <summary>
    /// Função utilitária para os scripts filhos (Koopa, Goomba)
    /// usarem para aplicar o impulso no jogador.
    /// </summary>
    protected void ApplyStompBounce(GameObject stomper)
    {
        if (stomper.TryGetComponent<Rigidbody2D>(out Rigidbody2D stomperRb))
        {
            stomperRb.velocity = new Vector2(stomperRb.velocity.x, stompBounceForce);
        }
    }
}