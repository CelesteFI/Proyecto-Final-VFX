using System.Collections;
using UnityEngine;

/// <summary>
/// Puzzle secuencial de obeliscos de `04_Isla_Templo_Central_Puzzle` (Nivel 2).
///
/// Vive en el padre `Obeliscos_Puzzle` y su collider es el área que abarca las tres
/// piezas. El ciclo es estrictamente ordenado:
///
///   1. El jugador entra al área  -> se habilita el giro del Obelisco 1 ("Girar [F]").
///   2. Gira el obelisco         -> "Ve al fresco para guardar el dato" y se ACTIVA el
///                                  Trigger del <c>Pista_Fresco_Astronomico</c>.
///   3. En el fresco, 'F'        -> "Confirmar Posición [F]" guarda el dato, DESACTIVA el
///                                  Trigger del fresco y habilita el obelisco siguiente.
///   4. Tras confirmar el tercero -> se comparan las tres rotaciones con
///      <see cref="rotacionesCorrectas"/>: si coinciden se envía "Completado" al
///      <see cref="LogicaPilar"/>; si no, aviso temporal, reset a 0 y ciclo desde el 1.
///
/// El fresco nunca se apaga: solo se conmuta el <c>enabled</c> de su collider Trigger,
/// y el gestor es el único que lo toca. No crea elementos de interfaz: escribe en el
/// Banner_Instrucciones del <see cref="Canvas_HUD"/> y no toca Time.timeScale.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class GestorObeliscos : MonoBehaviour
{
    private enum Estado { Esperando, Girando, Resuelto }

    [Header("Obeliscos del Puzzle")]
    [Tooltip("Las tres piezas en el ORDEN de la secuencia (Obelisco 1, 2 y 3).")]
    [SerializeField] private ObeliscoInteractivo[] obeliscos = new ObeliscoInteractivo[0];

    [Header("Solución (grados 0 / 90 / 180 / 270)")]
    [Tooltip("Rotación final exigida a cada obelisco, en el mismo orden que 'obeliscos'.")]
    [SerializeField] private int[] rotacionesCorrectas = { 90, 180, 270 };

    [Header("Recompensa")]
    [Tooltip("Pilar que recibe la señal de Completado al validarse la secuencia.")]
    [SerializeField] private LogicaPilar pilar;

    [Header("Fresco de Confirmación")]
    [Tooltip("Collider Trigger del 'Pista_Fresco_Astronomico'. Arranca DESACTIVADO y lo conmuta este gestor.")]
    [SerializeField] private Collider triggerFresco;

    [Header("HUD")]
    [Tooltip("Canvas_HUD del nivel: por él se escribe el Banner_Instrucciones.")]
    [SerializeField] private Canvas_HUD canvasHUD;

    [Header("Mensajes del Banner")]
    [SerializeField] private string mensajeGirar = "Girar [F]";
    [SerializeField] private string mensajeFresco = "Ve al fresco para guardar el dato";
    [SerializeField] private string mensajeConfirmar = "Confirmar Posición [F]";
    [SerializeField] private string mensajeError = "Patrón incorrecto. Repite la secuencia";
    [SerializeField] private string mensajeExito = "El fresco reconoce el patrón: el viento fluye";
    [Tooltip("Segundos que permanece el aviso de error antes de reiniciar el ciclo.")]
    [SerializeField] private float esperaTrasError = 2.5f;

    [Header("Entrada del Jugador")]
    [Tooltip("PlayerInputReader del Player: la tecla 'F' respeta el bloqueo por UI.")]
    [SerializeField] private PlayerInputReader entrada;

    [Header("Estado (solo lectura)")]
    [SerializeField] private Estado estado = Estado.Esperando;
    [SerializeField] private int obeliscoActivo;
    [SerializeField] private int obeliscosConfirmados;
    [SerializeField] private int girosDeEsteObelisco;
    [SerializeField] private bool jugadorDentro;
    [SerializeField] private bool reiniciando;

    public bool Resuelto => estado == Estado.Resuelto;
    public int ObeliscosConfirmados => obeliscosConfirmados;

    private static readonly Collider[] BufferFresco = new Collider[16];

    private void Awake()
    {
        if (entrada == null) entrada = Object.FindFirstObjectByType<PlayerInputReader>();
        if (canvasHUD == null && entrada != null) canvasHUD = entrada.canvasHUD;

        BoxCollider area = GetComponent<BoxCollider>();
        if (area != null) area.isTrigger = true;

        if (triggerFresco != null) triggerFresco.enabled = false;

        BloquearObeliscos();
        estado = Estado.Esperando;
        OcultarBanner();
    }

    private void Update()
    {
        if (estado == Estado.Resuelto || reiniciando) return;

        if (estado == Estado.Esperando)
        {
            if (jugadorDentro) IniciarCiclo();
            return;
        }

        // Girando. El fresco solo cuenta cuando su Trigger está activo, es decir,
        // después del primer giro de la pieza en curso.
        if (JugadorEnFresco())
        {
            MostrarBanner(mensajeConfirmar);
            if (entrada != null && entrada.InteraccionPulsada) ConfirmarEnFresco();
            return;
        }

        if (jugadorDentro)
        {
            // Mientras la pieza siga habilitada, 'F' vuelve a girarla 90°.
            MostrarBanner(girosDeEsteObelisco > 0 ? mensajeFresco : mensajeGirar);
            if (entrada != null && entrada.InteraccionPulsada) GirarObeliscoActivo();
            return;
        }

        // Fuera del área: si ya hay un giro pendiente de guardar, se recuerda el fresco.
        if (girosDeEsteObelisco > 0) MostrarBanner(mensajeFresco);
        else OcultarBanner();
    }

    // ------------------------------------------------------------------
    // CICLO SECUENCIAL
    // ------------------------------------------------------------------

    private void IniciarCiclo()
    {
        BloquearObeliscos();
        obeliscoActivo = 0;
        obeliscosConfirmados = 0;
        girosDeEsteObelisco = 0;
        estado = Estado.Girando;

        if (triggerFresco != null) triggerFresco.enabled = false;

        HabilitarObelisco(0);
        MostrarBanner(mensajeGirar);
    }

    private void GirarObeliscoActivo()
    {
        if (estado != Estado.Girando) return;

        ObeliscoInteractivo actual = ObeliscoActual();
        if (actual == null) return;
        if (actual.Girando) return;

        actual.Girar();
        girosDeEsteObelisco++;

        // El dato solo se guarda en el fresco: tras el primer giro se abre su Trigger.
        if (triggerFresco != null) triggerFresco.enabled = true;

        MostrarBanner(mensajeFresco);
        Debug.Log($"[OBELISCOS] Obelisco {obeliscoActivo + 1} girado ({actual.PasoActual}°). Toca confirmar en el fresco.");
    }

    private void ConfirmarEnFresco()
    {
        if (estado != Estado.Girando) return;
        if (girosDeEsteObelisco <= 0) return;

        obeliscosConfirmados++;

        if (triggerFresco != null) triggerFresco.enabled = false;
        girosDeEsteObelisco = 0;

        Debug.Log($"[OBELISCOS] Posición {obeliscosConfirmados}/{obeliscos.Length} guardada en el fresco.");

        obeliscoActivo++;

        if (obeliscoActivo >= obeliscos.Length)
        {
            ValidarSecuencia();
            return;
        }

        HabilitarObelisco(obeliscoActivo);
        estado = Estado.Girando;
        MostrarBanner(mensajeGirar);
    }

    private void ValidarSecuencia()
    {
        bool correcta = true;

        for (int i = 0; i < obeliscos.Length; i++)
        {
            if (obeliscos[i] == null) { correcta = false; break; }

            int esperado = Mathf.RoundToInt(Mathf.Repeat(i < rotacionesCorrectas.Length ? rotacionesCorrectas[i] : 0, 360f));
            if (obeliscos[i].PasoActual != esperado) { correcta = false; break; }
        }

        if (correcta)
        {
            estado = Estado.Resuelto;
            BloquearObeliscos();
            MostrarBanner(mensajeExito);

            if (pilar != null) pilar.Completar();
            else Debug.LogWarning("[OBELISCOS] No hay LogicaPilar asignado: el escudo no se retirará.");
            return;
        }

        StartCoroutine(ReiniciarTrasError());
    }

    private IEnumerator ReiniciarTrasError()
    {
        reiniciando = true;
        MostrarBanner(mensajeError);

        yield return new WaitForSeconds(Mathf.Max(0.5f, esperaTrasError));

        for (int i = 0; i < obeliscos.Length; i++)
        {
            if (obeliscos[i] != null) obeliscos[i].Resetear();
        }

        reiniciando = false;
        girosDeEsteObelisco = 0;

        if (jugadorDentro) IniciarCiclo();
        else
        {
            BloquearObeliscos();
            estado = Estado.Esperando;
            OcultarBanner();
        }

        Debug.Log("[OBELISCOS] Patrón incorrecto: rotaciones reseteadas y ciclo reiniciado.");
    }

    // ------------------------------------------------------------------
    // FRESCO
    // ------------------------------------------------------------------

    /// <summary>
    /// True si el jugador está dentro del Trigger del fresco. Solo se evalúa cuando
    /// ese Trigger está habilitado, es decir, únicamente entre girar y confirmar.
    /// </summary>
    private bool JugadorEnFresco()
    {
        if (triggerFresco == null || !triggerFresco.enabled) return false;

        Bounds zona = triggerFresco.bounds;
        int encontrados = Physics.OverlapBoxNonAlloc(zona.center, zona.extents, BufferFresco,
                                                     Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < encontrados; i++)
        {
            if (EsJugador(BufferFresco[i])) return true;
        }

        return false;
    }

    // ------------------------------------------------------------------
    // ÁREA Y UTILIDADES
    // ------------------------------------------------------------------

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;

        jugadorDentro = true;
        if (estado == Estado.Esperando && !reiniciando) IniciarCiclo();
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;

        // El aviso lo gobierna Update: con un giro pendiente de guardar se mantiene
        // en pantalla aunque el jugador salga del área hacia el fresco.
        jugadorDentro = false;
    }

    private void OnDisable()
    {
        jugadorDentro = false;
        OcultarBanner();
    }

    private ObeliscoInteractivo ObeliscoActual()
    {
        if (obeliscos == null || obeliscoActivo < 0 || obeliscoActivo >= obeliscos.Length) return null;
        return obeliscos[obeliscoActivo];
    }

    private void HabilitarObelisco(int indice)
    {
        for (int i = 0; i < obeliscos.Length; i++)
        {
            if (obeliscos[i] != null) obeliscos[i].Habilitar(i == indice);
        }
    }

    private void BloquearObeliscos()
    {
        for (int i = 0; i < obeliscos.Length; i++)
        {
            if (obeliscos[i] != null) obeliscos[i].Habilitar(false);
        }
    }

    private void MostrarBanner(string mensaje)
    {
        if (canvasHUD != null) canvasHUD.MostrarBannerInstrucciones(mensaje);
    }

    private void OcultarBanner()
    {
        if (canvasHUD != null) canvasHUD.OcultarBannerInstrucciones();
    }

    private static bool EsJugador(Collider otro)
    {
        if (otro == null) return false;
        if (otro.CompareTag("Player")) return true;
        return otro.GetComponentInParent<PlayerInputReader>() != null;
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(ObeliscoInteractivo[] nuevosObeliscos, int[] solucion, LogicaPilar nuevoPilar,
                           Collider fresco, Canvas_HUD hud, PlayerInputReader input)
    {
        obeliscos = (nuevosObeliscos != null) ? nuevosObeliscos : new ObeliscoInteractivo[0];
        if (solucion != null && solucion.Length > 0) rotacionesCorrectas = solucion;
        pilar = nuevoPilar;
        triggerFresco = fresco;
        canvasHUD = hud;
        entrada = input;

        BloquearObeliscos();
        estado = Estado.Esperando;
    }
}
