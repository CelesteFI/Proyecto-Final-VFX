using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

/// <summary>Rol de una placa dentro del circuito de raíces del Nivel 1.</summary>
public enum RolPlacaRaiz { A, B }

/// <summary>
/// Puzzle 1 del Nivel 1 — «El Circuito de Presión y la Falla de Basalto».
/// Orden ESTRICTO con ventana temporal:
///  · Pisar la placa A enciende el canal rúnico (azul brillante) y arranca la cuenta
///    regresiva de <see cref="ventanaSegundos"/> segundos.
///  · Pisar la placa B dentro de esa ventana marca el puzzle como RESUELTO.
///  · Si la ventana expira, o si la B se pisa antes que la A, el circuito se RESETEA:
///    el canal se apaga y hay que empezar de nuevo por la A.
/// Al resolverse envía la señal a la barrera de raíces, que hunde a su hijo `Vines`.
/// No hace búsquedas en runtime: todo llega por referencias del Inspector.
/// </summary>
[DisallowMultipleComponent]
public class PuzzleRaicesCorruptas : MonoBehaviour
{
    private enum Estado { EsperandoA, VentanaAbierta, Resuelto }

    [Header("Temporizador")]
    [Tooltip("Segundos que el canal rúnico permanece encendido tras pisar la placa A.")]
    [SerializeField] private float ventanaSegundos = 5f;

    [Header("Feedback del canal rúnico")]
    [SerializeField] private Renderer canalRunico;
    [SerializeField] private Color colorInactivo = new Color(0.05f, 0.10f, 0.16f, 1f);
    [SerializeField] private Color colorActivo = new Color(0.15f, 0.70f, 1f, 1f);

    [Header("Condición de victoria")]
    [Tooltip("Componente de la barrera de raíces que hunde al hijo 'Vines'.")]
    [SerializeField] private HundimientoRaices hundimiento;

    [Header("VFX de Raíces al resolverse")]
    [Tooltip("GameObject 'Desaparecer_Raices'. Arranca desactivado en la escena.")]
    [SerializeField] private GameObject vfxDesaparecerRaices;
    [Tooltip("Segundos que el VFX permanece activo antes de apagarse.")]
    [SerializeField] private float segundosVfx = 3f;
    [SerializeField] private float duracionSacudidaCamara = 0.28f;
    [SerializeField] private float fuerzaSacudidaCamara = 0.20f;

    [Header("Progreso del Nivel (HUD)")]
    [Tooltip("Canvas_HUD del nivel. Recibe SumarProgreso al resolverse el puzzle.")]
    [SerializeField] private Canvas_HUD canvasHUD;
    [Tooltip("Fracción (0..1) que suma a la Barra_Progreso al resolverse el puzzle.")]
    [SerializeField] private float progresoAlResolver = 0.1f;

    [Header("Eventos")]
    [SerializeField] private UnityEvent alResolver = new UnityEvent();
    [SerializeField] private UnityEvent alResetear = new UnityEvent();

    [Header("Estado (solo lectura)")]
    [SerializeField] private string estadoActual = "EsperandoA";
    [SerializeField] private float tiempoRestante;

    public bool Resuelto => estado == Estado.Resuelto;
    public float TiempoRestante => tiempoRestante;

    private Estado estado = Estado.EsperandoA;
    private MaterialPropertyBlock bloque;
    private int idColorBase;
    private int idEmision;

    private void Awake()
    {
        idColorBase = Shader.PropertyToID("_BaseColor");
        idEmision = Shader.PropertyToID("_EmissionColor");

        PintarCanal(false);
        SincronizarEstadoInspeccion();
    }

    private void Update()
    {
        if (estado != Estado.VentanaAbierta) return;

        tiempoRestante -= Time.deltaTime;
        SincronizarEstadoInspeccion();

        if (tiempoRestante <= 0f) Reiniciar("la ventana de 5 s expiró sin pisar la placa B");
    }

    // ------------------------------------------------------------------ entrada de placas

    /// <summary>La llama una placa cuando el héroe la pisa.</summary>
    public void ReportarPisada(RolPlacaRaiz placa)
    {
        if (estado == Estado.Resuelto) return;

        if (placa == RolPlacaRaiz.A)
        {
            if (estado == Estado.VentanaAbierta) return; // la ventana ya está armada
            AbrirVentana();
            return;
        }

        // Placa B: solo resuelve si la ventana de la placa A sigue viva.
        if (estado == Estado.VentanaAbierta) Resolver();
        else Reiniciar("la placa B se pisó antes que la placa A");
    }

