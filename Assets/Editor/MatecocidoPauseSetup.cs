#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MatecocidoPauseSetup
{
    private const string SetupKey = "Matecocido.PauseSetupDone";
    private const string Npc2MaterialPath = "Assets/Models/npc_2.mat";
    private const string Npc2TexturePath = "Assets/Animations/base_npc_.png";
    private const string Npc2PrefabPath = "Assets/Prefabs/NPC2.prefab";

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        if (SessionState.GetBool(SetupKey, false))
            return;

        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SetupKey, false) || EditorApplication.isPlaying)
                return;

            if (FindHud() == null)
            {
                Debug.LogWarning("MatecocidoPauseSetup: Level01 no está abierta; usá Matecocido/Setup Pausa + textura NPC2.");
                return;
            }

            RunAll();
            SessionState.SetBool(SetupKey, true);
        };
    }

    [MenuItem("Matecocido/Setup Pausa + textura NPC2")]
    public static void RunAll()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("MatecocidoPauseSetup: salí del modo Play antes de ejecutar el setup.");
            return;
        }

        ConfigurePause();
        ConfigureNpc2Texture();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("MatecocidoPauseSetup: pausa, sliders, alert y textura de NPC2 configurados.");
    }

    private static Canvas FindHud()
    {
        return Resources.FindObjectsOfTypeAll<Canvas>()
            .FirstOrDefault(canvas => canvas.name == "HUD" && canvas.gameObject.scene.IsValid());
    }

    private static void ConfigurePause()
    {
        Canvas hud = FindHud();
        if (hud == null)
        {
            Debug.LogError("MatecocidoPauseSetup: no se encontró el Canvas HUD.");
            return;
        }

        Transform root = hud.transform;
        Transform pausePanelTransform = Child(root, "pause_panel");
        Transform pauseButtonTransform = Child(root, "btn_pause");
        Transform closeButtonTransform = Child(root, "pause_panel/btn_closePanel");
        Transform quitButtonTransform = Child(root, "pause_panel/btn_quit");
        Transform alertTransform = Child(root, "pause_panel/btn_quit/alert");

        if (pausePanelTransform == null || pauseButtonTransform == null || closeButtonTransform == null ||
            quitButtonTransform == null || alertTransform == null)
        {
            Debug.LogError("MatecocidoPauseSetup: falta algún objeto de la UI de pausa en el HUD.");
            return;
        }

        Slider musicSlider = Component<Slider>(Child(root, "pause_panel/Music/Slider"));
        Slider sfxSlider = Component<Slider>(Child(root, "pause_panel/fx/Slider"));
        if (musicSlider == null || sfxSlider == null)
        {
            Debug.LogError("MatecocidoPauseSetup: no se encontraron los sliders Music/fx del panel de pausa.");
            return;
        }

        PauseMenuController controller = hud.GetComponent<PauseMenuController>();
        if (controller == null)
            controller = hud.gameObject.AddComponent<PauseMenuController>();

        GameObject[] otherHud = root.Cast<Transform>()
            .Where(child => child.gameObject != pausePanelTransform.gameObject)
            .Select(child => child.gameObject)
            .ToArray();

        SerializedObject data = new SerializedObject(controller);
        SetReference(data, "pausePanel", pausePanelTransform.gameObject);
        SetReference(data, "quitAlert", alertTransform.gameObject);
        SetReference(data, "pauseButton", Component<Button>(pauseButtonTransform));
        SetReference(data, "closeButton", Component<Button>(closeButtonTransform));
        SetReference(data, "quitButton", Component<Button>(quitButtonTransform));
        SetReference(data, "yesButton", Component<Button>(Child(alertTransform, "btn_yes")));
        SetReference(data, "noButton", Component<Button>(Child(alertTransform, "btn_no")));
        SetReference(data, "musicSlider", musicSlider);
        SetReference(data, "sfxSlider", sfxSlider);

        SerializedProperty list = data.FindProperty("hudObjects");
        list.arraySize = otherHud.Length;
        for (int i = 0; i < otherHud.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = otherHud[i];

        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        ClearPersistentListeners(Component<Button>(pauseButtonTransform));
        ClearPersistentListeners(Component<Button>(closeButtonTransform));
        ClearPersistentListeners(Component<Button>(quitButtonTransform));
        ClearPersistentListeners(Component<Button>(Child(alertTransform, "btn_yes")));
        ClearPersistentListeners(Component<Button>(Child(alertTransform, "btn_no")));
    }

    private static void ConfigureNpc2Texture()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(Npc2MaterialPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Npc2TexturePath);
        if (material == null || texture == null)
        {
            Debug.LogError("MatecocidoPauseSetup: no se encontró npc_2.mat o base_npc_.png.");
            return;
        }

        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(material);

        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(Npc2PrefabPath);
        if (prefabRoot != null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(Npc2PrefabPath);
            foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            PrefabUtility.SaveAsPrefabAsset(contents, Npc2PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        GameObject instance = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == "NPC2" && go.scene.IsValid());
        if (instance != null)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            EditorUtility.SetDirty(instance);
        }
    }

    private static Transform Child(Transform parent, string path)
    {
        return parent != null ? parent.Find(path) : null;
    }

    private static T Component<T>(Transform transform) where T : Component
    {
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static void SetReference(SerializedObject data, string propertyName, Object value)
    {
        SerializedProperty property = data.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void ClearPersistentListeners(Button button)
    {
        if (button == null)
            return;

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }
}
#endif
