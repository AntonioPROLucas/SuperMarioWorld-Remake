using UnityEngine;
using System.Collections;

public class QuestionBlock : BlockBase
{
    // Variáveis configuráveis no Inspector
    public Sprite inactiveSprite; // Bloco Inativo/Vazio
    public GameObject itemPrefab; // Prefab do item que sairá (Moeda, Cogumelo, Yoshi)

    private SpriteRenderer spriteRenderer;
    private Animator animator; // Referência ao Animator para parar a animação de giro
    private bool isActive = true; 

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        // NOTA: O sprite ativo (girando) deve ser o estado padrão de entrada do Animator.
    }

    public override void OnHitFromBelow(CharacterBase player)
    {
        if (!isActive) return;

        isActive = false;
        StartCoroutine(BumpAndSpawnItem());
    }

    private IEnumerator BumpAndSpawnItem()
    {
        // Inicia o Bump (visual)
        StartCoroutine(BumpAnimation());

        // Espera um pequeno momento para o item sair (visual)
        yield return new WaitForSeconds(0.05f); 

        // 3. Spawning do Item
        SpawnItem();

        // 4. Mudança de Visual: PARAR ANIMAÇÃO E TROCAR SPRITE
        if (animator != null)
        {
            animator.enabled = false; // ⬅️ IMPORTANTE: Desativa o Animator, parando a animação de giro.
        }
        
        // Troca o SpriteRenderer para o sprite estático inativo (X ou Tijolo)
        spriteRenderer.sprite = inactiveSprite;
    }
    
    private void SpawnItem()
    {
        if (itemPrefab == null) return;

        // Se for Moeda (Coleta Imediata)
        if (itemPrefab.TryGetComponent<Coin>(out Coin coinRef))
        {
            // Cria o visual
            GameObject visualCoin = Instantiate(itemPrefab, transform.position + Vector3.up, Quaternion.identity);
            
            if (GameController.instance != null) 
            {
                GameController.instance.AddCoin(); // Conta a moeda na HUD
                
                // ✨ Texto flutuante mostrando 10 pontos!
                GameController.instance.AddScore(10, transform.position + (Vector3.up * 1.5f)); 
            }

            // Animação visual (subir e sumir)
            if (visualCoin.TryGetComponent<Coin>(out Coin sceneCoin))
            {
                sceneCoin.LaunchItem(); // Faz ela pular (visual)
                Destroy(sceneCoin.gameObject, 0.5f);
                if(sceneCoin.GetComponent<Collider2D>()) sceneCoin.GetComponent<Collider2D>().enabled = false;
            }
            return;
        }

        // --- LÓGICA PADRÃO PARA OUTROS ITENS (Cogumelo, Yoshi, etc) ---
        Vector3 spawnPosition = transform.position + Vector3.up * 1f; 
        GameObject newItem = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);

        if (newItem.TryGetComponent<ItemBase>(out var item))
        {
            item.LaunchItem();
            
            // 💡 Dica extra: Se quiser que cogumelos e flores dêem 1000 pontos ao sair do bloco,
            // você pode adicionar a chamada do AddScore aqui também!
            // if (GameController.instance != null) GameController.instance.AddScore(1000, spawnPosition);
        }
    }
}