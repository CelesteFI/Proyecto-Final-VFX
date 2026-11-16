using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Contador de oleada. Vigila la muerte de todos los enemigos asignados en el
/// Inspector y solo cumple su hito cuando el contador de bajas llega a cero.
/// Sustituye los hitos individuales: matar a un solo miembro no marca el hito.
/// </summary>
[DisallowMultipleComponent]
public class GestorOleada : MonoBehaviour
{
    [Header("Miembros de la Oleada")]
    [Tooltip("Asignados en el Inspector. Cada uno debe tener HealthComponent.")]
    [SerializeField] private List<HealthComponent> miembros = new List<HealthComponent>();

    [Header("Hito al Completar")]
    [Tooltip("Solo se marca cuando TODOS los miembros han muerto.")]
    [SerializeField] private HitoJuego hitoAlCompletar = HitoJuego.EnemigoNivel1;

    [Header("Progreso del Nivel (HUD)")]
    [Tooltip("Canvas_HUD del nivel. Recibe SumarProgreso al morir el último enemigo de la arena.")]
    [SerializeField] private Canvas_HUD canvasHUD;
    [Tooltip("Fracción (0..1) que suma a la Barra_Progreso al completarse la arena.")]
    [SerializeField] private float progresoAlCompletar = 0.1f;

    [Header("Consecuencias (inspector)")]
    [Tooltip("Se invoca una sola vez al completarse la oleada.")]
    [SerializeField] private UnityEvent alCompletar = new UnityEvent();

    public int Total => miembros.Count;
    public int Restantes => restantes;
    public bool Completada => completada;

    private int restantes;
    private bool completada;
    private bool suscrito;

    private void Awake()
    {
        restantes = miembros.Count;
    }

    private void OnEnable()
    {
        Suscribir();
        Recalcular();
    }

    private void OnDisable()
    {
        Desuscribir();
    }

    private void Suscribir()
    {
        if (suscrito) return;

        for (int i = 0; i < miembros.Count; i++)
        {
            if (miembros[i] == null) continue;
            miembros[i].OnDied += AlMorirMiembro;
        }

        suscrito = true;
    }

    private void Desuscribir()
    {
        if (!suscrito) return;

        for (int i = 0; i < miembros.Count; i++)
        {
            if (miembros[i] == null) continue;
            miembros[i].OnDied -= AlMorirMiembro;
        }

        suscrito = false;
    }

    /// <summary>Cuenta los miembros que siguen en pie (por si la oleada arranca ya mermada).</summary>
    private void Recalcular()
    {
        // En edición el estado serializado de los HealthComponent todavía no está
        // sincronizado con las instancias: el contador se toma del tamaño de la lista
        // y se recalcula de verdad al entrar en Play Mode.
        if (!Application.isPlaying)
        {
            restantes = miembros.Count;
            return;
        }

        int vivos = 0;

        for (int i = 0; i < miembros.Count; i++)
        {
            if (miembros[i] == null) continue;
            if (!miembros[i].EstaMuerto) vivos++;
        }

        restantes = vivos;

        if (restantes == 0 && miembros.Count > 0) Completar();
    }

    private void AlMorirMiembro()
    {
        if (completada) return;

        if (restantes > 0) restantes--;

        Debug.Log($"[OLEADA] {name}: baja registrada. Quedan {restantes}/{miembros.Count}.");

        if (restantes == 0) Completar();
    }

    /// <summary>Registra el hito y dispara las consecuencias una única vez.</summary>
    private void Completar()
    {
        if (completada) return;
        completada = true;

        GameProgressManager gm = GameProgressManager.Instance;
        if (gm != null) gm.MarcarHito(hitoAlCompletar);

        // Señal de progreso al HUD: una sola vez, cuando murió el último enemigo.
        if (canvasHUD != null) canvasHUD.SumarProgreso(progresoAlCompletar);

        alCompletar?.Invoke();

        Debug.Log($"[OLEADA] {name} completada. Hito {hitoAlCompletar}. Progreso: {gm?.ProgresoPorcentaje ?? 0} %.");
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(List<HealthComponent> nuevosMiembros, HitoJuego nuevoHito)
    {
        Desuscribir();

        miembros = (nuevosMiembros != null) ? nuevosMiembros : new List<HealthComponent>();
        hitoAlCompletar = nuevoHito;

        completada = false;
        Suscribir();
        Recalcular();
    }
}
