using TMPro;
using UnityEngine;

/// <summary>
/// Presentación del lore en el HUD. NO crea ningún elemento: opera sobre el
/// GameObject 'Modal_Lore' ya serializado en 'Canvas_HUD' (título, cuerpo,
/// contador y botón de cierre) y lo abre a través de <see cref="UIModalWindow"/>,
/// que congela el juego mientras dura la lectura.
/// </summary>
[DisallowMultipleComponent]
public class LoreUI : MonoBehaviour
{
    [Header("Referencias del Modal Físico")]
    [SerializeField] private UIModalWindow modal;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoCuerpo;
    [SerializeField] private TextMeshProUGUI textoContador;

    [Header("Formato")]
    [SerializeField] private string formatoContador = "Fragmentos recuperados: {0} / {1}";
    [SerializeField] private int totalFragmentos = 3;

    /// <summary>True mientras el panel de lectura está en pantalla.</summary>
    public bool EstaVisible => modal != null && modal.EstaAbierto;

    /// <summary>Muestra un fragmento narrativo y refresca el contador de lectura.</summary>
    public void MostrarFragmento(TotemLore fragmento)
    {
        if (fragmento == null) return;

        if (textoTitulo != null) textoTitulo.text = fragmento.Titulo;
        if (textoCuerpo != null) textoCuerpo.text = fragmento.Cuerpo;

        RefrescarContador();

        if (modal != null) modal.Abrir();
    }

    /// <summary>Cierra el panel de lectura.</summary>
    public void Cerrar()
    {
        if (modal != null) modal.Cerrar();
    }

    /// <summary>Botón de cierre del modal (sin animación, para cambios de escena).</summary>
    public void CerrarInmediato()
    {
        if (modal != null) modal.CerrarInmediato();
    }

    private void RefrescarContador()
    {
        if (textoContador == null) return;

        int leidos = (GameProgressManager.Instance != null)
            ? GameProgressManager.Instance.FragmentosLeidos
            : 0;

        textoContador.text = string.Format(formatoContador, Mathf.Min(leidos, totalFragmentos), totalFragmentos);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(UIModalWindow ventana, TextMeshProUGUI titulo,
                           TextMeshProUGUI cuerpo, TextMeshProUGUI contador, int total)
    {
        modal = ventana;
        textoTitulo = titulo;
        textoCuerpo = cuerpo;
        textoContador = contador;
        totalFragmentos = Mathf.Max(1, total);
    }
}
