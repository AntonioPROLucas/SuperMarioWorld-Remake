using UnityEngine;
using System.Collections; // Necessário para a Coroutine

public class MontyMoleSpawner : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    public GameObject montyMolePrefab;
    public GameObject dirtEffectPrefab;
    public GameObject holePrefab;
    
    [Tooltip("Ajuste X e Y para alinhar o buraco na terra")]
    public Vector2 holeOffset = new Vector2(0f, -0.5f); 

    [Tooltip("Distância do Mario para o castor começar a sair")]
    public float triggerDistance = 8f; 

    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered || Mario.instance == null) return;

        float distanceToMario = Vector2.Distance(transform.position, Mario.instance.transform.position);

        if (distanceToMario <= triggerDistance)
        {
            // O Mario chegou perto! Inicia a sequência.
            StartCoroutine(SpawnSequence());
        }
    }

    private IEnumerator SpawnSequence()
    {
        hasTriggered = true;

        // 1. Instancia o Efeito de Terra e guarda a referência dele!
        GameObject activeDirt = null;
        if (dirtEffectPrefab != null)
        {
            activeDirt = Instantiate(dirtEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Aguarda a animação da terra (o tempo que ele fica "cavando")
        yield return new WaitForSeconds(0.3f);

        // 3. O Castor vai sair agora! Destrói a sujeira imediatamente.
        if (activeDirt != null)
        {
            Destroy(activeDirt);
        }

        // 4. Instancia o Buraco (Cenário estático)
        if (holePrefab != null)
        {
            Vector2 spawnPos = (Vector2)transform.position + holeOffset;
            Instantiate(holePrefab, spawnPos, Quaternion.identity);
        }

        // 5. Instancia o Castor de fato
        if (montyMolePrefab != null)
        {
            Instantiate(montyMolePrefab, transform.position, Quaternion.identity);
        }

        // 6. Destrói o Spawner invisível
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.3f, 0f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}