using UnityEngine;

public class MarioAnimator : MonoBehaviour
{
    private Animator anim;

    private bool currentWalk;
    private bool currentRun;
    private bool currentJump;
    private bool currentFall;
    private bool currentLow;
    private bool currentSkid; // <--- ADICIONE ESTA LINHA
    private bool currentIdle; // <--- ADICIONE ESTA LINHA

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    

    public void SetWalk(bool walking)
    {
        if (currentWalk != walking)
        {
            anim.SetBool("walk", walking);
            currentWalk = walking;
        }
    }

    public void SetRun(bool running)
    {
        if (currentRun != running)
        {
            anim.SetBool("run", running);
            currentRun = running;
        }
    }

    public void SetSkid(bool skidding)
    {
        if (currentSkid != skidding) // <--- AGORA COM OTIMIZAÇÃO
        {
            anim.SetBool("skid", skidding);
            currentSkid = skidding;
        }
    }

    public void SetJump(bool jumping)
    {
        if (currentJump != jumping)
        {
            anim.SetBool("jump", jumping);
            currentJump = jumping;
            Debug.Log("Animator: SetJump(" + jumping + ")");
        }
    }

    public void SetFall(bool falling)
    {
        if (currentFall != falling)
        {
            anim.SetBool("fall", falling);
            currentFall = falling;
            Debug.Log("Animator: SetFall(" + falling + ")");
        }
    }

    public void SetLow(bool low)
    {
        if (currentLow != low)
        {
            anim.SetBool("low", low);
            currentLow = low;
        }
    }

    public void SetIdle(bool idle) // <--- NOVO MÉTODO PARA IDLE (com otimização)
    {
        if (currentIdle != idle)
        {
            anim.SetBool("idle", idle);
            currentIdle = idle;
        }
    }

    public AnimatorStateInfo GetCurrentState()
    {
        return anim.GetCurrentAnimatorStateInfo(0);
    }
}