using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor único del HUD de combate (Nivel 2). No construye ni reorganiza nada:
/// todas las referencias apuntan a GameObjects que ya existen en la jerarquía
/// del Canvas_HUD y aquí solo se escribe en sus Slider, Image y TextMeshPro.
///
/// Responsabilidades:
///   1. Nombre del héroe activo y barra de vida del héroe que está en combate.
///   2. Pintar los cooldowns radiales (Dash, Q, Especial) con los datos REALES que
///      le envía PlayerCombat: este HUD no calcula ni adivina ningún tiempo.
///   3. Símbolos centrales del Pentagrama sincronizados con el héroe activo
///      y tinte del ícono satélite de Illya según el nivel.
///   4. Barra de progreso del nivel: los hitos la alimentan con SumarProgreso.
/// </summary>
[DisallowMultipleComponent]
public class Canvas_HUD : MonoBehaviour
{
    [Header("Configuración de Nivel")]
    [Tooltip("Nivel 1: Illya aún está sellada y su ícono satélite se muestra gris.")]
    public bool esNivel1 = false;

    [Header("Enlace con el Tag-Team")]
    [Tooltip("Gestor que valida el relevo con 'Tab'.")]
    public TagTeamManager tagTeam;

    [Header("Nombre y Barras de Vida")]
    public TextMeshProUGUI txtHeroName;
    public Slider barraVidaCaelen;
    public Slider barraVidaIllya;

    [Header("Cooldowns Radiales (los pinta PlayerCombat)")]
    [Tooltip("Image radial de la habilidad Especial (Clic Derecho).")]
    public Image cooldownEspecial;
    [Tooltip("Image radial del Dash (Shift).")]
    public Image cooldownDash;
    [Tooltip("Image radial del ataque básico (Q). Opcional: si queda vacío, el dato se ignora.")]
    public Image cooldownAtaqueQ;

    [Header("Pentagrama · Símbolos Centrales")]
    public GameObject iconoCentralCaelen;
    public GameObject iconoCentralIllya;

    [Header("Pentagrama · Satélite de Illya")]
    [Tooltip("Image 'Icono Illya' dentro de Slot_2_Elemento2. Nunca se desactiva: solo se tiñe.")]
    public Image iconoIllyaSlot2;

    [Header("Modales")]
    public GameObject Modal_Resonancia;
    public GameObject Modal_Recursos;
    public GameObject Modal_Pausa;
    public GameObject Modal_Ayuda;
    public GameObject Modal_Configuracion;

    [Header("Avisos del Héroe (los escribe el Player)")]
    [Tooltip("GameObject 'Modal_Derrota'. Debe estar desactivado en la Hierarchy.")]
    public GameObject Modal_Derrota;
    [Tooltip("Texto que recibe el mensaje contextual de derrota.")]
    public TextMeshProUGUI txtDerrota;
    [Tooltip("GameObject 'Banner_Instrucciones'. Oculto por defecto en la Hierarchy.")]
    public GameObject Banner_Instrucciones;
    [Tooltip("Texto 'Txt_Instruccion' del banner contextual.")]
    public TextMeshProUGUI txtBannerInstrucciones;

    [Header("Progreso")]
    [Tooltip("Slider 'Barra_Progreso' dentro de MenuBasicos.")]
    public Slider Barra_Progreso;
    [Tooltip("Texto 'Txt_Progreso' de la barra: muestra el porcentaje que suman los hitos.")]
    public TextMeshProUGUI txtProgresoBarra;

    [Header("Auto-tutorial (solo Nivel 1)")]
    [Tooltip("Abre Modal_Ayuda por sí solo al empezar el Nivel 1 (congela el tiempo por la regla de oro de los modales).")]
    [SerializeField] private bool abrirAyudaAlIniciar = true;
    [Tooltip("Segundos reales de espera antes de abrir la ayuda.")]
    [SerializeField] private float retrasoAyudaInicial = 2f;

    [Header("Tinte de Bloqueo (solo Nivel 1)")]
    public Color tinteBloqueado = new Color(0.22f, 0.22f, 0.22f, 1f);

    private HeroCombatData HeroeActivo => (tagTeam != null) ? tagTeam.ActiveHero : null;

    private bool HeroeActivoEsCaelen => tagTeam == null || tagTeam.ActiveHeroIndex == 0;

    private void Awake()
    {
        // El contador de pausa es estático y sobrevive a la recarga de escena: se limpia
        // aquí, antes de que ningún modal pueda activarse, para no arrancar congelado.
        UIModalWindow.ReiniciarPausa();

        if (tagTeam == null) tagTeam = Object.FindFirstObjectByType<TagTeamManager>();
    }

