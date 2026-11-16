using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modal de victoria. La pausa la aplica su UIModalWindow al activarse. El botón de
/// reinicio fuerza Time.timeScale = 1 justo antes de recargar la escena.
/// </summary>
public class Gestor_ModalVictoria : MonoBehaviour
{
    public void VolverAJugar()
    {
        UIModalWindow.ReiniciarPausa();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Maqueta Nivel 1");
    }
}
