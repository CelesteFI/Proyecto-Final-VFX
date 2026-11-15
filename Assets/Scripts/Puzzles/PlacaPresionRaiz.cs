using UnityEngine;

/// <summary>
/// Placa de presión del circuito de raíces del Nivel 1.
/// Su collider es un Trigger: avisa al <see cref="PuzzleRaicesCorruptas"/> cuando el
/// héroe pisa la placa, indicando su rol (A o B) para que el puzzle valide el orden.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PlacaPresionRaiz : MonoBehaviour
{
    [Header("Enlaces (asignados en el Inspector)")]
    [SerializeField] private PuzzleRaicesCorruptas puzzle;
    [SerializeField] private RolPlacaRaiz rol = RolPlacaRaiz.A;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool heroeEncima;

    public RolPlacaRaiz Rol => rol;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsHeroe(otro)) return;

        heroeEncima = true;
        if (puzzle != null) puzzle.ReportarPisada(rol);
        else Debug.LogWarning($"[PUZZLE RAÍCES] {name}: placa sin puzzle asignado.");
    }

    private void OnTriggerExit(Collider otro)
    {
        if (EsHeroe(otro)) heroeEncima = false;
    }

    private static bool EsHeroe(Collider otro)
    {
        if (otro == null) return false;
        if (otro.GetComponentInParent<TagTeamManager>() != null) return true;
        return otro.CompareTag("Player");
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(PuzzleRaicesCorruptas gestor, RolPlacaRaiz nuevoRol)
    {
        puzzle = gestor;
        rol = nuevoRol;
    }
}
