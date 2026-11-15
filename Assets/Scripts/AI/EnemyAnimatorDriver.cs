using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sincroniza el Animator de un enemigo con el estado real de EnemyAI y NavMeshAgent.
/// Se apoya en la API pública ya existente (EstadoActual, RetrocesoActivo y los
/// eventos de HealthComponent): no modifica ni envuelve la IA, y no busca nada en
/// runtime. Los controladores AC_Scorpion y AC_Quimera exponen exactamente los
/// cuatro parámetros que se escriben aquí (Speed, Attack, TakeDamage, Die).
/// </summary>
[DisallowMultipleComponent]
public class EnemyAnimatorDriver : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [SerializeField] private Animator animador;
    [SerializeField] private NavMeshAgent agente;
    [SerializeField] private EnemyAI ia;
    [SerializeField] private HealthComponent salud;

    [Header("Umbrales")]
    [Tooltip("Velocidad del agente que corresponde a la animación de carrera al 100 %.")]
    [SerializeField] private float velocidadMaxima = 4f;

    private static readonly int IdSpeed = Animator.StringToHash("Speed");
    private static readonly int IdAttack = Animator.StringToHash("Attack");
    private static readonly int IdTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int IdDie = Animator.StringToHash("Die");

    private EnemyAI.Estado estadoAnterior = EnemyAI.Estado.Inactivo;
    private bool retrocesoAnterior;
    private bool suscrito;

    private void Awake()
    {
        Resolver();
    }

    private void OnEnable()
    {
        Resolver();
        Suscribir();
        ReiniciarAnimador();
    }

    private void OnDisable()
    {
        Desuscribir();
    }

    private void Update()
    {
        if (animador == null) return;

        // 1. Locomoción: magnitud de la velocidad real del agente.
        if (agente != null)
        {
            float rapidez = agente.velocity.magnitude;
            float normalizada = (velocidadMaxima > 0.01f) ? Mathf.Clamp01(rapidez / velocidadMaxima) : 0f;
            animador.SetFloat(IdSpeed, normalizada);
        }

        if (ia == null) return;

        // 2. Ataque: flanco de entrada al estado de ataque (no cada frame).
        EnemyAI.Estado actual = ia.EstadoActual;

        if (actual == EnemyAI.Estado.Ataque && estadoAnterior != EnemyAI.Estado.Ataque)
            animador.SetTrigger(IdAttack);

        // 3. Impacto: flanco de subida del retroceso, que HealthComponent activa al recibir daño.
        bool retroceso = ia.RetrocesoActivo;
        if (retroceso && !retrocesoAnterior) animador.SetTrigger(IdTakeDamage);

        estadoAnterior = actual;
        retrocesoAnterior = retroceso;
    }

    private void ReiniciarAnimador()
    {
        estadoAnterior = (ia != null) ? ia.EstadoActual : EnemyAI.Estado.Inactivo;
        retrocesoAnterior = false;

        if (animador == null) return;

        animador.applyRootMotion = false;
        animador.Rebind();
        animador.SetFloat(IdSpeed, 0f);
    }

    private void Resolver()
    {
        if (animador == null) animador = GetComponentInChildren<Animator>(true);
        if (agente == null) agente = GetComponent<NavMeshAgent>();
        if (ia == null) ia = GetComponent<EnemyAI>();
        if (salud == null) salud = GetComponent<HealthComponent>();
    }

    private void Suscribir()
    {
        if (suscrito || salud == null) return;
        salud.OnDied += AlMorir;
        suscrito = true;
    }

    private void Desuscribir()
    {
        if (!suscrito || salud == null) return;
        salud.OnDied -= AlMorir;
        suscrito = false;
    }

    private void AlMorir()
    {
        if (animador != null) animador.SetTrigger(IdDie);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(Animator nuevoAnimador, NavMeshAgent nuevoAgente, EnemyAI nuevaIa,
                           HealthComponent nuevaSalud, float velocidad)
    {
        animador = nuevoAnimador;
        agente = nuevoAgente;
        ia = nuevaIa;
        salud = nuevaSalud;
        velocidadMaxima = Mathf.Max(0.01f, velocidad);

        Desuscribir();
        Suscribir();
    }
}
