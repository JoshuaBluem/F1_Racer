using UnityEngine;

public class Rabbit_RandomWalk : MonoBehaviour
{
    [SerializeField] Animator rabbitAnimator;

    [SerializeField] float runningSpeed = 10;
    bool isRunnig = false;
    private void Update()
    {
        if (isRunnig)
        {
            //move forward
            this.transform.position += this.transform.forward * runningSpeed * Time.deltaTime;
            //Make random turns
            this.transform.eulerAngles += 360 * Random.Range(-1f, 1f) * Time.deltaTime * Vector3.up;
        }
    }

    private void Start()
    {
        ChangeRunning();
    }
    void ChangeRunning()
    {
        if (isRunnig)
        {
            isRunnig = false;
            rabbitAnimator.SetBool("IsRunning", false);
        }
        else
        {
            isRunnig = true;
            rabbitAnimator.SetBool("IsRunning", true);
        }
        Invoke(nameof(ChangeRunning), 4);
    }

}
