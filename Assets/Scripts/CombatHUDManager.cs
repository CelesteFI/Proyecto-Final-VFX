using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class CombatHUDManager : MonoBehaviour
{
    public static CombatHUDManager Instance { get; private set; }

    [Header("Conexión con el Sistema Tag-Team")]
    public TagTeamManager tagTeam;

    [Header("Panel Héroe (Inferior Izquierda)")]
    public Image portraitHero;
    public TextMeshProUGUI textHeroName;
    public Slider healthSlider;
    public Slider healthDelaySlider; // Ghost bar blanca
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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (tagTeam == null)
        {
            tagTeam = FindFirstObjectByType<TagTeamManager>();
        }

        Canvas.ForceUpdateCanvases();
        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>())
            text.ForceMeshUpdate(false, true);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.mKey.wasPressedThisFrame && modalRecursos != null) modalRecursos.Alternar();
            if (kb.kKey.wasPressedThisFrame && modalHabilidades != null) modalHabilidades.Alternar();
        }

        if (tagTeam == null) return;

        HeroCombatData hero = tagTeam.ActiveHero;
        if (hero == null) return;

        // 1. Datos e identidad del Héroe activo
        if (textHeroName != null) textHeroName.text = $"{hero.heroName} [{hero.element}]";
        if (portraitHero != null)
        {
            portraitHero.color = (hero.element == HeroCombatData.ElementType.Piedra)
                ? colorCaelenPiedra
                : colorIllyaMadera;
        }

        // 2. Barra de Salud y Delay Blanco
        float targetFill = Mathf.Clamp01(hero.currentHealth / hero.maxHealth);
        if (healthSlider != null) healthSlider.value = targetFill;

        if (healthDelaySlider != null)
        {
            if (healthDelaySlider.value > targetFill)
            {
                healthDelaySlider.value = Mathf.MoveTowards(healthDelaySlider.value, targetFill, Time.deltaTime * delayBarSpeed);
            }
            else
            {
                healthDelaySlider.value = targetFill;
            }
        }

        // 3. Cooldown Radial sobre el icono de habilidad (E)
        if (iconSkillRadialCooldown != null)
        {
            float fillRatio = (hero.skillCooldown > 0f) ? (hero.skillCooldownTimer / hero.skillCooldown) : 0f;
            iconSkillRadialCooldown.fillAmount = Mathf.Clamp01(fillRatio);

            if (textSkillCooldown != null)
            {
                if (hero.skillCooldownTimer > 0.05f)
                {
                    textSkillCooldown.gameObject.SetActive(true);
                    textSkillCooldown.text = hero.skillCooldownTimer.ToString("F1");
                }
                else
                {
                    textSkillCooldown.gameObject.SetActive(false);
                }
            }
        }
    }
}