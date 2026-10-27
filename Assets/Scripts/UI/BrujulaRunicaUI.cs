using UnityEngine;

/// <summary>
/// Brújula rúnica del HUD (Superior Derecha).
/// Rota la aguja en Z según la orientación Y del héroe respecto al norte del
/// mundo, con Mathf.SmoothDampAngle para que no vibre en los cambios bruscos.
/// </summary>
[DisallowMultipleComponent]
public class BrujulaRunicaUI : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("RectTransform de la aguja dorada (Indicador_Orientacion).")]
    [SerializeField] private RectTransform aguja;

    [Tooltip("Transform del Player. Se asigna a mano, sin búsquedas en runtime.")]
    [SerializeField] private Transform jugador;

    [Header("Suavizado")]
    [SerializeField] private float suavizado = 0.12f;

    private float velocidad;

    private void LateUpdate()
    {
        if (aguja == null || jugador == null) return;

        // Norte del mundo = 0 en Z. Girar el héroe a la derecha hace girar la aguja al revés.
        float objetivo = -jugador.eulerAngles.y;
        float actual = Mathf.SmoothDampAngle(aguja.localEulerAngles.z, objetivo, ref velocidad, suavizado);

        aguja.localRotation = Quaternion.Euler(0f, 0f, actual);
    }
}
