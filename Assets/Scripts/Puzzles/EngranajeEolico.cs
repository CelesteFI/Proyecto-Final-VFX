using UnityEngine;

/// <summary>
/// Engranaje rúnico del Santuario del Viento. Solo puede anclarse con un golpe de
/// Madera mientras la tobera está sellada por el Muro de Basalto (8 s); si el sello
/// ya colapsó, el engranaje vuelve a girar y hay que reintentarlo.
/// </summary>
[DisallowMultipleComponent]
public class EngranajeEolico : MonoBehaviour, IGolpeableElemental
{
    [Header("Condición de Anclaje")]
    [SerializeField] private WeightPressurePlate tobera;
    [SerializeField] private bool requiereSelloActivo = true;

    [Header("Recompensa")]
    [SerializeField] private PuzzleDoor puerta;

    [Header("Feedback")]
    [SerializeField] private Renderer visual;
    [SerializeField] private Color colorAnclado = new Color(1f, 0.78f, 0.20f);
    [SerializeField] private Color colorLibre = new Color(0.30f, 0.55f, 0.65f);

    public bool Anclado { get; private set; }

    private void Awake()
    {
        Pintar(colorLibre);
    }

    public void RecibirGolpeElemental(HeroCombatData.ElementType elemento, Vector3 puntoImpacto)
    {
        if (Anclado) return;

        if (elemento != HeroCombatData.ElementType.Madera)
        {
            Debug.Log("[SANTUARIO] El engranaje solo responde a la madera de Illya.");
            return;
        }

        if (requiereSelloActivo && tobera != null && !tobera.EstaEnergizada)
        {
            Debug.Log("[SANTUARIO] El sello de basalto ya colapsó: el engranaje vuelve a girar.");
            return;
        }

        Anclado = true;
        Pintar(colorAnclado);

        if (puerta != null) puerta.AbrirPermanentemente();

        Debug.Log("[SANTUARIO] Engranaje anclado: se abre el paso a la arena del jefe.");
    }

    private void Pintar(Color color)
    {
        if (visual == null) return;

        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        visual.GetPropertyBlock(bloque);
        bloque.SetColor(Shader.PropertyToID("_BaseColor"), color);
        bloque.SetColor(Shader.PropertyToID("_EmissionColor"), color * 0.5f);
        visual.SetPropertyBlock(bloque);
    }

    public void Configurar(WeightPressurePlate placaTobera, PuzzleDoor puertaObjetivo, Renderer visualEngranaje)
    {
        tobera = placaTobera;
        puerta = puertaObjetivo;
        visual = visualEngranaje;
        Pintar(colorLibre);
    }
}
