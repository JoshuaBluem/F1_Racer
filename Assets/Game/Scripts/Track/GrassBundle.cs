using CustomInspector;
using UnityEngine;

/// <summary>
/// saves position of a grassbundle
/// </summary>
public class GrassBundle : MonoBehaviour
{
    public Vector3 WorldPosition
    {
        get => worldPosition;
        set
        {
            if (worldPosition == value)
                return;

            worldPosition = value;
            // OnTeleportation();
        }
    }
    [Tooltip("Current position")]
    [SerializeField, ReadOnly] Vector3 worldPosition;

    private void Awake()
    {
        worldPosition = this.transform.position;
    }
    private void Update()
    {
        this.transform.position = worldPosition;
    }
}
