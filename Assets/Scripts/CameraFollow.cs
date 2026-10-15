using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Objetivo")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);
    public float followSmoothTime = 0.08f;

    [Header("Control Orbital (Mouse Smoothing)")]
    public float mouseSensitivity = 0.12f;
    public float rotationSmoothTime = 0.05f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 65f;

    [Header("Distancias y Oclusi�n F�sica")]
    public float defaultDistance = 5.2f;
    public float minDistance = 1.0f;
    public float distanceSmoothSpeed = 15f;
    public float sphereRadius = 0.25f;
    public LayerMask collisionLayers = ~0;

    private float targetYaw = 0f;
    private float targetPitch = 20f;
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float yawVelocity;
    private float pitchVelocity;

    private Vector3 currentTargetPos;
    private Vector3 followVelocity;
    private float currentDistance;

    // Screen Shake
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentDistance = defaultDistance;
        if (target != null) currentTargetPos = target.position + targetOffset;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard kb = Keyboard.current;

        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            targetYaw += delta.x * mouseSensitivity;
            targetPitch -= delta.y * mouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
        }

        // Suavizado inercial del rat�n
        currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Seguimiento suave del objetivo
        Vector3 desiredTargetPos = target.position + targetOffset;
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, desiredTargetPos, ref followVelocity, followSmoothTime);

        // 2. Rotaci�n orbital
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 cameraDir = rotation * -Vector3.forward;

        // 3. SphereCast anti-muros
        float targetDist = defaultDistance;
        RaycastHit hit;
        if (Physics.SphereCast(currentTargetPos, sphereRadius, cameraDir, out hit, defaultDistance, collisionLayers))
        {
            targetDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, defaultDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDist, Time.deltaTime * distanceSmoothSpeed);

        // 4. Posici�n final + Screen Shake
        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
        }

        transform.position = currentTargetPos + (cameraDir * currentDistance) + shakeOffset;
        transform.LookAt(currentTargetPos);
    }

    public void TriggerScreenShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }
}