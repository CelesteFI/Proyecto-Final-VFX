using UnityEngine;

/// <summary>
/// Corriente de la Tobera del Viento (Nivel 2). Mientras la placa de presión
/// asociada permanece energizada (8 s), la columna de aire impulsa al héroe
/// hacia arriba y hacia la Isla del Templo para cruzar el abismo.
/// No hace búsquedas en runtime: la placa y el héroe se asignan en el Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class ToberaViento : MonoBehaviour
{
    [Header("Disparo (asignado en el Inspector)")]
    [Tooltip("Placa de presión que abre la corriente durante su ventana de energía.")]
    [SerializeField] private WeightPressurePlate placa;

    [Header("Empuje")]
    [Tooltip("Dirección normalizada del soplo (arriba y hacia la Isla del Templo).")]
    [SerializeField] private Vector3 direccion = new Vector3(0.5f, 1f, 0.15f);
    [SerializeField] private float fuerza = 10f;

    [Header("Enlaces (asignados en el Inspector)")]
    [SerializeField] private PlayerController jugador;

    /// <summary>True mientras la placa mantiene abierta la corriente.</summary>
    public bool Activa => placa != null && placa.EstaEnergizada;

    private Collider zona;

    private void Awake()
    {
        zona = GetComponent<Collider>();
        zona.isTrigger = true;
    }

    private void Update()
    {
        if (!Activa) return;
        if (jugador == null) return;
        if (!DentroDeLaZona(jugador.transform.position)) return;

        jugador.AplicarEmpujeExterno(direccion.normalized * fuerza * Time.deltaTime);
    }

    /// <summary>Comprueba si el héroe está dentro de la columna de aire.</summary>
    private bool DentroDeLaZona(Vector3 posicion)
    {
        if (zona == null) return true;

        Bounds b = zona.bounds;
        b.Expand(1f);
        return b.Contains(posicion);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(WeightPressurePlate placaTobera, PlayerController heroe,
                           Vector3 direccionSoplo, float fuerzaSoplo)
    {
        placa = placaTobera;
        jugador = heroe;
        direccion = direccionSoplo;
        fuerza = fuerzaSoplo;
    }
}
