using TMPro;
using UnityEngine;

/// <summary>
/// Modal de Resonancia: contadores de recursos y fichas del pentagrama.
/// El botón Btn_Cerrar_X y los siete nodos de Rejilla_Nodos llaman a estos
/// métodos desde su evento On Click configurado en el Inspector.
/// </summary>
[DisallowMultipleComponent]
public class Gestor_ModalResonancia : MonoBehaviour
{
    [Header("Recursos y UI")]
    public TMP_Text txtOro;
    public int cantidadOro = 0;
    public RectTransform objetoBrillo;
    public TMP_Text txtDetalleNombre;
    public TMP_Text txtDetalleCosto;
    public TMP_Text txtDetalleDescripcion;

    [Header("Posiciones de los Nodos")]
    public RectTransform btnResonanciaElemental;
    public RectTransform btnMuroBasalto;
    public RectTransform btnPielPiedra;
    public RectTransform btnFortalezaPiedra;
    public RectTransform btnRaicesVoraces;
    public RectTransform btnPulsosSavia;
    public RectTransform btnMantoHojas;

    public void CerrarModal() { gameObject.SetActive(false); }

    public void SumarOro(int cantidad) { cantidadOro += cantidad; txtOro.text = cantidadOro.ToString(); }

    // ------------------------------------------------------------------
    // SELECCIÓN DE NODOS
    // ------------------------------------------------------------------

    public void SeleccionarResonanciaElemental()
    {
        objetoBrillo.position = btnResonanciaElemental.position;

        txtDetalleNombre.text = "Resonancia Elemental";
        txtDetalleCosto.text = "Costo: 1 Cristal de Voto, 1 Esencia Elemental";
        txtDetalleDescripcion.text = "Sincroniza la energía de los héroes, potenciando los ataques combinados del Tag-Team.";
    }

    public void SeleccionarMuroBasalto()
    {
        objetoBrillo.position = btnMuroBasalto.position;

        txtDetalleNombre.text = "Muro de Basalto";
        txtDetalleCosto.text = "Costo: 3 Cristales de Voto";
        txtDetalleDescripcion.text = "Caelen arranca una losa de basalto del suelo y la clava frente a él. Bloquea el paso, sirve de trinchera contra ráfagas y es la única forma de energizar las placas telúricas de presión.";
    }

    public void SeleccionarPielPiedra()
    {
        objetoBrillo.position = btnPielPiedra.position;

        txtDetalleNombre.text = "Piel de Piedra";
        txtDetalleCosto.text = "Costo: 3 Cristales de Voto";
        txtDetalleDescripcion.text = "Endurece la piel de Caelen, reduciendo significativamente el daño físico recibido de ataques cuerpo a cuerpo.";
    }

    public void SeleccionarFortalezaPiedra()
    {
        objetoBrillo.position = btnFortalezaPiedra.position;

        txtDetalleNombre.text = "Fortaleza de Piedra";
        txtDetalleCosto.text = "Costo: 5 Cristales de Voto, 2 Esencias";
        txtDetalleDescripcion.text = "Crea una zona de invulnerabilidad temporal anclada a la tierra que protege a los héroes.";
    }

    public void SeleccionarRaicesVoraces()
    {
        objetoBrillo.position = btnRaicesVoraces.position;

        txtDetalleNombre.text = "Raíces Voraces";
        txtDetalleCosto.text = "Costo: 2 Cristales de Voto";
        txtDetalleDescripcion.text = "Illya invoca raíces del suelo que inmovilizan a los enemigos en un área designada.";
    }

    public void SeleccionarPulsosSavia()
    {
        objetoBrillo.position = btnPulsosSavia.position;

        txtDetalleNombre.text = "Pulsos de Savia";
        txtDetalleCosto.text = "Costo: 3 Cristales de Voto";
        txtDetalleDescripcion.text = "Emite ondas de energía reparadora que regeneran gradualmente la salud de quien se mantenga en el área.";
    }

    public void SeleccionarMantoHojas()
    {
        objetoBrillo.position = btnMantoHojas.position;

        txtDetalleNombre.text = "Manto de Hojas";
        txtDetalleCosto.text = "Costo: 4 Cristales de Voto, 2 Esencias";
        txtDetalleDescripcion.text = "Otorga camuflaje temporal, volviendo a Illya indetectable para patrullas y evadiendo aggro.";
    }
}
