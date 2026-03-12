using UnityEngine;

public class CoinBlockTransformation : MonoBehaviour
{
    private Animator anim;
    private Coin coinScript;
    
    // Referências aos dois tipos de colisor
    private CircleCollider2D coinCol; // O Trigger Redondo
    private BoxCollider2D blockCol;   // O Chão Quadrado

    private int originalLayer;
    private int blockLayer;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        coinScript = GetComponent<Coin>();
        
        // Pega o CircleCollider existente
        coinCol = GetComponent<CircleCollider2D>();
        
        // Tenta pegar o BoxCollider (se você adicionou manualmente)
        blockCol = GetComponent<BoxCollider2D>();

        // SE NÃO TIVER BOX COLLIDER, CRIA UM AUTOMATICAMENTE (Salva-vidas)
        if (blockCol == null)
        {
            blockCol = gameObject.AddComponent<BoxCollider2D>();
            blockCol.enabled = false; // Começa desligado
        }
        
        originalLayer = gameObject.layer;
        blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer == -1) blockLayer = LayerMask.NameToLayer("Ground"); 
    }

    private void OnEnable() => PSwitch.OnPSwitchActive += OnSwitchActive;
    private void OnDisable() => PSwitch.OnPSwitchActive -= OnSwitchActive;

    private void OnSwitchActive(bool active)
    {
        // Trava lógica
        if (coinScript != null) coinScript.isSolidBlock = active;

        if (active)
        {
            // --- 🧱 MODO BLOCO ---
            if (blockLayer != -1) gameObject.layer = blockLayer;
            
            // DESLIGA A BOLA, LIGA O QUADRADO
            if (coinCol != null) coinCol.enabled = false;
            if (blockCol != null) blockCol.enabled = true;

            if (anim != null) anim.SetBool("isSolid", true);
            gameObject.tag = "Untagged"; 
        }
        else
        {
            // --- 🪙 MODO MOEDA ---
            gameObject.layer = originalLayer;
            
            // DESLIGA O QUADRADO, LIGA A BOLA
            if (blockCol != null) blockCol.enabled = false;
            if (coinCol != null) coinCol.enabled = true;
            
            if (anim != null) anim.SetBool("isSolid", false);
            gameObject.tag = "Coin";
        }
    }
}