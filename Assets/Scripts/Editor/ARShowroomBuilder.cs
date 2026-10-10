#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Menu: AR Showroom > Build Car Prefab + UI
/// Generates a placeholder car prefab (primitives), the manager object and the UI canvas.
public static class ARShowroomBuilder
{
    [MenuItem("AR Showroom/Build Car Prefab + UI")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Materials");
        GameObject prefab = BuildCarPrefab();
        BuildSceneObjects(prefab);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("AR Showroom built. Next: add AR Session + XR Origin if the scene doesn't have them (see README).");
    }

    // ---------- Car ----------
    static Material Mat(string name, Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        string path = "Assets/Materials/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static GameObject BuildCarPrefab()
    {
        Material body = Mat("CarBody", new Color(0.8f, 0.05f, 0.05f));
        Material glass = Mat("CarGlass", new Color(0.1f, 0.15f, 0.2f));
        Material tyre = Mat("CarTyre", new Color(0.03f, 0.03f, 0.03f));
        Material lamp = Mat("CarLamp", new Color(1f, 1f, 0.85f));
        Material floor = Mat("ShowroomFloor", new Color(0.8f, 0.8f, 0.85f));

        var root = new GameObject("ShowroomCar");
        var cc = root.AddComponent<CarController>();
        var bodyR = new System.Collections.Generic.List<Renderer>();

        bodyR.Add(Prim(PrimitiveType.Cube, "Body", root.transform, new Vector3(0, 0.55f, 0), new Vector3(1.8f, 0.6f, 4f), body).GetComponent<Renderer>());
        bodyR.Add(Prim(PrimitiveType.Cube, "Hood", root.transform, new Vector3(0, 0.9f, 1.3f), new Vector3(1.7f, 0.1f, 1.2f), body).GetComponent<Renderer>());
        Prim(PrimitiveType.Cube, "Cabin", root.transform, new Vector3(0, 1.1f, -0.3f), new Vector3(1.6f, 0.55f, 2f), glass);
        bodyR.Add(Prim(PrimitiveType.Cube, "Roof", root.transform, new Vector3(0, 1.4f, -0.3f), new Vector3(1.65f, 0.08f, 1.9f), body).GetComponent<Renderer>());
        Prim(PrimitiveType.Cube, "Platform", root.transform, new Vector3(0, 0.01f, 0), new Vector3(3.2f, 0.02f, 5.4f), floor);

        foreach (float x in new[] { -0.95f, 0.95f })
            foreach (float z in new[] { -1.3f, 1.3f })
            {
                var w = Prim(PrimitiveType.Cylinder, "Wheel", root.transform, new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.15f, 0.7f), tyre);
                w.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }

        // Doors (hinge at front edge)
        Transform MakeDoor(string n, float side)
        {
            var pivot = new GameObject(n).transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0.91f * side, 0.7f, 0.5f);
            bodyR.Add(Prim(PrimitiveType.Cube, n + "Panel", pivot, new Vector3(0, 0, -0.5f), new Vector3(0.06f, 0.5f, 1f), body).GetComponent<Renderer>());
            return pivot;
        }
        cc.rightDoor = MakeDoor("RightDoor", 1);
        cc.leftDoor = MakeDoor("LeftDoor", -1);

        // Headlights
        var lamps = new System.Collections.Generic.List<Renderer>();
        var lights = new System.Collections.Generic.List<Light>();
        foreach (float x in new[] { -0.6f, 0.6f })
        {
            var l = Prim(PrimitiveType.Cube, "Headlamp", root.transform, new Vector3(x, 0.65f, 2.0f), new Vector3(0.35f, 0.15f, 0.05f), lamp);
            lamps.Add(l.GetComponent<Renderer>());
            var lg = new GameObject("Light"); lg.transform.SetParent(l.transform, false);
            lg.transform.localPosition = new Vector3(0, 0, 4f);
            var li = lg.AddComponent<Light>(); li.type = LightType.Spot; li.range = 6; li.spotAngle = 60; li.intensity = 3; li.enabled = false;
            lights.Add(li);
        }

        cc.bodyRenderers = bodyR.ToArray();
        cc.lampRenderers = lamps.ToArray();
        cc.headlights = lights.ToArray();

        string path = "Assets/Prefabs/ShowroomCar.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- Scene ----------
    static void BuildSceneObjects(GameObject prefab)
    {
        var old = GameObject.Find("ShowroomManager"); if (old) Object.DestroyImmediate(old);
        var oldC = GameObject.Find("ShowroomCanvas"); if (oldC) Object.DestroyImmediate(oldC);

        var mgr = new GameObject("ShowroomManager");
        var placer = mgr.AddComponent<ARCarPlacer>(); placer.carPrefab = prefab;
        var gestures = mgr.AddComponent<TouchGestures>(); gestures.placer = placer;
        var ui = mgr.AddComponent<ShowroomUI>(); ui.placer = placer;

        if (!Object.FindObjectOfType<EventSystem>())
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var canvasGO = new GameObject("ShowroomCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // Instruction text
        var tgo = new GameObject("Hint", typeof(RectTransform), typeof(Text));
        tgo.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)tgo.transform;
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 160); rt.anchoredPosition = new Vector2(0, -60);
        var txt = tgo.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = "Scan the floor, then tap to place the car\nDrag = rotate   Pinch = scale";
        txt.fontSize = 38; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;
        tgo.AddComponent<Outline>();

        string[] colorNames = { "Red", "Blue", "White", "Black" };
        Color[] colorVals = { new Color(0.8f,0.1f,0.1f), new Color(0.15f,0.35f,0.9f), Color.white, new Color(0.1f,0.1f,0.1f) };
        for (int i = 0; i < 4; i++)
        {
            var b = MakeButton(canvasGO.transform, colorNames[i], new Vector2(-390 + i * 260, 230), colorVals[i]);
            UnityEventTools.AddIntPersistentListener(b.onClick, ui.SetColorIndex, i);
        }
        var gray = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        UnityEventTools.AddVoidPersistentListener(MakeButton(canvasGO.transform, "Doors",  new Vector2(-390, 90), gray).onClick, ui.ToggleDoors);
        UnityEventTools.AddVoidPersistentListener(MakeButton(canvasGO.transform, "Lights", new Vector2(-130, 90), gray).onClick, ui.ToggleLights);
        UnityEventTools.AddVoidPersistentListener(MakeButton(canvasGO.transform, "Spin",   new Vector2(130, 90), gray).onClick, ui.ToggleSpin);
        UnityEventTools.AddVoidPersistentListener(MakeButton(canvasGO.transform, "Move",   new Vector2(390, 90), gray).onClick, ui.Reposition);
        UnityEventTools.AddVoidPersistentListener(MakeButton(canvasGO.transform, "Reset",  new Vector2(390, 1500), gray, 200, 90).onClick, ui.ResetCar);
        // Reset sits top-right area (anchored bottom, high y)
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Color col, float w = 240, float h = 110)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0); rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = col;

        var t = new GameObject("Text", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(go.transform, false);
        var trt = (RectTransform)t.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = t.GetComponent<Text>();
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.text = label; tx.fontSize = 38; tx.alignment = TextAnchor.MiddleCenter;
        tx.color = (col.r + col.g + col.b) / 3f > 0.6f ? Color.black : Color.white;
        return go.GetComponent<Button>();
    }
}
#endif
