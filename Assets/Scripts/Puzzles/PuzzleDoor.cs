using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reja / losa rúnica que bloquea el paso. Se retrae con una animación simple
/// al resolverse un puzzle y desactiva sus colliders de bloqueo.
/// </summary>
public class PuzzleDoor : MonoBehaviour
{
    [Header("Geometría")]
    [Tooltip("Transform que se desplaza al abrirse (la reja visible).")]
    [SerializeField] private Transform reja;
    [SerializeField] private Vector3 desplazamientoApertura = new Vector3(0f, -4f, 0f);
    [SerializeField] private float duracionApertura = 0.8f;

    [Header("Bloqueo Físico")]
    [SerializeField] private Collider[] collidersBloqueo;

    [Header("Eventos")]
    [SerializeField] private UnityEvent alAbrir = new UnityEvent();
    [SerializeField] private UnityEvent alCerrar = new UnityEvent();

    public bool Abierta { get; private set; }

    /// <summary>Avisa a otros sistemas cuando la puerta cambia de estado.</summary>
    public event System.Action<bool> OnEstadoCambiado;

    private Vector3 posicionCerrada;
    private Coroutine rutina;

    private void Awake()
    {
        if (reja == null) reja = transform;
        posicionCerrada = reja.localPosition;
        AplicarBloqueo(true);
    }

    /// <summary>Apertura definitiva (puzzles de recompensa permanente).</summary>
    public void AbrirPermanentemente()
    {
        if (Abierta) return;
        DetenerRutina();
        rutina = StartCoroutine(Animar(true));
    }

    /// <summary>Apertura temporal; vuelve a cerrarse tras el tiempo indicado.</summary>
    public void AbrirTemporal(float segundos)
    {
        DetenerRutina();
        rutina = StartCoroutine(RutinaTemporal(segundos));
    }

    /// <summary>Vuelve a la posición cerrada.</summary>
    public void Cerrar()
    {
        DetenerRutina();
        rutina = StartCoroutine(Animar(false));
    }

    private IEnumerator RutinaTemporal(float segundos)
    {
        yield return Animar(true);
        yield return new WaitForSeconds(segundos);
        yield return Animar(false);
    }

    private IEnumerator Animar(bool abrir)
    {
        Vector3 destino = abrir ? posicionCerrada + desplazamientoApertura : posicionCerrada;
        Vector3 origen = reja.localPosition;

        AplicarBloqueo(!abrir);

        float t = 0f;
        while (t < duracionApertura)
        {
            t += Time.deltaTime;
            reja.localPosition = Vector3.Lerp(origen, destino, Mathf.SmoothStep(0f, 1f, t / duracionApertura));
            yield return null;
        }

        reja.localPosition = destino;
        Abierta = abrir;

        if (abrir) alAbrir?.Invoke();
        else alCerrar?.Invoke();

        OnEstadoCambiado?.Invoke(abrir);

        rutina = null;
    }

    private void AplicarBloqueo(bool bloquear)
    {
        if (collidersBloqueo == null) return;
        for (int i = 0; i < collidersBloqueo.Length; i++)
        {
            if (collidersBloqueo[i] != null) collidersBloqueo[i].enabled = bloquear;
        }
    }

    private void DetenerRutina()
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = null;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(Transform rejaVisible, Collider[] bloqueos)
    {
        reja = rejaVisible;
        collidersBloqueo = bloqueos;
        posicionCerrada = reja != null ? reja.localPosition : transform.localPosition;
        AplicarBloqueo(true);
    }
}
