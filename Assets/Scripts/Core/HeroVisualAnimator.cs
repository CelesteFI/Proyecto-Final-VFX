using UnityEngine;

/// <summary>
/// Sincroniza el Animator del modelo definitivo del héroe con el estado real de
/// locomoción y combate. Vive en el enchufe visual ('Player_Caelen' / 'Player_Illya'),
/// no en la raíz 'Player': al conmutar el Tag-Team los enchufes se activan y
/// desactivan, y con ellos este componente (que se reinicia en OnEnable, de modo
/// que un héroe que había muerto vuelve a arrancar en Idle al reaparecer).
/// No busca nada en runtime: todas las referencias llegan del Inspector o del
/// propio linaje del Player con GetComponentInParent.
/// </summary>
[DisallowMultipleComponent]
public class HeroVisualAnimator : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("Animator del modelo FBX definitivo. Si queda vacío se busca en los hijos.")]
    [SerializeField] private Animator animador;
    [SerializeField] private CharacterController controlador;
    [Tooltip("Fuente de la velocidad real del héroe. CharacterController.velocity solo refleja el último Move() del frame (el de gravedad).")]
    [SerializeField] private PlayerController locomocion;
    [SerializeField] private PlayerCombat combate;
    [SerializeField] private HealthComponent salud;

    [Header("Umbrales de Animación")]
    [Tooltip("Velocidad de referencia del héroe (Caelen 6.5 · Illya 7.6).")]
    [SerializeField] private float velocidadMaxima = 6.5f;
    [Tooltip("Velocidad vertical por encima de la cual se considera salto.")]
    [SerializeField] private float umbralSalto = 1.5f;
    [Tooltip("Velocidad vertical por debajo de la cual se considera caída.")]
    [SerializeField] private float umbralCaida = -1.5f;

    private static readonly int IdSpeed = Animator.StringToHash("Speed");
    private static readonly int IdIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IdIsFalling = Animator.StringToHash("IsFalling");
    private static readonly int IdJump = Animator.StringToHash("Jump");
    private static readonly int IdAttackNormal = Animator.StringToHash("AttackNormal");
    private static readonly int IdAttackSpecial = Animator.StringToHash("AttackSpecial");
    private static readonly int IdDie = Animator.StringToHash("Die");

    private bool estabaEnSuelo = true;
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

        // La velocidad se lee de PlayerController: CharacterController.velocity refleja
        // únicamente el último Move() del frame, que en este proyecto es el de gravedad,
        // así que su componente horizontal vale siempre 0 aunque el héroe corra.
        float horizontal;
        float vertical;
        bool enSuelo;

        if (locomocion != null)
        {
            horizontal = locomocion.VelocidadHorizontal;
            vertical = locomocion.VelocidadVertical;
            enSuelo = locomocion.EnSuelo;
        }
        else if (controlador != null)
        {
            Vector3 velocidad = controlador.velocity;
            horizontal = new Vector3(velocidad.x, 0f, velocidad.z).magnitude;
            vertical = velocidad.y;
            enSuelo = controlador.isGrounded;
        }
        else
        {
            return;
        }

        float normalizada = (velocidadMaxima > 0.01f) ? Mathf.Clamp01(horizontal / velocidadMaxima) : 0f;
        bool cayendo = !enSuelo && vertical < umbralCaida;

        animador.SetFloat(IdSpeed, normalizada);
        animador.SetBool(IdIsGrounded, enSuelo);
        animador.SetBool(IdIsFalling, cayendo);

        // El salto es un disparo único: solo en el tránsito suelo -> aire ascendente.
        if (estabaEnSuelo && !enSuelo && vertical > umbralSalto)
            animador.SetTrigger(IdJump);

        estabaEnSuelo = enSuelo;
    }

    /// <summary>Deja el Animator en su estado por defecto con los parámetros limpios.</summary>
    private void ReiniciarAnimador()
    {
        estabaEnSuelo = true;

        if (animador == null) return;

        // El desplazamiento lo arbitra PlayerController sobre el CharacterController:
        // el root motion del clip duplicaría el movimiento.
        animador.applyRootMotion = false;
        animador.Rebind();

        animador.SetFloat(IdSpeed, 0f);
        animador.SetBool(IdIsGrounded, true);
        animador.SetBool(IdIsFalling, false);
    }

    private void Resolver()
    {
        if (animador == null) animador = GetComponentInChildren<Animator>(true);
        if (controlador == null) controlador = GetComponentInParent<CharacterController>();
        if (locomocion == null) locomocion = GetComponentInParent<PlayerController>();
        if (combate == null) combate = GetComponentInParent<PlayerCombat>();
        if (salud == null) salud = GetComponentInParent<HealthComponent>();
    }

    private void Suscribir()
    {
        if (suscrito) return;

        if (combate != null)
        {
            combate.OnAtaqueBasico += AlAtacarBasico;
            combate.OnHabilidad += AlUsarHabilidad;
        }

        if (salud != null) salud.OnDied += AlMorir;

        suscrito = true;
    }

    private void Desuscribir()
    {
        if (!suscrito) return;

        if (combate != null)
        {
            combate.OnAtaqueBasico -= AlAtacarBasico;
            combate.OnHabilidad -= AlUsarHabilidad;
        }

        if (salud != null) salud.OnDied -= AlMorir;

        suscrito = false;
    }

    private void AlAtacarBasico()
    {
        if (animador != null) animador.SetTrigger(IdAttackNormal);
    }

    private void AlUsarHabilidad()
    {
        if (animador != null) animador.SetTrigger(IdAttackSpecial);
    }

    private void AlMorir()
    {
        if (animador != null) animador.SetTrigger(IdDie);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(Animator nuevoAnimador, CharacterController nuevoControlador,
                           PlayerController nuevaLocomocion, PlayerCombat nuevoCombate,
                           HealthComponent nuevaSalud, float velocidad)
    {
        animador = nuevoAnimador;
        controlador = nuevoControlador;
        locomocion = nuevaLocomocion;
        combate = nuevoCombate;
        salud = nuevaSalud;
        velocidadMaxima = Mathf.Max(0.01f, velocidad);

        Desuscribir();
        Suscribir();
    }
}
