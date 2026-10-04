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
            Dictionary<string, UnitClassData> classAssets = ImportUnitClasses(errors, warnings);
            int failedMaps = 0;
            var maps = terrainAssets != null
                ? ImportMaps(terrainAssets, errors, ref failedMaps)
                : new Dictionary<string, (MapData Asset, GridMap Grid)>();

            // Deployments need both maps and classes; without classes there is nothing to validate against.
            int importedDeployments = 0;
            int failedDeployments = 0;
            if (terrainAssets != null && classAssets != null)
                importedDeployments = ImportDeployments(maps, classAssets, errors, ref failedDeployments);

            AssetDatabase.SaveAssets();

            foreach (string warning in warnings)
                Debug.LogWarning($"[Tactics] {warning}");

            var parts = new List<string>
            {
                terrainAssets == null ? "terrain FAILED (nothing written, maps skipped)" : $"{terrainAssets.Count} terrain type(s)",
                classAssets == null ? "unit classes FAILED (nothing written)" : $"{classAssets.Count} unit class(es)",
            };
            if (terrainAssets != null)
                parts.Add(Counted("map(s)", maps.Count, failedMaps));
            parts.Add(terrainAssets != null && classAssets != null
                ? Counted("deployment(s)", importedDeployments, failedDeployments)
                : "deployments skipped");

            string summary = $"[Tactics] Imported {string.Join(", ", parts)}";
            if (errors.Count > 0)
            {
                Debug.LogError($"{summary}:\n{string.Join("\n", errors)}");
                return false;
            }

            Debug.Log($"{summary}.");
            return true;

            static string Counted(string what, int imported, int failed) =>
                failed == 0 ? $"{imported} {what}" : $"{imported} {what}, {failed} failed and not written";
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
                    definition.Id, row.DisplayName, definition.MoveCost, definition.IsPassable, definition.IsFlyable,
                    definition.DefenseBonus, definition.AvoidBonus, row.Color);
                EditorUtility.SetDirty(asset);
                assets.Add(row.Id, asset);
            }

            WarnAboutOrphans(DataPaths.GeneratedTerrainFolder, assets.Values, Path.GetFileName(DataPaths.TerrainCsv), warnings);
            return assets;
        }

        /// <summary>All-or-nothing: deployments reference classes, so a bad unit_classes.csv writes nothing.</summary>
        private static Dictionary<string, UnitClassData> ImportUnitClasses(List<string> errors, List<string> warnings)
        {
            if (!File.Exists(DataPaths.UnitClassesCsv))
            {
                errors.Add($"Missing {DataPaths.UnitClassesCsv}.");
                return null;
            }

            string csvName = Path.GetFileName(DataPaths.UnitClassesCsv);
            CsvParseResult<IReadOnlyList<UnitClassRow>> parsed =
                UnitClassCsvParser.Parse(File.ReadAllText(DataPaths.UnitClassesCsv), csvName);
            if (!parsed.Success)
            {
                errors.AddRange(parsed.Errors);
                return null;
            }

            EnsureFolder(DataPaths.GeneratedUnitClassesFolder);
            var assets = new Dictionary<string, UnitClassData>();

            foreach (UnitClassRow row in parsed.Value)
            {
                UnitClassData asset = LoadOrCreate<UnitClassData>(
                    $"{DataPaths.GeneratedUnitClassesFolder}/UnitClass_{row.Id}.asset");
                UnitClassDefinition definition = row.Definition;
                asset.SetValues(
                    definition.Id, row.DisplayName, definition.MaxHp, definition.Move, definition.Jump,
                    definition.Flies, row.Color);
                EditorUtility.SetDirty(asset);
                assets.Add(row.Id, asset);
            }

            WarnAboutOrphans(DataPaths.GeneratedUnitClassesFolder, assets.Values, csvName, warnings);
            return assets;
        }

        /// <summary>Never deletes automatically: something may still reference the asset.</summary>
        private static void WarnAboutOrphans<T>(
            string folder, ICollection<T> current, string csvName, List<string> warnings) where T : ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && !current.Contains(asset))
                    warnings.Add($"{path} is no longer in {csvName}. Delete it if nothing uses it.");
            }
        }

        /// <returns>Maps written this run, by file name without extension, with their validated grid.</returns>
        private static Dictionary<string, (MapData Asset, GridMap Grid)> ImportMaps(
            Dictionary<string, TerrainTypeData> terrainAssets, List<string> errors, ref int failed)
        {
            var imported = new Dictionary<string, (MapData, GridMap)>();
            if (!Directory.Exists(DataPaths.MapsCsvFolder))
            {
                errors.Add($"Missing folder {DataPaths.MapsCsvFolder}.");
                return imported;
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

                string mapName = Path.GetFileNameWithoutExtension(file);
                MapData asset = LoadOrCreate<MapData>($"{DataPaths.GeneratedMapsFolder}/{mapName}.asset");
                asset.SetLayout(layout.Width, layout.Depth, layout.TerrainIds, layout.Heights, usedTerrainTypes);
                EditorUtility.SetDirty(asset);
                imported.Add(mapName, (asset, check.Map));
            }

            return imported;
        }

        /// <summary>
        /// Each Deployments/X.csv is validated against Maps/X.csv as imported this run, so a deployment
        /// is never written against a map that failed or no longer exists.
        /// </summary>
        private static int ImportDeployments(
            Dictionary<string, (MapData Asset, GridMap Grid)> maps, Dictionary<string, UnitClassData> classAssets,
            List<string> errors, ref int failed)
        {
            if (!Directory.Exists(DataPaths.DeploymentsCsvFolder))
            {
                errors.Add($"Missing folder {DataPaths.DeploymentsCsvFolder}.");
                return 0;
            }

            EnsureFolder(DataPaths.GeneratedDeploymentsFolder);
            Dictionary<string, UnitClassDefinition> classes =
                classAssets.ToDictionary(pair => pair.Key, pair => pair.Value.ToDefinition());

            IEnumerable<string> files = Directory.GetFiles(DataPaths.DeploymentsCsvFolder)
                .Where(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.Ordinal);

            int imported = 0;
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string mapName = Path.GetFileNameWithoutExtension(file);
                if (!maps.TryGetValue(mapName, out (MapData Asset, GridMap Grid) map))
                {
                    errors.Add($"{fileName}: no map '{mapName}' was imported (Maps/{mapName}.csv is missing or has errors).");
                    failed++;
                    continue;
                }

                CsvParseResult<IReadOnlyList<DeploymentRow>> parsed =
                    DeploymentCsvParser.Parse(File.ReadAllText(file), fileName, classes, map.Grid);
                if (!parsed.Success)
                {
                    errors.AddRange(parsed.Errors);
                    failed++;
                    continue;
                }

                UnitPlacement[] placements = parsed.Value
                    .Select(row => new UnitPlacement(classAssets[row.ClassId], row.Faction, row.Position))
                    .ToArray();

                DeploymentData asset = LoadOrCreate<DeploymentData>(
                    $"{DataPaths.GeneratedDeploymentsFolder}/Deployment_{mapName}.asset");
                asset.SetValues(map.Asset, placements);
                EditorUtility.SetDirty(asset);
                imported++;
            }

            return imported;
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
