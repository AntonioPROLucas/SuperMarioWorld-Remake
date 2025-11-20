using UnityEngine;

public class MarioAnimator : MonoBehaviour
{
    private Animator anim;

    // Hashes dos parâmetros (forma otimizada)
    private int walkHash;
    private int runHash;
    private int jumpHash;
    private int fallHash;
    private int lowHash;
    private int skidHash;
    private int powerUpStateHash; // 🔹 NOVO
    private int isHoldingHash;    // 🔹 NOVO
    
    // Variáveis de cache para otimização
    private bool currentWalk;
    private bool currentRun;
    private bool currentJump;
    private bool currentFall;
    private bool currentLow;
    private bool currentSkid;
    private int currentPowerUpState; // 🔹 NOVO
    private float currentHolding;    // 🔹 NOVO

    private void Awake()
    {
        anim = GetComponent<Animator>();

        // Converte os nomes dos parâmetros em Hashes no início
        walkHash = Animator.StringToHash("walk");
        runHash = Animator.StringToHash("run");
        jumpHash = Animator.StringToHash("jump");
        fallHash = Animator.StringToHash("fall");
        lowHash = Animator.StringToHash("low");
        skidHash = Animator.StringToHash("skid");
        powerUpStateHash = Animator.StringToHash("powerUpState"); // 🔹 NOVO
        isHoldingHash = Animator.StringToHash("isHolding");       // 🔹 NOVO

        // Inicializa com valores impossíveis para forçar a primeira atualização
        currentPowerUpState = -1;
        currentHolding = -1f;
    }

    // 🔹 ======================================================= 🔹
    // 🔹 NOVOS MÉTODOS PARA OS PARÂMETROS GLOBAIS
    // 🔹 ======================================================= 🔹

    /// <summary>
    /// Define o estado de power-up (0=Pequeno, 1=Grande, 2=Capa, etc.)
    /// </summary>
    public void SetPowerUpState(int state)
    {
        if (currentPowerUpState != state)
        {
            anim.SetInteger(powerUpStateHash, state);
            currentPowerUpState = state;
        }
    }

    public void SetHolding(float holdingValue)
    {
        if (currentHolding != holdingValue)
        {
            anim.SetFloat(isHoldingHash, holdingValue);
            currentHolding = holdingValue;
        }
    }

    public void SetWalk(bool walking)
    {
        if (currentWalk != walking)
        {
            anim.SetBool(walkHash, walking);
            currentWalk = walking;
        }
    }

    public void SetRun(bool running)
    {
        if (currentRun != running)
        {
            anim.SetBool(runHash, running);
            currentRun = running;
        }
    }

    public void SetSkid(bool skidding)
    {
        if (currentSkid != skidding)
        {
            anim.SetBool(skidHash, skidding);
            currentSkid = skidding;
        }
    }

    public void SetJump(bool jumping)
    {
        if (currentJump != jumping)
        {
            anim.SetBool(jumpHash, jumping);
            currentJump = jumping;
            // Debug.Log("Animator: SetJump(" + jumping + ")"); // Podes manter se quiseres
        }
    }

    public void SetFall(bool falling)
    {
        if (currentFall != falling)
        {
            anim.SetBool(fallHash, falling);
            currentFall = falling;
            // Debug.Log("Animator: SetFall(" + falling + ")"); // Podes manter se quiseres
        }
    }

    public void SetLow(bool low)
    {
        if (currentLow != low)
        {
            anim.SetBool(lowHash, low);
            currentLow = low;
        }
    }

    // Nota: O teu método SetIdle() não parece ser chamado em Mario.cs.
    // O estado 'idle' é geralmente o estado padrão para onde o Animator vai
    // quando 'walk', 'jump', 'fall', etc., são falsos.

   public void SetTrigger(string triggerName)
    {
        // Esta linha chama o "SetTrigger" real no componente Animator
        anim.SetTrigger(triggerName);
    }


    public AnimatorStateInfo GetCurrentState()
    {
        return anim.GetCurrentAnimatorStateInfo(0);
    }
}