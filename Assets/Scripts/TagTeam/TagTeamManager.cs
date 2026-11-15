using System;
using UnityEngine;

/// <summary>
/// Gestor del sistema Tag-Team: conserva los datos de Caelen e Illya, arbitra el
/// relevo ('Tab') y mantiene vidas independientes delegando la vitalidad en
/// un único HealthComponent que se re-inicializa al cambiar de héroe.
/// El combate vive en PlayerCombat (Assets/Scripts/Combat) para mantener el
/// principio de responsabilidad única.
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
        attackImpulse = 0.55f,
        proxyColor = new Color(0.25f, 0.65f, 0.35f)
    };

    [Header("Estado")]
    [SerializeField] private int activeHeroIndex = 0; // 0 = Caelen, 1 = Illya
    [SerializeField] private bool illyaDesbloqueada = true;

    [Tooltip("Héroe con el que arranca la escena. Falso = Player_Caelen (Nivel 1). " +
             "Verdadero = Player_Illya (Nivel 2). Si Illya sigue sellada, manda Caelen.")]
    public bool iniciarConIllya = false;

    [Header("Reaparición tras la Derrota")]
    [Tooltip("Punto al que vuelve el héroe al pulsar 'R' cuando ambos han caído.")]
    public Vector3 puntoReaparicion = new Vector3(0f, 0.8f, 0f);

    public int ActiveHeroIndex => activeHeroIndex;
    public bool IllyaDesbloqueada => illyaDesbloqueada;
    public HeroCombatData ActiveHero => (activeHeroIndex == 0) ? caelen : illya;
    public HeroCombatData HeroeInactivo => (activeHeroIndex == 0) ? illya : caelen;

    /// <summary>
    /// Punto de ataque que corresponde al héroe activo: el centro exterior del escudo
    /// de Caelen o la punta del báculo de Illya. Si la escena no los tiene cableados,
    /// se devuelve el punto genérico para no dejar el golpe sin ancla.
    /// </summary>
    public Transform PuntoAtaqueActivo
    {
        get
        {
            Transform punto = (activeHeroIndex == 0) ? attackPointCaelen : attackPointIllya;
            return (punto != null) ? punto : attackPoint;
        }
    }

    /// <summary>
    /// Ancla del ataque especial del héroe ACTIVO: Caelen invoca el Muro de Basalto en un
    /// punto cercano y Illya hace emerger las Raíces Voraces mucho más adelante, acorde a
    /// su rol a distancia. Ambas anclas están a ras de suelo (local Y = 0 de su héroe).
    /// </summary>
    public Transform PuntoAtaqueEspecial
    {
        get { return (activeHeroIndex == 0) ? specialAttackPointCaelen : specialAttackPointIllya; }
    }

    /// <summary>Vitalidad del héroe activo (eventos observadores para el HUD).</summary>
    public HealthComponent SaludHeroe => saludHeroe;

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

    /// <summary>Se dispara tras reaparecer y restaurar a ambos héroes.</summary>
    public event Action OnReaparecido;

    [Header("Puntos de Ataque e Hitbox")]
    [Tooltip("Punto del héroe ACTIVO. El relevo lo refresca desde los puntos individuales.")]
    public Transform attackPoint;
    [Tooltip("Centro exterior del escudo de Caelen (lado opuesto al brazo).")]
    public Transform attackPointCaelen;
    [Tooltip("Punta superior del báculo de Illya.")]
    public Transform attackPointIllya;
    [Tooltip("Ancla del Muro de Basalto: a ras de suelo y CERCA de Caelen (ataque de vanguardia).")]
    public Transform specialAttackPointCaelen;
    [Tooltip("Ancla de Raíces Voraces: a ras de suelo y LEJOS de Illya (ataque a distancia).")]
    public Transform specialAttackPointIllya;
    public LayerMask enemyLayers;

    [Header("Prefabs de VFX")]
    public GameObject vfxSwapPrefab;
    public GameObject vfxRockWallPrefab;
    public GameObject vfxVinesAoEPrefab;

    [Header("Enchufes Visuales (Reemplazo Manual de Modelo)")]
    [Tooltip("Contenedor 'Player_Caelen' dentro de Visual_Model: arrastra aquí el FBX final.")]
    [SerializeField] private GameObject socketCaelen;

    [Tooltip("Contenedor 'Player_Illya' dentro de Visual_Model: arrastra aquí el FBX final.")]
    [SerializeField] private GameObject socketIllya;

    [Header("Enlaces (asignados en el Inspector)")]
    [Tooltip("PlayerInputReader del mismo Player. No se busca en runtime.")]
    [SerializeField] private PlayerInputReader entrada;

    private HealthComponent saludHeroe;

    // 0 = Caelen, 1 = Illya. Marca qué héroes han caído en la partida actual.
    private readonly bool[] caido = new bool[2];

    /// <summary>True cuando los dos héroes están a 0 de vida.</summary>
    public bool AmbosCaidos => caido[0] && caido[1];

    /// <summary>Consulta si un héroe concreto ha caído.</summary>
    public bool HeroeCaido(int indice) => indice >= 0 && indice < 2 && caido[indice];

    private void Awake()
    {
        Instance = this;

        if (entrada == null) entrada = GetComponent<PlayerInputReader>();

        saludHeroe = GetComponent<HealthComponent>();
        if (saludHeroe == null) saludHeroe = gameObject.AddComponent<HealthComponent>();

        saludHeroe.OnHealthChanged += AlCambiarVidaHeroe;
        saludHeroe.OnDied += AlMorirHeroeActivo;
    }

    private void OnDestroy()
    {
        if (saludHeroe != null)
        {
            saludHeroe.OnHealthChanged -= AlCambiarVidaHeroe;
            saludHeroe.OnDied -= AlMorirHeroeActivo;
        }
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

        // Inicializa la vitalidad del héroe que arranca la partida.
        saludHeroe.Initialize(ActiveHero.maxHealth, ActiveHero.currentHealth);
        ActualizarVisualProxy();

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

        // 2. Cooldowns de habilidad de ambos héroes (corren siempre, activos o no)
        if (caelen.skillCooldownTimer > 0f) caelen.skillCooldownTimer -= Time.deltaTime;
        if (illya.skillCooldownTimer > 0f) illya.skillCooldownTimer -= Time.deltaTime;
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
        if (vfxSwapPrefab != null) Instantiate(vfxSwapPrefab, transform.position, Quaternion.identity);

        OnHeroeCambiado?.Invoke(heroe);
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
    /// si ambos han caído, se notifica la derrota total.
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
            return;
        }

        Debug.Log("[TAG-TEAM] Ambos héroes han caído. Esperando reinicio con 'R'.");
        OnAmbosCaidos?.Invoke();
    }

    /// <summary>
    /// Reaparición completa tras la derrota: devuelve a Caelen al frente con la
    /// salud de ambos perfiles al 100 %. La reubicación física del Player la
    /// realiza el gestor de derrota mediante PlayerController.Reubicar.
    /// </summary>
    public void Reaparecer()
    {
        caido[0] = false;
        caido[1] = false;

        caelen.currentHealth = caelen.maxHealth;
        illya.currentHealth = illya.maxHealth;

        activeHeroIndex = 0;
        saludHeroe.Initialize(caelen.maxHealth, caelen.maxHealth);
        ActualizarVisualProxy();

        OnHeroeCambiado?.Invoke(ActiveHero);
        OnReaparecido?.Invoke();

        Debug.Log($"[TAG-TEAM] Reaparición completa: Caelen al frente con {caelen.currentHealth}/{caelen.maxHealth} de vida.");
    }

    /// <summary>
    /// Conmuta los enchufes visuales de cada héroe y reapunta el punto de ataque al
    /// que corresponde al activo. Los modelos finales (FBX) viven dentro de cada
    /// enchufe, así que aquí no hay ninguna malla placeholder que conmutar.
    /// </summary>
    private void ActualizarVisualProxy()
    {
        bool caelenActivo = activeHeroIndex == 0;

        if (socketCaelen != null && socketCaelen.activeSelf != caelenActivo)
            socketCaelen.SetActive(caelenActivo);

        if (socketIllya != null && socketIllya.activeSelf != !caelenActivo)
            socketIllya.SetActive(!caelenActivo);

        // El punto de ataque acompaña al héroe activo: escudo de Caelen o báculo de Illya.
        Transform puntoActivo = PuntoAtaqueActivo;
        if (puntoActivo != null) attackPoint = puntoActivo;
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

    /// <summary>Asigna la capa de enemigos desde código si la escena no la define.</summary>
    public void ConfigurarCapasDeDano(LayerMask capas)
    {
        enemyLayers = capas;
    }
}
