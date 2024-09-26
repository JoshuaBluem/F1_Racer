using CustomInspector;
using UnityEngine;

/// <summary>
/// Single trackpart that can be used to generate a big track
/// </summary>
public class TrackPart : MonoBehaviour
{
    public Bounds Bounds => _collider.bounds;
    [SerializeField, ForceFill] Collider _collider;

    /// <summary>
    /// The index of this track-parts. Start is 0. First generated is 1. FinishLine has tracksNumber-1
    /// </summary>
    public int TrackNumber { get; private set; } = 0; //zero means uninitialized

    /// <summary>
    /// To identify the shape of the trackPart
    /// </summary>
    public sbyte Shape_id => shape_id;
    [SerializeField] sbyte shape_id = 1;

    public float TrackWidth => this.transform.lossyScale.x * localTrackWidth;
    public const float localTrackWidth = 10; //loca space
    public Vector3 CurrentWorldEndPosition => this.transform.TransformPoint(LocalEndPosition);
    public Vector3 LocalEndPosition => new Vector3(endPosition_flat.x, 0, endPosition_flat.y); //Local Space
    [Tooltip("The position, where the next trackpart docks")]
    [SerializeField, ForceFill, LabelSettings("End Position")] Vector2 endPosition_flat = Vector2.up * 10;

    public float EndAngle => endAngle;
    [Tooltip("The angle, at which the next trackpart docks on")]
    [SerializeField, Min2(-360), Max(360)] float endAngle = 0;


    /// <summary>
    /// The point the curve turns around
    /// </summary>
    public Vector3 CurrentWorldCurvePivot => this.transform.TransformPoint(new Vector3(curvePivot_flat.x, 0, curvePivot_flat.y));
    /// <summary>
    /// Min(0)
    /// </summary>
    public float WorldCurveRadius => this.transform.lossyScale.x * local_curveRadius;
    [SerializeField, Min2(0), Indent, ReadOnly] float local_curveRadius; //local space
    [SerializeField, Indent, ReadOnly] Vector2 curvePivot_flat; //local space

    public void SetTrackNumber(int trackNumber)
    {
        this.TrackNumber = trackNumber;
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw Start and End
        //Start
        Gizmos.DrawSphere(this.transform.position, .2f);
        //End
        Vector3 endWorldPosition = CurrentWorldEndPosition;
        Gizmos.DrawSphere(endWorldPosition, .2f);
        //Normal-Direction
        Gizmos.DrawLine(endWorldPosition, endWorldPosition + this.transform.TransformDirection(Quaternion.Euler(0, endAngle, 0) * Vector3.forward));

        //curve pivot
        if (endPosition_flat.x != 0)
        {
            Vector3 pivot = CurrentWorldCurvePivot;
            Gizmos.DrawSphere(pivot, .2f);
            Gizmos.DrawLine(this.transform.position, pivot);
            Gizmos.DrawLine(CurrentWorldEndPosition, pivot);
        }
    }
    private void OnValidate()
    {
        if (LocalEndPosition.x == 0)
        {
            curvePivot_flat = Vector2.zero;
            local_curveRadius = 0;
        }
        else
        {
            float starttoEndMag = endPosition_flat.magnitude;
            local_curveRadius = (starttoEndMag / 2f) / Mathf.Sin(endAngle * Mathf.Deg2Rad / 2f);
            curvePivot_flat = endAngle > 0 ? Vector2.right * local_curveRadius : Vector2.left * local_curveRadius;
        }
    }
#endif
}
