using TMPro;
using UnityEngine;

/// <summary>
/// Cierre y textos del modal de derrota. El título y el cuerpo se escriben al
/// abrirse el panel según el nivel (1 o posterior). El botón Btn_Cerrar_X llama
/// a CerrarModal() desde su evento On Click configurado en el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalDerrota : MonoBehaviour
{
    public bool esNivel1 = false;
    public TMP_Text txtTituloDerrota;
    public TMP_Text txtCuerpoDerrota;

    private void OnEnable()
    {
        if (esNivel1)
        {
            txtTituloDerrota.text = "CAÍDO EN COMBATE";
            txtCuerpoDerrota.text = "Caelen ha caído. La tierra olvida a los débiles.\n\nReaparecer en el Santuario";
        }
        else
        {
            txtTituloDerrota.text = "CAÍDOS EN COMBATE";
            txtCuerpoDerrota.text = "Caelen e Illya han caído.\nEl bosque reclama su juramento.\n\nReaparecer en el Santuario";
        }
    }

    public void CerrarModal() { gameObject.SetActive(false); }
}
