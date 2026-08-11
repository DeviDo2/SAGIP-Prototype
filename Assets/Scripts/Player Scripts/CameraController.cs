using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Camera Settings")]
    [Tooltip("Offset from the target position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f); // Default offset

    private Transform target; // The target the camera will follow

    void Awake()
    {
        Instance = this;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
    }
}
