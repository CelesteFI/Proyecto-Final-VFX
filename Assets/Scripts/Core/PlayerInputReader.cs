using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Punto único de lectura del New Input System para todo el juego.
/// Cualquier módulo (locomoción, combate, tag-team, HUD) consume este componente
/// en lugar de consultar Keyboard.current / Mouse.current por su cuenta.
/// Se ejecuta antes que el resto ([DefaultExecutionOrder]) para que los valores
/// leídos en el frame estén disponibles para todos los consumidores.
///
/// Esquema de controles:
///   WASD          movimiento
///   Ratón X/Y     rotación de cámara (siempre activa salvo hover sobre UI)
///   Q             ataque básico
///   Clic derecho  ataque especial / habilidad
///   Espacio       salto (nunca pausa)
///   Shift         dash
///   F             interactuar
///   Tab           relevo Tag-Team (único atajo: '1' y '2' ya no cambian de héroe)
///   Escape        pausa
/// El clic izquierdo queda reservado a la interfaz y a la selección en el mundo.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class PlayerInputReader : MonoBehaviour
{
    public static PlayerInputReader Instance { get; private set; }

    [Header("Lectura del Frame Actual")]
    [SerializeField] private Vector2 movimiento;
    [SerializeField] private Vector2 deltaRaton;

    public Vector2 Movimiento => movimiento;
    public Vector2 DeltaRaton => deltaRaton;

    public bool SaltoPulsado { get; private set; }
    public bool DashPulsado { get; private set; }
    public bool AtaquePulsado { get; private set; }
    public bool HabilidadPulsada { get; private set; }
    public bool InteraccionPulsada { get; private set; }

    /// <summary>Apertura del menú de pausa con 'Escape'. 'Espacio' no interviene.</summary>
    public bool PausaPulsada { get; private set; }

    /// <summary>Reaparición tras la derrota de ambos héroes ('R').</summary>
    public bool ReinicioPulsado { get; private set; }

    /// <summary>
    /// True únicamente en el frame en que se pulsa 'Tab'. El destino del relevo lo
    /// decide TagTeamManager (alterna al héroe que no está activo), de modo que este
    /// lector no necesita conocer el estado del Tag-Team.
    /// </summary>
    public bool RelevoPulsado { get; private set; }

    /// <summary>
    /// Bloqueo temporal de la entrada de combate. Lo activa el gestor de derrota
    /// mientras "CAÍDO EN COMBATE" está en pantalla, para que el héroe caído no
    /// siga atacando. La tecla 'R' se sigue leyendo para poder reiniciar.
    /// </summary>
    public bool EntradaBloqueada { get; set; }

    /// <summary>
    /// True cuando el puntero está sobre un elemento de interfaz interactuable.
    /// Se usa para que el clic derecho, la cámara y la interacción no se disparen
    /// mientras se opera el HUD.
    /// </summary>
    public static bool PunteroSobreUI
    {
        get
        {
            EventSystem sistema = EventSystem.current;
            return sistema != null && sistema.IsPointerOverGameObject();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        AsegurarCursorLibre();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        ReiniciarPulsaciones();

        // El cursor nunca se bloquea: siempre libre y visible.
        AsegurarCursorLibre();

        Keyboard teclado = Keyboard.current;
        Mouse raton = Mouse.current;

        if (teclado == null)
        {
            movimiento = Vector2.zero;
            deltaRaton = Vector2.zero;
            return;
        }

        // El reinicio de partida se lee SIEMPRE, incluso con la entrada bloqueada.
        ReinicioPulsado = teclado.rKey.wasPressedThisFrame;

        bool bloqueada = EntradaBloqueada;
        bool sobreUI = PunteroSobreUI || UIModalWindow.HayModalAbierto;

        // ---- Eje de movimiento (WASD) ----
        float h = 0f;
        float v = 0f;
        if (!bloqueada)
        {
            if (teclado.wKey.isPressed) v += 1f;
            if (teclado.sKey.isPressed) v -= 1f;
            if (teclado.dKey.isPressed) h += 1f;
            if (teclado.aKey.isPressed) h -= 1f;
        }
        movimiento = new Vector2(h, v).normalized;

        // ---- Cámara orbital: el ratón siempre alimenta el delta ----
        // El bloqueo por hover sobre la UI lo aplica CameraFollow con la misma guarda.
        deltaRaton = (raton != null) ? raton.delta.ReadValue() : Vector2.zero;

        // ---- Menú de pausa: exclusivamente Escape ----
        PausaPulsada = teclado.escapeKey.wasPressedThisFrame;

        // ---- Locomoción ----
        SaltoPulsado = !bloqueada && teclado.spaceKey.wasPressedThisFrame;
        DashPulsado = !bloqueada && teclado.leftShiftKey.wasPressedThisFrame;

        // ---- Relevo Tag-Team: exclusivamente Tab ----
        // Las teclas numéricas 1 y 2 quedan libres: ya no solicitan ningún relevo.
        RelevoPulsado = teclado.tabKey.wasPressedThisFrame;

        // ---- Interacción de entorno ----
        InteraccionPulsada = !sobreUI && teclado.fKey.wasPressedThisFrame;

        // ---- Combate: Q = básico, clic derecho = especial. El clic izquierdo es solo UI ----
        if (!bloqueada && !sobreUI)
        {
            AtaquePulsado = teclado.qKey.wasPressedThisFrame;
            HabilidadPulsada = raton != null && raton.rightButton.wasPressedThisFrame;
        }
    }

    /// <summary>Fuerza el cursor libre y visible en cada frame.</summary>
    private static void AsegurarCursorLibre()
    {
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    private void ReiniciarPulsaciones()
    {
        SaltoPulsado = false;
        DashPulsado = false;
        AtaquePulsado = false;
        HabilidadPulsada = false;
        InteraccionPulsada = false;
        PausaPulsada = false;
        ReinicioPulsado = false;
        RelevoPulsado = false;
    }
}
