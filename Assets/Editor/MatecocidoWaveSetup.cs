#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MatecocidoWaveSetup
{
    [MenuItem("Matecocido/(Legacy) Configure Ranged Wave")]
    public static void ConfigureRangedWave()
    {
        MatecocidoFullSetup.RunAll();
    }

    private static void LegacyConfigureRangedWave()
    {
        EnemyWaveManager manager = Object.FindFirstObjectByType<EnemyWaveManager>();

        if (manager == null)
        {
            Debug.LogError("MatecocidoWaveSetup: no se encontró EnemyWaveManager en la escena.");
            return;
        }

        List<EnemyController> ranged = new List<EnemyController>();

        for (int i = 7; i <= 12; i++)
        {
            string objectName = $"Police_{i:00}";
            GameObject police = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => go.name == objectName && go.scene.IsValid());

            if (police == null)
            {
                Debug.LogError($"MatecocidoWaveSetup: no se encontró {objectName}.");
                continue;
            }

            EnemyController enemy = police.GetComponent<EnemyController>();

            if (enemy == null)
            {
                Debug.LogError($"MatecocidoWaveSetup: {objectName} no tiene EnemyController.");
                continue;
            }

            PoliceWeaponHandler weapons = police.GetComponent<PoliceWeaponHandler>();

            if (weapons != null)
            {
                SerializedObject weaponData = new SerializedObject(weapons);
                SerializedProperty startMelee = weaponData.FindProperty("startWithCachiporra");

                if (startMelee != null)
                    startMelee.boolValue = false;

                weaponData.ApplyModifiedPropertiesWithoutUndo();
                weapons.SetCachiporraEquipped(false);
                EditorUtility.SetDirty(weapons);
            }

            SerializedObject enemyData = new SerializedObject(enemy);
            SerializedProperty combatEnabled = enemyData.FindProperty("combatEnabled");
            SerializedProperty detectionRange = enemyData.FindProperty("detectionRange");

            if (combatEnabled != null)
                combatEnabled.boolValue = false;

            if (detectionRange != null)
                detectionRange.floatValue = 16f;

            enemyData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            ranged.Add(enemy);
        }

        if (ranged.Count == 0)
        {
            Debug.LogError("MatecocidoWaveSetup: no se configuró ningún policía.");
            return;
        }

        manager.ConfigureWave(1, "Ranged", ranged, false, 8f, 10f, 1.5f);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();

        ConfigurePlayerBullet(false);

        Debug.Log($"MatecocidoWaveSetup: oleada 2 configurada con {ranged.Count} policías que disparan.");
    }

    [MenuItem("Matecocido/Assign Player Bullet Prefab")]
    public static void ConfigurePlayerBullet()
    {
        ConfigurePlayerBullet(true);
    }

    public static void ConfigurePlayerBullet(bool saveScene)
    {
        EnemyProjectile bullet = AssetDatabase.LoadAssetAtPath<EnemyProjectile>("Assets/Prefabs/PoliceBullet.prefab");

        if (bullet == null)
        {
            Debug.LogError("MatecocidoWaveSetup: no se encontró Assets/Prefabs/PoliceBullet.prefab.");
            return;
        }

        PlayerCombat combat = Object.FindFirstObjectByType<PlayerCombat>();

        if (combat == null)
        {
            Debug.LogError("MatecocidoWaveSetup: no se encontró PlayerCombat en la escena.");
            return;
        }

        SerializedObject data = new SerializedObject(combat);
        SerializedProperty projectile = data.FindProperty("projectilePrefab");

        if (projectile != null)
            projectile.objectReferenceValue = bullet;

        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(combat);
        EditorSceneManager.MarkSceneDirty(combat.gameObject.scene);

        if (saveScene)
            EditorSceneManager.SaveOpenScenes();

        Debug.Log("MatecocidoWaveSetup: PoliceBullet.prefab asignado a PlayerCombat.");
    }
}
#endif
