using UnityEngine;

/// <summary>
/// Relé de apertura: replica el estado de una PuzzleDoor sobre una segunda puerta.
/// Se usa en el Puente Fracturado, donde dos losas deben elevarse simultáneamente
/// al resolverse la secuencia de runas.
/// </summary>
[RequireComponent(typeof(PuzzleDoor))]
public class RelayApertura : MonoBehaviour
{
    [SerializeField] private PuzzleDoor puertaSecundaria;

    private PuzzleDoor puertaOrigen;
    private bool disparado;

    private void Awake()
    {
        puertaOrigen = GetComponent<PuzzleDoor>();
    }

    private void OnEnable()
    {
        if (puertaOrigen != null) puertaOrigen.OnEstadoCambiado += AlCambiarEstado;
    }

    private void OnDisable()
    {
        if (puertaOrigen != null) puertaOrigen.OnEstadoCambiado -= AlCambiarEstado;
    }

    private void AlCambiarEstado(bool abierta)
    {
        if (disparado || !abierta || puertaSecundaria == null) return;

        disparado = true;
        puertaSecundaria.AbrirPermanentemente();
    }

    public void Configurar(PuzzleDoor secundaria)
    {
        puertaSecundaria = secundaria;
    }
}
