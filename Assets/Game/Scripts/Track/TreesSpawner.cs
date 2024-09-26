using CustomInspector;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Spawns trees that are not colliding with the actual track
/// </summary>
public class TreesSpawner : MonoBehaviour
{
    [SerializeField, ForceFill] GameObject[] treePrefabs = null;

    [Tooltip("How many trees are spawned")]
    [SerializeField] float spawnAmount = 30;
    [Tooltip("If trees are spawned at a position, at what spread?")]
    [SerializeField] float spawnRadius = 30;
    [Tooltip("Additional percentage added to the height of the tree")]
    [SerializeField, Range(0, 1)] float scaleSpread = 0.1f;

    [SerializeField] bool spawnOnStart = true;
    [SerializeField] public Vector3 startSpawnPosition = Vector3.zero;

    [SerializeField, Layer] int trackLayer;

    private void Start()
    {
        if (spawnOnStart)
            SpawnTrees(startSpawnPosition);
    }
    public void SpawnTrees(Vector3 areaCenter)
    {
        GameObject preferredTree = treePrefabs[Random.Range(0, treePrefabs.Length)]; //in a real environment one tree-type is always dominating

        for (int i = 0; i < spawnAmount; i++)
        {
            Vector3 randomPosition;
            int tries = 0;
            do
            {
                randomPosition = areaCenter
                            + new Vector3(Random.Range(-spawnRadius, spawnRadius),
                                          0,
                                          Random.Range(-spawnRadius, spawnRadius));
                randomPosition.y = this.transform.position.y;

                tries++;
                if (tries > 100)
                    throw new Exception("Could not find space to spawn a tree. Every position is blocked");
            }
            while (Physics.OverlapBox(randomPosition, new Vector3(.4f, 3, .4f), Quaternion.identity, 1 << trackLayer).Length > 0); //do not spawn trees on the track

            SpawnAt(randomPosition);
        }

        void SpawnAt(Vector3 treePosition)
        {
            GameObject prefab = Random.Range(0, 3) != 0 ? preferredTree : treePrefabs[Random.Range(0, treePrefabs.Length)];
            GameObject tree = Instantiate(prefab, this.transform);
            tree.transform.SetPositionAndRotation(treePosition, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            tree.transform.localScale *= 1 + Random.Range(-scaleSpread / 2f, scaleSpread / 2f);
        }
    }
    public void DestroyAllTrees()
    {
        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (spawnOnStart)
        {
            Gizmos.DrawWireCube(startSpawnPosition + Vector3.up * 2, new Vector3(2 * spawnRadius, 4, 2 * spawnRadius));
        }
    }
}
