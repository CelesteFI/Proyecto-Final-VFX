using System.Collections;
using UnityEngine;

/// <summary>
/// Disolución de muerte sobre el shader "Custom/DissolveSurface".
/// Al morir la entidad interpola '_DissolveAmount' de 0 a 1 antes de que el
/// HealthComponent desactive el GameObject, de modo que el cadáver se deshace
/// en lugar de desaparecer de golpe.
/// Todo se aplica con MaterialPropertyBlock: no instancia materiales por
/// renderer ni filtra el efecto al resto de enemigos que comparten material.
/// </summary>
[DisallowMultipleComponent]
public class DisolucionMuerte : MonoBehaviour
{
    [Header("Disolución")]
    [SerializeField] private float duracion = 1.05f;
    [SerializeField] private string propiedad = "_DissolveAmount";
    [SerializeField] private float valorFinal = 1f;

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
    }

    private void OnEnable()
    {
        if (salud == null) salud = GetComponent<HealthComponent>();
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
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Rutina());
    }

    private IEnumerator Rutina()
    {
        float total = Mathf.Max(0.05f, duracion);
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;
            Aplicar(Mathf.Lerp(0f, valorFinal, Mathf.Clamp01(t / total)));
            yield return null;
        }

        Aplicar(valorFinal);
        rutina = null;
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
}
