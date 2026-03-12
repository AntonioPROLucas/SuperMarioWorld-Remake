using UnityEngine;

public class GoalTapeTrigger : MonoBehaviour
{
    public GoalGate parentGate; // Arraste o objeto GoalGate aqui

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentGate != null)
        {
            // Manda o colisor direto para o portão analisar
            parentGate.TapeTouched(other); 
        }
    }
}