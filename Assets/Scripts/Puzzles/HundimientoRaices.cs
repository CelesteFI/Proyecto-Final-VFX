using System.Collections;
using UnityEngine;

/// <summary>
/// Hundimiento de la barrera de raíces (<c>Barrera_Raices_Puzle1</c> → hijo <c>Vines</c>).
/// Secuencia al resolverse el puzzle:
///  1. desactiva de inmediato el collider que bloquea el paso,
///  2. interpola la Y hacia abajo durante <see cref="duracion"/> segundos con una
///     sacudida decreciente en X/Z,
///  3. desactiva por completo el objeto de raíces.
/// La distancia de hundimiento y la amplitud de la sacudida se deducen del tamaño
/// real de la geometría, no de valores fijos.
/// </summary>
[DisallowMultipleComponent]
public class HundimientoRaices : MonoBehaviour
{
    [Header("Piezas de la barrera")]
    [Tooltip("Hijo 'Vines' que se hunde.")]
    [SerializeField] private Transform vines;
    [Tooltip("Collider que bloquea el paso (se desactiva al instante).")]
    [SerializeField] private Collider bloqueo;

    [Header("Animación")]
    [SerializeField] private float duracion = 1.5f;
    [Tooltip("0 = se calcula a partir del ancho de la geometría.")]
    [SerializeField] private float amplitudSacudida = 0f;
    [SerializeField] private bool desactivarAlTerminar = true;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool hundido;

    public bool Hundido => hundido;

    private void Awake()
    {
        if (vines == null)
        {
            Transform hijo = transform.Find("Vines");
            if (hijo != null) vines = hijo;
        }

        if (bloqueo == null && vines != null) bloqueo = vines.GetComponent<Collider>();
    }

    /// <summary>Libera el paso y hunde las raíces. Idempotente.</summary>
    public void Hundir()
    {
        if (hundido) return;
        hundido = true;

        // 1. El paso se libera de inmediato, sin esperar a la animación.
        if (bloqueo != null) bloqueo.enabled = false;

        if (vines == null)
        {
            Debug.LogWarning($"[PUZZLE RAÍCES] {name}: no hay hijo 'Vines' asignado.");
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            // La barrera ya fue destruida por otra vía: solo se oculta.
            if (desactivarAlTerminar) vines.gameObject.SetActive(false);
            return;
        }

        StartCoroutine(RutinaHundimiento());
    }

    private IEnumerator RutinaHundimiento()
    {
        Vector3 origen = vines.localPosition;
        float distancia = MedirAltura();
        float amplitud = amplitudSacudida > 0f ? amplitudSacudida : MedirAmplitud();
        float duracionEfectiva = Mathf.Max(0.01f, duracion);

        float t = 0f;
        while (t < duracionEfectiva)
        {
            t += Time.deltaTime;
            float avance = Mathf.Clamp01(t / duracionEfectiva);
            float suave = Mathf.SmoothStep(0f, 1f, avance);
            float decaimiento = 1f - avance;

            Vector3 sacudida = new Vector3(
                Mathf.Sin(avance * 42f) * amplitud * decaimiento,
                0f,
                Mathf.Cos(avance * 55f) * amplitud * decaimiento);

            vines.localPosition = origen + Vector3.down * (distancia * suave) + sacudida;
            yield return null;
        }

        vines.localPosition = origen + Vector3.down * distancia;

        // 3. Las raíces quedan fuera de la escena.
        if (desactivarAlTerminar) vines.gameObject.SetActive(false);

        Debug.Log($"[PUZZLE RAÍCES] Las raíces se han hundido {distancia:F2} m en {duracionEfectiva:F1} s: el paso queda libre.");
    }

    private Bounds MedirBounds()
    {
        var renderers = vines.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(vines.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    /// <summary>Hundimiento = altura real de la geometría de raíces.</summary>
    private float MedirAltura()
    {
        float alto = MedirBounds().size.y;
        return alto > 0.01f ? alto : 1f;
    }

    /// <summary>Sacudida lateral = 1,5 % del ancho menor de la geometría.</summary>
    private float MedirAmplitud()
    {
        Bounds b = MedirBounds();
        float ancho = Mathf.Min(b.size.x, b.size.z);
        return Mathf.Max(0.02f, ancho * 0.015f);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(Transform raices, Collider colliderBloqueo, float duracionHundimiento)
    {
        vines = raices;
        bloqueo = colliderBloqueo;
        duracion = duracionHundimiento;
    }
}
