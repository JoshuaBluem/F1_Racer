using CustomInspector;
using UnityEngine;

/// <summary>
/// controls wheel-effects like sound and drift trails
/// </summary>
public class WheelHandler : MonoBehaviour
{
    [SerializeField, ForceFill, AssetsOnly] GameObject trailPrefab = null;
    public TrailRenderer Trail => trail;
    TrailRenderer trail;
    [SerializeField, ForceFill, AssetsOnly] GameObject smokePrefab = null;
    ParticleSystem smoke;
    [SerializeField, ForceFill] CarSounds carSounds = null;

    [SerializeField, Layer] int lightsOffLayer;
    [SerializeField, ForceFill] Material outlineMaterial;

    [Tooltip("The max force applied to wheels until a visual driftEffect is shown")]
    [SerializeField, Min(0)] float driftForce = 10;

    public WheelHit LastGroundHit { get; private set; }
    public WheelCollider WheelCollider => wheelCollider;
    WheelCollider wheelCollider;
    Transform meshChild;

    [SerializeField, ReadOnly]
    bool isDrifting = false;

    private void Awake()
    {
        //read infos
        wheelCollider = GetComponent<WheelCollider>();
        Debug.Assert(wheelCollider != null);

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Debug.Assert(meshRenderer != null);
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Debug.Assert(meshFilter != null);

        //create mesh
        meshChild = new GameObject("Mesh").transform;
        meshChild.parent = this.transform;
        meshChild.gameObject.AddComponent<MeshRenderer>().sharedMaterials = meshRenderer.sharedMaterials;
        meshChild.gameObject.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;
        //+ outline
        var outlineMesh = new GameObject("Mesh").transform;
        outlineMesh.gameObject.layer = lightsOffLayer;
        outlineMesh.parent = meshChild;
        outlineMesh.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        outlineMesh.gameObject.AddComponent<MeshRenderer>().sharedMaterial = outlineMaterial;
        outlineMesh.gameObject.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;

        //Delete on current
        Destroy(meshRenderer);
        Destroy(meshFilter);

        //effect
        trail = Instantiate(trailPrefab, this.transform).GetComponent<TrailRenderer>();
        trail.transform.localPosition = -Vector3.up;
        trail.enabled = true;
        trail.emitting = false;

        smoke = Instantiate(smokePrefab, this.transform).GetComponent<ParticleSystem>();
        smoke.transform.localPosition = -Vector3.up * wheelCollider.radius;
        smoke.Stop();
    }

    private void Update()
    {
        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        meshChild.position = position;
        meshChild.rotation = rotation;

        //Drift Check
        // Rigidbody rb = wheelCollider.attachedRigidbody;
        if (IsDrifting())
        {
            if (!isDrifting)
            {
                //StartDrifting
                isDrifting = true;
                trail.emitting = true;
                smoke.Play();
                carSounds.AddWheelsToDrift(1);
            }
            trail.transform.position = position + Vector3.up * (wheelCollider.radius * -0.99f);
        }
        else if (isDrifting)
        {
            //Stop drifting
            isDrifting = false;
            trail.emitting = false;
            smoke.Stop();
            carSounds.AddWheelsToDrift(-1);
        }

        bool IsDrifting()
        {
            if (wheelCollider.GetGroundHit(out WheelHit wheelHit))
            {
                LastGroundHit = wheelHit;
                return Mathf.Abs(wheelHit.sidewaysSlip) > driftForce || -wheelHit.forwardSlip > driftForce;
            }
            else
            {
                return false;
            }
        }

    }
}
