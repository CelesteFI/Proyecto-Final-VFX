using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Tótem rúnico del bloque `06_Puzzle_Lore` (Nivel 1).
/// Su collider es un Trigger: al entrar el héroe se publica «Presiona [F] para encender»
/// en el banner de instrucciones del HUD. Una única pulsación de 'F' enciende el tótem
/// (intensidad 0 -> 1 del material `Fuego  Azul` mediante <see cref="MaterialPropertyBlock"/>,
/// de modo que cada tótem es independiente y no instancia materiales), avisa al
/// <see cref="PuzzleLoreTotems"/> de la zona y, si el tótem tiene lore, entrega su
/// fragmento a <see cref="LoreUI"/> (que abre `Modal_Lore` y refresca el contador).
/// El banner se oculta de forma permanente para un tótem ya encendido.
/// No construye interfaz ni busca referencias en runtime.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TotemLore : MonoBehaviour
{
    [Header("Identidad del Fragmento")]
    [Tooltip("Identificador único persistente (PlayerPrefs).")]
    [SerializeField] private string idFragmento = "N1_Totem_06_01";
    [SerializeField] private string titulo = "Fragmento Rúnico";

    [Tooltip("Lore del tótem. Si está vacío, el tótem se enciende pero no entrega fragmento.")]
    [TextArea(3, 10)]
    [SerializeField] private string cuerpo = string.Empty;

    [Header("Banner de Instrucciones del HUD")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Presiona [F] para encender";

    [Header("Fuego Azul (MaterialPropertyBlock por tótem)")]
    [Tooltip("Quads con el material 'Fuego  Azul'. Cada uno recibe su propio bloque de propiedades.")]
    [SerializeField] private Renderer[] visualesFuego = new Renderer[0];
    [SerializeField] private string propiedadIntensidad = "_Intensity";
    [SerializeField] private float intensidadEncendido = 1f;

    [Header("Luz Azul del Tótem")]
    [Tooltip("Point Light hija del tótem. Arranca apagada y solo se enciende con la 'F'.")]
    [SerializeField] private Light luzTotem;
    [SerializeField] private float intensidadLuz = 5f;

    [Header("Entrada y Gestor de la Zona")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;
    [SerializeField] private PuzzleLoreTotems gestor;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool yaEncendido;

    /// <summary>Se dispara la primera vez que el tótem se enciende.</summary>
    public event Action<TotemLore> OnLeido;

    public string IdFragmento => idFragmento;
    public string Titulo => titulo;
    public string Cuerpo => cuerpo;

    /// <summary>True si el tótem tiene un fragmento de lore que entregar.</summary>
    public bool TieneLore => !string.IsNullOrWhiteSpace(cuerpo);

    public bool YaEncendido => yaEncendido;
    public bool JugadorDentro => jugadorDentro;

    /// <summary>Alias histórico de <see cref="YaEncendido"/> (lo consulta SelloNarrativo).</summary>
    public bool Leido => yaEncendido;

    public bool PuedeInteractuar => !yaEncendido;
    public string TextoPrompt => yaEncendido ? $"«{titulo}» ya encendido" : mensajeEntrada;

    private MaterialPropertyBlock bloque;
    private int idIntensidad;

    private void Awake()
    {
        idIntensidad = Shader.PropertyToID(propiedadIntensidad);

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        if (luzTotem == null) luzTotem = GetComponentInChildren<Light>(true);

        // Todos los tótems arrancan apagados: la intensidad se escribe por bloque de
        // propiedades, así que el material compartido no se modifica.
        AplicarIntensidad(yaEncendido ? intensidadEncendido : 0f);
        AplicarLuz(yaEncendido);
        OcultarBanner();
    }

    private void Update()
    {
        if (!jugadorDentro || yaEncendido) return;

        // Con un modal en pantalla el aviso se retira y 'F' no enciende nada.
        if (UIModalWindow.HayModalAbierto)
        {
            OcultarBanner();
            return;
        }

        // El aviso se reafirma cada frame para que ningún otro sistema lo pise.
        MostrarBanner();

        if (entrada != null && entrada.InteraccionPulsada) Encender();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (yaEncendido || UIModalWindow.HayModalAbierto) return;

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
    }

    /// <summary>
    /// Enciende el tótem una única vez: fuego azul a 1, aviso retirado y registro en el
    /// gestor de la zona (que actualiza contadores y abre el lore si corresponde).
    /// </summary>
    public void Encender()
    {
        if (yaEncendido) return;

        yaEncendido = true;
        OcultarBanner();
        AplicarIntensidad(intensidadEncendido);
        AplicarLuz(true);

        if (gestor != null) gestor.RegistrarEncendido(this);
        else Debug.LogWarning($"[TÓTEM] {name}: no hay gestor de zona asignado; el encendido no cuenta.");

        OnLeido?.Invoke(this);

        Debug.Log($"[TÓTEM] {name} encendido (lore={(TieneLore ? "'" + titulo + "'" : "sin fragmento")}).");
    }

    /// <summary>Compatibilidad con las herramientas de diagnóstico anteriores.</summary>
    public void Interactuar(Transform actor)
    {
        Encender();
    }

    /// <summary>Fuerza el estado encendido/apagado (restauración o pruebas automáticas).</summary>
    public void MarcarLeido(bool valor)
    {
        yaEncendido = valor;
        AplicarIntensidad(yaEncendido ? intensidadEncendido : 0f);
        AplicarLuz(yaEncendido);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void ConfigurarTotem(string id, string tituloNuevo, string cuerpoNuevo, Renderer[] fuegos,
                                PlayerInputReader input, PuzzleLoreTotems gestorZona,
                                GameObject banner, TextMeshProUGUI txtBanner, string mensaje)
    {
        idFragmento = id;
        titulo = string.IsNullOrWhiteSpace(tituloNuevo) ? "Fragmento Rúnico" : tituloNuevo;
        cuerpo = cuerpoNuevo ?? string.Empty;
        visualesFuego = fuegos ?? new Renderer[0];
        entrada = input;
        gestor = gestorZona;
        bannerInstrucciones = banner;
        textoInstruccion = txtBanner;
        mensajeEntrada = string.IsNullOrWhiteSpace(mensaje) ? "Presiona [F] para encender" : mensaje;

        AplicarIntensidad(yaEncendido ? intensidadEncendido : 0f);
    }

    /// <summary>Compatibilidad con el integrador histórico (firma anterior a la zona 06).</summary>
    public void Configurar(string id, string tituloNuevo, string cuerpoNuevo, string textoPrompt,
                           LoreUI ui, Renderer visualTotem, GameObject marca)
    {
        idFragmento = id;
        titulo = string.IsNullOrWhiteSpace(tituloNuevo) ? "Fragmento Rúnico" : tituloNuevo;
        cuerpo = cuerpoNuevo ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(textoPrompt)) mensajeEntrada = textoPrompt;
        if (visualTotem != null) visualesFuego = new[] { visualTotem };
    }

    // ------------------------------------------------------------------ utilidades

    /// <summary>Enciende o apaga la Point Light hija del tótem (arranca apagada).</summary>
    private void AplicarLuz(bool encendida)
    {
        if (luzTotem == null) return;

        luzTotem.intensity = encendida ? intensidadLuz : 0f;
        luzTotem.enabled = encendida;
    }

    /// <summary>Configuración de la luz desde el integrador de escena.</summary>
    public void ConfigurarLuz(Light luz, float intensidad)
    {
        luzTotem = luz;
        if (intensidad > 0f) intensidadLuz = intensidad;
        AplicarLuz(yaEncendido);
    }

    private void AplicarIntensidad(float valor)
    {
        if (visualesFuego == null || visualesFuego.Length == 0) return;
        if (bloque == null) bloque = new MaterialPropertyBlock();
        if (idIntensidad == 0) idIntensidad = Shader.PropertyToID(propiedadIntensidad);

        for (int i = 0; i < visualesFuego.Length; i++)
        {
            Renderer r = visualesFuego[i];
            if (r == null) continue;

            r.GetPropertyBlock(bloque);
            bloque.SetFloat(idIntensidad, valor);
            r.SetPropertyBlock(bloque);
        }
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
            Debug.LogWarning($"[TÓTEM] {name}: el collider debe estar en modo Trigger.", this);
    }
#endif
}
