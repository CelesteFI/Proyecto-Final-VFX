using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tótem del umbral (`Totem_Final_N1`) de `08_Altar_Arbol_Sombrio_BossArena`.
/// Su collider es un Trigger: al entrar el héroe publica
/// «Activa el portal para salir del bosque oscuro [F]» en el banner de instrucciones
/// del HUD y, al salir, lo oculta. Una única pulsación de 'F' cierra el aviso y carga
/// la siguiente escena del Build Settings con <see cref="SceneManager.LoadScene(int)"/>.
/// No crea ni destruye objetos: banner y entrada ya existen en la escena.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TotemSalidaNivel : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Índice de la escena destino en Build Settings. Maqueta Nivel 2 = 1.")]
    [SerializeField] private int indiceEscenaDestino = 1;

    [Header("Banner de Instrucciones del HUD")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Activa el portal para salir del bosque oscuro [F]";

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool cargando;

    public bool JugadorDentro => jugadorDentro;
    public bool Cargando => cargando;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        OcultarBanner();
    }

    private void Update()
    {
        if (!jugadorDentro || cargando) return;

        // El aviso se reafirma cada frame para que ningún otro sistema lo pise.
        MostrarBanner();

        if (entrada != null && entrada.InteraccionPulsada) Salir();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (!cargando) MostrarBanner();
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

    /// <summary>Cierra el aviso y carga la siguiente escena del Build Settings.</summary>
    public void Salir()
    {
        if (cargando) return;
        cargando = true;

        OcultarBanner();

        if (indiceEscenaDestino < 0 || indiceEscenaDestino >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[UMBRAL] Índice de escena {indiceEscenaDestino} fuera del Build Settings; no se carga nada.");
            cargando = false;
            return;
        }

        // Un modal abierto deja Time.timeScale = 0; la escena nueva debe arrancar limpia.
        Time.timeScale = 1f;

        Debug.Log($"[UMBRAL] Portal activado: cargando la escena {indiceEscenaDestino} " +
                  $"('{System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(indiceEscenaDestino))}').");

        SceneManager.LoadScene(indiceEscenaDestino);
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
    public void Configurar(int indiceDestino, GameObject banner, TextMeshProUGUI txtBanner,
                           string mensaje, PlayerInputReader input)
    {
        indiceEscenaDestino = indiceDestino;
        bannerInstrucciones = banner;
        textoInstruccion = txtBanner;
        if (!string.IsNullOrWhiteSpace(mensaje)) mensajeEntrada = mensaje;
        entrada = input;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[UMBRAL] {name}: el collider debe estar en modo Trigger.", this);
    }
#endif
}
