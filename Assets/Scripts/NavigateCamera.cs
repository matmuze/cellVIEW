using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Component = UnityEngine.Component;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class NavigateCamera : MonoBehaviour
{
    const float DefaultDistance = 5.0f;

    public Vector3 TargetPosition;

    public float AcrBallRotationSpeed = 0.25f;
    public float FpsRotationSpeed = 0.25f;
    public float TranslationSpeed = 2.0f;
    public float ZoomingSpeed = 2.0f;
    public float PannigSpeed = 0.25f;
    
    public float Distance;
    public float EulerAngleX;
    public float EulerAngleY;

    /*****/

    private GameObject Target;

    private bool forward;
    private bool backward;
    private bool right;
    private bool left;

    /*****/

    void OnEnable()
    {
        #if UNITY_EDITOR
        if (!EditorApplication.isPlaying) EditorApplication.update += Update;
        #endif
    }

    private float deltaTime = 0;
    private float lastUpdateTime = 0;

    private float deltaScroll;

    void Update()
    {
        deltaTime = Time.realtimeSinceStartup - lastUpdateTime;
        lastUpdateTime = Time.realtimeSinceStartup;

        //Debug.Log(deltaTime);

        if (Mathf.Abs(deltaScroll) > 0.01f)
        {
            deltaScroll *= 0.90f;
            Distance -= deltaScroll * deltaTime;
            transform.position = TargetPosition - transform.forward * Distance;

            if (Distance < 0)
            {
                TargetPosition = transform.position + transform.forward*DefaultDistance;
                Distance = Vector3.Distance(TargetPosition, transform.position);
            }
        }

        if (forward)
        {
            TargetPosition += gameObject.transform.forward * TranslationSpeed * deltaTime; 
            transform.position += gameObject.transform.forward * TranslationSpeed * deltaTime; 
        }

        if (backward)
        {
            TargetPosition -= gameObject.transform.forward * TranslationSpeed * deltaTime;
            transform.position -= gameObject.transform.forward * TranslationSpeed * deltaTime; 
        }

        if (right)
        {
            TargetPosition += gameObject.transform.right * TranslationSpeed * deltaTime;
            transform.position += gameObject.transform.right * TranslationSpeed * deltaTime; 
        }

        if (left)
        {
            TargetPosition -= gameObject.transform.right * TranslationSpeed * deltaTime;
            transform.position -= gameObject.transform.right * TranslationSpeed * deltaTime; 
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
            deltaScroll += Event.current.delta.y * ZoomingSpeed;

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
                //Distance = 75;
                TargetPosition = Vector3.zero;
                transform.position = TargetPosition - transform.forward * Distance;
            }

            if (Event.current.keyCode == KeyCode.F)
            {
                if (!Target)
                {
                    Target = GameObject.Find("Selected Element");
                }

                if (Target)
                {
                    //Distance = 75;
                    TargetPosition = Target.gameObject.transform.position;
                    transform.position = TargetPosition - transform.forward * Distance;
                }
            }
        }

        if (Event.current.keyCode == KeyCode.W)
        {
            forward = Event.current.type == EventType.KeyDown;
        }

        if (Event.current.keyCode == KeyCode.S)
        {
            backward = Event.current.type == EventType.KeyDown;
        }

        if (Event.current.keyCode == KeyCode.A)
        {
            left = Event.current.type == EventType.KeyDown;
        }

        if (Event.current.keyCode == KeyCode.D)
        {
            right = Event.current.type == EventType.KeyDown;
        }
    }
}
