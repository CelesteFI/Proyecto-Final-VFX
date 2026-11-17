using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Autoridad única del volumen global de audio del juego.
/// Vive en <c>DontDestroyOnLoad</c>, persiste el valor en <see cref="PlayerPrefs"/> y lo
/// reaplica en cada carga de escena para que el Nivel 1 y el Nivel 2 compartan siempre el
/// último valor elegido por el jugador en el Slider del modal de Configuración.
/// No crea GameObjects de UI ni toca <c>Time.timeScale</c>.
/// </summary>
[DisallowMultipleComponent]
public class GestorAudioGlobal : MonoBehaviour
{
    /// <summary>Clave de PlayerPrefs donde se guarda el volumen (0..1).</summary>
    public const string ClaveVolumen = "VFX_VolumenGlobal";

    /// <summary>Volumen usado la primera vez que se ejecuta el juego.</summary>
    public const float VolumenPorDefecto = 0.8f;

    private static GestorAudioGlobal instancia;

    [Header("Estado (persistente)")]
    [SerializeField, Range(0f, 1f)] private float volumenGlobal = VolumenPorDefecto;

    [Header("Aplicación")]
    [Tooltip("Aplica el volumen sobre AudioListener.volume, que afecta a todos los AudioSource de la escena.")]
    [SerializeField] private bool aplicarAlAudioListener = true;

    /// <summary>Instancia viva del gestor (puede ser null antes del primer Awake).</summary>
    public static GestorAudioGlobal Instancia => instancia;

    /// <summary>Volumen global actual, de 0 (silencio) a 1 (máximo).</summary>
    public float VolumenGlobal => volumenGlobal;

    /// <summary>
    /// Devuelve el gestor activo y, si no existe, crea uno permanente.
    /// Permite que cualquier escena use el volumen global aunque no tenga el componente.
    /// </summary>
    public static GestorAudioGlobal Obtener()
    {
        if (instancia != null) return instancia;

        var existente = FindFirstObjectByType<GestorAudioGlobal>(FindObjectsInactive.Include);
        if (existente != null)
        {
            instancia = existente;
            return instancia;
        }

        var go = new GameObject("GestorAudioGlobal");
        instancia = go.AddComponent<GestorAudioGlobal>();
        return instancia;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            // Ya hay un gestor permanente (viene del Nivel 1): este sobra.
            Destroy(this);
            return;
        }

        instancia = this;

        if (transform.parent != null) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        volumenGlobal = Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveVolumen, VolumenPorDefecto));
        Aplicar();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (instancia == this)
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            instancia = null;
        }
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Aplicar();
    }

    /// <summary>
    /// Fija el volumen global (0..1), lo persiste y lo aplica de inmediato.
    /// Es el método que invoca el Slider del modal de Configuración.
    /// </summary>
    public void EstablecerVolumen(float valor)
    {
        volumenGlobal = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(ClaveVolumen, volumenGlobal);
        PlayerPrefs.Save();
        Aplicar();
    }

    /// <summary>Recarga el volumen guardado (por si otra escena lo cambió).</summary>
    public void RecargarDesdeDisco()
    {
        volumenGlobal = Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveVolumen, VolumenPorDefecto));
        Aplicar();
    }

    /// <summary>Vuelve al volumen por defecto y lo persiste.</summary>
    public void RestablecerVolumen()
    {
        EstablecerVolumen(VolumenPorDefecto);
    }

    private void Aplicar()
    {
        if (aplicarAlAudioListener) AudioListener.volume = volumenGlobal;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        volumenGlobal = Mathf.Clamp01(volumenGlobal);
    }
#endif
}
