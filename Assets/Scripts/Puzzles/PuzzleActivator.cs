using UnityEngine;

/// <summary>
/// Elemento golpeable de un puzzle (tótem rúnico, ranura de muro, corazón de savia).
/// Se ilumina al activarse y responde con un pulso de escala al recibir un golpe elemental.
/// </summary>
[DisallowMultipleComponent]
public class PuzzleActivator : MonoBehaviour, IGolpeableElemental
{
    [Header("Identidad en la Secuencia")]
    [Tooltip("Índice lógico del elemento (0 = primero).")]
    public int idSecuencia;
    public string etiqueta = "Topo";

    [Header("Feedback Visual")]
    [SerializeField] private Color colorApagado = new Color(0.22f, 0.16f, 0.06f, 1f);
    [SerializeField] private Color colorEncendido = new Color(1f, 0.76f, 0.18f, 1f);
    [SerializeField] private float duracionPulso = 0.14f;

    public bool EstaEncendido { get; private set; }

    private Renderer[] renderers;
    private MaterialPropertyBlock bloque;
    private int idColor;
    private Vector3 escalaBase;
    private float temporizadorPulso;

    private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        bloque = new MaterialPropertyBlock();
        idColor = Shader.PropertyToID("_BaseColor");
        escalaBase = transform.localScale;
        Pintar(colorApagado);
    }

    private void Update()
    {
        if (temporizadorPulso <= 0f) return;

        temporizadorPulso -= Time.deltaTime;
        float t = Mathf.Clamp01(temporizadorPulso / duracionPulso);
        transform.localScale = Vector3.Lerp(escalaBase, escalaBase * 1.12f, t);
        if (temporizadorPulso <= 0f) transform.localScale = escalaBase;
    }

    /// <summary>Recibe el golpe del héroe y responde con el pulso de resonancia.</summary>
    public void RecibirGolpeElemental(HeroCombatData.ElementType elemento, Vector3 puntoImpacto)
    {
        temporizadorPulso = duracionPulso;
    }

    /// <summary>Enciende o apaga el elemento (resonancia rúnica).</summary>
    public void Encender(bool encendido)
    {
        EstaEncendido = encendido;
        Pintar(encendido ? colorEncendido : colorApagado);
    }

    private void Pintar(Color color)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(bloque);
            bloque.SetColor(idColor, color);
            bloque.SetColor(IdEmision, color * 0.55f);
            r.SetPropertyBlock(bloque);
        }
    }
}
