using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class Level2WindIslandsBuilder : EditorWindow
{
    [MenuItem("Tools/Generar Nivel 2 (Islas del Viento - Flotante)")]
    public static void ShowWindow()
    {
        GetWindow<Level2WindIslandsBuilder>("Nivel 2 Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Generador Nivel 2: La Isla del Viento", EditorStyles.boldLabel);
        GUILayout.Label("Construye el archipiélago vertical con islas flotantes, templos, corrientes de viento y abismo.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Construir Nivel 2 (Archipiélago Flotante)", GUILayout.Height(45)))
        {
            GenerarNivel2();
        }
    }

    private static void GenerarNivel2()
    {
        // 1. Limpiar versión previa si existe
        GameObject oldRoot = GameObject.Find("Level2_Isla_Del_Viento_Blockout");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        GameObject root = new GameObject("Level2_Isla_Del_Viento_Blockout");
        Undo.RegisterCreatedObjectUndo(root, "Generar Nivel 2");

        // 2. Materiales temáticos para el Nivel 2
        Material matRocaCeleste = ObtenerMaterial("M_L2_RocaCeleste", new Color(0.38f, 0.45f, 0.52f));
        Material matTierraViento = ObtenerMaterial("M_L2_TierraPrado", new Color(0.25f, 0.40f, 0.30f));
        Material matVientoCian = ObtenerMaterialEmisivo("M_L2_VientoCorriente", new Color(0.2f, 0.8f, 0.9f), 1.8f);
        Material matRuina = ObtenerMaterial("M_L2_TemploBlanco", new Color(0.70f, 0.72f, 0.75f));
        Material matOro = ObtenerMaterial("M_L2_ReliquiaOro", new Color(0.9f, 0.75f, 0.2f));

        // ---------------------------------------------------------------------
        // ISLA 1: RISCOS DEL SUR (Punto de Spawn) - Y = 0
        // ---------------------------------------------------------------------
        GameObject secSpawn = CrearContenedor(root, "01_Isla_Spawn_RiscosDelSur");
        CrearIslaFlotante(secSpawn, "Isla_Spawn", new Vector3(0, 0, 0), 16f, 10f, matTierraViento, matRocaCeleste);
        // Ruina del portal de viento derruido
        CrearBloque(secSpawn, "Columna_Portal_1", new Vector3(-4, 3, -4), new Vector3(1.6f, 6, 1.6f), matRuina);
        CrearBloque(secSpawn, "Columna_Portal_2", new Vector3(4, 3, -4), new Vector3(1.6f, 6, 1.6f), matRuina);
        CrearBloque(secSpawn, "Dintel_Portal", new Vector3(0, 6.5f, -4), new Vector3(10, 1.2f, 2), matRuina);

        // ---------------------------------------------------------------------
        // CONEXIÓN 1: PUENTE DE VIENTO / CORRIENTE DE AIRE (Spawn -> Arena A)
        // ---------------------------------------------------------------------
        GameObject secPuente1 = CrearContenedor(root, "Conexion_PuenteViento_1");
        // Bloque visual de corriente translúcida
        CrearBloque(secPuente1, "Puente_Viento_Visual", new Vector3(0, 1f, 16), new Vector3(4, 0.4f, 16), matVientoCian);
        // Plataformas intermedias de piedra flotante
        CrearBloque(secPuente1, "Pedestal_Flotante_1", new Vector3(0, 1.5f, 16), new Vector3(3, 1, 3), matRocaCeleste);

        // ---------------------------------------------------------------------
        // ISLA 2: DESFILADERO SÓNICO (Arena de Combate A) - Y = 3
        // ---------------------------------------------------------------------
        GameObject secArenaA = CrearContenedor(root, "02_Isla_DesfiladeroSonico_ArenaA");
        CrearIslaFlotante(secArenaA, "Isla_ArenaA", new Vector3(0, 3, 36), 26f, 14f, matTierraViento, matRocaCeleste);
        // Coberturas y monolitos eólicos
        CrearColumna(secArenaA, new Vector3(-8, 3, 30), 6f, matRuina);
        CrearColumna(secArenaA, new Vector3(8, 3, 30), 7f, matRuina);
        CrearColumna(secArenaA, new Vector3(-7, 3, 42), 5f, matRuina);
        CrearColumna(secArenaA, new Vector3(7, 3, 42), 6f, matRuina);
        // Anillo rúnico central de combate
        CrearBloque(secArenaA, "Pilar_Resonancia", new Vector3(0, 4.5f, 36), new Vector3(2.5f, 3f, 2.5f), matRuina);

        // ---------------------------------------------------------------------
        // ISLA SECRETA OPCIONAL: AGUJA LATERAL CON RELIQUIA - Y = 5
        // ---------------------------------------------------------------------
        GameObject secSecreto = CrearContenedor(root, "03_Isla_Aguja_ReliquiaSecreta");
        CrearIslaFlotante(secSecreto, "Isla_Aguja", new Vector3(-24, 5, 36), 9f, 12f, matTierraViento, matRocaCeleste);
        // Paso angosto hacia el secreto
        CrearBloque(secSecreto, "Paso_Roca_1", new Vector3(-15, 4.5f, 36), new Vector3(2.5f, 0.8f, 2.5f), matRocaCeleste);
        CrearBloque(secSecreto, "Cofre_Viento", new Vector3(-24, 6f, 36), new Vector3(1.5f, 1.2f, 1.2f), matOro);

        // ---------------------------------------------------------------------
        // ISLA 3: ARCHIPIÉLAGO CENTRAL & SANTUARIO DE ZEPHYR (Puzzle Viento-Madera) - Y = 7
        // ---------------------------------------------------------------------
        GameObject secTemplo = CrearContenedor(root, "04_Isla_Templo_Central_Puzzle");
        CrearIslaFlotante(secTemplo, "Isla_Templo", new Vector3(26, 7, 48), 28f, 16f, matTierraViento, matRocaCeleste);

        // Estructura del Templo del Viento
        CrearBloque(secTemplo, "Templo_Piso_Elevado", new Vector3(26, 8.5f, 52), new Vector3(14, 1, 14), matRuina);
        for (int x = -5; x <= 5; x += 5)
        {
            CrearColumna(secTemplo, new Vector3(26 + x, 9, 46), 5f, matRuina);
            CrearColumna(secTemplo, new Vector3(26 + x, 9, 58), 5f, matRuina);
        }
        CrearBloque(secTemplo, "Techo_Templo", new Vector3(26, 14.5f, 52), new Vector3(16, 1, 16), matRuina);

        // Bloqueos de raíces voraces mecánicas cruzadas con viento
        CrearBloque(secTemplo, "Puzzle_Enredadera_Madera", new Vector3(26, 8, 42), new Vector3(6, 4, 2), matTierraViento);
        CrearBloque(secTemplo, "Pedestal_Canalizador_Zephyr", new Vector3(26, 9.5f, 52), new Vector3(2, 1.5f, 2), matOro);

        // ---------------------------------------------------------------------
        // CONEXIÓN 2: RAMPA DE SALTO SÓNICO HACIA EL JEFE
        // ---------------------------------------------------------------------
        GameObject secUpdraft = CrearContenedor(root, "Conexion_Updraft_Jefe");
        CrearBloque(secUpdraft, "Rampa_Viento_Impulso", new Vector3(26, 8, 70), new Vector3(5, 1, 16), matVientoCian);

        // ---------------------------------------------------------------------
        // ISLA 4: ARENA DEL JEFE "OJO DE LA TEMPESTAD" - Y = 12
        // ---------------------------------------------------------------------
        GameObject secBoss = CrearContenedor(root, "05_Isla_Boss_OjoDeLaTempestad");
        CrearIslaFlotante(secBoss, "Isla_Boss_Arena", new Vector3(26, 12, 102), 36f, 22f, matTierraViento, matRocaCeleste);

        // Altar central de la tormenta
        CrearBloque(secBoss, "Altar_Central_Tormenta", new Vector3(26, 13f, 102), new Vector3(12, 0.8f, 12), matOro);

        // Monolitos rituales que encierran la arena
        float radio = 15f;
        for (int a = 0; a < 360; a += 45)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector3 p = new Vector3(26 + Mathf.Cos(rad) * radio, 12.5f, 102 + Mathf.Sin(rad) * radio);
            CrearColumna(secBoss, p, 7f, matRuina);
        }

        // ---------------------------------------------------------------------
        // GESTIÓN DE VACÍO / ABISMO (Killzone Trigger a Y = -12)
        // ---------------------------------------------------------------------
        GameObject secAbismo = CrearContenedor(root, "00_KillZone_Abismo");
        CrearKillzoneTrigger(secAbismo, new Vector3(10, -12, 50), new Vector3(180, 2, 180));

        // ---------------------------------------------------------------------
        // POSICIONAR AL JUGADOR EN EL SPAWN
        // ---------------------------------------------------------------------
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Posicionar Player N2");
            player.transform.position = new Vector3(0, 2f, 0);
            player.transform.rotation = Quaternion.identity;
        }

        Selection.activeGameObject = root;
        Debug.Log("¡Nivel 2: La Isla del Viento generado con arquitectura optimizada y sin errores!");
    }

    // -------------------------------------------------------------------------
    // MÉTODOS AUXILIARES Y CONSTRUCTORES
    // -------------------------------------------------------------------------

    private static GameObject CrearContenedor(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    private static void CrearIslaFlotante(GameObject parent, string name, Vector3 pos, float diametro, float profundidad, Material matPlataforma, Material matConoRoca)
    {
        GameObject goIsla = new GameObject(name);
        goIsla.transform.SetParent(parent.transform);
        goIsla.transform.position = pos;

        // Plataforma superior caminable
        CrearBloque(goIsla, "Superficie_Caminable", pos + new Vector3(0, 0.5f, 0), new Vector3(diametro, 1f, diametro), matPlataforma);

        // Quilla rocosa inferior (cono escalonado)
        CrearBloque(goIsla, "Quilla_Roca_Media", pos + new Vector3(0, -profundidad * 0.25f, 0), new Vector3(diametro * 0.75f, profundidad * 0.5f, diametro * 0.75f), matConoRoca);
        CrearBloque(goIsla, "Punta_Roca_Abisal", pos + new Vector3(0, -profundidad * 0.65f, 0), new Vector3(diametro * 0.4f, profundidad * 0.4f, diametro * 0.4f), matConoRoca);
    }

    private static void CrearKillzoneTrigger(GameObject parent, Vector3 pos, Vector3 size)
    {
        GameObject killBox = new GameObject("KillZone_Trigger_Abismo");
        killBox.transform.SetParent(parent.transform);
        killBox.transform.position = pos;

        BoxCollider col = killBox.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = true;
    }

    private static ProBuilderMesh CrearBloque(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
        pbMesh.name = name;
        pbMesh.transform.SetParent(parent.transform);
        pbMesh.transform.localScale = size;
        pbMesh.transform.position = pos;

        if (mat != null) pbMesh.GetComponent<MeshRenderer>().sharedMaterial = mat;

        pbMesh.ToMesh();
        pbMesh.Refresh();

        MeshCollider col = pbMesh.gameObject.GetComponent<MeshCollider>();
        if (col == null) col = pbMesh.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;

        return pbMesh;
    }

    private static void CrearColumna(GameObject parent, Vector3 basePos, float altura, Material mat)
    {
        CrearBloque(parent, "Columna_Eolica", new Vector3(basePos.x, basePos.y + (altura * 0.5f), basePos.z), new Vector3(1.8f, altura, 1.8f), mat);
    }

    private static Material ObtenerMaterial(string nombre, Color color)
    {
        string folder = "Assets/Materials/BlockoutColors";
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Materials", "BlockoutColors");

        string path = $"{folder}/{nombre}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            mat = new Material(s);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }

    private static Material ObtenerMaterialEmisivo(string nombre, Color color, float intensidad)
    {
        string folder = "Assets/Materials/BlockoutColors";
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Materials", "BlockoutColors");

        string path = $"{folder}/{nombre}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            mat = new Material(s);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * intensidad);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }
}