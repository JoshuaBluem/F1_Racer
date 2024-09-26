using CustomInspector;
using UnityEngine;

/// <summary>
/// Makes this.transform follow any other transform
/// </summary>
public class FollowTransform : MonoBehaviour
{
    [SerializeField] Transform target;



    [SerializeField] Vector3 offset;
    [SerializeField, Indent] bool localOffset = false;

    enum Constraints
    {
        Free = 0,
        KeepX = 1 << 0,
        KeepY = 1 << 1,
        KeepZ = 1 << 2,
    }
    [SerializeField, Mask] Constraints constraints = FollowTransform.Constraints.Free;

    [ShowIf(StaticConditions.IsNotPlaying)]
    [Button(nameof(Update), label = "Apply Position")]
    [SerializeField, HideField] bool _;

    void Update()
    {
        if (target != null)
        {
            if (!localOffset)
            {
                Vector3 pos = this.transform.position;

                if ((constraints & Constraints.KeepX) == 0)
                    pos.x = target.position.x + offset.x;
                if ((constraints & Constraints.KeepY) == 0)
                    pos.y = target.position.y + offset.y;
                if ((constraints & Constraints.KeepZ) == 0)
                    pos.z = target.position.z + offset.z;

                this.transform.position = pos;
            }
            else
            {
                Vector3 pos = this.transform.position;
                Vector3 offsetPos = target.TransformPoint(offset);

                if ((constraints & Constraints.KeepX) == 0)
                    pos.x = offsetPos.x;
                if ((constraints & Constraints.KeepY) == 0)
                    pos.y = offsetPos.y;
                if ((constraints & Constraints.KeepZ) == 0)
                    pos.z = offsetPos.z;

                this.transform.position = pos;
            }
        }
    }
}
