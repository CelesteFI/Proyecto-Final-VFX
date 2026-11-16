using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.IO;

public class Level1BlockoutBuilder : EditorWindow
{
    [MenuItem("Tools/Generar Nivel 1 (Bosque Ultra Detallado + Exterior y Color)")]
    public static void ShowWindow()
    {
        GetWindow<Level1BlockoutBuilder>("Nivel 1 Master");
    }

    void OnGUI()
    {
        GUILayout.Label("Generador Maestro de Blockout: Nivel 1", EditorStyles.boldLabel);
        GUILayout.Label("Construye el Bosque de la Oscuridad con exterior, árboles, lagos, props, ruinas y materiales con paleta de color diferenciada.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Construir Bosque Completo con Colores y Props", GUILayout.Height(50)))
        {
            GenerarNivelMaestro();
        }
    }

    private static void GenerarNivelMaestro()
    {
        // 1. Limpiar versión previa si existe
        GameObject oldRoot = GameObject.Find("Level1_Bosque_Oscuridad_Blockout");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        GameObject root = new GameObject("Level1_Bosque_Oscuridad_Blockout");
        Undo.RegisterCreatedObjectUndo(root, "Generar Nivel 1 Maestro");

        // 2. Generar Paleta de Materiales con Color (URP / Standard)
        Material matSuelo = ObtenerOMaterial("M_Blockout_SueloTierra", new Color(0.18f, 0.26f, 0.20f));      // Verde musgo oscuro
        Material matMuro = ObtenerOMaterial("M_Blockout_RocaMuro", new Color(0.28f, 0.33f, 0.38f));         // Gris pizarra
        Material matMadera = ObtenerOMaterial("M_Blockout_MaderaTronco", new Color(0.32f, 0.22f, 0.15f));   // Café corteza
        Material matFollaje = ObtenerOMaterial("M_Blockout_FollajeCopa", new Color(0.12f, 0.35f, 0.18f));   // Verde follaje
        Material matRaiz = ObtenerOMaterial("M_Blockout_RaizCorrupta", new Color(0.25f, 0.15f, 0.30f));     // Púrpura zarza
        Material matAgua = ObtenerOMaterial("M_Blockout_ManaMuertoAgua", new Color(0.40f, 0.12f, 0.55f));   // Púrpura tóxico brillante
        Material matOro = ObtenerOMaterial("M_Blockout_ReliquiaOro", new Color(0.85f, 0.68f, 0.20f));       // Dorado rúnico
        Material matPuntal = ObtenerOMaterial("M_Blockout_PiedraRuina", new Color(0.45f, 0.48f, 0.52f));    // Gris claro altar

        // ---------------------------------------------------------------------
        // SECCIÓN 0: ENTORNO EXTERIOR (Terreno base y lagos de maná)
        // ---------------------------------------------------------------------
        GameObject secExterior = CrearContenedor(root, "00_Entorno_Exterior");
        // Suelo exterior inmenso para que el nivel no flote en la nada
        CrearBloque(secExterior, "Exterior_Terreno_Base", new Vector3(50, -0.6f, 38), new Vector3(180, 1, 140), matSuelo);

        // Lagos laterales de Maná Muerto / Pantano
        CrearBloque(secExterior, "Lago_Mana_Norte", new Vector3(40, -0.3f, 75), new Vector3(70, 0.8f, 35), matAgua);
        CrearBloque(secExterior, "Lago_Mana_Sur", new Vector3(30, -0.3f, 2), new Vector3(50, 0.8f, 30), matAgua);

        // Cordillera de acantilados perimetrales que enmarcan todo el bosque
        CrearBloque(secExterior, "Risco_Frontera_Norte", new Vector3(50, 8, 100), new Vector3(180, 18, 16), matMuro);
        CrearBloque(secExterior, "Risco_Frontera_Sur", new Vector3(50, 8, -25), new Vector3(180, 18, 16), matMuro);
        CrearBloque(secExterior, "Risco_Frontera_Oeste", new Vector3(-35, 8, 38), new Vector3(16, 18, 140), matMuro);
        CrearBloque(secExterior, "Risco_Frontera_Este", new Vector3(145, 8, 38), new Vector3(16, 18, 140), matMuro);

        // ---------------------------------------------------------------------
        // SECCIÓN 1: SPAWN POINT (Patio del campamento con hoguera y arcos)
        // ---------------------------------------------------------------------
        GameObject secSpawn = CrearContenedor(root, "01_Zona_Spawn");
        CrearBloque(secSpawn, "Spawn_Piso", new Vector3(0, 0, 0), new Vector3(16, 1, 16), matPuntal);
        CrearBloque(secSpawn, "Arco_Entrada_Izq", new Vector3(-4, 3, -6), new Vector3(1.8f, 6, 1.8f), matMuro);
        CrearBloque(secSpawn, "Arco_Entrada_Der", new Vector3(4, 3, -6), new Vector3(1.8f, 6, 1.8f), matMuro);
        CrearBloque(secSpawn, "Arco_Entrada_Dintel", new Vector3(0, 6.5f, -6), new Vector3(10, 1.4f, 2), matMuro);
        CrearBloque(secSpawn, "Muro_Fondo_Spawn", new Vector3(0, 3, -8), new Vector3(18, 6, 2), matMuro);

        // Detalles Spawn: Fogata de cazador y cajas de víveres
        CrearBloque(secSpawn, "Camp_Hoguera_Centro", new Vector3(0, 0.8f, 2), new Vector3(1.6f, 0.6f, 1.6f), matOro);
        CrearCajaProp(secSpawn, new Vector3(-5, 1, -4), new Vector3(1.2f, 1.2f, 1.2f), matMadera);
        CrearCajaProp(secSpawn, new Vector3(-5, 2.2f, -4), new Vector3(1f, 1f, 1f), matMadera);
        CrearCajaProp(secSpawn, new Vector3(-3.5f, 1, -4.5f), new Vector3(1.4f, 1.4f, 1.4f), matMadera);

        // ---------------------------------------------------------------------
        // SECCIÓN 2: SENDERO DEL DESFILADERO (Estrecho con muros y árboles)
        // ---------------------------------------------------------------------
        GameObject secSendero = CrearContenedor(root, "02_Sendero_Desfiladero");
        CrearBloque(secSendero, "Piso_Sendero_1", new Vector3(0, 0, 15), new Vector3(8, 1, 18), matSuelo);
        CrearBloque(secSendero, "Acantilado_Izq", new Vector3(-7, 4, 15), new Vector3(6, 8, 18), matMuro);
        CrearBloque(secSendero, "Acantilado_Der", new Vector3(7, 4, 15), new Vector3(6, 8, 18), matMuro);
        // Tronco caído que obliga a rodar o saltar
        CrearBloque(secSendero, "Tronco_Caido_Bloqueo", new Vector3(0, 1.3f, 16), new Vector3(7.5f, 1.4f, 1.4f), matMadera);

        // ---------------------------------------------------------------------
        // SECCIÓN 3: COMBAT ARENA A (Claro abierto de vegetación y rocas)
        // ---------------------------------------------------------------------
        GameObject secArenaA = CrearContenedor(root, "03_Combat_Arena_A");
        CrearBloque(secArenaA, "ArenaA_Piso", new Vector3(0, 0, 38), new Vector3(32, 1, 32), matSuelo);
        // Muros perimetrales irregulares
        CrearBloque(secArenaA, "Borde_Rocoso_Sur", new Vector3(0, 3, 22), new Vector3(32, 6, 2), matMuro);
        CrearBloque(secArenaA, "Borde_Rocoso_Norte", new Vector3(0, 3, 54), new Vector3(32, 6, 2), matMuro);

        // Coberturas y barricadas en la arena
        CrearBloque(secArenaA, "Roca_Cobertura_1", new Vector3(-6, 1.5f, 34), new Vector3(3.5f, 2.8f, 3.5f), matMuro);
        CrearBloque(secArenaA, "Roca_Cobertura_2", new Vector3(7, 1.2f, 42), new Vector3(4.5f, 2.2f, 2.5f), matMuro);
        CrearBloque(secArenaA, "Columna_Partida_Suelo", new Vector3(-2, 1, 44), new Vector3(2f, 1.8f, 6f), matPuntal);

        // ---------------------------------------------------------------------
        // SECCIÓN 4: LABERINTO SECRETO / PUERTA DE ACCESO DIRECTO
        // ---------------------------------------------------------------------
        GameObject secAtajo = CrearContenedor(root, "04_Atajo_Secreto_Labyrinth");
        CrearBloque(secAtajo, "Atajo_Piso", new Vector3(-24, 0, 38), new Vector3(18, 1, 18), matPuntal);
        CrearBloque(secAtajo, "Muro_Laberinto_Norte", new Vector3(-24, 3, 47), new Vector3(18, 5, 1.5f), matMuro);
        CrearBloque(secAtajo, "Muro_Laberinto_Sur", new Vector3(-24, 3, 29), new Vector3(18, 5, 1.5f), matMuro);
        CrearBloque(secAtajo, "Muro_Laberinto_Oeste", new Vector3(-33, 3, 38), new Vector3(1.5f, 5, 18), matMuro);
        CrearBloque(secAtajo, "Pared_Interna_1", new Vector3(-20, 2.5f, 38), new Vector3(1.5f, 4, 11), matMuro);
        CrearBloque(secAtajo, "Pared_Interna_2", new Vector3(-28, 2.5f, 38), new Vector3(1.5f, 4, 11), matMuro);
        // Pedestal y Cofre de Tesoro Secreto
        CrearBloque(secAtajo, "Pedestal_Cofre", new Vector3(-24, 1, 38), new Vector3(3, 1.5f, 3), matPuntal);
        CrearBloque(secAtajo, "Cofre_Tesoro_Dorado", new Vector3(-24, 2.2f, 38), new Vector3(1.6f, 1.2f, 1.2f), matOro);

        // ---------------------------------------------------------------------
        // SECCIÓN 5: PUZZLE DE RAÍCES CORRUPTAS (Zarzales y columnas)
        // ---------------------------------------------------------------------
        GameObject secPuzzle = CrearContenedor(root, "05_Puzzle_Raices_Corruptas");
        CrearBloque(secPuzzle, "Piso_Puzzle", new Vector3(25, 0, 38), new Vector3(22, 1, 14), matSuelo);
        CrearBloque(secPuzzle, "Desfiladero_Norte", new Vector3(25, 4.5f, 46), new Vector3(22, 8, 3), matMuro);
        CrearBloque(secPuzzle, "Desfiladero_Sur", new Vector3(25, 4.5f, 30), new Vector3(22, 8, 3), matMuro);

        // Bloqueos de raíces voraces
        CrearBloque(secPuzzle, "Raiz_Bloqueo_1", new Vector3(18, 2.5f, 35), new Vector3(3, 4.5f, 7), matRaiz);
        CrearBloque(secPuzzle, "Raiz_Bloqueo_2", new Vector3(24, 3f, 41), new Vector3(3.5f, 5.5f, 6), matRaiz);
        CrearBloque(secPuzzle, "Raiz_Bloqueo_3", new Vector3(30, 2.5f, 36), new Vector3(3, 4.5f, 8), matRaiz);
        CrearBloque(secPuzzle, "Monolito_Sellado_Puzzle", new Vector3(23, 3, 33), new Vector3(2, 6, 2), matPuntal);

        // ---------------------------------------------------------------------
        // SECCIÓN 6: ESCALINATA Y COMBAT ARENA B (Patio de Templo Elevado)
        // ---------------------------------------------------------------------
        GameObject secArenaB = CrearContenedor(root, "06_Combat_Arena_B_Elevada");
        CrearEscalera(secArenaB, "Escalinata_Monumental", new Vector3(40, 0, 38), new Vector3(8, 3.5f, 10), matPuntal);
        CrearBloque(secArenaB, "ArenaB_Piso_Elevado", new Vector3(58, 3.5f, 38), new Vector3(28, 1, 28), matPuntal);

        // Perímetro de columnatas clásicas en ruinas
        for (int i = -11; i <= 11; i += 5)
        {
            CrearColumnaCompleta(secArenaB, new Vector3(47, 3.5f, 38 + i), 6f, matPuntal);
            CrearColumnaCompleta(secArenaB, new Vector3(69, 3.5f, 38 + i), 6f, matPuntal);
        }
        CrearBloque(secArenaB, "Altar_Secundario_Mesa", new Vector3(58, 4.6f, 38), new Vector3(5, 1.4f, 3.5f), matMuro);

        // ---------------------------------------------------------------------
        // SECCIÓN 7: PUENTE FRACTURADO SOBRE EL ABISMO
        // ---------------------------------------------------------------------
        GameObject secPuente = CrearContenedor(root, "07_Puente_Fracturado");
        CrearBloque(secPuente, "Puente_Segmento_1", new Vector3(76, 3.5f, 38), new Vector3(8, 1, 6), matPuntal);
        CrearBloque(secPuente, "Puente_Segmento_2", new Vector3(87, 3.5f, 38), new Vector3(8, 1, 6), matPuntal);
        // Barandales
        CrearBloque(secPuente, "Barandal_Norte_1", new Vector3(76, 4.6f, 41.6f), new Vector3(8, 1.4f, 0.6f), matMuro);
        CrearBloque(secPuente, "Barandal_Sur_1", new Vector3(76, 4.6f, 34.4f), new Vector3(8, 1.4f, 0.6f), matMuro);
        CrearBloque(secPuente, "Barandal_Norte_2", new Vector3(87, 4.6f, 41.6f), new Vector3(8, 1.4f, 0.6f), matMuro);

        // ---------------------------------------------------------------------
        // SECCIÓN 8: EL ALTAR DEL ÁRBOL SOMBRÍO (ARENA FINAL DEL JEFE)
        // ---------------------------------------------------------------------
        GameObject secBoss = CrearContenedor(root, "08_Altar_Arbol_Sombrio_BossArena");
        CrearBloque(secBoss, "BossArena_Piso_Gran_Circo", new Vector3(116, 3.5f, 38), new Vector3(44, 1, 44), matPuntal);

        // Gran Árbol Sombrío Colosal (Base + Troncos ramificados)
        CrearBloque(secBoss, "Arbol_Sombrio_Tronco_Principal", new Vector3(132, 10, 38), new Vector3(14, 19, 14), matMadera);
        CrearBloque(secBoss, "Arbol_Sombrio_Copa_Negra", new Vector3(132, 21, 38), new Vector3(22, 10, 22), matRaiz);
        CrearBloque(secBoss, "Arbol_Sombrio_Raiz_Lateral_1", new Vector3(124, 5, 48), new Vector3(6, 6, 6), matMadera);
        CrearBloque(secBoss, "Arbol_Sombrio_Raiz_Lateral_2", new Vector3(124, 5, 28), new Vector3(6, 6, 6), matMadera);

        // Disco Rúnico Ceremonial Central
        CrearBloque(secBoss, "Disco_Solar_Altar_Runa", new Vector3(116, 4.3f, 38), new Vector3(12, 0.6f, 12), matOro);

        // Monolitos rituales en círculo
        float radio = 18f;
        for (int a = 0; a < 360; a += 45)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector3 posCol = new Vector3(116 + Mathf.Cos(rad) * radio, 3.5f, 38 + Mathf.Sin(rad) * radio);
            CrearColumnaCompleta(secBoss, posCol, 8f, matMuro);
        }

        // ---------------------------------------------------------------------
        // SECCIÓN 9: BOSQUE COMPLETO DE ÁRBOLES Y VEGETACIÓN PROCEDURAL
        // ---------------------------------------------------------------------
        GameObject secArboleda = CrearContenedor(root, "09_Arboles_Y_Arbustos_Bosque");

        // Sembrar árboles estilizados bordeando los caminos
        Vector3[] puntosArboles = new Vector3[]
        {
            new Vector3(-10, 0, 5), new Vector3(10, 0, 5), new Vector3(-8, 0, 22), new Vector3(8, 0, 22),
            new Vector3(-18, 0, 24), new Vector3(-18, 0, 52), new Vector3(18, 0, 24), new Vector3(18, 0, 52),
            new Vector3(-10, 0, 56), new Vector3(8, 0, 56), new Vector3(28, 0, 50), new Vector3(34, 0, 26),
            new Vector3(45, 0, 20), new Vector3(55, 0, 18), new Vector3(70, 0, 22), new Vector3(80, 0, 24),
            new Vector3(45, 0, 56), new Vector3(60, 0, 58), new Vector3(75, 0, 54), new Vector3(90, 0, 56),
            new Vector3(100, 3.5f, 15), new Vector3(125, 3.5f, 14), new Vector3(140, 3.5f, 25),
            new Vector3(100, 3.5f, 60), new Vector3(125, 3.5f, 62), new Vector3(140, 3.5f, 50)
        };

        foreach (var p in puntosArboles)
        {
            CrearArbolEstilizado(secArboleda, p, matMadera, matFollaje);
        }

        // Sembrar grupos de arbustos / rocas en las esquinas
        Vector3[] puntosArbustos = new Vector3[]
        {
            new Vector3(-5, 0.5f, 8), new Vector3(5, 0.5f, 9), new Vector3(-14, 0.5f, 32),
            new Vector3(13, 0.5f, 33), new Vector3(54, 4f, 28), new Vector3(62, 4f, 48),
            new Vector3(102, 4f, 30), new Vector3(102, 4f, 46)
        };

        foreach (var a in puntosArbustos)
        {
            CrearBloque(secArboleda, "Arbusto_Foliage", a, new Vector3(3, 2, 3), matFollaje);
        }

        // ---------------------------------------------------------------------
        // REPOSICIONAR AL JUGADOR
        // ---------------------------------------------------------------------
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Posicionar Player");
            player.transform.position = new Vector3(0, 2.5f, 0);
            player.transform.rotation = Quaternion.identity;
        }

        Selection.activeGameObject = root;
        Debug.Log("¡Nivel 1 Master generado con éxito! Escenario completo con materiales de color, árboles, lagos y props.");
    }

    public static void ConfigurarEntregaNivel1()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Maqueta Nivel 1.unity")
            throw new System.InvalidOperationException("Abre Maqueta Nivel 1 fuera de Play Mode.");

        GameObject player = GameObject.Find("Player");
        GameObject enemy = GameObject.Find("Enemigo");
        GameObject root = GameObject.Find("Level1_Bosque_Oscuridad_Blockout");
        Camera camera = Camera.main;
        if (player == null || enemy == null || root == null || camera == null)
            throw new System.InvalidOperationException("Faltan objetos requeridos de la maqueta.");
        var controller = player.GetComponent<CharacterController>();
        var tagTeam = player.GetComponent<TagTeamManager>();
        var follow = camera.GetComponent<CameraFollow>();
        var body = enemy.GetComponent<Rigidbody>();
        var health = enemy.GetComponent<HealthComponent>();
        var surface = root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
        if (controller == null || player.GetComponent<PlayerController>() == null || tagTeam == null || follow == null || body == null || health == null || surface == null)
            throw new System.InvalidOperationException("Faltan componentes requeridos.");
        Undo.RecordObjects(new Object[] { controller, tagTeam, follow, body, health, player.transform, enemy.transform }, "Configurar entrega Nivel 1");
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.08f;
        controller.center = Vector3.up;
        controller.height = 2f;
        controller.radius = 0.5f;
        player.layer = LayerMask.NameToLayer("Player");
        player.transform.position = new Vector3(0f, 0.6f, 34f);
        player.transform.rotation = Quaternion.identity;
        enemy.layer = LayerMask.NameToLayer("Default");
        enemy.transform.position = new Vector3(0f, 1.6f, 38f);
        body.isKinematic = false;
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        enemy.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Enemigo.mat");

        // Reutilizar la cápsula existente y alinear su imagen con Center Y = 1.
        MeshFilter originalMesh = player.GetComponent<MeshFilter>();
        MeshRenderer originalRenderer = player.GetComponent<MeshRenderer>();
        if (originalMesh != null && originalRenderer != null)
        {
            GameObject visual = new GameObject("Visual_Capsula");
            Undo.RegisterCreatedObjectUndo(visual, "Alinear cápsula existente");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.up;
            visual.layer = player.layer;
            Undo.AddComponent<MeshFilter>(visual).sharedMesh = originalMesh.sharedMesh;
            var renderer = Undo.AddComponent<MeshRenderer>(visual);
            EditorUtility.CopySerialized(originalRenderer, renderer);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_Blockout_PiedraRuina.mat");
            Undo.DestroyObjectImmediate(originalRenderer);
            Undo.DestroyObjectImmediate(originalMesh);
        }
        tagTeam.caelen.currentHealth = tagTeam.caelen.maxHealth;
        tagTeam.illya.currentHealth = tagTeam.illya.maxHealth;
        follow.target = player.transform;
        follow.targetOffset = new Vector3(0f, 1.8f, 0f);
        follow.pivoteMirada = new Vector3(0f, 1.3f, 0f);
        follow.defaultDistance = 4.5f;
        follow.collisionLayers = LayerMask.GetMask("Default", "Environment");
        follow.mouseSensitivity = 0.12f;

        int groundCount = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = root.layer;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            if (collider.isTrigger) continue;
            string name = collider.gameObject.name;
            bool ground = name.Contains("Piso") || name.Contains("Terreno_Base") || name.Contains("Puente_Segmento") || name.Contains("Escalinata") || name.Contains("Escalon") || name.Contains("Rampa");
            var modifier = collider.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
            if (modifier == null) modifier = Undo.AddComponent<Unity.AI.Navigation.NavMeshModifier>(collider.gameObject);
            Undo.RecordObject(modifier, "Clasificar geometría NavMesh");
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName(ground ? "Walkable" : "Not Walkable");
            modifier.applyToChildren = false;
            modifier.ignoreFromBuild = false;
            if (ground) groundCount++;
        }
        Undo.RecordObject(surface, "Hornear NavMesh Nivel 1");
        surface.agentTypeID = UnityEngine.AI.NavMesh.GetSettingsByIndex(0).agentTypeID;
        surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
        surface.layerMask = LayerMask.GetMask("Environment");
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");
        surface.BuildNavMesh();
        if (surface.navMeshData == null || UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length == 0)
            throw new System.InvalidOperationException("El Bake no produjo una malla transitable.");
        const string folder = "Assets/Scenes/Maqueta Nivel 1";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Scenes", "Maqueta Nivel 1");
        const string dataPath = folder + "/NavMesh_Level1.asset";
        var previousData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(dataPath);
        if (previousData == null) AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
        else
        {
            var baked = surface.navMeshData;
            surface.RemoveData();
            EditorUtility.CopySerialized(baked, previousData);
            surface.navMeshData = previousData;
            surface.AddData();
            Object.DestroyImmediate(baked);
            EditorUtility.SetDirty(previousData);
        }
        UnityEngine.AI.NavMeshHit hit;
        if (!UnityEngine.AI.NavMesh.SamplePosition(new Vector3(0f, 0.5f, 38f), out hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
            throw new System.InvalidOperationException("Arena A no tiene NavMesh en su centro.");
        EditorUtility.SetDirty(surface);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Entrega configurada: Player y dummy en Arena A, CollisionLayers={follow.collisionLayers.value}, suelos={groundCount}, triángulos NavMesh={UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length / 3}, datos={dataPath}.");
    }

    private static Material ObtenerOMaterial(string nombre, Color colorBase)
    {
        string folder = "Assets/Materials/BlockoutColors";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "BlockoutColors");
        }

        string path = $"{folder}/{nombre}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            // Intentar con Shader URP Lit; si no, Standard
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");

            mat = new Material(s);
            mat.color = colorBase;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", colorBase);
            }
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }

    private static GameObject CrearContenedor(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
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

    private static void CrearCajaProp(GameObject parent, Vector3 pos, Vector3 size, Material mat)
    {
        CrearBloque(parent, "Prop_CajaMadera", pos, size, mat);
    }

    private static void CrearColumnaCompleta(GameObject parent, Vector3 basePos, float altura, Material mat)
    {
        // Fuste de la columna
        CrearBloque(parent, "Columna_Fuste", new Vector3(basePos.x, basePos.y + (altura * 0.5f), basePos.z), new Vector3(2f, altura, 2f), mat);
        // Capitel superior
        CrearBloque(parent, "Columna_Capitel", new Vector3(basePos.x, basePos.y + altura + 0.3f, basePos.z), new Vector3(2.6f, 0.6f, 2.6f), mat);
    }

    private static void CrearArbolEstilizado(GameObject parent, Vector3 basePos, Material matTronco, Material matCopa)
    {
        GameObject goTree = new GameObject("Arbol_Bosque");
        goTree.transform.SetParent(parent.transform);

        // Tronco (Cube estilizado de 6m)
        CrearBloque(goTree, "Tronco", new Vector3(basePos.x, basePos.y + 3f, basePos.z), new Vector3(1.6f, 6f, 1.6f), matTronco);
        // Copa del árbol (Cubo grande de follaje de 5x5x5m arriba)
        CrearBloque(goTree, "Copa_Follaje", new Vector3(basePos.x, basePos.y + 7.5f, basePos.z), new Vector3(5f, 4.5f, 5f), matCopa);
    }

    private static void CrearEscalera(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Stair);
        pbMesh.name = name;
        pbMesh.transform.SetParent(parent.transform);
        pbMesh.transform.localScale = size;
        pbMesh.transform.position = pos;
        pbMesh.transform.rotation = Quaternion.Euler(0, 90, 0);

        if (mat != null)
        {
            pbMesh.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        MeshCollider col = pbMesh.gameObject.GetComponent<MeshCollider>();
        if (col == null) col = pbMesh.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;
    }
}