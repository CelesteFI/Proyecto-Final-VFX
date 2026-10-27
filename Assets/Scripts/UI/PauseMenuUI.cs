using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de pausa físico del HUD. No genera ningún elemento: opera sobre el
/// GameObject 'Modal_Pausa' ya presente en la Hierarchy.
/// Se abre con 'Btn_Pausa' o con Escape, congela el juego (Time.timeScale = 0
/// a través de UIModalWindow) y ofrece Reanudar y Reiniciar Nivel.
/// </summary>
[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [Header("Enlaces")]
    [SerializeField] private UIModalWindow modal;
    [SerializeField] private TextMeshProUGUI textoProgreso;
    [Tooltip("PlayerInputReader del Player. Se asigna en el Inspector.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Banner de Controles")]
    [Tooltip("GameObject 'Banner_Instrucciones'. 'Btn_InfoControles' lo muestra u oculta.")]
    [SerializeField] private GameObject bannerInstrucciones;

    [Header("Textos")]
    [SerializeField] private string formatoProgreso = "Progreso del Nivel: {0}%";
    [SerializeField] private string textoControles =
        "WASD mover · Ratón cámara · Q ataque básico · Clic der. habilidad · Espacio salto · Shift dash · F interactuar · 1 / 2 relevo · Esc pausa";
    [Tooltip("Segundos que el recordatorio de controles permanece en pantalla.")]
    [SerializeField] private float duracionAyuda = 4f;

    private Coroutine rutinaAyuda;

    private void Update()
    {
        if (entrada == null) return;

        if (!entrada.PausaPulsada) return;

        if (modal != null && modal.EstaAbierto) Reanudar();
        else AbrirPausa();
    }

    /// <summary>Abre el modal de pausa refrescando antes el porcentaje de progreso.</summary>
    public void AbrirPausa()
    {
        if (modal == null) return;

        ActualizarProgreso();
        modal.Abrir();
    }

    /// <summary>Cierra el modal y devuelve el juego a velocidad normal.</summary>
    public void Reanudar()
    {
        if (modal != null) modal.Cerrar();
        else Time.timeScale = 1f;
    }

    /// <summary>
    /// Reinicia el nivel desde cero: borra el progreso persistido y recarga la
    /// escena activa. El progreso se resetea a cero, como pide el GDD.
    /// </summary>
    public void ReiniciarNivel()
    {
        GameSaveState.BorrarTodo();

        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ResetearProgreso();

        Time.timeScale = 1f;

        Scene escena = SceneManager.GetActiveScene();
        if (escena.IsValid() && !string.IsNullOrEmpty(escena.name))
            SceneManager.LoadScene(escena.name);
    }

    /// <summary>
    /// Muestra el recordatorio de controles (Btn_InfoControles) durante unos
    /// segundos y lo retira solo: nunca queda un recuadro de controles fijo en
    /// pantalla. El banner pertenece al aviso contextual de HeroInteractor.
    /// </summary>
    public void AlternarAyuda()
    {
        if (bannerInstrucciones == null) return;

        if (rutinaAyuda != null)
        {
            StopCoroutine(rutinaAyuda);
            rutinaAyuda = null;
        }

        if (bannerInstrucciones.activeSelf)
        {
            bannerInstrucciones.SetActive(false);
            return;
        }

        TextMeshProUGUI texto = bannerInstrucciones.GetComponentInChildren<TextMeshProUGUI>(true);
        if (texto != null) texto.text = textoControles;

        bannerInstrucciones.SetActive(true);
        rutinaAyuda = StartCoroutine(AutoOcultarAyuda(duracionAyuda));
    }

    private IEnumerator AutoOcultarAyuda(float segundos)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, segundos));

        if (bannerInstrucciones != null) bannerInstrucciones.SetActive(false);
        rutinaAyuda = null;
    }

    private void ActualizarProgreso()
    {
        if (textoProgreso == null) return;

        int porcentaje = (GameProgressManager.Instance != null)
            ? GameProgressManager.Instance.ProgresoPorcentaje
            : 0;

        textoProgreso.text = string.Format(formatoProgreso, porcentaje);
    }
}
