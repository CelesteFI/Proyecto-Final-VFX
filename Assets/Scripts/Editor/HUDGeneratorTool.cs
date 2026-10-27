using UnityEditor;
using UnityEngine;

/// <summary>
/// UTILIDAD RETIRADA.
/// El HUD de 'Ecos de una Era Olvidada' se compone exclusivamente de GameObjects
/// físicos dentro de 'Canvas_HUD' en la Hierarchy de cada maqueta.
/// La regla de diseño prohíbe generar interfaz desde código, así que este
/// generador ya no construye nada: se conserva la ventana únicamente como
/// recordatorio y para las utilidades de consola.
/// </summary>
public class HUDGeneratorTool : EditorWindow
{
    [MenuItem("Tools/HUD (Retirado - Estructura Física)")]
    public static void ShowWindow()
    {
        GetWindow<HUDGeneratorTool>("HUD Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("HUD de estructura física", EditorStyles.boldLabel);
        GUILayout.Label(
            "La interfaz vive como GameObjects reales bajo 'Canvas_HUD' en cada maqueta.\n\n" +
            "Este generador está retirado: construir UI desde código rompería el árbol " +
            "físico y las referencias [SerializeField] ya cableadas en el Inspector.",
            EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Explicar estado de la estructura", GUILayout.Height(40)))
        {
            Debug.Log(
                "[HUD] Estructura física activa: Pentagram_Selector, Health_Container, " +
                "Abilities_Container, Btn_Recursos, TopRight_Container, Banner_Instrucciones, " +
                "Modal_ArbolTalentos, Modal_Recursos, Modal_Pausa y Modal_Derrota.");
        }
    }

    /// <summary>
    /// Punto de entrada conservado por compatibilidad con los integradores
    /// antiguos. Falla de forma explícita para no destruir el HUD físico.
    /// </summary>
    public static void ConstruirHUD()
    {
        throw new System.InvalidOperationException(
            "HUDGeneratorTool.ConstruirHUD está retirado: el HUD es un árbol de GameObjects " +
            "físicos en 'Canvas_HUD'. Edítalo en la Hierarchy, no por código.");
    }

    public static string RevisarConsola()
    {
        object[] counts = { 0, 0, 0 };
        System.Type entries = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        entries.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
               .Invoke(null, counts);
        return "CONSOLE errors=" + counts[0] + ", warnings=" + counts[1] + ", logs=" + counts[2];
    }

    public static void LimpiarConsolaPrueba()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Limpia la consola antes de Play Mode.");

        System.Type entries = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        entries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
               .Invoke(null, null);
    }
}
