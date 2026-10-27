using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de vida modular y desacoplado. Lo usan héroes, enemigos y minijefes.
/// Expone eventos C# (patrón observador) para que HUD y barras flotantes se
/// actualicen SOLO cuando cambia la vida, sin comprobaciones en Update().
/// </summary>
[DisallowMultipleComponent]
public class HealthComponent : MonoBehaviour, IDamageable
{
    // ---- Eventos observadores ----
    /// <summary>(vidaActual, vidaMaxima)</summary>
    public event Action<float, float> OnHealthChanged;
    /// <summary>(cantidadDeDano, puntoDeImpacto)</summary>
    public event Action<float, Vector3> OnDamaged;
    public event Action OnDied;

    [Header("Vitalidad")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Muerte")]
    [SerializeField] private bool desactivarAlMorir = true;
    [SerializeField] private float retardoDesactivacion = 1.4f;

    [Header("Eventos de Inspector")]
    [SerializeField] private UnityEvent alRecibirDano = new UnityEvent();
    [SerializeField] private UnityEvent alMorir = new UnityEvent();

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float Normalizada => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public bool EstaMuerto => currentHealth <= 0f;

    private DamageFlash damageFlash;
    private EnemyAI enemyAI;
    private Rigidbody cuerpoRigido;
    private bool danoAplicadoAlMenosUnaVez;

    private void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
        if (damageFlash == null) damageFlash = GetComponentInChildren<DamageFlash>();
        enemyAI = GetComponent<EnemyAI>();
        cuerpoRigido = GetComponent<Rigidbody>();

        // El GameObject del héroe NUNCA se desactiva al caer: la derrota la
        // arbitra el Tag-Team y el gestor de reaparición ('R').
        if (GetComponent<TagTeamManager>() != null) desactivarAlMorir = false;
    }

    private void Start()
    {
        // Primer aviso para que las barras se pinten con el valor real de arranque.
        NotificarCambio();
    }

    /// <summary>Configura la vitalidad (usado por el Tag-Team al cambiar de héroe).</summary>
    public void Initialize(float maximo, float actual)
    {
        maxHealth = Mathf.Max(1f, maximo);
        currentHealth = Mathf.Clamp(actual, 0f, maxHealth);
        NotificarCambio();
    }

    /// <summary>Fija la vida actual sin aplicar reducciones (curación directa).</summary>
    public void SetHealth(float valor)
    {
        currentHealth = Mathf.Clamp(valor, 0f, maxHealth);
        NotificarCambio();
    }

    /// <summary>Suma vida respetando el máximo.</summary>
    public void Heal(float cantidad)
    {
        if (cantidad <= 0f || EstaMuerto) return;
        SetHealth(currentHealth + cantidad);
    }

    /// <summary>Aplica daño completo: vida, feedback visual, retroceso y muerte.</summary>
    public void ApplyDamage(float cantidad, Vector3 fuerzaRetroceso, Vector3 puntoImpacto)
    {
        if (cantidad <= 0f || EstaMuerto) return;

        danoAplicadoAlMenosUnaVez = true;
        currentHealth = Mathf.Max(0f, currentHealth - cantidad);
        NotificarCambio();

        // 1. Destello blanco de 0.18 s
        if (damageFlash != null) damageFlash.Destellar();

        // 2. Retroceso: si hay IA con NavMesh, ella arbitra el conflicto PhysX/NavMesh
        if (enemyAI != null)
        {
            enemyAI.AplicarRetroceso(fuerzaRetroceso);
        }
        else if (cuerpoRigido != null && !cuerpoRigido.isKinematic)
        {
            cuerpoRigido.AddForce(fuerzaRetroceso, ForceMode.Impulse);
        }

        // 3. Eventos
        OnDamaged?.Invoke(cantidad, puntoImpacto);
        alRecibirDano?.Invoke();

        if (currentHealth <= 0f) Morir();
    }

    /// <summary>Implementación de IDamageable para que el combate quede agnóstico del tipo de entidad.</summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce)
    {
        ApplyDamage(amount, knockbackForce, hitPoint);
    }

    /// <summary>
    /// Variante simplificada de la API: aplica daño sin retroceso. La usan los
    /// enemigos al golpear al héroe, que se desplaza con CharacterController y
    /// no admite impulsos de PhysX.
    /// </summary>
    public void TakeDamage(float amount)
    {
        ApplyDamage(amount, Vector3.zero, transform.position);
    }

    /// <summary>Restaura la vida al máximo (reinicio de arena / reaparición).</summary>
    public void RestaurarVidaCompleta()
    {
        currentHealth = maxHealth;
        NotificarCambio();
    }

    private void Morir()
    {
        OnDied?.Invoke();
        alMorir?.Invoke();

        if (enemyAI != null) enemyAI.NotificarMuerte();

        if (desactivarAlMorir) StartCoroutine(RutinaDesactivar());
    }

    private IEnumerator RutinaDesactivar()
    {
        yield return new WaitForSeconds(retardoDesactivacion);
        gameObject.SetActive(false);
    }

    private void NotificarCambio()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>True si la entidad ya recibió daño alguna vez (útil para depuración y pruebas).</summary>
    public bool HaRecibidoDano => danoAplicadoAlMenosUnaVez;
}
