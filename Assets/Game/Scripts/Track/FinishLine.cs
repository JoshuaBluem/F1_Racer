using CustomInspector;
using UnityEngine;

/// <summary>
/// marks the finish line
/// </summary>
public class FinishLine : MonoBehaviour
{
    [Tooltip("Particle System that is played, when car reaches finish line")]
    [SerializeField, ForceFill] ParticleSystem winParticleS;
    private void OnTriggerEnter(Collider other)
    {
        //play some party effect
        winParticleS.Play();
    }
}
