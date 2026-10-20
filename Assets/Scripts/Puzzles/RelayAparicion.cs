using UnityEngine;

/// <summary>
/// Relé de aparición: muestra un objeto (la Llave de las Zarzas) cuando la
/// entidad con HealthComponent de este GameObject muere. Se usa en el pedestal
/// destructible de la Arena del Jefe del Nivel 1.
/// </summary>
public class RelayAparicion : MonoBehaviour
{
    [SerializeField] private GameObject objetoARevelar;

    private bool revelado;

    private void OnEnable()
    {
        HealthComponent salud = GetComponent<HealthComponent>();
        if (salud != null) salud.OnDied += AlMorir;
    }

    private void OnDisable()
    {
        HealthComponent salud = GetComponent<HealthComponent>();
        if (salud != null) salud.OnDied -= AlMorir;
    }

    private void AlMorir()
    {
        if (revelado || objetoARevelar == null) return;

        revelado = true;
        objetoARevelar.SetActive(true);
        Debug.Log("[PUZZLE] El pedestal se rompió: la Llave de las Zarzas ha aparecido.");
    }

    public void Configurar(GameObject objeto)
    {
        objetoARevelar = objeto;
    }
}
