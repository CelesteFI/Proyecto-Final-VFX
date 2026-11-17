using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modal de victoria. La pausa la aplica su UIModalWindow al activarse. El botón de
/// reinicio ('Volver a jugar') es un reinicio TOTAL de la partida: deja todas las
/// variables de progreso a 0/false, manda 0 a la Barra_Progreso del HUD y carga el
/// Nivel 1 con Time.timeScale = 1.
/// </summary>
public class Gestor_ModalVictoria : MonoBehaviour
{
    public void VolverAJugar()
    {
        // El tiempo vuelve a 1 y el contador de modales a 0 antes de recargar, para que la
        // escena nueva jamás arranque congelada (softlock).
        UIModalWindow.ReiniciarPausa();
        Time.timeScale = 1f;

        GameSaveState.BorrarTodo();
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ResetearProgreso();

        // El HUD dueño de este modal deja la barra en el valor inicial del Nivel 1 (0 %).
        Canvas_HUD hud = GetComponentInParent<Canvas_HUD>();
        if (hud != null) hud.AplicarProgresoInicial(GameProgressManager.ProgresoInicialDeNivel(true));

        SceneManager.LoadScene("Maqueta Nivel 1");
    }
}
