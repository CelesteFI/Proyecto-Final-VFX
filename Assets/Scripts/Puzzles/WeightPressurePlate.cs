using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Placa de presión del puzzle "El Circuito de Presión y la Falla de Basalto".
/// Dos modos:
///  - MuroBasalto: se energiza cuando el Muro de Basalto cae sobre ella y
///    mantiene un canal rúnico activo durante `ventanaEnergia` segundos.
///  - PresionJugador: si el héroe pisa la placa mientras la placa vinculada
///    sigue energizada, el circuito se resuelve y abre la reja.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeightPressurePlate : MonoBehaviour
{
    public enum ModoActivacion { MuroBasalto, PresionJugador }

    [Header("Configuración del Circuito")]
    public ModoActivacion modo = ModoActivacion.MuroBasalto;
    [Tooltip("Tiempo que el canal rúnico permanece energizado tras liberar la placa.")]
    public float ventanaEnergia = 5f;

    [Header("Enlaces del Circuito")]
    [Tooltip("Placa origen cuya energía debe seguir activa al pisar esta placa.")]
    public WeightPressurePlate placaVinculada;
    public PuzzleDoor puerta;

    [Header("Feedback Visual")]
    [SerializeField] private Renderer canalRunico;
    [SerializeField] private Color colorInactivo = new Color(0.05f, 0.10f, 0.16f, 1f);
    [SerializeField] private Color colorActivo = new Color(0.10f, 0.62f, 0.95f, 1f);

    [Header("Eventos")]
    [SerializeField] private UnityEvent alEnergizar = new UnityEvent();
    [SerializeField] private UnityEvent alResolver = new UnityEvent();

    public bool EstaEnergizada => temporizador > 0f;
    public float TiempoRestante => temporizador;

    private float temporizador;
    private bool resuelto;
    private MaterialPropertyBlock bloque;
    private int idColor;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (bloque == null) bloque = new MaterialPropertyBlock();
        idColor = Shader.PropertyToID("_BaseColor");
        PintarCanal(false);
    }

    private void Update()
    {
        if (temporizador <= 0f) return;

        float anterior = temporizador;
        temporizador -= Time.deltaTime;

        if (anterior > 0f && temporizador <= 0f) PintarCanal(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (resuelto) return;

        if (modo == ModoActivacion.MuroBasalto)
        {
            if (other.GetComponentInParent<MarcadorMuroBasalto>() == null) return;
            Energizar();
        }
        else
        {
            if (other.GetComponentInParent<TagTeamManager>() == null) return;

            // El héroe pisa la placa: el canal rúnico se enciende y arranca el
            // contador de la ventana. La placa receptora comprueba después si el
            // canal de su placa vinculada sigue energizado.
            Energizar();

            if (placaVinculada == null) return;

            if (placaVinculada.EstaEnergizada) Resolver();
            else Debug.Log("[PUZZLE] La placa receptora se pisó sin el canal rúnico activo.");
        }
    }

    /// <summary>Activa la ventana de energía del canal rúnico.</summary>
    public void Energizar()
    {
        if (resuelto) return;

        temporizador = ventanaEnergia;
        PintarCanal(true);
        alEnergizar?.Invoke();

        // Placa terminal (sin placa vinculada): su único desenlace posible es
        // energizarse, así que ese es el momento de disparar 'alResolver'.
        // No se marca 'resuelto' para que la placa pueda reutilizarse
        // (tobera del viento del Nivel 2).
        if (placaVinculada == null) alResolver?.Invoke();

        Debug.Log($"[PUZZLE] Canal rúnico energizado por {ventanaEnergia:F1}s.");
    }

    private void Resolver()
    {
        resuelto = true;
        PintarCanal(true);

        if (puerta != null) puerta.AbrirPermanentemente();
        alResolver?.Invoke();

        Debug.Log("[PUZZLE] Circuito de presión resuelto: la reja rúnica se abre.");
    }

    private void PintarCanal(bool activo)
    {
        if (canalRunico == null) return;

        // En modo edición Awake() aún no se ha ejecutado cuando el integrador
        // configura la placa, así que la caché se crea de forma perezosa.
        if (bloque == null) bloque = new MaterialPropertyBlock();
        if (idColor == 0) idColor = Shader.PropertyToID("_BaseColor");

        canalRunico.GetPropertyBlock(bloque);
        bloque.SetColor(idColor, activo ? colorActivo : colorInactivo);
        bloque.SetColor(Shader.PropertyToID("_EmissionColor"), activo ? colorActivo * 0.8f : Color.black);
        canalRunico.SetPropertyBlock(bloque);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(ModoActivacion nuevoModo, float ventana, WeightPressurePlate vinculada, PuzzleDoor puertaObjetivo, Renderer canal)
    {
        modo = nuevoModo;
        ventanaEnergia = ventana;
        placaVinculada = vinculada;
        puerta = puertaObjetivo;
        canalRunico = canal;
        PintarCanal(false);
    }

    /// <summary>Expuesto para pruebas automáticas.</summary>
    public bool Resuelto => resuelto;
}
