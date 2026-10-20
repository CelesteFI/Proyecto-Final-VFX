using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Objeto interactuable genérico de entorno (tecla 'F'): cofres de alijo,
/// la Llave de las Zarzas, la celda de Illya y el Núcleo del Engranaje Eólico.
/// Toda la consecuencia de diseño se conecta por UnityEvent en el inspector.
/// </summary>
public class InteractableItem : MonoBehaviour, IInteractuable
{
    [Header("Prompt")]
    [SerializeField] private string textoPrompt = "Interactuar";
    [SerializeField] private bool disponible = true;

    [Header("Consumo")]
    [SerializeField] private bool consumirAlUsar = true;
    [SerializeField] private GameObject objetoVisualQueDesaparece;

    [Header("Progreso")]
    [Tooltip("Identificador único para guardar el estado en GameProgressManager.")]
    [SerializeField] private string idProgreso;

    [Tooltip("Identificador de objeto que debe estar recogido para poder interactuar (ej. una llave).")]
    [SerializeField] private string idRequisito;

    [Tooltip("Mensaje mostrado cuando falta el requisito.")]
    [SerializeField] private string mensajeFaltaRequisito = "Falta un objeto necesario.";

    [Header("Eventos")]
    [SerializeField] private UnityEvent alInteractuar = new UnityEvent();
    [SerializeField] private UnityEvent alNoDisponible = new UnityEvent();

    public string TextoPrompt => textoPrompt;
    public bool PuedeInteractuar => disponible;

    private void Start()
    {
        if (GameProgressManager.Instance != null
            && !string.IsNullOrEmpty(idProgreso)
            && GameProgressManager.Instance.EstaRecogido(idProgreso))
        {
            consumirAlUsar = false;
            disponible = false;
            if (objetoVisualQueDesaparece != null) objetoVisualQueDesaparece.SetActive(false);
        }
    }

    public void Interactuar(Transform actor)
    {
        if (!disponible)
        {
            alNoDisponible?.Invoke();
            return;
        }

        // Requisito opcional (Llave de las Zarzas, Núcleo del Engranaje Eólico...)
        if (!string.IsNullOrEmpty(idRequisito)
            && (GameProgressManager.Instance == null || !GameProgressManager.Instance.EstaRecogido(idRequisito)))
        {
            Debug.Log($"[INTERACCIÓN] {name}: {mensajeFaltaRequisito}");
            alNoDisponible?.Invoke();
            return;
        }

        alInteractuar?.Invoke();

        if (!string.IsNullOrEmpty(idProgreso) && GameProgressManager.Instance != null)
            GameProgressManager.Instance.RegistrarCofreOItem(idProgreso);

        Debug.Log($"[INTERACCIÓN] {textoPrompt} ejecutado por {actor.name}");

        if (consumirAlUsar)
        {
            disponible = false;
            if (objetoVisualQueDesaparece != null) objetoVisualQueDesaparece.SetActive(false);
        }
    }

    /// <summary>Habilita la interacción (por ejemplo tras obtener la llave).</summary>
    public void Habilitar(bool habilitado)
    {
        disponible = habilitado;
    }

    /// <summary>Cambia el texto del prompt en tiempo de ejecución.</summary>
    public void CambiarPrompt(string nuevoPrompt)
    {
        textoPrompt = nuevoPrompt;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(string prompt, string id, bool consumible)
    {
        textoPrompt = prompt;
        idProgreso = id;
        consumirAlUsar = consumible;
    }

    /// <summary>Define un objeto requerido para poder interactuar.</summary>
    public void ConfigurarRequisito(string idNecesario, string mensaje)
    {
        idRequisito = idNecesario;
        mensajeFaltaRequisito = mensaje;
    }
}
