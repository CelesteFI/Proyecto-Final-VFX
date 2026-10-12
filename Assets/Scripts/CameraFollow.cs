using UnityEngine;
using UnityEngine.InputSystem; // Librería oficial del Nuevo Input System

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Control Orbital")]
    public float mouseSensitivity = 0.15f; // Ajustado para el valor delta del New Input System
    public float minVerticalAngle = -15f;
    public float maxVerticalAngle = 60f;

    [Header("Distancias y Colisión")]
    public float defaultDistance = 5.0f;
    public float minDistance = 1.0f;
    public float smoothSpeed = 14f;
    public float sphereRadius = 0.25f;
    public LayerMask collisionLayers = ~0;

    private float currentX = 0f;
    private float currentY = 20f;
    private float currentDistance;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentDistance = defaultDistance;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse != null)
        {
            // Lectura directa del delta del ratón en píxeles del nuevo sistema
            Vector2 mouseDelta = mouse.delta.ReadValue();
            currentX += mouseDelta.x * mouseSensitivity;
            currentY -= mouseDelta.y * mouseSensitivity;
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        }

        // Liberar cursor con Escape
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivot = target.position + targetOffset;
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 camDir = rotation * -Vector3.forward;

        // Detección física de muros/árboles
        float targetDist = defaultDistance;
        RaycastHit hit;
        if (Physics.SphereCast(pivot, sphereRadius, camDir, out hit, defaultDistance, collisionLayers))
        {
            targetDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, defaultDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDist, Time.deltaTime * smoothSpeed);

        transform.position = pivot + (camDir * currentDistance);
        transform.LookAt(pivot);
    }
}