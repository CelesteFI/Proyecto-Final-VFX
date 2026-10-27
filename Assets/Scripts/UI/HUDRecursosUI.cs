using TMPro;
using UnityEngine;

/// <summary>
/// Contadores numéricos de recursos del modal de Resonancia / Árbol de Talentos.
/// Solo escribe texto en los TMP ya presentes en la Hierarchy
/// ('Cnt_CristalesVoto', 'Cnt_OroPuro', 'Cnt_Esencias').
/// </summary>
[DisallowMultipleComponent]
public class HUDRecursosUI : MonoBehaviour
{
    [Header("Contadores TMP")]
    [SerializeField] private TextMeshProUGUI textoCristalesVoto;
    [SerializeField] private TextMeshProUGUI textoOroPuro;
    [SerializeField] private TextMeshProUGUI textoEsencias;

    [Header("Formato")]
    [SerializeField] private string formato = "x{0}";

    private void OnEnable()
    {
        GameSaveState.OnCambio += Refrescar;
        Refrescar();
    }

    private void OnDisable()
    {
        GameSaveState.OnCambio -= Refrescar;
    }

    /// <summary>Vuelca los valores persistidos en los contadores de pantalla.</summary>
    public void Refrescar()
    {
        if (textoCristalesVoto != null) textoCristalesVoto.text = string.Format(formato, GameSaveState.CristalesVoto);
        if (textoOroPuro != null) textoOroPuro.text = string.Format(formato, GameSaveState.OroPuro);
        if (textoEsencias != null) textoEsencias.text = string.Format(formato, GameSaveState.Esencias);
    }
}
