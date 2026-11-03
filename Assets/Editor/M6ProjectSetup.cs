using System;
using System.Collections.Generic;
using System.IO;
using Survivor.Combat;
using Survivor.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Survivor.Editor
{
    public static class M6ProjectSetup
    {
        [MenuItem("Survivor/Create M6 Assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play Mode and save edits first.");
            var flow = AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            if (flow == null)
            {
                string[] matches = AssetDatabase.FindAssets("t:FlowConfig", new[] { "Assets/Data" });
                if (matches.Length != 1) throw new InvalidOperationException("Expected one FlowConfig");
                flow = AssetDatabase.LoadAssetAtPath<FlowConfig>(AssetDatabase.GUIDToAssetPath(matches[0]));
            }
            var battle = AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            var progression = battle.progression;
            var abilities = new List<UpgradeDefinition>();
            foreach (var weapon in flow.weapons)
            {
                weapon.combatAvailable = true; weapon.maxLevel = 5;
                switch (weapon.pattern)
                {
                    case WeaponPattern.Straight: weapon.cooldown = 0.55f; weapon.range = 12; weapon.damageMultiplier = 1.6f; weapon.maxHits = 1; break;
                    case WeaponPattern.Orbit: weapon.cooldown = 1.6f; weapon.range = 3; weapon.damageMultiplier = 1.2f; weapon.areaRadius = 2.4f; weapon.maxHits = 3; weapon.projectileLifetime = 1.6f; break;
                    case WeaponPattern.Blast: weapon.cooldown = 1.8f; weapon.range = 10; weapon.damageMultiplier = 2.2f; weapon.areaRadius = 2.2f; break;
                    case WeaponPattern.Homing: weapon.cooldown = 0.9f; weapon.range = 14; weapon.damageMultiplier = 1.4f; weapon.projectileSpeed = 9; weapon.projectileLifetime = 3; weapon.maxHits = 1; break;
                    case WeaponPattern.Pulse: weapon.cooldown = 2.2f; weapon.range = 5; weapon.damageMultiplier = 1.8f; weapon.areaRadius = 4; break;
                }
                EditorUtility.SetDirty(weapon);
                var second = weapon.pattern == WeaponPattern.Straight || weapon.pattern == WeaponPattern.Homing || weapon.pattern == WeaponPattern.Orbit
                    ? Effect(weapon.id + ".pierce", UpgradeEffect.Pierce, 1) : Effect(weapon.id + ".area", UpgradeEffect.WeaponArea, 0.4f);
                abilities.Add(Ability(weapon.id, UpgradeCategory.Weapon, Effect(weapon.id + ".new", UpgradeEffect.WeaponDamage, 0), Effect(weapon.id + ".damage", UpgradeEffect.WeaponDamage, 0.25f), second));
            }
            abilities.Add(Ability("bloom", UpgradeCategory.Passive, Effect("bloom.new", UpgradeEffect.RegenAmount, 2), Effect("bloom.amount", UpgradeEffect.RegenAmount, 2), Effect("bloom.interval", UpgradeEffect.RegenInterval, 0.5f)));
            abilities.Add(Ability("magnet", UpgradeCategory.Passive, Effect("magnet.new", UpgradeEffect.MagnetRadius, 0.5f), Effect("magnet.radius", UpgradeEffect.MagnetRadius, 0.5f), Effect("magnet.speed", UpgradeEffect.MagnetSpeed, 1)));
            abilities.Add(Ability("echo", UpgradeCategory.Passive, Effect("echo.new", UpgradeEffect.EchoDamage, 0.25f), Effect("echo.damage", UpgradeEffect.EchoDamage, 0.15f)));
            abilities.Add(Ability("thorns", UpgradeCategory.Passive, Effect("thorns.new", UpgradeEffect.ThornsDamage, 1.2f), Effect("thorns.damage", UpgradeEffect.ThornsDamage, 0.3f)));
            abilities.Add(Ability("trail", UpgradeCategory.Passive, Effect("trail.new", UpgradeEffect.TrailDamage, 0.5f), Effect("trail.damage", UpgradeEffect.TrailDamage, 0.2f)));
            abilities.Add(Ability("storm", UpgradeCategory.Skill, Effect("storm.new", UpgradeEffect.StormDamage, 2), Effect("storm.damage", UpgradeEffect.StormDamage, 0.4f)));
            abilities.Add(Ability("sanctuary", UpgradeCategory.Skill, Effect("sanctuary.new", UpgradeEffect.SanctuaryDefense, 4), Effect("sanctuary.defense", UpgradeEffect.SanctuaryDefense, 2)));
            foreach (var pair in new[] { ("health", UpgradeEffect.Health, 20f), ("attack", UpgradeEffect.Attack, 2f), ("speed", UpgradeEffect.AttackSpeed, 0.12f), ("move", UpgradeEffect.MoveSpeed, 0.5f), ("defense", UpgradeEffect.Defense, 3f) })
                abilities.Add(Ability("stat." + pair.Item1, UpgradeCategory.Attribute, null, Effect("stat." + pair.Item1, pair.Item2, pair.Item3)));
            progression.abilities = abilities.ToArray(); EditorUtility.SetDirty(progression);
            Directory.CreateDirectory("Assets/Prefabs/Effects"); AssetDatabase.Refresh();
            const string areaPath = "Assets/Prefabs/Effects/AreaEffect.prefab";
            if (!File.Exists(areaPath))
            {
                var root = new GameObject("AreaEffect", typeof(AreaEffectActor), typeof(SpriteRenderer));
                var area = root.GetComponent<AreaEffectActor>(); area.body = root.GetComponent<SpriteRenderer>();
                area.body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Circle.png");
                area.body.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlaceholderUnlit.mat");
                area.body.sortingOrder = 1; root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, areaPath); Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity"); var world = Object.FindFirstObjectByType<BattleWorld>();
            world.areaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(areaPath).GetComponent<AreaEffectActor>();
            EditorUtility.SetDirty(world); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        private static UpgradeEffectDefinition Effect(string id, UpgradeEffect kind, float amount)
            => new UpgradeEffectDefinition { id = id, effect = kind, amount = amount, nameKey = "upgrade." + id, descriptionKey = "upgrade." + id + ".desc" };
        private static UpgradeDefinition Ability(string id, UpgradeCategory category, UpgradeEffectDefinition acquisition, params UpgradeEffectDefinition[] effects)
        {
            string path = "Assets/Data/Progression/" + id + ".asset";
            var ability = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (ability == null) { ability = ScriptableObject.CreateInstance<UpgradeDefinition>(); AssetDatabase.CreateAsset(ability, path); }
            ability.id = id; ability.category = category; ability.maxLevel = 5; ability.acquisition = acquisition; ability.effects = effects;
            EditorUtility.SetDirty(ability); return ability;
        }
    }
}
