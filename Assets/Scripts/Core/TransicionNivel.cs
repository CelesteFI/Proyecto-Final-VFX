using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tótem del Umbral: puerta narrativa entre niveles (tecla 'F').
/// Solo se vuelve interactuable cuando se cumplen los hitos de progreso; al
/// pulsarla confirma el estado en GameSaveState/GameProgressManager y ejecuta
/// SceneManager.LoadScene con el nombre de la maqueta destino.
/// La escena destino debe figurar en Build Settings.
/// </summary>
[DisallowMultipleComponent]
public class TransicionNivel : MonoBehaviour, IInteractuable
{
    [Header("Destino")]
    [Tooltip("Nombre exacto de la escena en Build Settings, sin extensión.")]
    [SerializeField] private string nombreEscenaDestino = "Maqueta Nivel 2";

    [SerializeField] private string prompt = "Cruzar el Umbral";

    [Header("Requisitos de Progreso")]
    [SerializeField] private bool requiereJefeNivel1 = true;
    [SerializeField] private bool requiereIllyaLiberada = true;

    [Header("Feedback")]
    [SerializeField] private Renderer visual;
    [SerializeField] private Color colorBloqueado = new Color(0.35f, 0.38f, 0.42f, 1f);
    [SerializeField] private Color colorListo = new Color(0.42f, 1f, 0.72f, 1f);

    private bool enTransito;

    public string TextoPrompt => prompt;

    /// <summary>Solo se ofrece halo y banner cuando el umbral está realmente abierto.</summary>
    public bool PuedeInteractuar => !enTransito && RequisitosCumplidos;

    public bool RequisitosCumplidos
    {
        get
        {
            GameProgressManager gm = GameProgressManager.Instance;
            if (gm == null) return false;

            if (requiereJefeNivel1 && !gm.bossNivel1Derrotado) return false;
            if (requiereIllyaLiberada && !gm.illyaDesbloqueada) return false;

            return true;
        }
    }

    private void Start()
    {
        Pintar();
    }

    public void Interactuar(Transform actor)
    {
        if (enTransito) return;

        if (!RequisitosCumplidos)
        {
            Debug.Log("[TRANSICIÓN] El Umbral sigue sellado: falta derrotar a la bestia del altar o liberar a Illya.");
            return;
        }

        Transitar();
    }

    /// <summary>Guarda el estado y carga la maqueta destino.</summary>
    public void Transitar()
    {
        if (enTransito) return;

        if (string.IsNullOrEmpty(nombreEscenaDestino))
        {
            Debug.LogWarning("[TRANSICIÓN] No hay escena destino configurada.");
            return;
        }

        enTransito = true;

        // Un modal abierto deja Time.timeScale = 0; la escena nueva debe arrancar limpia.
        Time.timeScale = 1f;

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.DesbloquearIllya(GameProgressManager.Instance.illyaDesbloqueada);

        GameSaveState.GuardarObjeto("Nivel1_Completado");

        Debug.Log($"[TRANSICIÓN] Umbral activado. Cargando '{nombreEscenaDestino}' (progreso {GameProgressManager.Instance?.ProgresoPorcentaje ?? 0} %).");
        SceneManager.LoadScene(nombreEscenaDestino);
    }

    private void Pintar()
    {
        if (visual == null) return;

        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        visual.GetPropertyBlock(bloque);

        Color color = RequisitosCumplidos ? colorListo : colorBloqueado;
        bloque.SetColor(Shader.PropertyToID("_BaseColor"), color);
        bloque.SetColor(Shader.PropertyToID("_EmissionColor"), color * (RequisitosCumplidos ? 0.85f : 0.1f));
        visual.SetPropertyBlock(bloque);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(string escenaDestino, bool exigirJefe, bool exigirIllya, string textoPrompt, Renderer visualTotem)
    {
        nombreEscenaDestino = escenaDestino;
        requiereJefeNivel1 = exigirJefe;
        requiereIllyaLiberada = exigirIllya;
        prompt = textoPrompt;
        visual = visualTotem;
        Pintar();
    }
}
