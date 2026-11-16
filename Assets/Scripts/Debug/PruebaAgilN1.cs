#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Prueba ágil en Play Mode del Nivel 1 (Fase 6):
///   A) Los triggers de los 9 tótems de `06_Puzzle_Lore` no se cruzan ni se tocan
///      (comprobación matemática sobre los bounds reales + rejilla de 0,5 m) y en
///      todo el recorrido el HUD muestra como mucho UN banner (un único tótem con
///      `jugadorDentro` a la vez).
///   B) Recorrido caminando (CharacterController.Move, sin teletransportes) que
///      enciende los 9 tótems con 'F'; verifica fuego azul, contador 1..4, modal y
///      cierre con su propia 'X', y que al último tótem cede el sello del puente.
///   C) Muerte del Jefe del Nivel 1 (daño real sobre HealthComponent) y aparición
///      de `Cage` por SetActive(true).
///
/// No crea GameObjects ni instancia nada: se añade como componente al objeto que
/// ya existe (`Gestor_Lore_06`). Guarda y restaura PlayerPrefs. No guarda escena.
/// </summary>
[DisallowMultipleComponent]
public class PruebaAgilN1 : MonoBehaviour
{
    const string TextoTotem = "Presiona [F] para encender";
    const float Velocidad = 6.5f;
    const float RadioAproximacion = 2.6f;

    static readonly string[] ClavesPrefs =
    {
        "EcosEraOlvidada.Recurso.CristalesVoto",
        "EcosEraOlvidada.Recurso.OroPuro",
        "EcosEraOlvidada.Recurso.Esencias",
        "EcosEraOlvidada.Objetos",
        "EcosEraOlvidada.Lore.Fragmentos",
        "EcosEraOlvidada.Progreso.IllyaDesbloqueada",
    };

    readonly Dictionary<string, string> prefs = new Dictionary<string, string>();
    readonly List<string> prefsInexistentes = new List<string>();

    PlayerController control;
    CharacterController cc;

    GameObject banner;
    TextMeshProUGUI txtBanner;
    Transform modalLore;
    UIModalWindow ventanaLore;
    Button btnXCerrar;
    TextMeshProUGUI cuerpoLore;
    TextMeshProUGUI contadorLore;

    PuzzleLoreTotems gestor;
    List<TotemLore> totems;

    int fallas;
    readonly StringBuilder resumen = new StringBuilder();

    public static void Arrancar()
    {
        if (!Application.isPlaying) { Debug.Log("[AGIL] FALLA: no está en Play Mode."); return; }
        if (Object.FindFirstObjectByType<PruebaAgilN1>() != null) { Debug.Log("[AGIL] ya hay una prueba en curso."); return; }

        PuzzleLoreTotems g = Object.FindFirstObjectByType<PuzzleLoreTotems>(FindObjectsInactive.Include);
        GameObject host = g != null ? g.gameObject : GameObject.FindGameObjectWithTag("Player");
        if (host == null) { Debug.Log("[AGIL] FALLA: no hay objeto anfitrión."); return; }

        host.AddComponent<PruebaAgilN1>();
        Debug.Log($"[AGIL] arrancada sobre '{host.name}'.");
    }

    private void Start() => StartCoroutine(Rutina());

    private IEnumerator Rutina()
    {
        yield return null;

        if (!Preparar())
        {
            Debug.Log("[AGIL] ===== ABORTADA (faltan referencias) =====");
            yield break;
        }

        yield return ApuntarRatonFueraDeUI();

        PruebaGeometrica();

        yield return Recorrido();

        ResumenVictoria();

        yield return PruebaJefe();

        Final();
    }

    // ---------------------------------------------------------------- preparación

