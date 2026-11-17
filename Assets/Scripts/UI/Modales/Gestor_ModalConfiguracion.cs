using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cierre del modal de Configuración y puente entre su Slider de volumen y
/// <see cref="GestorAudioGlobal"/>. El botón Btn_Cerrar_X llama a CerrarModal()
/// desde su evento On Click configurado en el Inspector, y el Slider
/// Slider_Volumen invoca EstablecerVolumen(float) también desde un listener
/// persistente de Inspector (nunca desde código en runtime).
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalConfiguracion : MonoBehaviour
{
    [Header("Slider de Volumen")]
    [Tooltip("Slider 'Slider_Volumen' del propio modal.")]
    [SerializeField] private Slider sliderVolumen;

    [Tooltip("Texto que muestra el porcentaje de volumen.")]
    [SerializeField] private TextMeshProUGUI textoVolumen;

    [Tooltip("Formato del porcentaje. {0} es el valor 0-100.")]
    [SerializeField] private string formatoVolumen = "Volumen de audio: {0}%";

    private void OnEnable()
    {
        SincronizarSlider();
    }

    /// <summary>
    /// Recibe el valor del Slider (0..1) desde su evento On Value Changed y lo
    /// propaga al gestor global, que lo persiste y lo aplica a toda la escena.
    /// </summary>
    public void EstablecerVolumen(float valor)
    {
        GestorAudioGlobal.Obtener().EstablecerVolumen(valor);
        ActualizarTexto(valor);
    }

    /// <summary>Ajusta el Slider al volumen vigente sin disparar su evento.</summary>
    public void SincronizarSlider()
    {
        float volumen = GestorAudioGlobal.Obtener().VolumenGlobal;

        if (sliderVolumen != null)
        {
#if UNITY_2022_3_OR_NEWER
            sliderVolumen.SetValueWithoutNotify(volumen);
#else
            sliderVolumen.value = volumen;
#endif
        }

        ActualizarTexto(volumen);
    }

    private void ActualizarTexto(float valor)
    {
        if (textoVolumen == null) return;
        textoVolumen.text = string.Format(formatoVolumen, Mathf.RoundToInt(Mathf.Clamp01(valor) * 100f));
    }

    public void CerrarModal()
    {
        gameObject.SetActive(false);
    }
}
