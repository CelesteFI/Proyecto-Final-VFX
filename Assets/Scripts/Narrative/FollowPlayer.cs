using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Seguimiento de Illya a Caelen tras el rescate (Maqueta Nivel 1).
/// Mide cada frame la distancia en planta al héroe: si supera
/// <see cref="distanciaSeguimiento"/> camina hacia él y activa el bool «Seguir» del
/// Animator; al quedar dentro del umbral se detiene y lo desactiva.
/// Usa el NavMeshAgent si existe y, si no, avanza con <see cref="Vector3.MoveTowards"/>
/// sobre el NavMesh. No instancia ni destruye ningún objeto y no toca el control del jugador.
/// </summary>
[DisallowMultipleComponent]
public class FollowPlayer : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Transform del héroe al que sigue. Lo asigna la secuencia de rescate.")]
    [SerializeField] private Transform objetivo;
    [Tooltip("Distancia a partir de la cual Illya camina hacia el héroe.")]
    [SerializeField] private float distanciaSeguimiento = 2f;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 3.5f;
    [SerializeField] private float velocidadGiro = 10f;
    [Tooltip("Ajusta el paso al NavMesh para no salirse del suelo navegable.")]
    [SerializeField] private bool ajustarAlNavMesh = true;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    [SerializeField] private string parametroSeguir = "Seguir";

    [Header("Agente (opcional)")]
    [Tooltip("Si Illya tuviera NavMeshAgent se usa su ruta; si no, se mueve por transform.")]
    [SerializeField] private NavMeshAgent agente;

    /// <summary>Héroe seguido. Lo fija la secuencia de rescate.</summary>
    public Transform Objetivo
    {
        get => objetivo;
        set => objetivo = value;
    }

    /// <summary>True mientras Illya camina detrás del héroe.</summary>
    public bool Siguiendo { get; private set; }

    /// <summary>Distancia en planta al héroe en el último frame.</summary>
    public float DistanciaActual { get; private set; }

    private void Awake()
    {
        if (agente == null) agente = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (objetivo == null) return;

        Vector3 delta = objetivo.position - transform.position;
        delta.y = 0f;
        DistanciaActual = delta.magnitude;

        bool debeSeguir = DistanciaActual > distanciaSeguimiento;
        if (debeSeguir != Siguiendo) AplicarAnimacion(debeSeguir);

        if (!debeSeguir)
        {
            Detener();
            return;
        }

        Vector3 direccion = delta / Mathf.Max(DistanciaActual, 0.0001f);
        Avanzar(direccion);
        Girar(direccion);
    }

    private void OnDisable()
    {
        Siguiendo = false;
        Detener();
        if (animator != null) animator.SetBool(parametroSeguir, false);
    }

    // ------------------------------------------------------------------ movimiento

    private void Avanzar(Vector3 direccion)
    {
        if (agente != null && agente.enabled && agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.speed = velocidad;
            agente.SetDestination(objetivo.position);
            return;
        }

        Vector3 paso = transform.position + direccion * (velocidad * Time.deltaTime);
        paso.y = objetivo.position.y;

        if (ajustarAlNavMesh &&
            NavMesh.SamplePosition(paso, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            paso = hit.position;
        }

        transform.position = paso;
    }

    private void Girar(Vector3 direccion)
    {
        Quaternion destino = Quaternion.LookRotation(direccion, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, destino, velocidadGiro * Time.deltaTime);
    }

    private void Detener()
    {
        if (agente != null && agente.enabled && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.ResetPath();
        }
    }

    private void AplicarAnimacion(bool seguir)
    {
        Siguiendo = seguir;
        if (animator != null) animator.SetBool(parametroSeguir, seguir);
    }

    /// <summary>Configuración desde la secuencia de rescate o desde el integrador.</summary>
    public void Configurar(Transform heroe, Animator animadorIllya, float distancia = 2f)
    {
        objetivo = heroe;
        animator = animadorIllya;
        distanciaSeguimiento = Mathf.Max(0.1f, distancia);
    }
}
