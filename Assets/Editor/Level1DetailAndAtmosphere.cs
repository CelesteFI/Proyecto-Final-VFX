using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class Level1DetailAndAtmosphere : EditorWindow
{
    [MenuItem("Tools/Agregar Detalles y Props de Atmósfera (Nivel 1)")]
    public static void ShowWindow()
    {
        GetWindow<Level1DetailAndAtmosphere>("Props & Atmósfera");
    }

    void OnGUI()
    {
        GUILayout.Label("Decorador de Atmósfera y Props: Nivel 1", EditorStyles.boldLabel);
        GUILayout.Label("Siembra antorchas, monolitos rúnicos, vegetación baja y vapor tóxico sin alterar la arquitectura base.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Añadir Props y Luces al Bosque", GUILayout.Height(45)))
        {
            AgregarDetalles();
        }
    }

    private static void AgregarDetalles()
    {
        GameObject baseLevel = GameObject.Find("Level1_Bosque_Oscuridad_Blockout");
        if (baseLevel == null)
        {
            EditorUtility.DisplayDialog("Aviso", "No se encontró el objeto 'Level1_Bosque_Oscuridad_Blockout' en la escena. Genera primero el nivel base.", "OK");
            return;
        }

        // Limpiar contenedor de detalles previo si existe
        Transform oldDetails = baseLevel.transform.Find("09_Detalles_Y_Props_Adicionales");
        if (oldDetails != null)
        {
            Undo.DestroyObjectImmediate(oldDetails.gameObject);
        }

        GameObject rootProps = new GameObject("09_Detalles_Y_Props_Adicionales");
        rootProps.transform.SetParent(baseLevel.transform);
        Undo.RegisterCreatedObjectUndo(rootProps, "Agregar Props y Atmósfera");

        // Materiales existentes o nuevos de acento
        Material matRoca = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_RocaMuro.mat");
        Material matPuntal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_PiedraRuina.mat");
        Material matMadera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_MaderaTronco.mat");
        Material matFollaje = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_FollajeCopa.mat");
        Material matOro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_ReliquiaOro.mat");

        // Material emisivo para antorchas / fuego
        Material matFuegoEmisivo = CrearMaterialEmisivo("M_Blockout_FuegoEmisivo", new Color(1f, 0.45f, 0.05f));
        // Material para vapor de maná
        Material matVaporMana = CrearMaterialEmisivo("M_Blockout_VaporMana", new Color(0.6f, 0.2f, 0.8f));

        // -------------------------------------------------------------
        // 1. ANTORCHAS DE PIEDRA CON LUZ SUAVE EN SENDEROS Y ARENAS
        // -------------------------------------------------------------
        Vector3[] posAntorchas = new Vector3[]
        {
            new Vector3(-3, 0.5f, -3),  // Spawn
            new Vector3(3, 0.5f, -3),
            new Vector3(-4, 0.5f, 10),  // Sendero A
            new Vector3(4, 0.5f, 10),
            new Vector3(-10, 0.5f, 25), // Arena A entrada
            new Vector3(10, 0.5f, 25),
            new Vector3(35, 0.5f, 35),  // Acceso Escalinata
            new Vector3(35, 0.5f, 41),
            new Vector3(72, 4.0f, 36),  // Puente
            new Vector3(72, 4.0f, 40),
            new Vector3(100, 4.0f, 30), // Arena Boss
            new Vector3(100, 4.0f, 46)
        };

        foreach (var pos in posAntorchas)
        {
            CrearAntorchaConLuz(rootProps, pos, matRoca, matFuegoEmisivo);
        }

        // -------------------------------------------------------------
        // 2. MONOLITOS RÚNICOS Y ESTELAS SAGRADAS
        // -------------------------------------------------------------
        Vector3[] posMonolitos = new Vector3[]
        {
            new Vector3(-8, 0, 48),
            new Vector3(8, 0, 48),
            new Vector3(25, 0, 43),
            new Vector3(125, 3.5f, 22),
            new Vector3(125, 3.5f, 54)
        };

        foreach (var m in posMonolitos)
        {
            CrearMonolitoRunico(rootProps, m, matPuntal, matOro);
        }

        // -------------------------------------------------------------
        // 3. MATORRALES Y HIERBA VOLUMÉTRICA A RAS DE SUELO
        // -------------------------------------------------------------
        Vector3[] posHierba = new Vector3[]
        {
            new Vector3(-2, 0.2f, 5), new Vector3(3, 0.2f, 8),
            new Vector3(-8, 0.2f, 30), new Vector3(9, 0.2f, 32),
            new Vector3(-5, 0.2f, 46), new Vector3(6, 0.2f, 47),
            new Vector3(20, 0.2f, 34), new Vector3(28, 0.2f, 42),
            new Vector3(50, 3.7f, 32), new Vector3(62, 3.7f, 44),
            new Vector3(108, 3.7f, 25), new Vector3(118, 3.7f, 50)
        };

        foreach (var h in posHierba)
        {
            CrearBloque(rootProps, "Patch_Hierba_Baja", h, new Vector3(2.5f, 0.4f, 2.5f), matFollaje);
        }

        // -------------------------------------------------------------
        // 4. EMANACIONES FLOTANTES SOBRE LAGOS DE MANÁ MUERTO
        // -------------------------------------------------------------
        CrearBloque(rootProps, "Vapor_Mana_Norte_1", new Vector3(35, 1.5f, 70), new Vector3(15, 1.5f, 15), matVaporMana);
        CrearBloque(rootProps, "Vapor_Mana_Norte_2", new Vector3(55, 2.0f, 80), new Vector3(20, 2f, 12), matVaporMana);
        CrearBloque(rootProps, "Vapor_Mana_Sur_1", new Vector3(25, 1.5f, 5), new Vector3(16, 1.5f, 12), matVaporMana);

        Debug.Log("¡Detalles, antorchas y atmósfera integrados con éxito!");
    }

    private static Material CrearMaterialEmisivo(string nombre, Color color)
    {
        string path = $"Assets/Materials/BlockoutColors/{nombre}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");

            mat = new Material(s);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            // Configurar Emisión para brillo nocturno
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2.2f);

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }

    private static void CrearAntorchaConLuz(GameObject parent, Vector3 pos, Material matBase, Material matFuego)
    {
        GameObject goAntorcha = new GameObject("Antorcha_Piedra");
        goAntorcha.transform.SetParent(parent.transform);
        goAntorcha.transform.position = pos;

        // Pilar base de piedra
        CrearBloque(goAntorcha, "Fuste_Antorcha", new Vector3(pos.x, pos.y + 0.8f, pos.z), new Vector3(0.6f, 1.6f, 0.6f), matBase);
        // Plato superior
        CrearBloque(goAntorcha, "Plato_Antorcha", new Vector3(pos.x, pos.y + 1.65f, pos.z), new Vector3(1f, 0.25f, 1f), matBase);
        // Brasa brillante
        CrearBloque(goAntorcha, "Fuego_Brasa", new Vector3(pos.x, pos.y + 1.9f, pos.z), new Vector3(0.5f, 0.4f, 0.5f), matFuego);

        // Luz puntual de apoyo para que ilumine el suelo
        GameObject goLight = new GameObject("Luz_Fuego");
        goLight.transform.SetParent(goAntorcha.transform);
        goLight.transform.position = new Vector3(pos.x, pos.y + 2.1f, pos.z);
        Light l = goLight.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.6f, 0.2f);
        l.range = 8f;
        l.intensity = 1.8f;
    }

    private static void CrearMonolitoRunico(GameObject parent, Vector3 pos, Material matPiedra, Material matRuna)
    {
        GameObject goMonolito = new GameObject("Monolito_Ancestral");
        goMonolito.transform.SetParent(parent.transform);

        // Obelisco vertical
        CrearBloque(goMonolito, "Cuerpo_Monolito", new Vector3(pos.x, pos.y + 2.5f, pos.z), new Vector3(1.6f, 5f, 1.6f), matPiedra);
        // Anillo rúnico incrustado
        CrearBloque(goMonolito, "Runa_Central", new Vector3(pos.x, pos.y + 3.5f, pos.z), new Vector3(1.8f, 0.8f, 1.8f), matRuna);
    }

    private static ProBuilderMesh CrearBloque(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
        pbMesh.name = name;
        pbMesh.transform.SetParent(parent.transform);
        pbMesh.transform.localScale = size;
        pbMesh.transform.position = pos;

        if (mat != null)
        {
            pbMesh.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        MeshCollider col = pbMesh.gameObject.GetComponent<MeshCollider>();
        if (col == null) col = pbMesh.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;

        return pbMesh;
    }
}