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
        StopAllCoroutines();
        gameObject.SetActive(true);
        StartCoroutine(AnimarVentana(Vector3.zero, Vector3.one, 0f, 1f, true));
    }

    public void Cerrar()
    {
        StopAllCoroutines();
        StartCoroutine(AnimarVentana(Vector3.one, Vector3.zero, 1f, 0f, false));
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
        estaAbierto = abriendo;
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