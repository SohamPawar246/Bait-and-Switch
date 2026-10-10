#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BaitSwitch.EditorTools
{
    /// <summary>
    /// Tools > Bait & Switch > Build Part Catalog
    /// Creates/updates one PartDef asset per spec in PartCatalog.Specs(), wires prefabs and thumbnails
    /// from the Blender export folders by naming convention, and writes Resources/PartCatalog.asset.
    /// Also creates Resources/GameConfig.asset if missing. Safe to re-run any time.
    /// </summary>
    public static class PartCatalogBuilder
    {
        const string PartsDir = "Assets/_Game/Data/Parts";
        const string ResDir = "Assets/_Game/Resources";
        const string MeshDir = "Assets/_Game/Art/Parts";   // FBX per part from Blender: <slot>_<name>.fbx
        const string ThumbDir = "Assets/_Game/Art/Thumbs"; // PNG per part from Blender: <slot>_<name>.png

        [MenuItem("Tools/Bait & Switch/Build Part Catalog")]
        public static void Build()
        {
            Directory.CreateDirectory(PartsDir);
            Directory.CreateDirectory(ResDir);

            var catPath = $"{ResDir}/PartCatalog.asset";
            var cat = AssetDatabase.LoadAssetAtPath<PartCatalog>(catPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<PartCatalog>(); AssetDatabase.CreateAsset(cat, catPath); }
            cat.parts.Clear();

            foreach (var s in PartCatalog.Specs())
            {
                string file = $"{s.slot}_{Slug(s.name)}";
                string path = $"{PartsDir}/{file}.asset";
                var p = AssetDatabase.LoadAssetAtPath<PartDef>(path);
                if (p == null) { p = ScriptableObject.CreateInstance<PartDef>(); AssetDatabase.CreateAsset(p, path); }

                p.id = s.id; p.slot = s.slot; p.displayName = s.name; p.plus = s.plus; p.minus = s.minus;
                p.cosmetic = s.cosmetic; p.abilities = s.abilities; p.tint = s.cosmetic ? s.tint : Color.white;
                p.mods.Clear(); if (s.mods != null) p.mods.AddRange(s.mods);

                string key = $"{s.slot.ToString().ToLowerInvariant()}_{Slug(s.name)}";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{MeshDir}/{key}.fbx");
                if (prefab != null) p.prefab = prefab;
                var thumb = AssetDatabase.LoadAssetAtPath<Sprite>($"{ThumbDir}/{key}.png");
                if (thumb != null) p.thumbnail = thumb;

                EditorUtility.SetDirty(p);
                cat.parts.Add(p);
            }
            EditorUtility.SetDirty(cat);

            var cfgPath = $"{ResDir}/GameConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(cfgPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(), cfgPath);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Bait & Switch] Part catalog built: {cat.parts.Count} parts.");
        }

        static string Slug(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s.ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString().Trim('_');
        }
    }
}
#endif
