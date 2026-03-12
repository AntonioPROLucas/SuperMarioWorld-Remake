using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class HoldableObject : MonoBehaviour
{
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected bool isHeld = false;
    protected bool justThrown = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public abstract bool CanBePickedUp();

    // ⚠️ ESSA É A PARTE MAIS IMPORTANTE ⚠️
    public virtual void GetPickedUp(Mario mario)
    {
        if (isHeld) return;
        isHeld = true;

        // Desliga a física para não brigar com o Mario
        rb.simulated = false; 
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        col.isTrigger = true; 
    }

    public virtual void Throw(Vector2 throwDirection, float force, Collider2D throwerCollider = null)
    {
        isHeld = false;
        transform.SetParent(null);
        justThrown = true;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false;
        
        rb.linearVelocity = Vector2.zero;

        if (throwDirection == Vector2.up)
             rb.linearVelocity = Vector2.up * force;
        else
             rb.linearVelocity = new Vector2(throwDirection.x, 0).normalized * force;

        StartCoroutine(ResetThrownFlag());

        if (throwerCollider != null)
        {
            Physics2D.IgnoreCollision(col, throwerCollider, true);
            StartCoroutine(RestoreCollision(throwerCollider));
        }
    }

    protected IEnumerator ResetThrownFlag()
    {
        yield return new WaitForSeconds(0.1f);
        justThrown = false;
    }

    protected IEnumerator RestoreCollision(Collider2D playerCollider)
    {
        yield return new WaitForSeconds(0.25f);
        if (playerCollider != null && col != null)
            Physics2D.IgnoreCollision(col, playerCollider, false);
    }
}