using TMPro;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Interruptor del portal de salida del Nivel 1 (`Activador_Portal`, hijo visible del
/// tótem del umbral en `08_Altar_Arbol_Sombrio_BossArena`).
/// Su collider es un Trigger: al entrar el héroe publica «Activa el portal [F]» en el banner
/// de instrucciones del HUD y, al salir, lo oculta. Una única pulsación de 'F':
///   1) oculta el banner;
///   2) activa el VFX `Activar Portal` y le da Play();
///   3) enciende la emisión del material del `altar` (por MaterialPropertyBlock,
///      sin instanciar materiales);
///   4) abre el modal de pista con «El nexo primordial resuena…»;
///   5) habilita el collider Trigger de transición que vive en el `altar`.
/// No crea ni destruye objetos y no implementa IInteractuable: el aviso propio manda.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ActivadorPortal : MonoBehaviour
{
    [Header("Banner de Instrucciones del HUD")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Activa el portal [F]";

    [Header("VFX del Portal")]
    [Tooltip("GameObject 'Activar_Portal'. Arranca desactivado en la escena.")]
    [SerializeField] private GameObject vfxActivarPortal;

    [Header("Emisión del Altar")]
    [Tooltip("Renderer del objeto `altar`. Su emisión arranca apagada en el material.")]
    [SerializeField] private Renderer altarRenderer;
    [SerializeField] private Color colorEmision = new Color(0.15f, 0.85f, 1f, 1f);
    [SerializeField] private float intensidadEmision = 3f;

    [Header("Modal de Pista")]
    [SerializeField] private GameObject modalPista;
    [SerializeField] private TextMeshProUGUI textoPista;
    [TextArea(2, 5)]
    [SerializeField] private string textoLore =
        "El nexo primordial resuena. Entra al portal para salir del bosque sombrío.";

    [Header("Transición en el Altar")]
    [Tooltip("Collider Trigger del `altar`. Inicia desactivado y se habilita al activar el portal.")]
    [SerializeField] private Collider triggerAltar;

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool activado;

    public bool Activado => activado;
    public bool JugadorDentro => jugadorDentro;

    private MaterialPropertyBlock bloque;
    private int idEmision;

    private void Awake()
    {
        idEmision = Shader.PropertyToID("_EmissionColor");

        BoxCollider caja = GetComponent<BoxCollider>();
        if (caja != null) caja.isTrigger = true;

        OcultarBanner();
        if (modalPista != null) modalPista.SetActive(false);

        // La transición de escena solo existe después de activar el portal.
        if (triggerAltar != null) triggerAltar.enabled = false;
    }

    private void Update()
    {
        if (!jugadorDentro || activado) return;

        // El aviso se reafirma cada frame para que ningún otro sistema lo pise.
        MostrarBanner();

        if (entrada != null && entrada.InteraccionPulsada) Activar();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (!activado) MostrarBanner();
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

    /// <summary>Activa el portal una única vez (idempotente).</summary>
    public void Activar()
    {
        if (activado) return;
        activado = true;

        OcultarBanner();

        // 1. VFX del portal: se enciende y se le da Play().
        if (vfxActivarPortal != null)
        {
            vfxActivarPortal.SetActive(true);

            VisualEffect efecto = vfxActivarPortal.GetComponent<VisualEffect>();
            if (efecto != null) efecto.Play();
        }

        // 2. Emisión del altar por bloque de propiedades (no toca el material compartido).
        EncenderEmisionAltar();

        // 3. Modal de pista.
        if (textoPista != null) textoPista.text = textoLore;
        if (modalPista != null) modalPista.SetActive(true);

        // 4. Se habilita el Trigger de transición del altar.
        if (triggerAltar != null) triggerAltar.enabled = true;

        Debug.Log("[PORTAL] El nexo primordial resuena: el portal se ha activado y el altar admite el paso.");
    }

    private void EncenderEmisionAltar()
    {
        if (altarRenderer == null) return;

        if (bloque == null) bloque = new MaterialPropertyBlock();
        if (idEmision == 0) idEmision = Shader.PropertyToID("_EmissionColor");

        altarRenderer.GetPropertyBlock(bloque);
        bloque.SetColor(idEmision, colorEmision * Mathf.Max(0f, intensidadEmision));
        altarRenderer.SetPropertyBlock(bloque);
    }

    // ------------------------------------------------------------------ utilidades

    private void MostrarBanner()
    {
        if (bannerInstrucciones != null && !bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(true);

        if (textoInstruccion != null && textoInstruccion.text != mensajeEntrada)
            textoInstruccion.text = mensajeEntrada;
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
    public void Configurar(GameObject banner, TextMeshProUGUI txtBanner, string mensaje,
                           GameObject vfxPortal, Renderer altar, Collider triggerEscena,
                           GameObject modal, TextMeshProUGUI txtPista, string lore,
                           PlayerInputReader input)
    {
        bannerInstrucciones = banner;
        textoInstruccion = txtBanner;
        if (!string.IsNullOrWhiteSpace(mensaje)) mensajeEntrada = mensaje;
        vfxActivarPortal = vfxPortal;
        altarRenderer = altar;
        triggerAltar = triggerEscena;
        modalPista = modal;
        textoPista = txtPista;
        if (!string.IsNullOrWhiteSpace(lore)) textoLore = lore;
        entrada = input;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[PORTAL] {name}: el collider debe estar en modo Trigger.", this);
    }
#endif
}
