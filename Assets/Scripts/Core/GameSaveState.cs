using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistencia local del proyecto sobre PlayerPrefs: recursos, llaves/objetos
/// recogidos y el desbloqueo de Illya.
/// No crea ni dibuja interfaz: el HUD lee estos valores con observadores.
/// Se borra por completo tras una derrota total o al pulsar 'Reiniciar Nivel'.
/// </summary>
public static class GameSaveState
{
    private const string Prefijo = "EcosEraOlvidada.";
    private const string ClaveCristalesVoto = Prefijo + "Recurso.CristalesVoto";
    private const string ClaveOroPuro = Prefijo + "Recurso.OroPuro";
    private const string ClaveEsencias = Prefijo + "Recurso.Esencias";
    private const string ClaveObjetos = Prefijo + "Objetos";
    private const string ClaveFragmentos = Prefijo + "Lore.Fragmentos";
    private const string ClaveIllya = Prefijo + "Progreso.IllyaDesbloqueada";
    private const char Separador = '|';

    /// <summary>Se dispara cuando cambia cualquier valor persistido.</summary>
    public static event Action OnCambio;

    private static HashSet<string> objetos;
    private static HashSet<string> fragmentos;
    private static bool cargado;

    private static void AsegurarCarga()
    {
        if (cargado) return;
        cargado = true;

        objetos = new HashSet<string>();
        fragmentos = new HashSet<string>();

        CargarConjunto(ClaveObjetos, objetos);
        CargarConjunto(ClaveFragmentos, fragmentos);
    }

    private static void CargarConjunto(string clave, HashSet<string> destino)
    {
        string bruto = PlayerPrefs.GetString(clave, string.Empty);
        if (string.IsNullOrEmpty(bruto)) return;

        string[] partes = bruto.Split(Separador);
        for (int i = 0; i < partes.Length; i++)
        {
            if (!string.IsNullOrEmpty(partes[i])) destino.Add(partes[i]);
        }
    }

    // ----------------------------------------------------------------
    // RECURSOS
    // ----------------------------------------------------------------

    public static int CristalesVoto
    {
        get => PlayerPrefs.GetInt(ClaveCristalesVoto, 0);
        set => EscribirRecurso(ClaveCristalesVoto, value);
    }

    public static int OroPuro
    {
        get => PlayerPrefs.GetInt(ClaveOroPuro, 0);
        set => EscribirRecurso(ClaveOroPuro, value);
    }

    public static int Esencias
    {
        get => PlayerPrefs.GetInt(ClaveEsencias, 0);
        set => EscribirRecurso(ClaveEsencias, value);
    }

    /// <summary>Suma (o resta, con cantidad negativa) un recurso respetando el mínimo 0.</summary>
    public static void SumarRecurso(string clave, int cantidad)
    {
        if (string.IsNullOrEmpty(clave) || cantidad == 0) return;
        PlayerPrefs.SetInt(clave, Mathf.Max(0, PlayerPrefs.GetInt(clave, 0) + cantidad));
        PlayerPrefs.Save();
        OnCambio?.Invoke();
    }

    private static void EscribirRecurso(string clave, int valor)
    {
        PlayerPrefs.SetInt(clave, Mathf.Max(0, valor));
        PlayerPrefs.Save();
        OnCambio?.Invoke();
    }

    // ----------------------------------------------------------------
    // LLAVES Y OBJETOS RECOGIDOS
    // ----------------------------------------------------------------

    public static bool EstaRecogido(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        AsegurarCarga();
        return objetos.Contains(id);
    }

    public static void GuardarObjeto(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        AsegurarCarga();
        if (!objetos.Add(id)) return;

        PlayerPrefs.SetString(ClaveObjetos, string.Join(Separador.ToString(), objetos));
        PlayerPrefs.Save();
        OnCambio?.Invoke();
    }

    // ----------------------------------------------------------------
    // FRAGMENTOS DE LORE
    // ----------------------------------------------------------------

    /// <summary>Consulta si un fragmento narrativo ya se leyó en esta partida guardada.</summary>
    public static bool EstaFragmentoLeido(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        AsegurarCarga();
        return fragmentos.Contains(id);
    }

    /// <summary>Persiste un fragmento narrativo leído (idempotente).</summary>
    public static void GuardarFragmento(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        AsegurarCarga();
        if (!fragmentos.Add(id)) return;

        PlayerPrefs.SetString(ClaveFragmentos, string.Join(Separador.ToString(), fragmentos));
        PlayerPrefs.Save();
        OnCambio?.Invoke();
    }

    /// <summary>Número de fragmentos narrativos leídos en la partida guardada.</summary>
    public static int FragmentosLeidos
    {
        get { AsegurarCarga(); return fragmentos.Count; }
    }

    // ----------------------------------------------------------------
    // PROGRESO DE PERSONAJES
    // ----------------------------------------------------------------

    public static bool IllyaDesbloqueada
    {
        get => PlayerPrefs.GetInt(ClaveIllya, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(ClaveIllya, value ? 1 : 0);
            PlayerPrefs.Save();
            OnCambio?.Invoke();
        }
    }

    /// <summary>Número de llaves/objetos persistidos (para el cálculo de progreso).</summary>
    public static int ObjetosRecogidos
    {
        get { AsegurarCarga(); return objetos.Count; }
    }

    // ----------------------------------------------------------------
    // REINICIO
    // ----------------------------------------------------------------

    /// <summary>
    /// Borra todo el progreso guardado. Se invoca en la derrota total de ambos
    /// héroes y en 'Reiniciar Nivel'.
    /// </summary>
    public static void BorrarTodo()
    {
        PlayerPrefs.DeleteKey(ClaveCristalesVoto);
        PlayerPrefs.DeleteKey(ClaveOroPuro);
        PlayerPrefs.DeleteKey(ClaveEsencias);
        PlayerPrefs.DeleteKey(ClaveObjetos);
        PlayerPrefs.DeleteKey(ClaveFragmentos);
        PlayerPrefs.DeleteKey(ClaveIllya);
        PlayerPrefs.Save();

        objetos = new HashSet<string>();
        fragmentos = new HashSet<string>();
        cargado = true;

        OnCambio?.Invoke();
    }
}
