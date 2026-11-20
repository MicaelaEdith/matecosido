#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MatecocidoFullSetup
{
    private const string SetupKey = "Matecocido.FullSetupDone";
    private const string Npc2PrefabPath = "Assets/Prefabs/NPC2.prefab";
    private const string Npc2ControllerPath = "Assets/Animations/Npc2Animator.controller";

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        if (SessionState.GetBool(SetupKey, false))
            return;

        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SetupKey, false))
                return;

            if (UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>() == null)
            {
                Debug.LogWarning("MatecocidoFullSetup: Level01 no está abierta; usá Matecocido/Setup Completo.");
                return;
            }

            RunAll();
            SessionState.SetBool(SetupKey, true);
        };
    }

    [MenuItem("Matecocido/Setup Completo (3 oleadas + NPC2 + disparo)")]
    public static void RunAll()
    {
        ConfigureShotDelay();
        ConfigureWaves();
        ConfigureNpc2();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("MatecocidoFullSetup: 3 oleadas, NPC2 y delay de disparo configurados.");
    }

    private static void ConfigureShotDelay()
    {
        PlayerCombat combat = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
        if (combat == null)
            return;

        SerializedObject data = new SerializedObject(combat);
        SerializedProperty delay = data.FindProperty("shootDelay");
        if (delay != null)
            delay.floatValue = 0.35f;

        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(combat);
    }

    private static void ConfigureWaves()
    {
        EnemyWaveManager manager = UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>();
        if (manager == null)
        {
            Debug.LogError("MatecocidoFullSetup: no se encontró EnemyWaveManager.");
            return;
        }

        EnemyController[] police = Resources.FindObjectsOfTypeAll<EnemyController>()
            .Where(e => e.gameObject.scene.IsValid() && e.name.StartsWith("Police_", StringComparison.Ordinal))
            .OrderBy(e => e.name)
            .ToArray();

        Dictionary<int, EnemyController> byIndex = new Dictionary<int, EnemyController>();
        foreach (EnemyController enemy in police)
        {
            if (int.TryParse(enemy.name.Substring("Police_".Length), out int index))
                byIndex[index] = enemy;
        }

        List<EnemyController> melee = Enemies(byIndex, 0, 15);
        List<EnemyController> mixed = Enemies(byIndex, 16, 23);
        List<EnemyController> ranged = Enemies(byIndex, 24, 31);

        if (melee.Count == 0 || mixed.Count == 0 || ranged.Count == 0)
        {
            Debug.LogError($"MatecocidoFullSetup: faltan policías (melee={melee.Count}, mixed={mixed.Count}, ranged={ranged.Count}).");
            return;
        }

        foreach (EnemyController enemy in police)
        {
            SerializedObject enemyData = new SerializedObject(enemy);
            SerializedProperty combatEnabled = enemyData.FindProperty("combatEnabled");
            SerializedProperty detectionRange = enemyData.FindProperty("detectionRange");

            if (combatEnabled != null)
                combatEnabled.boolValue = false;

            if (detectionRange != null)
                detectionRange.floatValue = 16f;

            enemyData.ApplyModifiedPropertiesWithoutUndo();

            PoliceWeaponHandler weapons = enemy.GetComponent<PoliceWeaponHandler>();
            if (weapons != null)
            {
                int enemyIndex = int.Parse(enemy.name.Substring("Police_".Length));
                bool useMelee = melee.Contains(enemy) || (mixed.Contains(enemy) && enemyIndex % 2 == 0);
                SerializedObject weaponData = new SerializedObject(weapons);
                SerializedProperty startMelee = weaponData.FindProperty("startWithCachiporra");
                if (startMelee != null)
                    startMelee.boolValue = useMelee;

                weaponData.ApplyModifiedPropertiesWithoutUndo();
                weapons.SetCachiporraEquipped(useMelee);
                EditorUtility.SetDirty(weapons);
            }

            EditorUtility.SetDirty(enemy);
        }

        SerializedObject data = new SerializedObject(manager);
        SerializedProperty waves = data.FindProperty("waves");
        waves.arraySize = 3;

        WriteWave(waves.GetArrayElementAtIndex(0), "Melee", melee, true, 1.8f, 10f, 1.2f, 1f, false);
        WriteWave(waves.GetArrayElementAtIndex(1), "Mixed", mixed, true, 8f, 10f, 1.5f, 0.5f, true);
        WriteWave(waves.GetArrayElementAtIndex(2), "Ranged", ranged, false, 8f, 10f, 1.5f, 1f, false);

        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        MatecocidoWaveSetup.ConfigurePlayerBullet(false);
    }

    private static List<EnemyController> Enemies(Dictionary<int, EnemyController> byIndex, int from, int to)
    {
        List<EnemyController> result = new List<EnemyController>();
        for (int i = from; i <= to; i++)
        {
            if (byIndex.TryGetValue(i, out EnemyController enemy) && enemy != null)
                result.Add(enemy);
        }
        return result;
    }

    private static void WriteWave(SerializedProperty wave, string name, List<EnemyController> enemies,
        bool melee, float range, float damage, float cooldown, float previousFraction, bool alternateMelee)
    {
        wave.FindPropertyRelative("name").stringValue = name;
        wave.FindPropertyRelative("melee").boolValue = melee;
        wave.FindPropertyRelative("attackRange").floatValue = range;
        wave.FindPropertyRelative("damage").floatValue = damage;
        wave.FindPropertyRelative("attackCooldown").floatValue = cooldown;
        wave.FindPropertyRelative("spawnAfterPreviousDefeatedFraction").floatValue = previousFraction;
        wave.FindPropertyRelative("alternateMelee").boolValue = alternateMelee;

        SerializedProperty list = wave.FindPropertyRelative("enemies");
        list.arraySize = enemies.Count;
        for (int i = 0; i < enemies.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
    }

    private static void ConfigureNpc2()
    {
        AnimatorController controller = BuildAnimatorController();
        DialogueData first = GetOrCreateDialogue("Assets/Scripts/Dialogue_NPC2_First.asset",
            "Recepcionista",
            new[] { "Soy el encargado de la estación. Trae el dinero cuando termines el trabajo." },
            false);
        DialogueData repeat = GetOrCreateDialogue("Assets/Scripts/Dialogue_NPC2_Repeat.asset",
            "Recepcionista",
            new[] { "Todavía no tengo el dinero. Vuelve cuando lo tengas." },
            false);
        DialogueData final = GetOrCreateDialogue("Assets/Scripts/Dialogue_NPC2_Final.asset",
            "Recepcionista",
            new[] { "Gracias, hiciste un buen trabajo. El dinero queda en buenas manos." },
            true);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Npc2PrefabPath);
        if (prefab == null)
            prefab = BuildNpc2Prefab(controller, first, repeat, final);

        if (Resources.FindObjectsOfTypeAll<GameObject>().Any(go => go.name == "NPC2" && go.scene.IsValid()))
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Transform company = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(t => t.name == "Company" && t.gameObject.scene.IsValid());

        instance.transform.position = company != null
            ? company.position + new Vector3(0f, 0f, 6f)
            : new Vector3(-19.36f, 0f, -22.06f);

        EditorUtility.SetDirty(instance);
    }

    private static AnimatorController BuildAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Npc2ControllerPath);
        if (controller != null)
            return controller;

        controller = AnimatorController.CreateAnimatorControllerAtPath(Npc2ControllerPath);
        controller.AddParameter("IsSad", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsWaiting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsThankful", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "Idle", "Assets/Animations/npc2_idle.fbx", "idle");
        AnimatorState espera = AddState(machine, "Espera", "Assets/Animations/npc2_espera.fbx", "espera");
        AnimatorState corre = AddState(machine, "Corre", "Assets/Animations/npc2_corre.fbx", "corre");
        AnimatorState thankful = AddState(machine, "Thankful", "Assets/Animations/npc2_Thankful.fbx", "thankful");

        machine.defaultState = idle;

        AddTransition(idle, espera, AnimatorConditionMode.If, "IsWaiting");
        AddTransition(idle, espera, AnimatorConditionMode.If, "IsTalking");
        AddTransition(espera, idle, AnimatorConditionMode.IfNot, "IsWaiting", "IsTalking");
        AddTransition(idle, corre, AnimatorConditionMode.If, "IsRunning");
        AddTransition(espera, corre, AnimatorConditionMode.If, "IsRunning");
        AddTransition(corre, idle, AnimatorConditionMode.IfNot, "IsRunning");

        AnimatorStateTransition thanks = machine.AddAnyStateTransition(thankful);
        thanks.hasExitTime = false;
        thanks.duration = 0.1f;
        thanks.AddCondition(AnimatorConditionMode.If, 0f, "IsThankful");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode,
        params string[] parameters)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.1f;

        foreach (string parameter in parameters)
            transition.AddCondition(mode, 0f, parameter);
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, string path, string token)
    {
        AnimatorState state = machine.AddState(name);
        AnimationClip clip = LoadClip(path, token);
        if (clip != null)
            state.motion = clip;

        return state;
    }

    private static AnimationClip LoadClip(string path, string token)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
        return clips.FirstOrDefault(c => c.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            ?? clips.FirstOrDefault();
    }

    private static DialogueData GetOrCreateDialogue(string path, string npcName, string[] lines, bool stopsSad)
    {
        DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (data != null)
            return data;

        data = ScriptableObject.CreateInstance<DialogueData>();
        SerializedObject so = new SerializedObject(data);
        so.FindProperty("npcName").stringValue = npcName;
        so.FindProperty("npcStopsBeingSad").boolValue = stopsSad;

        SerializedProperty list = so.FindProperty("lines");
        list.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            SerializedProperty line = list.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("speaker").enumValueIndex = 0;
            line.FindPropertyRelative("text").stringValue = lines[i];
            line.FindPropertyRelative("playerTalkingVariant").intValue = 0;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static GameObject BuildNpc2Prefab(AnimatorController controller, DialogueData first,
        DialogueData repeat, DialogueData final)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/npc2_idle.fbx");
        GameObject root = new GameObject("NPC2");

        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
        }

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.height = 2f;
        collider.radius = 0.35f;

        NpcController npc = root.AddComponent<NpcController>();
        SerializedObject npcData = new SerializedObject(npc);
        SerializedProperty startSad = npcData.FindProperty("startSad");
        if (startSad != null)
            startSad.boolValue = false;
        npcData.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<MissionMarker>();
        NpcInteractable interactable = root.AddComponent<NpcInteractable>();

        SerializedObject data = new SerializedObject(interactable);
        data.FindProperty("firstDialogue").objectReferenceValue = first;
        data.FindProperty("repeatDialogue").objectReferenceValue = repeat;
        data.FindProperty("finalDeliveryDialogue").objectReferenceValue = final;
        data.FindProperty("isStationNpc").boolValue = true;
        data.FindProperty("requiredStateForFinalDelivery").enumValueIndex = (int)MissionState.DeliverToNpc2;
        data.FindProperty("requireFinalBattleCleared").boolValue = true;
        data.FindProperty("playThankfulOnFinalDelivery").boolValue = true;
        data.FindProperty("interactionRadius").floatValue = 2.5f;
        data.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Npc2PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }
}
#endif
