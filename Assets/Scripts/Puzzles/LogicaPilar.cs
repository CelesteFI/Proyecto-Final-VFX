using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pilar de Resonancia del Nivel 2. Controla las cuatro piezas de un pilar:
///
///   1. El escudo (<c>Aparicion</c>): arranca ACTIVADO y bloquea el acceso al orbe.
///   2. El orbe: visible desde el principio, pero inaccesible hasta retirar el escudo.
///   3. La conexión al puente: empieza apagado y se enciende al recoger el orbe.
///   4. La interacción: al quedar libre, reafirma en el Banner_Instrucciones
///      "Recoge el Astro de Viento [F]" y recoge con 'F'.
///
/// La señal de "Completado" llega desde fuera (MinijuegoFlores al apagar las tres
/// flores, o el UnityEvent 'alCompletar' de un GestorOleada al ganar el combate),
/// de modo que el pilar no conoce el origen de la recompensa.
/// Este script NO escribe Time.timeScale ni crea elementos de interfaz.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class LogicaPilar : MonoBehaviour
{
    [Header("Piezas del Pilar")]
    [Tooltip("VFX 'Aparicion': escudo que bloquea el paso mientras el puzzle o el combate no se ha resuelto.")]
    [SerializeField] private GameObject escudo;
    [Tooltip("Modelo del Orbe del Astro de Viento. Visible pero inaccesible hasta retirar el escudo.")]
    [SerializeField] private GameObject orbe;
    [Tooltip("GameObject de la conexión al puente de la siguiente isla. Arranca apagado.")]
    [SerializeField] private GameObject puente;

    [Header("Interacción")]
    [SerializeField] private string mensajeInteraccion = "Recoge el Astro de Viento [F]";
    [Tooltip("Fracción (0..1) que suma a la Barra_Progreso del HUD al recoger el orbe.")]
    [SerializeField] private float progresoAlRecoger = 0.1f;

    [Header("Enlaces del HUD y la Entrada")]
    [SerializeField] private Canvas_HUD canvasHUD;
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private PlayerInputReader entrada;

    [Header("Consecuencias (Inspector)")]
    [Tooltip("Se invoca una sola vez al retirarse el escudo.")]
    [SerializeField] private UnityEvent alCompletarse = new UnityEvent();
    [Tooltip("Se invoca una sola vez al recoger el orbe.")]
    [SerializeField] private UnityEvent alRecogerse = new UnityEvent();

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool completado;
    [SerializeField] private bool recogido;
    [SerializeField] private bool jugadorDentro;

    public bool Completado => completado;
    public bool Recogido => recogido;

    private void Awake()
    {
        if (entrada == null) entrada = Object.FindFirstObjectByType<PlayerInputReader>();
        if (canvasHUD == null && entrada != null) canvasHUD = entrada.canvasHUD;

        // Se prefiere un BoxCollider dedicado: si el pilar conserva su MeshCollider
        // (la geometría física del pedestal), ese no se convierte en Trigger.
        Collider col = GetComponent<BoxCollider>();
        if (col == null) col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        AplicarEstadoInicial();
    }

    /// <summary>Escudo puesto, orbe visible y puente apagado.</summary>
    private void AplicarEstadoInicial()
    {
        if (escudo != null) escudo.SetActive(true);
        if (orbe != null) orbe.SetActive(true);
        if (puente != null) puente.SetActive(false);

        OcultarBanner();
    }

    private void Update()
    {
        if (!completado || recogido || !jugadorDentro) return;

        // El aviso se reafirma cada frame para que ningún otro sistema lo pise.
        MostrarBanner();

        if (entrada != null && entrada.InteraccionPulsada) Recoger();
    }

    // ------------------------------------------------------------------
    // COMPLETADO (flores o combate)
    // ------------------------------------------------------------------

    /// <summary>
    /// Retira el escudo y habilita la interacción con el orbe. Es el destino de
    /// la señal que envían MinijuegoFlores y el UnityEvent de GestorOleada.
    /// </summary>
    public void Completar()
    {
        if (completado) return;
        completado = true;

        if (escudo != null) escudo.SetActive(false);

        alCompletarse?.Invoke();

        Debug.Log($"[PILAR] {name}: escudo retirado. El orbe ya es accesible.");
    }

    // ------------------------------------------------------------------
    // RECOGIDA DEL ORBE
    // ------------------------------------------------------------------

    private void Recoger()
    {
        if (recogido) return;
        recogido = true;

        if (orbe != null) orbe.SetActive(false);
        if (canvasHUD != null) canvasHUD.SumarProgreso(progresoAlRecoger);
        if (puente != null) puente.SetActive(true);

        OcultarBanner();

        alRecogerse?.Invoke();

        Debug.Log($"[PILAR] {name}: Astro de Viento recogido. Puente habilitado.");
    }

    // ------------------------------------------------------------------
    // DETECCIÓN DEL JUGADOR
    // ------------------------------------------------------------------

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (completado && !recogido) MostrarBanner();
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = false;
        OcultarBanner();
    }

    private void OnDisable()
    {
        jugadorDentro = false;
        OcultarBanner();
    }

    private void MostrarBanner()
    {
        if (bannerInstrucciones != null && !bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(true);

        if (textoInstruccion != null && textoInstruccion.text != mensajeInteraccion)
            textoInstruccion.text = mensajeInteraccion;
    }

    private void OcultarBanner()
    {
        if (bannerInstrucciones != null && bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(false);
    }

    private static bool EsJugador(Collider otro)
    {
        if (otro == null) return false;
        if (otro.CompareTag("Player")) return true;
        return otro.GetComponentInParent<PlayerInputReader>() != null;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(GameObject nuevoEscudo, GameObject nuevoOrbe, GameObject nuevoPuente,
                           Canvas_HUD hud, GameObject banner, TextMeshProUGUI txtBanner,
                           PlayerInputReader input, string mensaje)
    {
        escudo = nuevoEscudo;
        orbe = nuevoOrbe;
        puente = nuevoPuente;
        canvasHUD = hud;
        bannerInstrucciones = banner;
        textoInstruccion = txtBanner;
        entrada = input;
        mensajeInteraccion = mensaje;
    }
}
