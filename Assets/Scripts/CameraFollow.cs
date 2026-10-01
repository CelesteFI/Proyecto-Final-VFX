using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Configuración Orbital")]
    public float distance = 5.0f;          // Distancia detrás del jugador
    public float height = 2.2f;            // Altura sobre el jugador
    public float mouseSensitivity = 3.0f;  // Sensibilidad del ratón
    public float minVerticalAngle = -20f;  // Límite hacia arriba
    public float maxVerticalAngle = 60f;   // Límite hacia abajo

    private float currentX = 0.0f;
    private float currentY = 20.0f;

    void Start()
    {
        // Ocultar y bloquear el cursor en el centro de la pantalla mientras juegas
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Leer el movimiento del ratón
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

        // Si presionas Escape, liberas el ratón
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calcular posición orbital alrededor del Player
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = new Vector3(0, 0, -distance);
        Vector3 targetPosition = target.position + Vector3.up * height;

        transform.position = targetPosition + rotation * direction;
        transform.LookAt(targetPosition);
    }
}