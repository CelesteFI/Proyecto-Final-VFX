using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Módulo de combate del héroe activo. Se desacopla de TagTeamManager:
/// esta clase resuelve hitbox, daño, VFX de impacto, hitstop y la habilidad
/// elemental; el gestor de Tag-Team solo conserva los datos y el relevo.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Enlaces")]
    [SerializeField] private TagTeamManager tagTeam;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask capasAfectadas = 1;

    [Header("VFX de Impacto (opcional, se cargan de Resources si están vacíos)")]
    [SerializeField] private GameObject vfxImpactoPiedra;
    [SerializeField] private GameObject vfxImpactoMadera;

    [Header("Habilidad: Muro de Basalto")]
    [SerializeField] private GameObject prefabMuroBasalto;
    [SerializeField] private float distanciaMuro = 2.5f;

    [Header("Habilidad: Raíces Voraces")]
    [SerializeField] private GameObject prefabRaicesAoE;
    [SerializeField] private float radioRaices = 3f;
    [SerializeField] private float danoRaices = 15f;
    [Tooltip("Duración del anclaje de zarzas que inmoviliza a los enemigos alcanzados.")]
    [SerializeField] private float duracionZarzas = 2.5f;

    public float CooldownAtaqueRestante => temporizadorAtaque;

    private PlayerInputReader entrada;
    private CharacterController controlador;
    private float temporizadorAtaque;

    private readonly List<Collider> bufferHits = new List<Collider>(24);
    private readonly HashSet<int> raicesProcesadas = new HashSet<int>();

    private void Awake()
    {
        if (tagTeam == null) tagTeam = GetComponent<TagTeamManager>();
        if (tagTeam == null) tagTeam = FindFirstObjectByType<TagTeamManager>();

        entrada = GetComponent<PlayerInputReader>();
        if (entrada == null) entrada = FindFirstObjectByType<PlayerInputReader>();

        controlador = GetComponent<CharacterController>();

        if (attackPoint == null && tagTeam != null) attackPoint = tagTeam.attackPoint;
        if (capasAfectadas == 0) capasAfectadas = 1;

        // Carga de recursos: permite que el prefab de impacto sea intercambiable
        // sin volver a cablear la escena.
        if (vfxImpactoPiedra == null) vfxImpactoPiedra = Resources.Load<GameObject>("VFX/VFX_Impacto_Piedra");
        if (vfxImpactoMadera == null) vfxImpactoMadera = Resources.Load<GameObject>("VFX/VFX_Impacto_Madera");
        if (prefabMuroBasalto == null && tagTeam != null) prefabMuroBasalto = tagTeam.vfxRockWallPrefab;
        if (prefabMuroBasalto == null) prefabMuroBasalto = Resources.Load<GameObject>("VFX/Muro_Basalto_Habilidad");
    }

    private void Update()
    {
        if (entrada == null || tagTeam == null) return;

        if (temporizadorAtaque > 0f) temporizadorAtaque -= Time.deltaTime;

        if (entrada.AtaquePulsado && temporizadorAtaque <= 0f) EjecutarAtaqueBasico();
        if (entrada.HabilidadPulsada) EjecutarHabilidadElemental();
    }

    // ------------------------------------------------------------------
    // ATAQUE BÁSICO
    // ------------------------------------------------------------------

    /// <summary>
    /// Golpe cuerpo a cuerpo con hitbox esférica sobre el AttackPoint.
    /// El compromiso físico y el VFX se ejecutan SIEMPRE, conecte o no, para que
    /// el jugador perciba la intención del ataque; el daño y el retroceso solo se
    /// aplican a los objetivos dentro del rango de la capa afectada.
    /// </summary>
    public void EjecutarAtaqueBasico()
    {
        HeroCombatData heroe = tagTeam.ActiveHero;
        if (heroe == null) return;

        temporizadorAtaque = heroe.attackCooldown;

        // 1. Compromiso físico: traslación frontal que da peso al golpe.
        ImpulsarHaciaAdelante(heroe.attackImpulse);

        Vector3 centro = (attackPoint != null)
            ? attackPoint.position
            : transform.position + transform.forward * 1.2f;

        // 2. VFX de impacto garantizado en el AttackPoint, orientado a transform.forward.
        GameObject prefabImpacto = (heroe.element == HeroCombatData.ElementType.Piedra)
            ? vfxImpactoPiedra
            : vfxImpactoMadera;
        InstanciarImpacto(prefabImpacto, centro, transform.forward);

        Collider[] hits = Physics.OverlapSphere(centro, heroe.attackRange, capasAfectadas);
        raicesProcesadas.Clear();

        int impactos = 0;
        bool golpeoAlgoGolpeable = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;
            if (EsParteDelHeroe(col.transform)) continue;

            Transform raiz = col.transform.root;
            if (!raicesProcesadas.Add(raiz.GetInstanceID())) continue;

            Vector3 punto = col.ClosestPoint(centro);
            if (punto == centro) punto = col.bounds.ClosestPoint(centro) == Vector3.zero ? col.transform.position : col.bounds.ClosestPoint(centro);

            Vector3 normal = punto - centro;
            if (normal.sqrMagnitude < 0.0001f) normal = col.transform.position - transform.position;
            normal.y = Mathf.Max(normal.y, 0f);
            normal.Normalize();
            if (normal.sqrMagnitude < 0.0001f) normal = transform.forward;

            // 1. Daño a entidades con vida
            IDamageable victima = col.GetComponentInParent<IDamageable>();
            if (victima != null)
            {
                Vector3 direccionRetroceso = (col.transform.position - transform.position).normalized + Vector3.up * 0.25f;
                victima.TakeDamage(heroe.attackDamage, punto, direccionRetroceso * heroe.knockbackStrength);
                impactos++;
                golpeoAlgoGolpeable = true;
            }

            // 2. Elementos de puzzle golpeables (tótems, corazones, engranajes)
            IGolpeableElemental golpeable = col.GetComponentInParent<IGolpeableElemental>();
            if (golpeable != null)
            {
                golpeable.RecibirGolpeElemental(heroe.element, punto);
                golpeoAlgoGolpeable = true;
            }

            // 3. Chispa de contacto sobre la superficie golpeada (feedback adicional)
            if (victima != null || golpeable != null)
            {
                InstanciarImpacto(prefabImpacto, punto, normal);
            }
        }

        if (golpeoAlgoGolpeable)
        {
            HitstopManager.Instance?.TriggerHitstop(0.04f);
            CameraFollow.Instance?.TriggerScreenShake(0.15f, 0.25f);
            Debug.Log($"[COMBATE] {heroe.heroName} impactó {impactos} entidad(es) por {heroe.attackDamage} de daño.");
        }
        else
        {
            Debug.Log($"[COMBATE] {heroe.heroName} atacó al aire.");
        }
    }

    // ------------------------------------------------------------------
    // HABILIDAD ELEMENTAL
    // ------------------------------------------------------------------

    /// <summary>Habilidad primaria: Muro de Basalto (Caelen) o Raíces Voraces (Illya).</summary>
    public void EjecutarHabilidadElemental()
    {
        HeroCombatData heroe = tagTeam.ActiveHero;
        if (heroe == null) return;

        if (heroe.skillCooldownTimer > 0f)
        {
            Debug.Log($"[COMBATE] Habilidad en recarga: {heroe.skillCooldownTimer:F1}s restantes.");
            return;
        }

        heroe.skillCooldownTimer = heroe.skillCooldown;

        if (heroe.element == HeroCombatData.ElementType.Piedra) InvocarMuroBasalto();
        else InvocarRaicesVoraces(heroe);
    }

    private void InvocarMuroBasalto()
    {
        CameraFollow.Instance?.TriggerScreenShake(0.2f, 0.35f);

        if (prefabMuroBasalto == null)
        {
            Debug.LogWarning("[COMBATE] No hay prefab de Muro de Basalto asignado.");
            return;
        }

        Vector3 posicion = transform.position + transform.forward * distanciaMuro;
        Instantiate(prefabMuroBasalto, posicion, transform.rotation);
        Debug.Log("[COMBATE] Caelen invocó el Muro de Basalto.");
    }

    private void InvocarRaicesVoraces(HeroCombatData heroe)
    {
        CameraFollow.Instance?.TriggerScreenShake(0.18f, 0.2f);

        if (prefabRaicesAoE != null)
        {
            GameObject raices = Instantiate(prefabRaicesAoE, transform.position, Quaternion.identity);
            Destroy(raices, 3f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, radioRaices, capasAfectadas);
        int alcanzados = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || EsParteDelHeroe(hits[i].transform)) continue;

            IDamageable victima = hits[i].GetComponentInParent<IDamageable>();
            if (victima == null) continue;

            // Anclaje de zarzas: inmoviliza al objetivo alcanzado.
            EnemyAI ia = hits[i].GetComponentInParent<EnemyAI>();
            if (ia != null) ia.AplicarInmovilizacion(duracionZarzas);

            Vector3 direccion = (hits[i].transform.position - transform.position).normalized + Vector3.up * 0.2f;
            victima.TakeDamage(danoRaices, hits[i].transform.position, direccion * heroe.knockbackStrength * 0.6f);
            alcanzados++;
        }

        Debug.Log($"[COMBATE] Illya invocó Raíces Voraces sobre {alcanzados} objetivo(s) con anclaje de zarzas {duracionZarzas:F1}s.");
    }

    private bool EsParteDelHeroe(Transform otro)
    {
        if (otro == null) return true;
        Transform raizHeroe = transform.root;
        return otro.root == raizHeroe;
    }

    /// <summary>
    /// Desplaza al héroe unos centímetros hacia delante con CharacterController.Move,
    /// de modo que el impulso respeta paredes y desniveles en lugar de teletransportarlo.
    /// </summary>
    private void ImpulsarHaciaAdelante(float distancia)
    {
        if (distancia <= 0f) return;
        if (controlador == null || !controlador.enabled) return;

        controlador.Move(transform.forward * distancia);
    }

    /// <summary>Instancia un VFX de impacto autodestruible y orientado.</summary>
    private void InstanciarImpacto(GameObject prefab, Vector3 posicion, Vector3 direccion)
    {
        if (prefab == null) return;

        Quaternion rotacion = (direccion.sqrMagnitude > 0.0001f)
            ? Quaternion.LookRotation(direccion.normalized)
            : transform.rotation;

        GameObject instancia = Instantiate(prefab, posicion, rotacion);
        Destroy(instancia, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (tagTeam == null || tagTeam.ActiveHero == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Vector3 centro = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1.2f;
        Gizmos.DrawWireSphere(centro, tagTeam.ActiveHero.attackRange);
    }
}
