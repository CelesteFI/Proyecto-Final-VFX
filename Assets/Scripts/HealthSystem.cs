using System.Collections;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Salud")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Feedback Visual")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    private MeshRenderer[] renderers;
    private Color[] originalColors;
    private Rigidbody rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<MeshRenderer>();

        if (renderers != null && renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce)
    {
        currentHealth -= amount;
        Debug.Log($"💥 [{gameObject.name}] recibió {amount} daño. HP restante: {currentHealth}/{maxHealth}");

        // Retroceso físico
        if (rb != null)
        {
            rb.AddForce(knockbackForce, ForceMode.Impulse);
        }

        // Parpadeo de color
        StartCoroutine(FlashRoutine());

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (renderers == null) yield break;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalColors != null) renderers[i].material.color = originalColors[i];
        }
    }

    private void Die()
    {
        Debug.Log($"💀 [{gameObject.name}] fue derrotado.");
        gameObject.SetActive(false);
    }
}