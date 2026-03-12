using UnityEngine;

public abstract class ItemBase : MonoBehaviour
{
    public abstract void LaunchItem();

    // 🔄 MUDANÇA: Agora aceita CharacterBase, não apenas Mario
    protected abstract void Collect(CharacterBase player); 

    protected void OnTriggerEnter2D(Collider2D other)
    {
        // 🔄 MUDANÇA: Verifica se tem CharacterBase (Mario OU Yoshi)
        // A tag "Player" deve estar tanto no Mario quanto no Yoshi!
        if (other.CompareTag("Player")) 
        {
            if (other.TryGetComponent<CharacterBase>(out CharacterBase player))
            {
                Collect(player);
            }
            // Fallback: Se o Mario ainda não foi atualizado, tenta pegar o Mario antigo
            // (Remova este else if depois de atualizar o Mario)
            else if (other.TryGetComponent<Mario>(out Mario marioLegacy))
            {
                // Aqui teríamos um problema pois Collect exige CharacterBase.
                // Por isso, PRECISAMOS atualizar o Mario na próxima etapa.
            }
        }
    }
}