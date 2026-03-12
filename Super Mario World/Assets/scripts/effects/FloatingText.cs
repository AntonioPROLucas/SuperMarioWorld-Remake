using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float lifetime = 1f;
    
    // ✨ A MÁGICA AQUI: TMP_Text funciona tanto pra UI quanto pra World Space!
    private TMP_Text textMesh; 
    private Color textColor;

    public void Setup(string text, Color color)
    {
        // Tenta pegar qualquer um dos dois tipos de TextMeshPro
        textMesh = GetComponent<TMP_Text>();
        
        if (textMesh == null) 
        {
            Debug.LogWarning("O Prefab do FloatingText não tem um componente TextMeshPro anexado!");
            return;
        }

        textMesh.text = text;
        textMesh.color = color;
        textColor = color;
        
        // Destrói o objeto automaticamente após o tempo acabar
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Faz o texto subir constantemente
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Faz o texto ir ficando transparente (Fade Out)
        if (textMesh != null)
        {
            textColor.a -= Time.deltaTime / lifetime;
            textMesh.color = textColor;
        }
    }
}