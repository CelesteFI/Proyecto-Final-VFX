using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de combate. Todo el árbol de UI es físico: este script solo escribe en
/// componentes ya presentes en la Hierarchy (nunca instancia GameObjects).
/// La vida se actualiza SOLO por eventos del HealthComponent (patrón observador);
/// Update() se reserva para lo que es inherentemente animado: la barra fantasma
/// de daño y el radial de cooldown.
/// </summary>
public class CombatHUDManager : MonoBehaviour
{
    public static CombatHUDManager Instance { get; private set; }

    [Header("Conexión con el Sistema Tag-Team")]
    public TagTeamManager tagTeam;

    [Header("Panel Héroe (Inferior Centro)")]
    public Image portraitHero;
    public TextMeshProUGUI textHeroName;
    public Slider healthSlider;
    public Slider healthDelaySlider;
    public Slider staminaSlider;
    public float delayBarSpeed = 1.8f;

    [Header("Barras de Reserva (orden: Caelen, Illya)")]
    [Tooltip("Índice 0 = Caelen, índice 1 = Illya. Se oculta la del héroe activo.")]
    public List<Slider> barrasReserva = new List<Slider>();
    public List<TextMeshProUGUI> etiquetasReserva = new List<TextMeshProUGUI>();

    [Header("Panel Habilidades (Inferior Derecha)")]
    public Image iconSkillRadialCooldown;
    public TextMeshProUGUI textSkillCooldown;

    [Header("Colores Temáticos GDD")]
    public Color colorCaelenPiedra = new Color(0.85f, 0.55f, 0.25f);
    public Color colorIllyaMadera = new Color(0.25f, 0.85f, 0.45f);

    [Header("Parpadeo de Daño")]
    [Tooltip("Duración del destello sobre el relleno de la barra al recibir un golpe.")]
    [SerializeField] private float duracionParpadeo = 0.18f;

    [Tooltip("Color que toma el relleno durante el parpadeo.")]
    [SerializeField] private Color colorImpacto = Color.white;

    private float vidaObjetivo = 1f;
    private float staminaObjetivo = 1f;
    private bool suscrito;

    private float vidaActualEtiqueta = -1f;
    private float vidaMaximaEtiqueta = -1f;

