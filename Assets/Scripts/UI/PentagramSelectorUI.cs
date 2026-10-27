using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selector Pentagrama del HUD (Inferior Izquierda).
/// Sincroniza la ranura central con el héroe activo y aplica el estado de
/// bloqueo (alpha 0.35 / no interactuable) a los héroes aún sellados.
/// No construye ningún elemento: todos existen en la Hierarchy.
/// </summary>
[DisallowMultipleComponent]
public class PentagramSelectorUI : MonoBehaviour
{
    [Header("Enlace")]
    [Tooltip("TagTeamManager de la escena. Se asigna en el Inspector.")]
    [SerializeField] private TagTeamManager tagTeam;

    [Header("Ranura Central")]
    [SerializeField] private Image iconoCentral;
    [SerializeField] private GameObject glowCentral;

    [Header("5 Ranuras Satélite (orden: Caelen, E2, E3, E4, Illya)")]
    [SerializeField] private List<Image> iconosSatelite = new List<Image>(5);
    [SerializeField] private List<GameObject> glowsSatelite = new List<GameObject>(5);
    [SerializeField] private List<CanvasGroup> gruposSatelite = new List<CanvasGroup>(5);

    [Header("Estado de Bloqueo")]
    [SerializeField] private float alfaBloqueado = 0.35f;

    private const int IndiceCaelen = 0;
    private const int IndiceIllya = 4;

    private void Start()
    {
        if (tagTeam != null) tagTeam.OnHeroeCambiado += Refrescar;

        Refrescar(tagTeam != null ? tagTeam.ActiveHero : null);
    }

    private void OnDestroy()
    {
        if (tagTeam != null) tagTeam.OnHeroeCambiado -= Refrescar;
    }

    /// <summary>Repinta la ranura central y los satélites según el héroe activo.</summary>
    public void Refrescar(HeroCombatData heroe)
    {
        if (tagTeam == null || heroe == null) return;

        bool caelenActivo = tagTeam.ActiveHeroIndex == 0;
        bool illyaLibre = tagTeam.IllyaDesbloqueada;

        // 1. Ranura central: réplica del héroe activo con su halo dorado.
        if (iconoCentral != null)
        {
            iconoCentral.enabled = true;
            iconoCentral.color = heroe.proxyColor;
        }

        if (glowCentral != null && !glowCentral.activeSelf) glowCentral.SetActive(true);

        // 2. Satélites: bloqueo de los sellados y halo fino del activo.
        for (int i = 0; i < 5; i++)
        {
            bool esCaelen = i == IndiceCaelen;
            bool esIllya = i == IndiceIllya;
            bool bloqueado = esIllya && !illyaLibre;
            bool activo = (esCaelen && caelenActivo) || (esIllya && !caelenActivo);

            if (i < gruposSatelite.Count && gruposSatelite[i] != null)
            {
                gruposSatelite[i].alpha = bloqueado ? alfaBloqueado : 1f;
                gruposSatelite[i].interactable = !bloqueado;
                gruposSatelite[i].blocksRaycasts = !bloqueado;
            }

            if (i < glowsSatelite.Count && glowsSatelite[i] != null)
            {
                bool encender = activo && !bloqueado;
                if (glowsSatelite[i].activeSelf != encender) glowsSatelite[i].SetActive(encender);
            }

            if (i < iconosSatelite.Count && iconosSatelite[i] != null && (esCaelen || esIllya))
            {
                // El marco y el icono de los héroes van en blanco puro con alpha 1.0;
                // la atenuación de Illya la aplica el CanvasGroup de la ranura.
                iconosSatelite[i].color = Color.white;
            }
        }
    }
}
