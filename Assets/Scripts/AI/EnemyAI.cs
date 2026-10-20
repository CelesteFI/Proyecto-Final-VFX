using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Máquina de estados finita para enemigos y minijefes.
/// Estados: Inactivo -> Patrulla -> Persecución -> Ataque, con un estado de
/// Aturdimiento que arbitra el conflicto entre PhysX (retroceso) y NavMeshAgent.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HealthComponent))]
[DisallowMultipleComponent]
public class EnemyAI : MonoBehaviour
{
    public enum Estado { Inactivo, Patrulla, Persecucion, Ataque, Aturdido, Regreso, Muerto }

    /// <summary>Arquetipos del GDD; se usan para etiquetar la barra de vida flotante.</summary>
    public enum Arquetipo { FaunaCorrupta, HuesteCelestial, BestiaPiedraVoraz, GuardianCeleste }

    [Header("Arquetipo y Presentación")]
    public Arquetipo arquetipo = Arquetipo.FaunaCorrupta;
    public string nombreVisible = "Fauna Corrupta";

    [Header("Objetivo")]
    [Tooltip("Si se deja vacío se usa automáticamente al héroe activo del Tag-Team.")]
    public Transform objetivo;
    public float rangoDeteccion = 14f;

    [Header("Ataque")]
    public float rangoAtaque = 2.4f;
    public float tiempoTelegrafiado = 0.55f;
    public float cooldownAtaque = 1.4f;
    public float danoAtaque = 12f;
    public float fuerzaRetrocesoAlHeroe = 4f;

    [Header("Patrulla")]
    public float radioPatrulla = 6f;
    public float pausaPatrulla = 1.2f;

    [Header("Zona de Guardia (Leash)")]
    [Tooltip("Radio máximo de persecución medido desde el punto de aparición.\nFauna Corrupta = 12 m · Huestes Celestiales = 10 m.")]
    public float radioPersecucionMaximo = 12f;

    [Tooltip("Los minijefes quedan confinados al radio de su arena de combate.")]
    public bool confinadoAArena = false;

    [Tooltip("Radio de la arena de combate usada por los minijefes como leash.")]
    public float radioArena = 14f;

    [Header("Sincronización NavMesh / PhysX")]
    public float duracionAturdimiento = 0.28f;

    public Estado EstadoActual => estado;
    public Arquetipo ArquetipoActual => arquetipo;

    /// <summary>True mientras un anclaje de zarzas mantiene al enemigo sin desplazarse.</summary>
    public bool Inmovilizado => temporizadorInmovilizacion > 0f;

    private NavMeshAgent agente;
    private HealthComponent salud;
    private Rigidbody cuerpoRigido;
    private TagTeamManager tagTeam;

