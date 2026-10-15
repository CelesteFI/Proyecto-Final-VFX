using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class HUDGeneratorTool : EditorWindow
{
    [MenuItem("Tools/Generar HUD Completo (GDD Replica)")]
    public static void ShowWindow()
    {
        GetWindow<HUDGeneratorTool>("HUD Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generador de HUD: El Juramento de las Cinco Tierras", EditorStyles.boldLabel);
        GUILayout.Label("Construye el Canvas, barras de vida con ghost delay, cooldowns y ventanas modales de zoom interactivo con mouse.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Construir Estructura Completa del HUD", GUILayout.Height(45)))
        {
            ConstruirHUD();
        }
    }

    public static void ConstruirHUD()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Genera el HUD fuera de Play Mode.");

        GameObject oldCanvas = GameObject.Find("Canvas_CombatHUD");
        if (oldCanvas != null) Undo.DestroyObjectImmediate(oldCanvas);

        GameObject canvasGO = new GameObject("Canvas_CombatHUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Generar Combat HUD");
        CombatHUDManager hudMgr = canvasGO.AddComponent<CombatHUDManager>();
        GameObject player = GameObject.Find("Player");
        if (player != null) hudMgr.tagTeam = player.GetComponent<TagTeamManager>();

        var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            Undo.RegisterCreatedObjectUndo(eventGO, "Generar EventSystem HUD");
            eventSystem = eventGO.GetComponent<UnityEngine.EventSystems.EventSystem>();
        }
        var legacyModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (legacyModule != null) Undo.DestroyObjectImmediate(legacyModule);
        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(eventSystem.gameObject);

        // PANEL HÉROE Y SALUD (Inferior Izquierda)
        GameObject panelHero = CrearRect(canvasGO, "Panel_Heroe_Status", new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(360, 110));
        panelHero.GetComponent<RectTransform>().pivot = Vector2.zero;

        GameObject portrait = CrearImagen(panelHero, "Retrato_Marco", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(45, 0), new Vector2(80, 80), new Color(0.2f, 0.2f, 0.2f, 0.9f));
        GameObject portraitInner = CrearImagen(portrait, "Retrato_Icono", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72), new Color(0.85f, 0.55f, 0.25f, 1f));
        hudMgr.portraitHero = portraitInner.GetComponent<Image>();

        GameObject textNameGO = CrearTexto(panelHero, "Texto_Nombre", new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -20), "Caelen [Piedra]", 20);
        hudMgr.textHeroName = textNameGO.GetComponent<TextMeshProUGUI>();

        Slider sDelay = CrearBarraDeslizante(panelHero, "Barra_Vida_Delay", new Vector2(190, -50), new Vector2(200, 22), Color.white);
        hudMgr.healthDelaySlider = sDelay;

        Slider sHealth = CrearBarraDeslizante(panelHero, "Barra_Vida_Frontal", new Vector2(190, -50), new Vector2(200, 22), new Color(0.85f, 0.2f, 0.2f, 1f));
        hudMgr.healthSlider = sHealth;
        sHealth.transform.Find("Background").GetComponent<Image>().color = Color.clear;

        Slider sStam = CrearBarraDeslizante(panelHero, "Barra_Resistencia", new Vector2(190, -78), new Vector2(170, 14), new Color(0.9f, 0.7f, 0.15f, 1f));
        hudMgr.staminaSlider = sStam;

        // PANEL HABILIDADES Y COOLDOWNS (Inferior Derecha)
        GameObject panelSkills = CrearRect(canvasGO, "Panel_Habilidades", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(240, 110));
        panelSkills.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);

        GameObject skillSlot = CrearImagen(panelSkills, "Slot_Habilidad_E", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(74, 74), new Color(0.15f, 0.15f, 0.15f, 0.9f));
        CrearTexto(skillSlot, "KeyPrompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "E", 24);

        GameObject radialCD = CrearImagen(skillSlot, "Cooldown_Radial", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74, 74), new Color(0, 0, 0, 0.75f));
        Image imgRadial = radialCD.GetComponent<Image>();
        imgRadial.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        imgRadial.type = Image.Type.Filled;
        imgRadial.fillMethod = Image.FillMethod.Radial360;
        imgRadial.fillOrigin = (int)Image.Origin360.Top;
        imgRadial.fillAmount = 0f;
        hudMgr.iconSkillRadialCooldown = imgRadial;

        GameObject cdText = CrearTexto(skillSlot, "Texto_CD", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "", 22);
        hudMgr.textSkillCooldown = cdText.GetComponent<TextMeshProUGUI>();

        GameObject dashSlot = CrearImagen(panelSkills, "Slot_Dash_Shift", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-140, -10), new Vector2(54, 54), new Color(0.25f, 0.25f, 0.25f, 0.85f));
        hudMgr.iconDashCooldown = dashSlot.GetComponent<Image>();
        CrearTexto(dashSlot, "KeyPromptDash", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "Shift", 16);

        // BOTONES DE IMAGEN SUPERIORES (Clickeables con Mouse)
        GameObject btnRec = CrearBoton(canvasGO, "Boton_Recursos_Top", new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -40), new Vector2(180, 42), "Recursos [M]");
        GameObject btnHab = CrearBoton(canvasGO, "Boton_Habilidades_Top", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -40), new Vector2(180, 42), "Resonancia [K]");

        // VENTANAS EMERGENTES CON ZOOM (MODALES)
        UIModalWindow modalRec = CrearModalWindow(canvasGO, "Modal_Ventana_Recursos", "ALMACÉN DE SÍLICE Y MATERIALES", "Fragmentos de Sílice acumulados: 0\nMateriales de mejora para el Bastión de Granito.\n\n[Haz clic fuera o presiona M para cerrar]");
        UIModalWindow modalHab = CrearModalWindow(canvasGO, "Modal_Ventana_Habilidades", "ÁRBOL DE RESONANCIA ELEMENTAL", "• Caelen: Baluarte Telúrico (Activo)\n• Illya: Zarzas Vinculantes (Activo)\n• Zephyr: Sellado en el Firmamento\n\n[Haz clic fuera o presiona K para cerrar]");

        hudMgr.modalRecursos = modalRec;
        hudMgr.modalHabilidades = modalHab;

        // Los enlaces persistentes onClick se configuran después de la generación.
        foreach (Transform child in canvasGO.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = LayerMask.NameToLayer("UI");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("HUD generado: Canvas_CombatHUD, barras duales, cooldown radial y dos modales.");
    }

    public static void ImportarRecursosTMP()
    {
        AssetDatabase.ImportPackage(System.IO.Path.GetFullPath("Library/PackageCache/com.unity.ugui@20629863fcab/Package Resources/TMP Essential Resources.unitypackage"), false);
    }

    public static void EnlazarTextosHUD()
    {
        string[] fonts = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
        if (fonts.Length == 0) throw new System.InvalidOperationException("Faltan recursos esenciales TMP.");
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fonts[0]));
        GameObject canvas = GameObject.Find("Canvas_CombatHUD");
        foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(text, "Enlazar fuente HUD");
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.ForceMeshUpdate(true);
            EditorUtility.SetDirty(text);
        }
        canvas.transform.Find("Modal_Ventana_Recursos/Body").GetComponent<TextMeshProUGUI>().text = "Fragmentos de Sílice acumulados: 0\nMateriales de mejora para el Bastión de Granito.\n\n[Presiona M o el botón Recursos para cerrar]";
        canvas.transform.Find("Modal_Ventana_Habilidades/Body").GetComponent<TextMeshProUGUI>().text = "• Caelen: Muro de Basalto (Activo)\n• Illya: Madera\n• Zephyr: Sellado en el Firmamento\n\n[Presiona K o el botón Resonancia para cerrar]";
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("Textos HUD enlazados a " + font.name);
    }

    public static string RevisarConsola()
    {
        object[] counts = { 0, 0, 0 };
        System.Type entries = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        entries.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, counts);
        return "CONSOLE errors=" + counts[0] + ", warnings=" + counts[1] + ", logs=" + counts[2];
    }

    public static void LimpiarConsolaPrueba()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Limpia la consola antes de Play Mode.");
        System.Type entries = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        entries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, null);
    }

    public static void ProbarEntregaPlay()
    {
        if (!Application.isPlaying) throw new System.InvalidOperationException("La prueba requiere Play Mode.");
        CombatHUDManager hud = Object.FindFirstObjectByType<CombatHUDManager>();
        if (hud == null || hud.tagTeam == null) throw new System.InvalidOperationException("HUD sin Tag-Team.");
        hud.StartCoroutine(PruebaEntregaPlay(hud));
    }

    private static System.Collections.IEnumerator PruebaEntregaPlay(CombatHUDManager hud)
    {
        TagTeamManager team = hud.tagTeam;
        HealthSystem dummy = GameObject.Find("Enemigo").GetComponent<HealthSystem>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        team.CambiarHeroe(1);
        yield return null;
        if (!hud.textHeroName.text.Contains("Illya")) throw new System.InvalidOperationException("El HUD no sigue el relevo a Illya.");
        team.CambiarHeroe(0);
        yield return null;
        if (!hud.textHeroName.text.Contains("Caelen")) throw new System.InvalidOperationException("El HUD no sigue el relevo a Caelen.");

        float originalHealth = team.caelen.currentHealth;
        team.caelen.currentHealth = team.caelen.maxHealth * 0.5f;
        hud.healthDelaySlider.value = 1f;
        hud.GetType().GetMethod("Update", flags).Invoke(hud, null);
        if (!Mathf.Approximately(hud.healthSlider.value, 0.5f) || hud.healthDelaySlider.value <= hud.healthSlider.value)
            throw new System.InvalidOperationException("Las barras de vida dual no muestran el retraso.");
        team.caelen.currentHealth = originalHealth;
        team.caelen.skillCooldownTimer = 0f;
        team.GetType().GetMethod("EjecutarHabilidadElemental", flags).Invoke(team, null);
        yield return null;
        if (hud.iconSkillRadialCooldown.sprite == null || hud.iconSkillRadialCooldown.fillAmount <= 0f || GameObject.Find("Muro_Basalto_Habilidad(Clone)") == null)
            throw new System.InvalidOperationException("Muro o cooldown radial no funcional.");

        Transform originalAttackPoint = team.attackPoint;
        Vector3 originalPoint = originalAttackPoint.localPosition;
        originalAttackPoint.position = dummy.transform.position;
        team.GetType().GetMethod("EjecutarAtaqueBasico", flags).Invoke(team, null);
        originalAttackPoint.localPosition = originalPoint;
        if (!Mathf.Approximately(dummy.currentHealth, 100f - team.ActiveHero.attackDamage) || dummy.GetComponent<MeshRenderer>().material.color != Color.white)
            throw new System.InvalidOperationException("Daño o flash blanco del dummy no funcional.");
        yield return new WaitForSecondsRealtime(0.3f);
        if (!Mathf.Approximately(Time.timeScale, 1f)) throw new System.InvalidOperationException("Hitstop no restauró el tiempo.");

        string[] buttons = { "Boton_Recursos_Top", "Boton_Habilidades_Top" };
        UIModalWindow[] modals = { hud.modalRecursos, hud.modalHabilidades };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = hud.transform.Find(buttons[i]).GetComponent<Button>();
            if (button.onClick.GetPersistentEventCount() != 1) throw new System.InvalidOperationException("onClick modal no persistente.");
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.35f);
            CanvasGroup group = modals[i].GetComponent<CanvasGroup>();
            if (!modals[i].gameObject.activeSelf || group.alpha < 0.99f || modals[i].transform.localScale != Vector3.one)
                throw new System.InvalidOperationException("Modal no abrió con zoom.");
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.35f);
            if (modals[i].gameObject.activeSelf) throw new System.InvalidOperationException("Modal no cerró.");
        }
        var path = new UnityEngine.AI.NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(new Vector3(0f, 0.5f, 34f), new Vector3(0f, 0.5f, 38f), UnityEngine.AI.NavMesh.AllAreas, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
            throw new System.InvalidOperationException("Ruta NavMesh de Arena A incompleta.");
        Debug.Log("PRUEBA ENTREGA OK: relevo Caelen/Illya, HUD dual y ghost, muro y cooldown radial, daño 100->70, flash blanco, hitstop, ambos onClick persistentes con zoom y ruta NavMesh completa. Controles físicos pendientes de prueba manual.");
    }

    private static GameObject CrearRect(GameObject parent, string name, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }

    private static GameObject CrearImagen(GameObject parent, string name, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = CrearRect(parent, name, min, max, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    private static GameObject CrearTexto(GameObject parent, string name, Vector2 min, Vector2 max, Vector2 pos, string texto, float fontSize)
    {
        GameObject go = CrearRect(parent, name, min, max, pos, new Vector2(200, 35));
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = texto; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return go;
    }

    private static Slider CrearBarraDeslizante(GameObject parent, string name, Vector2 pos, Vector2 size, Color fillCol)
    {
        GameObject sliderGO = CrearRect(parent, name, new Vector2(0, 1), new Vector2(0, 1), pos, size);
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        GameObject bg = CrearImagen(sliderGO, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.1f, 0.1f, 0.1f, 0.7f));
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

        GameObject fillArea = CrearRect(sliderGO, "Fill Area", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

        GameObject fill = CrearImagen(fillArea, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fillCol);
        RectTransform fRT = fill.GetComponent<RectTransform>();
        fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;

        slider.targetGraphic = fill.GetComponent<Image>();
        slider.fillRect = fRT;
        slider.value = 1f;
        return slider;
    }

    private static GameObject CrearBoton(GameObject parent, string name, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, string label)
    {
        GameObject go = CrearImagen(parent, name, min, max, pos, size, new Color(0.2f, 0.25f, 0.3f, 0.9f));
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        go.GetComponent<Image>().raycastTarget = true;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.35f, 0.45f, 0.55f);
        btn.colors = cb;
        GameObject labelGO = CrearTexto(go, "BtnLabel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, label, 18);
        labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(174f, 35f);
        return go;
    }

    private static UIModalWindow CrearModalWindow(GameObject parent, string name, string titulo, string contenido)
    {
        GameObject modalGO = CrearImagen(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 380), new Color(0.08f, 0.09f, 0.12f, 0.95f));
        modalGO.AddComponent<Outline>().effectColor = new Color(0.85f, 0.7f, 0.25f, 0.8f);

        modalGO.AddComponent<CanvasGroup>();
        modalGO.GetComponent<Image>().raycastTarget = true;
        GameObject headerGO = CrearTexto(modalGO, "Header", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -35), titulo, 22);
        headerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 40f);

        GameObject bodyGO = CrearTexto(modalGO, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), contenido, 17);
        bodyGO.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 240);
        bodyGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

        UIModalWindow modal = modalGO.AddComponent<UIModalWindow>();
        modal.zoomCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.65f, 1.12f), new Keyframe(1f, 1f));
        return modal;
    }
}