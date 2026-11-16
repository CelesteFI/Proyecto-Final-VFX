using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor del sistema Tag-Team: conserva los datos de Caelen e Illya (perfiles, modelos
/// y Animators de cada héroe), el índice del héroe activo, el relevo ('Tab') y los VFX
/// de relevo, y mantiene vidas independientes delegando la vitalidad en un único
/// HealthComponent que se re-inicializa al cambiar de héroe.
/// TODO el combate (puntos de ataque, prefabs de Q y especiales, proyectil de Illya,
/// cooldowns) vive en PlayerCombat (Assets/Scripts/Player/Combat).
///
/// Absorbe el antiguo 'GestorDerrotaHeroe': la secuencia de derrota (animación de
/// muerte, pausa estricta, apertura del Modal_Derrota, congelación del tiempo y
/// reinicio total del nivel) vive ahora aquí, porque es este gestor el que detecta
/// qué héroe ha caído y si queda relevo posible.
/// </summary>
[DisallowMultipleComponent]
public class TagTeamManager : MonoBehaviour
{
    public static TagTeamManager Instance { get; private set; }

    [Header("Perfiles de Personaje (GDD)")]
    public HeroCombatData caelen = new HeroCombatData
    {
        heroName = "Caelen",
        element = HeroCombatData.ElementType.Piedra,
        maxHealth = 130f,
        currentHealth = 130f,
        moveSpeed = 6.5f,
        attackDamage = 30f,
        attackRange = 2.2f,
        attackCooldown = 0.55f,
        knockbackStrength = 9f,
        attackImpulse = 0.40f
    };

    public HeroCombatData illya = new HeroCombatData
    {
        heroName = "Illya",
        element = HeroCombatData.ElementType.Madera,
        maxHealth = 95f,
        currentHealth = 95f,
        moveSpeed = 7.6f,
        attackDamage = 18f,
        attackRange = 3.5f,
        attackCooldown = 0.30f, // cadencia 1.5x mayor que Caelen
        knockbackStrength = 5.5f,
        attackImpulse = 0.55f
    };

    [Header("Estado")]
    [SerializeField] private int activeHeroIndex = 0; // 0 = Caelen, 1 = Illya
    [SerializeField] private bool illyaDesbloqueada = true;

    [Tooltip("Héroe con el que arranca la escena. Falso = Player_Caelen (Nivel 1). " +
             "Verdadero = Player_Illya (Nivel 2). Si Illya sigue sellada, manda Caelen.")]
    public bool iniciarConIllya = false;

    [Header("VFX de Relevo (GameObjects hijos del Player, ya en escena)")]
    [Tooltip("Hijo del Player con el aura al relevar HACIA Caelen (amarillo-café, tierra). Arranca apagado.")]
    [SerializeField] private GameObject vfxRelevoCaelenEscena;
    [Tooltip("Hijo del Player con el aura al relevar HACIA Illya (verde, madera). Arranca apagado.")]
    [SerializeField] private GameObject vfxRelevoIllyaEscena;
    [Tooltip("Segundos que permanece encendido el VFX antes de Stop() + SetActive(false).")]
    [SerializeField] private float duracionVfxRelevo = 0.8f;

    [Header("Enchufes Visuales (Reemplazo Manual de Modelo)")]
    [Tooltip("Contenedor 'Player_Caelen' dentro de Visual_Model: arrastra aquí el FBX final.")]
    [SerializeField] private GameObject socketCaelen;

    [Tooltip("Contenedor 'Player_Illya' dentro de Visual_Model: arrastra aquí el FBX final.")]
    [SerializeField] private GameObject socketIllya;

    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("PlayerInputReader del mismo Player. No se busca en runtime.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Derrota y Reinicio")]
    [Tooltip("Se escribe solo cuando la derrota es exclusiva de Caelen (Illya sigue sellada).")]
    [SerializeField] private string mensajeCaelenSolo = "Caelen ha caído. El bosque reclama su juramento.";
    [Tooltip("Se escribe cuando ambos héroes han caído.")]
    [SerializeField] private string mensajeAmbosCaidos = "Caelen e Illya han caído. El bosque reclama sus juramentos.";
    [Tooltip("Segundos de pausa estricta entre la animación de muerte y la aparición del modal.")]
    [SerializeField] private float pausaAntesDelModal = 2.5f;

    [Header("Resurrección (estado Idle por héroe)")]
    [Tooltip("Estado de reposo del AC_Heroe_Caelen. El Animator no tiene un estado llamado 'Idle'.")]
    [SerializeField] private string estadoIdleCaelen = "Caelen_Idle";
    [Tooltip("Estado de reposo del AC_Heroe_Illya. El Animator no tiene un estado llamado 'Idle'.")]
    [SerializeField] private string estadoIdleIllya = "Illya_Idle";

    /// <summary>HUD del nivel. Se resuelve en Awake desde PlayerInputReader.canvasHUD.</summary>
    [NonSerialized] public Canvas_HUD canvasHUD;

    public int ActiveHeroIndex => activeHeroIndex;
    public bool IllyaDesbloqueada => illyaDesbloqueada;
    public HeroCombatData ActiveHero => (activeHeroIndex == 0) ? caelen : illya;
    public HeroCombatData HeroeInactivo => (activeHeroIndex == 0) ? illya : caelen;

    /// <summary>True mientras la pantalla de derrota está activa.</summary>
    public bool DerrotaActiva => derrotaActiva;

    /// <summary>Vitalidad del héroe activo (eventos observadores para el HUD).</summary>
    public HealthComponent SaludHeroe => saludHeroe;

    /// <summary>
    /// Animator del modelo del héroe ACTIVO. Lo usa la secuencia de derrota para disparar
    /// 'Muerte' sobre el héroe que ha caído y no sobre el que está en reserva.
    /// </summary>
    public Animator AnimadorHeroeActivo
    {
        get
        {
            GameObject enchufe = (activeHeroIndex == 0) ? socketCaelen : socketIllya;
            return AnimadorConControlador(enchufe);
        }
    }

    /// <summary>
    /// Animator del enchufe que realmente tiene un AnimatorController (el del modelo FBX).
    /// El propio enchufe 'Player_Illya' lleva un Animator vacío que no debe usarse.
    /// </summary>
    private static Animator AnimadorConControlador(GameObject enchufe)
    {
        if (enchufe == null) return null;

        Animator[] animadores = enchufe.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animadores.Length; i++)
        {
            if (animadores[i].runtimeAnimatorController != null) return animadores[i];
        }

        return (animadores.Length > 0) ? animadores[0] : null;
    }

    /// <summary>Se dispara al completar un relevo.</summary>
    public event Action<HeroCombatData> OnHeroeCambiado;

    /// <summary>Se dispara cuando cambia la vida del héroe activo.</summary>
    public event Action<float, float> OnVidaHeroeCambiada;

    /// <summary>Se dispara cuando Caelen e Illya han caído: derrota total.</summary>
    public event Action OnAmbosCaidos;

    /// <summary>
    /// Se dispara cuando cae Caelen y 'Player_Illya' todavía no ha sido rescatada:
    /// no hay relevo posible, así que la partida termina ahí.
    /// </summary>
    public event Action OnDerrotaSinIllya;

    private HealthComponent saludHeroe;

    // 0 = Caelen, 1 = Illya. Marca qué héroes han caído en la partida actual.
    private readonly bool[] caido = new bool[2];

    private static readonly int IdMuerte = Animator.StringToHash("Muerte");
    private static readonly int IdDie = Animator.StringToHash("Die");

    private bool derrotaActiva;
    private bool rutinaDerrotaEnCurso;

    // Sistemas de partículas de los VFX de relevo (cacheados en Awake: cero búsquedas en runtime).
    private ParticleSystem[] sistemasRelevoCaelen = new ParticleSystem[0];
    private ParticleSystem[] sistemasRelevoIllya = new ParticleSystem[0];
    private readonly Coroutine[] rutinasRelevo = new Coroutine[2];

    /// <summary>True cuando los dos héroes están a 0 de vida.</summary>
    public bool AmbosCaidos => caido[0] && caido[1];

    /// <summary>Consulta si un héroe concreto ha caído.</summary>
    public bool HeroeCaido(int indice) => indice >= 0 && indice < 2 && caido[indice];

    private void Awake()
    {
        Instance = this;

        if (entrada == null) entrada = GetComponent<PlayerInputReader>();
        if (entrada != null) canvasHUD = entrada.canvasHUD;

        saludHeroe = GetComponent<HealthComponent>();
        if (saludHeroe == null) saludHeroe = gameObject.AddComponent<HealthComponent>();

        saludHeroe.OnHealthChanged += AlCambiarVidaHeroe;
        saludHeroe.OnDied += AlMorirHeroeActivo;

        PrepararVfxRelevo(vfxRelevoCaelenEscena, out sistemasRelevoCaelen);
        PrepararVfxRelevo(vfxRelevoIllyaEscena, out sistemasRelevoIllya);
    }

    private static void PrepararVfxRelevo(GameObject vfx, out ParticleSystem[] sistemas)
    {
        if (vfx == null)
        {
            sistemas = new ParticleSystem[0];
            return;
        }

        sistemas = vfx.GetComponentsInChildren<ParticleSystem>(true);
        vfx.SetActive(false);
    }

    private void OnDestroy()
    {
        if (saludHeroe != null)
        {
            saludHeroe.OnHealthChanged -= AlCambiarVidaHeroe;
            saludHeroe.OnDied -= AlMorirHeroeActivo;
        }

        if (entrada != null) entrada.EntradaBloqueada = false;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Héroe inicial por escena. 'iniciarConIllya' manda, pero Illya sellada
        // nunca puede arrancar al frente: en ese caso queda Caelen.
        // El relevo que dejó el altar del Nivel 1 (Illya activa, Caelen reserva)
        // también empuja a Illya al frente en la escena siguiente.
        bool relevoDesdeNivel1 = GameProgressManager.Instance != null &&
                                 GameProgressManager.Instance.illyaActivaEnNivel2;

        activeHeroIndex = ((iniciarConIllya || relevoDesdeNivel1) && illyaDesbloqueada) ? 1 : 0;

        // Blindaje de resurrección: la escena recién cargada (primera vez o tras una derrota)
        // siempre arranca con ambos héroes a vida máxima y sin marcas de caído.
        RestablecerEstadoHeroes();

        // Inicializa la vitalidad del héroe que arranca la partida.
        saludHeroe.Initialize(ActiveHero.maxHealth, ActiveHero.currentHealth);
        ActualizarVisualProxy();

        // Con el enchufe correcto ya activo, los Animators vuelven a su estado de reposo.
        RestablecerAnimadores();

        // La maqueta es la fuente de verdad del estado inicial: el Nivel 1 arranca
        // con Illya sellada y el Nivel 2 con Illya ya libre. Se sincroniza el
        // progreso global para que el HUD y el porcentaje no queden desfasados.
        if (GameProgressManager.Instance != null
            && GameProgressManager.Instance.illyaDesbloqueada != illyaDesbloqueada)
        {
            GameProgressManager.Instance.DesbloquearIllya(illyaDesbloqueada);
        }
    }

    private void Update()
    {
        // 1. Relevo Tag-Team: 'Tab' alterna al héroe que no está activo.
        if (entrada != null && entrada.RelevoPulsado) IntentarRelevo(1 - activeHeroIndex);

        // 2. Los cooldowns de habilidad de ambos héroes los gestiona PlayerCombat.

        // 3. Atajo de reinicio ('R') mientras la pantalla de derrota está en pantalla.
        if (derrotaActiva && entrada != null && entrada.ReinicioPulsado) Reaparecer();
    }

    /// <summary>
    /// Vida al máximo y fuera cualquier marca de muerte. El héroe no tiene un booleano
    /// 'isDead': "muerto" es HealthComponent.EstaMuerto (vida &lt;= 0), así que devolver la
    /// vida al máximo y limpiar el registro de caídos lo revive por completo.
    /// </summary>
    private void RestablecerEstadoHeroes()
    {
        caelen.currentHealth = caelen.maxHealth;
        illya.currentHealth = illya.maxHealth;

        caido[0] = false;
        caido[1] = false;
        derrotaActiva = false;
        rutinaDerrotaEnCurso = false;

        if (entrada != null) entrada.EntradaBloqueada = false;
    }

    /// <summary>
    /// Manda a los Animators de ambos héroes a su estado de reposo y descarta los triggers
    /// de muerte pendientes. Los estados reales se llaman 'Caelen_Idle' / 'Illya_Idle'.
    /// Un enchufe apagado no admite Play(): se reinicia solo al encenderse
    /// (HeroVisualAnimator.OnEnable hace Rebind al estado por defecto, el Idle).
    /// </summary>
    private void RestablecerAnimadores()
    {
        RestablecerAnimador(AnimadorConControlador(socketCaelen), estadoIdleCaelen);
        RestablecerAnimador(AnimadorConControlador(socketIllya), estadoIdleIllya);
    }

    private static void RestablecerAnimador(Animator animador, string estadoIdle)
    {
        if (animador == null || animador.runtimeAnimatorController == null) return;
        if (!animador.isActiveAndEnabled) return;

        animador.ResetTrigger(IdDie);
        animador.ResetTrigger(IdMuerte);
        animador.Play(estadoIdle, 0, 0f);
    }

    /// <summary>Valida y ejecuta el relevo de héroe.</summary>
    public void IntentarRelevo(int nuevoIndice)
    {
        if (nuevoIndice == 1 && !illyaDesbloqueada)
        {
            Debug.Log("[TAG-TEAM] Illya permanece sellada en la celda del Árbol Sombrío.");
            return;
        }

        CambiarHeroe(nuevoIndice);
    }

    /// <summary>Ejecuta el cambio de héroe manteniendo vidas independientes.</summary>
    public void CambiarHeroe(int nuevoIndice)
    {
        if (nuevoIndice == activeHeroIndex) return;
        if (nuevoIndice < 0 || nuevoIndice > 1) return;

        activeHeroIndex = nuevoIndice;
        HeroCombatData heroe = ActiveHero;

        // El HealthComponent se re-inicializa con los valores propios del nuevo héroe.
        saludHeroe.Initialize(heroe.maxHealth, heroe.currentHealth);
        ActualizarVisualProxy();

        Debug.Log($"[TAG-TEAM] Relevo a {heroe.heroName} ({heroe.element}). Vida: {heroe.currentHealth}/{heroe.maxHealth}");

        if (CameraFollow.Instance != null) CameraFollow.Instance.TriggerScreenShake(0.12f, 0.15f);
        // Modelo anterior apagado y nuevo encendido (ActualizarVisualProxy): ahora el aura
        // propia del héroe que ENTRA (amarillo-café Caelen, verde Illya), sin instanciar nada.
        ReproducirVfxRelevo(nuevoIndice);

        OnHeroeCambiado?.Invoke(heroe);
    }

    /// <summary>
    /// SetActive(true) + Play() al VFX del héroe que entra, espera 'duracionVfxRelevo' y
    /// después Stop() + SetActive(false). Reutiliza siempre el mismo GameObject.
    /// </summary>
    private void ReproducirVfxRelevo(int indice)
    {
        GameObject vfx = (indice == 0) ? vfxRelevoCaelenEscena : vfxRelevoIllyaEscena;
        if (vfx == null) return;

        if (rutinasRelevo[indice] != null) StopCoroutine(rutinasRelevo[indice]);
        rutinasRelevo[indice] = StartCoroutine(RutinaVfxRelevo(indice, vfx));
    }

    private IEnumerator RutinaVfxRelevo(int indice, GameObject vfx)
    {
        ParticleSystem[] sistemas = (indice == 0) ? sistemasRelevoCaelen : sistemasRelevoIllya;

        vfx.SetActive(true);
        for (int i = 0; i < sistemas.Length; i++)
        {
            if (sistemas[i] == null) continue;
            sistemas[i].Clear(false);
            sistemas[i].Play(false);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, duracionVfxRelevo));

        for (int i = 0; i < sistemas.Length; i++)
        {
            if (sistemas[i] != null) sistemas[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        vfx.SetActive(false);
        rutinasRelevo[indice] = null;
    }

    /// <summary>Reacciona a cambios de vida del héroe activo y los persiste en su perfil.</summary>
    private void AlCambiarVidaHeroe(float actual, float maximo)
    {
        HeroCombatData heroe = ActiveHero;
        if (heroe == null) return;

        heroe.currentHealth = actual;
        heroe.maxHealth = maximo;

        OnVidaHeroeCambiada?.Invoke(actual, maximo);
    }

    // ------------------------------------------------------------------
    // DERROTA Y REAPARICIÓN
    // ------------------------------------------------------------------

    /// <summary>
    /// Un héroe ha llegado a 0 de vida. El GameObject del Player no se destruye
    /// ni se desactiva: se releva automáticamente al héroe que siga en pie y,
    /// si ambos han caído, se abre el modal de derrota.
    /// </summary>
    private void AlMorirHeroeActivo()
    {
        caido[activeHeroIndex] = true;

        int otro = 1 - activeHeroIndex;
        HeroCombatData otroHeroe = (otro == 0) ? caelen : illya;

        // Illya solo puede relevar si ya fue rescatada de la celda del Nivel 1.
        bool otroHabilitado = (otro == 0) || illyaDesbloqueada;
        bool otroEnPie = otroHabilitado && !caido[otro] && otroHeroe.currentHealth > 0f;

        if (otroEnPie)
        {
            Debug.Log($"[TAG-TEAM] {ActiveHero.heroName} ha caído; relevo automático a {otroHeroe.heroName}.");
            CambiarHeroe(otro);
            return;
        }

        // Caelen ha caído con Illya todavía sellada: no se cambia de personaje.
        if (activeHeroIndex == 0 && !illyaDesbloqueada)
        {
            Debug.Log("[TAG-TEAM] Caelen ha caído e Illya sigue sellada: derrota sin relevo.");
            OnDerrotaSinIllya?.Invoke();
            IniciarSecuenciaDerrota(mensajeCaelenSolo);
            return;
        }

        Debug.Log("[TAG-TEAM] Ambos héroes han caído: se abre la pantalla de derrota.");
        OnAmbosCaidos?.Invoke();
        IniciarSecuenciaDerrota(mensajeAmbosCaidos);
    }

    /// <summary>Cae Caelen y Illya sigue sellada: no hay relevo posible.</summary>
    public void MostrarDerrotaCaelenSolo()
    {
        IniciarSecuenciaDerrota(mensajeCaelenSolo);
    }

    /// <summary>Cae también el segundo héroe: derrota total.</summary>
    public void MostrarDerrotaAmbos()
    {
        IniciarSecuenciaDerrota(mensajeAmbosCaidos);
    }

    private void IniciarSecuenciaDerrota(string mensaje)
    {
        if (derrotaActiva || rutinaDerrotaEnCurso) return;
        StartCoroutine(RutinaDerrota(mensaje));
    }

    /// <summary>
    /// Secuencia de derrota del jugador al llegar a 0 de vida:
    ///   1. Dispara 'Muerte' en el Animator del héroe que ha caído.
    ///   2. Pausa estricta de 'pausaAntesDelModal' (con el juego aún en marcha, para
    ///      que la animación corra).
    ///   3. Pide al Canvas_HUD que abra 'Modal_Derrota'; solo entonces se congela el tiempo.
    /// </summary>
    private IEnumerator RutinaDerrota(string mensaje)
    {
        rutinaDerrotaEnCurso = true;

        if (entrada != null) entrada.EntradaBloqueada = true;

        Animator animador = AnimadorHeroeActivo;
        if (animador != null && animador.isActiveAndEnabled)
        {
            // 'HeroVisualAnimator' ya dispara 'Die' en el mismo frame: se limpia para que
            // la muerte entre por un único camino y la transición no se reinicie dos veces.
            animador.ResetTrigger(IdDie);
            animador.SetTrigger(IdMuerte);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, pausaAntesDelModal));

        rutinaDerrotaEnCurso = false;
        derrotaActiva = true;

        if (canvasHUD != null) canvasHUD.MostrarModalDerrota(mensaje);

        // El Modal_Derrota congela el tiempo por sí mismo (UIModalWindow.OnEnable).

        Debug.Log("[DERROTA] " + mensaje + " Usa 'Btn_Reiniciar' o pulsa 'R' para reiniciar el nivel.");
    }

    /// <summary>Cierra el panel sin reaparecer (botón de cierre del modal).</summary>
    public void CerrarPanelDerrota()
    {
        derrotaActiva = false;
        if (canvasHUD != null) canvasHUD.OcultarModalDerrota();
        // Al apagarse el modal, su UIModalWindow libera la pausa (OnDisable).
    }

    /// <summary>
    /// REINICIO TOTAL DEL NIVEL (hard reset). El botón 'Btn_Reiniciar' del modal y la
    /// tecla 'R' llaman aquí: se recarga la escena activa completa, de modo que tótems,
    /// VFX, enemigos y estados de Idle vuelven al estado horneado en el editor.
    /// </summary>
    public void Reaparecer()
    {
        // ANTES de recargar: el tiempo vuelve a 1 y el contador de modales a 0, para que la
        // escena nueva jamás arranque congelada (softlock) aunque haya un modal abierto.
        UIModalWindow.ReiniciarPausa();
        Time.timeScale = 1f;

        if (entrada != null) entrada.EntradaBloqueada = false;

        GameSaveState.BorrarTodo();

        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ResetearProgreso();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Conmuta los enchufes visuales de cada héroe. Los modelos finales (FBX) viven dentro
    /// de cada enchufe, así que aquí no hay ninguna malla placeholder que conmutar.
    /// Los puntos de ataque ya no se tocan: son fijos y los resuelve PlayerCombat.
    /// </summary>
    private void ActualizarVisualProxy()
    {
        bool caelenActivo = activeHeroIndex == 0;

        if (socketCaelen != null && socketCaelen.activeSelf != caelenActivo)
            socketCaelen.SetActive(caelenActivo);

        if (socketIllya != null && socketIllya.activeSelf != !caelenActivo)
            socketIllya.SetActive(!caelenActivo);
    }

    /// <summary>Desbloquea a Illya al completar el mini-puzle de rescate del Nivel 1.</summary>
    public void DesbloquearIllya(bool desbloqueada)
    {
        illyaDesbloqueada = desbloqueada;
        GameSaveState.IllyaDesbloqueada = desbloqueada;

        if (GameProgressManager.Instance != null) GameProgressManager.Instance.DesbloquearIllya(desbloqueada);

        // El selector pentagrama solo se repinta con OnHeroeCambiado; se publica
        // aquí para que la ranura de Illya se ilumine en el mismo frame en que
        // se libera, sin esperar a un relevo.
        OnHeroeCambiado?.Invoke(ActiveHero);

        Debug.Log(desbloqueada
            ? "[TAG-TEAM] Illya liberada: el relevo con 'Tab' queda habilitado."
            : "[TAG-TEAM] Illya sellada: el relevo con 'Tab' queda deshabilitado.");
    }
}
