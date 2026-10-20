using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Punto único de lectura del New Input System para todo el juego.
/// Cualquier módulo (locomoción, combate, tag-team, HUD) consume este componente
/// en lugar de consultar Keyboard.current / Mouse.current por su cuenta.
/// Se ejecuta antes que el resto ([DefaultExecutionOrder]) para que los valores
/// leídos en el frame estén disponibles para todos los consumidores.
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
    public bool AlternarRecursos { get; private set; }
    public bool AlternarHabilidades { get; private set; }
    public bool AlternarCursor { get; private set; }

    /// <summary>Reaparición tras la derrota de ambos héroes ('R').</summary>
    public bool ReinicioPulsado { get; private set; }

    /// <summary>-1 = sin relevo este frame; 0 = Caelen; 1 = Illya.</summary>
    public int RelevoSolicitado { get; private set; } = -1;

    /// <summary>
    /// Bloqueo temporal de la entrada de combate. Lo activa el gestor de derrota
    /// mientras "CAÍDO EN COMBATE" está en pantalla, para que el héroe caído no
    /// siga atacando. La tecla 'R' se sigue leyendo para poder reiniciar.
    /// </summary>
    public bool EntradaBloqueada { get; set; }

    /// <summary>
    /// El cursor bloqueado indica "modo juego". Con el cursor libre el jugador
    /// está interactuando con la UI, por lo que los botones de combate se ignoran.
    /// </summary>
    public bool EnModoJuego => Cursor.lockState == CursorLockMode.Locked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        ReiniciarPulsaciones();

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

        // ---- Cámara orbital ----
        deltaRaton = (raton != null && EnModoJuego) ? raton.delta.ReadValue() : Vector2.zero;
        AlternarCursor = teclado.escapeKey.wasPressedThisFrame;

        // ---- Locomoción ----
        SaltoPulsado = !bloqueada && teclado.spaceKey.wasPressedThisFrame;
        DashPulsado = !bloqueada && teclado.leftShiftKey.wasPressedThisFrame;

        // ---- Relevo Tag-Team ----
        if (teclado.digit1Key.wasPressedThisFrame) RelevoSolicitado = 0;
        if (teclado.digit2Key.wasPressedThisFrame) RelevoSolicitado = 1;

        // ---- Interacción de entorno ----
        InteraccionPulsada = teclado.fKey.wasPressedThisFrame;

        // ---- Ventanas modales de HUD ----
        AlternarRecursos = teclado.mKey.wasPressedThisFrame;
        AlternarHabilidades = teclado.kKey.wasPressedThisFrame;

        // ---- Combate (solo con el cursor bloqueado para no disparar al hacer clic en la UI) ----
        if (EnModoJuego && !bloqueada)
        {
            AtaquePulsado = raton != null && raton.leftButton.wasPressedThisFrame;
            HabilidadPulsada = teclado.eKey.wasPressedThisFrame
                               || (raton != null && raton.rightButton.wasPressedThisFrame);
        }
    }

    private void ReiniciarPulsaciones()
    {
        SaltoPulsado = false;
        DashPulsado = false;
        AtaquePulsado = false;
        HabilidadPulsada = false;
        InteraccionPulsada = false;
        AlternarRecursos = false;
        AlternarHabilidades = false;
        AlternarCursor = false;
        ReinicioPulsado = false;
        RelevoSolicitado = -1;
    }
}
