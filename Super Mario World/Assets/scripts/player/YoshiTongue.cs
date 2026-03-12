using UnityEngine;

public class YoshiTongue : MonoBehaviour
{
    private Yoshi yoshiParent;

    void Awake()
    {
        yoshiParent = GetComponentInParent<Yoshi>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (yoshiParent == null) return;

        // 🔥 CORREÇÃO: Se a boca está cheia, a língua ignora tudo.
        if (yoshiParent.hasMouthFull) return;

        // Ignora Yoshi, Mario e Filhos
        if (other.CompareTag("Player") || other.transform.IsChildOf(yoshiParent.transform)) return;

        bool isValidTarget = false;

        // Verifica Tags
        if (other.CompareTag("Enemy") || other.CompareTag("Shell") || other.CompareTag("Fruit")) 
            isValidTarget = true;
        // Verifica Componente
        else if (other.GetComponent<Coin>() != null) 
            isValidTarget = true;

        if (isValidTarget)
        {
            // 🏈 REGRA DO CHUCK: A língua bate na armadura e escorrega!
            if (other.GetComponent<CharginChuck>() != null)
            {
                // A língua ignora o alvo e volta para a boca naturalmente.
                // O colisor do Chuck NUNCA é desligado, então ele não cai no abismo!
                return; 
            }

            other.enabled = false; // Desliga o colisor APENAS de quem vai ser engolido
            yoshiParent.OnTongueHit(other.gameObject);
        }
    }
}