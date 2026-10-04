namespace Tactics.Editor
{
    /// <summary>Where the CSV sources live and where generated assets are written.</summary>
    internal static class DataPaths
    {
        public const string CsvRoot = "Assets/_Project/Data/Csv";
        public const string TerrainCsv = CsvRoot + "/terrain.csv";
        public const string UnitClassesCsv = CsvRoot + "/unit_classes.csv";
        public const string MapsCsvFolder = CsvRoot + "/Maps";
        /// <summary>One CSV per map, same file name as the map it deploys onto.</summary>
        public const string DeploymentsCsvFolder = CsvRoot + "/Deployments";

        public const string GeneratedRoot = "Assets/_Project/Data/Generated";
        public const string GeneratedTerrainFolder = GeneratedRoot + "/Terrain";
        public const string GeneratedUnitClassesFolder = GeneratedRoot + "/UnitClasses";
        public const string GeneratedMapsFolder = GeneratedRoot + "/Maps";
        public const string GeneratedDeploymentsFolder = GeneratedRoot + "/Deployments";
    }
}
