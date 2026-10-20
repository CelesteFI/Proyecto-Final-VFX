using UnityEngine;

[System.Serializable]
public class HeroCombatData
{
    public enum ElementType { Piedra, Madera }

    [Header("Identidad y Elemento")]
    public string heroName = "Caelen";
    public ElementType element = ElementType.Piedra;
    public Color proxyColor = new Color(0.45f, 0.40f, 0.35f); // Color representativo de la cápsula

    [Header("Estadísticas de Vitalidad")]
    public float maxHealth = 120f;
    public float currentHealth = 120f;

    [Header("Locomoción Específica")]
    public float moveSpeed = 6.8f;
    public float dashSpeed = 16f;

    [Header("Combate Básico (Clic Izquierdo)")]
    public float attackDamage = 25f;
    public float attackRange = 2.2f;
    public float attackCooldown = 0.45f;
    public float knockbackStrength = 8f;

    [Tooltip("Traslación frontal (en metros) que acompaña a cada golpe, conecte o no.")]
    public float attackImpulse = 0.35f;

    [Header("Habilidad Elemental (Clic Derecho / E)")]
    public float skillCooldown = 6.0f;
    [System.NonSerialized] public float skillCooldownTimer = 0f;
}