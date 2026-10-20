using UnityEngine;
using System.Collections.Generic;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Progreso de Personajes")]
    public bool illyaDesbloqueada = false;
    public bool zephyrLiberada = false;

    [Header("Progreso del Nivel 1")]
    public bool puzzleRaicesNivel1Resuelto = false;
    public bool bossNivel1Derrotado = false;
    public bool atajoSpawnNivel1Abierto = false;

    [Header("Progreso del Nivel 2")]
    public bool puzzleTemploNivel2Resuelto = false;
    public bool bossNivel2Derrotado = false;

    private HashSet<string> objetosRecogidos = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegistrarCofreOItem(string idUnico)
    {
        if (!objetosRecogidos.Contains(idUnico))
        {
            objetosRecogidos.Add(idUnico);
        }
    }

    public bool EstaRecogido(string idUnico)
    {
        return objetosRecogidos.Contains(idUnico);
    }
}