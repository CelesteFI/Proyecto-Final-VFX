using System.Collections;
using UnityEngine;

/// <summary>
/// Ventana modal del HUD. NO construye ni instancia ningún elemento: opera
/// sobre el GameObject 'Modal_*' que ya existe en la Hierarchy, desactivado, y
/// solo lo conmuta con SetActive desde referencias [SerializeField].
/// La animación de zoom usa SIEMPRE tiempo no escalado (Time.unscaledDeltaTime), de modo
/// que con Time.timeScale = 0 el modal llega igualmente a su tamaño y alfa finales.
///
/// ESCALA DE DISEÑO: la ventana se anima desde 0 hasta la escala con la que está
/// guardada en la escena (p. ej. Modal_Lore = 1.6894), nunca hasta (1,1,1) a ciegas.
///
/// REGLA DE ORO DE LA PAUSA (única autoridad de Time.timeScale en la interfaz):
///  · Al ACTIVARSE la ventana (OnEnable: ya sea con Abrir() o con un SetActive(true)
///    externo) suma 1 al contador compartido, Time.timeScale pasa a 0 y se reproduce el
///    zoom de apertura con el CanvasGroup interactivo (blocksRaycasts = true), así que
///    los botones internos reciben clics también en pausa.
///  · Al DESACTIVARSE (OnDisable: Cerrar(), SetActive(false) de un botón o descarga de
///    escena) resta 1 y, cuando no queda ningún modal con pausa, Time.timeScale vuelve a 1.
///  · Los avisos sin este componente (Banner_Instrucciones, Modal_Pistas) nunca tocan
///    el tiempo. Una ventana con 'pausarJuego' desmarcado tampoco.
/// Ningún otro script de UI debe escribir Time.timeScale (solo los botones de reinicio,
/// que lo fuerzan a 1 justo antes de SceneManager.LoadScene).
/// El cursor lo arbitra HUDCursorDetector: aquí no se toca Cursor.lockState.
/// </summary>
public class UIModalWindow : MonoBehaviour
{
    [Header("Pausa")]
    [Tooltip("Si está marcado, mientras la ventana esté activa Time.timeScale = 0.")]
    [SerializeField] private bool pausarJuego = true;

    [Header("Animación de Zoom Elástico")]
    public float openDuration = 0.22f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>True mientras exista al menos un modal en pantalla.</summary>
    public static bool HayModalAbierto => modalesAbiertos > 0;

    /// <summary>Estado de esta ventana concreta.</summary>
    public bool EstaAbierto => estaAbierto;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool estaAbierto = false;
    private Vector3 escalaDiseno = Vector3.one;
    private bool escalaCapturada = false;

    // Pausa compartida: si hay dos modales abiertos, cerrar uno no debe reanudar
    // el juego mientras el otro siga en pantalla.
    private static int modalesAbiertos = 0;
    private bool pausaAplicada = false;

    /// <summary>
    /// Pone a cero el contador compartido y devuelve el tiempo a 1. Lo llama el HUD al
    /// nacer la escena y los botones de reinicio antes de recargarla: un contador sucio
    /// (por ejemplo tras recargar con un modal abierto) nunca puede dejar el juego congelado.
    /// </summary>
    public static void ReiniciarPausa()
    {
        modalesAbiertos = 0;
        Time.timeScale = 1f;
    }

    private void Awake()
    {
        // Solo se cachean referencias: el estado inicial (desactivado) lo define
        // la Hierarchy, no el código.
        AsegurarReferencias();
    }

    private void OnEnable()
    {
        // Cubre las aperturas con SetActive(true) (p. ej. Modal_Derrota, botones del HUD)
        // y no solo Abrir(): todas pasan por el mismo zoom y la misma pausa.
        AsegurarReferencias();
        IniciarApertura();
    }

