using UnityEngine;
using UnityEngine.Events;

/// <summary>Hitos de muerte que alimentan el progreso global.</summary>
public enum HitoJuego
{
    Ninguno,
    EnemigoNivel1,
    JefeNivel1,
    EnemigoNivel2,
    GuardianNivel2,
    AltarNivel2
}

/// <summary>
/// Marca un hito de progreso cuando el <see cref="HealthComponent"/> de este
/// GameObject muere. Sustituye los enganches manuales por inspector: el jefe
/// del Nivel 1 activa 'bossNivel1Derrotado' y el Guardián Celeste del Nivel 2
/// activa 'bossNivel2Derrotado'.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
[DisallowMultipleComponent]
public class HitoMuerte : MonoBehaviour
{
    [Header("Hito")]
    [SerializeField] private HitoJuego hito = HitoJuego.JefeNivel1;

    [Header("Progreso del Nivel (HUD)")]
    [Tooltip("Canvas_HUD del nivel. Recibe SumarProgreso al morir el jefe.")]
    [SerializeField] private Canvas_HUD canvasHUD;
    [Tooltip("Fracción (0..1) que suma a la Barra_Progreso al morir el jefe.")]
    [SerializeField] private float progresoAlCumplirse = 0.1f;

    [Header("Consecuencias (inspector)")]
    [Tooltip("Se invoca una sola vez al cumplirse el hito.")]
    [SerializeField] private UnityEvent alCumplirse = new UnityEvent();

    public bool Cumplido => cumplido;

    private HealthComponent salud;
    private bool cumplido;

    private void Awake()
    {
        salud = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (salud == null) salud = GetComponent<HealthComponent>();
        if (salud != null) salud.OnDied += Cumplir;
    }

    private void OnDisable()
    {
        if (salud != null) salud.OnDied -= Cumplir;
    }

    /// <summary>Registra el hito en GameProgressManager y dispara las consecuencias.</summary>
    public void Cumplir()
    {
        if (cumplido) return;
        cumplido = true;

        GameProgressManager gm = GameProgressManager.Instance;
        if (gm != null) gm.MarcarHito(hito);

        // Señal de progreso al HUD: el jefe del nivel ha caído.
        if (canvasHUD != null) canvasHUD.SumarProgreso(progresoAlCumplirse);

        alCumplirse?.Invoke();
        Debug.Log($"[HITO] {hito} cumplido en '{name}'. Progreso: {gm?.ProgresoPorcentaje ?? 0} %.");
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(HitoJuego nuevoHito)
    {
        hito = nuevoHito;
    }
}
