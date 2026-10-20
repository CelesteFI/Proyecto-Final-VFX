using UnityEngine;

/// <summary>
/// Contrato de interacción de entorno (tecla 'F').
/// Lo implementan cofres, llaves, celdas de rescate y altares.
/// </summary>
public interface IInteractuable
{
    /// <summary>Texto que el HUD puede mostrar como pista ("Abrir cofre", "Liberar a Illya"...).</summary>
    string TextoPrompt { get; }

    /// <summary>Indica si la interacción está disponible en este momento.</summary>
    bool PuedeInteractuar { get; }

    /// <summary>Ejecuta la interacción sobre el actor indicado.</summary>
    void Interactuar(Transform actor);
}
