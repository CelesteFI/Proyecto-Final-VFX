using UnityEngine;

/// <summary>
/// Locomoción del héroe activo sobre CharacterController.
/// Toda la lectura de entrada proviene de PlayerInputReader (New Input System),
/// de modo que este módulo no conoce teclados ni ratones concretos.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Aceleración y Suavizado")]
    public float acceleration = 14f;
    public float deceleration = 18f;
    public float rotationSmoothTime = 0.08f;

    [Header("Física y Salto Asistido")]
    public float jumpHeight = 2.0f;
    public float gravity = -20f;
    public float fallMultiplier = 1.8f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.12f;

    [Header("Dash (Shift Izquierdo)")]
    public float dashDuration = 0.22f;
    [Tooltip("Enfriamiento REAL del Dash en segundos (empieza al pulsar Shift). Es el dato que PlayerCombat envía al radial del HUD. 0 = sin enfriamiento.")]
    public float dashCooldown = 3f;
    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("PlayerInputReader del mismo Player.")]
    [SerializeField] private PlayerInputReader entrada;
    [Tooltip("Transform de 'Main Camera'. Se asigna a mano, sin Camera.main.")]
    [SerializeField] private Transform camTransform;

    private CharacterController controller;
    private TagTeamManager tagTeam;

    private Vector3 currentMoveVelocity;
    private float verticalVelocityY;
    private float turnSmoothVelocity;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;
    private float dashCooldownTimer;
    private float velocidadHorizontalActual;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        tagTeam = GetComponent<TagTeamManager>();
        if (entrada == null) entrada = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (entrada == null) return;

        HeroCombatData activeHero = (tagTeam != null) ? tagTeam.ActiveHero : null;
        float baseSpeed = (activeHero != null) ? activeHero.moveSpeed : 7f;
        float dashSpeed = (activeHero != null) ? activeHero.dashSpeed : 16f;

        // 1. Detección de suelo y Coyote Time
        if (controller.isGrounded)
        {
            coyoteCounter = coyoteTime;
            if (verticalVelocityY < 0f) verticalVelocityY = -2f;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // 2. Buffer de salto
        if (entrada.SaltoPulsado) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // 3. Ejecutar salto si las ventanas coinciden
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isDashing)
        {
            verticalVelocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // 4. Dash (con enfriamiento real)
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        if (entrada.DashPulsado && !isDashing && dashCooldownTimer <= 0f)
        {
            IniciarDash(entrada.Movimiento, dashSpeed);
            dashCooldownTimer = Mathf.Max(0f, dashCooldown);
        }

        // 5. Locomoción horizontal
        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }
        else
        {
            ProcesarMovimiento(entrada.Movimiento, baseSpeed);
        }

        // 6. Gravedad con caída rápida responsiva
        if (!isDashing)
        {
            float mult = (verticalVelocityY < 0f) ? fallMultiplier : 1f;
            verticalVelocityY += gravity * mult * Time.deltaTime;
            controller.Move(new Vector3(0, verticalVelocityY, 0) * Time.deltaTime);
        }

        // 7. Velocidad horizontal real del frame, expuesta para la animación.
        //    CharacterController.velocity NO sirve para esto: solo refleja el último
        //    Move() del frame, que aquí es el de gravedad (vertical); leerlo devuelve
        //    siempre 0 en XZ aunque el héroe esté corriendo.
        velocidadHorizontalActual = isDashing
            ? dashSpeed
            : new Vector3(currentMoveVelocity.x, 0f, currentMoveVelocity.z).magnitude;
    }

    private void ProcesarMovimiento(Vector2 input, float speed)
    {
        Vector3 targetDir = new Vector3(input.x, 0f, input.y);
        Vector3 targetVelocity = Vector3.zero;

        if (targetDir.sqrMagnitude >= 0.01f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            targetVelocity = (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward).normalized * speed;
        }

        // Aceleración / desaceleración suave
        float rate = (targetVelocity.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        currentMoveVelocity = Vector3.MoveTowards(currentMoveVelocity, targetVelocity, rate * Time.deltaTime);

        controller.Move(currentMoveVelocity * Time.deltaTime);
    }

    private void IniciarDash(Vector2 input, float dashSpeed)
    {
        isDashing = true;
        dashTimer = dashDuration;
        verticalVelocityY = 0f; // Suspende la caída durante el dash

        Vector3 inputDir = new Vector3(input.x, 0f, input.y);

        if (inputDir.sqrMagnitude >= 0.01f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            dashDirection = (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward).normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }
    }

    /// <summary>Duración total del enfriamiento del Dash (s).</summary>
    public float DashCooldownTotal => dashCooldown;

    /// <summary>Segundos que faltan para poder volver a hacer Dash (0 = listo).</summary>
    public float DashCooldownRestante => Mathf.Max(0f, dashCooldownTimer);

    /// <summary>True mientras el héroe ejecuta un dash (usado por pruebas y puzzles).</summary>
    public bool EstaHaciendoDash => isDashing;

    /// <summary>Velocidad horizontal real del frame (m/s). Alimenta el Animator del héroe.</summary>
    public float VelocidadHorizontal => velocidadHorizontalActual;

    /// <summary>True si el CharacterController está apoyado en el suelo.</summary>
    public bool EnSuelo => controller != null && controller.isGrounded;

    /// <summary>Velocidad vertical real del frame (m/s), para salto y caída.</summary>
    public float VelocidadVertical => verticalVelocityY;

    /// <summary>
    /// Desplazamiento externo (ráfagas de viento, plataformas móviles).
    /// Se aplica mediante CharacterController.Move para no romper la colisión.
    /// </summary>
    public void AplicarEmpujeExterno(Vector3 desplazamiento)
    {
        if (controller == null || !controller.enabled) return;
        controller.Move(desplazamiento);
    }

    /// <summary>
    /// Teletransporte seguro (rescate del abismo, geiser, reaparición).
    /// Desactiva el CharacterController para que la colisión no cancele el
    /// movimiento y, sobre todo, <b>anula la velocidad acumulada</b>: si no se
    /// hiciera, el héroe reaparecería conservando la caída (~25 m/s) y volvería
    /// a cruzar el volumen de muerte antes de poder tocar suelo.
    /// </summary>
    public void Reubicar(Vector3 destino)
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        bool estabaActivo = controller != null && controller.enabled;
        if (estabaActivo) controller.enabled = false;

        transform.position = destino;

        verticalVelocityY = -2f;
        currentMoveVelocity = Vector3.zero;
        velocidadHorizontalActual = 0f;
        isDashing = false;
        dashTimer = 0f;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;

        if (estabaActivo) controller.enabled = true;
    }
}