    private Image rellenoVida;
    private Color colorRellenoOriginal = Color.white;
    private Coroutine rutinaParpadeo;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CachearRellenoVida();
        SuscribirEventos();
        RefrescarIdentidad();
        RefrescarVida(tagTeam != null && tagTeam.SaludHeroe != null ? tagTeam.SaludHeroe.CurrentHealth : 1f,
                      tagTeam != null && tagTeam.SaludHeroe != null ? tagTeam.SaludHeroe.MaxHealth : 1f);
        RefrescarReservas();

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value = staminaObjetivo;
        }

        Canvas.ForceUpdateCanvases();
        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            text.ForceMeshUpdate(false, true);
    }

    private void OnDestroy()
    {
        DesuscribirEventos();
        if (Instance == this) Instance = null;
    }

    private void SuscribirEventos()
    {
        if (tagTeam == null || suscrito) return;

        tagTeam.OnVidaHeroeCambiada += RefrescarVida;
        tagTeam.OnHeroeCambiado += AlCambiarHeroe;

        if (tagTeam.SaludHeroe != null)
        {
            tagTeam.SaludHeroe.OnHealthChanged += RefrescarVida;
            tagTeam.SaludHeroe.OnDamaged += AlRecibirDano;
        }

        suscrito = true;
    }

    private void DesuscribirEventos()
    {
        if (tagTeam == null || !suscrito) return;

        tagTeam.OnVidaHeroeCambiada -= RefrescarVida;
        tagTeam.OnHeroeCambiado -= AlCambiarHeroe;

        if (tagTeam.SaludHeroe != null)
        {
            tagTeam.SaludHeroe.OnHealthChanged -= RefrescarVida;
            tagTeam.SaludHeroe.OnDamaged -= AlRecibirDano;
        }

        suscrito = false;
    }

    /// <summary>Guarda el relleno de la barra activa y su color base para el parpadeo.</summary>
    private void CachearRellenoVida()
    {
        if (healthSlider == null || healthSlider.fillRect == null) return;

        rellenoVida = healthSlider.fillRect.GetComponent<Image>();
        if (rellenoVida != null) colorRellenoOriginal = rellenoVida.color;
    }

    /// <summary>Destello blanco de 0.18 s sobre la barra de vida al recibir un golpe.</summary>
    private void AlRecibirDano(float cantidad, Vector3 puntoImpacto)
    {
        if (rellenoVida == null) return;

        if (rutinaParpadeo != null) StopCoroutine(rutinaParpadeo);
        rutinaParpadeo = StartCoroutine(RutinaParpadeo());
    }

    private IEnumerator RutinaParpadeo()
    {
        rellenoVida.color = colorImpacto;
        yield return new WaitForSeconds(duracionParpadeo);
        rellenoVida.color = colorRellenoOriginal;
        rutinaParpadeo = null;
    }

    private void Update()
    {
        // 1. Barra fantasma de daño (animación dependiente del tiempo)
        if (healthDelaySlider != null)
        {
            if (healthDelaySlider.value > vidaObjetivo)
                healthDelaySlider.value = Mathf.MoveTowards(healthDelaySlider.value, vidaObjetivo, Time.deltaTime * delayBarSpeed);
            else
                healthDelaySlider.value = vidaObjetivo;
        }

        // 2. Cooldown radial de la habilidad elemental
        if (tagTeam == null) return;
        HeroCombatData hero = tagTeam.ActiveHero;
        if (hero == null) return;

        if (iconSkillRadialCooldown != null)
        {
            float fillRatio = (hero.skillCooldown > 0f) ? (hero.skillCooldownTimer / hero.skillCooldown) : 0f;
            iconSkillRadialCooldown.fillAmount = Mathf.Clamp01(fillRatio);

            if (textSkillCooldown != null)
            {
                bool enRecarga = hero.skillCooldownTimer > 0.05f;
                if (textSkillCooldown.gameObject.activeSelf != enRecarga)
                    textSkillCooldown.gameObject.SetActive(enRecarga);

                if (enRecarga) textSkillCooldown.text = hero.skillCooldownTimer.ToString("F1");
            }
        }
    }

    // ------------------------------------------------------------------
    // OBSERVADORES DE ESTADO
    // ------------------------------------------------------------------

    /// <summary>Actualiza barras al recibir el evento OnHealthChanged.</summary>
    private void RefrescarVida(float actual, float maximo)
    {
        vidaActualEtiqueta = actual;
        vidaMaximaEtiqueta = maximo;

        vidaObjetivo = (maximo > 0f) ? Mathf.Clamp01(actual / maximo) : 0f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = vidaObjetivo;
        }

        PintarEtiqueta();
    }

    /// <summary>Escribe el nombre del héroe y su vida numérica en la etiqueta del panel.</summary>
    private void PintarEtiqueta()
    {
        if (textHeroName == null || tagTeam == null) return;

        HeroCombatData heroe = tagTeam.ActiveHero;
        if (heroe == null) return;

        textHeroName.text = (vidaMaximaEtiqueta > 0f)
            ? $"{heroe.heroName} · {Mathf.CeilToInt(vidaActualEtiqueta)} / {Mathf.CeilToInt(vidaMaximaEtiqueta)}"
            : heroe.heroName;
    }

    /// <summary>Refresca identidad, colores y la fila de reserva al completar un relevo.</summary>
    private void AlCambiarHeroe(HeroCombatData heroe)
    {
        RefrescarIdentidad();
        RefrescarReservas();

        // Al cambiar de héroe la barra fantasma se sincroniza de inmediato.
        if (healthDelaySlider != null && tagTeam != null && tagTeam.SaludHeroe != null)
            healthDelaySlider.value = tagTeam.SaludHeroe.Normalizada;
    }

    /// <summary>
    /// Pinta las barras compactas de los héroes en reserva en orden
    /// (Caelen, Illya), ocultando la del héroe que está en combate.
    /// </summary>
    public void RefrescarReservas()
    {
        if (tagTeam == null || barrasReserva == null) return;

        HeroCombatData[] heroes = { tagTeam.caelen, tagTeam.illya };

        for (int i = 0; i < barrasReserva.Count; i++)
        {
            Slider barra = barrasReserva[i];
            if (barra == null) continue;

            bool esActivo = (i == tagTeam.ActiveHeroIndex);
            bool visible = !esActivo;

            if (barra.gameObject.activeSelf != visible) barra.gameObject.SetActive(visible);
            if (!visible) continue;

            HeroCombatData heroe = (i < heroes.Length) ? heroes[i] : null;
            if (heroe == null) continue;

            barra.minValue = 0f;
            barra.maxValue = 1f;
            barra.value = (heroe.maxHealth > 0f) ? Mathf.Clamp01(heroe.currentHealth / heroe.maxHealth) : 0f;

            if (etiquetasReserva != null && i < etiquetasReserva.Count && etiquetasReserva[i] != null)
                etiquetasReserva[i].text = heroe.heroName;
        }
    }

    /// <summary>Fija la barra de energía/stamina (0..1). Reservado para el sistema de dash.</summary>
    public void EstablecerStamina(float normalizada)
    {
        staminaObjetivo = Mathf.Clamp01(normalizada);

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value = staminaObjetivo;
        }
    }

    private void RefrescarIdentidad()
    {
        if (tagTeam == null) return;

        HeroCombatData hero = tagTeam.ActiveHero;
        if (hero == null) return;

        PintarEtiqueta();

        if (portraitHero != null)
        {
            portraitHero.color = (hero.element == HeroCombatData.ElementType.Piedra)
                ? colorCaelenPiedra
                : colorIllyaMadera;
        }
    }
}