    private Estado estado = Estado.Inactivo;
    private Vector3 origenPatrulla;
    private float temporizadorEstado;
    private float temporizadorCooldownAtaque;
    private float temporizadorInmovilizacion;
    private bool ataqueEnCurso;
    private bool retrocesoActivo;
    private Coroutine rutinaAtaque;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        salud = GetComponent<HealthComponent>();
        cuerpoRigido = GetComponent<Rigidbody>();
        origenPatrulla = transform.position;
    }

    private void Start()
    {
        tagTeam = FindFirstObjectByType<TagTeamManager>();
        if (objetivo == null && tagTeam != null) objetivo = tagTeam.transform;

        AplicarLimitesPorArquetipo();

        // El NavMesh conduce la posición: el Rigidbody queda cinemático salvo
        // durante el retroceso, donde PhysX toma el control temporalmente.
        if (cuerpoRigido != null)
        {
            cuerpoRigido.isKinematic = true;
            cuerpoRigido.useGravity = false;
        }

        if (agente != null && agente.isOnNavMesh)
        {
            agente.updatePosition = true;
            agente.isStopped = false;
        }

        CambiarEstado(Estado.Patrulla);
    }

    private void Update()
    {
        if (estado == Estado.Muerto || estado == Estado.Inactivo) return;

        // Ventana de aturdimiento: PhysX mueve la entidad, el agente está pausado.
        if (estado == Estado.Aturdido)
        {
            temporizadorEstado -= Time.deltaTime;
            if (temporizadorEstado <= 0f) ReanudarNavegacion();
            return;
        }

        if (temporizadorCooldownAtaque > 0f) temporizadorCooldownAtaque -= Time.deltaTime;
        ActualizarInmovilizacion();

        // Estado de regreso: no persigue ni ataca, solo vuelve a su guardia.
        if (estado == Estado.Regreso)
        {
            ProcesarRegreso();
            return;
        }

        if (objetivo == null)
        {
            ResolverObjetivo();
            if (objetivo == null) return;
        }

        float distancia = Vector3.Distance(transform.position, objetivo.position);

        // Ruptura de agro al salir de la zona de guardia (leash distance).
        float distanciaOrigen = Vector3.Distance(transform.position, origenPatrulla);
        if (distanciaOrigen > radioPersecucionMaximo &&
            (estado == Estado.Persecucion || estado == Estado.Ataque))
        {
            IniciarRegreso();
            return;
        }

        switch (estado)
        {
            case Estado.Patrulla:
                ProcesarPatrulla(distancia);
                break;

            case Estado.Persecucion:
                ProcesarPersecucion(distancia);
                break;

            case Estado.Ataque:
                ProcesarAtaque(distancia);
                break;
        }
    }

    // ------------------------------------------------------------------
    // ESTADOS
    // ------------------------------------------------------------------

    private void ProcesarPatrulla(float distancia)
    {
        if (distancia <= rangoDeteccion)
        {
            CambiarEstado(Estado.Persecucion);
            return;
        }

        temporizadorEstado -= Time.deltaTime;

        bool llego = !agente.pathPending && agente.remainingDistance <= Mathf.Max(agente.stoppingDistance, 0.35f);

        if (temporizadorEstado <= 0f || llego)
        {
            Vector3 candidato = origenPatrulla + Random.insideUnitSphere * radioPatrulla;
            candidato.y = origenPatrulla.y;

            if (NavMesh.SamplePosition(candidato, out NavMeshHit punto, 4f, NavMesh.AllAreas))
            {
                agente.SetDestination(punto.position);
            }

            temporizadorEstado = pausaPatrulla + Random.Range(0f, 1.2f);
        }
    }

    private void ProcesarPersecucion(float distancia)
    {
        // Anclaje de zarzas: el enemigo no se desplaza mientras dure el efecto.
        if (Inmovilizado)
        {
            agente.isStopped = true;
            return;
        }

        if (distancia > rangoDeteccion * 1.35f)
        {
            CambiarEstado(Estado.Patrulla);
            return;
        }

        if (distancia <= rangoAtaque)
        {
            CambiarEstado(Estado.Ataque);
            return;
        }

        agente.isStopped = false;
        agente.SetDestination(objetivo.position);
    }

    private void ProcesarAtaque(float distancia)
    {
        // Anclaje de zarzas: tampoco puede golpear mientras esté inmovilizado.
        if (Inmovilizado)
        {
            agente.isStopped = true;
            return;
        }

        // Se detiene y encara al héroe para telegrafiar el golpe.
        agente.isStopped = true;
        EncararObjetivo();

        if (distancia > rangoAtaque * 1.25f)
        {
            CambiarEstado(Estado.Persecucion);
            return;
        }

        if (!ataqueEnCurso && temporizadorCooldownAtaque <= 0f)
        {
            rutinaAtaque = StartCoroutine(RutinaAtaqueTelegrafiado());
        }
    }

    /// <summary>
    /// Regreso a la guardia: cancela el ataque en curso, se deshace del impulso
    /// físico y camina de vuelta al punto de aparición. Al llegar regenera la
    /// salud al máximo y retoma la patrulla.
    /// </summary>
    private void ProcesarRegreso()
    {
        if (agente == null || !agente.isOnNavMesh)
        {
            CambiarEstado(Estado.Patrulla);
            return;
        }

        agente.isStopped = Inmovilizado;
        if (Inmovilizado) return;

        bool llego = !agente.pathPending &&
                     agente.remainingDistance <= Mathf.Max(agente.stoppingDistance, 0.5f);

        if (!llego) return;

        if (NavMesh.SamplePosition(origenPatrulla, out NavMeshHit punto, 2f, NavMesh.AllAreas))
            agente.Warp(punto.position);

        if (salud != null) salud.RestaurarVidaCompleta();

        CambiarEstado(Estado.Patrulla);
    }

    private System.Collections.IEnumerator RutinaAtaqueTelegrafiado()
    {
        ataqueEnCurso = true;

        // Telegrafiado: pausa legible antes de golpear.
        Vector3 escalaBase = transform.localScale;
        float t = 0f;
        while (t < tiempoTelegrafiado)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaBase, escalaBase * 1.08f, Mathf.PingPong(t / tiempoTelegrafiado, 1f));
            yield return null;
        }
        transform.localScale = escalaBase;

        if (estado == Estado.Ataque && objetivo != null)
        {
            float distancia = Vector3.Distance(transform.position, objetivo.position);
            if (distancia <= rangoAtaque * 1.35f) EjecutarGolpe();
        }

        temporizadorCooldownAtaque = cooldownAtaque;
        ataqueEnCurso = false;
    }

    private void EjecutarGolpe()
    {
        HealthComponent saludHeroe = (tagTeam != null) ? tagTeam.SaludHeroe : null;

        if (saludHeroe == null || saludHeroe.EstaMuerto) return;

        Vector3 direccion = (objetivo.position - transform.position).normalized;
        Vector3 fuerza = direccion * fuerzaRetrocesoAlHeroe + Vector3.up * 0.6f;
        Vector3 punto = objetivo.position + Vector3.up * 1f;

        saludHeroe.ApplyDamage(danoAtaque, fuerza, punto);

        if (CameraFollow.Instance != null) CameraFollow.Instance.TriggerScreenShake(0.12f, 0.12f);
    }

    // ------------------------------------------------------------------
    // RETROCESO FÍSICO Y SINCRONIZACIÓN CON NAVMESH
    // ------------------------------------------------------------------

    /// <summary>
    /// Pausa el agente y deja que PhysX impulse el Rigidbody.
    /// Al terminar, se reposiciona el agente sobre la malla con Warp para
    /// evitar desincronización entre transform físico y posición de pathfinding.
    /// </summary>
    public void AplicarRetroceso(Vector3 fuerza)
    {
        if (estado == Estado.Muerto) return;

        retrocesoActivo = true;
        estado = Estado.Aturdido;
        temporizadorEstado = duracionAturdimiento;

        if (agente != null)
        {
            agente.isStopped = true;
            agente.updatePosition = false;
            agente.updateRotation = false;
        }

        if (cuerpoRigido != null)
        {
            cuerpoRigido.isKinematic = false;
            cuerpoRigido.useGravity = true;
            cuerpoRigido.linearVelocity = Vector3.zero;
            cuerpoRigido.angularVelocity = Vector3.zero;
            cuerpoRigido.AddForce(fuerza, ForceMode.Impulse);
        }
    }

    private void ReanudarNavegacion()
    {
        if (estado == Estado.Muerto) return;

        if (cuerpoRigido != null)
        {
            // Un Rigidbody cinemático rechaza linearVelocity/angularVelocity y emite
            // advertencia. Solo se anulan cuando el cuerpo NO es cinemático, que es
            // el caso tras Aturdido; si ya lo era, la velocidad no tiene efecto.
            if (!cuerpoRigido.isKinematic)
            {
                cuerpoRigido.linearVelocity = Vector3.zero;
                cuerpoRigido.angularVelocity = Vector3.zero;
            }

            cuerpoRigido.isKinematic = true;
            cuerpoRigido.useGravity = false;
        }

        if (agente != null && agente.enabled)
        {
            // Reposiciona el agente sobre la malla de navegación más cercana.
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit punto, 2.5f, NavMesh.AllAreas))
            {
                agente.Warp(punto.position);
            }

            agente.updatePosition = true;
            agente.updateRotation = true;
            agente.isStopped = false;
        }

        retrocesoActivo = false;
        CambiarEstado(Estado.Persecucion);
    }

    /// <summary>Detiene la IA y apaga el agente al morir.</summary>
    public void NotificarMuerte()
    {
        estado = Estado.Muerto;
        StopAllCoroutines();
        rutinaAtaque = null;
        ataqueEnCurso = false;

        if (agente != null)
        {
            agente.isStopped = true;
            agente.enabled = false;
        }

        if (cuerpoRigido != null && !cuerpoRigido.isKinematic)
        {
            cuerpoRigido.linearVelocity = Vector3.zero;
        }
    }

    // ------------------------------------------------------------------
    // ZONA DE GUARDIA, ANCLAJE DE ZARZAS Y REINICIO
    // ------------------------------------------------------------------

    /// <summary>
    /// Fija el leash según el arquetipo del GDD: la fauna corrupta persigue
    /// hasta 12 m, las huestes celestiales hasta 10 m y los minijefes quedan
    /// confinados al radio de su propia arena de combate.
    /// </summary>
    private void AplicarLimitesPorArquetipo()
    {
        switch (arquetipo)
        {
            case Arquetipo.FaunaCorrupta:
                radioPersecucionMaximo = 12f;
                confinadoAArena = false;
                break;

            case Arquetipo.HuesteCelestial:
                radioPersecucionMaximo = 10f;
                confinadoAArena = false;
                break;

            case Arquetipo.BestiaPiedraVoraz:
            case Arquetipo.GuardianCeleste:
                confinadoAArena = true;
                radioPersecucionMaximo = radioArena;
                break;
        }
    }

    /// <summary>Cancela el ataque en curso y ordena volver caminando al punto de guardia.</summary>
    private void IniciarRegreso()
    {
        if (rutinaAtaque != null)
        {
            StopCoroutine(rutinaAtaque);
            rutinaAtaque = null;
        }

        ataqueEnCurso = false;
        temporizadorCooldownAtaque = 0f;

        // Se abandona el control físico para que el NavMeshAgent retome la posición.
        if (cuerpoRigido != null && !cuerpoRigido.isKinematic)
        {
            cuerpoRigido.linearVelocity = Vector3.zero;
            cuerpoRigido.angularVelocity = Vector3.zero;
            cuerpoRigido.isKinematic = true;
            cuerpoRigido.useGravity = false;
        }

        if (agente != null && agente.isOnNavMesh)
        {
            agente.updatePosition = true;
            agente.updateRotation = true;
            agente.isStopped = false;
            agente.SetDestination(origenPatrulla);
        }

        Debug.Log($"[IA] {nombreVisible} rompe el agro a {Vector3.Distance(transform.position, origenPatrulla):F1} m de su guardia (leash {radioPersecucionMaximo:F0} m).");

        CambiarEstado(Estado.Regreso);
    }

    /// <summary>Anclaje de zarzas de Illya: inmoviliza sin desplazar al enemigo.</summary>
    public void AplicarInmovilizacion(float duracion)
    {
        if (estado == Estado.Muerto || duracion <= 0f) return;

        temporizadorInmovilizacion = Mathf.Max(temporizadorInmovilizacion, duracion);

        if (agente != null && agente.enabled && agente.isOnNavMesh) agente.isStopped = true;
    }

    /// <summary>Cuenta atrás del anclaje y liberación del agente al expirar.</summary>
    private void ActualizarInmovilizacion()
    {
        if (temporizadorInmovilizacion <= 0f) return;

        temporizadorInmovilizacion -= Time.deltaTime;
        if (temporizadorInmovilizacion > 0f) return;

        if (agente != null && agente.enabled && agente.isOnNavMesh && estado == Estado.Persecucion)
            agente.isStopped = false;
    }

    /// <summary>
    /// Reinicia al enemigo a su estado inicial: vuelve a su guardia con la salud
    /// completa. Lo usa el gestor de derrota al reaparecer el héroe.
    /// </summary>
    public void ReiniciarEstado()
    {
        if (estado == Estado.Muerto) return;

        StopAllCoroutines();
        rutinaAtaque = null;
        ataqueEnCurso = false;
        temporizadorCooldownAtaque = 0f;
        temporizadorInmovilizacion = 0f;
        temporizadorEstado = 0f;

        if (cuerpoRigido != null)
        {
            // Un Rigidbody cinemático rechaza linearVelocity/angularVelocity y emite
            // advertencia. Solo se anulan cuando el cuerpo NO es cinemático, que es
            // el caso tras Aturdido; si ya lo era, la velocidad no tiene efecto.
            if (!cuerpoRigido.isKinematic)
            {
                cuerpoRigido.linearVelocity = Vector3.zero;
                cuerpoRigido.angularVelocity = Vector3.zero;
            }

            cuerpoRigido.isKinematic = true;
            cuerpoRigido.useGravity = false;
        }

        if (agente != null && agente.enabled && agente.isOnNavMesh)
        {
            agente.Warp(origenPatrulla);
            agente.updatePosition = true;
            agente.updateRotation = true;
            agente.isStopped = false;
        }

        transform.position = origenPatrulla;

        if (salud != null) salud.RestaurarVidaCompleta();

        estado = Estado.Patrulla;
    }

    // ------------------------------------------------------------------
    // APOYO
    // ------------------------------------------------------------------

    private void ResolverObjetivo()
    {
        if (tagTeam == null) tagTeam = FindFirstObjectByType<TagTeamManager>();
        if (tagTeam != null) objetivo = tagTeam.transform;
    }

    private void EncararObjetivo()
    {
        if (objetivo == null) return;

        Vector3 direccion = objetivo.position - transform.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direccion.normalized),
            Time.deltaTime * 8f);
    }

    private void CambiarEstado(Estado nuevo)
    {
        if (estado == nuevo) return;
        estado = nuevo;

        if (nuevo == Estado.Patrulla && agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = false;
            temporizadorEstado = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(Application.isPlaying ? origenPatrulla : transform.position, radioPatrulla);
    }

    /// <summary>Expuesto para pruebas y depuración.</summary>
    public bool RetrocesoActivo => retrocesoActivo;
}
