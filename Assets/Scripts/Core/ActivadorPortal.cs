using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

/// <summary>
/// Interruptor del portal de salida del Nivel 1 (`Activador_Portal`, hijo visible del
/// tótem del umbral en `08_Altar_Arbol_Sombrio_BossArena`).
/// Su collider es un Trigger: al entrar el héroe publica «Resuena con el elemento piedra [F]»
/// en el banner de instrucciones del HUD y, al salir, lo oculta. Una única pulsación de 'F':
///   1) oculta el banner;
///   2) activa el VFX `Activar Portal` y le da Play();
///   3) cambia el material del ALTAR por `Altar_Encendido`, el único que trae la EMISSION
///      encendida (el tótem NO enciende el suyo). Sin lerps ni materiales de copia;
///   4) habilita el collider Trigger de transición que vive en el `altar`, que al cruzarlo
///      carga la escena siguiente (<see cref="TransicionAltarNivel1"/>).
/// No crea ni destruye objetos: el aviso propio manda.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ActivadorPortal : MonoBehaviour
{
    [Header("Banner de Instrucciones del HUD")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Resuena con el elemento piedra [F]";

    [Header("VFX del Portal")]
    [Tooltip("GameObject 'Activar_Portal'. Arranca desactivado en la escena.")]
    [SerializeField] private GameObject vfxActivarPortal;

    [Header("Altar Encendido (material del ALTAR, no del tótem)")]
    [Tooltip("Renderer del objeto `altar`. Arranca con el material `altar`, que trae la emisión apagada.")]
    [SerializeField] private Renderer altarRenderer;
    [Tooltip("Material 'Altar_Encendido': el único de la carpeta Rewards con la keyword _EMISSION y el color de emisión ya encendidos.")]
    [SerializeField] private Material materialAltarEncendido;

    [Header("Transición en el Altar")]
    [Tooltip("Collider Trigger del `altar`. Inicia desactivado y se habilita al activar el portal.")]
    [SerializeField] private Collider triggerAltar;

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Consecuencias (Inspector)")]
    [Tooltip("Se invoca una sola vez al activarse el altar. El final del Nivel 2 lo usa para abrir el Modal_Victoria.")]
    [SerializeField] private UnityEvent alActivarse = new UnityEvent();

    [Header("Retardo de las Consecuencias (solo UI)")]
    [Tooltip("Segundos que se espera antes de invocar 'alActivarse'. El VFX del portal, el cambio de material del altar " +
             "y el Trigger de paso ocurren SIEMPRE al instante: aquí solo se retrasa la UI (el Modal_Victoria). 0 = sin retardo.")]
    [SerializeField] private float retardoConsecuencias = 0f;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool activado;

    public bool Activado => activado;
    public bool JugadorDentro => jugadorDentro;

    private void Awake()
    {
        BoxCollider caja = GetComponent<BoxCollider>();
        if (caja != null) caja.isTrigger = true;

        OcultarBanner();

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

        // 2. El altar pasa a su material encendido (el tótem no se ilumina).
        EncenderAltar();

        // 3. Se habilita el Trigger de transición del altar (al cruzarlo carga la escena 2).
        if (triggerAltar != null) triggerAltar.enabled = true;

        // 4. Consecuencias declaradas en el Inspector (en el Nivel 2, el Modal_Victoria).
        //    El material del altar ya está cambiado en el paso 2: si hay retardo, solo se
        //    retrasa la UI, nunca el encendido del altar.
        if (retardoConsecuencias > 0f) StartCoroutine(InvocarConsecuenciasConRetardo());
        else alActivarse?.Invoke();

        Debug.Log($"[PORTAL] El portal se ha activado y el altar admite el paso. Consecuencias de UI en {retardoConsecuencias:F1}s.");
    }

    /// <summary>Retrasa únicamente la UI de las consecuencias (p. ej. el Modal_Victoria).</summary>
    private System.Collections.IEnumerator InvocarConsecuenciasConRetardo()
    {
        yield return new WaitForSeconds(retardoConsecuencias);
        alActivarse?.Invoke();
    }

    /// <summary>
    /// Sustituye el material del altar por `Altar_Encendido`, que ya trae la keyword
    /// _EMISSION activa y `_EmissionColor` brillante. Se cambia la referencia en el
    /// renderer (sharedMaterials) para no tocar el asset compartido ni crear copias.
    /// </summary>
    private void EncenderAltar()
    {
        if (altarRenderer == null || materialAltarEncendido == null) return;

        Material[] materiales = altarRenderer.sharedMaterials;
        for (int i = 0; i < materiales.Length; i++) materiales[i] = materialAltarEncendido;
        altarRenderer.sharedMaterials = materiales;
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
                           PlayerInputReader input)
    {
        bannerInstrucciones = banner;
        textoInstruccion = txtBanner;
        if (!string.IsNullOrWhiteSpace(mensaje)) mensajeEntrada = mensaje;
        vfxActivarPortal = vfxPortal;
        altarRenderer = altar;
        triggerAltar = triggerEscena;
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
