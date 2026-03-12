using UnityEngine;

public class Coin : ItemBase
{
    [Header("Configurações da Moeda")]
    public int scoreValue = 0; 
    public float popForce = 10f; 
    
    // 🍓 MARQUE ISSO COMO TRUE NO PREFAB DA FRUTA!
    [Header("Tipo de Item")]
    public bool isFruit = false; 

    // 🔒 TRAVAS DE SEGURANÇA
    [HideInInspector] public bool isSolidBlock = false; 
    private bool isCollected = false; // <-- A MÁGICA ACONTECE AQUI!

    [Header("Efeitos Visuais")]
    public GameObject collectEffectPrefab; 
    public float effectDuration = 1.0f;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 🛑 TRAVA ABSOLUTA:
        // Se já foi coletada nesse frame OU é um bloco sólido, ignora!
        if (isCollected || isSolidBlock) return;

        if (other.CompareTag("Player"))
        {
            // 🛑 NOVA TRAVA DA FRUTA:
            if (isFruit)
            {
                Yoshi yoshi = other.GetComponent<Yoshi>();
                if (yoshi == null) yoshi = other.GetComponentInParent<Yoshi>();

                if (yoshi != null && yoshi.IsMouthFull())
                {
                    return; 
                }
            }

            if (other.TryGetComponent<CharacterBase>(out CharacterBase player))
            {
                Collect(player);
            }
        }
    }

    protected override void Collect(CharacterBase player)
    {
        // Trava extra de segurança caso a função seja chamada por outro script
        if (isCollected) return; 
        
        isCollected = true; // Tranca a porta! Ninguém mais coleta.

        player.CollectCoin(); 
        if (GameController.instance != null && scoreValue > 0) GameController.instance.AddScore(scoreValue);

        if (collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        Destroy(gameObject);
    }
    
    public override void LaunchItem()
    {
        scoreValue = 10;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * popForce, ForceMode2D.Impulse);
            if (col != null) { col.enabled = false; StartCoroutine(EnableColliderDelay(0.2f)); }
        }
    }
    
    public void CollectDirectly(CharacterBase collector) 
    { 
        Collect(collector); 
    }
    
    private System.Collections.IEnumerator EnableColliderDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null) col.enabled = true;
    }
}