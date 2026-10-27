using UnityEngine;

/// <summary>
/// Ráfaga huracanada intermitente del pasillo secreto de la Isla Aguja.
/// Ciclo de 3 s de viento y 2 s de calma: mientras sopla, empuja al héroe hacia
/// atrás salvo que haya un Muro de Basalto usado como trinchera.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RafagaViento : MonoBehaviour
{
    [Header("Ciclo")]
    [SerializeField] private float duracionViento = 3f;
    [SerializeField] private float duracionCalma = 2f;

    [Header("Empuje")]
    [SerializeField] private Vector3 direccionEmpuje = new Vector3(0f, 0f, -1f);
    [SerializeField] private float fuerza = 5.5f;
    [SerializeField] private float radioBloqueoMuro = 7f;

    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("PlayerController del héroe. No se busca en runtime.")]
    [SerializeField] private PlayerController jugador;

    public bool Soplando { get; private set; }

    private float temporizador;
    private Collider zona;

    private void Awake()
    {
        zona = GetComponent<Collider>();
        zona.isTrigger = true;
        temporizador = duracionViento;
        Soplando = true;
    }

    private void Update()
    {
        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            Soplando = !Soplando;
            temporizador = Soplando ? duracionViento : duracionCalma;
        }

        if (!Soplando) return;
        if (HayMuroDeTrinchera()) return;

        if (jugador == null) return;

        if (!DentroDeLaZona(jugador.transform.position)) return;

        jugador.AplicarEmpujeExterno(direccionEmpuje.normalized * fuerza * Time.deltaTime);
    }

    /// <summary>Comprueba si hay un Muro de Basalto bloqueando la corriente.</summary>
    private bool HayMuroDeTrinchera()
    {
        Collider[] encontrados = Physics.OverlapSphere(transform.position, radioBloqueoMuro);
        for (int i = 0; i < encontrados.Length; i++)
        {
            if (encontrados[i] != null && encontrados[i].GetComponentInParent<MarcadorMuroBasalto>() != null)
                return true;
        }
        return false;
    }

    private bool DentroDeLaZona(Vector3 posicion)
    {
        if (zona == null) return true;
        Bounds b = zona.bounds;
        b.Expand(0.5f);
        return b.Contains(posicion);
    }

    public void Configurar(Vector3 direccion, float fuerzaEmpuje)
    {
        direccionEmpuje = direccion;
        fuerza = fuerzaEmpuje;
    }
}
