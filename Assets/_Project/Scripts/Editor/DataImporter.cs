using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tactics.Core;
using Tactics.Data;
using UnityEditor;
using UnityEngine;

namespace Tactics.Editor
{
    /// <summary>
    /// Regenerates data assets from the CSVs under DataPaths.CsvRoot.
    /// Existing assets are updated in place (same file, same GUID), so scene and prefab
    /// references survive a re-import. A file with errors is not written at all; the last
    /// good asset stays.
    /// </summary>
    public static class DataImporter
    {
        [MenuItem("Tactics/Import All Data")]
        private static void ImportAllMenu() => ImportAll();

        /// <returns>True if everything imported without errors. Public so it can run from batch mode / CI later.</returns>
        public static bool ImportAll()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            Dictionary<string, TerrainTypeData> terrainAssets = ImportTerrain(errors, warnings);
            int importedMaps = 0;
            int failedMaps = 0;
            if (terrainAssets != null)
                ImportMaps(terrainAssets, errors, ref importedMaps, ref failedMaps);

            AssetDatabase.SaveAssets();

            foreach (string warning in warnings)
                Debug.LogWarning($"[Tactics] {warning}");

            if (terrainAssets == null)
            {
                Debug.LogError($"[Tactics] Terrain import failed; nothing was written.\n{string.Join("\n", errors)}");
                return false;
            }

            string summary = $"[Tactics] Imported {terrainAssets.Count} terrain type(s) and {importedMaps} map(s)";
            if (errors.Count > 0)
            {
                Debug.LogError($"{summary}; {failedMaps} map(s) failed and were not written:\n{string.Join("\n", errors)}");
                return false;
            }

            Debug.Log($"{summary}.");
            return true;
        }

        /// <summary>All-or-nothing: maps depend on terrain, so a bad terrain.csv writes nothing.</summary>
        private static Dictionary<string, TerrainTypeData> ImportTerrain(List<string> errors, List<string> warnings)
        {
            if (!File.Exists(DataPaths.TerrainCsv))
            {
                errors.Add($"Missing {DataPaths.TerrainCsv}.");
                return null;
            }

            CsvParseResult<IReadOnlyList<TerrainRow>> parsed =
                TerrainCsvParser.Parse(File.ReadAllText(DataPaths.TerrainCsv), Path.GetFileName(DataPaths.TerrainCsv));
            if (!parsed.Success)
            {
                errors.AddRange(parsed.Errors);
                return null;
            }

            EnsureFolder(DataPaths.GeneratedTerrainFolder);
            var assets = new Dictionary<string, TerrainTypeData>();

            foreach (TerrainRow row in parsed.Value)
            {
                TerrainTypeData asset = LoadOrCreate<TerrainTypeData>(
                    $"{DataPaths.GeneratedTerrainFolder}/Terrain_{row.Id}.asset");
                TerrainDefinition definition = row.Definition;
                asset.SetValues(
                    definition.Id, row.DisplayName, definition.MoveCost, definition.IsPassable,
                    definition.DefenseBonus, definition.AvoidBonus, row.Color);
                EditorUtility.SetDirty(asset);
                assets.Add(row.Id, asset);
            }

            // Never delete automatically: something may still reference the asset.
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TerrainTypeData)}", new[] { DataPaths.GeneratedTerrainFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TerrainTypeData>(path);
                if (asset != null && !assets.ContainsValue(asset))
                    warnings.Add($"{path} is no longer in terrain.csv. Delete it if nothing uses it.");
            }

            return assets;
        }

        private static void ImportMaps(
            Dictionary<string, TerrainTypeData> terrainAssets, List<string> errors, ref int imported, ref int failed)
        {
            if (!Directory.Exists(DataPaths.MapsCsvFolder))
            {
                errors.Add($"Missing folder {DataPaths.MapsCsvFolder}.");
                return;
            }

            EnsureFolder(DataPaths.GeneratedMapsFolder);
            List<TerrainDefinition> definitions = terrainAssets.Values.Select(t => t.ToDefinition()).ToList();

            IEnumerable<string> files = Directory.GetFiles(DataPaths.MapsCsvFolder)
                .Where(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.Ordinal);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                CsvParseResult<MapLayout> parsed = MapCsvParser.Parse(File.ReadAllText(file), fileName, terrainAssets.Keys);
                if (!parsed.Success)
                {
                    errors.AddRange(parsed.Errors);
                    failed++;
                    continue;
                }

                // Final gate: the same validation the game runs when it loads the map.
                MapLayout layout = parsed.Value;
                GridBuildResult check = GridMapBuilder.Build(
                    layout.Width, layout.Depth, layout.TerrainIds, layout.Heights, definitions);
                if (!check.Success)
                {
                    errors.AddRange(check.Errors.Select(e => $"{fileName}: {e}"));
                    failed++;
                    continue;
                }

                TerrainTypeData[] usedTerrainTypes = layout.TerrainIds
                    .Distinct()
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .Select(id => terrainAssets[id])
                    .ToArray();

                MapData asset = LoadOrCreate<MapData>(
                    $"{DataPaths.GeneratedMapsFolder}/{Path.GetFileNameWithoutExtension(file)}.asset");
                asset.SetLayout(layout.Width, layout.Depth, layout.TerrainIds, layout.Heights, usedTerrainTypes);
                EditorUtility.SetDirty(asset);
                imported++;
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
