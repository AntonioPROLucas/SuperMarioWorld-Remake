using UnityEngine;
using System.Collections;

public class WarpPipe : MonoBehaviour
{
    [Header("Configurações")]
    public Transform destination; // Onde o Mario vai sair?
    public Vector2 enterDirection = Vector2.down; // Direção para ENTRAR (Baixo, Cima, Dir, Esq)
    public Vector2 exitDirection = Vector2.up;    // Direção para SAIR
    
    [Header("Animação")]
    public float enterDuration = 1.0f;
    public float exitDuration = 1.0f;
    
    private bool isUsingPipe = false;

    // Detecta se o jogador está encostando no Trigger de entrada
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isUsingPipe) return;

        if (other.CompareTag("Player"))
        {
            // Verifica o Input baseado na direção configurada
            bool correctInput = false;

            if (enterDirection == Vector2.down && Input.GetKey(KeyCode.DownArrow)) correctInput = true;
            else if (enterDirection == Vector2.up && Input.GetKey(KeyCode.UpArrow)) correctInput = true;
            else if (enterDirection == Vector2.right && Input.GetKey(KeyCode.RightArrow)) correctInput = true;
            else if (enterDirection == Vector2.left && Input.GetKey(KeyCode.LeftArrow)) correctInput = true;

            if (correctInput)
            {
                StartCoroutine(EnterPipeRoutine(other.transform));
            }
        }
    }

    IEnumerator EnterPipeRoutine(Transform player)
    {
        isUsingPipe = true;
        
        // 1. Desativar controles e física do Mario
        Mario mario = player.GetComponent<Mario>();
        MarioMovement move = player.GetComponent<MarioMovement>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Collider2D col = player.GetComponent<Collider2D>();

        if (mario) mario.enabled = false;
        if (move) move.enabled = false;
        if (rb) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }
        if (col) col.enabled = false; // Desliga colisão para passar pela parede do tubo

        // 2. Animação de Entrada (Entrando no cano)
        Vector3 startPos = player.position;
        // Entra 1 unidade na direção do tubo
        Vector3 endPos = startPos + (Vector3)enterDirection * 1.5f; 

        float t = 0;
        while (t < enterDuration)
        {
            t += Time.deltaTime;
            player.position = Vector3.Lerp(startPos, endPos, t / enterDuration);
            yield return null;
        }

        // 3. Teletransporte (Tela Preta opcional aqui)
        yield return new WaitForSeconds(0.5f); // Pequena pausa no escuro
        
        if (destination != null)
        {
            player.position = destination.position;
        }

        // 4. Animação de Saída (Saindo do destino)
        startPos = player.position;
        endPos = startPos + (Vector3)exitDirection * 1.5f;
        
        // Toca som de sair do tubo se tiver
        
        t = 0;
        while (t < exitDuration)
        {
            t += Time.deltaTime;
            player.position = Vector3.Lerp(startPos, endPos, t / exitDuration);
            yield return null;
        }

        // 5. Reativar Mario
        if (col) col.enabled = true;
        if (rb) { rb.isKinematic = false; }
        if (move) move.enabled = true;
        if (mario) mario.enabled = true;

        isUsingPipe = false;
    }
}