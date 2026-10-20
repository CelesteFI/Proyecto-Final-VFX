using UnityEngine;

/// <summary>
/// Orienta un elemento de UI en World Space hacia la cámara principal.
/// Se usa en las barras de vida flotantes de todos los enemigos.
/// </summary>
[DefaultExecutionOrder(50)]
public class Billboard : MonoBehaviour
{
    [Header("Orientación")]
    [Tooltip("Si está activo, la barra mantiene el eje vertical fijo (no se inclina).")]
    [SerializeField] private bool bloquearEjeY = true;

    [SerializeField] private bool invertir = false;

    private Transform camara;

    private void LateUpdate()
    {
        if (camara == null)
        {
            if (Camera.main == null) return;
            camara = Camera.main.transform;
        }

        Vector3 direccion = transform.position - camara.position;
        if (bloquearEjeY) direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(invertir ? -direccion : direccion);
    }
}
