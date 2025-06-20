using UnityEngine;

public class MarioAnimator : MonoBehaviour
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    #region Movimento Horizontal
    public void SetWalk(bool walking)
    {
        anim.SetBool("walk", walking);
    }
    #endregion

    #region Corrida
    public void SetRun(bool running)
    {
        anim.SetBool("run", running);
    }
    #endregion
    
    #region Pulo e Queda
    public void SetJump(bool jumping)
    {
        anim.SetBool("jump", jumping);
    }

    public void SetFall(bool falling)
    {
        anim.SetBool("fall", falling);
    }
    #endregion

    #region Agachar
    public void SetLow(bool low)
    {
        anim.SetBool("low", low);
    }
    #endregion

    public AnimatorStateInfo GetCurrentState()
    {
        return anim.GetCurrentAnimatorStateInfo(0);
    }
}