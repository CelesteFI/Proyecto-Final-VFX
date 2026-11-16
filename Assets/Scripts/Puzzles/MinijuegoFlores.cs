using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Puzzle 1 de la Isla Spawn (Nivel 2): las tres Flores_Puzzle.
///
/// No crea interfaz ni duplica lógica de combate: implementa
/// <see cref="IGolpeableElemental"/>, de modo que el proyectil de Illya
/// (PlayerCombat) se lo entrega al impactar contra el collider de una flor
/// mediante GetComponentInParent. Solo la magia de la Naturaleza (Madera)
/// hace florecer la reacción; Caelen no puede resolverlo.
///
/// Al golpear una flor: reproduce su VFX 'Flor_Desaparecer' en el punto donde
/// estaba, la desactiva y suma una baja. Con las tres apagadas publica
/// "Completado" al <see cref="LogicaPilar"/> correspondiente, que es quien
/// retira el escudo y abre la interacción.
/// </summary>
[DisallowMultipleComponent]
public class MinijuegoFlores : MonoBehaviour, IGolpeableElemental
{
    [Header("Flores del Puzzle")]
    [Tooltip("Las tres Flores_Puzzle de la isla. Su collider debe estar en la capa Puzzle.")]
    [SerializeField] private Transform[] flores = new Transform[0];

    [Header("Elemento Válido")]
    [Tooltip("Illya (Madera) es la única que puede golpear las flores.")]
    [SerializeField] private HeroCombatData.ElementType elementoRequerido = HeroCombatData.ElementType.Madera;

    [Header("Efecto de Desaparición")]
    [Tooltip("Nombre del hijo de cada flor que contiene el VFX Flor_Desaparecer.")]
    [SerializeField] private string nombreVfx = "Flor_Desaparecer";
    [Tooltip("Segundos que se conserva el VFX una vez separado de la flor.")]
    [SerializeField] private float vidaVfx = 3f;

    [Header("Recompensa")]
    [Tooltip("Pilar que recibe la señal de Completado al apagarse la tercera flor.")]
    [SerializeField] private LogicaPilar pilar;

    [Header("Detección")]
    [Tooltip("Radio (m) alrededor del punto de impacto para reconocer qué flor fue golpeada.")]
    [SerializeField] private float radioImpacto = 4f;

    [Header("Estado (solo lectura)")]
    [SerializeField] private int golpeadas;

    public int Total => flores.Length;
    public int Golpeadas => golpeadas;
    public int Restantes => Mathf.Max(0, flores.Length - golpeadas);
    public bool Completado => flores.Length > 0 && golpeadas >= flores.Length;

    private bool[] alcanzadas;

    private void Awake()
    {
        if (flores == null) flores = new Transform[0];
        alcanzadas = new bool[flores.Length];
        golpeadas = 0;
    }

    // ------------------------------------------------------------------
    // GOLPE DEL PROYECTIL
    // ------------------------------------------------------------------

    /// <summary>
    /// PlayerCombat entrega aquí el impacto cuando el proyectil alcanza el
    /// collider de una flor. Se apaga la flor más cercana al punto de impacto.
    /// </summary>
    public void RecibirGolpeElemental(HeroCombatData.ElementType elemento, Vector3 puntoImpacto)
    {
        if (elemento != elementoRequerido) return;
        if (Completado || alcanzadas == null) return;

        int indice = FlorMasCercana(puntoImpacto);
        if (indice < 0) return;

        GolpearFlor(indice);
    }

    private int FlorMasCercana(Vector3 punto)
    {
        int mejor = -1;
        float mejorDistancia = radioImpacto;

        for (int i = 0; i < flores.Length; i++)
        {
            if (alcanzadas[i] || flores[i] == null) continue;

            float distancia = Vector3.Distance(punto, CentroDe(flores[i]));
            if (distancia > mejorDistancia) continue;

            mejorDistancia = distancia;
            mejor = i;
        }

        return mejor;
    }

    private static Vector3 CentroDe(Transform flor)
    {
        Renderer render = flor.GetComponentInChildren<Renderer>();
        return render != null ? render.bounds.center : flor.position;
    }

    private void GolpearFlor(int indice)
    {
        alcanzadas[indice] = true;
        golpeadas++;

        Transform flor = flores[indice];

        if (flor != null)
        {
            ReproducirDesaparicion(flor);
            flor.gameObject.SetActive(false);
        }

        Debug.Log($"[FLORES] Flor {indice + 1} apagada con magia de {elementoRequerido}. " +
                  $"Quedan {Restantes}/{Total}.");

        if (!Completado) return;

        Debug.Log("[FLORES] Minijuego completado: se envía la señal al pilar.");

        if (pilar != null) pilar.Completar();
        else Debug.LogWarning("[FLORES] No hay LogicaPilar asignado: el escudo no se retirará.");
    }

    /// <summary>
    /// Separa el VFX del árbol de la flor (para que sobreviva a su desactivación)
    /// y lo reproduce en el sitio. Se destruye solo al terminar.
    /// </summary>
    private void ReproducirDesaparicion(Transform flor)
    {
        if (string.IsNullOrEmpty(nombreVfx)) return;

        Transform vfx = flor.Find(nombreVfx);
        if (vfx == null) return;

        vfx.SetParent(null, true);

        VisualEffect efecto = vfx.GetComponent<VisualEffect>();
        if (efecto != null) efecto.Play();

        ParticleSystem[] sistemas = vfx.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < sistemas.Length; i++) sistemas[i].Play();

        Destroy(vfx.gameObject, Mathf.Max(0.5f, vidaVfx));
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(Transform[] nuevasFlores, LogicaPilar nuevoPilar)
    {
        flores = (nuevasFlores != null) ? nuevasFlores : new Transform[0];
        pilar = nuevoPilar;

        alcanzadas = new bool[flores.Length];
        golpeadas = 0;
    }
}
