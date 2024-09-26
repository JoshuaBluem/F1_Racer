using CustomInspector;
using UnityEngine;

/// <summary>
/// decelerates tourching rogidbodys and plays effect on touch
/// </summary>
public class StickyCollider : MonoBehaviour
{
    [Tooltip("The amount of speed removed over time")]
    [SerializeField, Min(0), Unit("%/sec")] float amount = .2f;

    [SerializeField] LayerMask affected;
    [SerializeField, ForceFill] ParticleSystem dirtSplash = null;

    [SerializeField] AudioClip collisionSound = null;


    private void OnCollisionEnter(Collision collision)
    {
        AudioSource.PlayClipAtPoint(collisionSound, collision.contacts[0].point);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.rigidbody == null)
            return;

        if (((1 << collision.gameObject.layer) & affected) == 0)
            return;

        if (!dirtSplash.isPlaying)
            dirtSplash.Play();

        dirtSplash.transform.position = collision.contacts[0].point;

        collision.rigidbody.velocity *= 1 - amount * Time.deltaTime;
    }
    private void OnCollisionExit(Collision collision)
    {
        dirtSplash.Stop();
    }
}
