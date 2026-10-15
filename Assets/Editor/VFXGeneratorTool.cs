using UnityEngine;
using UnityEditor;
using System.IO;

public class VFXGeneratorTool : EditorWindow
{
    [MenuItem("Tools/Generar Prefabs de VFX y Habilidades")]
    public static void ShowWindow()
    {
        GetWindow<VFXGeneratorTool>("VFX Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generador de Efectos Visuales (VFX) Estilizados", EditorStyles.boldLabel);
        GUILayout.Label("Recrea desde cero las texturas, materiales URP sin cuadros morados y prefabs con brillo.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Forzar Regeneración Limpia de VFX", GUILayout.Height(45)))
        {
            GenerarTodoElPaqueteVFX();
        }
    }

    private static void GenerarTodoElPaqueteVFX()
    {
        // 1. Asegurar rutas
        AsegurarCarpeta("Assets/Resources");
        AsegurarCarpeta("Assets/Resources/VFX");
        AsegurarCarpeta("Assets/Textures");
        AsegurarCarpeta("Assets/Textures/VFX");
        AsegurarCarpeta("Assets/Materials");
        AsegurarCarpeta("Assets/Materials/VFX");

        // 2. Generar texturas en disco
        Texture2D texGlow = GenerarTexturaGlowOrb("Assets/Textures/VFX/T_GlowOrb.png");
        Texture2D texRock = GenerarTexturaRockShard("Assets/Textures/VFX/T_RockShard.png");
        Texture2D texLeaf = GenerarTexturaLeafSpore("Assets/Textures/VFX/T_LeafSpore.png");

        // 3. Crear materiales URP
        Material matPiedraRoca = CrearMaterialParticulaURP("M_VFX_Piedra_Roca", texRock, new Color(0.85f, 0.60f, 0.35f, 1f), false);
        Material matPiedraChispa = CrearMaterialParticulaURP("M_VFX_Piedra_Chispa", texGlow, new Color(1.8f, 1.1f, 0.3f, 1f), true);
        Material matMaderaHoja = CrearMaterialParticulaURP("M_VFX_Madera_Hoja", texLeaf, new Color(0.3f, 1.0f, 0.5f, 1f), false);
        Material matMaderaEspora = CrearMaterialParticulaURP("M_VFX_Madera_Espora", texGlow, new Color(0.5f, 1.8f, 0.8f, 1f), true);
        Material matDashEstela = CrearMaterialParticulaURP("M_VFX_Dash_Viento", texGlow, new Color(0.8f, 1.2f, 1.5f, 0.6f), true);

        // 4. Borrar prefabs viejos en disco para evitar que use la versión anterior
        AssetDatabase.DeleteAsset("Assets/Resources/VFX/VFX_Impacto_Piedra.prefab");
        AssetDatabase.DeleteAsset("Assets/Resources/VFX/VFX_Impacto_Madera.prefab");
        AssetDatabase.DeleteAsset("Assets/Resources/VFX/VFX_Dash_Estela.prefab");
        AssetDatabase.DeleteAsset("Assets/Resources/VFX/Muro_Basalto_Habilidad.prefab");
        AssetDatabase.Refresh();

        // 5. Construir nuevos prefabs
        ConstruirVFXPiedra(matPiedraRoca, matPiedraChispa);
        ConstruirVFXMadera(matMaderaHoja, matMaderaEspora);
        ConstruirVFXDash(matDashEstela);
        ConstruirMuroBasalto();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("¡VFX Recreados!", "Se crearon los prefabs limpios en 'Assets/Resources/VFX/' con texturas asignadas y shaders URP.", "Aceptar");
    }

    private static void ConstruirVFXPiedra(Material matRoca, Material matChispa)
    {
        GameObject root = new GameObject("VFX_Impacto_Piedra");

        ParticleSystem psRoca = root.AddComponent<ParticleSystem>();
        var main = psRoca.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = 1.4f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = psRoca.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 16) });

        var shape = psRoca.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ConfigurarCurvasVida(psRoca);

        ParticleSystemRenderer rendRoca = root.GetComponent<ParticleSystemRenderer>();
        rendRoca.material = matRoca;

        // Subemisor chispas
        GameObject goChispas = new GameObject("Chispas");
        goChispas.transform.SetParent(root.transform);
        ParticleSystem psChispa = goChispas.AddComponent<ParticleSystem>();
        var mainC = psChispa.main;
        mainC.duration = 0.35f;
        mainC.loop = false;
        mainC.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        mainC.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        mainC.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);

        var emC = psChispa.emission;
        emC.rateOverTime = 0;
        emC.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 8) });

        ConfigurarCurvasVida(psChispa);

        ParticleSystemRenderer rendChispa = goChispas.GetComponent<ParticleSystemRenderer>();
        rendChispa.material = matChispa;

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/VFX/VFX_Impacto_Piedra.prefab");
        DestroyImmediate(root);
    }

    private static void ConstruirVFXMadera(Material matHoja, Material matEspora)
    {
        GameObject root = new GameObject("VFX_Impacto_Madera");

        ParticleSystem psHoja = root.AddComponent<ParticleSystem>();
        var main = psHoja.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.2f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = psHoja.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 20) });

        var shape = psHoja.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.2f;

        ConfigurarCurvasVida(psHoja);

        ParticleSystemRenderer rendHoja = root.GetComponent<ParticleSystemRenderer>();
        rendHoja.material = matHoja;

        // Subemisor esporas
        GameObject goEsporas = new GameObject("Esporas");
        goEsporas.transform.SetParent(root.transform);
        ParticleSystem psEsporas = goEsporas.AddComponent<ParticleSystem>();
        var mainE = psEsporas.main;
        mainE.duration = 0.45f;
        mainE.loop = false;
        mainE.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        mainE.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3.5f);
        mainE.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);

        var emE = psEsporas.emission;
        emE.rateOverTime = 0;
        emE.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 12) });

        ConfigurarCurvasVida(psEsporas);

        ParticleSystemRenderer rendEspora = goEsporas.GetComponent<ParticleSystemRenderer>();
        rendEspora.material = matEspora;

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/VFX/VFX_Impacto_Madera.prefab");
        DestroyImmediate(root);
    }

    private static void ConstruirVFXDash(Material matDash)
    {
        GameObject root = new GameObject("VFX_Dash_Estela");
        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.25f;
        main.startSpeed = 1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 30;

        ConfigurarCurvasVida(ps);

        ParticleSystemRenderer rend = root.GetComponent<ParticleSystemRenderer>();
        rend.material = matDash;

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/VFX/VFX_Dash_Estela.prefab");
        DestroyImmediate(root);
    }

    private static void ConstruirMuroBasalto()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Muro_Basalto_Habilidad";
        go.transform.localScale = new Vector3(3.2f, 2.4f, 0.7f);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockoutColors/M_L1_RocaBasalto.mat");
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        go.AddComponent<TemporarySummon>().lifeTime = 5.0f;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/VFX/Muro_Basalto_Habilidad.prefab");
        DestroyImmediate(go);
    }

    private static void ConfigurarCurvasVida(ParticleSystem ps)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(0.6f, 0.8f);
        curve.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        col.color = grad;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
    }

    private static Texture2D GenerarTexturaGlowOrb(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1.0f - norm, 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        ConfigurarImportador(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D GenerarTexturaRockShard(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / (size * 0.4f);
                float dy = (y - center.y) / (size * 0.45f);
                float shapeDist = Mathf.Abs(dx * 1.2f + dy * 0.4f) + Mathf.Abs(dy * 1.1f - dx * 0.3f);
                float alpha = (shapeDist <= 0.85f) ? 1.0f : 0.0f;
                float inner = (shapeDist <= 0.65f) ? 0.85f : 1.0f;
                tex.SetPixel(x, y, new Color(inner, inner, inner, alpha));
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        ConfigurarImportador(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D GenerarTexturaLeafSpore(string path)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center.x) / (size * 0.35f);
                float ny = (y - center.y) / (size * 0.45f);
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float leafShape = Mathf.Abs(nx) * 1.8f + Mathf.Abs(ny) * 0.9f;
                float alpha = (leafShape <= 0.9f && r <= 1.0f) ? 1.0f : 0.0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        ConfigurarImportador(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void ConfigurarImportador(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static Material CrearMaterialParticulaURP(string nombre, Texture2D textura, Color colorTint, bool esAditivo)
    {
        string path = $"Assets/Materials/VFX/{nombre}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");

        if (mat == null)
        {
            mat = new Material(s);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = s;
        }

        mat.mainTexture = textura;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", textura);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorTint);
        mat.color = colorTint;

        // Transparencia y modo de mezcla
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", esAditivo ? 1 : 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", esAditivo ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (!AssetDatabase.IsValidFolder(ruta))
        {
            string parent = Path.GetDirectoryName(ruta).Replace("\\", "/");
            string folderName = Path.GetFileName(ruta);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}