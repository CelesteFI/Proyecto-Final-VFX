using TMPro;
using UnityEngine;

/// <summary>
/// Monolito de pista de `05_Puzzle_Raices_Corruptas`.
/// Su collider es un Trigger: al entrar el héroe se publica el aviso
/// "Presiona [F] para ver la pista" en el banner de instrucciones del HUD.
/// Al pulsar 'F' dentro del Trigger se oculta el banner y se abre el `Modal_Pistas`
/// con el lore del circuito. Al salir del Trigger se ocultan ambos y el ciclo
/// vuelve a empezar la próxima vez que se entre.
/// No crea elementos de interfaz: banner y modal ya existen en el Canvas_HUD.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MonolitoPista : MonoBehaviour
{
    [Header("Banner de Instrucciones del HUD")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Presiona [F] para ver la pista";

    [Header("Modal de Pistas del HUD")]
    [SerializeField] private GameObject modalPistas;
    [SerializeField] private TextMeshProUGUI textoPista;
    [TextArea(2, 6)]
    [SerializeField] private string lore =
        "Para purgar las raíces, la resonancia debe fluir en orden: pisa primero la placa rúnica inicial " +
        "y, antes de que su luz se extinga, alcanza la segunda placa para abrir el camino.";

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool pistaAbierta;

    public bool PistaAbierta => pistaAbierta;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        OcultarBanner();
        if (modalPistas != null) modalPistas.SetActive(false);
    }

    private void Update()
    {
        if (!jugadorDentro) return;

        if (!pistaAbierta)
        {
            // El aviso se reafirma cada frame para que ningún otro sistema lo pise.
            MostrarBanner();

            if (entrada != null && entrada.InteraccionPulsada) AbrirPista();
            return;
        }

        // Con el modal abierto la tecla 'F' no vuelve a disparar el aviso.
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (!pistaAbierta) MostrarBanner();
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = false;
        pistaAbierta = false;

        // Limpieza total de la pantalla al salir del monolito.
        OcultarBanner();
        if (modalPistas != null) modalPistas.SetActive(false);
    }

    private void OnDisable()
    {
        jugadorDentro = false;
        pistaAbierta = false;
        OcultarBanner();
        if (modalPistas != null) modalPistas.SetActive(false);
    }

    /// <summary>Abre el modal de pistas con el lore del circuito.</summary>
    public void AbrirPista()
    {
        if (pistaAbierta) return;
        pistaAbierta = true;

        OcultarBanner();

        if (textoPista != null) textoPista.text = lore;
        if (modalPistas != null) modalPistas.SetActive(true);

        Debug.Log("[MONOLITO] Pista abierta: el circuito de raíces exige A y luego B antes de 5 s.");
    }

    private void MostrarBanner()
    {
        if (bannerInstrucciones != null && !bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(true);

        if (textoInstruccion != null && textoInstruccion.text != mensajeEntrada)
            textoInstruccion.text = mensajeEntrada;
    }

    private void OcultarBanner()
    {
        if (bannerInstrucciones != null && bannerInstrucciones.activeSelf)
            bannerInstrucciones.SetActive(false);
    }

    private static bool EsJugador(Collider otro)
    {
        if (otro == null) return false;
        if (otro.CompareTag("Player")) return true;
        return otro.GetComponentInParent<PlayerInputReader>() != null;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(GameObject banner, TextMeshProUGUI txtBanner, string mensaje,
                           GameObject modal, TextMeshProUGUI txtPista, string textoLore,
                           PlayerInputReader input)
    {
        bannerInstrucciones = banner;
        textoInstruccion = txtBanner;
        mensajeEntrada = mensaje;
        modalPistas = modal;
        textoPista = txtPista;
        lore = textoLore;
        entrada = input;
    }
}