    private void Start()
    {
        if (tagTeam != null)
        {
            tagTeam.OnHeroeCambiado += AlCambiarHeroe;
            tagTeam.OnVidaHeroeCambiada += ActualizarVidaHeroeActivo;
        }

        AplicarTinteNivel();

        // Radiales de recarga: parten en Fill Amount 0 (habilidad lista, sin oscurecer). Al usar la
        // habilidad PlayerCombat reporta progreso 0 y el radial salta a 1 (icono oscurecido); luego
        // desciende gradualmente a 0 conforme avanza la recarga real.
        ActualizarCooldowns(1f, 1f, 1f);

        // Progreso inicial del nivel: 0 % en el Nivel 1, 50 % en el Nivel 2.
        InicializarBarraDeProgreso();

        ActualizarHeroeHUD(HeroeActivoEsCaelen);

        if (esNivel1 && abrirAyudaAlIniciar) StartCoroutine(RutinaAyudaInicial());
    }

    /// <summary>
    /// Auto-tutorial express: pasados 2 s REALES desde el inicio del Nivel 1 abre Modal_Ayuda.
    /// La pausa la aplica UIModalWindow (el modal se abre con su Abrir()). No se abre si
    /// ya hay otro modal en pantalla o si el héroe ya cayó.
    /// </summary>
    private IEnumerator RutinaAyudaInicial()
    {
        yield return new WaitForSecondsRealtime(retrasoAyudaInicial);

        if (UIModalWindow.HayModalAbierto) yield break;
        if (tagTeam != null && tagTeam.DerrotaActiva) yield break;

        AbrirModalAyuda();
    }

    private void OnDestroy()
    {
        if (tagTeam != null)
        {
            tagTeam.OnHeroeCambiado -= AlCambiarHeroe;
            tagTeam.OnVidaHeroeCambiada -= ActualizarVidaHeroeActivo;
        }
    }

    // ------------------------------------------------------------------
    // HÉROE ACTIVO
    // ------------------------------------------------------------------

    /// <summary>
    /// Adaptador del evento OnHeroeCambiado, que solo se publica cuando el
    /// TagTeamManager valida el relevo (nunca por pulsar 'Tab' a un héroe sellado).
    /// </summary>
    private void AlCambiarHeroe(HeroCombatData heroe)
    {
        ActualizarHeroeHUD(heroe != null ? heroe.element == HeroCombatData.ElementType.Piedra : HeroeActivoEsCaelen);
    }

    /// <summary>
    /// Repinta el nombre, conmuta las barras de vida y los símbolos centrales
    /// del Pentagrama. Se invoca SOLO cuando el TagTeamManager confirma el
    /// relevo (evento OnHeroeCambiado), nunca al pulsar la tecla.
    /// </summary>
    public void ActualizarHeroeHUD(bool esCaelen)
    {
        if (txtHeroName != null)
        {
            HeroCombatData heroe = HeroeActivo;
            txtHeroName.text = (heroe != null)
                ? heroe.heroName
                : (esCaelen ? "Caelen" : "Illya");
        }

        if (barraVidaCaelen != null && barraVidaCaelen.gameObject.activeSelf != esCaelen)
            barraVidaCaelen.gameObject.SetActive(esCaelen);

        if (barraVidaIllya != null && barraVidaIllya.gameObject.activeSelf == esCaelen)
            barraVidaIllya.gameObject.SetActive(!esCaelen);

        if (iconoCentralCaelen != null && iconoCentralCaelen.activeSelf != esCaelen)
            iconoCentralCaelen.SetActive(esCaelen);

        if (iconoCentralIllya != null && iconoCentralIllya.activeSelf == esCaelen)
            iconoCentralIllya.SetActive(!esCaelen);

        RefrescarVidaHeroeActivo();
    }

    /// <summary>Escribe en la barra activa la vida actual del héroe que está en combate.</summary>
    public void RefrescarVidaHeroeActivo()
    {
        HeroCombatData heroe = HeroeActivo;
        if (heroe == null) return;

        ActualizarVidaHeroeActivo(heroe.currentHealth, heroe.maxHealth);
    }

    /// <summary>
    /// Actualiza el Slider del héroe activo con los valores que publica el
    /// HealthComponent. Suscrito a OnVidaHeroeCambiada (daño y curación).
    /// </summary>
    public void ActualizarVidaHeroeActivo(float actual, float maximo)
    {
        Slider barra = HeroeActivoEsCaelen ? barraVidaCaelen : barraVidaIllya;
        if (barra == null) return;

        barra.minValue = 0f;
        barra.maxValue = 1f;
        barra.value = (maximo > 0f) ? Mathf.Clamp01(actual / maximo) : 0f;
    }

    // ------------------------------------------------------------------
    // COOLDOWNS RADIALES (datos reales enviados por PlayerCombat)
    // ------------------------------------------------------------------

    /// <summary>
    /// Pinta los tres radiales. Cada parámetro es el progreso REAL de la recarga que envía
    /// PlayerCombat (1 = lista, 0 = recién usada). El Fill Amount es su inverso:
    /// 0 = lista (sin oscurecer); al usar la habilidad salta a 1 (oscurecido) y desciende
    /// gradualmente hasta 0. Lo llama PlayerCombat cada frame.
    /// </summary>
    public void ActualizarCooldowns(float progresoDash, float progresoAtaqueQ, float progresoEspecial)
    {
        if (cooldownDash != null) cooldownDash.fillAmount = 1f - Mathf.Clamp01(progresoDash);
        if (cooldownAtaqueQ != null) cooldownAtaqueQ.fillAmount = 1f - Mathf.Clamp01(progresoAtaqueQ);
        if (cooldownEspecial != null) cooldownEspecial.fillAmount = 1f - Mathf.Clamp01(progresoEspecial);
    }

