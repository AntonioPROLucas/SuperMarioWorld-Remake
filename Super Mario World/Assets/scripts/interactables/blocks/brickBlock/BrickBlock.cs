using UnityEngine;
using System.Collections;

public class BrickBlock : BlockBase
{
    [Header("Configurações de Giro")]
    public float spinDuration = 3.0f;  

    private Animator animator;
    private Collider2D col;
    private bool isSpinning = false;
    
    // =========================================================
    // 🔒 [LÓGICA COMENTADA] VARIÁVEIS DO P-SWITCH
    // =========================================================
    /*
    private bool isCoinState = false;
    private bool wasCollected = false; 
    */

    private void Awake()
    {
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    // =========================================================
    // 🔒 [LÓGICA COMENTADA] EVENTOS DO P-SWITCH
    // =========================================================
    /*
    private void OnEnable() => PSwitch.OnPSwitchActive += HandlePSwitch;
    private void OnDisable() => PSwitch.OnPSwitchActive -= HandlePSwitch;
    */

    public override void OnHitFromBelow(CharacterBase player)
    {
        // 🔒 [LÓGICA COMENTADA] Verificação antiga:
        // if (isSpinning || isCoinState || wasCollected) return;

        // ✅ LÓGICA ATUAL: Apenas verifica se já está girando
        if (isSpinning) return;

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;

        // 1. Bump Físico
        StartCoroutine(BumpAnimation());
        
        // 2. Animação de Giro
        if (animator != null) animator.SetBool("isRotating", true);

        // 3. Desliga colisão (vira trigger para passar por dentro)
        col.isTrigger = true; 

        yield return new WaitForSeconds(spinDuration);

        // 🔒 [LÓGICA COMENTADA] Verificação se virou moeda no meio do caminho
        /*
        if (!isCoinState && !wasCollected)
        {
            if (animator != null) animator.SetBool("isRotating", false);
            col.isTrigger = false;
            isSpinning = false;
        }
        */

        // ✅ LÓGICA ATUAL: Sempre restaura o bloco
        if (animator != null) animator.SetBool("isRotating", false);
        col.isTrigger = false;
        isSpinning = false;
    }

    // =========================================================
    // 🔒 [LÓGICA COMENTADA] MÉTODOS DE TRANSFORMAÇÃO E COLETA
    // =========================================================
    /*
    private void HandlePSwitch(bool active)
    {
        if (wasCollected) return; 

        if (active)
        {
            // --- VIRA MOEDA ---
            isCoinState = true;
            isSpinning = false; 
            
            if (animator != null)
            {
                animator.SetBool("isRotating", false); 
                animator.SetBool("isCoin", true);      
            }
            
            col.isTrigger = true; 
        }
        else
        {
            // --- VOLTA A SER BLOCO ---
            if (!wasCollected)
            {
                isCoinState = false;
                
                if (animator != null)
                {
                    animator.SetBool("isCoin", false);
                }
                
                col.isTrigger = false; 
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCoinState && !wasCollected && other.CompareTag("Player"))
        {
            wasCollected = true;

            if (GameController.instance != null)
            {
                GameController.instance.AddCoin();
                GameController.instance.AddScore(10);
            }

            if (GetComponent<Renderer>() != null) GetComponent<Renderer>().enabled = false;
            col.enabled = false; 
        }
    }
    */
}