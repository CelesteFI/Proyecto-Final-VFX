using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Rescate de Illya en `08_Altar_Arbol_Sombrio_BossArena` (Maqueta Nivel 1).
/// Su collider es un Trigger: al entrar el héroe publica «Libera a Illya con el poder
/// de la tierra [F]» en el banner de instrucciones del HUD y, al salir, lo oculta.
/// Una única pulsación de 'F' arranca la secuencia completa:
///   1) reproduce la apertura de la jaula en el componente Legacy `Animation` de `Cage`;
///   2) al terminar la animación desactiva la jaula;
///   3) activa el VFX `Desaparecer_Jaula` y lo apaga pasados 3 s;
///   4) dispara el trigger «Rescate» del Animator de Illya y la gira hacia el héroe.
/// Illya se queda estática en Idle: NO sigue a Caelen en esta escena.
/// No crea ni destruye objetos: jaula, animador y banner ya existen en la escena.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class CeldaDeRescate : MonoBehaviour
{
    [Header("Jaula (`Cage`)")]
    [Tooltip("GameObject de la jaula. Se oculta al terminar su animación de apertura.")]
    [SerializeField] private GameObject jaula;
    [Tooltip("Componente Legacy `Animation` de la jaula.")]
    [SerializeField] private Animation animacionJaula;
    [Tooltip("Clip de apertura embebido en el prefab de la jaula.")]
    [SerializeField] private AnimationClip clipApertura;

    [Header("Illya")]
    [SerializeField] private Transform illya;
    [SerializeField] private Animator animatorIllya;
    [Tooltip("Script de seguimiento de Illya. En esta escena se fuerza a deshabilitado: " +
             "Illya se queda en Idle junto a la jaula, no acompaña al héroe.")]
    [SerializeField] private FollowPlayer seguidor;
    [Tooltip("Héroe al que Illya mira al ser liberada.")]
    [SerializeField] private Transform heroe;
    [SerializeField] private string triggerRescate = "Rescate";

    [Header("VFX de la Jaula")]
    [Tooltip("GameObject 'Desaparecer_Jaula'. Arranca desactivado en la escena.")]
    [SerializeField] private GameObject vfxDesaparecerJaula;
    [Tooltip("Segundos que el VFX permanece activo antes de apagarse.")]
    [SerializeField] private float segundosVfx = 3f;

    [Header("Banner de Instrucciones del HUD")]
    [SerializeField] private GameObject bannerInstrucciones;
    [SerializeField] private TextMeshProUGUI textoInstruccion;
    [SerializeField] private string mensajeEntrada = "Libera a Illya con el poder de la tierra [F]";

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Tiempos de la Cinemática")]
    [Tooltip("Colchón tras el final del clip de apertura antes de ocultar la jaula.")]
    [SerializeField] private float margenFinAnimacion = 0.10f;
    [Tooltip("Tope de seguridad por si el clip no termina de reproducirse.")]
    [SerializeField] private float esperaMaximaAnimacion = 2f;

    [Header("Requisito de Progreso")]
    [Tooltip("Si está activo, el rescate exige el hito del jefe del Nivel 1.")]
    [SerializeField] private bool requiereJefeDerrotado = false;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool liberada;

    public bool Liberada => liberada;
    public bool JugadorDentro => jugadorDentro;
    public string TextoPrompt => liberada ? "Illya ya está libre" : mensajeEntrada;
    public bool PuedeInteractuar => !liberada && (jaula == null || jaula.activeInHierarchy);

    private Coroutine secuencia;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        OcultarBanner();
    }

    private void Update()
    {
        SincronizarPrisionera();

        if (!jugadorDentro || liberada) return;

        // Mientras la jaula siga inactiva (el sello del altar no ha caído) no hay nada que rescatar.
        if (jaula != null && !jaula.activeInHierarchy)
        {
            OcultarBanner();
            return;
        }

        // El aviso se reafirma cada frame para que ningún otro sistema lo pise.
        MostrarBanner();

        if (entrada != null && entrada.InteraccionPulsada) Liberar();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (liberada) return;
        if (jaula != null && !jaula.activeInHierarchy) return;

        MostrarBanner();
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = false;
        OcultarBanner();
    }

    private void OnDisable()
    {
        jugadorDentro = false;
        OcultarBanner();
    }

    /// <summary>
    /// Illya solo existe en escena mientras exista su jaula: ambas aparecen cuando cae el
    /// sello del altar. No crea ni destruye nada, solo conmuta el GameObject ya asignado.
    /// </summary>
    private void SincronizarPrisionera()
    {
        if (illya == null || jaula == null || liberada) return;

        bool debeEstar = jaula.activeInHierarchy;
        if (illya.gameObject.activeSelf != debeEstar) illya.gameObject.SetActive(debeEstar);
    }

    /// <summary>Arranca la secuencia de rescate una única vez.</summary>
    public void Liberar()
    {
        if (liberada) return;

        if (requiereJefeDerrotado)
        {
            bool jefeCaido = GameProgressManager.Instance != null
                             && GameProgressManager.Instance.bossNivel1Derrotado;

            if (!jefeCaido)
            {
                Debug.Log("[RESCATE] La jaula sigue sellada: la bestia del altar respira todavía.");
                return;
            }
        }

        liberada = true;
        jugadorDentro = false;
        OcultarBanner();

        if (secuencia != null) StopCoroutine(secuencia);
        secuencia = StartCoroutine(SecuenciaRescate());
    }

    /// <summary>Compatibilidad con el interactuable histórico (HeroInteractor).</summary>
    public void Interactuar(Transform actor)
    {
        Liberar();
    }

    /// <summary>Marca la celda como ya liberada (restauración de partida guardada).</summary>
    public void MarcarLiberada(bool valor)
    {
        liberada = valor;
    }

    // ------------------------------------------------------------------ secuencia

    private IEnumerator SecuenciaRescate()
    {
        // 1. Animación de apertura en el componente Legacy `Animation` de la jaula.
        yield return StartCoroutine(ReproducirAperturaJaula());

        // 2. La jaula desaparece de la escena.
        if (jaula != null && jaula.activeSelf) jaula.SetActive(false);

        // El hito narrativo del rescate queda en el progreso (no habilita el relevo en esta escena).
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.DesbloquearIllya(true);

        // 3. VFX de desaparición de la jaula: Play() y apagado a los 3 s.
        LanzarVfxJaula();
        if (segundosVfx > 0f) yield return new WaitForSeconds(segundosVfx);
        ApagarVfxJaula();

        // 4. Illya despierta, habla y se queda en Idle mirando al héroe.
        if (illya != null && !illya.gameObject.activeSelf) illya.gameObject.SetActive(true);
        if (animatorIllya != null && !string.IsNullOrEmpty(triggerRescate)) animatorIllya.SetTrigger(triggerRescate);
        MirarAlHeroe();

        // Illya NO es jugable ni acompaña al héroe en esta escena.
        if (seguidor != null) seguidor.enabled = false;

        secuencia = null;
        Debug.Log("[RESCATE] Illya liberada: la jaula se ha abierto y ella espera en Idle junto al altar.");
    }

    /// <summary>Activa el VFX de la jaula y le da Play() (no instancia objetos).</summary>
    private void LanzarVfxJaula()
    {
        if (vfxDesaparecerJaula == null) return;

        vfxDesaparecerJaula.SetActive(true);

        VisualEffect efecto = vfxDesaparecerJaula.GetComponent<VisualEffect>();
        if (efecto != null) efecto.Play();
    }

    private void ApagarVfxJaula()
    {
        if (vfxDesaparecerJaula != null && vfxDesaparecerJaula.activeSelf)
            vfxDesaparecerJaula.SetActive(false);
    }

    private IEnumerator ReproducirAperturaJaula()
    {
        if (animacionJaula == null) yield break;

        AnimationClip clip = clipApertura != null ? clipApertura : animacionJaula.clip;
        if (clip == null) yield break;

        animacionJaula.Stop();
        if (!animacionJaula.Play(clip.name))
        {
            // El clip no estaba en la lista del componente: se registra en memoria (no crea objetos).
            animacionJaula.AddClip(clip, clip.name);
            animacionJaula.Play(clip.name);
        }

        // Se respeta siempre la duración del clip y se da un margen por si no llega a arrancar.
        float inicio = Time.unscaledTime;
        float finMinimo = inicio + clip.length;
        float tope = finMinimo + Mathf.Max(esperaMaximaAnimacion, 0f);

        while (Time.unscaledTime < finMinimo ||
               (animacionJaula != null && animacionJaula.isPlaying && Time.unscaledTime < tope))
        {
            yield return null;
        }

        if (margenFinAnimacion > 0f) yield return new WaitForSeconds(margenFinAnimacion);
    }

    private void MirarAlHeroe()
    {
        if (illya == null || heroe == null) return;

        Vector3 direccion = heroe.position - illya.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;

        illya.rotation = Quaternion.LookRotation(direccion.normalized, Vector3.up);
    }

    // ------------------------------------------------------------------ utilidades

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

    /// <summary>Compatibilidad con el integrador histórico (la jaula sustituye a la puerta).</summary>
    public void Configurar(PuzzleDoor puertaCelda, string llaveRequerida, string textoPrompt)
    {
        if (!string.IsNullOrWhiteSpace(textoPrompt)) mensajeEntrada = textoPrompt;
    }

    /// <summary>Exige el hito de jefe derrotado antes de poder liberar a Illya.</summary>
    public void ConfigurarRequisitoJefe(bool exigir)
    {
        requiereJefeDerrotado = exigir;
    }

    /// <summary>Configura el VFX de la jaula desde el integrador de escena.</summary>
    public void ConfigurarVfx(GameObject vfx, float segundosActivo)
    {
        vfxDesaparecerJaula = vfx;
        if (segundosActivo > 0f) segundosVfx = segundosActivo;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[RESCATE] {name}: el collider debe estar en modo Trigger.", this);
    }
#endif
}
