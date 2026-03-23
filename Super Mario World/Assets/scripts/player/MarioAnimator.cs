using UnityEngine;

public class MarioAnimator : MonoBehaviour
{
    private Animator anim;

    // Hashes dos parâmetros
    private int walkHash;
    private int runHash;
    private int jumpHash;
    private int fallHash;
    private int lowHash;
    private int skidHash;
    private int powerUpStateHash;
    private int isHoldingHash;    
    private int lookUpHash;
    private int throwUpHash;
    private int isRidingHash;
    private int isTurningHash; 
    
    // Yoshi
    private int startRidingHash; 
    private int ridePointHash;

    // ⬇️ 1. NOVO HASH PARA VITÓRIA
    private int victoryHash; 

    // Variáveis de cache
    private bool currentWalk;
    private bool currentRun;
    private bool currentJump;
    private bool currentFall;
    private bool currentLow;
    private bool currentSkid;
    private int currentPowerUpState; 
    private float currentHolding;    
    private bool currentLookUp;

    private void Awake()
    {
        anim = GetComponent<Animator>();

        walkHash = Animator.StringToHash("walk");
        runHash = Animator.StringToHash("run");
        jumpHash = Animator.StringToHash("jump");
        fallHash = Animator.StringToHash("fall");
        lowHash = Animator.StringToHash("low");
        skidHash = Animator.StringToHash("skid");
        powerUpStateHash = Animator.StringToHash("powerUpState");
        isHoldingHash = Animator.StringToHash("isHolding");      
        lookUpHash = Animator.StringToHash("lookUp");
        throwUpHash = Animator.StringToHash("throwUp");
        isRidingHash = Animator.StringToHash("isRiding");
        isTurningHash = Animator.StringToHash("isTurning"); 

        startRidingHash = Animator.StringToHash("startRiding");
        ridePointHash = Animator.StringToHash("ridePoint");

        // ⬇️ 2. INICIALIZAÇÃO
        victoryHash = Animator.StringToHash("victory");

        currentPowerUpState = -1;
        currentHolding = -1f;
    }

    // 🔹 ======================================================= 🔹
    // 🔹 MÉTODOS DE CONTROLE
    // 🔹 ======================================================= 🔹

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
        }
    }

    public void SetFall(bool falling)
    {
        if (currentFall != falling)
        {
            anim.SetBool(fallHash, falling);
            currentFall = falling;
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

    public void SetLookUp(bool lookingUp)
    {
        if (currentLookUp != lookingUp)
        {
            anim.SetBool(lookUpHash, lookingUp);
            currentLookUp = lookingUp;
        }
    }

    public void SetTrigger(string triggerName)
    {
        anim.SetTrigger(triggerName);
    }

    public void SetThrowUpTrigger()
    {
        anim.SetTrigger(throwUpHash);
    }
    
    // ⬇️ 3. NOVO MÉTODO PARA VITÓRIA
    public void SetVictory()
    {
        anim.SetTrigger(victoryHash);
    }
    
    public AnimatorStateInfo GetCurrentState()
    {
        return anim.GetCurrentAnimatorStateInfo(0);
    }

    // 🔹 ======================================================= 🔹
    // 🔹 MÉTODOS DO YOSHI
    // 🔹 ======================================================= 🔹

    public void SetRiding(bool isRiding)
    {
        anim.SetBool(isRidingHash, isRiding);
        if (isRiding)
        {
            anim.SetTrigger(startRidingHash);
        }
    }

    public void SetRideTurning(bool isTurning)
    {
        anim.SetBool(isTurningHash, isTurning);
    }

    public void SetRidePoint(bool isPointing)
    {
        anim.SetBool(ridePointHash, isPointing);
    }

    public void ResetAll()
    {
        anim.SetBool(walkHash, false);
        anim.SetBool(runHash, false);
        anim.SetBool(jumpHash, false);
        anim.SetBool(fallHash, false);
        anim.SetBool(skidHash, false);
        anim.SetBool(lowHash, false);
        anim.SetBool(lookUpHash, false);
    }

    public bool IsRiding()
    {
        if (anim != null)
        {
            return anim.GetBool(isRidingHash);
        }
        return false;
    }

    // 🔹 ======================================================= 🔹
    // 🔹 EVENTOS DE ANIMAÇÃO (CHAMADOS PELO ANIMATOR WINDOW)
    // 🔹 ======================================================= 🔹
}