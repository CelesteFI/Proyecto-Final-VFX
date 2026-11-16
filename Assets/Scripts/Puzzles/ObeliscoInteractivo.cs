using UnityEngine;

/// <summary>
/// Obelisco del puzzle secuencial de `04_Isla_Templo_Central_Puzzle` (Nivel 2).
/// Contiene únicamente la rotación de la pieza: 90° en Y por pulsación.
///
/// NO lleva trigger propio ni detecta al jugador: quien decide CUÁNDO se gira es
/// <see cref="GestorObeliscos"/>, que habilita un único obelisco por paso del ciclo.
/// El giro se interpola en <see cref="duracionGiro"/> segundos para que la pieza no
/// salte de golpe, pero el paso lógico (<see cref="PasoActual"/>) se resuelve con el
/// ángulo OBJETIVO, de modo que la validación del gestor nunca lee un valor intermedio.
/// </summary>
[DisallowMultipleComponent]
public class ObeliscoInteractivo : MonoBehaviour
{
    [Header("Rotación")]
    [Tooltip("Grados que avanza la pieza por cada giro (90 = cuatro posiciones por vuelta).")]
    [SerializeField] private float gradosPorPaso = 90f;
    [Tooltip("Segundos que tarda el giro en completarse.")]
    [SerializeField] private float duracionGiro = 0.35f;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool habilitado;
    [SerializeField] private bool girando;

    private Vector3 rotacionBase;
    private float anguloActual;
    private float anguloInicio;
    private float anguloObjetivo;
    private float progresoGiro = 1f;

    /// <summary>El gestor solo gira la pieza que tiene habilitada en este paso.</summary>
    public bool Habilitado => habilitado;

    /// <summary>True mientras la pieza se está moviendo hacia su ángulo objetivo.</summary>
    public bool Girando => girando;

    /// <summary>
    /// Posición lógica de la pieza en grados (0, 90, 180, 270 con el paso por defecto),
    /// calculada con el ángulo objetivo y no con el interpolado.
    /// </summary>
    public int PasoActual => Mathf.RoundToInt(Mathf.Repeat(anguloObjetivo, 360f));

    private void Awake()
    {
        rotacionBase = transform.localEulerAngles;
        anguloActual = 0f;
        anguloObjetivo = 0f;
    }

    private void Update()
    {
        if (!girando) return;

        progresoGiro += Time.deltaTime / Mathf.Max(0.01f, duracionGiro);
        float suavizado = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progresoGiro));
        Aplicar(Mathf.Lerp(anguloInicio, anguloObjetivo, suavizado));

        if (progresoGiro >= 1f)
        {
            Aplicar(anguloObjetivo);
            girando = false;
        }
    }

    /// <summary>El gestor habilita la pieza al inicio de su paso y la bloquea al confirmarla.</summary>
    public void Habilitar(bool activo)
    {
        habilitado = activo;
    }

    /// <summary>Avanza la pieza un paso de <see cref="gradosPorPaso"/> grados en Y.</summary>
    public void Girar()
    {
        if (!habilitado) return;
        if (girando) return;

        anguloInicio = anguloActual;
        anguloObjetivo = anguloActual + gradosPorPaso;
        progresoGiro = 0f;
        girando = true;
    }

    /// <summary>Devuelve la pieza a 0° sin animación (reinicio de la secuencia).</summary>
    public void Resetear()
    {
        girando = false;
        progresoGiro = 1f;
        anguloActual = 0f;
        anguloObjetivo = 0f;
        Aplicar(0f);
    }

    private void Aplicar(float angulo)
    {
        anguloActual = angulo;
        transform.localRotation = Quaternion.Euler(rotacionBase.x, rotacionBase.y + angulo, rotacionBase.z);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(float nuevoPaso, float nuevaDuracion)
    {
        gradosPorPaso = nuevoPaso;
        duracionGiro = nuevaDuracion;
    }
}
