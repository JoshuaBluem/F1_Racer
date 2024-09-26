using CustomInspector;
using System.Collections;
using UnityEngine;

/// <summary>
/// called from wheelHandler, controls car (wheel) sounds
/// </summary>
public class CarSounds : MonoBehaviour
{
    [SerializeField, SelfFill] Rigidbody rb;

    [Title("Engine")]
    [SerializeField] AudioSource engineSource;
    [SerializeField] int gearsAmount = 4;
    [SerializeField, Unit("m/s")] float minSpeed = 1;
    [SerializeField, Range(-3, 3)] float minPitch = .5f;
    [SerializeField, Range(0, 1)] float minVolume = .1f;
    [SerializeField, Unit("m/s")] float maxSpeed = 200 / 3.6f;
    [SerializeField, Range(-3, 3)] float maxPitch = 2.5f;
    [SerializeField, Range(0, 1)] float maxVolume = .2f;

    [Title("Crash")]
    [SerializeField, ForceFill] AudioClip crashSound;
    [Tooltip("Minimum force needed on collisions to play a sound")]
    [SerializeField, Min(0)] float minCollisionForce = 10;

    [Title("Drift")]
    [SerializeField, Range(0, 1)] float driftVolume = 1;
    [SerializeField, ForceFill] AudioSource driftSource = null;
    [SerializeField, Unit("/sec")] float driftVolumeApplySpeed = 2;
    float desiredVolume = 0;
    Coroutine applyVolumeC;

    private void Awake()
    {
        desiredVolume = 0;
        driftSource.volume = desiredVolume;
    }
    /// <summary>
    /// adds sound to wheels drifting amount times
    /// </summary>
    public void AddWheelsToDrift(int amount)
    {
        desiredVolume += amount * driftVolume / 4f;


        if (applyVolumeC != null)
            StopCoroutine(applyVolumeC);
        applyVolumeC = StartCoroutine(Apply());

        IEnumerator Apply()
        {
            if (desiredVolume > 0)
                driftSource.Play();

            if (desiredVolume > driftSource.volume)
            {
                while (desiredVolume > driftSource.volume)
                {
                    yield return null;
                    driftSource.volume += driftVolumeApplySpeed * Time.deltaTime;
                }
            }
            else //desiredVolume < driftSource.volume
            {
                while (desiredVolume < driftSource.volume)
                {
                    yield return null;
                    driftSource.volume -= driftVolumeApplySpeed * Time.deltaTime;
                }
            }
            driftSource.volume = desiredVolume;

            if (desiredVolume == 0)
                driftSource.Stop();
        }
    }
    private void FixedUpdate()
    {
        //get speed
        float speed = rb.velocity.magnitude;
        float speedPercent = (speed - minSpeed) / (maxSpeed - minSpeed);
        //three gears
        for (int gear = 0; gear < gearsAmount; gear++)
        {
            if (speedPercent < (float)gear / (float)gearsAmount)
            {
                speedPercent *= (float)gearsAmount / (float)gear;
                break;
            }
        }
        if (speedPercent > 1)
            Debug.LogWarning($"Speed was expected in range [{minSpeed}, {maxSpeed}] but was {speed}");

        //apply
        engineSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercent);
        engineSource.volume = Mathf.Lerp(minVolume, maxVolume, speedPercent);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude > rb.mass * minCollisionForce)
            AudioSource.PlayClipAtPoint(crashSound, collision.contacts[0].point);
    }
}