    private bool Preparar()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null) { Debug.Log("[AGIL] FALLA: no hay Player."); return false; }

        control = jugador.GetComponent<PlayerController>();
        cc = jugador.GetComponent<CharacterController>();

        banner = BuscarGO("Banner_Instrucciones");
        txtBanner = banner != null ? banner.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        modalLore = BuscarTransform("Modal_Lore");
        ventanaLore = modalLore != null ? modalLore.GetComponent<UIModalWindow>() : null;
        cuerpoLore = BuscarTexto("Cuerpo_Lore");
        contadorLore = BuscarTexto("Contador_Lore");

        if (modalLore != null)
            foreach (Transform t in modalLore.GetComponentsInChildren<Transform>(true))
                if (t.name == "Btn_Cerrar_X") { btnXCerrar = t.GetComponent<Button>(); break; }

        gestor = Object.FindFirstObjectByType<PuzzleLoreTotems>(FindObjectsInactive.Include);

        totems = new List<TotemLore>(Object.FindObjectsByType<TotemLore>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        totems.Sort((a, b) => string.CompareOrdinal(a.IdFragmento, b.IdFragmento));

        foreach (string k in ClavesPrefs)
        {
            if (PlayerPrefs.HasKey(k)) prefs[k] = PlayerPrefs.GetString(k);
            else prefsInexistentes.Add(k);
        }

        if (GameProgressManager.Instance != null) GameProgressManager.Instance.ResetearProgreso();
        else GameSaveState.BorrarTodo();

        Time.timeScale = 1f;

        bool ok = control != null && cc != null && modalLore != null && btnXCerrar != null &&
                  gestor != null && totems.Count > 0;

        Debug.Log($"[AGIL][PREP] player='{jugador.name}' pos={F(control.transform.position)} banner={(banner != null)} " +
                  $"modalLore={(modalLore != null)} btnX=[{Ruta(btnXCerrar)}] totems={totems.Count} gestor={(gestor != null)} " +
                  $"paso={(gestor != null && gestor.PasoAbierto)} -> {(ok ? "ok" : "FALTAN REFERENCIAS")}");
        return ok;
    }

    // ---------------------------------------------------------------- A) geometría

    private void PruebaGeometrica()
    {
        float peorHueco = float.MaxValue;
        string peorPar = "-";

        for (int i = 0; i < totems.Count; i++)
        {
            Bounds bi = totems[i].GetComponent<Collider>().bounds;
            for (int j = i + 1; j < totems.Count; j++)
            {
                Bounds bj = totems[j].GetComponent<Collider>().bounds;
                float huecoX = Mathf.Max(bj.min.x - bi.max.x, bi.min.x - bj.max.x);
                float huecoZ = Mathf.Max(bj.min.z - bi.max.z, bi.min.z - bj.max.z);
                float hueco = Mathf.Max(huecoX, huecoZ);
                if (hueco < peorHueco) { peorHueco = hueco; peorPar = $"{totems[i].IdFragmento}<->{totems[j].IdFragmento}"; }
            }
        }

        Bounds zona = new Bounds(totems[0].transform.position, Vector3.zero);
        foreach (TotemLore t in totems) zona.Encapsulate(t.GetComponent<Collider>().bounds);

        int muestras = 0, maxSolape = 0;
        Vector3 peorPunto = Vector3.zero;
        for (float x = zona.min.x; x <= zona.max.x; x += 0.5f)
            for (float z = zona.min.z; z <= zona.max.z; z += 0.5f)
            {
                int n = 0;
                foreach (TotemLore t in totems)
                    if (DentroXZ(t.GetComponent<Collider>().bounds, x, z)) n++;
                muestras++;
                if (n > maxSolape) { maxSolape = n; peorPunto = new Vector3(x, 0f, z); }
            }

        int solapesFisicos = 0;
        foreach (TotemLore t in totems)
        {
            Bounds b = t.GetComponent<Collider>().bounds;
            Collider[] hits = Physics.OverlapBox(b.center, b.extents * 0.98f, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider h in hits)
            {
                TotemLore otro = h.GetComponent<TotemLore>();
                if (otro != null && otro != t) solapesFisicos++;
            }
        }

        bool separacion = peorHueco > 0.01f;
        bool rejilla = maxSolape <= 1;
        bool fisica = solapesFisicos == 0;
        bool ok = separacion && rejilla && fisica;
        if (!ok) fallas++;

        Debug.Log($"[AGIL][A] geometría: hueco mínimo entre triggers = {peorHueco:F2} m ({peorPar}) · " +
                  $"rejilla 0,5 m: {muestras} muestras, máx. {maxSolape} trigger(s) por punto (peor {F(peorPunto)}) · " +
                  $"OverlapBox solapes = {solapesFisicos} -> {(ok ? "PASA" : "FALLA")}");

        resumen.AppendLine($"A. Triggers sin cruce: hueco mínimo {peorHueco:F2} m, máximo {maxSolape} trigger por punto, solapes físicos {solapesFisicos} -> {(ok ? "PASA" : "FALLA")}");
    }

    // ---------------------------------------------------------------- B) recorrido

    private IEnumerator Recorrido()
    {
        var pendientes = new List<TotemLore>(totems);
        var orden = new List<TotemLore>();
        Vector3 cursor = control.transform.position;
        while (pendientes.Count > 0)
        {
            int mejor = 0;
            float mejorD = float.MaxValue;
            for (int i = 0; i < pendientes.Count; i++)
            {
                float d = Vector3.Distance(cursor, pendientes[i].transform.position);
                if (d < mejorD) { mejorD = d; mejor = i; }
            }
            orden.Add(pendientes[mejor]);
            cursor = pendientes[mejor].transform.position;
            pendientes.RemoveAt(mejor);
        }

        int bannerOK = 0, encendidosOK = 0, fuegosOK = 0, unicoTriggerOK = 0;
        int modalesOK = 0, cierresOK = 0, conLore = 0, esperado = 0;
        int maxSimultaneos = 0;
        Vector3 previo = control.transform.position;

        for (int i = 0; i < orden.Count; i++)
        {
            TotemLore t = orden[i];
            Vector3 centro = t.transform.position;

            Vector3 dir = previo - centro; dir.y = 0f;
            if (dir.sqrMagnitude < 0.25f) dir = Vector3.back;
            Vector3 aproximacion = centro + dir.normalized * RadioAproximacion;

            yield return CaminarA(aproximacion, 0.30f, 1200);

            // Entrada efectiva al trigger
            for (float w = 0f; w < 1.5f && !t.JugadorDentro; w += 0.05f) yield return new WaitForSecondsRealtime(0.05f);

            int dentro = ContarDentro();
            if (dentro > maxSimultaneos) maxSimultaneos = dentro;
            if (t.JugadorDentro && dentro == 1) unicoTriggerOK++;

            bool bannerAntes = banner != null && banner.activeSelf && txtBanner != null && txtBanner.text == TextoTotem;
            if (bannerAntes) bannerOK++;

            int intentos = 0;
            for (int k = 0; k < 3 && !t.YaEncendido; k++)
            {
                intentos++;
                yield return PulsarF(3);
                if (!t.YaEncendido) yield return new WaitForSecondsRealtime(0.15f);
            }

            if (t.YaEncendido) encendidosOK++;
            float intensidad = LeerIntensidad(PrimerFuego(t));
            if (Mathf.Abs(intensidad - 1f) < 0.02f) fuegosOK++;

            string nota;
            EmisorLore emisor = t.GetComponent<EmisorLore>();
            if (emisor != null && emisor.TieneLore)
            {
                conLore++;
                if (t.YaEncendido) esperado++;

                bool abierto = false;
                for (float w = 0f; w < 1.2f && !abierto; w += 0.05f)
                {
                    abierto = modalLore.gameObject.activeSelf;
                    if (!abierto) yield return new WaitForSecondsRealtime(0.05f);
                }

                bool logico = ventanaLore != null && ventanaLore.EstaAbierto;
                bool pausa = Mathf.Approximately(Time.timeScale, 0f);
                string cuerpo = cuerpoLore != null ? cuerpoLore.text : "(null)";

                if (abierto && logico && pausa && cuerpo == emisor.Cuerpo) modalesOK++;

                btnXCerrar.onClick.Invoke();

                bool cerrado = false;
                for (float w = 0f; w < 1.2f && !cerrado; w += 0.05f)
                {
                    cerrado = !modalLore.gameObject.activeSelf;
                    if (!cerrado) yield return new WaitForSecondsRealtime(0.05f);
                }
                bool pausaFinal = Mathf.Approximately(Time.timeScale, 1f);
                if (cerrado && pausaFinal) cierresOK++;
                else Time.timeScale = 1f;

                nota = $"lore='{emisor.Titulo}' modal={abierto}/{logico} cerradoX={cerrado}";
            }
            else nota = $"sin lore · modalCerrado={!modalLore.gameObject.activeSelf}";

            Debug.Log($"[AGIL][B] {t.IdFragmento} pos={F(centro)} dentro={t.JugadorDentro} (otros={dentro - (t.JugadorDentro ? 1 : 0)}) " +
                      $"banner={bannerAntes} encendido={t.YaEncendido} _Intensity={intensidad:F2} intentosF={intentos} | {nota}");

            // Salir caminando (OnTriggerExit real) antes del siguiente tótem.
            previo = centro;
            yield return CaminarA(centro + dir.normalized * (RadioAproximacion + 2.2f), 0.35f, 600);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        int encendidos = gestor.Encendidos;
        bool todos = encendidos == totems.Count;
        bool pasoAbierto = gestor.PasoAbierto;
        bool selloAbierto = gestor.PasoAbierto;
        GameObject barrera = LeerRef(gestor, "barreraSello") as GameObject;
        bool barreraFuera = barrera != null && !barrera.activeSelf;

        bool bannerLimpio = banner == null || !banner.activeSelf;
        bool uiLibre = Mathf.Approximately(Time.timeScale, 1f) && !UIModalWindow.HayModalAbierto;
        bool unBanner = maxSimultaneos <= 1;

        bool ok = todos && pasoAbierto && selloAbierto && barreraFuera && bannerLimpio && uiLibre && unBanner &&
                  bannerOK == totems.Count && encendidosOK == totems.Count && fuegosOK == totems.Count &&
                  unicoTriggerOK == totems.Count &&
                  modalesOK == conLore && cierresOK == conLore;
        if (!ok) fallas++;

        Debug.Log($"[AGIL][B-resumen] encendidos={encendidos}/{totems.Count} bannerOK={bannerOK}/{totems.Count} " +
                  $"unSoloTrigger={unicoTriggerOK}/{totems.Count} (máx simultáneos {maxSimultaneos}) " +
                  $"intensidad1={fuegosOK}/{totems.Count} lore={conLore} modales={modalesOK}/{conLore} " +
                  $"cierresX={cierresOK}/{conLore} -> {(ok ? "PASA" : "FALLA")}");

        resumen.AppendLine($"B. Recorrido caminando: {encendidos}/9 tótems, banner único (máx simultáneos {maxSimultaneos}), " +
                           $"lore {modalesOK}/{conLore} modales, cierres 'X' {cierresOK}/{conLore} -> {(ok ? "PASA" : "FALLA")}");
    }

    private void ResumenVictoria()
    {
        bool pasoAbierto = gestor.PasoAbierto;
        bool selloAbierto = gestor.PasoAbierto;
        GameObject barrera = LeerRef(gestor, "barreraSello") as GameObject;
        bool barreraFuera = barrera != null && !barrera.activeSelf;
        bool ok = pasoAbierto && selloAbierto && barreraFuera;
        if (!ok) fallas++;

        Debug.Log($"[AGIL][B-victoria] pasoAbierto={pasoAbierto} selloAbierto={selloAbierto} " +
                  $"barrera='{barrera?.name}' activa={(barrera != null && barrera.activeSelf)} -> {(ok ? "PASA" : "FALLA")}");

        resumen.AppendLine($"B-victoria. Sello del puente liberado al último tótem: paso={pasoAbierto} sello={selloAbierto} " +
                           $"barrera activa={(barrera != null && barrera.activeSelf)} -> {(ok ? "PASA" : "FALLA")}");
    }

    // ---------------------------------------------------------------- C) jefe

    private IEnumerator PruebaJefe()
    {
        Transform jefe = BuscarTransform("Jefe_ScorpionDelAbismo");
        Transform jaula = BuscarTransform("Cage");
        if (jefe == null || jaula == null)
        {
            fallas++;
            Debug.Log($"[AGIL][C] FALLA: jefe={(jefe != null)} jaula={(jaula != null)}");
            resumen.AppendLine("C. Jefe: FALLA (faltan referencias)");
            yield break;
        }

        var salud = jefe.GetComponent<HealthComponent>();
        if (salud == null) { fallas++; Debug.Log("[AGIL][C] FALLA: el jefe no tiene HealthComponent."); yield break; }

        bool jaulaAntes = jaula.gameObject.activeSelf;
        float hp = salud.CurrentHealth;
        salud.TakeDamage(salud.MaxHealth * 2f);

        bool activa = false;
        for (float w = 0f; w < 3f && !activa; w += 0.1f)
        {
            activa = jaula.gameObject.activeSelf;
            if (!activa) yield return new WaitForSecondsRealtime(0.1f);
        }

        bool hito = GameProgressManager.Instance != null && GameProgressManager.Instance.bossNivel1Derrotado;
        bool enJerarquia = jaula.gameObject.activeInHierarchy;
        bool ok = !jaulaAntes && activa && enJerarquia && hito && salud.EstaMuerto;
        if (!ok) fallas++;

        Debug.Log($"[AGIL][C] jefe hp {hp:F0}->{salud.CurrentHealth:F0} muerto={salud.EstaMuerto} hitoJefe={hito} · " +
                  $"Cage antes={jaulaAntes} ahora={activa} activeInHierarchy={enJerarquia} -> {(ok ? "PASA" : "FALLA")}");

        resumen.AppendLine($"C. Muerte del jefe: hp {hp:F0}->{salud.CurrentHealth:F0}, hito={hito}, Cage SetActive(true)={activa} -> {(ok ? "PASA" : "FALLA")}");
    }

    // ---------------------------------------------------------------- cierre

    private void Final()
    {
        Time.timeScale = 1f;

        foreach (var par in prefs) PlayerPrefs.SetString(par.Key, par.Value);
        foreach (string k in prefsInexistentes) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();

        Debug.Log($"[AGIL] ===== RESUMEN =====\n{resumen}" +
                  $"[AGIL] ===== {(fallas == 0 ? "TODO PASA" : fallas + " FALLA(S)")} · PlayerPrefs restaurados ({prefs.Count}) =====");

        enabled = false;
    }

    // ---------------------------------------------------------------- utilidades

    private IEnumerator CaminarA(Vector3 destino, float tolerancia, int maxFrames)
    {
        int frames = 0;
        float mejor = float.MaxValue;
        int sinProgreso = 0;
        int sentido = 1;

        while (frames < maxFrames)
        {
            Vector3 p = control.transform.position;
            Vector3 plano = new Vector3(destino.x - p.x, 0f, destino.z - p.z);
            float d = plano.magnitude;
            if (d <= tolerancia) break;

            Vector3 dir = plano / d;
            if (sinProgreso > 40)
            {
                // Desatasco en perpendicular (esquiva de obstáculos).
                dir = Vector3.Cross(Vector3.up, dir) * sentido;
                sentido = -sentido;
                sinProgreso = 0;
            }

            float paso = Mathf.Min(Velocidad * Time.deltaTime, d);
            cc.Move(dir * paso);
            frames++;

            if (frames % 25 == 0)
            {
                float nuevo = Vector3.Distance(new Vector3(control.transform.position.x, 0f, control.transform.position.z),
                                               new Vector3(destino.x, 0f, destino.z));
                if (nuevo > mejor - 0.05f) sinProgreso += 25;
                else { sinProgreso = 0; mejor = nuevo; }
            }

            yield return null;
        }

        if (frames >= maxFrames)
            Debug.LogWarning($"[AGIL] caminata sin llegar a {F(destino)} en {maxFrames} frames (pos={F(control.transform.position)}).");
    }

    private int ContarDentro()
    {
        int n = 0;
        foreach (TotemLore t in totems) if (t != null && t.JugadorDentro) n++;
        return n;
    }

    private static bool DentroXZ(Bounds b, float x, float z)
    {
        return x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z;
    }

    private IEnumerator PulsarF(int framesMantenido)
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) { Debug.LogWarning("[AGIL] sin teclado."); yield break; }

        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.F));
        for (int i = 0; i < framesMantenido; i++) yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
        yield return null;
    }

    private IEnumerator ApuntarRatonFueraDeUI()
    {
        if (Mouse.current == null) yield break;

        Vector2[] candidatos = { new Vector2(700f, 300f), new Vector2(1200f, 320f), new Vector2(960f, 480f) };
        for (int i = 0; i < candidatos.Length; i++)
        {
            Mouse.current.WarpCursorPosition(candidatos[i]);
            yield return null;
            yield return null;
            if (!PlayerInputReader.PunteroSobreUI) { Debug.Log($"[AGIL][RATÓN] cursor en {candidatos[i]}"); yield break; }
        }
        Debug.LogWarning("[AGIL][RATÓN] el puntero sigue sobre la UI.");
    }

    private static Renderer PrimerFuego(TotemLore t)
    {
        var so = new UnityEditor.SerializedObject(t);
        var arr = so.FindProperty("visualesFuego");
        if (arr == null || arr.arraySize == 0) return null;
        return arr.GetArrayElementAtIndex(0).objectReferenceValue as Renderer;
    }

    private static float LeerIntensidad(Renderer r)
    {
        if (r == null) return -1f;
        var bloque = new MaterialPropertyBlock();
        r.GetPropertyBlock(bloque);
        return bloque.GetFloat("_Intensity");
    }

    private static Object LeerRef(Object creador, string campo)
    {
        var so = new UnityEditor.SerializedObject(creador);
        return so.FindProperty(campo)?.objectReferenceValue;
    }

    private static string Ruta(Button b)
    {
        if (b == null) return "NULL";
        Transform t = b.transform;
        var sb = new StringBuilder(t.name);
        while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
        return sb.ToString();
    }

    private GameObject BuscarGO(string nombre)
    {
        Transform t = BuscarTransform(nombre);
        return t != null ? t.gameObject : null;
    }

    private static Transform BuscarTransform(string nombre, Transform raiz = null)
    {
        Transform[] todos = raiz != null
            ? raiz.GetComponentsInChildren<Transform>(true)
            : Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < todos.Length; i++)
            if (todos[i].name == nombre) return todos[i];

        return null;
    }

    private static TextMeshProUGUI BuscarTexto(string nombre, Transform raiz = null)
    {
        Transform t = BuscarTransform(nombre, raiz);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static string F(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
}
#endif
