using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class Level1AdvancedDecor : EditorWindow
{
    [MenuItem("Tools/Agregar Decoración Avanzada (Ruinas y Variación Forestal)")]
    public static void ShowWindow()
    {
        GetWindow<Level1AdvancedDecor>("Ruinas y Bosque");
    }

    void OnGUI()
    {
        GUILayout.Label("Decorador Avanzado: Nivel 1", EditorStyles.boldLabel);
        GUILayout.Label("Añade arcos quebrados, plataformas de salto, variedad de copas de árboles y marcadores de arena.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Generar Ruinas y Bosque Denso", GUILayout.Height(45)))
        {
            GenerarDecoracion();
        }
    }

    private static void GenerarDecoracion()
    {
        GameObject baseLevel = GameObject.Find("Level1_Bosque_Oscuridad_Blockout");
        if (baseLevel == null)
        {
            EditorUtility.DisplayDialog("Aviso", "No se encontró el nivel base en la escena.", "OK");
            return;
        }

        // Limpiar si ya se había ejecutado
        Transform oldDecor = baseLevel.transform.Find("10_Decoraciones_Avanzadas");
        if (oldDecor != null)
        {
            Undo.DestroyObjectImmediate(oldDecor.gameObject);
        }

        GameObject rootDecor = new GameObject("10_Decoraciones_Avanzadas");
        rootDecor.transform.SetParent(baseLevel.transform);
        Undo.RegisterCreatedObjectUndo(rootDecor, "Generar Ruinas y Bosque");

        // Cargar materiales existentes
        Material matMuro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_RocaMuro.mat");
        Material matPuntal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_PiedraRuina.mat");
        Material matFollaje = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_FollajeCopa.mat");
        Material matMadera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_MaderaTronco.mat");
        Material matOro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_ReliquiaOro.mat");

        // ---------------------------------------------------------------------
        // 1. ARCOS MONUMENTALES QUEBRADOS (Enmarcan el paso del jugador)
        // ---------------------------------------------------------------------
        // Arco de entrada a Arena A
        CrearArcoRuina(rootDecor, new Vector3(0, 0, 24), matPuntal);
        // Arco de entrada a Arena B
        CrearArcoRuina(rootDecor, new Vector3(44, 3.5f, 38), matPuntal);

        // ---------------------------------------------------------------------
        // 2. PLATAFORMAS DE ESCALADA / PARKOUR (Hacia el risco secreto)
        // ---------------------------------------------------------------------
        CrearBloque(rootDecor, "Roca_Salto_1", new Vector3(-13, 0.8f, 18), new Vector3(3, 1.2f, 3), matMuro);
        CrearBloque(rootDecor, "Roca_Salto_2", new Vector3(-16, 2.0f, 21), new Vector3(3, 1.2f, 3), matMuro);
        CrearBloque(rootDecor, "Roca_Salto_3", new Vector3(-19, 3.2f, 25), new Vector3(3, 1.2f, 3), matMuro);
        // Mirador secreto con cofre
        CrearBloque(rootDecor, "Plataforma_Mirador_Secreto", new Vector3(-22, 4.2f, 28), new Vector3(5, 1, 5), matPuntal);
        CrearBloque(rootDecor, "Cofre_Mirador", new Vector3(-22, 5.2f, 28), new Vector3(1.2f, 1f, 1.2f), matOro);

        // ---------------------------------------------------------------------
        // 3. PUNTOS DE SPAWN DE ENEMIGOS (Preparación para IA NavMesh - Sem 8)
        // ---------------------------------------------------------------------
        Vector3[] puntosEnemigos = new Vector3[]
        {
            new Vector3(-6, 0.1f, 38),   // Arena A
            new Vector3(6, 0.1f, 44),    // Arena A
            new Vector3(25, 0.1f, 38),   // Zona Puzzle
            new Vector3(52, 3.6f, 34),   // Arena B
            new Vector3(64, 3.6f, 42)    // Arena B
        };

        foreach (var p in puntosEnemigos)
        {
            CrearBloque(rootDecor, "Marcador_Spawn_Enemigo", p, new Vector3(2.5f, 0.2f, 2.5f), matMuro);
        }

        // ---------------------------------------------------------------------
        // 4. ÁRBOLES ANCESTRALES GIGANTES (Rompen la cuadrícula con copas altas)
        // ---------------------------------------------------------------------
        Vector3[] arbolesGigantes = new Vector3[]
        {
            new Vector3(-14, 0, 8),
            new Vector3(15, 0, 10),
            new Vector3(-16, 0, 48),
            new Vector3(16, 0, 48),
            new Vector3(35, 0, 20),
            new Vector3(35, 0, 56),
            new Vector3(95, 3.5f, 20),
            new Vector3(95, 3.5f, 56),
            new Vector3(135, 3.5f, 18),
            new Vector3(135, 3.5f, 58)
        };

        foreach (var pos in arbolesGigantes)
        {
            CrearArbolColosal(rootDecor, pos, matMadera, matFollaje);
        }

        Debug.Log("¡Ruinas, plataformas de escalada y árboles colosales agregados!");
    }

    private static void CrearArcoRuina(GameObject parent, Vector3 centro, Material mat)
    {
        GameObject goArco = new GameObject("Arco_Ruina_Colapsado");
        goArco.transform.SetParent(parent.transform);

        // Pilar izquierdo
        CrearBloque(goArco, "Columna_Izq", centro + new Vector3(-3.5f, 3f, 0), new Vector3(1.8f, 6f, 1.8f), mat);
        // Pilar derecho (quebrado a menor altura)
        CrearBloque(goArco, "Columna_Der_Quebrada", centro + new Vector3(3.5f, 2f, 0), new Vector3(1.8f, 4f, 1.8f), mat);
        // Viga superior inclinada/caída
        GameObject viga = CrearBloque(goArco, "Dintel_Inclinado", centro + new Vector3(0, 5.5f, 0), new Vector3(8.5f, 1.2f, 2f), mat).gameObject;
        viga.transform.rotation = Quaternion.Euler(0, 0, -8);
    }

    private static void CrearArbolColosal(GameObject parent, Vector3 basePos, Material matTronco, Material matCopa)
    {
        GameObject go = new GameObject("Arbol_Colosal");
        go.transform.SetParent(parent.transform);

        // Tronco grueso de 9 metros
        CrearBloque(go, "Tronco_Fuerte", basePos + new Vector3(0, 4.5f, 0), new Vector3(2.5f, 9f, 2.5f), matTronco);
        // Doble copa escalonada
        CrearBloque(go, "Copa_Inferior", basePos + new Vector3(0, 9f, 0), new Vector3(8f, 4f, 8f), matCopa);
        GameObject copaSup = CrearBloque(go, "Copa_Superior", basePos + new Vector3(0, 12f, 0), new Vector3(5.5f, 3.5f, 5.5f), matCopa).gameObject;
        copaSup.transform.rotation = Quaternion.Euler(0, 45, 0); // Rotación a 45 grados para romper la forma de caja
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