using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnalogOscillator : MonoBehaviour
{
    public Volume volume;
    private ColorAdjustments colorAdjustments;

    [Header("Oscillation Settings")]
    public float saturationAmplitude = 5f;
    public float contrastAmplitude = 5f;
    public float exposureAmplitude = 0.1f;

    public float frequency = 0.5f;

    private float timeOffset;

    void Start()
    {
        // Acha o efeito no Volume
        if (volume.profile.TryGet(out colorAdjustments))
        {
            timeOffset = Random.Range(0f, 1000f); // Deslocamento aleatório pra variação não ser idêntica em diferentes volumes
        }
        else
        {
            Debug.LogError("Color Adjustments não encontrados no Volume!");
        }
    }

    void Update()
    {
        if (colorAdjustments == null) return;

        float time = Time.time + timeOffset;

        colorAdjustments.saturation.value = Mathf.Sin(time * frequency) * saturationAmplitude;
        colorAdjustments.contrast.value = Mathf.Sin(time * frequency + 1f) * contrastAmplitude;
        colorAdjustments.postExposure.value = Mathf.Sin(time * frequency + 2f) * exposureAmplitude;
    }
}