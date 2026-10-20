using UnityEngine;

/// <summary>
/// Interacción de entorno del héroe activo (tecla 'F').
/// Busca el IInteractuable válido más cercano dentro de un radio corto y lo ejecuta.
/// </summary>
public class HeroInteractor : MonoBehaviour
{
    [Header("Alcance de Interacción")]
    [SerializeField] private float radioInteraccion = 2.4f;
    [SerializeField] private LayerMask capasInteractuables = ~0;

    [Header("Depuración")]
    [SerializeField] private string ultimoPrompt;

    public string UltimoPrompt => ultimoPrompt;

    private PlayerInputReader entrada;
    private Collider[] buffer = new Collider[16];

    private void Awake()
    {
        entrada = GetComponent<PlayerInputReader>();
        if (entrada == null) entrada = FindFirstObjectByType<PlayerInputReader>();
    }

    private void Update()
    {
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
        return true;
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
