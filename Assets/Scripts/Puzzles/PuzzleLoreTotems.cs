using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor del bloque `06_Puzzle_Lore` (Nivel 1).
/// Lleva la cuenta de los tótems encendidos de la zona, entrega los fragmentos de lore
/// al <see cref="LoreUI"/> (contador «Fragmentos recuperados: X / 4» y `Modal_Lore`)
/// y, cuando el número de tótems encendidos iguala al total de la zona, desactiva la
/// barrera/sello que abre el paso hacia `08_Altar_Arbol_Sombrio_BossArena`.
/// No busca referencias en runtime: todo llega por el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class PuzzleLoreTotems : MonoBehaviour
{
    [Header("Tótems de la Zona")]
    [SerializeField] private List<TotemLore> totems = new List<TotemLore>();

    [Header("HUD de Lore")]
    [Tooltip("LoreUI del 'Modal_Lore' de Canvas_HUD.")]
    [SerializeField] private LoreUI loreUI;

    [Header("Paso hacia el Altar del Jefe")]
    [Tooltip("Sello rúnico del puente fracturado: se disuelve al encender el último tótem.")]
    [SerializeField] private SelloNarrativo sello;
    [Tooltip("Collider/barrera física que impide el paso (se desactiva).")]
    [SerializeField] private GameObject barreraSello;

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

        if (totem.TieneLore)
        {
            // Se persiste el fragmento para que el contador del HUD y el progreso global
            // lo reflejen (RegistrarFragmento es idempotente por identificador).
            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.RegistrarFragmento(totem.IdFragmento);

            fragmentosEntregados = ContarFragmentosEntregados();

            if (loreUI != null) loreUI.MostrarFragmento(totem);
            else Debug.LogWarning($"[TÓTEMS 06] {totem.name} tiene lore pero no hay LoreUI asignado.");
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

        if (sello != null) sello.Disolver();
        if (barreraSello != null) barreraSello.SetActive(false);

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
            if (t != null && t.TieneLore && t.YaEncendido) total++;
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

    /// <summary>Configuración desde el integrador de escena (lista de tótems y HUD).</summary>
    public void ConfigurarTotems(List<TotemLore> lista, LoreUI ui)
    {
        totems = lista ?? new List<TotemLore>();
        if (ui != null) loreUI = ui;
        SincronizarInspeccion();
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(List<TotemLore> lista, LoreUI ui, SelloNarrativo selloPuente, GameObject barrera)
    {
        totems = lista ?? new List<TotemLore>();
        loreUI = ui;
        sello = selloPuente;
        barreraSello = barrera;
        SincronizarInspeccion();
    }
}
