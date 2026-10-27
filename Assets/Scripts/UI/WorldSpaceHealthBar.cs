using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida flotante en World Space sobre la cabeza del enemigo.
/// Se suscribe al HealthComponent (patrón observador): no consulta la vida en Update.
/// Muestra el nombre del arquetipo y se oculta al morir la entidad.
/// </summary>
public class WorldSpaceHealthBar : MonoBehaviour
{
    [Header("Enlace de Datos")]
    [SerializeField] private HealthComponent salud;

    [Header("Referencias de UI")]
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private Slider barraVida;
    [SerializeField] private Image relleno;
    [Tooltip("Image 'Fondo' de la barra. Se asigna en el Inspector.")]
    [SerializeField] private Image fondoBarra;

    [Header("Colores de Arquetipo")]
    [SerializeField] private Color colorRelleno = new Color(0.78f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color colorFondo = new Color(0.05f, 0.05f, 0.07f, 0.85f);

    [Header("Etiqueta")]
    [SerializeField] private string nombreArquetipo = "Fauna Corrupta";

    public HealthComponent Salud => salud;

    private void Awake()
    {
        if (salud == null) salud = GetComponentInParent<HealthComponent>();
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (textoNombre != null) textoNombre.text = nombreArquetipo;
        if (relleno != null) relleno.color = colorRelleno;
        if (fondoBarra != null) fondoBarra.color = colorFondo;
    }

    private void OnEnable()
    {
        if (salud == null) return;
        salud.OnHealthChanged += RefrescarVida;
        salud.OnDied += OcultarAlMorir;

        RefrescarVida(salud.CurrentHealth, salud.MaxHealth);
    }

    private void OnDisable()
    {
        if (salud == null) return;
        salud.OnHealthChanged -= RefrescarVida;
        salud.OnDied -= OcultarAlMorir;
    }

    /// <summary>Configura el nombre visible del arquetipo (usado por el integrador de escena).</summary>
    public void Configurar(string etiqueta, HealthComponent componente)
    {
        nombreArquetipo = etiqueta;
        salud = componente;
        if (textoNombre != null) textoNombre.text = etiqueta;
        if (salud != null) RefrescarVida(salud.CurrentHealth, salud.MaxHealth);
    }

    private void RefrescarVida(float actual, float maximo)
    {
        if (barraVida != null)
        {
            barraVida.minValue = 0f;
            barraVida.maxValue = Mathf.Max(1f, maximo);
            barraVida.value = actual;
        }

        if (grupo != null) grupo.alpha = 1f;
    }

    private void OcultarAlMorir()
    {
        if (grupo != null) grupo.alpha = 0f;
        gameObject.SetActive(false);
    }
}
