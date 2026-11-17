using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generador procedural de Light Probes para los niveles del proyecto.
///
/// Algoritmo (idéntico para cualquier escena):
///   1. Busca el <see cref="LightProbeGroup"/> de la escena (si no existe, lo crea) y
///      LIMPIA todos los probes existentes: <c>probePositions = new Vector3[0]</c>.
///   2. Calcula el Bounds total de la escena iterando TODOS los MeshRenderer estáticos
///      (islas, suelo, rocas, templos…).
///   3. Expande ese Bounds con un margen (padding) de +3 unidades en X, Y y Z para
///      cubrir saltos y caídas en los bordes.
///   4. Recorre ese volumen tridimensional con ciclos `for` anidados (X, Y, Z) a un
///      espaciado EXACTO de 1.5 unidades.
///   5. Guarda las posiciones en una lista y las asigna a
///      <c>LightProbeGroup.probePositions</c>.
///
/// Menú:  Aura ▸ Light Probes ▸ Regenerar escena activa
///        Aura ▸ Light Probes ▸ Regenerar Nivel 1 y Nivel 2
/// También es invocable por código: <see cref="RegenerarEscenaActiva"/> y
/// <see cref="RegenerarNivelesDelBuild"/>.
/// </summary>
public static class GeneradorLightProbes
{
    /// <summary>Separación exacta entre probes (unidades de mundo).</summary>
    public const float Espaciado = 6f;

    /// <summary>Margen añadido al Bounds de la escena en X, Y y Z.</summary>
    public const float Margen = 3f;

    private const string PrefijoGrupo = "LightProbeGroup_";

    // ------------------------------------------------------------------
    // MENÚ
    // ------------------------------------------------------------------

    [MenuItem("Aura/Light Probes/Regenerar escena activa", false, 10)]
    public static void MenuEscenaActiva()
    {
        Scene escena = SceneManager.GetActiveScene();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[LIGHT PROBES] Regeneración cancelada por el usuario.");
            return;
        }

        InfoRegeneracion info = Regenerar(escena);
        Registrar(info);

