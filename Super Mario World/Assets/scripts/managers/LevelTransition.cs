using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using System.Collections;

public class LevelTransition : MonoBehaviour
{
    public static LevelTransition instance;

    [Header("UI Referências")]
    public Image blackScreenImage;
    
    [Header("Configurações")]
    public float transitionDuration = 2.0f;

    private Material irisMaterial;

    void Awake() 
    { 
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (blackScreenImage != null)
        {
            // Cria uma cópia do material para não alterar o arquivo original
            irisMaterial = new Material(blackScreenImage.material);
            blackScreenImage.material = irisMaterial;
            blackScreenImage.gameObject.SetActive(false); 
        }
    }

    public bool IsTransitioning()
    {
        return blackScreenImage != null && blackScreenImage.gameObject.activeSelf;
    }

    public void PlayIrisOut(Transform target)
    {
        Debug.Log("🏁 LevelTransition: Iniciando Iris Out...");

        // 🚨 A CORREÇÃO MÁGICA:
        // Força o tempo a rodar. Se estiver 0, o 'timer' do loop não sobe e trava tudo.
        Time.timeScale = 1.0f; 

        if (blackScreenImage != null)
        {
            blackScreenImage.gameObject.SetActive(true);
            StartCoroutine(IrisRoutine(target));
        }
        else
        {
            Debug.LogError("🚨 ERRO: A imagem preta (Black Screen Image) não está associada no Inspector!");
        }
    }

    IEnumerator IrisRoutine(Transform target)
    {
        float timer = 0f;
        float startRadius = 1.5f; 
        float endRadius = 0f;     

        float aspect = (float)Screen.width / Screen.height;
        if(irisMaterial) irisMaterial.SetFloat("_AspectRatio", aspect);

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;

            float currentRadius = Mathf.Lerp(startRadius, endRadius, t);
            if(irisMaterial) irisMaterial.SetFloat("_Radius", currentRadius);

            // Verificação de segurança extra para não dar erro se a câmera sumir
            if (target != null && irisMaterial != null && Camera.main != null)
            {
                Vector3 screenPos = Camera.main.WorldToViewportPoint(target.position);
                irisMaterial.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
            }

            yield return null;
        }

        if(irisMaterial) irisMaterial.SetFloat("_Radius", 0f);
        
        // Espera um pouco com a tela preta antes de recarregar
        yield return new WaitForSeconds(0.5f);

        // Recarrega a cena atual
        string currentSceneName = SceneManager.GetActiveScene().name; 
        Debug.Log("🔄 Recarregando Cena: " + currentSceneName);
        SceneManager.LoadScene(currentSceneName);
    }
}