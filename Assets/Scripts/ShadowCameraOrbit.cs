using UnityEngine;

[ExecuteInEditMode]
public class ShadowCameraOrbit : MonoBehaviour
{
    const float DefaultDistance = 5.0f;

    public float X;
    public float Y;
    public float Distance = DefaultDistance;

    public Vector3 Target;

    void Update()
    {
        Distance = Mathf.Max(Distance, 0);

        var rotation = Quaternion.Euler(Y, X, 0.0f);
        var position = rotation * new Vector3(0.0f, 0.0f, -Distance) + Target;

        transform.rotation = rotation;
        transform.position = position;
    }
}
