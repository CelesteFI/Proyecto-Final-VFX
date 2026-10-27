using TMPro;
using UnityEngine;

/// <summary>
/// Interacción de entorno del héroe activo (tecla 'F').
/// Busca el IInteractuable válido más cercano dentro de un radio corto, enciende
/// su halo rúnico y publica el aviso en el banner de instrucciones del HUD.
/// No crea ningún elemento de interfaz: halo y banner ya existen en la escena.
/// </summary>
public class HeroInteractor : MonoBehaviour
{
    [Header("Alcance de Interacción")]
    [SerializeField] private float radioInteraccion = 2.5f;
    [SerializeField] private LayerMask capasInteractuables = ~0;

    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("PlayerInputReader del mismo Player. No se busca en runtime.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Halo Rúnico")]
    [Tooltip("Al entrar en el radio se enciende el 'Halo_Brillo_Runico' del objeto.")]
    [SerializeField] private bool usarHalo = true;

    [Header("Banner de Instrucciones del HUD")]
    [Tooltip("GameObject 'Banner_Instrucciones'. Oculto por defecto en la Hierarchy.")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeInteraccion = "[F] Interactuar";

    [Header("Depuración")]
    [SerializeField] private string ultimoPrompt;

    public string UltimoPrompt => ultimoPrompt;

    /// <summary>True mientras hay un interactuable dentro del radio.</summary>
    public bool HayObjetivoEnRango => objetivoActual != null;

    private readonly Collider[] buffer = new Collider[16];

    private IInteractuable mejorActual;
    private Collider objetivoActual;
    private HaloBrillo haloActual;

    private void Awake()
    {
        if (entrada == null) entrada = GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        if (bannerInstrucciones != null) bannerInstrucciones.SetActive(false);
    }

    private void Update()
    {
        // 1. Sondeo del entorno: enciende halo y banner al entrar en el radio.
        IInteractuable cercano = BuscarInteractuableCercano(out Collider colliderCercano);
        AplicarObjetivo(cercano, colliderCercano);

        // 2. El banner contextual se reafirma cada frame mientras haya objetivo,
        //    de modo que ningún otro sistema pueda dejar un texto fijo encima.
        if (mejorActual != null) MostrarBanner();

        // 3. Consumo de la pulsación 'F'.
        if (entrada == null || !entrada.InteraccionPulsada) return;
        IntentarInteractuar();
    }

    /// <summary>Ejecuta la interacción disponible. Devuelve true si algo respondió.</summary>
    public bool IntentarInteractuar()
    {
        IInteractuable mejor = BuscarInteractuableCercano(out Collider objetivo);
        if (mejor == null) return false;

        ultimoPrompt = mejor.TextoPrompt;
        mejor.Interactuar(transform);

        if (objetivo != null) Debug.Log($"[INTERACCIÓN] {mejor.TextoPrompt} sobre {objetivo.gameObject.name}");

        // Tras accionar el objeto se apagan halo y banner de inmediato.
        ApagarHaloActual();
        OcultarBanner();

        return true;
    }

    /// <summary>
    /// Conmuta halo y banner cuando cambia el objetivo en rango.
    /// Un objetivo consumido se apaga solo en el siguiente sondeo.
    /// </summary>
    private void AplicarObjetivo(IInteractuable nuevo, Collider colliderNuevo)
    {
        if (ReferenceEquals(nuevo, mejorActual) && colliderNuevo == objetivoActual) return;

        ApagarHaloActual();

        mejorActual = nuevo;
        objetivoActual = colliderNuevo;

        if (nuevo == null)
        {
            OcultarBanner();
            return;
        }

        if (usarHalo && colliderNuevo != null)
        {
            haloActual = colliderNuevo.GetComponentInParent<HaloBrillo>();
            if (haloActual != null) haloActual.Encender(true);
        }

        MostrarBanner();
    }

    private void MostrarBanner()
    {
        if (bannerInstrucciones != null && !bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(true);

        if (textoInstruccion != null) textoInstruccion.text = mensajeInteraccion;
    }

    private void OcultarBanner()
    {
        if (bannerInstrucciones != null && bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(false);
    }

    private void ApagarHaloActual()
    {
        if (haloActual != null) haloActual.Encender(false);
        haloActual = null;
    }

    private void OnDisable()
    {
        ApagarHaloActual();
        OcultarBanner();
        mejorActual = null;
        objetivoActual = null;
    }

    private IInteractuable BuscarInteractuableCercano(out Collider encontrado)
    {
        encontrado = null;
        int total = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.9f, radioInteraccion, buffer, capasInteractuables);

        IInteractuable mejor = null;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < total; i++)
        {
            Collider col = buffer[i];
            if (col == null) continue;

            IInteractuable candidato = col.GetComponentInParent<IInteractuable>();
            if (candidato == null || !candidato.PuedeInteractuar) continue;

            float distancia = Vector3.Distance(transform.position, col.transform.position);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = candidato;
                encontrado = col;
            }
        }

        return mejor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, radioInteraccion);
    }
}
