using UnityEngine;

/// <summary>
/// Cierre del modal de Configuración. El botón Btn_Cerrar_X llama a CerrarModal()
/// desde su evento On Click configurado en el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalConfiguracion : MonoBehaviour
{
    public void CerrarModal() { gameObject.SetActive(false); }
}
