using System;
using System.IO;
using System.Linq;

namespace WDE.MangosEventAiEditor.Acid
{
    public static class AcidFileLocator
    {
        public static readonly string[] KnownAcidFileNames = { "acid_wotlk.sql", "acid_tbc.sql", "acid_classic.sql" };

        /// <summary>
        /// Accepts either the repository root (wotlk-db) or its ACID subfolder and returns
        /// the repository root, or null when neither contains a known acid_*.sql file.
        /// </summary>
        public static string? NormalizeRepositoryPath(string? pickedFolder)
        {
            if (string.IsNullOrWhiteSpace(pickedFolder))
                return null;

            if (HasKnownAcidFile(Path.Combine(pickedFolder, "ACID")))
                return pickedFolder;

            var trimmed = Path.TrimEndingDirectorySeparator(pickedFolder);
            if (string.Equals(Path.GetFileName(trimmed), "ACID", StringComparison.OrdinalIgnoreCase) &&
                HasKnownAcidFile(trimmed))
                return Path.GetDirectoryName(trimmed);

            return null;
        }

        private static bool HasKnownAcidFile(string directory)
        {
            if (!Directory.Exists(directory))
                return false;
            return KnownAcidFileNames.Any(name => File.Exists(Path.Combine(directory, name)));
        }

        /// <summary>
        /// Given the path to a cmangos *-db repository, finds the ACID .sql file
        /// (e.g. ACID/acid_wotlk.sql), preferring the file matching the current core.
        /// </summary>
        public static string? Locate(string? dbRepositoryPath, string coreTag)
        {
            if (string.IsNullOrWhiteSpace(dbRepositoryPath))
                return null;

            var acidDirectory = Path.Combine(dbRepositoryPath, "ACID");
            if (!Directory.Exists(acidDirectory))
                return null;

            var files = Directory.GetFiles(acidDirectory, "*.sql");
            if (files.Length == 0)
                return null;
            if (files.Length == 1)
                return files[0];

            string? hint = coreTag switch
            {
                "CMaNGOS-WoTLK" => "wotlk",
                "CMaNGOS-TBC" => "tbc",
                "CMaNGOS-Classic" => "classic",
                _ => null
            };
            if (hint != null)
            {
                var match = files.FirstOrDefault(f => Path.GetFileName(f).Contains(hint, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }

            Array.Sort(files, StringComparer.Ordinal);
            return files[0];
        }
    }
}
