using System;
using UnityEngine;

/// <summary>
/// Tótem rúnico de lore de la Arena B (tecla 'F').
/// Cada tótem entrega un fragmento TEMÁTICAMENTE AUTOCONTENIDO: no depende de
/// una cronología estricta, así que la historia se entiende en cualquier orden
/// de lectura. Al leerse, publica <see cref="OnLeido"/> para que el
/// <see cref="SelloNarrativo"/> del puente evalúe su apertura.
/// No construye interfaz: delega la presentación en <see cref="LoreUI"/>, que
/// opera sobre el modal físico ya existente en 'Canvas_HUD'.
/// </summary>
[DisallowMultipleComponent]
public class TotemLore : MonoBehaviour, IInteractuable
{
    [Header("Fragmento Autocontenido")]
    [Tooltip("Identificador único persistente (PlayerPrefs).")]
    [SerializeField] private string idFragmento = "N1_Fragmento_1";

    [SerializeField] private string titulo = "El Voto de Piedra";

    [Tooltip("Registro temático completo: debe tener sentido por sí solo.")]
    [TextArea(3, 10)]
    [SerializeField] private string cuerpo = string.Empty;

    [SerializeField] private string prompt = "Leer el fragmento rúnico";

    [Header("Enlaces de Presentación")]
    [SerializeField] private LoreUI panelLore;
    [SerializeField] private Renderer visual;
    [SerializeField] private GameObject marcaLeido;

    [Header("Estado")]
    [SerializeField] private bool leido;

    [Header("Colores del Tótem")]
    [SerializeField] private Color colorPendiente = new Color(1f, 0.70f, 0.12f, 1f);
    [SerializeField] private Color colorLeido = new Color(1f, 0.90f, 0.58f, 1f);

    /// <summary>Se dispara al leer el fragmento por primera vez.</summary>
    public event Action<TotemLore> OnLeido;

    public string IdFragmento => idFragmento;
    public string Titulo => titulo;
    public string Cuerpo => cuerpo;
    public bool Leido => leido;

    public string TextoPrompt => leido ? $"«{titulo}» ya leído" : prompt;
    public bool PuedeInteractuar => !leido;

    private void Start()
    {
        // Estado persistido: un fragmento ya leído sigue dándolo por leído
        // aunque se recargue la escena.
        if (!leido && GameProgressManager.Instance != null
            && GameProgressManager.Instance.EstaFragmentoLeido(idFragmento))
        {
            leido = true;
        }

        Pintar();
        if (marcaLeido != null) marcaLeido.SetActive(leido);
    }

    public void Interactuar(Transform actor)
    {
        if (leido) return;

        leido = true;
        Pintar();
        if (marcaLeido != null) marcaLeido.SetActive(true);

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.RegistrarFragmento(idFragmento);

        if (panelLore != null) panelLore.MostrarFragmento(this);

        Debug.Log($"[LORE] {actor?.name} recuperó «{titulo}» ({idFragmento}).");
        OnLeido?.Invoke(this);
    }

    /// <summary>Fuerza el estado leído (restauración o pruebas automáticas).</summary>
    public void MarcarLeido(bool valor)
    {
        leido = valor;
        Pintar();
        if (marcaLeido != null) marcaLeido.SetActive(valor);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(string id, string tituloNuevo, string cuerpoNuevo, string textoPrompt,
                           LoreUI ui, Renderer visualTotem, GameObject marca)
    {
        idFragmento = id;
        titulo = tituloNuevo;
        cuerpo = cuerpoNuevo;
        prompt = textoPrompt;
        panelLore = ui;
        visual = visualTotem;
        marcaLeido = marca;
        Pintar();
    }

    private void Pintar()
    {
        if (visual == null) return;

        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        visual.GetPropertyBlock(bloque);
        Color color = leido ? colorLeido : colorPendiente;
        bloque.SetColor(Shader.PropertyToID("_BaseColor"), color);
        bloque.SetColor(Shader.PropertyToID("_EmissionColor"), color * (leido ? 0.55f : 0.8f));
        visual.SetPropertyBlock(bloque);
    }
}
