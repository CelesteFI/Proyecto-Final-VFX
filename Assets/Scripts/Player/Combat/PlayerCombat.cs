using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

/// <summary>
/// Módulo ÚNICO de combate del jugador. TagTeamManager solo gestiona a los héroes
/// (modelos, animators, índice activo, VFX de relevo); todo lo que golpea, dispara,
/// invoca o enfría vive aquí. La lógica se decide con el índice del héroe activo
/// (0 = Caelen, 1 = Illya):
///
///   Q (ataque básico)
///     · Caelen, melee: hitbox esférica en 'puntoAtaqueNormalCaelen' y VFX_Hero_SkillQ
///       en ese mismo instante (el timing no cambia).
///     · Illya, distancia: instancia VFX_AtaqueBasico_Illya en 'puntoAtaqueNormalIllya'.
///       El proyectil es 100% Particle System (Start Speed en Z local + Sub-Emitter en
///       'Death' hacia Green hit): este script NO lo mueve. Sí lee su Start Speed/Start
///       Lifetime, barre el volumen que recorre y, si toca a un enemigo o a una flor del
///       puzzle antes de agotarse, aplica daño/interacción, instancia 'Green hit' y
///       destruye la instancia antes de tiempo. Sin contacto, aplica el daño ciego
///       original al final del recorrido.
///
///   Clic derecho (especial)
///     · Caelen: instancia 'AtaqueEspecial_Caelen' en 'puntoEspecialCaelen'.
///     · Illya: instancia 'AtaqueEspecial_Illya' en 'puntoEspecialIllya'.
///       El interior de esos prefabs lo gestiona el usuario; aquí solo se instancian.
///       Ambos se invocan con la rotación ORIGINAL del prefab (prefab.transform.rotation),
///       nunca con Quaternion.identity, para que las raíces/rocas no aparezcan acostadas.
///
/// Además es la única fuente de cooldowns hacia el HUD: cada frame envía a Canvas_HUD el
/// progreso real del Dash (PlayerController), de la Q y de la habilidad especial.
/// No toca el Animator: solo publica OnAtaqueBasico / OnHabilidad, que ya consume
/// HeroVisualAnimator.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [SerializeField] private TagTeamManager tagTeam;
    [SerializeField] private PlayerInputReader entrada;
    [SerializeField] private LayerMask capasAfectadas = 1;

    [Header("Puntos de Ataque Fijos")]
    [Tooltip("Centro exterior del escudo de Caelen: origen de la hitbox melee y del VFX de la Q.")]
    [SerializeField] private Transform puntoAtaqueNormalCaelen;
    [Tooltip("Punta superior del báculo de Illya: de aquí sale el proyectil de la Q.")]
    [SerializeField] private Transform puntoAtaqueNormalIllya;
    [Tooltip("A ras de suelo y CERCA de Caelen: ancla del Muro de Basalto.")]
    [SerializeField] private Transform puntoEspecialCaelen;
    [Tooltip("A ras de suelo y LEJOS de Illya: ancla de las Raíces Voraces.")]
    [SerializeField] private Transform puntoEspecialIllya;

    [Header("Prefabs · Ataque Básico (Q)")]
    [Tooltip("Caelen: VFX_Hero_SkillQ. Se instancia en el punto de contacto, al golpear.")]
    [SerializeField, FormerlySerializedAs("vfxAtaqueCaelen")] private GameObject prefabQCaelen;
    [Tooltip("Illya: VFX_AtaqueBasico_Illya. Particle System puro que viaja por Start Speed y explota con un Sub-Emitter (Death).")]
    [SerializeField] private GameObject prefabQIllya;

    [Header("Prefabs · Ataque Especial (Clic Derecho)")]
    [Tooltip("Caelen: prefab del ataque especial (Muro de Basalto). Se instancia en 'puntoEspecialCaelen'.")]
    [SerializeField, FormerlySerializedAs("prefabEspecialCaelen"), FormerlySerializedAs("prefabMuroBasalto")]
    private GameObject AtaqueEspecial_Caelen;
    [Tooltip("Illya: prefab del ataque especial (Raíces Voraces). Se instancia en 'puntoEspecialIllya'.")]
    [SerializeField, FormerlySerializedAs("prefabEspecialIllya"), FormerlySerializedAs("prefabRaicesAoE")]
    private GameObject AtaqueEspecial_Illya;
    [Tooltip("Segundos tras los que se destruye la instancia del especial. 0 = no se destruye (el prefab se gestiona solo).")]
    [SerializeField] private float vidaInstanciaEspecial = 5f;
    [SerializeField] private float distanciaMuro = 2.5f;
    [Tooltip("Radio del golpe de tierra que aplica el Muro de Basalto al emerger.")]
    [SerializeField] private float radioGolpeMuro = 2.8f;
    [SerializeField] private float radioRaices = 3f;
    [SerializeField] private float danoRaices = 15f;
    [Tooltip("Duración del anclaje de zarzas que inmoviliza a los enemigos alcanzados.")]
    [SerializeField] private float duracionZarzas = 2.5f;

    [Header("Proyectil de Illya (Q) · daño ciego")]
    [Tooltip("Radio del Physics.OverlapSphere que se aplica en el punto donde termina el recorrido del proyectil.")]
    [SerializeField] private float radioExplosionProyectil = 2.4f;
    [Tooltip("Adelanto del origen a lo largo de la dirección para que el proyectil no nazca dentro del modelo.")]
    [SerializeField, FormerlySerializedAs("desplazamientoOrigenRayo")] private float desplazamientoOrigenProyectil = 0.15f;
    [Tooltip("Elevación (en Y) aplicada al punto de instanciación del proyectil para que no nazca a ras del báculo.")]
    [SerializeField] private float elevacionOrigenProyectil = 0.45f;
    [Tooltip("Segundos extra (tras el Start Lifetime) que se conserva la instancia para que el Sub-Emitter termine su explosión.")]
    [SerializeField] private float margenLimpiezaProyectil = 3f;

    [Header("Proyectil · impacto en vuelo")]
    [Tooltip("Capas que puede golpear el proyectil. El terreno no lo detiene: solo cuenta lo que tenga IDamageable o IGolpeableElemental.")]
    [SerializeField] private LayerMask capasImpactoProyectil = ~0;
    [Tooltip("Radio (m) del volumen que barre el proyectil para detectar el impacto antes de agotarse.")]
    [SerializeField] private float radioImpactoProyectil = 0.6f;
    [Tooltip("Efecto que se instancia en el punto de impacto cuando el proyectil choca en vuelo (Green hit).")]
    [SerializeField] private GameObject prefabImpactoVerde;

    /// <summary>Segundos que faltan para poder volver a usar la Q.</summary>
    public float CooldownAtaqueRestante => temporizadorAtaque;

    /// <summary>Se dispara al iniciar un ataque básico. Lo consume el Animator del héroe.</summary>
    public event System.Action OnAtaqueBasico;

    /// <summary>Se dispara al invocar la habilidad elemental. Lo consume el Animator del héroe.</summary>
    public event System.Action OnHabilidad;

    private CharacterController controlador;
    private PlayerController locomocion;
    private Canvas_HUD canvasHUD;
    private float temporizadorAtaque;

    private readonly HashSet<int> raicesProcesadas = new HashSet<int>();

    private bool EsCaelenActivo => tagTeam == null || tagTeam.ActiveHeroIndex == 0;

    private void Awake()
    {
        if (tagTeam == null) tagTeam = GetComponent<TagTeamManager>();
        if (entrada == null) entrada = GetComponent<PlayerInputReader>();

        controlador = GetComponent<CharacterController>();
        locomocion = GetComponent<PlayerController>();

        // Única vía hacia la interfaz: el HUD que ya tiene PlayerInputReader.
        if (entrada != null) canvasHUD = entrada.canvasHUD;

        if (capasAfectadas == 0) capasAfectadas = 1;
    }

    private void Update()
    {
        if (entrada == null || tagTeam == null) return;

        if (temporizadorAtaque > 0f) temporizadorAtaque -= Time.deltaTime;

        // Los cooldowns de habilidad de AMBOS héroes corren siempre, activos o no.
        EnfriarHabilidad(tagTeam.caelen);
        EnfriarHabilidad(tagTeam.illya);

        EnviarCooldownsAlHUD();

        // Ninguna acción de combate se dispara con el puntero sobre la interfaz.
        if (PunteroSobreUI()) return;

        if (entrada.AtaquePulsado && temporizadorAtaque <= 0f) EjecutarAtaqueBasico();
        if (entrada.HabilidadPulsada) EjecutarHabilidadElemental();
    }

    private static void EnfriarHabilidad(HeroCombatData heroe)
    {
        if (heroe != null && heroe.skillCooldownTimer > 0f) heroe.skillCooldownTimer -= Time.deltaTime;
    }

    // ------------------------------------------------------------------
    // COOLDOWNS → HUD
    // ------------------------------------------------------------------

    /// <summary>
    /// Envía al HUD el progreso REAL (0 = recién usada, 1 = lista) del Dash, la Q y la
    /// habilidad especial del héroe activo. El HUD no calcula ni adivina ningún tiempo.
    /// </summary>
    private void EnviarCooldownsAlHUD()
    {
        if (canvasHUD == null) return;

        HeroCombatData heroe = tagTeam.ActiveHero;
        if (heroe == null) return;

        float dash = (locomocion != null)
            ? ProgresoRecarga(locomocion.DashCooldownRestante, locomocion.DashCooldownTotal)
            : 1f;
        float ataque = ProgresoRecarga(temporizadorAtaque, heroe.attackCooldown);
        float especial = ProgresoRecarga(heroe.skillCooldownTimer, heroe.skillCooldown);

        canvasHUD.ActualizarCooldowns(dash, ataque, especial);
    }

    private static float ProgresoRecarga(float restante, float total)
    {
        if (total <= 0f) return 1f;
        return Mathf.Clamp01(1f - (Mathf.Max(0f, restante) / total));
    }

    // ------------------------------------------------------------------
    // PUNTOS DE ATAQUE
    // ------------------------------------------------------------------

    /// <summary>Punto de ataque normal del héroe ACTIVO (se resuelve en cada golpe).</summary>
    private Transform PuntoAtaqueNormal()
    {
        return EsCaelenActivo ? puntoAtaqueNormalCaelen : puntoAtaqueNormalIllya;
    }

    /// <summary>
    /// Ancla del ataque especial del héroe ACTIVO. Si no está cableada se conserva el
    /// cálculo original que recibe 'porDefecto'.
    /// </summary>
    private Vector3 PuntoEspecial(Vector3 porDefecto)
    {
        Transform especial = EsCaelenActivo ? puntoEspecialCaelen : puntoEspecialIllya;
        return (especial != null) ? especial.position : porDefecto;
    }

    /// <summary>
    /// Protección común del esquema de controles: el clic derecho (habilidad)
    /// y el ataque básico quedan inertes mientras el puntero está sobre la UI.
    /// </summary>
    private static bool PunteroSobreUI()
    {
        EventSystem sistema = EventSystem.current;
        return sistema != null && sistema.IsPointerOverGameObject();
    }

    /// <summary>
    /// Publica el disparo del ataque básico para que el Animator del héroe activo
    /// reproduzca el golpe.
    /// </summary>
    public void NotificarAtaqueBasico()
    {
        OnAtaqueBasico?.Invoke();
    }

    // ------------------------------------------------------------------
    // ATAQUE BÁSICO (Q)
    // ------------------------------------------------------------------

    /// <summary>Resuelve la Q del héroe activo según su índice: Caelen melee, Illya proyectil.</summary>
    public void EjecutarAtaqueBasico()
    {
        if (PunteroSobreUI()) return;

        HeroCombatData heroe = tagTeam.ActiveHero;
        if (heroe == null) return;

        if (EsCaelenActivo) AtaqueBasicoCaelen(heroe);
        else AtaqueBasicoIllya(heroe);
    }

    /// <summary>
    /// Caelen (melee). El compromiso físico y el VFX se ejecutan SIEMPRE, conecte o no,
    /// para que el jugador perciba la intención del ataque; el daño y el retroceso solo
    /// se aplican a los objetivos dentro del rango de la capa afectada.
    /// </summary>
    private void AtaqueBasicoCaelen(HeroCombatData heroe)
    {
        temporizadorAtaque = heroe.attackCooldown;
        OnAtaqueBasico?.Invoke();

        // 1. Compromiso físico: traslación frontal que da peso al golpe.
        ImpulsarHaciaAdelante(heroe.attackImpulse);

        Transform ancla = PuntoAtaqueNormal();
        Vector3 centro = (ancla != null)
            ? ancla.position
            : transform.position + transform.forward * 1.2f;

        // 2. VFX_Hero_SkillQ en el punto de ataque, orientado a transform.forward.
        //    Es el único VFX de la Q: al chocar NO se instancia ningún impacto adicional.
        InstanciarImpacto(prefabQCaelen, centro, transform.forward);

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
    // PROYECTIL DE ILLYA
    // ------------------------------------------------------------------

    /// <summary>
    /// Illya (distancia). Instancia VFX_AtaqueBasico_Illya en 'puntoAtaqueNormalIllya'
    /// orientado hacia delante. El viaje y la explosión visual son del Particle System
    /// (Start Speed + Sub-Emitter 'Death'). Aquí solo se lee su Start Lifetime y su
    /// Start Speed, se espera ese tiempo exacto y se aplica el daño ciego en el punto
    /// frontal donde se supone que terminó el recorrido.
    /// </summary>
    private void AtaqueBasicoIllya(HeroCombatData heroe)
    {
        if (prefabQIllya == null)
        {
            Debug.LogWarning("[COMBATE] Illya no tiene prefab de proyectil (VFX_AtaqueBasico_Illya) asignado.");
            return;
        }

        temporizadorAtaque = Mathf.Max(0.05f, heroe.attackCooldown);
        OnAtaqueBasico?.Invoke();

        Transform origen = PuntoAtaqueNormal();
        Vector3 direccion = DireccionDisparo();

        Vector3 posicion = ((origen != null) ? origen.position : transform.position + Vector3.up * 1.5f)
                           + direccion * desplazamientoOrigenProyectil
                           + Vector3.up * elevacionOrigenProyectil;

        GameObject instancia = Instantiate(prefabQIllya, posicion, RotacionHacia(direccion));

        ParticleSystem sistema = instancia.GetComponent<ParticleSystem>();
        if (sistema == null) sistema = instancia.GetComponentInChildren<ParticleSystem>();

        float espera = 0f;
        float distancia = 0f;
        if (sistema != null)
        {
            ParticleSystem.MainModule principal = sistema.main;
            espera = principal.startDelay.constantMax + principal.startLifetime.constantMax;
            distancia = principal.startSpeed.constantMax * principal.startLifetime.constantMax;
        }
        else
        {
            Debug.LogWarning("[COMBATE] VFX_AtaqueBasico_Illya no tiene ParticleSystem: no se aplicará daño.");
        }

        // Punto frontal donde termina el recorrido del proyectil.
        Vector3 fin = posicion + direccion * distancia;

        Destroy(instancia, espera + Mathf.Max(0f, margenLimpiezaProyectil));

        if (sistema != null) StartCoroutine(VueloProyectil(heroe, instancia, posicion, direccion, distancia, espera, fin));
    }

    /// <summary>
    /// Acompaña al Particle System durante su vida útil: barre el tramo recorrido en cada
    /// frame y, si el proyectil toca a un enemigo o a una flor del puzzle, aplica el
    /// daño/interacción, instancia 'Green hit' y destruye la instancia antes de tiempo.
    /// Si no toca nada, conserva el daño ciego original en el punto final del recorrido.
    /// </summary>
    private IEnumerator VueloProyectil(HeroCombatData heroe, GameObject instancia, Vector3 origen,
                                       Vector3 direccion, float distancia, float espera, Vector3 fin)
    {
        float velocidad = (espera > 0.01f) ? distancia / espera : distancia;
        float tiempo = 0f;
        Vector3 anterior = origen;

        while (tiempo < espera)
        {
            tiempo += Time.deltaTime;

            Vector3 actual = origen + direccion * Mathf.Min(velocidad * tiempo, distancia);

            if (BuscarImpactoProyectil(anterior, actual, out Collider objetivo, out Vector3 punto))
            {
                ResolverImpactoProyectil(heroe, objetivo, punto, direccion);

                if (instancia != null) Destroy(instancia);
                yield break;
            }

            anterior = actual;
            yield return null;
        }

        bool golpeoElemental;
        int alcanzados = AplicarDanoArea(fin, radioExplosionProyectil, heroe.attackDamage, heroe,
                                         false, 1f, out golpeoElemental);

        if (alcanzados > 0 || golpeoElemental)
        {
            HitstopManager.Instance?.TriggerHitstop(0.03f);
            CameraFollow.Instance?.TriggerScreenShake(0.10f, 0.18f);
        }

        Debug.Log($"[COMBATE] Proyectil de {heroe.heroName} terminó su recorrido tras {espera:F2}s: {alcanzados} entidad(es) por {heroe.attackDamage} de daño.");
    }

    /// <summary>
    /// Muestrea el tramo que recorre el proyectil en un frame. Solo son válidos los
    /// colliders con IDamageable (enemigos) o IGolpeableElemental (flores del puzzle):
    /// el terreno, los props y el propio proyectil no detienen el disparo.
    /// </summary>
    private bool BuscarImpactoProyectil(Vector3 desde, Vector3 hasta, out Collider objetivo, out Vector3 punto)
    {
        objetivo = null;
        punto = hasta;

        float largo = Vector3.Distance(desde, hasta);
        float radio = Mathf.Max(0.05f, radioImpactoProyectil);
        int subpasos = Mathf.Max(1, Mathf.CeilToInt(largo / radio));

        for (int i = 1; i <= subpasos; i++)
        {
            Vector3 muestra = Vector3.Lerp(desde, hasta, (float)i / subpasos);

            Collider[] hits = Physics.OverlapSphere(muestra, radio, capasImpactoProyectil,
                                                    QueryTriggerInteraction.Collide);

            for (int h = 0; h < hits.Length; h++)
            {
                Collider col = hits[h];
                if (col == null) continue;
                if (EsParteDelHeroe(col.transform)) continue;
                if (col.GetComponentInParent<IDamageable>() == null &&
                    col.GetComponentInParent<IGolpeableElemental>() == null) continue;

                objetivo = col;
                punto = muestra;
                return true;
            }
        }

        return false;
    }

    /// <summary>Aplica el daño/interacción del impacto en vuelo y su feedback visual.</summary>
    private void ResolverImpactoProyectil(HeroCombatData heroe, Collider objetivo, Vector3 punto, Vector3 direccion)
    {
        IDamageable victima = objetivo.GetComponentInParent<IDamageable>();
        if (victima != null)
        {
            Vector3 retroceso = (objetivo.transform.position - transform.position).normalized + Vector3.up * 0.2f;
            victima.TakeDamage(heroe.attackDamage, punto, retroceso * heroe.knockbackStrength);
        }

        IGolpeableElemental golpeable = objetivo.GetComponentInParent<IGolpeableElemental>();
        if (golpeable != null) golpeable.RecibirGolpeElemental(heroe.element, punto);

        InstanciarImpacto(prefabImpactoVerde, punto, direccion);

        HitstopManager.Instance?.TriggerHitstop(0.03f);
        CameraFollow.Instance?.TriggerScreenShake(0.10f, 0.18f);

        Debug.Log($"[COMBATE] Proyectil de {heroe.heroName} impactó en vuelo contra {objetivo.name}.");
    }

    /// <summary>
    /// Dirección del disparo: SIEMPRE el frente horizontal del personaje. Illya ya no
    /// apunta con el cursor; el proyectil sale del báculo hacia delante.
    /// </summary>
    private Vector3 DireccionDisparo()
    {
        Vector3 adelante = transform.forward;
        adelante.y = 0f;

        if (adelante.sqrMagnitude < 0.0001f) adelante = Vector3.forward;
        return adelante.normalized;
    }

    /// <summary>Rotación segura: evita el LookRotation degenerado cuando se dispara en vertical.</summary>
    private static Quaternion RotacionHacia(Vector3 direccion)
    {
        if (direccion.sqrMagnitude < 0.0001f) return Quaternion.identity;

        Vector3 normalizada = direccion.normalized;
        if (Mathf.Abs(Vector3.Dot(normalizada, Vector3.up)) > 0.99f)
            return Quaternion.LookRotation(normalizada, Vector3.forward);

        return Quaternion.LookRotation(normalizada);
    }

    // ------------------------------------------------------------------
    // HABILIDAD ELEMENTAL (CLIC DERECHO)
    // ------------------------------------------------------------------

    /// <summary>Especial del héroe activo: Muro de Basalto (Caelen) o Raíces Voraces (Illya).</summary>
    public void EjecutarHabilidadElemental()
    {
        if (PunteroSobreUI()) return;

        HeroCombatData heroe = tagTeam.ActiveHero;
        if (heroe == null) return;

        if (heroe.skillCooldownTimer > 0f)
        {
            Debug.Log($"[COMBATE] Habilidad en recarga: {heroe.skillCooldownTimer:F1}s restantes.");
            return;
        }

        heroe.skillCooldownTimer = heroe.skillCooldown;
        OnHabilidad?.Invoke();

        if (EsCaelenActivo) InvocarMuroBasalto(heroe);
        else InvocarRaicesVoraces(heroe);
    }

    private void InvocarMuroBasalto(HeroCombatData heroe)
    {
        CameraFollow.Instance?.TriggerScreenShake(0.2f, 0.35f);

        if (AtaqueEspecial_Caelen == null)
        {
            Debug.LogWarning("[COMBATE] AtaqueEspecial_Caelen no tiene prefab asignado.");
            return;
        }

        Vector3 posicion = PuntoEspecial(transform.position + transform.forward * distanciaMuro);
        GameObject muro = InstanciarEspecial(AtaqueEspecial_Caelen, posicion);

        // Parenting dinámico: el muro se cuelga del ancla de Caelen, de modo que si el héroe
        // rota, el ataque rota con él. worldPositionStays conserva el sitio donde nació.
        if (muro != null && puntoEspecialCaelen != null)
            muro.transform.SetParent(puntoEspecialCaelen, true);

        // El muro emerge del suelo: el golpe de tierra daña lo que pilla delante.
        bool golpeoElemental;
        AplicarDanoArea(posicion, radioGolpeMuro, heroe.attackDamage * 1.5f, heroe, true, 0.6f, out golpeoElemental);

        Debug.Log("[COMBATE] Caelen invocó el Muro de Basalto.");
    }

    private void InvocarRaicesVoraces(HeroCombatData heroe)
    {
        CameraFollow.Instance?.TriggerScreenShake(0.18f, 0.2f);

        // El ancla del ataque especial manda: las raíces emergen del suelo frente a
        // Illya y el golpe en área se centra en ese mismo punto, para que el VFX y el
        // daño coincidan.
        Vector3 centro = PuntoEspecial(transform.position);

        if (AtaqueEspecial_Illya != null)
            InstanciarEspecial(AtaqueEspecial_Illya, centro);
        else
            Debug.LogWarning("[COMBATE] AtaqueEspecial_Illya no tiene prefab asignado.");

        bool golpeoElemental;
        int alcanzados = AplicarDanoArea(centro, radioRaices, danoRaices, heroe, true, 0.6f, out golpeoElemental);

        Debug.Log($"[COMBATE] Illya invocó Raíces Voraces sobre {alcanzados} objetivo(s) con anclaje de zarzas {duracionZarzas:F1}s.");
    }

    /// <summary>
    /// Daño en área compartido por las habilidades y por la explosión del proyectil:
    /// aplica IDamageable a entidades y IGolpeableElemental a estructuras (raíces,
    /// tótems, celda). Devuelve cuántas entidades con vida alcanzó.
    /// </summary>
    private int AplicarDanoArea(Vector3 centro, float radio, float dano, HeroCombatData heroe,
                                bool inmovilizar, float factorRetroceso, out bool golpeoElemental)
    {
        golpeoElemental = false;

        Collider[] hits = Physics.OverlapSphere(centro, radio, capasAfectadas);
        raicesProcesadas.Clear();
        int alcanzados = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;
            if (EsParteDelHeroe(col.transform)) continue;
            if (!raicesProcesadas.Add(col.transform.root.GetInstanceID())) continue;

            IGolpeableElemental golpeable = col.GetComponentInParent<IGolpeableElemental>();
            if (golpeable != null)
            {
                golpeable.RecibirGolpeElemental(heroe.element, col.ClosestPoint(centro));
                golpeoElemental = true;
            }

            IDamageable victima = col.GetComponentInParent<IDamageable>();
            if (victima == null) continue;

            if (inmovilizar)
            {
                EnemyAI ia = col.GetComponentInParent<EnemyAI>();
                if (ia != null) ia.AplicarInmovilizacion(duracionZarzas);
            }

            Vector3 direccion = (col.transform.position - centro).normalized + Vector3.up * 0.2f;
            victima.TakeDamage(dano, col.transform.position, direccion * heroe.knockbackStrength * factorRetroceso);
            alcanzados++;
        }

        return alcanzados;
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

    /// <summary>
    /// Instancia el prefab del ataque especial tal cual, sin tocar su interior, y SIEMPRE
    /// con la rotación original del prefab (prefab.transform.rotation): los modelos de
    /// raíces y rocas traen su orientación cocida, así que usar Quaternion.identity los
    /// sacaría acostados. Solo si 'vidaInstanciaEspecial' &gt; 0 se limpia la instancia.
    /// Devuelve la instancia para que el llamante decida si la emparenta (Caelen) o la deja
    /// fija en el mundo (Illya).
    /// </summary>
    private GameObject InstanciarEspecial(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return null;

        GameObject instancia = Instantiate(prefab, posicion, prefab.transform.rotation);
        if (vidaInstanciaEspecial > 0f) Destroy(instancia, vidaInstanciaEspecial);

        return instancia;
    }

    /// <summary>Instancia un VFX de impacto autodestruible y orientado.</summary>
    private void InstanciarImpacto(GameObject prefab, Vector3 posicion, Vector3 direccion)
    {
        if (prefab == null) return;

        Quaternion rotacion = (direccion.sqrMagnitude > 0.0001f)
            ? RotacionHacia(direccion)
            : transform.rotation;

        GameObject instancia = Instantiate(prefab, posicion, rotacion);
        Destroy(instancia, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (tagTeam == null || tagTeam.ActiveHero == null) return;

        Transform ancla = PuntoAtaqueNormal();
        Vector3 centro = ancla != null ? ancla.position : transform.position + transform.forward * 1.2f;

        if (EsCaelenActivo)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(centro, tagTeam.ActiveHero.attackRange);
        }
        else
        {
            Gizmos.color = new Color(0.25f, 1f, 0.35f, 0.7f);
            ParticleSystem sistema = (prefabQIllya != null) ? prefabQIllya.GetComponent<ParticleSystem>() : null;
            if (sistema == null) return;

            ParticleSystem.MainModule principal = sistema.main;
            float distancia = principal.startSpeed.constantMax * principal.startLifetime.constantMax;
            Vector3 fin = centro + DireccionDisparo() * distancia;
            Gizmos.DrawLine(centro, fin);
            Gizmos.DrawWireSphere(fin, radioExplosionProyectil);
        }
    }
}
