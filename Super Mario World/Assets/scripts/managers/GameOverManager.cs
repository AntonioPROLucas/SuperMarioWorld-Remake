using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

// 🔥 Novo: Garante que o objeto tenha um CanvasGroup!
[RequireComponent(typeof(CanvasGroup))] 
public class GameOverManager : MonoBehaviour
{
    [Header("Animação do Texto")]
    public RectTransform textGame; // Arraste o texto "GAME" aqui no Inspector
    public RectTransform textOver; // Arraste o texto "OVER" aqui no Inspector
    public float animationDuration = 0.8f; // Tempo que leva para se juntarem
    public float initialDistance = 600f; // O quão longe eles começam fora da tela

    private CanvasGroup canvasGroup; // Referência ao componente de transparência
    private bool isDead = false;    // Evita rodar o código duas vezes

    void Awake()
    {
        // Pega a referência do CanvasGroup no início
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Garante que a tela comece invisível (Alpha 0)
        canvasGroup.alpha = 0f;
    }

    // Chame essa função lá no GameController quando as vidas zerarem
    public void ActivateGameOver()
    {
        if (isDead) return; // Segurança extra
        isDead = true;

        if (textGame != null && textOver != null)
        {
            textGame.gameObject.SetActive(true);
            textOver.gameObject.SetActive(true);
        }

        // Inicia a coroutine que faz o Fade-In suave
        StartCoroutine(FadeInRoutine());
        StartCoroutine(AnimateTextRoutine());
    }

    // ⭐ COROUTINE DO FADE-IN
    private IEnumerator FadeInRoutine()
    {
        float fadeDuration = 0.5f; // Quanto tempo a transição vai demorar (em segundos)
        float timePassed = 0f;

        // Loop que roda a cada frame até a duração acabar
        while (timePassed < fadeDuration)
        {
            timePassed += Time.deltaTime; // Soma o tempo que passou
            
            // Calcula a transparência suave (Lerp) de 0 até 1
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timePassed / fadeDuration);
            
            yield return null; // Espera o próximo frame
        }

        // Garante que o Alpha termine exatamente em 1 (totalmente visível)
        canvasGroup.alpha = 1f;

        // Depois que o fade terminar, espera o tempo de risada e reinicia
        StartCoroutine(RestartLevel());
    }

    private IEnumerator AnimateTextRoutine()
    {
        // 1. Grava a posição FINAL (onde você deixou eles na tela)
        Vector2 finalPositionGame = textGame.anchoredPosition;
        Vector2 finalPositionOver = textOver.anchoredPosition;

        // 2. Joga eles para longe (Posição INICIAL)
        Vector2 initialPositionGame = finalPositionGame + new Vector2(-initialDistance, 0);
        Vector2 initialPositionOver = finalPositionOver + new Vector2(initialDistance, 0);

        textGame.anchoredPosition = initialPositionGame;
        textOver.anchoredPosition = initialPositionOver;

        float timePassed = 0f;

        // 3. O Loop da Animação
        while (timePassed < animationDuration)
        {
            timePassed += Time.deltaTime;
            
            // Calcula a porcentagem da animação (de 0.0 a 1.0)
            float t = timePassed / animationDuration;
            
            // Fórmula mágica (SmoothStep) para o movimento começar rápido e frear suavemente no final
            float softness = t * t * (3f - 2f * t);

            // Move os textos um pouquinho a cada frame
            textGame.anchoredPosition = Vector2.Lerp(initialPositionGame, finalPositionGame, softness);
            textOver.anchoredPosition = Vector2.Lerp(initialPositionOver, finalPositionOver, softness);

            yield return null; // Espera o próximo frame
        }

        // 4. Garante que eles terminem cirurgicamente no lugar certo
        textGame.anchoredPosition = finalPositionGame;
        textOver.anchoredPosition = finalPositionOver;
    }

    private IEnumerator RestartLevel()
    {
        // Espera 3 segundos com o Mario nocauteado na tela
        yield return new WaitForSeconds(3f); 
        
        // Recarrega a cena atual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
}