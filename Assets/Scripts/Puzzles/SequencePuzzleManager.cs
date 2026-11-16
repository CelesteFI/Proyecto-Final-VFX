using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor de puzles de secuencia cronometrada.
/// Nivel 1 "Sendero de las Runas Ciegas": Topo -> Ciervo -> Halcón -> Oso (18 s).
/// Nivel 1 "El Eco de las Tres Raíces": 3 corazones de savia (35 s).
/// Si el orden es incorrecto o expira el tiempo, la resonancia se reinicia.
/// </summary>
public class SequencePuzzleManager : MonoBehaviour
{
    [Header("Secuencia Correcta (en orden)")]
    [SerializeField] private List<PuzzleActivator> secuencia = new List<PuzzleActivator>();

    [Header("Temporizador")]
    [SerializeField] private float tiempoLimite = 18f;

    [Header("Recompensa")]
    [SerializeField] private UnityEvent alResolver = new UnityEvent();
    [SerializeField] private UnityEvent alFallar = new UnityEvent();

    public bool Resuelto { get; private set; }
    public int IndiceActual => indiceActual;
    public float TiempoRestante => temporizador;

    private int indiceActual;
    private float temporizador;
    private bool secuenciaActiva;

    private void Start()
    {
        ReiniciarSecuencia();
    }

    private void Update()
    {
        if (!secuenciaActiva || Resuelto) return;

        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            Debug.Log("[PUZZLE] Secuencia expirada: la resonancia rúnica se apaga.");
            alFallar?.Invoke();
            ReiniciarSecuencia();
        }
    }

    /// <summary>Registra el golpe sobre un elemento y valida el orden.</summary>
    public void RegistrarGolpe(PuzzleActivator elemento)
    {
        if (Resuelto || elemento == null || secuencia == null || secuencia.Count == 0) return;

        // Volver a golpear un elemento ya encendido no rompe la secuencia.
        if (elemento.EstaEncendido) return;

        // Si es el primero, arranca la ventana de tiempo.
        if (!secuenciaActiva)
        {
            if (elemento != secuencia[0])
            {
                Fallar("Primer elemento incorrecto");
                return;
            }

            secuenciaActiva = true;
            temporizador = tiempoLimite;
            indiceActual = 0;
        }

        if (elemento != secuencia[indiceActual])
        {
            Fallar($"Orden incorrecto (se esperaba {secuencia[indiceActual].etiqueta})");
            return;
        }

        elemento.Encender(true);
        indiceActual++;

        Debug.Log($"[PUZZLE] {elemento.etiqueta} encendido ({indiceActual}/{secuencia.Count}) · {temporizador:F1}s restantes");

        if (indiceActual >= secuencia.Count) ResolverSecuencia();
    }

    private void ResolverSecuencia()
    {
        Resuelto = true;
        secuenciaActiva = false;
        temporizador = 0f;

        Debug.Log("[PUZZLE] Secuencia resuelta: las losas del puente se elevan.");

        alResolver?.Invoke();
    }

    private void Fallar(string motivo)
    {
        Debug.Log($"[PUZZLE] Secuencia fallida: {motivo}.");
        alFallar?.Invoke();
        ReiniciarSecuencia();
    }

    /// <summary>
    /// Abre la ventana de tiempo sin exigir un primer golpe: el monolito de
    /// sincronización del Nivel 2 llama a este método al interactuar.
    /// </summary>
    public void ArmarSecuencia()
    {
        if (Resuelto) return;

        secuenciaActiva = true;
        temporizador = tiempoLimite;
        indiceActual = 0;

        Debug.Log($"[PUZZLE] Secuencia armada: {tiempoLimite:F0}s para encender los {secuencia.Count} elementos.");
    }

    /// <summary>Apaga todos los elementos y limpia el progreso temporal.</summary>
    public void ReiniciarSecuencia()
    {
        indiceActual = 0;
        temporizador = 0f;
        secuenciaActiva = false;

        if (secuencia == null) return;
        for (int i = 0; i < secuencia.Count; i++)
        {
            if (secuencia[i] != null) secuencia[i].Encender(false);
        }
    }

    /// <summary>Asigna la lista de elementos desde código o desde el integrador de escena.</summary>
    public void Configurar(List<PuzzleActivator> elementos, float limite)
    {
        secuencia = elementos;
        tiempoLimite = limite;
        ReiniciarSecuencia();
    }
}
