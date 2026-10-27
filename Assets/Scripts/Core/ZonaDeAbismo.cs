using UnityEngine;

/// <summary>
/// Red de seguridad del abismo del Nivel 2. El volumen de muerte
/// (00_KillZone_Abismo, en Y = −12) no tenía ningún comportamiento asociado:
/// el héroe atravesaba el trigger y seguía cayendo indefinidamente.
///
/// Esta zona muestrea la última posición en la que el CharacterController estaba
/// apoyado y devuelve al héroe ahí cuando cruza el volumen. Reutiliza la misma
/// técnica de teletransporte que <see cref="GeiserRescate"/>: desactivar el
/// CharacterController, mover el transform y volver a activarlo, para que la
/// colisión no cancele la reubicación.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaDeAbismo : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("TagTeamManager del Player. No se busca en runtime.")]
    [SerializeField] private TagTeamManager tagTeam;

    [Header("Reaparición")]
    [Tooltip("Separación vertical sobre la última posición firme para evitar reaparecer dentro del suelo.")]
    [SerializeField] private Vector3 desplazamientoSeguro = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Cada cuántos segundos se registra la posición firme del héroe.")]
    [SerializeField] private float intervaloMuestreo = 0.35f;
    [Tooltip("Por debajo de esta altura no se registra la posición como segura.")]
    [SerializeField] private float alturaMinimaSegura = -5f;
    [Tooltip("Tiempo mínimo entre dos reposicionamientos para no encadenar rescates.")]
    [SerializeField] private float enfriamiento = 0.3f;
    [Tooltip("Distancia al borde que debe tener un punto para considerarse firme. Evita reaparecer al filo de la isla y volver a caer.")]
    [SerializeField] private float radioSeguro = 1.2f;

    [Header("Daño por Caída")]
    [Tooltip("Si está activo, cruzar el volumen aplica daño al héroe activo antes de devolverlo a tierra.")]
    [SerializeField] private bool aplicarDano = false;

    [Tooltip("Fracción de la vida máxima que resta la caída. El GDD pide un 25 %.")]
    [Range(0f, 1f)]
    [SerializeField] private float porcentajeDano = 0.25f;

    /// <summary>Número de veces que el abismo ha devuelto al héroe (útil para depurar).</summary>
    public int Reapariciones { get; private set; }

    private CharacterController caracter;
    private Transform transformHeroe;
    private Vector3 ultimaPosicionSegura;
    private float muestreo;
    private float enfriamientoRestante;
    private bool tienePosicionSegura;

    private void Awake()
    {
        Collider volumen = GetComponent<Collider>();
        if (volumen != null) volumen.isTrigger = true;
    }

    private void Start()
    {
        ResolverHeroe();
    }

    private void Update()
    {
        if (enfriamientoRestante > 0f) enfriamientoRestante -= Time.deltaTime;

        if (caracter == null)
        {
            ResolverHeroe();
            if (caracter == null) return;
        }

        muestreo -= Time.deltaTime;
        if (muestreo > 0f) return;
        muestreo = intervaloMuestreo;

        if (!caracter.isGrounded) return;
        if (transformHeroe.position.y < alturaMinimaSegura) return;

        // Solo se memoriza suelo que se extiende al menos radioSeguro en las cuatro
        // direcciones: si se guardaba el filo de la isla, el rescate devolvía al
        // héroe al borde y volvía a caer, encadenando dos rescates por caída.
        if (!EsPuntoFirme(transformHeroe.position)) return;

        ultimaPosicionSegura = transformHeroe.position;
        tienePosicionSegura = true;
    }

    /// <summary>Comprueba que hay suelo firme alrededor del punto, no un borde.</summary>
    private bool EsPuntoFirme(Vector3 posicion)
    {
        Vector3 origen = posicion + Vector3.up * 0.6f;
        const float alcance = 2.5f;

        if (!Physics.Raycast(origen, Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(origen + Vector3.forward * radioSeguro, Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(origen + Vector3.back * radioSeguro, Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(origen + Vector3.right * radioSeguro, Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(origen + Vector3.left * radioSeguro, Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore)) return false;

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enfriamientoRestante > 0f) return;

        CharacterController candidato = other.GetComponentInParent<CharacterController>();
        if (candidato == null) return;

        Reposicionar(candidato);
    }

    /// <summary>Devuelve al héroe a la última posición firme registrada.</summary>
    private void Reposicionar(CharacterController candidato)
    {
        // La caída al abismo cuesta vida: si llega a 0 el Tag-Team releva al
        // compañero y, si ambos caen, se abre 'Modal_Derrota'.
        AplicarDanoDeCaida();

        Vector3 destino = tienePosicionSegura
            ? ultimaPosicionSegura + desplazamientoSeguro
            : new Vector3(0f, 2f, 0f);

        // PlayerController.Reubicar anula la velocidad acumulada; sin eso el héroe
        // reaparece conservando la caída y vuelve a cruzar el volumen de muerte.
        PlayerController locomocion = candidato.GetComponent<PlayerController>();
        if (locomocion != null)
        {
            locomocion.Reubicar(destino);
        }
        else
        {
            candidato.enabled = false;
            candidato.transform.position = destino;
            candidato.enabled = true;
        }

        // No se toma el destino como referencia: está en el aire y encadenaría
        // reposicionamientos cada vez más altos. La referencia sigue siendo la
        // última posición realmente pisada, que Update vuelve a muestrear.
        muestreo = intervaloMuestreo;
        enfriamientoRestante = enfriamiento;

        Reapariciones++;
        Debug.Log($"[ABISMO] Héroe devuelto a la última posición firme: {destino.ToString("F1")}");
    }

    /// <summary>
    /// Aplica al héroe activo un porcentaje de su vida máxima. Se ejecuta antes
    /// de la reubicación para que el relevo automático (si la vida llega a 0)
    /// ocurra con el héroe todavía en el volumen de muerte.
    /// </summary>
    private void AplicarDanoDeCaida()
    {
        if (!aplicarDano || porcentajeDano <= 0f) return;

        TagTeamManager tag = TagTeamManager.Instance;
        if (tag == null || tag.SaludHeroe == null) return;
        if (tag.SaludHeroe.EstaMuerto) return;

        float dano = tag.SaludHeroe.MaxHealth * Mathf.Clamp01(porcentajeDano);
        Vector3 punto = tag.transform.position + Vector3.up * 1f;

        tag.SaludHeroe.ApplyDamage(dano, Vector3.zero, punto);
        Debug.Log($"[ABISMO] Caída: {dano:F0} de daño ({porcentajeDano * 100f:F0} % de la vida máxima).");
    }

    private void ResolverHeroe()
    {
        if (tagTeam == null || transformHeroe != null) return;

        transformHeroe = tagTeam.transform;
        caracter = tagTeam.GetComponent<CharacterController>();

        if (caracter != null)
        {
            ultimaPosicionSegura = transformHeroe.position;
            tienePosicionSegura = true;
        }
    }

    /// <summary>Activa el daño de caída y su porcentaje desde el integrador de escena.</summary>
    public void ConfigurarDanoDeCaida(bool activo, float porcentaje)
    {
        aplicarDano = activo;
        porcentajeDano = Mathf.Clamp01(porcentaje);
    }
}
