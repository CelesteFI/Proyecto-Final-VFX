using UnityEngine;
using UnityEngine.InputSystem; // Librería oficial del Nuevo Input System

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Locomoción")]
    public float moveSpeed = 6.5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Física")]
    public float gravity = -18.0f;

    [Header("Esquiva (Dash)")]
    public float dashSpeed = 16.0f;
    public float dashDuration = 0.25f;
    private bool isDashing = false;
    private float dashTimeCounter = 0f;
    private Vector3 dashDirection;

    private CharacterController controller;
    private Transform camTransform;
    private float turnSmoothVelocity;
    private Vector3 verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // Verificar que el teclado y el ratón existan en el hardware
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null) return;

        // 1. Gravedad y suelo
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 2. Clics y acciones directas (Nuevo Input System)
        if (mouse != null)
        {
            // Clic Izquierdo Físico = Ataque
            if (mouse.leftButton.wasPressedThisFrame && !isDashing)
            {
                Debug.Log("⚔️ [CLIC IZQUIERDO]: Ataque Básico");
            }

            // Clic Derecho Físico = Habilidad
            if (mouse.rightButton.wasPressedThisFrame && !isDashing)
            {
                Debug.Log("🛡️ [CLIC DERECHO]: Habilidad Primaria");
            }
        }

        // Tecla E Física = Habilidad alternativa
        if (keyboard.eKey.wasPressedThisFrame && !isDashing)
        {
            Debug.Log("🛡️ [TECLA E]: Habilidad Primaria");
        }

        // Barra Espaciadora Física = Dash / Esquiva
        if (keyboard.spaceKey.wasPressedThisFrame && !isDashing)
        {
            IniciarDash(keyboard);
        }

        // 3. Movimiento WASD o ejecución del Dash
        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0f)
            {
                isDashing = false;
            }
        }
        else
        {
            // Lectura de teclas físicas individuales
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.wKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed) horizontal -= 1f;

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDir.magnitude >= 0.1f && camTransform != null)
            {
                // Orientación hacia el frente de la cámara
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
            }
        }

        // Aplicar gravedad
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void IniciarDash(Keyboard keyboard)
    {
        isDashing = true;
        dashTimeCounter = dashDuration;

        float h = 0f;
        float v = 0f;
        if (keyboard.wKey.isPressed) v += 1f;
        if (keyboard.sKey.isPressed) v -= 1f;
        if (keyboard.dKey.isPressed) h += 1f;
        if (keyboard.aKey.isPressed) h -= 1f;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            dashDirection = (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward).normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }

        Debug.Log("💨 [ESPACIO]: Esquiva / Dash con invulnerabilidad");
    }
}