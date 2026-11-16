using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor del bloque `06_Puzzle_Lore` (Nivel 1).
/// Lleva la cuenta de los tótems encendidos de la zona y, cuando el número de tótems
/// encendidos iguala al total, disuelve el sello rúnico del puente: retira la barrera
/// física, habilita el paso hacia `08_Altar_Arbol_Sombrio_BossArena` y tiñe la malla
/// del sello.
///
/// Absorbe el antiguo 'SelloNarrativo': el sello ya no es un componente aparte, sino el
/// paso que abre este gestor (que es quien conoce el estado real de los tótems).
/// El contenido narrativo de cada tótem vive en su módulo <see cref="EmisorLore"/>.
/// No busca referencias en runtime: todo llega por el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class PuzzleLoreTotems : MonoBehaviour
{
    [Header("Tótems de la Zona")]
    [SerializeField] private List<TotemLore> totems = new List<TotemLore>();

    [Header("HUD de Lore")]
    [Tooltip("Gestor_Lore del 'Modal_Lore' de Canvas_HUD.")]
    [SerializeField] private Gestor_Lore gestorLore;

    [Header("Progreso del Nivel (HUD)")]
    [Tooltip("Canvas_HUD del nivel. Recibe SumarProgreso cuando todos los tótems están encendidos.")]
    [SerializeField] private Canvas_HUD canvasHUD;
    [Tooltip("Fracción (0..1) que suma a la Barra_Progreso al abrirse el sello.")]
    [SerializeField] private float progresoAlResolver = 0.1f;

    [Header("Paso hacia el Altar del Jefe")]
    [Tooltip("Barrera física del sello rúnico. Se desactiva al abrirse el paso.")]
    [SerializeField] private GameObject barreraSello;
    [Tooltip("Paso que solo existe cuando el sello cede (opcional).")]
    [SerializeField] private GameObject pasoHabilitado;
    [Tooltip("Malla del sello que se tiñe al disolverse (opcional).")]
    [SerializeField] private Renderer visualSello;

    [Header("Colores del Sello")]
    [SerializeField] private Color colorSellado = new Color(0.55f, 0.92f, 0.95f, 1f);
    [SerializeField] private Color colorDisuelto = new Color(0.28f, 0.33f, 0.38f, 1f);

    [Header("Estado (solo lectura)")]
    [SerializeField] private int totemsEncendidos;
    [SerializeField] private int totalTotems;
    [SerializeField] private int fragmentosEntregados;
    [SerializeField] private bool pasoAbierto;

    /// <summary>Total de tótems registrados en la zona.</summary>
    public int TotalTotems => totalTotems;

    /// <summary>Tótems encendidos (recuento vivo, no solo el último aviso).</summary>
    public int Encendidos => RecuentoEncendidos();

    /// <summary>Fragmentos de lore entregados por esta zona.</summary>
    public int FragmentosEntregados => fragmentosEntregados;

    /// <summary>True cuando el paso hacia el altar del jefe ya está abierto.</summary>
    public bool PasoAbierto => pasoAbierto;

    private void Awake()
    {
        SincronizarInspeccion();
        PintarSello();
    }

    private void Update()
    {
        // Revisión constante: si todos los tótems están encendidos, el sello cede.
        ComprobarVictoria();
    }

    /// <summary>La llama cada tótem la primera vez que se enciende.</summary>
    public void RegistrarEncendido(TotemLore totem)
    {
        if (totem == null) return;

        if (!totems.Contains(totem))
        {
            totems.Add(totem);
            Debug.LogWarning($"[TÓTEMS 06] {totem.name} no estaba registrado en el gestor; se añade a la zona.");
        }

        // El fragmento se persiste para que el progreso global lo refleje
        // (RegistrarFragmento es idempotente por identificador). Mostrar el panel
        // es responsabilidad del EmisorLore del propio tótem.
        EmisorLore emisor = totem.GetComponent<EmisorLore>();
        if (emisor != null && emisor.TieneLore)
        {
            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.RegistrarFragmento(totem.IdFragmento);
        }

        SincronizarInspeccion();

        Debug.Log($"[TÓTEMS 06] Encendido {totem.name}: {totemsEncendidos}/{totalTotems} tótems · " +
                  $"fragmentos {fragmentosEntregados}.");

        ComprobarVictoria();
    }

    /// <summary>Abre el paso hacia el altar si ya no queda ningún tótem apagado.</summary>
    public void ComprobarVictoria()
    {
        if (pasoAbierto) return;

        int encendidos = RecuentoEncendidos();
        if (encendidos <= 0 || encendidos != TotalTotems) return;

        pasoAbierto = true;
        totemsEncendidos = encendidos;

        Disolver();

        // Señal de progreso al HUD: puzzle de tótems resuelto (pasoAbierto evita repetirla).
        if (canvasHUD != null) canvasHUD.SumarProgreso(progresoAlResolver);
    }

    /// <summary>
    /// Disuelve el sello: retira la barrera, habilita el paso y tiñe la malla.
    /// Se conserva el nombre histórico para las herramientas de diagnóstico.
    /// </summary>
    public void Disolver()
    {
        if (barreraSello != null && barreraSello.activeSelf) barreraSello.SetActive(false);
        if (pasoHabilitado != null && !pasoHabilitado.activeSelf) pasoHabilitado.SetActive(true);

        PintarSello();

        Debug.Log($"[TÓTEMS 06] Los {TotalTotems} tótems de la zona están encendidos: " +
                  "el sello del puente cede y el paso al Altar del Árbol Sombrío queda abierto.");
    }

    // ------------------------------------------------------------------ utilidades

    private int RecuentoEncendidos()
    {
        int total = 0;
        for (int i = 0; i < totems.Count; i++)
        {
            TotemLore t = totems[i];
            if (t != null && t.YaEncendido) total++;
        }

        return total;
    }

    private int ContarFragmentosEntregados()
    {
        int total = 0;
        for (int i = 0; i < totems.Count; i++)
        {
            TotemLore t = totems[i];
            if (t == null || !t.YaEncendido) continue;

            EmisorLore emisor = t.GetComponent<EmisorLore>();
            if (emisor != null && emisor.TieneLore) total++;
        }

        return total;
    }

    private void SincronizarInspeccion()
    {
        totalTotems = 0;
        for (int i = 0; i < totems.Count; i++)
            if (totems[i] != null) totalTotems++;

        totemsEncendidos = RecuentoEncendidos();
        fragmentosEntregados = ContarFragmentosEntregados();
    }

    private void PintarSello()
    {
        if (visualSello == null) return;

        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        visualSello.GetPropertyBlock(bloque);

        Color color = pasoAbierto ? colorDisuelto : colorSellado;
        bloque.SetColor(Shader.PropertyToID("_BaseColor"), color);
        bloque.SetColor(Shader.PropertyToID("_EmissionColor"), color * (pasoAbierto ? 0.05f : 0.9f));
        visualSello.SetPropertyBlock(bloque);
    }

    /// <summary>Configuración desde el integrador de escena (lista de tótems y HUD).</summary>
    public void ConfigurarTotems(List<TotemLore> lista, Gestor_Lore lore)
    {
        totems = lista ?? new List<TotemLore>();
        if (lore != null) gestorLore = lore;
        SincronizarInspeccion();
    }

    /// <summary>Configuración del paso físico que abre el sello.</summary>
    public void ConfigurarPaso(GameObject barrera, GameObject paso, Renderer visual)
    {
        barreraSello = barrera;
        pasoHabilitado = paso;
        visualSello = visual;
        PintarSello();
    }
}
