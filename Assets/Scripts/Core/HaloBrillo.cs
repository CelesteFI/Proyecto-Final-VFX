using UnityEngine;

/// <summary>
/// Halo de interacción de los objetos de entorno.
/// Vive en el objeto interactuable y conmuta el GameObject hijo
/// 'Halo_Brillo_Runico' (desactivado por defecto en la Hierarchy).
/// Lo enciende y lo apaga HeroInteractor al entrar o salir del radio de 2.5 m.
/// </summary>
[DisallowMultipleComponent]
public class HaloBrillo : MonoBehaviour
{
    [Header("Piezas del Halo")]
    [Tooltip("GameObject hijo con el material emisivo. Desactivado por defecto.")]
    [SerializeField] private GameObject visualHalo;

    [Tooltip("Luz tenue opcional que acompaña al halo.")]
    [SerializeField] private Light luzHalo;

    [Header("Animación")]
    [SerializeField] private float intensidadMaxima = 1.4f;
    [SerializeField] private float velocidad = 4.5f;
    [SerializeField] private float amplitudPulso = 0.25f;

    public bool EstaEncendido => encendido;

    private bool encendido;
    private float intensidadActual;
    private float fase;

    private void Awake()
    {
        if (luzHalo == null && visualHalo != null)
            luzHalo = visualHalo.GetComponentInChildren<Light>(true);

        if (luzHalo != null)
        {
            luzHalo.enabled = false;
            luzHalo.intensity = 0f;
        }
    }

    /// <summary>Enciende o apaga el halo con un fundido suave.</summary>
    public void Encender(bool valor)
    {
        if (encendido == valor) return;
        encendido = valor;

        if (visualHalo != null && visualHalo.activeSelf != valor)
            visualHalo.SetActive(valor);

        if (valor && luzHalo != null) luzHalo.enabled = true;
    }

    private void Update()
    {
        if (luzHalo == null) return;

        float objetivo = encendido ? intensidadMaxima : 0f;
        intensidadActual = Mathf.MoveTowards(intensidadActual, objetivo, velocidad * Time.deltaTime);

        if (encendido)
        {
            fase += Time.deltaTime * 2.2f;
            luzHalo.intensity = intensidadActual + Mathf.Sin(fase) * amplitudPulso;
        }
        else
        {
            luzHalo.intensity = intensidadActual;
            if (intensidadActual <= 0.01f) luzHalo.enabled = false;
        }
    }

    private void OnDisable()
    {
        encendido = false;
        intensidadActual = 0f;

        if (luzHalo != null)
        {
            luzHalo.intensity = 0f;
            luzHalo.enabled = false;
        }
    }
}