    // ------------------------------------------------------------------ estados

    private void AbrirVentana()
    {
        estado = Estado.VentanaAbierta;
        tiempoRestante = Mathf.Max(0.01f, ventanaSegundos);
        PintarCanal(true);
        SincronizarEstadoInspeccion();

        Debug.Log($"[PUZZLE RAÍCES] Placa A pisada: el canal rúnico se enciende {tiempoRestante:F1} s. Busca la placa B.");
    }

    private void Reiniciar(string motivo)
    {
        estado = Estado.EsperandoA;
        tiempoRestante = 0f;
        PintarCanal(false);
        SincronizarEstadoInspeccion();
        alResetear?.Invoke();

        Debug.Log($"[PUZZLE RAÍCES] Circuito reiniciado ({motivo}). Vuelve a empezar por la placa A.");
    }

    private void Resolver()
    {
        estado = Estado.Resuelto;
        tiempoRestante = 0f;
        PintarCanal(true);
        SincronizarEstadoInspeccion();

        if (hundimiento != null) hundimiento.Hundir();
        else Debug.LogWarning($"[PUZZLE RAÍCES] {name}: no hay barrera asignada; las raíces no se hundirán.");

        LanzarVfxRaices();

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.puzzleRaicesNivel1Resuelto = true;

        // Señal de progreso al HUD: el circuito de raíces queda resuelto (una sola vez).
        if (canvasHUD != null) canvasHUD.SumarProgreso(progresoAlResolver);

        alResolver?.Invoke();

        Debug.Log("[PUZZLE RAÍCES] Placa B pisada dentro de la ventana: el circuito queda resuelto y las raíces ceden.");
    }

    // ------------------------------------------------------------------ VFX de raíces

    /// <summary>
    /// Activa el VFX 'Desaparecer_Raices', le da Play(), sacude la cámara y lo apaga
    /// pasados <see cref="segundosVfx"/> segundos para no gastar recursos.
    /// Solo conmuta un GameObject ya presente en la escena: no instancia nada.
    /// </summary>
    private void LanzarVfxRaices()
    {
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.TriggerScreenShake(duracionSacudidaCamara, fuerzaSacudidaCamara);

        if (vfxDesaparecerRaices == null) return;

        vfxDesaparecerRaices.SetActive(true);

        VisualEffect efecto = vfxDesaparecerRaices.GetComponent<VisualEffect>();
        if (efecto != null) efecto.Play();

        StartCoroutine(ApagarVfxTras(segundosVfx));
    }

    private IEnumerator ApagarVfxTras(float segundos)
    {
        if (segundos > 0f) yield return new WaitForSeconds(segundos);

        if (vfxDesaparecerRaices != null && vfxDesaparecerRaices.activeSelf)
            vfxDesaparecerRaices.SetActive(false);
    }

    /// <summary>Configura el VFX de raíces desde el integrador de escena.</summary>
    public void ConfigurarVfx(GameObject vfx, float segundosActivo)
    {
        vfxDesaparecerRaices = vfx;
        if (segundosActivo > 0f) segundosVfx = segundosActivo;
    }

    // ------------------------------------------------------------------ utilidades

    private void PintarCanal(bool activo)
    {
        if (canalRunico == null) return;

        if (bloque == null) bloque = new MaterialPropertyBlock();
        if (idColorBase == 0) idColorBase = Shader.PropertyToID("_BaseColor");
        if (idEmision == 0) idEmision = Shader.PropertyToID("_EmissionColor");

        canalRunico.GetPropertyBlock(bloque);
        bloque.SetColor(idColorBase, activo ? colorActivo : colorInactivo);
        bloque.SetColor(idEmision, activo ? colorActivo * 1.6f : Color.black);
        canalRunico.SetPropertyBlock(bloque);
    }

    private void SincronizarEstadoInspeccion()
    {
        estadoActual = estado.ToString();
        if (estado != Estado.VentanaAbierta) tiempoRestante = 0f;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(float ventana, Renderer canal, HundimientoRaices barrera)
    {
        ventanaSegundos = ventana;
        canalRunico = canal;
        hundimiento = barrera;
        PintarCanal(false);
    }
}
