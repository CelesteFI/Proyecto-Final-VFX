using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Progreso de la partida (personajes, puzles, jefes y objetos recogidos).
/// Persiste el progreso en disco mediante GameSaveState (PlayerPrefs) para que
/// sobreviva a salir y volver a entrar, y expone un porcentaje para el menú de
/// pausa. Se reinicia a cero en la derrota total y en 'Reiniciar Nivel'.
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Progreso de Personajes")]
    public bool illyaDesbloqueada = false;
    public bool zephyrLiberada = false;

    [Tooltip("El altar del Nivel 1 entrega el relevo: en el Nivel 2 la jugadora activa es Illya y Caelen queda como reserva.")]
    public bool illyaActivaEnNivel2 = false;

    [Header("Progreso del Nivel 1")]
    [Tooltip("10 % · Primer enemigo de la Arena A derrotado.")]
    public bool enemigoNivel1Derrotado = false;

    [Tooltip("20 % · Puzle 1 (raíces corruptas) destruido.")]
    public bool puzzleRaicesNivel1Resuelto = false;

    [Tooltip("40 % · Minijefe del Disco Solar derrotado.")]
    public bool bossNivel1Derrotado = false;
    public bool atajoSpawnNivel1Abierto = false;

    [Header("Progreso del Nivel 2")]
    [Tooltip("60 % · Primer enemigo del Nivel 2 derrotado.")]
    public bool enemigoNivel2Derrotado = false;
    public bool puzzleTemploNivel2Resuelto = false;

    [Tooltip("90 % · Guardián Celeste derrotado.")]
    public bool bossNivel2Derrotado = false;

    [Header("Hitos de Flujo y Narrativa")]
    [Tooltip("70 % · Tobera eólica de la Isla Spawn activada.")]
    public bool toberaEolicaActivada = false;

    [Tooltip("80 % · Puzle 2 (pilares / monolito de sincronización) resuelto.")]
    public bool monolitoNivel2Sincronizado = false;

    [Tooltip("100 % · Altar del Viento purificado.")]
    public bool altarPurificadoNivel2 = false;

    private HashSet<string> objetosRecogidos = new HashSet<string>();
    private readonly HashSet<string> fragmentosLeidos = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        objetosRecogidos = new HashSet<string>();

        // El desbloqueo de Illya viaja con la partida guardada.
        illyaDesbloqueada = illyaDesbloqueada || GameSaveState.IllyaDesbloqueada;
    }

    /// <summary>Marca un cofre, llave o ítem como recogido y lo persiste en disco.</summary>
    public void RegistrarCofreOItem(string idUnico)
    {
        if (string.IsNullOrEmpty(idUnico)) return;
        if (!objetosRecogidos.Add(idUnico)) return;

        GameSaveState.GuardarObjeto(idUnico);
    }

    /// <summary>Consulta el estado combinado de memoria y de la partida guardada.</summary>
    public bool EstaRecogido(string idUnico)
    {
        if (string.IsNullOrEmpty(idUnico)) return false;
        return objetosRecogidos.Contains(idUnico) || GameSaveState.EstaRecogido(idUnico);
    }

    // ----------------------------------------------------------------
    // FRAGMENTOS DE LORE
    // ----------------------------------------------------------------

    /// <summary>Registra un fragmento narrativo leído y lo persiste en disco.</summary>
    public void RegistrarFragmento(string idFragmento)
    {
        if (string.IsNullOrEmpty(idFragmento)) return;
        fragmentosLeidos.Add(idFragmento);
        GameSaveState.GuardarFragmento(idFragmento);
    }

    /// <summary>Consulta combinada de memoria y partida guardada.</summary>
    public bool EstaFragmentoLeido(string idFragmento)
    {
        if (string.IsNullOrEmpty(idFragmento)) return false;
        return fragmentosLeidos.Contains(idFragmento) || GameSaveState.EstaFragmentoLeido(idFragmento);
    }

    /// <summary>Total de fragmentos narrativos leídos (memoria o disco).</summary>
    public int FragmentosLeidos => Mathf.Max(fragmentosLeidos.Count, GameSaveState.FragmentosLeidos);

    /// <summary>Habilita el relevo a Illya y lo persiste.</summary>
    public void DesbloquearIllya(bool desbloqueada)
    {
        illyaDesbloqueada = desbloqueada;
        GameSaveState.IllyaDesbloqueada = desbloqueada;
    }

    /// <summary>
    /// Deja constancia de que el Nivel 1 terminó con Illya al frente: en el Nivel 2 ella
    /// arranca como jugadora activa y Caelen como reserva.
    /// </summary>
    public void MarcarIllyaAlFrenteEnNivel2()
    {
        illyaActivaEnNivel2 = true;
        DesbloquearIllya(true);

        Debug.Log("[PROGRESO] Relevo registrado: en el Nivel 2 la jugadora activa es Illya y Caelen queda como reserva.");
    }

    /// <summary>Marca la tobera eólica del Nivel 2 como activada (hito del 70 %).</summary>
    public void MarcarToberaEolica()
    {
        toberaEolicaActivada = true;
    }

    /// <summary>Marca el monolito sellado del Nivel 2 como sincronizado (hito del 80 %).</summary>
    public void MarcarMonolitoSincronizado()
    {
        monolitoNivel2Sincronizado = true;
    }

    /// <summary>
    /// Marca el hito de muerte indicado. Lo comparten HitoMuerte (una entidad) y
    /// GestorOleada (un grupo de enemigos que cuenta bajas).
    /// </summary>
    public void MarcarHito(HitoJuego hito)
    {
        switch (hito)
        {
            case HitoJuego.EnemigoNivel1:
                enemigoNivel1Derrotado = true;
                break;

            case HitoJuego.JefeNivel1:
                bossNivel1Derrotado = true;
                break;

            case HitoJuego.EnemigoNivel2:
                enemigoNivel2Derrotado = true;
                break;

            case HitoJuego.GuardianNivel2:
                bossNivel2Derrotado = true;
                break;

            case HitoJuego.AltarNivel2:
                altarPurificadoNivel2 = true;
                break;
        }
    }

    /// <summary>
    /// Porcentaje global de avance, de 0 % a 100 %:
    /// Nivel 1 · 10 enemigo de Arena A · 20 raíces · 30 los 3 tótems ·
    ///           40 minijefe del Disco Solar · 50 Illya liberada.
    /// Nivel 2 · 60 primer enemigo · 70 tobera eólica · 80 pilares ·
    ///           90 Guardián Celeste · 100 altar purificado.
    /// Cada hito solo puede subir el porcentaje, nunca bajarlo.
    /// </summary>
    public int ProgresoPorcentaje
    {
        get
        {
            int progreso = 0;

            if (enemigoNivel1Derrotado) progreso = Mathf.Max(progreso, 10);
            if (puzzleRaicesNivel1Resuelto) progreso = Mathf.Max(progreso, 20);
            if (FragmentosLeidos >= 3) progreso = Mathf.Max(progreso, 30);
            if (bossNivel1Derrotado) progreso = Mathf.Max(progreso, 40);
            if (illyaDesbloqueada) progreso = Mathf.Max(progreso, 50);

            if (enemigoNivel2Derrotado) progreso = Mathf.Max(progreso, 60);
            if (toberaEolicaActivada) progreso = Mathf.Max(progreso, 70);
            if (monolitoNivel2Sincronizado) progreso = Mathf.Max(progreso, 80);
            if (bossNivel2Derrotado) progreso = Mathf.Max(progreso, 90);
            if (altarPurificadoNivel2) progreso = Mathf.Max(progreso, 100);

            return Mathf.Clamp(progreso, 0, 100);
        }
    }

    /// <summary>Devuelve todo el progreso a cero (derrota total o reinicio de nivel).</summary>
    public void ResetearProgreso()
    {
        illyaDesbloqueada = false;
        zephyrLiberada = false;
        illyaActivaEnNivel2 = false;
        enemigoNivel1Derrotado = false;
        puzzleRaicesNivel1Resuelto = false;
        bossNivel1Derrotado = false;
        atajoSpawnNivel1Abierto = false;
        enemigoNivel2Derrotado = false;
        puzzleTemploNivel2Resuelto = false;
        bossNivel2Derrotado = false;

        toberaEolicaActivada = false;
        monolitoNivel2Sincronizado = false;
        altarPurificadoNivel2 = false;

        objetosRecogidos.Clear();
        fragmentosLeidos.Clear();
        GameSaveState.BorrarTodo();

        Debug.Log("[PROGRESO] Progreso reiniciado a cero.");
    }
}
