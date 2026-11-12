using UnityEngine;
using System.Collections;

public class YoshiEgg : MonoBehaviour
{
    [Header("O Que Vai Nascer?")]
    public GameObject yoshiPrefab;       // O Prefab do Yoshi (PowerUp de Montaria)
    public GameObject oneUpMushroomPrefab; // O Prefab do Cogumelo Verde (Vida)

    [Header("Configuração da Animação")]
    public float hatchDuration = 1.0f;   // Tempo da animação do ovo quebrando
    public float yoshiGrowthDuration = 0.5f; // Tempo pro Yoshi crescer ao nascer

    // Referências internas
    private Animator anim;
    private bool hasHatched = false;

    private void Start()
    {
        anim = GetComponent<Animator>();
        
        // Assim que o ovo é instanciado (pelo bloco), começa a chocar
        StartCoroutine(HatchRoutine());
    }

    private IEnumerator HatchRoutine()
    {
        if (hasHatched) yield break;
        hasHatched = true;

        // 1. Toca animação do Ovo Quebrando (Se tiver animação 'hatch' no Animator)
        // Se não tiver Animator, ele só espera o tempo parado.
        if (anim != null) anim.SetTrigger("hatch");
        
        // Espera o tempo da animação/efeito
        yield return new WaitForSeconds(hatchDuration);

        // 2. DECISÃO LÓGICA: O Yoshi já existe na cena?
        Yoshi existingYoshi = FindObjectOfType<Yoshi>();

        if (existingYoshi != null)
        {
            // === CENÁRIO A: JÁ TEM YOSHI ===
            // O jogador é recompensado com uma vida extra (1-Up)
            SpawnItem(oneUpMushroomPrefab);
        }
        else
        {
            // === CENÁRIO B: NÃO TEM YOSHI ===
            // O fiel companheiro nasce!
            SpawnYoshi();
        }

        // 3. Destrói o Ovo (A casca some para dar lugar ao item)
        Destroy(gameObject);
    }

    private void SpawnItem(GameObject prefab)
    {
        if (prefab != null)
        {
            GameObject item = Instantiate(prefab, transform.position, Quaternion.identity);
            if (item.TryGetComponent<ItemBase>(out ItemBase itemScript))
            {
                itemScript.LaunchItem();
            }
        }
    }

    private void SpawnYoshi()
    {
        if (yoshiPrefab != null)
        {
            GameObject yoshiObj = Instantiate(yoshiPrefab, transform.position, Quaternion.identity);
            
            // Chama a animação especial de nascimento no script do Yoshi
            if (yoshiObj.TryGetComponent<Yoshi>(out Yoshi yoshiScript))
            {
                yoshiScript.PlayBirthAnimation(yoshiGrowthDuration);
            }
        }
    }
}