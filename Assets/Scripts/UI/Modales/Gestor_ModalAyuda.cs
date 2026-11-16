using UnityEngine;

/// <summary>
/// Cierre del modal de Ayuda. El botón Btn_Cerrar_X llama a CerrarModal()
/// desde su evento On Click configurado en el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalAyuda : MonoBehaviour
{
    public void CerrarModal() { gameObject.SetActive(false); }
}
