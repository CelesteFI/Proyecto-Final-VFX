using UnityEngine;

/// <summary>
/// Bulbo eólico de Illya: al golpearlo en salto despliega una plataforma temporal
/// de lianas (Cian Translúcido) que permanece 5 segundos. Encadenar los 4 bulbos
/// permite cruzar la sima entre la Arena A y la Isla del Templo.
/// </summary>
[DisallowMultipleComponent]
public class BulboEolico : MonoBehaviour, IGolpeableElemental
{
    [Header("Plataforma Temporal (asset asignado en el Inspector)")]
    [SerializeField] private GameObject prefabPlataforma;
    [Tooltip("Punto donde aparece la plataforma. Si está vacío se usa debajo del bulbo.")]
    [SerializeField] private Transform puntoPlataforma;
    [SerializeField] private float duracion = 5f;
    [SerializeField] private Vector3 desplazamientoPlataforma = new Vector3(0f, -2.2f, 0f);

    [Header("Feedback")]
    [SerializeField] private float escalaPulso = 1.25f;
    [SerializeField] private float duracionPulso = 0.18f;

    public bool Desplegado { get; private set; }

    private Vector3 escalaBase;
    private float temporizadorPulso;

    private void Awake()
    {
        escalaBase = transform.localScale;
    }

    private void Update()
    {
        if (temporizadorPulso <= 0f) return;

        temporizadorPulso -= Time.deltaTime;
        float t = Mathf.Clamp01(temporizadorPulso / duracionPulso);
        transform.localScale = Vector3.Lerp(escalaBase, escalaBase * escalaPulso, t);
        if (temporizadorPulso <= 0f) transform.localScale = escalaBase;
    }

    public void RecibirGolpeElemental(HeroCombatData.ElementType elemento, Vector3 puntoImpacto)
    {
        temporizadorPulso = duracionPulso;
        DesplegarPlataforma();
    }

    /// <summary>Crea la plataforma de lianas con vida limitada.</summary>
    public void DesplegarPlataforma()
    {
        if (prefabPlataforma == null)
        {
            Debug.LogWarning("[BULBO] No hay prefab de plataforma temporal asignado.");
            return;
        }

        Vector3 posicion = (puntoPlataforma != null)
            ? puntoPlataforma.position
            : transform.position + desplazamientoPlataforma;

        GameObject plataforma = Instantiate(prefabPlataforma, posicion, Quaternion.identity);

        TemporarySummon temporal = plataforma.GetComponent<TemporarySummon>();
        if (temporal != null) temporal.lifeTime = duracion;

        Destroy(plataforma, duracion + 1.2f);
        Desplegado = true;

        Debug.Log($"[BULBO] {name}: plataforma de lianas desplegada durante {duracion:F0}s.");
    }

    public void Configurar(GameObject plataforma, Transform punto)
    {
        prefabPlataforma = plataforma;
        puntoPlataforma = punto;
    }
}
