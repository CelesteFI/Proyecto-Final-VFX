using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datos de un nodo del árbol de habilidades elementales.
/// Serializado en el inspector para poder equilibrar costes sin tocar código.
/// </summary>
[Serializable]
public class NodoHabilidad
{
    public string nombre = "Nodo";
    [Tooltip("Tierra / Madera / Pasiva")]
    public string elemento = "Tierra";
    public int costo = 3;
    [Tooltip("Cristales de Voto, Oro Puro o Esencias Elementales")]
    public string moneda = "Cristales de Voto";
    [TextArea(3, 6)]
    public string descripcion = "Describe el efecto elemental del nodo.";
    public Color colorNodo = new Color(0.55f, 0.42f, 0.22f, 1f);
}

/// <summary>
/// Controla la ventana modal de habilidades: rejilla de botones de nodo y
/// panel de detalle dinámico (Nombre, Costo y Descripción elemental).
/// Sin comprobaciones en Update: reacciona a los clics de la rejilla.
/// </summary>
public class HeroAbilityTreeUI : MonoBehaviour
{
    [Header("Rejilla de Nodos")]
    [SerializeField] private List<Button> botones = new List<Button>();
    [SerializeField] private List<NodoHabilidad> nodos = new List<NodoHabilidad>();

    [Header("Panel de Detalle")]
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoCosto;
    [SerializeField] private TextMeshProUGUI textoDescripcion;
    [SerializeField] private Image iconoElemento;

    [Header("Estado Inicial")]
    [SerializeField] private int indiceInicial = 0;

    [Header("Vínculo con el Héroe Activo")]
    [Tooltip("Si se deja vacío se busca el TagTeamManager de la escena.")]
    [SerializeField] private TagTeamManager tagTeam;

    /// <summary>Elemento del GDD que corresponde a cada héroe.</summary>
    private const string ElementoTierra = "Tierra";
    private const string ElementoMadera = "Madera";
    private const string ElementoPasiva = "Pasiva";

    private void Start()
    {
        EnlazarBotones();

        if (tagTeam == null) tagTeam = FindFirstObjectByType<TagTeamManager>();
        if (tagTeam != null) tagTeam.OnHeroeCambiado += RefrescarRamas;

        if (nodos.Count > 0)
            SeleccionarNodo(Mathf.Clamp(indiceInicial, 0, nodos.Count - 1));

        RefrescarRamas(tagTeam != null ? tagTeam.ActiveHero : null);
    }

    private void OnDestroy()
    {
        if (tagTeam != null) tagTeam.OnHeroeCambiado -= RefrescarRamas;
    }

    /// <summary>
    /// Vincula el árbol al héroe activo: Caelen ('1') muestra las ramas de
    /// Tierra/Muro y Illya ('2') las de Madera/Zarzas. Las pasivas compartidas
    /// permanecen visibles para ambos, porque son la sinergia del Tag-Team.
    /// </summary>
    private void RefrescarRamas(HeroCombatData heroe)
    {
        if (heroe == null || nodos.Count == 0) return;

        string elementoHeroe = (heroe.element == HeroCombatData.ElementType.Piedra)
            ? ElementoTierra
            : ElementoMadera;

        int primeroVisible = -1;

        for (int i = 0; i < botones.Count; i++)
        {
            if (botones[i] == null) continue;

            bool visible = i >= nodos.Count
                || string.IsNullOrEmpty(nodos[i].elemento)
                || nodos[i].elemento == elementoHeroe
                || nodos[i].elemento == ElementoPasiva;

            if (botones[i].gameObject.activeSelf != visible) botones[i].gameObject.SetActive(visible);

            if (visible && primeroVisible < 0) primeroVisible = i;
        }

        if (primeroVisible >= 0) SeleccionarNodo(primeroVisible);
    }

    /// <summary>Conecta cada botón de la rejilla con su índice de nodo.</summary>
    private void EnlazarBotones()
    {
        for (int i = 0; i < botones.Count; i++)
        {
            if (botones[i] == null) continue;

            int indice = i; // captura local para el closure
            botones[i].onClick.RemoveAllListeners();
            botones[i].onClick.AddListener(() => SeleccionarNodo(indice));

            if (i < nodos.Count)
            {
                Image fondo = botones[i].targetGraphic as Image;
                if (fondo != null) fondo.color = nodos[i].colorNodo;

                TextMeshProUGUI etiqueta = botones[i].GetComponentInChildren<TextMeshProUGUI>();
                if (etiqueta != null) etiqueta.text = nodos[i].nombre;
            }
        }
    }

    /// <summary>Actualiza el panel inferior/side con el nodo seleccionado.</summary>
    public void SeleccionarNodo(int indice)
    {
        if (indice < 0 || indice >= nodos.Count) return;

        NodoHabilidad nodo = nodos[indice];

        if (textoNombre != null) textoNombre.text = nodo.nombre;
        if (textoCosto != null) textoCosto.text = $"Costo: {nodo.costo} {nodo.moneda}";
        if (textoDescripcion != null) textoDescripcion.text = nodo.descripcion;
        if (iconoElemento != null) iconoElemento.color = nodo.colorNodo;

        Debug.Log($"[HABILIDADES] Nodo seleccionado: {nodo.nombre} ({nodo.elemento}) · {nodo.costo} {nodo.moneda}");
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(List<Button> botonesRejilla, List<NodoHabilidad> datos, TextMeshProUGUI nombre, TextMeshProUGUI costo, TextMeshProUGUI descripcion, Image icono)
    {
        botones = botonesRejilla;
        nodos = datos;
        textoNombre = nombre;
        textoCosto = costo;
        textoDescripcion = descripcion;
        iconoElemento = icono;
    }
}
