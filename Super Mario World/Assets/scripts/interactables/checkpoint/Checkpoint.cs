using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Configurações")]
    public GameObject tapeObject;
    public GameObject sparkEffectPrefab;
    
    private bool isActivated = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        // Verifica se quem tocou foi o Mario (a pé ou montado no Yoshi)
        if (other.CompareTag("Player") || other.GetComponentInParent<Mario>() != null)
        {
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Checkpoint");

        // Toca o efeito de faísca e destrói a fita
        if (tapeObject != null)
        {
            if (sparkEffectPrefab != null)
            {
                GameObject spark = Instantiate(sparkEffectPrefab, tapeObject.transform.position, Quaternion.identity);
                Destroy(spark, 0.5f); // ⏱️ A faísca some depois de meio segundo!
            }
            Destroy(tapeObject);
        }

        // Registra a posição do Checkpoint
        if (GameController.instance != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 1f, 0);
            GameController.instance.SetCheckpoint(spawnPos);
        }
    }
}