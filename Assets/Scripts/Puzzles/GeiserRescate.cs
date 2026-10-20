using UnityEngine;

/// <summary>
/// Geiser ascendente de rescate del Nivel 2. Si el héroe cae de la ruta de bulbos,
/// esta corriente lo devuelve a un punto seguro en lugar de perder la partida.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GeiserRescate : MonoBehaviour
{
    [Header("Destino del Rescate")]
    [SerializeField] private Transform puntoRescate;
    [SerializeField] private Vector3 desplazamientoSeguro = new Vector3(0f, 2f, 0f);

    [Header("Enfriamiento")]
    [SerializeField] private float enfriamiento = 1.5f;

    public int RescatesRealizados { get; private set; }

    private float temporizador;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (temporizador > 0f) temporizador -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (temporizador > 0f) return;

        TagTeamManager tag = other.GetComponentInParent<TagTeamManager>();
        if (tag == null) return;

        ReposicionarHeroe(tag);
    }

    /// <summary>Reposiciona al héroe en el punto seguro (desactivando el CharacterController durante el teleport).</summary>
    public void ReposicionarHeroe(TagTeamManager tag)
    {
        Transform destino = (puntoRescate != null) ? puntoRescate : transform;
        Vector3 posicion = (puntoRescate != null) ? destino.position : transform.position + desplazamientoSeguro;

        // Preferente: Reubicar() anula la velocidad acumulada (~25 m/s en caída).
        // Sin eso el héroe reaparecía con la caída intacta y volvía a cruzar el
        // volumen, igual que ocurría en la red de seguridad del abismo.
        PlayerController controlador = tag.GetComponent<PlayerController>();
        if (controlador != null)
        {
            controlador.Reubicar(posicion);
        }
        else
        {
            CharacterController caracter = tag.GetComponent<CharacterController>();
            if (caracter != null) caracter.enabled = false;

            tag.transform.position = posicion;

            if (caracter != null) caracter.enabled = true;
        }

        temporizador = enfriamiento;
        RescatesRealizados++;

        Debug.Log($"[GEISER] Corriente ascendente: héroe rescatado a {posicion.ToString("F1")}.");
    }

    public void Configurar(Transform punto)
    {
        puntoRescate = punto;
    }
}
