using UnityEngine;
using UnityEngine.InputSystem;

public class TagTeamManager : MonoBehaviour
{
    [Header("Perfiles de Personaje (GDD)")]
    public HeroCombatData caelen = new HeroCombatData { heroName = "Caelen", element = HeroCombatData.ElementType.Piedra, maxHealth = 130f, currentHealth = 130f, moveSpeed = 6.5f, attackDamage = 30f };
    public HeroCombatData illya = new HeroCombatData { heroName = "Illya", element = HeroCombatData.ElementType.Madera, maxHealth = 95f, currentHealth = 95f, moveSpeed = 7.6f, attackDamage = 20f };

    [Header("Estado")]
    public int activeHeroIndex = 0; // 0 = Caelen, 1 = Illya
    public HeroCombatData ActiveHero => (activeHeroIndex == 0) ? caelen : illya;

    [Header("Puntos de Ataque e Hitbox")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Prefabs de VFX (Opcional por ahora)")]
    public GameObject vfxSwapPrefab;
    public GameObject vfxRockWallPrefab;
    public GameObject vfxVinesAoEPrefab;

    private MeshRenderer proxyRenderer;
    private float attackTimer = 0f;

    void Start()
    {
        proxyRenderer = GetComponentInChildren<MeshRenderer>();
        ActualizarVisualProxy();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null) return;

        // 1. Cambio de personaje (Tag-Team)
        if (kb.digit1Key.wasPressedThisFrame && activeHeroIndex != 0) CambiarHeroe(0);
        if (kb.digit2Key.wasPressedThisFrame && activeHeroIndex != 1) CambiarHeroe(1);

        // 2. Cooldowns
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (caelen.skillCooldownTimer > 0f) caelen.skillCooldownTimer -= Time.deltaTime;
        if (illya.skillCooldownTimer > 0f) illya.skillCooldownTimer -= Time.deltaTime;

        // 3. Ataque básico (Clic Izquierdo)
        if (Cursor.lockState == CursorLockMode.Locked && mouse != null && mouse.leftButton.wasPressedThisFrame && attackTimer <= 0f)
        {
            EjecutarAtaqueBasico();
        }

        // 4. Habilidad elemental (Clic Derecho o E)
        if (Cursor.lockState == CursorLockMode.Locked && ((mouse != null && mouse.rightButton.wasPressedThisFrame) || kb.eKey.wasPressedThisFrame))
        {
            EjecutarHabilidadElemental();
        }
    }

    public void CambiarHeroe(int nuevoIndice)
    {
        activeHeroIndex = nuevoIndice;
        ActualizarVisualProxy();
        Debug.Log($"🔄 [TAG-TEAM]: Relevo a {ActiveHero.heroName} ({ActiveHero.element}). Vida: {ActiveHero.currentHealth}/{ActiveHero.maxHealth}");

        // Sacudida sutil y spawn de partículas de relevo
        if (CameraFollow.Instance != null) CameraFollow.Instance.TriggerScreenShake(0.12f, 0.15f);
        if (vfxSwapPrefab != null) Instantiate(vfxSwapPrefab, transform.position, Quaternion.identity);
    }

    private void ActualizarVisualProxy()
    {
        if (proxyRenderer != null)
        {
            proxyRenderer.material.color = ActiveHero.proxyColor;
        }
    }

    private void EjecutarAtaqueBasico()
    {
        attackTimer = ActiveHero.attackCooldown;
        Vector3 center = (attackPoint != null) ? attackPoint.position : transform.position + transform.forward * 1.2f;

        // Hitbox esférica por código
        Collider[] hits = Physics.OverlapSphere(center, ActiveHero.attackRange, enemyLayers);

        if (hits.Length > 0)
        {
            foreach (var h in hits)
            {
                IDamageable victim = h.GetComponent<IDamageable>();
                if (victim != null)
                {
                    Vector3 knockDir = (h.transform.position - transform.position).normalized + Vector3.up * 0.25f;
                    victim.TakeDamage(ActiveHero.attackDamage, h.transform.position, knockDir * ActiveHero.knockbackStrength);
                }

                // Justo después de hacer el daño a la víctima en TagTeamManager.cs:
                GameObject prefabImpacto = (ActiveHero.element == HeroCombatData.ElementType.Piedra)
                    ? Resources.Load<GameObject>("VFX/VFX_Impacto_Piedra")
                    : Resources.Load<GameObject>("VFX/VFX_Impacto_Madera");

                if (prefabImpacto != null)
                {
                    Instantiate(prefabImpacto, h.transform.position + Vector3.up * 1f, Quaternion.identity);
                }
            }

            // Game juice: Hitstop y Screen Shake al conectar
            HitstopManager.Instance?.TriggerHitstop(0.04f);
            CameraFollow.Instance?.TriggerScreenShake(0.15f, 0.25f);
            Debug.Log($"⚔️ [{ActiveHero.heroName}]: Impactó {hits.Length} objetivo(s) por {ActiveHero.attackDamage} de daño.");
        }
        else
        {
            Debug.Log($"⚔️ [{ActiveHero.heroName}]: Ataque al aire.");
        }

    }

    private void EjecutarHabilidadElemental()
    {
        if (ActiveHero.skillCooldownTimer > 0f)
        {
            Debug.Log($"⏳ Habilidad en recarga: {ActiveHero.skillCooldownTimer:F1}s restantes.");
            return;
        }

        ActiveHero.skillCooldownTimer = ActiveHero.skillCooldown;

        if (ActiveHero.element == HeroCombatData.ElementType.Piedra)
        {
            Debug.Log("🛡️ [CAELEN - PIEDRA]: ¡Muralla de Basalto invocada!");
            CameraFollow.Instance?.TriggerScreenShake(0.2f, 0.35f);
            // Instanciar muro de piedra frente a él si el prefab existe
            if (vfxRockWallPrefab != null)
            {
                Instantiate(vfxRockWallPrefab, transform.position + transform.forward * 2.5f, transform.rotation);
            }
        }
        else
        {
            Debug.Log("🌿 [ILLYA - MADERA]: ¡Brote de Raíces Voraces en área!");
            CameraFollow.Instance?.TriggerScreenShake(0.18f, 0.2f);
            // Instanciar área botánica en el suelo si el prefab existe
            if (vfxVinesAoEPrefab != null)
            {
                Instantiate(vfxVinesAoEPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = (attackPoint != null) ? attackPoint.position : transform.position + transform.forward * 1.2f;
        float r = (ActiveHero != null) ? ActiveHero.attackRange : 2.0f;
        Gizmos.DrawWireSphere(center, r);
    }
}