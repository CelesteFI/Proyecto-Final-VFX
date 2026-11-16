using System.Collections;
using UnityEngine;

/// <summary>
/// Muerte de los enemigos del Nivel 1 en dos tiempos:
///  1. el renderer principal pasa al material de muerte (`Scorpion Death.mat`);
///  2. se interpola '_DissolveAmount' de 0 a 1 durante <see cref="duracion"/> segundos;
///  3. al terminar, el GameObject del enemigo se destruye.
/// Todo se aplica con MaterialPropertyBlock: no se instancia ningún material por
/// renderer ni se filtra el efecto al resto de enemigos que comparten material.
/// Los jefes que deben seguir existiendo en la escena (porque la arena o el gestor de
/// oleadas los referencian) se configuran con <c>destruirAlTerminar = false</c>: en ese
/// caso el material no se sustituye y el HealthComponent los desactiva como siempre.
/// </summary>
[DisallowMultipleComponent]
public class DisolucionMuerte : MonoBehaviour
{
    [Header("Disolución")]
    [SerializeField] private float duracion = 1.5f;
    [SerializeField] private string propiedad = "_DissolveAmount";
    [SerializeField] private float valorFinal = 1f;

    [Header("Material de Muerte y Destrucción")]
    [Tooltip("Material principal que sustituye al del enemigo al morir. Vacío = conserva el suyo.")]
    [SerializeField] private Material materialMuerte;
    [Tooltip("Si está activo, el GameObject del enemigo se destruye al acabar la disolución.")]
    [SerializeField] private bool destruirAlTerminar = false;

    private Renderer[] renderers;
    private MaterialPropertyBlock bloque;
    private int idPropiedad;
    private HealthComponent salud;
    private Coroutine rutina;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        bloque = new MaterialPropertyBlock();
        idPropiedad = Shader.PropertyToID(propiedad);
        salud = GetComponent<HealthComponent>();
        if (salud == null) salud = GetComponentInParent<HealthComponent>();

        // La disolución destruye el objeto cuando termina: la rutina de desactivación
        // del HealthComponent no debe cortarla por la mitad.
        if (destruirAlTerminar && salud != null) salud.ConfigurarDesactivacionAlMorir(false);
    }

    private void OnEnable()
    {
        if (salud == null) salud = GetComponent<HealthComponent>();
        if (salud == null) salud = GetComponentInParent<HealthComponent>();
        if (salud != null) salud.OnDied += Iniciar;
    }

    private void OnDisable()
    {
        if (salud != null) salud.OnDied -= Iniciar;
    }

    /// <summary>Arranca la disolución (idempotente).</summary>
    public void Iniciar()
    {
        if (!isActiveAndEnabled) return;
        if (rutina != null) return;
        rutina = StartCoroutine(Rutina());
    }

    private IEnumerator Rutina()
    {
        // 1. Material de muerte en el renderer principal (y en el resto de la jerarquía).
        SustituirMaterial();

        float total = Mathf.Max(0.05f, duracion);
        float t = 0f;

        Aplicar(0f);

        while (t < total)
        {
            t += Time.deltaTime;
            Aplicar(Mathf.Lerp(0f, valorFinal, Mathf.Clamp01(t / total)));
            yield return null;
        }

        Aplicar(valorFinal);

        // 2. El enemigo sale de la escena cuando la disolución ha terminado.
        if (destruirAlTerminar)
        {
            rutina = null;
            Destroy(gameObject);
            yield break;
        }

        rutina = null;
    }

    /// <summary>
    /// Sustituye el material de todos los renderers por <see cref="materialMuerte"/>.
    /// Solo se ejecuta cuando el enemigo va a desaparecer: los jefes conservan su material.
    /// </summary>
    private void SustituirMaterial()
    {
        if (materialMuerte == null || renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            // Solo las mallas llevan el material de disolución (no VFX ni partículas).
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;

            Material[] materiales = r.sharedMaterials;
            if (materiales == null || materiales.Length == 0) continue;

            for (int j = 0; j < materiales.Length; j++) materiales[j] = materialMuerte;

            r.sharedMaterials = materiales;
        }
    }

    private void Aplicar(float cantidad)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(bloque);
            bloque.SetFloat(idPropiedad, cantidad);
            r.SetPropertyBlock(bloque);
        }
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(float segundos)
    {
        duracion = Mathf.Max(0.05f, segundos);
    }

    /// <summary>Configuración completa desde el integrador de escena.</summary>
    public void ConfigurarMuerte(Material material, float segundos, bool destruir)
    {
        materialMuerte = material;
        duracion = Mathf.Max(0.05f, segundos);
        destruirAlTerminar = destruir;
    }
}
