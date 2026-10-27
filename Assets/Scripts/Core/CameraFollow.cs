using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Cámara orbital en tercera persona con seguimiento suave, SphereCast
/// anti-oclusión y screen shake.
///
/// TODO el cálculo (lectura de ratón, suavizado angular, seguimiento y
/// posicionamiento) ocurre en LateUpdate, de modo que la cámara se evalúa
/// DESPUÉS de que el CharacterController haya movido al héroe. Esto elimina el
/// temblor (jitter) que aparecía al repartir el trabajo entre Update y
/// LateUpdate con dos relojes distintos.
///
/// El cursor permanece libre y visible. La rotación se congela mientras el
/// puntero está sobre el HUD (con 25 px de margen alrededor de cada contenedor
/// y de los modales) o mientras hay un modal abierto, y se congela por completo
/// durante la derrota.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Objetivo")]
    public Transform target;
    [Tooltip("Altura del pivote de seguimiento sobre la base del héroe (Height Offset).")]
    public Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);
    [Tooltip("Punto al que mira la cámara: torso/hombros del héroe (LookAt Pivot Offset).")]
    public Vector3 pivoteMirada = new Vector3(0f, 1.3f, 0f);
    public float followSmoothTime = 0.08f;

    [Header("Control Orbital (Mouse Smoothing)")]
    public float mouseSensitivity = 0.12f;
    public float rotationSmoothTime = 0.05f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 65f;

    [Header("Distancias y Oclusión Física")]
    [Tooltip("Longitud del brazo de cámara (Camera Distance / Arm Length).")]
    public float defaultDistance = 4.5f;
    public float minDistance = 1.0f;
    public float distanceSmoothSpeed = 15f;
    public float sphereRadius = 0.25f;
    public LayerMask collisionLayers = ~0;

    [Header("Encuadre")]
    [Tooltip("Field of View aplicado a la cámara en Start.")]
    public float fieldOfView = 60f;

    [Header("Detección de UI")]
    [Tooltip("Contenedores y modales del HUD. Se aplica 'margenUI' px alrededor de cada uno " +
             "para pausar la rotación antes de que el puntero toque un botón.")]
    public RectTransform[] zonasHUD;
    [Tooltip("Padding en píxeles alrededor de cada zona del HUD y de los modales.")]
    public float margenUI = 25f;

    [Header("Derrota")]
    [Tooltip("GameObject 'Modal_Derrota'. Mientras esté activo la cámara queda congelada.")]
    public GameObject modalDerrota;

    /// <summary>
    /// La activa HUDCursorDetector mientras el puntero está sobre un menú:
    /// congela la lectura de rotación para que la cámara no gire al desplazar
    /// el ratón hacia la interfaz.
    /// </summary>
    public bool RotacionPausada { get; set; }

    [Header("Derrota (referencia en el Player)")]
    [SerializeField] private GestorDerrotaHeroe gestorDerrota;

    [Header("Entrada (asignada en el Inspector)")]
    [Tooltip("PlayerInputReader del Player. No se busca en runtime.")]
    [SerializeField] private PlayerInputReader entrada;

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

    private static readonly Vector3[] esquinas = new Vector3[4];

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // El cursor no se bloquea nunca: libre y visible en todo momento.
        AsegurarCursorLibre();

        currentDistance = defaultDistance;
        if (target != null) currentTargetPos = target.position + targetOffset;

        Camera camara = GetComponent<Camera>();
        if (camara != null) camara.fieldOfView = fieldOfView;
    }

    /// <summary>
    /// Único punto de cálculo de la cámara. Se ejecuta después de que el
    /// CharacterController haya integrado el movimiento del héroe.
    /// </summary>
    private void LateUpdate()
    {
        if (target == null) return;

        // Derrota: la cámara se congela por completo. Ni rotación ni
        // interpolaciones dependientes del tiempo con Time.timeScale = 0.
        if (DerrotaCongelada()) return;

        AsegurarCursorLibre();

        float dt = (Time.timeScale > 0f) ? Time.deltaTime : Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        // 1. Rotación orbital desde el lector central de Input System.
        if (entrada != null && !RotacionPausada && !PunteroSobreUI())
        {
            Vector2 delta = entrada.DeltaRaton;
            targetYaw += delta.x * mouseSensitivity;
            targetPitch -= delta.y * mouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);

            // El yaw se mantiene en (-180, 180] para no acumular vueltas enteras.
            targetYaw = Mathf.Repeat(targetYaw + 180f, 360f) - 180f;
        }

        // 2. Suavizado angular con un único reloj (el de este LateUpdate).
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime, Mathf.Infinity, dt);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime, Mathf.Infinity, dt);

        if (shakeTimer > 0f) shakeTimer -= dt;

        // 3. Seguimiento: pivote 1.8 m sobre la base del héroe.
        Vector3 puntoSeguimiento = target.position + targetOffset;
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, puntoSeguimiento, ref followVelocity,
            followSmoothTime, Mathf.Infinity, dt);

        // 4. Brazo orbital de 4.5 m con SphereCast anti-muros.
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 cameraDir = rotation * -Vector3.forward;

        float targetDist = defaultDistance;
        if (Physics.SphereCast(currentTargetPos, sphereRadius, cameraDir, out RaycastHit hit,
                defaultDistance, collisionLayers))
        {
            targetDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, defaultDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDist, Mathf.Clamp01(dt * distanceSmoothSpeed));

        // 5. Posición final + Screen Shake, mirando al pivote de torso/hombros.
        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimer > 0f) shakeOffset = Random.insideUnitSphere * shakeMagnitude;

        transform.position = currentTargetPos + (cameraDir * currentDistance) + shakeOffset;
        transform.LookAt(target.position + pivoteMirada);
    }

    /// <summary>Sacudida de cámara usada por combate, habilidad y relevo.</summary>
    public void TriggerScreenShake(float duration, float magnitude)
    {
        if (DerrotaCongelada()) return;

        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    /// <summary>True si la pantalla de derrota está en pantalla.</summary>
    public bool DerrotaCongelada()
    {
        if (modalDerrota != null && modalDerrota.activeInHierarchy) return true;
        return gestorDerrota != null && gestorDerrota.DerrotaActiva;
    }

    private static void AsegurarCursorLibre()
    {
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    /// <summary>
    /// True cuando el puntero debe detener la rotación: hover real sobre UI,
    /// zona de HUD ampliada 25 px, o modal abierto.
    /// </summary>
    private bool PunteroSobreUI()
    {
        if (HUDCursorDetector.PunteroSobreUI) return true;
        if (UIModalWindow.HayModalAbierto) return true;

        EventSystem sistema = EventSystem.current;
        if (sistema != null && sistema.IsPointerOverGameObject()) return true;

        return PunteroEnZonaAmpliada();
    }

    /// <summary>
    /// Comprueba el puntero contra los contenedores y modales del HUD aplicando
    /// 'margenUI' píxeles de padding, para que la cámara se detenga ANTES de
    /// que el cursor llegue a tocar un botón.
    /// </summary>
    private bool PunteroEnZonaAmpliada()
    {
        if (zonasHUD == null || zonasHUD.Length == 0) return false;

        UnityEngine.InputSystem.Mouse raton = UnityEngine.InputSystem.Mouse.current;
        if (raton == null) return false;

        Vector2 pantalla = raton.position.ReadValue();

        Camera camaraUI = null;
        if (zonasHUD[0] != null)
        {
            Canvas canvas = zonasHUD[0].GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camaraUI = canvas.worldCamera;
        }

        for (int i = 0; i < zonasHUD.Length; i++)
        {
            RectTransform zona = zonasHUD[i];
            if (zona == null || !zona.gameObject.activeInHierarchy) continue;

            zona.GetWorldCorners(esquinas);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camaraUI, esquinas[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camaraUI, esquinas[2]);

            Rect rect = Rect.MinMaxRect(
                Mathf.Min(min.x, max.x) - margenUI,
                Mathf.Min(min.y, max.y) - margenUI,
                Mathf.Max(min.x, max.x) + margenUI,
                Mathf.Max(min.y, max.y) + margenUI);

            if (rect.Contains(pantalla)) return true;
        }

        return false;
    }
}
