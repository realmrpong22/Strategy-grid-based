namespace Tactics.Editor
{
    /// <summary>Where the CSV sources live and where generated assets are written.</summary>
    internal static class DataPaths
    {
        public const string CsvRoot = "Assets/_Project/Data/Csv";
        public const string TerrainCsv = CsvRoot + "/terrain.csv";
        public const string MapsCsvFolder = CsvRoot + "/Maps";

        public const string GeneratedRoot = "Assets/_Project/Data/Generated";
        public const string GeneratedTerrainFolder = GeneratedRoot + "/Terrain";
        public const string GeneratedMapsFolder = GeneratedRoot + "/Maps";
    }
}
