using TMPro;
using UnityEngine;

/// <summary>
/// Etiqueta de progreso del HUD. Escribe en un TextMeshProUGUI ya serializado
/// en 'Canvas_HUD' y solo toca la cadena cuando el porcentaje cambia: no
/// instancia UI ni asigna memoria por frame.
/// </summary>
[DisallowMultipleComponent]
public class ProgresoHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI texto;

    [Header("Formato")]
    [SerializeField] private string formato = "Progreso {0}%";

    private int ultimo = -1;

    private void Update()
    {
        if (texto == null) return;

        int progreso = (GameProgressManager.Instance != null)
            ? GameProgressManager.Instance.ProgresoPorcentaje
            : 0;

        if (progreso == ultimo) return;

        ultimo = progreso;
        texto.text = string.Format(formato, progreso);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(TextMeshProUGUI etiqueta, string formatoNuevo)
    {
        texto = etiqueta;
        if (!string.IsNullOrEmpty(formatoNuevo)) formato = formatoNuevo;
        ultimo = -1;
    }
}
