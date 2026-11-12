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
        }
    }
}