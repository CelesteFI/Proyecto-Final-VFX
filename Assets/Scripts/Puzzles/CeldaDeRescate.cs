using UnityEngine;

/// <summary>
/// Mini-puzle de rescate del Nivel 1: la celda de Illya solo se abre con 'F'
/// cuando el jugador ya recogió la Llave de las Zarzas. Al liberarla se
/// habilita el relevo Tag-Team ('2').
/// </summary>
public class CeldaDeRescate : MonoBehaviour, IInteractuable
{
    [Header("Enlaces")]
    [SerializeField] private PuzzleDoor puerta;
    [SerializeField] private string idLlaveRequerida = "LlaveDeZarzas";
    [SerializeField] private string prompt = "Liberar a Illya";

    [Header("Estado")]
    [SerializeField] private bool liberada;

    public string TextoPrompt => liberada ? "Illya ya está libre" : prompt;
    public bool PuedeInteractuar => !liberada;

    public void Interactuar(Transform actor)
    {
        if (liberada) return;

        bool tieneLlave = GameProgressManager.Instance != null
                          && GameProgressManager.Instance.EstaRecogido(idLlaveRequerida);

        if (!tieneLlave)
        {
            Debug.Log("[RESCATE] La celda está sellada: falta la Llave de las Zarzas.");
            return;
        }

        liberada = true;

        if (puerta != null) puerta.AbrirPermanentemente();

        if (TagTeamManager.Instance != null) TagTeamManager.Instance.DesbloquearIllya(true);

        Debug.Log("[RESCATE] Illya ha sido liberada: el relevo Tag-Team queda habilitado.");
    }

    public void Configurar(PuzzleDoor puertaCelda, string llave, string textoPrompt)
    {
        puerta = puertaCelda;
        idLlaveRequerida = llave;
        prompt = textoPrompt;
    }

    /// <summary>Expuesto para pruebas automáticas.</summary>
    public bool Liberada => liberada;
}
