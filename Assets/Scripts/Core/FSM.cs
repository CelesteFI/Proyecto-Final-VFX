using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [LEGADO · NO USAR EN CONTENIDO NUEVO]
/// Controlador de jugador anterior a la refactorización: leía el teclado por su
/// cuenta (Keyboard.current) y no pasaba por <see cref="PlayerInputReader"/>.
///
/// Estado actual: no está montado en ningún GameObject de las maquetas.
/// Su reemplazo es <see cref="PlayerController"/> (movimiento, salto, dash y
/// gravedad) alimentado por <see cref="PlayerInputReader"/>.
///
/// No añadir este componente a ningún GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FSM : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // DEFINICIÓN DE ESTADOS DE LA FSM (Finite State Machine)
    // -------------------------------------------------------------------------
    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        Dashing,
        Attacking
    }

    [Header("Estado Actual de la FSM")]
    [SerializeField] private PlayerState currentState = PlayerState.Idle;
    public PlayerState CurrentState => currentState;

    [Header("Parámetros de Locomoción")]
    public float walkSpeed = 6.5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Física y Salto")]
    public float jumpHeight = 1.8f;      // Altura del salto en metros
    public float gravity = -18.0f;        // Gravedad ajustada
    public float fallMultiplier = 1.5f;   // Caída más rápida para sensación responsiva

    [Header("Mecánica de Dash (Shift Izquierdo o Doble Espacio)")]
    public float dashSpeed = 16.0f;
    public float dashDuration = 0.22f;
    private float dashTimer = 0f;
    private Vector3 dashDirection;

    [Header("Cámara")]
    [Tooltip("Transform de 'Main Camera'. Se asigna en el Inspector, sin Camera.main.")]
    [SerializeField] private Transform camTransform;

    private CharacterController controller;
    private float turnSmoothVelocity;
    private Vector3 verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null) return;

        // 1. Lectura de vector de entrada WASD
        Vector3 inputDir = ObtenerVectorDireccion(keyboard);

        // 2. Procesamiento de la Máquina de Estados Finitos
        switch (currentState)
        {
            case PlayerState.Idle:
                ProcesarIdle(keyboard, mouse, inputDir);
                break;

            case PlayerState.Moving:
                ProcesarMoving(keyboard, mouse, inputDir);
                break;

            case PlayerState.Jumping:
                ProcesarJumping(inputDir);
                break;

            case PlayerState.Falling:
                ProcesarFalling(inputDir);
                break;

            case PlayerState.Dashing:
                ProcesarDashing();
                break;

            case PlayerState.Attacking:
                // Estado reservado para cuando se añadan animaciones en la Semana 5
                break;
        }

        // 3. Aplicación de la física vertical (Gravedad)
        AplicarGravedad();
    }

    // -------------------------------------------------------------------------
    // MÉTODOS DE CADA ESTADO Y SUS TRANSICIONES
    // -------------------------------------------------------------------------

    private void ProcesarIdle(Keyboard keyboard, Mouse mouse, Vector3 inputDir)
    {
        // Transición: Salto (Barra Espaciadora)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            EjecutarSalto();
            return;
        }

        // Transición: Dash (Shift Izquierdo)
        if (keyboard.leftShiftKey.wasPressedThisFrame)
        {
            IniciarDash(inputDir);
            return;
        }

        // Transición: Movimiento
        if (inputDir.magnitude >= 0.1f)
        {
            CambiarEstado(PlayerState.Moving);
            return;
        }

        // Transición: Caída por desnivel repentino
        if (!controller.isGrounded && verticalVelocity.y < -1f)
        {
            CambiarEstado(PlayerState.Falling);
            return;
        }
    }

    private void ProcesarMoving(Keyboard keyboard, Mouse mouse, Vector3 inputDir)
    {
        // Transición: Salto en carrera
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            EjecutarSalto();
            return;
        }

        // Transición: Dash direccional
        if (keyboard.leftShiftKey.wasPressedThisFrame)
        {
            IniciarDash(inputDir);
            return;
        }

        // Transición: Detención a Idle
        if (inputDir.magnitude < 0.1f)
        {
            CambiarEstado(PlayerState.Idle);
            return;
        }

        // Transición: Caída al vacío o rampa pronunciada
        if (!controller.isGrounded && verticalVelocity.y < -1f)
        {
            CambiarEstado(PlayerState.Falling);
            return;
        }

        // Lógica de locomoción orientada a la cámara
        MoverJugador(inputDir, walkSpeed);
    }

    private void ProcesarJumping(Vector3 inputDir)
    {
        // Control aéreo reducido mientras salta
        MoverJugador(inputDir, walkSpeed * 0.85f);

        // Cuando la velocidad vertical empieza a descender, pasa a Falling
        if (verticalVelocity.y <= 0f)
        {
            CambiarEstado(PlayerState.Falling);
        }
    }

    private void ProcesarFalling(Vector3 inputDir)
    {
        // Control aéreo durante la caída
        MoverJugador(inputDir, walkSpeed * 0.85f);

        // Transición: Aterrizaje al tocar suelo
        if (controller.isGrounded)
        {
            if (inputDir.magnitude >= 0.1f)
                CambiarEstado(PlayerState.Moving);
            else
                CambiarEstado(PlayerState.Idle);
        }
    }

    private void ProcesarDashing()
    {
        // El Dash mueve al personaje a alta velocidad ignorando entradas
        controller.Move(dashDirection * dashSpeed * Time.deltaTime);

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f)
        {
            if (controller.isGrounded)
                CambiarEstado(PlayerState.Idle);
            else
                CambiarEstado(PlayerState.Falling);
        }
    }

    // -------------------------------------------------------------------------
    // MÉTODOS DE APOYO DE FÍSICA Y TRANSICIÓN
    // -------------------------------------------------------------------------

    private void EjecutarSalto()
    {
        // Fórmula cinemática: v = sqrt(h * -2 * g)
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        CambiarEstado(PlayerState.Jumping);
    }

    private void IniciarDash(Vector3 inputDir)
    {
        dashTimer = dashDuration;

        if (inputDir.magnitude >= 0.1f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            dashDirection = (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward).normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }

        CambiarEstado(PlayerState.Dashing);
    }

    private void MoverJugador(Vector3 inputDir, float velocidad)
    {
        if (inputDir.magnitude >= 0.1f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * velocidad * Time.deltaTime);
        }
    }

    private void AplicarGravedad()
    {
        // En Dashing no se aplica gravedad para mantener la trayectoria en el aire
        if (currentState == PlayerState.Dashing) return;

        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            // Gravedad con multiplicador de caída para que no parezca que flota
            float mult = (verticalVelocity.y < 0) ? fallMultiplier : 1.0f;
            verticalVelocity.y += gravity * mult * Time.deltaTime;
        }

        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private Vector3 ObtenerVectorDireccion(Keyboard keyboard)
    {
        float h = 0f;
        float v = 0f;
        if (keyboard.wKey.isPressed) v += 1f;
        if (keyboard.sKey.isPressed) v -= 1f;
        if (keyboard.dKey.isPressed) h += 1f;
        if (keyboard.aKey.isPressed) h -= 1f;
        return new Vector3(h, 0f, v).normalized;
    }

    private void CambiarEstado(PlayerState nuevoEstado)
    {
        if (currentState == nuevoEstado) return;
        currentState = nuevoEstado;
    }
}