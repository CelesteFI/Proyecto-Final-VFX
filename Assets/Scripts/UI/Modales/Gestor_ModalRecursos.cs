using TMPro;
using UnityEngine;

/// <summary>
/// Cierre del modal de Recursos y fichas del Codex. El botón Btn_Cerrar_X y las
/// tres tarjetas de Rejilla_Recursos llaman a estos métodos desde su evento
/// On Click configurado en el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalRecursos : MonoBehaviour
{
    [Header("Iconos del Detalle")]
    public GameObject iconoOro;
    public GameObject iconoCristales;
    public GameObject iconoElemental;

    [Header("Textos del Detalle")]
    public TMP_Text txtDetalleTitulo;
    public TMP_Text txtDetalleCategoria;
    public TMP_Text txtDetalleCuerpo;

    public void CerrarModal() { gameObject.SetActive(false); }

    public void MostrarDetalleOro()
    {
        if (iconoOro != null) iconoOro.SetActive(true);
        if (iconoCristales != null) iconoCristales.SetActive(false);
        if (iconoElemental != null) iconoElemental.SetActive(false);

        txtDetalleTitulo.text = "ORO PURO";
        txtDetalleCategoria.text = "Metal de forja";
        txtDetalleCuerpo.text = "Uso: Se funde en los altares de forja para reforzar el equipo de Caelen y anclar engranajes rúnicos.\n\n\"El oro que no se dobla no sirve para una era que se rompe\". Árbol Sombrío";
    }

    public void MostrarDetalleCristales()
    {
        if (iconoOro != null) iconoOro.SetActive(false);
        if (iconoCristales != null) iconoCristales.SetActive(true);
        if (iconoElemental != null) iconoElemental.SetActive(false);

        txtDetalleTitulo.text = "CRISTALES DE VOTO";
        txtDetalleCategoria.text = "Fragmentos de Resonancia";
        txtDetalleCuerpo.text = "Uso: Formados por devoción ancestral, necesarios para forjar vínculos en los altares y fortalecer a los héroes.\n\n\"La fe de los antiguos pesa más que la espada de los nuevos\".";
    }

    public void MostrarDetalleEsencias()
    {
        if (iconoOro != null) iconoOro.SetActive(false);
        if (iconoCristales != null) iconoCristales.SetActive(false);
        if (iconoElemental != null) iconoElemental.SetActive(true);

        txtDetalleTitulo.text = "ESENCIAS ELEMENTALES";
        txtDetalleCategoria.text = "Energía Primordial";
        txtDetalleCuerpo.text = "Uso: Vestigios del poder de la naturaleza, requeridos para desatar el verdadero potencial del pentagrama.\n\n\"No dominamos los elementos, solo pedimos prestada su ira\".";
    }
}
