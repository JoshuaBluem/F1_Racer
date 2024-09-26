using UnityEngine;


public class SetTargetFPS : MonoBehaviour
{
    [SerializeField] int targetFrameRate = 244;
    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
    }
}
