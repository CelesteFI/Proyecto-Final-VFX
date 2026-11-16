using TMPro;
using UnityEngine;

/// <summary>
/// Lectura del lore en el HUD. NO crea ni reorganiza ningún elemento: opera sobre el
/// GameObject 'Modal_Lore' ya serializado en 'Canvas_HUD' (solo título y cuerpo) y lo
/// abre mediante su <see cref="UIModalWindow"/>, que congela el juego mientras dura la
/// lectura. El contador de fragmentos se ha eliminado: este gestor únicamente recibe y
/// muestra título y cuerpo.
/// La pausa NO se escribe aquí: UIModalWindow pone Time.timeScale = 0 en su OnEnable y lo
/// devuelve a 1 en su OnDisable, así que ningún cierre (botón, SetActive externo, cambio
/// de escena) deja el juego congelado. La escala local del Player no se altera.
/// </summary>
[DisallowMultipleComponent]
public class Gestor_Lore : MonoBehaviour
{
    [Header("Referencias del Modal")]
    [Tooltip("Ventana modal del propio panel. Aporta la animación de zoom y la pausa compartida.")]
    [SerializeField] private UIModalWindow modal;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoCuerpo;

    /// <summary>True mientras el panel de lectura está en pantalla.</summary>
    public bool EstaVisible => (modal != null) ? modal.EstaAbierto : gameObject.activeSelf;

    /// <summary>
    /// Muestra un fragmento narrativo. Lo llama el 'EmisorLore' que cuelga del tótem
    /// la primera vez que este se enciende con 'F'.
    /// </summary>
    public void MostrarLore(string titulo, string cuerpo)
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoCuerpo != null) textoCuerpo.text = cuerpo;

        if (modal != null) modal.Abrir();
        else if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    /// <summary>Cierra el panel con su animación de zoom.</summary>
    public void Cerrar()
    {
        if (modal != null) modal.Cerrar();
        else gameObject.SetActive(false);
    }

    /// <summary>Cierra el panel sin animación (cambio de escena o reinicio).</summary>
    public void CerrarInmediato()
    {
        if (modal != null) modal.CerrarInmediato();
        else gameObject.SetActive(false);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(UIModalWindow ventana, TextMeshProUGUI titulo, TextMeshProUGUI cuerpo)
    {
        modal = ventana;
        textoTitulo = titulo;
        textoCuerpo = cuerpo;
    }
}
