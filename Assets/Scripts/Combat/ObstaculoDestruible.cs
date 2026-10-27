using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bloqueador de escenario que cede ante los ataques básicos o la habilidad
/// especial (por ejemplo el tronco caído del Sendero del Nivel 1).
/// Implementa IDamageable para el daño real e IGolpeableElemental para el
/// feedback elemental, sin duplicar la pérdida de integridad por golpe.
/// </summary>
[DisallowMultipleComponent]
public class ObstaculoDestruible : MonoBehaviour, IGolpeableElemental, IDamageable
{
    [Header("Integridad")]
    [SerializeField] private float integridadMaxima = 60f;
    [SerializeField] private float integridadActual = 60f;

    [Header("Piezas que ceden al destruirse")]
    [Tooltip("Si se deja vacío se usa este mismo GameObject.")]
    [SerializeField] private GameObject[] visuales;
    [SerializeField] private Collider[] colisiones;

    [Header("Feedback")]
    [SerializeField] private float intensidadSacudida = 0.08f;
    [SerializeField] private float duracionSacudida = 0.12f;

    [Header("Eventos de Inspector")]
    [SerializeField] private UnityEvent alRecibirGolpe = new UnityEvent();
    [SerializeField] private UnityEvent alDestruirse = new UnityEvent();

    public bool Destruido => integridadActual <= 0f;
    public float IntegridadNormalizada => integridadMaxima > 0f ? Mathf.Clamp01(integridadActual / integridadMaxima) : 0f;

    private Vector3 posicionOriginal;
    private float temporizadorSacudida;

    private void Awake()
    {
        posicionOriginal = transform.localPosition;

        if (integridadActual <= 0f) integridadActual = integridadMaxima;
        if (visuales == null || visuales.Length == 0) visuales = new[] { gameObject };
        if (colisiones == null || colisiones.Length == 0) colisiones = GetComponentsInChildren<Collider>(true);
    }

    /// <summary>Camino de daño real (ataque básico y habilidades).</summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce)
    {
        AplicarIntegridad(amount);
    }

    /// <summary>
    /// Camino elemental: solo feedback, para no contabilizar dos veces el mismo
    /// golpe cuando PlayerCombat notifica IDamageable e IGolpeableElemental.
    /// </summary>
    public void RecibirGolpeElemental(HeroCombatData.ElementType elemento, Vector3 puntoImpacto)
    {
        temporizadorSacudida = duracionSacudida;
        alRecibirGolpe?.Invoke();
    }

    private void AplicarIntegridad(float cantidad)
    {
        if (cantidad <= 0f || Destruido) return;

        integridadActual = Mathf.Max(0f, integridadActual - cantidad);
        temporizadorSacudida = duracionSacudida;
        alRecibirGolpe?.Invoke();

        Debug.Log($"[ESCENARIO] {name} golpeado: integridad {integridadActual:F0}/{integridadMaxima:F0}.");

        if (integridadActual <= 0f) Ceder();
    }

    private void Update()
    {
        if (temporizadorSacudida <= 0f) return;

        temporizadorSacudida -= Time.deltaTime;
        float factor = Mathf.Max(0f, temporizadorSacudida / Mathf.Max(0.0001f, duracionSacudida));
        transform.localPosition = posicionOriginal + Random.insideUnitSphere * (intensidadSacudida * factor);

        if (temporizadorSacudida <= 0f) transform.localPosition = posicionOriginal;
    }

    /// <summary>Libera el camino: retira colisión y oculta la malla.</summary>
    private void Ceder()
    {
        if (!activo) return;
        activo = false;

        for (int i = 0; i < colisiones.Length; i++)
        {
            if (colisiones[i] != null) colisiones[i].enabled = false;
        }

        for (int i = 0; i < visuales.Length; i++)
        {
            if (visuales[i] != null) visuales[i].SetActive(false);
        }

        alDestruirse?.Invoke();
        Debug.Log($"[ESCENARIO] {name} ha cedido: el camino queda libre.");
    }

    /// <summary>
    /// Disuelve el bloqueo de inmediato (recompensa de un puzzle, sin necesidad
    /// de golpearlo). Es la vía que usan las placas de presión y los sellos.
    /// </summary>
    public void Disolver()
    {
        if (!activo) return;

        integridadActual = 0f;
        Ceder();
    }

    /// <summary>Devuelve el bloqueador a su estado inicial (reinicio de nivel).</summary>
    public void ReiniciarObstaculo()
    {
        integridadActual = integridadMaxima;
        transform.localPosition = posicionOriginal;

        for (int i = 0; i < colisiones.Length; i++)
        {
            if (colisiones[i] != null) colisiones[i].enabled = true;
        }

        activo = true;
        for (int i = 0; i < visuales.Length; i++)
        {
            if (visuales[i] != null) visuales[i].SetActive(true);
        }
    }

    private bool activo = true;
    public bool Activo => activo;
}