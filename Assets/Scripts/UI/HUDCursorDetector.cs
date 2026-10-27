using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Autoridad del ratón en el HUD. El cursor permanece SIEMPRE libre y visible
/// (CursorLockMode.None): este componente sólo observa y publica si el puntero
/// está sobre la interfaz. Nunca bloquea ni deforma la posición del ratón.
///
/// La rotación de cámara se congela mientras <see cref="PunteroSobreUI"/> es
/// true, de modo que mover el ratón hacia los botones no gira la vista.
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class HUDCursorDetector : MonoBehaviour
{
    /// <summary>True mientras el puntero está sobre una zona de UI o hay un modal abierto.</summary>
    public static bool PunteroSobreUI { get; private set; }

    [Header("Zonas de HUD (margen adicional al hover real)")]
    [Tooltip("Esquinas de menú y modales: Superior Derecha, Superior Izquierda, Inferior Derecha " +
             "y los propios modales. El margen se aplica alrededor de cada una.")]
    [SerializeField] private RectTransform[] zonas;

    [Header("Ajustes")]
    [Tooltip("Padding en píxeles alrededor de cada contenedor y modal del HUD. " +
             "La rotación de cámara se pausa ANTES de que el puntero toque un botón.")]
    [SerializeField] private float margen = 25f;

    private Canvas canvas;
    private Camera camaraUI;
    private readonly Vector3[] esquinas = new Vector3[4];

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camaraUI = canvas.worldCamera;

        AsegurarCursorLibre();
    }

    private void OnDisable()
    {
        PunteroSobreUI = false;
        if (CameraFollow.Instance != null) CameraFollow.Instance.RotacionPausada = false;
    }

    private void Update()
    {
        AsegurarCursorLibre();

        Mouse raton = Mouse.current;

        bool hover = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool enZona = raton != null && EnAlgunaZona(raton.position.ReadValue());

        PunteroSobreUI = hover || enZona || UIModalWindow.HayModalAbierto;

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.RotacionPausada = PunteroSobreUI;
    }

    /// <summary>El cursor no se bloquea nunca: libre y visible en todo momento.</summary>
    private static void AsegurarCursorLibre()
    {
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    /// <summary>Comprueba si un punto de pantalla cae en alguna zona (con margen).</summary>
    private bool EnAlgunaZona(Vector2 pantalla)
    {
        if (zonas == null || zonas.Length == 0) return false;

        for (int i = 0; i < zonas.Length; i++)
        {
            RectTransform zona = zonas[i];
            if (zona == null || !zona.gameObject.activeInHierarchy) continue;

            zona.GetWorldCorners(esquinas);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camaraUI, esquinas[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camaraUI, esquinas[2]);

            Rect rect = Rect.MinMaxRect(
                Mathf.Min(min.x, max.x) - margen,
                Mathf.Min(min.y, max.y) - margen,
                Mathf.Max(min.x, max.x) + margen,
                Mathf.Max(min.y, max.y) + margen);

            if (rect.Contains(pantalla)) return true;
        }

        return false;
    }
}
