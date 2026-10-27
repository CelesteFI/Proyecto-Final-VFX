using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sello rúnico del puente fracturado (Nivel 1). Se disuelve cuando el jugador
/// ha recuperado el número requerido de fragmentos de lore de la Arena B, sin
/// importar el orden en que los leyó.
/// Al disolverse retira la barrera física y habilita el paso hacia la arena del
/// jefe final; el estado se evalúa también al arrancar, para que un fragmento
/// ya persistido no deje el sello cerrado tras recargar la escena.
/// </summary>
[DisallowMultipleComponent]
public class SelloNarrativo : MonoBehaviour
{
    [Header("Fragmentos que lo Disuelven")]
    [SerializeField] private List<TotemLore> fragmentos = new List<TotemLore>();
    [SerializeField] private int requeridos = 3;

    [Header("Piezas Físicas")]
    [Tooltip("GameObject con el collider que impide el paso. Se desactiva al abrir.")]
    [SerializeField] private GameObject barrera;

    [Tooltip("Paso que solo existe cuando el sello cede (opcional).")]
    [SerializeField] private GameObject pasoHabilitado;

    [SerializeField] private Renderer visualSello;

    [Header("Colores")]
    [SerializeField] private Color colorSellado = new Color(0.55f, 0.92f, 0.95f, 1f);
    [SerializeField] private Color colorDisuelto = new Color(0.28f, 0.33f, 0.38f, 1f);

    [Header("Estado")]
    [SerializeField] private bool abierto;

    /// <summary>True cuando el sello ya cedió.</summary>
    public bool Abierto => abierto;

    /// <summary>Se dispara al disolverse el sello.</summary>
    public event Action OnSelloDisuelto;

    private void OnEnable()
    {
        for (int i = 0; i < fragmentos.Count; i++)
        {
            if (fragmentos[i] != null) fragmentos[i].OnLeido += AlLeerFragmento;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < fragmentos.Count; i++)
        {
            if (fragmentos[i] != null) fragmentos[i].OnLeido -= AlLeerFragmento;
        }
    }

    private void Start()
    {
        Pintar();
        Evaluar();
    }

    private void AlLeerFragmento(TotemLore _)
    {
        Evaluar();
    }

    /// <summary>Cuenta los fragmentos recuperados y disuelve el sello si procede.</summary>
    public void Evaluar()
    {
        if (abierto || fragmentos.Count == 0) return;

        int leidos = 0;
        for (int i = 0; i < fragmentos.Count; i++)
        {
            TotemLore f = fragmentos[i];
            if (f == null) continue;

            // Se consulta también la partida guardada: así el orden de Start()
            // entre tótems y sello no altera el resultado.
            bool persistido = GameProgressManager.Instance != null
                              && GameProgressManager.Instance.EstaFragmentoLeido(f.IdFragmento);

            if (f.Leido || persistido) leidos++;
        }

        int objetivo = Mathf.Clamp(requeridos, 1, fragmentos.Count);
        if (leidos >= objetivo) Disolver();
    }

    /// <summary>Disuelve el sello: retira la barrera y deja libre el puente.</summary>
    public void Disolver()
    {
        if (abierto) return;
        abierto = true;

        if (barrera != null) barrera.SetActive(false);
        if (pasoHabilitado != null) pasoHabilitado.SetActive(true);

        Pintar();

        Debug.Log($"[SELLO] El sello rúnico del puente se ha disuelto: paso libre hacia la arena final.");
        OnSelloDisuelto?.Invoke();
    }

    private void Pintar()
    {
        if (visualSello == null) return;

        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        visualSello.GetPropertyBlock(bloque);

        Color color = abierto ? colorDisuelto : colorSellado;
        bloque.SetColor(Shader.PropertyToID("_BaseColor"), color);
        bloque.SetColor(Shader.PropertyToID("_EmissionColor"), color * (abierto ? 0.05f : 0.9f));
        visualSello.SetPropertyBlock(bloque);
    }

    /// <summary>Configuración desde el integrador de escena.</summary>
    public void Configurar(List<TotemLore> totems, int necesarios,
                           GameObject barreraFisica, GameObject paso, Renderer visual)
    {
        fragmentos = totems ?? new List<TotemLore>();
        requeridos = Mathf.Max(1, necesarios);
        barrera = barreraFisica;
        pasoHabilitado = paso;
        visualSello = visual;
    }
}