    private void AsegurarReferencias()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // La escala de diseño se lee una sola vez, con la ventana en reposo.
        if (!escalaCapturada && rectTransform != null)
        {
            Vector3 escala = rectTransform.localScale;
            bool valida = escala.x > 0.01f && escala.y > 0.01f && escala.z > 0.01f;
            escalaDiseno = valida ? escala : Vector3.one;
            escalaCapturada = true;
        }
    }

    public void Alternar()
    {
        if (estaAbierto) Cerrar();
        else Abrir();
    }

    /// <summary>Abre el modal con SetActive(true) sobre el GameObject existente.</summary>
    public void Abrir()
    {
        if (estaAbierto && gameObject.activeSelf) return;

        if (!gameObject.activeSelf)
        {
            // OnEnable arranca la pausa y el zoom.
            gameObject.SetActive(true);
            return;
        }

        // Reapertura mientras se estaba cerrando (sigue activo, pero ya no estaba abierto).
        AsegurarReferencias();
        IniciarApertura();
    }

    /// <summary>Cierra el modal; al terminar la animación se desactiva el GameObject.</summary>
    public void Cerrar()
    {
        if (!estaAbierto) return;

        estaAbierto = false;
        AsegurarReferencias();
        StopAllCoroutines();
        AplicarPausa(false);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimarVentana(escalaDiseno, Vector3.zero, 1f, 0f, false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>Cierra el modal sin animación (útil para derrota o cambio de escena).</summary>
    public void CerrarInmediato()
    {
        estaAbierto = false;
        AsegurarReferencias();
        StopAllCoroutines();
        AplicarPausa(false);

        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    /// <summary>Marca la ventana como abierta, aplica la pausa y lanza el zoom de apertura.</summary>
    private void IniciarApertura()
    {
        estaAbierto = true;
        StopAllCoroutines();
        if (pausarJuego) AplicarPausa(true);

        // Estado de partida del zoom: invisible, pero ya clicable para que los
        // botones internos respondan desde el primer fotograma.
        if (rectTransform != null) rectTransform.localScale = Vector3.zero;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        StartCoroutine(AnimarVentana(Vector3.zero, escalaDiseno, 0f, 1f, true));
    }

    /// <summary>
    /// Congela la simulación física y de gameplay mientras el menú modal está
    /// abierto (Time.timeScale = 0) y la reanuda al cerrarlo.
    /// </summary>
    private void AplicarPausa(bool pausar)
    {
        if (pausar == pausaAplicada) return;
        pausaAplicada = pausar;

        modalesAbiertos = Mathf.Max(0, modalesAbiertos + (pausar ? 1 : -1));
        Time.timeScale = (modalesAbiertos > 0) ? 0f : 1f;
    }

    private void OnDisable()
    {
        // Cualquier forma de apagar la ventana (Cerrar, SetActive(false) de un botón,
        // recarga de escena, cambio de nivel) libera su contribución a la pausa.
        estaAbierto = false;
        AplicarPausa(false);

        // Deja la ventana en su estado de reposo (escala de diseño, opaca y clicable) para
        // que una reapertura por SetActive(true) nunca herede un estado de cierre.
        if (rectTransform != null && escalaCapturada) rectTransform.localScale = escalaDiseno;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator AnimarVentana(Vector3 scaleIni, Vector3 scaleFin, float alphaIni, float alphaFin, bool abriendo)
    {
        float duracion = Mathf.Max(0.01f, openDuration);
        canvasGroup.blocksRaycasts = abriendo;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(t / duracion);
            float evaluado = zoomCurve.Evaluate(progreso);

            rectTransform.localScale = Vector3.LerpUnclamped(scaleIni, scaleFin, evaluado);
            canvasGroup.alpha = Mathf.Lerp(alphaIni, alphaFin, progreso);
            yield return null;
        }

        rectTransform.localScale = scaleFin;
        canvasGroup.alpha = alphaFin;

        if (!abriendo && gameObject.activeSelf) gameObject.SetActive(false);
    }
}
