using UnityEngine;
using UnityEngine.InputSystem;

public class PersistentPuzzleObstacle : MonoBehaviour
{
    public enum TipoPuzzle
    {
        RaicesNivel1,
        AtajoNivel1,
        TemploNivel2
    }

    [Header("Configuración del Obstáculo")]
    public TipoPuzzle tipoObstaculo = TipoPuzzle.RaicesNivel1;
    public GameObject objetoColisionADesactivar;
    public ParticleSystem vfxAlDesbloquear;

    private bool jugadorCerca = false;

    void Start()
    {
        // Si el GameManager existe y el obstáculo ya fue resuelto previamente, se apaga de inmediato
        if (GameProgressManager.Instance != null)
        {
            if (tipoObstaculo == TipoPuzzle.RaicesNivel1 && GameProgressManager.Instance.puzzleRaicesNivel1Resuelto)
            {
                DesactivarSinEfecto();
            }
            else if (tipoObstaculo == TipoPuzzle.AtajoNivel1 && GameProgressManager.Instance.atajoSpawnNivel1Abierto)
            {
                DesactivarSinEfecto();
            }
            else if (tipoObstaculo == TipoPuzzle.TemploNivel2 && GameProgressManager.Instance.puzzleTemploNivel2Resuelto)
            {
                DesactivarSinEfecto();
            }
        }
    }

    void Update()
    {
        if (!jugadorCerca) return;

        // Presionar la tecla física F
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame)
        {
            IntentarInteraccion();
        }
    }

    private void IntentarInteraccion()
    {
        if (tipoObstaculo == TipoPuzzle.RaicesNivel1 || tipoObstaculo == TipoPuzzle.AtajoNivel1)
        {
            // Requiere que Illya (Madera) esté desbloqueada en el progreso
            if (GameProgressManager.Instance != null && !GameProgressManager.Instance.illyaDesbloqueada)
            {
                Debug.Log("🔒 [BLOQUEADO]: Este paso requiere la afinidad de Madera de Illya.");
                return;
            }

            Debug.Log("🌿 [DESPEJADO]: Las raíces responden al poder de Madera y liberan el camino.");
            if (GameProgressManager.Instance != null)
            {
                if (tipoObstaculo == TipoPuzzle.RaicesNivel1) GameProgressManager.Instance.puzzleRaicesNivel1Resuelto = true;
                if (tipoObstaculo == TipoPuzzle.AtajoNivel1) GameProgressManager.Instance.atajoSpawnNivel1Abierto = true;
            }
        }
        else if (tipoObstaculo == TipoPuzzle.TemploNivel2)
        {
            Debug.Log("💨 [DESPEJADO]: Mecanismo del Templo del Viento activado.");
            if (GameProgressManager.Instance != null) GameProgressManager.Instance.puzzleTemploNivel2Resuelto = true;
        }

        if (vfxAlDesbloquear != null) vfxAlDesbloquear.Play();
        if (objetoColisionADesactivar != null) objetoColisionADesactivar.SetActive(false);
        gameObject.SetActive(false);
    }

    private void DesactivarSinEfecto()
    {
        if (objetoColisionADesactivar != null) objetoColisionADesactivar.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            jugadorCerca = false;
        }
    }
}