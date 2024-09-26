using CustomInspector;
using System.Linq;
using UnityEngine;

/// <summary>
/// Spawns grass bundles around current transform (transform can be moved)
/// </summary>
public class GrassBundlesSpawner : MonoBehaviour
{
    [Tooltip("The object that gets copied")]
    [SerializeField, ForceFill] GameObject blueprint;

    [Hook(nameof(InstantiateGrassBundles), ExecutionTarget.IsPlaying)]
    [SerializeField, Min(1)] int amount = 50;
    [Hook(nameof(InstantiateGrassBundles), ExecutionTarget.IsPlaying)]
    [SerializeField, Min(.1f), Unit("2m")] float size = 50;

    GrassBundle[] instantiated = null;


    void Start()
    {
        InstantiateGrassBundles();
    }
    void InstantiateGrassBundles()
    {
        //Delete old
        if (instantiated != null)
        {
            foreach (var bundle in instantiated)
                Destroy(bundle.gameObject);
        }
        //create new
        blueprint.SetActive(true);
        instantiated = Enumerable.Range(0, amount)
                                 .Select(i => Instantiate(original: blueprint,
                                                          position: this.transform.position + new Vector3(Random.Range(-size, size), 0, Random.Range(-size, size)),
                                                          rotation: Quaternion.Euler(0, Random.Range(0f, 360f), 0), this.transform).GetComponent<GrassBundle>())
                                 .ToArray();
        blueprint.SetActive(false);
    }
    private void FixedUpdate()
    {
        foreach (GrassBundle bundle in instantiated)
        {
            if ((bundle.WorldPosition.x - this.transform.position.x) > size)
            {
                bundle.WorldPosition -= 2 * size * Vector3.right;
            }
            else if ((this.transform.position.x - bundle.WorldPosition.x) > size)
            {
                bundle.WorldPosition += 2 * size * Vector3.right;
            }

            if ((bundle.WorldPosition.z - this.transform.position.z) > size)
            {
                bundle.WorldPosition -= 2 * size * Vector3.forward;
            }
            else if ((this.transform.position.z - bundle.WorldPosition.z) > size)
            {
                bundle.WorldPosition += 2 * size * Vector3.forward;
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(this.transform.position, new Vector3(size * 2, 1, size * 2));
    }
}
