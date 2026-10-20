using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Ficha de un recurso del juego (Oro Puro, Cristales de Voto, Esencias).</summary>
[Serializable]
public class FichaRecurso
{
    public string titulo = "Recurso";
    public string categoria = "Material";
    public int cantidad = 0;
    [TextArea(3, 6)]
    public string descripcionUso = "Describe el uso del recurso.";
    [TextArea(2, 5)]
    public string lore = "Fragmento de lore.";
    public Color colorFicha = Color.white;
}

/// <summary>
/// Códex de recursos de la ventana modal: tarjetas interactivas que al pulsarse
/// actualizan un panel de detalle con uso, lore y cantidad.
/// </summary>
public class ResourceCodexUI : MonoBehaviour
{
    [Header("Tarjetas Interactivas")]
    [SerializeField] private List<Button> tarjetas = new List<Button>();
    [SerializeField] private List<FichaRecurso> fichas = new List<FichaRecurso>();

    [Header("Panel de Detalle")]
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoCategoria;
    [SerializeField] private TextMeshProUGUI textoCuerpo;
    [SerializeField] private Image iconoDetalle;

    private void Start()
    {
        EnlazarTarjetas();

        if (fichas.Count > 0) SeleccionarFicha(0);
    }

    private void EnlazarTarjetas()
    {
        for (int i = 0; i < tarjetas.Count; i++)
        {
            if (tarjetas[i] == null) continue;

            int indice = i;
            tarjetas[i].onClick.RemoveAllListeners();
            tarjetas[i].onClick.AddListener(() => SeleccionarFicha(indice));

            if (i < fichas.Count)
            {
                Image fondo = tarjetas[i].targetGraphic as Image;
                if (fondo != null) fondo.color = fichas[i].colorFicha;
            }
        }
    }

    /// <summary>Muestra en el panel el uso y el lore del recurso seleccionado.</summary>
    public void SeleccionarFicha(int indice)
    {
        if (indice < 0 || indice >= fichas.Count) return;

        FichaRecurso ficha = fichas[indice];

        if (textoTitulo != null) textoTitulo.text = ficha.titulo;
        if (textoCategoria != null) textoCategoria.text = $"{ficha.categoria} · x{ficha.cantidad}";
        if (textoCuerpo != null) textoCuerpo.text = $"<b>Uso:</b> {ficha.descripcionUso}\n\n<i>{ficha.lore}</i>";
        if (iconoDetalle != null) iconoDetalle.color = ficha.colorFicha;

        Debug.Log($"[RECURSOS] Ficha seleccionada: {ficha.titulo} ({ficha.cantidad})");
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(List<Button> tarjetasUi, List<FichaRecurso> datos, TextMeshProUGUI titulo, TextMeshProUGUI categoria, TextMeshProUGUI cuerpo, Image icono)
    {
        tarjetas = tarjetasUi;
        fichas = datos;
        textoTitulo = titulo;
        textoCategoria = categoria;
        textoCuerpo = cuerpo;
        iconoDetalle = icono;
    }
}
