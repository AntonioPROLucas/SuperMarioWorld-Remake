using UnityEngine;
using System.Collections; // ✨ Necessário para usar IEnumerator

public class EffectAutoDestroy : MonoBehaviour
{
    public float delay = 0.5f;

    IEnumerator Start()
    {
        Animator anim = GetComponent<Animator>();
        
        if (anim != null)
        {
            yield return null; 

            float animTime = anim.GetCurrentAnimatorStateInfo(0).length;
            Destroy(gameObject, animTime);
        }
        else
        {
            Destroy(gameObject, delay);
        }
    }
}