using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Transición de escena del Nivel 1, alojada en el objeto `altar`
/// (`08_Altar_Arbol_Sombrio_BossArena`).
/// Su collider Trigger arranca DESACTIVADO y solo lo habilita <see cref="ActivadorPortal"/>
/// al pulsar 'F' en el tótem del portal (el VFX 'Activar Portal' y la emisión del altar son
/// cosa del tótem). Cuando Caelen entra en él:
///   1. notifica al GameProgressManager que en el Nivel 2 la jugadora activa es Illya y
///      Caelen queda como reserva;
///   2. carga la escena destino del Build Settings.
/// No lanza ningún modal ni pista (la pista ya la dio Illya en el banner), no toca el
/// Time.timeScale salvo para dejarlo limpio y no crea ni destruye objetos.
/// </summary>
[DisallowMultipleComponent]
public class TransicionAltarNivel1 : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Índice de la escena destino en Build Settings. Maqueta Nivel 2 = 1.")]
    [SerializeField] private int indiceEscenaDestino = 1;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool cargando;

    public bool Cargando => cargando;
    public bool JugadorDentro => jugadorDentro;

    private void OnTriggerEnter(Collider otro)
    {
        if (cargando || !isActiveAndEnabled) return;
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        CargarNivel2();
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;
        jugadorDentro = false;
    }

    private void CargarNivel2()
    {
        // En el Nivel 2 la jugadora activa es Illya y Caelen queda como reserva.
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.DesbloquearIllya(true);
            GameProgressManager.Instance.MarcarIllyaAlFrenteEnNivel2();
        }

        if (indiceEscenaDestino < 0 || indiceEscenaDestino >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[PORTAL] Índice de escena {indiceEscenaDestino} fuera del Build Settings; no se carga nada.");
            return;
        }

        cargando = true;
        Debug.Log("[PORTAL] Caelen ha cruzado el altar: se carga la escena " + indiceEscenaDestino + ".");

        // Un modal abierto puede dejar Time.timeScale = 0; la escena nueva debe arrancar limpia.
        UIModalWindow.ReiniciarPausa();

        SceneManager.LoadScene(indiceEscenaDestino);
    }

    private static bool EsJugador(Collider otro)
    {
        if (otro == null) return false;
        if (otro.CompareTag("Player")) return true;
        return otro.GetComponentInParent<PlayerInputReader>() != null;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(int indiceDestino)
    {
        indiceEscenaDestino = indiceDestino;
    }
}
