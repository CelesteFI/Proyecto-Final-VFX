using System.Collections;
using UnityEngine;

/// <summary>
/// Destello blanco de impacto sobre un material compartido sin instanciar
/// materiales por renderer: usa MaterialPropertyBlock, por lo que no rompe
/// el batching ni filtra el color al resto de enemigos que usan el mismo material.
/// Duración estándar del GDD: 0.18 s.
/// </summary>
[DisallowMultipleComponent]
public class DamageFlash : MonoBehaviour
{
    [Header("Destello de Impacto")]
    [SerializeField] private Color colorDestello = Color.white;
    [SerializeField] private float duracionDestello = 0.18f;
    [SerializeField] private string propiedadColor = "_BaseColor";

    [Header("Destello de Shader (_HitFlash)")]
    [Tooltip("Parámetro del shader 'Custom/DissolveSurface'. 0.15 s según el GDD.")]
    [SerializeField] private string propiedadHitFlash = "_HitFlash";
    [SerializeField] private float duracionHitFlash = 0.15f;

    private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    private Renderer[] renderers;
    private MaterialPropertyBlock bloque;
    private int idColor;
    private int idHitFlash;
    private Coroutine rutinaActiva;
    private bool tieneHitFlash;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        bloque = new MaterialPropertyBlock();
        idColor = Shader.PropertyToID(propiedadColor);
        idHitFlash = Shader.PropertyToID(propiedadHitFlash);
        tieneHitFlash = DetectarHitFlash();
    }

    /// <summary>
    /// Solo se escribe '_HitFlash' si el material compartido del enemigo declara
    /// la propiedad: así el componente sigue siendo válido sobre el Lit estándar.
    /// </summary>
    private bool DetectarHitFlash()
    {
        if (renderers == null) return false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material m = renderers[i].sharedMaterial;
            if (m != null && m.HasProperty(idHitFlash)) return true;
        }

        return false;
    }

    /// <summary>Lanza el destello de daño. Si ya había uno activo, lo reinicia.</summary>
    public void Destellar()
    {
        if (!isActiveAndEnabled) return;

        if (rutinaActiva != null) StopCoroutine(rutinaActiva);
        rutinaActiva = StartCoroutine(RutinaDestello());
    }

    private IEnumerator RutinaDestello()
    {
        AplicarColor(colorDestello);
        AplicarHitFlash(1f);

        // El blanco de vértice dura 0.15 s y el tinte de base 0.18 s: se cumple
        // el tiempo del GDD sin alargar el feedback del material.
        float esperaFlash = Mathf.Min(duracionHitFlash, duracionDestello);
        yield return new WaitForSeconds(esperaFlash);
        AplicarHitFlash(0f);

        float resto = duracionDestello - esperaFlash;
        if (resto > 0f) yield return new WaitForSeconds(resto);

        Limpiar();
        rutinaActiva = null;
    }

    /// <summary>Activa o desactiva '_HitFlash' en el material del renderer.</summary>
    private void AplicarHitFlash(float valor)
    {
        if (!tieneHitFlash) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(bloque);
            bloque.SetFloat(idHitFlash, valor);
            r.SetPropertyBlock(bloque);
        }
    }

    private void AplicarColor(Color color)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(bloque);
            bloque.SetColor(idColor, color);
            bloque.SetColor(IdEmision, color);
            r.SetPropertyBlock(bloque);
        }
    }

    private void Limpiar()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].SetPropertyBlock(null);
        }
    }

    private void OnDisable()
    {
        // Evita dejar el enemigo "en blanco" si lo desactivan a mitad del destello.
        Limpiar();
    }
}
