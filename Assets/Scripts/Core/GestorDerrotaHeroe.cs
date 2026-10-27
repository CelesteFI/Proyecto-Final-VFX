using TMPro;
using UnityEngine;

/// <summary>
/// Sistema de derrota y reaparición del GDD.
/// Activa el panel 'Modal_Derrota' YA EXISTENTE en la Hierarchy: este script no
/// construye ningún elemento de interfaz.
/// Distingue dos desenlaces:
///   1. Cae Caelen sin haber rescatado a Illya -> no hay relevo, derrota directa.
///   2. Caen ambos héroes -> derrota total.
/// El botón 'Btn_Reiniciar' del panel llama a Reaparecer(), y la tecla 'R' se
/// mantiene como atajo opcional.
/// </summary>
[DisallowMultipleComponent]
public class GestorDerrotaHeroe : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [SerializeField] private TagTeamManager tagTeam;
    [SerializeField] private PlayerController controlador;
    [SerializeField] private PlayerInputReader entrada;

    [Header("Interfaz Física de Derrota (en Canvas_HUD)")]
    [Tooltip("GameObject 'Modal_Derrota'. Debe estar desactivado en la Hierarchy.")]
    [SerializeField] private GameObject panelDerrota;
    [Tooltip("Texto 'Texto_Derrota' del modal.")]
    [SerializeField] private TextMeshProUGUI textoDerrota;

    [Header("Mensajes contextuales")]
    [Tooltip("Se escribe solo cuando la derrota es exclusiva de Caelen (Illya sigue sellada).")]
    [SerializeField] private string mensajeCaelenSolo = "Caelen ha caído. El bosque reclama su juramento.";
    [Tooltip("Se escribe cuando ambos héroes han caído.")]
    [SerializeField] private string mensajeAmbosCaidos = "Caelen e Illya han caído. El bosque reclama sus juramentos.";

    [Header("Reinicio de Combate")]
    [Tooltip("Radio en el que se devuelven los enemigos a su guardia al reaparecer.")]
    [SerializeField] private float radioReinicioEnemigos = 80f;

    [Header("Pausa del Modal")]
    [Tooltip("Si está activo, la derrota congela el juego con Time.timeScale = 0.")]
    [SerializeField] private bool congelarJuego = true;

    /// <summary>True mientras la pantalla de derrota está activa.</summary>
    public bool DerrotaActiva => derrotaActiva;

    private bool derrotaActiva;

    private void Awake()
    {
        if (tagTeam == null) tagTeam = GetComponent<TagTeamManager>();
        if (controlador == null) controlador = GetComponent<PlayerController>();
        if (entrada == null) entrada = GetComponent<PlayerInputReader>();

        OcultarDerrota();
    }

    private void Start()
    {
        if (tagTeam == null) return;

        tagTeam.OnAmbosCaidos += MostrarDerrotaAmbos;
        tagTeam.OnDerrotaSinIllya += MostrarDerrotaCaelenSolo;
    }

    private void OnDestroy()
    {
        if (tagTeam != null)
        {
            tagTeam.OnAmbosCaidos -= MostrarDerrotaAmbos;
            tagTeam.OnDerrotaSinIllya -= MostrarDerrotaCaelenSolo;
        }

        if (entrada != null) entrada.EntradaBloqueada = false;
    }

    private void Update()
    {
        if (!derrotaActiva) return;
        if (entrada == null || !entrada.ReinicioPulsado) return;

        Reaparecer();
    }

    // ------------------------------------------------------------------
    // DERROTA
    // ------------------------------------------------------------------

    /// <summary>Cae Caelen y Illya sigue sellada: no hay relevo posible.</summary>
    public void MostrarDerrotaCaelenSolo()
    {
        MostrarPanel(mensajeCaelenSolo);
    }

    /// <summary>Cae también el segundo héroe: derrota total.</summary>
    public void MostrarDerrotaAmbos()
    {
        MostrarPanel(mensajeAmbosCaidos);
    }

    private void MostrarPanel(string mensaje)
    {
        derrotaActiva = true;

        if (entrada != null) entrada.EntradaBloqueada = true;

        if (textoDerrota != null) textoDerrota.text = mensaje;

        if (panelDerrota != null) panelDerrota.SetActive(true);

        if (congelarJuego) Time.timeScale = 0f;

        Debug.Log("[DERROTA] " + mensaje + " Usa 'Btn_Reiniciar' o pulsa 'R' para reaparecer.");
    }

    private void OcultarDerrota()
    {
        derrotaActiva = false;
        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (congelarJuego) Time.timeScale = 1f;
    }

    /// <summary>Cierra el panel sin reaparecer (botón de cierre del modal).</summary>
    public void CerrarPanelDerrota()
    {
        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (congelarJuego) Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------
    // REAPARICIÓN
    // ------------------------------------------------------------------

    /// <summary>
    /// Devuelve al héroe al punto de inicio con la salud completa, reinicia a los
    /// enemigos del radio y deja el progreso persistido a cero (derrota total).
    /// </summary>
    public void Reaparecer()
    {
        OcultarDerrota();

        if (entrada != null) entrada.EntradaBloqueada = false;

        Vector3 destino = (tagTeam != null) ? tagTeam.puntoReaparicion : Vector3.zero;

        // El teleport debe anular la velocidad acumulada del CharacterController.
        if (controlador != null) controlador.Reubicar(destino);
        else transform.position = destino;

        if (tagTeam != null) tagTeam.Reaparecer();

        ReiniciarEnemigosCercanos(destino);

        // Derrota total => el progreso persistido vuelve a cero.
        GameSaveState.BorrarTodo();
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ResetearProgreso();

        if (CombatHUDManager.Instance != null) CombatHUDManager.Instance.RefrescarReservas();

        Debug.Log($"[DERROTA] Reaparición en {destino} con ambos héroes al 100 % de vida y progreso reiniciado.");
    }

    /// <summary>Devuelve a su guardia a los enemigos dentro del radio indicado.</summary>
    private void ReiniciarEnemigosCercanos(Vector3 centro)
    {
        EnemyAI[] enemigos = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        float radioCuadrado = radioReinicioEnemigos * radioReinicioEnemigos;
        int reiniciados = 0;

        for (int i = 0; i < enemigos.Length; i++)
        {
            if (enemigos[i] == null) continue;
            if ((enemigos[i].transform.position - centro).sqrMagnitude > radioCuadrado) continue;

            enemigos[i].ReiniciarEstado();
            reiniciados++;
        }

        Debug.Log($"[DERROTA] {reiniciados} enemigo(s) devueltos a su zona de guardia.");
    }
}