        if (info.valido) EditorSceneManager.SaveScene(escena);
    }

    [MenuItem("Aura/Light Probes/Regenerar Nivel 1 y Nivel 2", false, 11)]
    public static void MenuNivelesDelBuild()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[LIGHT PROBES] Regeneración cancelada por el usuario.");
            return;
        }

        RegenerarNivelesDelBuild();
    }

    // ------------------------------------------------------------------
    // API
    // ------------------------------------------------------------------

    /// <summary>Regenera los probes de la escena activa y la guarda.</summary>
    public static InfoRegeneracion RegenerarEscenaActiva()
    {
        Scene escena = SceneManager.GetActiveScene();
        InfoRegeneracion info = Regenerar(escena);
        Registrar(info);
        if (info.valido) EditorSceneManager.SaveScene(escena);
        return info;
    }

    /// <summary>
    /// Regenera los probes de TODAS las escenas del Build Settings (Nivel 1 y Nivel 2),
    /// guardando cada una y devolviendo al final la escena que estaba abierta.
    /// </summary>
    public static List<InfoRegeneracion> RegenerarNivelesDelBuild()
    {
        var resultados = new List<InfoRegeneracion>();

        string rutaOriginal = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
        {
            if (entrada == null || !entrada.enabled || string.IsNullOrEmpty(entrada.path)) continue;

            Scene escena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
            InfoRegeneracion info = Regenerar(escena);
            if (info.valido) EditorSceneManager.SaveScene(escena);
            resultados.Add(info);
            Registrar(info);
        }

        if (!string.IsNullOrEmpty(rutaOriginal) && System.IO.File.Exists(rutaOriginal))
            EditorSceneManager.OpenScene(rutaOriginal, OpenSceneMode.Single);

        return resultados;
    }

    // ------------------------------------------------------------------
    // ALGORITMO
    // ------------------------------------------------------------------

    /// <summary>Ejecuta los cinco pasos sobre la escena indicada (sin guardarla).</summary>
    public static InfoRegeneracion Regenerar(Scene escena)
    {
        var info = new InfoRegeneracion { escena = escena.name, ruta = escena.path };

        // ---- PASO 1: limpiar todos los probes existentes ----
        LightProbeGroup grupo = BuscarOCrearGrupo(escena);
        if (grupo == null)
        {
            Debug.LogError($"[LIGHT PROBES] '{escena.name}': no se pudo obtener ni crear un LightProbeGroup.");
            return info;
        }

        info.grupo = grupo.name;
        info.probesPrevios = grupo.probePositions.Length;

        grupo.probePositions = new Vector3[0];

        // ---- PASO 2: Bounds total de los MeshRenderer estáticos ----
        Bounds volumen = CalcularBoundsEstaticos(escena, out int renderers);
        if (renderers == 0)
        {
            Debug.LogError($"[LIGHT PROBES] '{escena.name}': no hay ningún MeshRenderer estático del que calcular el volumen.");
            return info;
        }

        info.renderersEstaticos = renderers;
        info.boundsSinMargen = volumen;

        // ---- PASO 3: margen de +3 en X, Y y Z ----
        volumen.min -= Vector3.one * Margen;
        volumen.max += Vector3.one * Margen;
        info.boundsConMargen = volumen;

        // ---- PASO 4: rejilla 3D con espaciado exacto de 1.5 ----
        int nx = Mathf.Max(0, Mathf.FloorToInt((volumen.max.x - volumen.min.x) / Espaciado));
        int ny = Mathf.Max(0, Mathf.FloorToInt((volumen.max.y - volumen.min.y) / Espaciado));
        int nz = Mathf.Max(0, Mathf.FloorToInt((volumen.max.z - volumen.min.z) / Espaciado));

        var puntos = new List<Vector3>((nx + 1) * (ny + 1) * (nz + 1));

        for (int ix = 0; ix <= nx; ix++)
        {
            float x = volumen.min.x + ix * Espaciado;

            for (int iy = 0; iy <= ny; iy++)
            {
                float y = volumen.min.y + iy * Espaciado;

                for (int iz = 0; iz <= nz; iz++)
                {
                    float z = volumen.min.z + iz * Espaciado;
                    puntos.Add(new Vector3(x, y, z));
                }
            }
        }

        // ---- PASO 5: asignar la lista al LightProbeGroup ----
        grupo.probePositions = puntos.ToArray();

        info.probes = puntos.Count;
        info.dimensiones = new Vector3Int(nx + 1, ny + 1, nz + 1);
        info.valido = true;

        EditorUtility.SetDirty(grupo);
        if (escena.IsValid()) EditorSceneManager.MarkSceneDirty(escena);

        // El gizmo del LightProbeGroup dibuja un punto por probe: con rejillas densas
        // conviene no dejarlo seleccionado al terminar.
        if (Selection.activeGameObject == grupo.gameObject) Selection.activeGameObject = null;

        return info;
    }

    /// <summary>Devuelve el LightProbeGroup de la escena; si no hay ninguno, crea uno.</summary>
    private static LightProbeGroup BuscarOCrearGrupo(Scene escena)
    {
        LightProbeGroup encontrado = null;

        if (escena.IsValid())
        {
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                LightProbeGroup g = raiz.GetComponentInChildren<LightProbeGroup>(true);
                if (g != null) { encontrado = g; break; }
            }
        }

        if (encontrado == null)
            encontrado = Object.FindFirstObjectByType<LightProbeGroup>(FindObjectsInactive.Include);

        if (encontrado != null) return encontrado;

        string nombre = PrefijoGrupo + (escena.IsValid() ? escena.name.Replace(" ", "_") : "Escena");
        var nuevo = new GameObject(nombre, typeof(LightProbeGroup));

        if (escena.IsValid() && nuevo.scene != escena)
            SceneManager.MoveGameObjectToScene(nuevo, escena);

        Debug.Log($"[LIGHT PROBES] No había LightProbeGroup en '{escena.name}': se ha creado '{nombre}' en el origen.");
        return nuevo.GetComponent<LightProbeGroup>();
    }

    /// <summary>
    /// Bounds que envuelve todos los MeshRenderer estáticos de la escena. Se considera
    /// estático cualquier objeto con ContributeGI, BatchingStatic o la marca isStatic.
    /// </summary>
    public static Bounds CalcularBoundsEstaticos(Scene escena, out int renderers)
    {
        renderers = 0;
        Bounds volumen = new Bounds(Vector3.zero, Vector3.zero);
        bool hay = false;

        if (!escena.IsValid()) return volumen;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null) continue;

                GameObject go = mr.gameObject;
                bool estatico = go.isStatic
                                || GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ContributeGI)
                                || GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.BatchingStatic);

                if (!estatico) continue;

                renderers++;

                if (!hay) { volumen = mr.bounds; hay = true; }
                else volumen.Encapsulate(mr.bounds);
            }
        }

        return volumen;
    }

    // ------------------------------------------------------------------
    // SALIDA
    // ------------------------------------------------------------------

    /// <summary>Resumen de una regeneración.</summary>
    public struct InfoRegeneracion
    {
        public string escena;
        public string ruta;
        public string grupo;
        public int probesPrevios;
        public int renderersEstaticos;
        public Bounds boundsSinMargen;
        public Bounds boundsConMargen;
        public Vector3Int dimensiones;
        public int probes;
        public bool valido;

        public string Resumen()
        {
            if (!valido) return $"[LIGHT PROBES] '{escena}': SIN CAMBIOS (ver errores en consola).";

            var sb = new StringBuilder();
            sb.AppendLine($"=== LIGHT PROBES · {escena} ===");
            sb.AppendLine($"Grupo                : {grupo}  ({probesPrevios} probes previos, limpiados)");
            sb.AppendLine($"MeshRenderer estáticos: {renderersEstaticos}");
            sb.AppendLine($"Bounds sin margen    : center {boundsSinMargen.center} size {boundsSinMargen.size}");
            sb.AppendLine($"Bounds con margen +3 : center {boundsConMargen.center} size {boundsConMargen.size}");
            sb.AppendLine($"Rejilla              : {dimensiones.x} x {dimensiones.y} x {dimensiones.z}  (espaciado {Espaciado})");
            sb.AppendLine($"PROBES GENERADOS     : {probes}");
            return sb.ToString().TrimEnd();
        }
    }

    private static void Registrar(InfoRegeneracion info)
    {
        if (info.valido) Debug.Log(info.Resumen());
        else Debug.LogError($"[LIGHT PROBES] '{info.escena}': la regeneración no se completó.");
    }
}
