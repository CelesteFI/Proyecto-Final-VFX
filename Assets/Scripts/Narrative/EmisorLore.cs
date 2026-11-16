using UnityEngine;

/// <summary>
/// Emisor narrativo de un tótem. Guarda el título y el cuerpo de su fragmento y los
/// entrega a <see cref="Gestor_Lore"/> la primera vez que el tótem se enciende con 'F'.
///
/// La separación es deliberada: <see cref="TotemLore"/> resuelve SOLO la mecánica
/// (trigger, banner, entrada, luz y fuego azul) y este módulo aporta únicamente la
/// parte de historia. Un tótem sin `EmisorLore` se enciende igual, pero no abre el panel.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TotemLore))]
public class EmisorLore : MonoBehaviour
{
    [Header("Contenido Narrativo")]
    [SerializeField] private string titulo = "Fragmento Rúnico";
    [Tooltip("Cuerpo del fragmento. Si está vacío, el tótem se enciende pero no abre el panel.")]
    [TextArea(3, 10)]
    [SerializeField] private string cuerpo = string.Empty;

    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("Gestor_Lore del panel 'Modal_Lore' de Canvas_HUD.")]
    [SerializeField] private Gestor_Lore gestorLore;
    [Tooltip("Tótem al que pertenece este emisor. Si queda vacío se toma el de este GameObject.")]
    [SerializeField] private TotemLore totem;

    public string Titulo => titulo;
    public string Cuerpo => cuerpo;

    /// <summary>True si este emisor tiene un fragmento que entregar.</summary>
    public bool TieneLore => !string.IsNullOrWhiteSpace(cuerpo);

    private void Awake()
    {
        if (totem == null) totem = GetComponent<TotemLore>();
    }

    private void OnEnable()
    {
        if (totem != null) totem.OnLeido += AlEncenderseTotem;
    }

    private void OnDisable()
    {
        if (totem != null) totem.OnLeido -= AlEncenderseTotem;
    }

    /// <summary>El tótem acaba de encenderse con 'F': se entrega el fragmento al HUD.</summary>
    private void AlEncenderseTotem(TotemLore _)
    {
        if (!TieneLore) return;

        if (gestorLore == null)
        {
            Debug.LogWarning($"[LORE] {name}: no hay Gestor_Lore asignado; el fragmento no se muestra.");
            return;
        }

        gestorLore.MostrarLore(titulo, cuerpo);
        Debug.Log($"[LORE] {name} mostró el fragmento '{titulo}'.");
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(string tituloNuevo, string cuerpoNuevo, Gestor_Lore gestor)
    {
        titulo = string.IsNullOrWhiteSpace(tituloNuevo) ? "Fragmento Rúnico" : tituloNuevo;
        cuerpo = cuerpoNuevo ?? string.Empty;
        if (gestor != null) gestorLore = gestor;
    }
}
