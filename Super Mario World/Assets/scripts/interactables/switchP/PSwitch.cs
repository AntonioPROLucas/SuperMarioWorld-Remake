using UnityEngine;
using System.Collections;
using System;

public class PSwitch : HoldableObject
{
    [Header("Configurações do Switch")]
    public float activeDuration = 10f;
    public Sprite pressedSprite;
    public static event Action<bool> OnPSwitchActive;

    private bool isPressed = false;
    private Animator anim;
    private SpriteRenderer sr;

    protected override void Awake()
    {
        base.Awake(); // Importante!
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    // P-Switch pode ser pego a qualquer momento, desde que não tenha sido apertado
    public override bool CanBePickedUp()
    {
        return !isPressed;
    }

    // Não precisamos sobrescrever Throw ou GetPickedUp, a lógica base serve perfeitamente!
    // Ele vai ser jogado como um objeto físico normal.

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isPressed || isHeld) return;

        // Se bater no Player (Pisão)
        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // Pisão vindo de cima
                {
                    PressSwitch();
                    break;
                }
            }
        }
    }

    private void PressSwitch()
    {
        if (isPressed) return;
        isPressed = true;
        
        // Se estava segurando, solta (caso raro, mas possível via código)
        if (isHeld) transform.SetParent(null);

        // Visual
        if (anim != null) anim.SetTrigger("Pressed");
        else if (pressedSprite != null && sr != null) sr.sprite = pressedSprite;

        // Trava no chão
        rb.bodyType = RigidbodyType2D.Static;
        col.enabled = false; // Desabilita colisão geral após pressionar

        StartCoroutine(SwitchRoutine());
    }

    private IEnumerator SwitchRoutine()
    {
        OnPSwitchActive?.Invoke(true); 
        yield return new WaitForSeconds(0.5f); // Tempo da animação

        // Some visualmente
        if (sr != null) sr.enabled = false;
        
        yield return new WaitForSeconds(activeDuration - 0.5f);

        OnPSwitchActive?.Invoke(false); 
        Destroy(gameObject);
    }
}