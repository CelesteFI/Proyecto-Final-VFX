using UnityEngine;

/// <summary>
/// Puzle 1 del Nivel 1: la barrera de zarzas que sella el pie de la Escalinata
/// Monumental. Se disuelve al encadenar las dos placas de presión (la placa A
/// enciende el canal rúnico y la placa B lo confirma dentro de la ventana de
/// 5 s) o al destruirla a golpes. En ambos casos registra el 20 % de progreso.
/// No hace búsquedas en runtime: la barrera está asignada en el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class RaicesNivel1 : MonoBehaviour
{
    [Header("Barrera de raíces (asignada en el Inspector)")]
    [SerializeField] private ObstaculoDestruible barrera;

    public bool Resuelto => resuelto;

    private bool resuelto;

    /// <summary>
    /// Disuelve la barrera de raíces y registra el hito del 20 %.
    /// Lo invocan la placa de presión B y la destrucción a golpes de la barrera.
    /// </summary>
    public void ResolverPuzle()
    {
        if (resuelto) return;
        resuelto = true;

        if (barrera != null) barrera.Disolver();

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.puzzleRaicesNivel1Resuelto = true;

        Debug.Log("[PUZZLE] La barrera de raíces cede: el paso a la escalinata queda abierto.");
    }
}
