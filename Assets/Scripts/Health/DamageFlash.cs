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

    private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    private Renderer[] renderers;
    private MaterialPropertyBlock bloque;
    private int idColor;
    private Coroutine rutinaActiva;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        bloque = new MaterialPropertyBlock();
        idColor = Shader.PropertyToID(propiedadColor);
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
        yield return new WaitForSeconds(duracionDestello);
        Limpiar();
        rutinaActiva = null;
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
