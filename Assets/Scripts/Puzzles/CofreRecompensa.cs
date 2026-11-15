using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Cofre de recompensa de `04_Primera Recompensa`.
/// El collider del cofre es un Trigger: al entrar el jugador se publica el aviso
/// "Presiona [F] para abrir" en el banner de instrucciones del HUD y al salir se limpia.
/// Una única pulsación de 'F' dentro del Trigger reproduce el clip de apertura con el
/// componente <see cref="Animation"/> (el clip viene embebido en el modelo del cofre),
/// late la intensidad del material del halo 0 -> 1 -> 0 y desactiva la barrera de sello.
/// No usa Animator ni crea elementos de interfaz.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class CofreRecompensa : MonoBehaviour
{
    [Header("Banner de Instrucciones del HUD")]
    [Tooltip("GameObject 'Banner_Instrucciones' del Canvas_HUD.")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Presiona [F] para abrir";

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Apertura (Animation, sin Animator)")]
    [SerializeField] private Animation animacionCofre;
    [SerializeField] private string nombreClipApertura = "CofreAbrir";

    [Header("Halo (intensidad del material)")]
    [Tooltip("Renderer hijo 'Halo' con el material Shader Graphs/HaloGlow.")]
    [SerializeField] private Renderer halo;
    [SerializeField] private string propiedadIntensidad = "_Intensity";
    [SerializeField] private float intensidadMaxima = 1f;
    [SerializeField] private float duracionPulso = 1.5f;

    [Header("Bloqueo")]
    [Tooltip("GameObject 'Barrera_Sello' que se desactiva al abrir el cofre.")]
    [SerializeField] private GameObject barreraSello;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool abierto;

    private Material materialHalo;
    private int idIntensidad;
    private Coroutine rutinaApertura;

    /// <summary>True una vez consumida la apertura: el cofre ya no ofrece el aviso.</summary>
    public bool Abierto => abierto;

    private void Awake()
    {
        idIntensidad = Shader.PropertyToID(propiedadIntensidad);

        if (halo != null)
        {
            // Instancia propia del material: el halo de este cofre no altera otros materiales.
            materialHalo = halo.material;
        }

        AplicarIntensidad(0f);
        OcultarBanner();
    }

    private void Update()
    {
        if (!jugadorDentro) return;

        // El aviso se reafirma cada frame mientras el jugador siga dentro del Trigger,
        // de modo que ningún otro sistema pueda dejar otro texto encima.
        if (!abierto) MostrarBanner();

        if (abierto || rutinaApertura != null) return;
        if (entrada == null || !entrada.InteraccionPulsada) return;

        IniciarApertura();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (abierto) return;

        MostrarBanner();
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
        AplicarIntensidad(0f);
    }

    // ------------------------------------------------------------------ apertura

    private void IniciarApertura()
    {
        if (abierto) return;

        abierto = true;
        OcultarBanner();

        rutinaApertura = StartCoroutine(RutinaApertura());
    }

    private IEnumerator RutinaApertura()
    {
        // a) Clip de apertura con el componente Animation (legacy, embebido en el modelo).
        if (animacionCofre != null)
        {
            if (string.IsNullOrEmpty(nombreClipApertura)) animacionCofre.Play();
            else animacionCofre.Play(nombreClipApertura);
        }
        else
        {
            Debug.LogWarning($"[COFRE] {name}: no hay componente Animation asignado; el cofre no se abre.");
        }

        // b) Latido 0 -> 1 -> 0 de la intensidad del halo.
        float t = 0f;
        while (t < duracionPulso)
        {
            t += Time.deltaTime;
            float avance = Mathf.Clamp01(t / duracionPulso);
            AplicarIntensidad(Mathf.Sin(avance * Mathf.PI) * intensidadMaxima);
            yield return null;
        }
        AplicarIntensidad(0f);

        // c) Desbloqueo del paso.
        if (barreraSello != null) barreraSello.SetActive(false);

        rutinaApertura = null;
        Debug.Log($"[COFRE] {name}: abierto (clip '{nombreClipApertura}'), halo devuelto a 0 y barrera liberada.");
    }

    // ------------------------------------------------------------------ utilidades

    private void AplicarIntensidad(float valor)
    {
        if (materialHalo != null) materialHalo.SetFloat(idIntensidad, valor);
    }

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[COFRE] {name}: el collider debe estar en modo Trigger.", this);
    }
#endif
}
