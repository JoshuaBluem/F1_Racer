using UnityEngine;
using UnityEngine.InputSystem;



/// <summary>
/// activates unitys new input system
/// </summary>
public class ActivateInputActionAsset : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActionAsset;
    private void OnEnable()
    {
        inputActionAsset.Enable();
    }
    private void OnDisable()
    {
        inputActionAsset.Disable();
    }
}
