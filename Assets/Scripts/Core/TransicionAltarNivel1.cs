using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Transición de escena del Nivel 1, alojada en el objeto `altar`
/// (`08_Altar_Arbol_Sombrio_BossArena`).
/// Su collider Trigger arranca DESACTIVADO y solo lo habilita <see cref="ActivadorPortal"/>
/// al pulsar 'F' en el interruptor del portal. Cuando Caelen entra en él:
///   1. se oculta el modal de pista;
///   2. espera 2.5 s (corrutina);
///   3. notifica al GameProgressManager que en el Nivel 2 la jugadora activa es Illya
///      y Caelen queda como reserva;
///   4. desactiva el VFX `Activar Portal` y carga la escena destino del Build Settings.
/// No crea ni destruye objetos.
/// </summary>
[DisallowMultipleComponent]
public class TransicionAltarNivel1 : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Índice de la escena destino en Build Settings. Maqueta Nivel 2 = 1.")]
    [SerializeField] private int indiceEscenaDestino = 1;
    [Tooltip("Espera antes de cargar la escena.")]
    [SerializeField] private float esperaSegundos = 2.5f;

    [Header("Piezas de la Escena")]
    [Tooltip("GameObject 'Activar_Portal': se desactiva antes de cargar.")]
    [SerializeField] private GameObject vfxActivarPortal;
    [Tooltip("Modal de pista abierto por el interruptor: se oculta al entrar.")]
    [SerializeField] private GameObject modalPista;

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
        StartCoroutine(CargarNivel2());
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;
        jugadorDentro = false;
    }

    private IEnumerator CargarNivel2()
    {
        cargando = true;

        if (modalPista != null && modalPista.activeSelf) modalPista.SetActive(false);

        Debug.Log("[PORTAL] Caelen ha cruzado el altar: el nexo se abre en 2.5 s.");

        if (esperaSegundos > 0f) yield return new WaitForSeconds(esperaSegundos);

        // En el Nivel 2 la jugadora activa es Illya y Caelen queda como reserva.
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.DesbloquearIllya(true);
            GameProgressManager.Instance.MarcarIllyaAlFrenteEnNivel2();
        }

        if (vfxActivarPortal != null && vfxActivarPortal.activeSelf) vfxActivarPortal.SetActive(false);

        if (indiceEscenaDestino < 0 || indiceEscenaDestino >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[PORTAL] Índice de escena {indiceEscenaDestino} fuera del Build Settings; no se carga nada.");
            cargando = false;
            yield break;
        }

        // Un modal abierto puede dejar Time.timeScale = 0; la escena nueva debe arrancar limpia.
        Time.timeScale = 1f;

        SceneManager.LoadScene(indiceEscenaDestino);
    }

    private static bool EsJugador(Collider otro)
    {
        if (otro == null) return false;
        if (otro.CompareTag("Player")) return true;
        return otro.GetComponentInParent<PlayerInputReader>() != null;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(int indiceDestino, float espera, GameObject vfxPortal, GameObject modal)
    {
        indiceEscenaDestino = indiceDestino;
        if (espera > 0f) esperaSegundos = espera;
        vfxActivarPortal = vfxPortal;
        modalPista = modal;
    }
}
