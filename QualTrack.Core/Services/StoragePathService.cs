using System;
using System.IO;

namespace QualTrack.Core.Services
{
    public static class StoragePathService
    {
        private static string? _basePath;

        public static bool IsInitialized => !string.IsNullOrWhiteSpace(_basePath);

        public static string BasePath => _basePath ?? string.Empty;

        public static void Initialize(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException("Base path cannot be empty.", nameof(basePath));
            }

            _basePath = Path.GetFullPath(basePath);
            EnsureDirectoryStructure();
        }

        public static string GetDatabasePath(string fallbackPath = "qualtrack.db")
        {
            if (!IsInitialized)
            {
                return fallbackPath;
            }

            return EnsureSubpath("data", "qualtrack.db");
        }

        public static string GetPendingDocsPath(string fallbackPath)
        {
            if (!IsInitialized)
            {
                return fallbackPath;
            }

            return EnsureSubpath("docs", "pending");
        }

        public static string GetSignedDocsPath(string fallbackPath)
        {
            if (!IsInitialized)
            {
                return fallbackPath;
            }

            return EnsureSubpath("docs", "signed");
        }

        public static string GetArchiveDocsPath(string fallbackPath)
        {
            if (!IsInitialized)
            {
                return fallbackPath;
            }

            return EnsureSubpath("docs", "archive");
        }

        public static string GetGeneratedDocsPath(string subfolder, string fallbackPath)
        {
            if (!IsInitialized)
            {
                return fallbackPath;
            }

            return EnsureSubpath("docs", "generated", subfolder);
        }

        public static string GetTempPath(string fallbackPath)
        {
            if (!IsInitialized)
            {
                return fallbackPath;
            }

            return EnsureSubpath("temp");
        }

        private static void EnsureDirectoryStructure()
        {
            EnsureSubpath("data");
            EnsureSubpath("docs");
            EnsureSubpath("docs", "pending");
            EnsureSubpath("docs", "signed");
            EnsureSubpath("docs", "archive");
            EnsureSubpath("docs", "generated");
            EnsureSubpath("temp");
        }

        private static string EnsureSubpath(params string[] parts)
        {
            var path = _basePath ?? string.Empty;
            foreach (var part in parts)
            {
                path = Path.Combine(path, part);
            }

            var directoryPath = Path.HasExtension(path) ? Path.GetDirectoryName(path) : path;
            if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return path;
        }
    }
}