    // ------------------------------------------------------------------
    // APERTURA DE MODALES
    // ------------------------------------------------------------------

    public void AbrirModalResonancia() { AbrirModal(Modal_Resonancia); }

    public void AbrirModalRecursos() { AbrirModal(Modal_Recursos); }

    public void AbrirModalPausa() { AbrirModal(Modal_Pausa); }

    public void AbrirModalAyuda() { AbrirModal(Modal_Ayuda); }

    public void AbrirModalConfiguracion() { AbrirModal(Modal_Configuracion); }

    /// <summary>
    /// Enciende el modal con su UIModalWindow.Abrir() (zoom elástico). La pausa
    /// (Time.timeScale = 0 al abrir, 1 al cerrar) es responsabilidad exclusiva de
    /// UIModalWindow: aquí no se escribe el tiempo.
    /// </summary>
    private void AbrirModal(GameObject modal)
    {
        if (modal == null) return;

        UIModalWindow ventana = modal.GetComponent<UIModalWindow>();
        if (ventana != null) ventana.Abrir();
        else modal.SetActive(true);
    }

    /// <summary>
    /// Abre el modal de derrota con el mensaje contextual que decide el Player.
    /// El texto se escribe ANTES de activar el panel; al activarse, su UIModalWindow
    /// congela el tiempo (OnEnable) y al apagarse lo libera (OnDisable).
    /// </summary>
    public void MostrarModalDerrota(string mensaje)
    {
        if (txtDerrota != null) txtDerrota.text = mensaje;
        if (Modal_Derrota != null) Modal_Derrota.SetActive(true);
    }

    /// <summary>Cierra el modal de derrota sin tocar el resto del HUD.</summary>
    public void OcultarModalDerrota()
    {
        if (Modal_Derrota != null && Modal_Derrota.activeSelf) Modal_Derrota.SetActive(false);
    }

    /// <summary>
    /// Banner contextual de interacción ('[F] Interactuar'). Lo reafirma cada frame
    /// el objeto interactuable (tótem, celda...) mientras el héroe esté en su radio.
    /// </summary>
    public void MostrarBannerInstrucciones(string mensaje)
    {
        if (Banner_Instrucciones != null && !Banner_Instrucciones.activeSelf)
            Banner_Instrucciones.SetActive(true);

        if (txtBannerInstrucciones != null) txtBannerInstrucciones.text = mensaje;
    }

    /// <summary>Oculta el banner contextual sin desactivar ningún otro contenedor.</summary>
    public void OcultarBannerInstrucciones()
    {
        if (Banner_Instrucciones != null && Banner_Instrucciones.activeSelf)
            Banner_Instrucciones.SetActive(false);
    }

    // ------------------------------------------------------------------
    // PROGRESO
    // ------------------------------------------------------------------

    /// <summary>Fija el punto de partida: 0 en el Nivel 1 y 0.5 en el Nivel 2.</summary>
    private void InicializarBarraDeProgreso()
    {
        if (Barra_Progreso == null) return;

        Barra_Progreso.minValue = 0f;
        Barra_Progreso.maxValue = 1f;
        Barra_Progreso.value = esNivel1 ? 0f : 0.5f;
        RefrescarTextoProgreso();
    }

    /// <summary>Escribe "Progreso NN%" en el texto de la barra con el valor real del slider.</summary>
    private void RefrescarTextoProgreso()
    {
        if (txtProgresoBarra == null || Barra_Progreso == null) return;

        int porcentaje = Mathf.RoundToInt(Barra_Progreso.value * 100f);
        txtProgresoBarra.text = "Progreso " + porcentaje + "%";
    }

    /// <summary>
    /// Suma un hito de avance (0..1) a la barra de progreso del nivel, acotado con
    /// Mathf.Clamp01. Lo llaman los scripts de hito: arena, puzles, rescate y jefe.
    /// </summary>
    public void SumarProgreso(float valor)
    {
        if (Barra_Progreso == null) return;

        Barra_Progreso.value = Mathf.Clamp01(Barra_Progreso.value + valor);
        RefrescarTextoProgreso();
    }

    // ------------------------------------------------------------------
    // PENTAGRAMA
    // ------------------------------------------------------------------

    /// <summary>
    /// Nivel 1: Illya sigue sellada y su ícono satélite va en gris oscuro.
    /// Nivel 2: blanco puro, sin tinte.
    /// </summary>
    public void AplicarTinteNivel()
    {
        if (iconoIllyaSlot2 == null) return;

        iconoIllyaSlot2.color = esNivel1 ? tinteBloqueado : Color.white;
    }
}
