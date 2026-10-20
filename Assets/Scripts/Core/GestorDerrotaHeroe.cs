using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema de derrota y reaparición del GDD.
/// Cuando Caelen e Illya han caído, el GameObject del Player NO se destruye:
/// se congela la entrada de combate, se muestra "CAÍDO EN COMBATE - Presiona R
/// para reiniciar" y la tecla 'R' (New Input System) reubica al héroe en el
/// punto de inicio con la salud al 100 % y reinicia a los enemigos cercanos.
/// </summary>
[DisallowMultipleComponent]
public class GestorDerrotaHeroe : MonoBehaviour
{
    [Header("Enlaces")]
    [SerializeField] private TagTeamManager tagTeam;
    [SerializeField] private PlayerController controlador;

    [Header("Mensaje de Derrota")]
    [SerializeField] private string mensajeDerrota = "CAÍDO EN COMBATE - Presiona R para reiniciar";
    [SerializeField] private float tamanoFuente = 46f;
    [SerializeField] private Color colorMensaje = new Color(0.95f, 0.32f, 0.28f, 1f);
    [SerializeField] private Color colorPanel = new Color(0.02f, 0.02f, 0.03f, 0.72f);

    [Header("Reinicio de Combate")]
    [Tooltip("Radio en el que se devuelven los enemigos a su guardia al reaparecer.")]
    [SerializeField] private float radioReinicioEnemigos = 80f;

    /// <summary>True mientras la pantalla de derrota está activa.</summary>
    public bool DerrotaActiva => derrotaActiva;

    private PlayerInputReader entrada;
    private RectTransform panelDerrota;
    private TextMeshProUGUI textoDerrota;
    private bool derrotaActiva;

    private void Awake()
    {
        if (tagTeam == null) tagTeam = GetComponent<TagTeamManager>();
        if (controlador == null) controlador = GetComponent<PlayerController>();

        entrada = GetComponent<PlayerInputReader>();
        if (entrada == null) entrada = FindFirstObjectByType<PlayerInputReader>();

        ConstruirInterfazDerrota();
        OcultarDerrota();
    }

    private void Start()
    {
        if (tagTeam == null) tagTeam = FindFirstObjectByType<TagTeamManager>();
        if (tagTeam != null) tagTeam.OnAmbosCaidos += MostrarDerrota;
    }

    private void OnDestroy()
    {
        if (tagTeam != null) tagTeam.OnAmbosCaidos -= MostrarDerrota;
        if (entrada != null) entrada.EntradaBloqueada = false;
    }

    private void Update()
    {
        if (!derrotaActiva) return;
        if (entrada == null || !entrada.ReinicioPulsado) return;

        Reaparecer();
    }

    // ------------------------------------------------------------------
    // DERROTA
    // ------------------------------------------------------------------

    /// <summary>Congela la entrada de combate y muestra el mensaje de derrota.</summary>
    private void MostrarDerrota()
    {
        derrotaActiva = true;

        if (entrada != null) entrada.EntradaBloqueada = true;
        if (panelDerrota != null) panelDerrota.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[DERROTA] Ambos héroes han caído. Pulsa 'R' para reaparecer en el punto de inicio.");
    }

    private void OcultarDerrota()
    {
        derrotaActiva = false;
        if (panelDerrota != null) panelDerrota.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // REAPARICIÓN
    // ------------------------------------------------------------------

    /// <summary>
    /// Devuelve al héroe al punto de inicio con la salud completa y reinicia a
    /// los enemigos que estén dentro del radio de reinicio.
    /// </summary>
    public void Reaparecer()
    {
        OcultarDerrota();

        if (entrada != null) entrada.EntradaBloqueada = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (tagTeam == null) tagTeam = FindFirstObjectByType<TagTeamManager>();

        Vector3 destino = (tagTeam != null) ? tagTeam.puntoReaparicion : Vector3.zero;

        // El teleport debe anular la velocidad acumulada del CharacterController.
        if (controlador != null) controlador.Reubicar(destino);
        else transform.position = destino;

        if (tagTeam != null) tagTeam.Reaparecer();

        ReiniciarEnemigosCercanos(destino);

        Debug.Log($"[DERROTA] Reaparición en {destino} con ambos héroes al 100 % de vida.");
    }

    /// <summary>Devuelve a su guardia a los enemigos dentro del radio indicado.</summary>
    private void ReiniciarEnemigosCercanos(Vector3 centro)
    {
        EnemyAI[] enemigos = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        float radioCuadrado = radioReinicioEnemigos * radioReinicioEnemigos;
        int reiniciados = 0;

        for (int i = 0; i < enemigos.Length; i++)
        {
            if (enemigos[i] == null) continue;
            if ((enemigos[i].transform.position - centro).sqrMagnitude > radioCuadrado) continue;

            enemigos[i].ReiniciarEstado();
            reiniciados++;
        }

        Debug.Log($"[DERROTA] {reiniciados} enemigo(s) devueltos a su zona de guardia.");
    }

    // ------------------------------------------------------------------
    // INTERFAZ
    // ------------------------------------------------------------------

    /// <summary>
    /// Construye el rótulo de derrota en el Canvas del HUD. Se crea desde código
    /// para que ambas maquetas lo hereden sin cableado manual en la escena.
    /// </summary>
    private void ConstruirInterfazDerrota()
    {
        Canvas canvas = null;

        Transform hud = transform.root.Find("Canvas_CombatHUD");
        if (hud != null) canvas = hud.GetComponent<Canvas>();

        if (canvas == null)
        {
            CombatHUDManager hudManager = FindFirstObjectByType<CombatHUDManager>();
            if (hudManager != null) canvas = hudManager.GetComponentInParent<Canvas>();
        }

        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[DERROTA] No hay Canvas en la escena: el mensaje de derrota no podrá mostrarse.");
            return;
        }

        GameObject panel = new GameObject("Panel_Derrota", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);

        panelDerrota = panel.GetComponent<RectTransform>();
        panelDerrota.anchorMin = Vector2.zero;
        panelDerrota.anchorMax = Vector2.one;
        panelDerrota.offsetMin = Vector2.zero;
        panelDerrota.offsetMax = Vector2.zero;

        Image fondo = panel.AddComponent<Image>();
        fondo.color = colorPanel;
        fondo.raycastTarget = false;

        GameObject textoObj = new GameObject("Texto_Derrota", typeof(RectTransform));
        textoObj.transform.SetParent(panel.transform, false);

        RectTransform rectTexto = textoObj.GetComponent<RectTransform>();
        rectTexto.anchorMin = new Vector2(0.1f, 0.5f);
        rectTexto.anchorMax = new Vector2(0.9f, 0.5f);
        rectTexto.offsetMin = new Vector2(0f, -60f);
        rectTexto.offsetMax = new Vector2(0f, 60f);
        rectTexto.anchoredPosition = Vector2.zero;

        textoDerrota = textoObj.AddComponent<TextMeshProUGUI>();
        textoDerrota.text = mensajeDerrota;
        textoDerrota.fontSize = tamanoFuente;
        textoDerrota.fontStyle = FontStyles.Bold;
        textoDerrota.alignment = TextAlignmentOptions.Center;
        textoDerrota.color = colorMensaje;
        textoDerrota.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null) textoDerrota.font = TMP_Settings.defaultFontAsset;
    }
}
