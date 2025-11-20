using UnityEngine;

public class SetKoopaStateOnExit : StateMachineBehaviour
{
    public int stateToSet;
    public string parameterName = "State";
    private bool stateHasBeenSet = false;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        stateHasBeenSet = false;
    }

    // Chamado na SAÍDA do estado "pulled"
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateHasBeenSet) return;

        // 1. Define o parâmetro "State" no Animator
        animator.SetInteger(parameterName, stateToSet);
        
        // 2. Avisa o script KoopaTroopa.cs que o estado mudou
        if (animator.TryGetComponent<KoopaTroopa>(out KoopaTroopa koopa))
        {
            koopa.SetInternalState(stateToSet);
            koopa.OnKnockbackComplete();
        }
        
        stateHasBeenSet = true;
    }
}