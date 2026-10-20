using System.Collections;
using UnityEngine;

public class UIModalWindow : MonoBehaviour
{
    [Header("Animación de Zoom Elástico")]
    public float openDuration = 0.22f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool estaAbierto = false;

    // Pausa compartida: si hay dos modales abiertos, cerrar uno no debe reanudar
    // el juego mientras el otro siga en pantalla.
    private static int modalesAbiertos = 0;
    private bool pausaAplicada = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CerrarInmediato();
    }

    public void Alternar()
    {
        if (estaAbierto) Cerrar();
        else Abrir();
    }

    public void Abrir()
    {
        if (estaAbierto) return;

        StopAllCoroutines();
        estaAbierto = true;
        gameObject.SetActive(true);
        AplicarPausa(true);
        StartCoroutine(AnimarVentana(Vector3.zero, Vector3.one, 0f, 1f, true));
    }

    public void Cerrar()
    {
        if (!estaAbierto) return;

        estaAbierto = false;
        StopAllCoroutines();
        AplicarPausa(false);
        StartCoroutine(AnimarVentana(Vector3.one, Vector3.zero, 1f, 0f, false));
    }

    /// <summary>
    /// Congela la simulación física y de gameplay mientras el menú modal está
    /// abierto (Time.timeScale = 0) y la reanuda al cerrarlo. La animación del
    /// modal usa tiempo no escalado, así que sigue siendo fluida.
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
        modalesAbiertos = Mathf.Max(0, modalesAbiertos - 1);
        if (modalesAbiertos == 0) Time.timeScale = 1f;
    }

    private void CerrarInmediato()
    {
        estaAbierto = false;
        transform.localScale = Vector3.zero;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator AnimarVentana(Vector3 scaleIni, Vector3 scaleFin, float alphaIni, float alphaFin, bool abriendo)
    {
        canvasGroup.blocksRaycasts = abriendo;

        // Liberar cursor al abrir el menú para interactuar con clics
        if (abriendo)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(t / openDuration);
            float evaluado = zoomCurve.Evaluate(progreso);

            transform.localScale = Vector3.LerpUnclamped(scaleIni, scaleFin, evaluado);
            canvasGroup.alpha = Mathf.Lerp(alphaIni, alphaFin, progreso);
            yield return null;
        }

        transform.localScale = scaleFin;
        canvasGroup.alpha = alphaFin;

        if (!abriendo)
        {
            gameObject.SetActive(false);
        }
    }
}