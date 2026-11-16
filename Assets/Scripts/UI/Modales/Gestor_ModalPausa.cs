using UnityEngine;

/// <summary>
/// Modal de Pausa. Solo cierra el propio modal: la pausa del tiempo la aplica su
/// UIModalWindow al activarse y la libera al apagarse. Btn_Reanudar y Btn_Cerrar_X
/// llaman a CerrarModal() desde su evento On Click del Inspector.
/// El porcentaje de avance ya NO se publica aquí: lo pinta la Barra_Progreso del HUD
/// a través de Canvas_HUD.RefrescarTextoProgreso().
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalPausa : MonoBehaviour
{
    public void CerrarModal() { gameObject.SetActive(false); }
}
