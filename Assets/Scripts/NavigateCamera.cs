using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class NavigateCamera : MonoBehaviour
{
    const float DefaultDistance = 5.0f;

    public float AcrBallRotationSpeed = 0.25f;
    public float FpsRotationSpeed = 0.25f;
    public float TranslationSpeed = 2.0f;
    public float ZoomingSpeed = 2.0f;
    public float PannigSpeed = 0.25f;
    
    public float Distance;
    public float EulerAngleX;
    public float EulerAngleY;
    
    public GameObject Target;
    public Vector3 TargetPosition;

    private float deltaScroll;
    
    void Update()
    {
        bool doPlayModeStuffs = true;

        #if UNITY_EDITOR
            if (!EditorApplication.isPlaying) doPlayModeStuffs = false;
        #endif
        
        if (doPlayModeStuffs && Mathf.Abs(deltaScroll) > 0.01f)
        {
            Distance -= deltaScroll * ZoomingSpeed;
            transform.position = TargetPosition - transform.forward * Distance;

            if (Distance < 0)
            {
                TargetPosition = transform.position + transform.forward * DefaultDistance;
                Distance = Vector3.Distance(TargetPosition, transform.position);
            }

            deltaScroll *= 0.90f;
        }
    }

    private void OnGUI()
    {
        #if UNITY_EDITOR
        if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
        {
            EditorUtility.SetDirty(this); // this is important, if omitted, "Mouse down" will not be display
        }
        #endif

        // Arc ball rotation
        if (Event.current.alt && Event.current.type == EventType.mouseDrag &&Event.current.button == 0)
        {
            EulerAngleX += Event.current.delta.x * AcrBallRotationSpeed;
            EulerAngleY += Event.current.delta.y * AcrBallRotationSpeed;

            var rotation = Quaternion.Euler(EulerAngleY, EulerAngleX, 0.0f);
            var position = TargetPosition + rotation * Vector3.back * Distance;

            transform.rotation = rotation;
            transform.position = position;
        }

        // Fps rotation
        if (!Event.current.alt && Event.current.type == EventType.mouseDrag && Event.current.button == 0)
        {
            EulerAngleX += Event.current.delta.x * FpsRotationSpeed;
            EulerAngleY += Event.current.delta.y * FpsRotationSpeed;

            var rotation = Quaternion.Euler(EulerAngleY, EulerAngleX, 0.0f);

            transform.rotation = rotation;
            TargetPosition = transform.position + transform.forward*Distance;
        }

        if (Event.current.type == EventType.mouseDrag && Event.current.button == 2)
        {
            TargetPosition += transform.up * Event.current.delta.y * PannigSpeed;
            transform.position += transform.up * Event.current.delta.y * PannigSpeed;

            TargetPosition -= transform.right * Event.current.delta.x * PannigSpeed;
            transform.position -= transform.right * Event.current.delta.x * PannigSpeed;
        }

        if (Event.current.type == EventType.ScrollWheel)
        {
            deltaScroll = Event.current.delta.y;

            Distance -= deltaScroll * ZoomingSpeed;
            transform.position = TargetPosition - transform.forward * Distance;

            if (Distance < 0)
            {
                TargetPosition = transform.position + transform.forward * DefaultDistance;
                Distance = Vector3.Distance(TargetPosition, transform.position);
            }

        }

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.R)
            {
                TargetPosition = Vector3.zero;
                transform.position = TargetPosition - transform.forward * Distance;
            }

            if (Event.current.keyCode == KeyCode.F)
            {
                if (Target)
                {
                    TargetPosition = Target.gameObject.transform.position;
                    transform.position = TargetPosition - transform.forward * Distance;
                }
            }

            if (Event.current.keyCode == KeyCode.W)
            {
                TargetPosition += gameObject.transform.forward * TranslationSpeed;
                transform.position += gameObject.transform.forward * TranslationSpeed;
            }

            if (Event.current.keyCode == KeyCode.A)
            {
                TargetPosition -= gameObject.transform.right * TranslationSpeed;
                transform.position -= gameObject.transform.right * TranslationSpeed;
            }

            if (Event.current.keyCode == KeyCode.D)
            {
                TargetPosition += gameObject.transform.right * TranslationSpeed;
                transform.position += gameObject.transform.right * TranslationSpeed;
            }

            if (Event.current.keyCode == KeyCode.S)
            {
                TargetPosition -= gameObject.transform.forward * TranslationSpeed;
                transform.position -= gameObject.transform.forward * TranslationSpeed;
            }
        }
    }
}
