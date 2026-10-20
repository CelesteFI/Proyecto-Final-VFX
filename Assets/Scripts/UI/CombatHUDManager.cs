using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de combate. La vida se actualiza SOLO por eventos del HealthComponent
/// (patrón observador); Update() se reserva para lo que es inherentemente
/// animado: la barra fantasma de daño, el radial de cooldown y las ventanas modales.
/// </summary>
public class CombatHUDManager : MonoBehaviour
{
    public static CombatHUDManager Instance { get; private set; }

    [Header("Conexión con el Sistema Tag-Team")]
    public TagTeamManager tagTeam;

    [Header("Panel Héroe (Inferior Izquierda)")]
    public Image portraitHero;
    public TextMeshProUGUI textHeroName;
    public Slider healthSlider;
    public Slider healthDelaySlider;
    public Slider staminaSlider;
    public float delayBarSpeed = 1.8f;

    [Header("Panel Habilidades (Inferior Derecha)")]
    public Image iconSkillRadialCooldown;
    public TextMeshProUGUI textSkillCooldown;
    public Image iconDashCooldown;

    [Header("Colores Temáticos GDD")]
    public Color colorCaelenPiedra = new Color(0.85f, 0.55f, 0.25f);
    public Color colorIllyaMadera = new Color(0.25f, 0.85f, 0.45f);

    [Header("Ventanas con Zoom")]
    public UIModalWindow modalRecursos;
    public UIModalWindow modalHabilidades;

    private PlayerInputReader entrada;
    private float vidaObjetivo = 1f;
    private bool suscrito;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (tagTeam == null) tagTeam = FindFirstObjectByType<TagTeamManager>();
        entrada = FindFirstObjectByType<PlayerInputReader>();

        SuscribirEventos();
        RefrescarIdentidad();
        RefrescarVida(tagTeam != null && tagTeam.SaludHeroe != null ? tagTeam.SaludHeroe.CurrentHealth : 1f,
                      tagTeam != null && tagTeam.SaludHeroe != null ? tagTeam.SaludHeroe.MaxHealth : 1f);

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

        if (tagTeam.SaludHeroe != null) tagTeam.SaludHeroe.OnHealthChanged += RefrescarVida;
        suscrito = true;
    }

    private void DesuscribirEventos()
    {
        if (tagTeam == null || !suscrito) return;

        tagTeam.OnVidaHeroeCambiada -= RefrescarVida;
        tagTeam.OnHeroeCambiado -= AlCambiarHeroe;

        if (tagTeam.SaludHeroe != null) tagTeam.SaludHeroe.OnHealthChanged -= RefrescarVida;
        suscrito = false;
    }

    private void Update()
    {
        // 1. Ventanas modales desde el lector central de Input System
        if (entrada != null)
        {
            if (entrada.AlternarRecursos && modalRecursos != null) modalRecursos.Alternar();
            if (entrada.AlternarHabilidades && modalHabilidades != null) modalHabilidades.Alternar();
        }

        // 2. Barra fantasma de daño (animación dependiente del tiempo)
        if (healthDelaySlider != null)
        {
            if (healthDelaySlider.value > vidaObjetivo)
                healthDelaySlider.value = Mathf.MoveTowards(healthDelaySlider.value, vidaObjetivo, Time.deltaTime * delayBarSpeed);
            else
                healthDelaySlider.value = vidaObjetivo;
        }

        // 3. Cooldown radial de la habilidad elemental
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
        vidaObjetivo = (maximo > 0f) ? Mathf.Clamp01(actual / maximo) : 0f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = vidaObjetivo;
        }
    }

    /// <summary>Refresca identidad y colores al completar un relevo.</summary>
    private void AlCambiarHeroe(HeroCombatData heroe)
    {
        RefrescarIdentidad();

        // Al cambiar de héroe la barra fantasma se sincroniza de inmediato.
        if (healthDelaySlider != null && tagTeam != null && tagTeam.SaludHeroe != null)
            healthDelaySlider.value = tagTeam.SaludHeroe.Normalizada;
    }

    private void RefrescarIdentidad()
    {
        if (tagTeam == null) return;

        HeroCombatData hero = tagTeam.ActiveHero;
        if (hero == null) return;

        if (textHeroName != null) textHeroName.text = $"{hero.heroName} [{hero.element}]";

        if (portraitHero != null)
        {
            portraitHero.color = (hero.element == HeroCombatData.ElementType.Piedra)
                ? colorCaelenPiedra
                : colorIllyaMadera;
        }
    }
}
