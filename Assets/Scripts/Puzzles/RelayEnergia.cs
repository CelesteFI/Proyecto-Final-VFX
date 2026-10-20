using UnityEngine;

/// <summary>
/// Relé de energía: replica el estado de una WeightPressurePlate sobre una
/// PuzzleDoor (la plataforma desciende mientras el sello de basalto siga activo).
/// Se usa en la plataforma superior del Santuario del Viento.
/// </summary>
public class RelayEnergia : MonoBehaviour
{
    [SerializeField] private WeightPressurePlate fuente;
    [SerializeField] private PuzzleDoor puerta;

    private bool estadoAnterior;

    private void Update()
    {
        bool activo = fuente != null && fuente.EstaEnergizada;
        if (activo == estadoAnterior) return;

        estadoAnterior = activo;

        if (puerta == null) return;
        if (activo) puerta.AbrirPermanentemente();
        else puerta.Cerrar();
    }

    public void Configurar(WeightPressurePlate placa, PuzzleDoor puertaObjetivo)
    {
        fuente = placa;
        puerta = puertaObjetivo;
    }
}
