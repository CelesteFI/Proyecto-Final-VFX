using System.Collections;
using UnityEngine;

/// <summary>
/// Ventana modal del HUD. NO construye ni instancia ningún elemento: opera
/// sobre el GameObject 'Modal_*' que ya existe en la Hierarchy, desactivado, y
/// solo lo conmuta con SetActive desde referencias [SerializeField].
/// La animación de zoom usa tiempo no escalado y aplica la pausa compartida
/// (Time.timeScale = 0) entre todos los modales abiertos.
/// El cursor lo arbitra HUDCursorDetector: aquí no se toca Cursor.lockState.
/// </summary>
public class UIModalWindow : MonoBehaviour
{
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

    // Pausa compartida: si hay dos modales abiertos, cerrar uno no debe reanudar
    // el juego mientras el otro siga en pantalla.
    private static int modalesAbiertos = 0;
    private bool pausaAplicada = false;

    private void Awake()
    {
        // Solo se cachean referencias: el estado inicial (desactivado) lo define
        // la Hierarchy, no el código.
        AsegurarReferencias();
    }

    private void AsegurarReferencias()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
        if (estaAbierto) return;

        estaAbierto = true;
        gameObject.SetActive(true);

        // Awake puede no haber corrido si el modal arrancó desactivado.
        AsegurarReferencias();

        StopAllCoroutines();
        AplicarPausa(true);
        StartCoroutine(AnimarVentana(Vector3.zero, Vector3.one, 0f, 1f, true));
    }

    /// <summary>Cierra el modal; al terminar la animación se desactiva el GameObject.</summary>
    public void Cerrar()
    {
        if (!estaAbierto) return;

        estaAbierto = false;
        AsegurarReferencias();
        StopAllCoroutines();
        AplicarPausa(false);
        StartCoroutine(AnimarVentana(Vector3.one, Vector3.zero, 1f, 0f, false));
    }

    /// <summary>Cierra el modal sin animación (útil para derrota o cambio de escena).</summary>
    public void CerrarInmediato()
    {
        estaAbierto = false;
        AsegurarReferencias();
        StopAllCoroutines();
        AplicarPausa(false);

        if (rectTransform != null) rectTransform.localScale = Vector3.zero;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        if (gameObject.activeSelf) gameObject.SetActive(false);
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
        // Salvaguarda: si la ventana desaparece sin cerrarse (recarga de escena,
        // cambio de nivel), libera su contribución para no dejar el juego congelado.
        if (!pausaAplicada) return;

        pausaAplicada = false;
        estaAbierto = false;
        modalesAbiertos = Mathf.Max(0, modalesAbiertos - 1);
        if (modalesAbiertos == 0) Time.timeScale = 1f;
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
