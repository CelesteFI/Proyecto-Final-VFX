using UnityEngine;

/// <summary>
/// Contrato para elementos que reaccionan a un golpe elemental
/// (tótems de secuencia, corazones de savia, engranajes rúnicos).
/// Permite que el combate notifique al sistema de puzzles sin acoplarse a él.
/// </summary>
public interface IGolpeableElemental
{
    /// <summary>Elemento con el que fue golpeado.</summary>
    void RecibirGolpeElemental(HeroCombatData.ElementType elemento, Vector3 puntoImpacto);
}
