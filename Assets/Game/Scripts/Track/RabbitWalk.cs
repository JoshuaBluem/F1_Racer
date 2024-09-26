using UnityEngine;

/// <summary>
/// moves and rotates transform randomly. Teleports transform always in front of camera
/// </summary>
public class RabbitWalk : MonoBehaviour
{
    [SerializeField] float runningSpeed = 10;
    [SerializeField] float maxDistanceToCam = 50;
    private void Update()
    {
        //move forward
        this.transform.position += this.transform.forward * runningSpeed * Time.deltaTime;
        //Make random turns
        this.transform.eulerAngles += 360 * Random.Range(-1f, 1f) * Time.deltaTime * Vector3.up;

        Vector3 camDir = this.transform.position - Camera.main.transform.position;
        Vector3 forward = Camera.main.transform.forward;

        if (Vector3.Dot(forward, camDir) < 0 //behind me
            || camDir.magnitude > maxDistanceToCam) //too far away
        {
            int rl = (Random.Range(0, 2) == 0) ? -1 : 1;
            Vector3 newPos = Camera.main.transform.position + forward * maxDistanceToCam * 0.7f + Camera.main.transform.right * (10 + rl);
            newPos.y = this.transform.position.y;
            this.transform.position = newPos;
        }
    }
}
