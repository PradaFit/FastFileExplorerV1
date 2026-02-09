using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Fastest_FileExplorer.Core
{
    public static class PathSecurity
    {
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static readonly string[] DangerousPatterns = { "..", "..\\", "../", "\\\\?\\", "\\\\." };

        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (path.Length > 260)
                return false;

            if (path.IndexOfAny(InvalidPathChars) >= 0)
                return false;

            foreach (var pattern in DangerousPatterns)
            {
                if (path.Contains(pattern))
                    return false;
            }

            return true;
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (fileName.Length > 255)
                return false;

            if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
                return false;

            return true;
        }

        public static string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            path = path.Trim();

            foreach (var c in InvalidPathChars)
            {
                path = path.Replace(c.ToString(), "");
            }

            while (path.Contains(".."))
            {
                path = path.Replace("..", "");
            }

            return path;
        }

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            fileName = fileName.Trim();

            foreach (var c in InvalidFileNameChars)
            {
                fileName = fileName.Replace(c.ToString(), "_");
            }

            if (fileName.Length > 255)
                fileName = fileName.Substring(0, 255);

            return fileName;
        }

        public static bool IsPathWithinBounds(string basePath, string targetPath)
        {
            try
            {
                var fullBase = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
                var fullTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar);

                return fullTarget.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase) ||
                       IsRootPath(fullTarget);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRootPath(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var root = Path.GetPathRoot(fullPath);
                return !string.IsNullOrEmpty(root);
            }
            catch
            {
                return false;
            }
        }

        public static bool CanAccessPath(string path)
        {
            try
            {
                if (!IsValidPath(path))
                    return false;

                var fullPath = Path.GetFullPath(path);

                if (Directory.Exists(fullPath))
                {
                    var testAccess = Directory.GetDirectories(fullPath);
                    return true;
                }

                if (File.Exists(fullPath))
                {
                    var attr = File.GetAttributes(fullPath);
                    return true;
                }

                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool IsSearchQuerySafe(string query)
        {
            if (string.IsNullOrEmpty(query))
                return true;

            if (query.Length > 500)
                return false;

            if (query.IndexOfAny(InvalidPathChars) >= 0)
                return false;

            return true;
        }

        public static string SanitizeSearchQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            query = query.Trim();

            foreach (var c in InvalidPathChars)
            {
                query = query.Replace(c.ToString(), "");
            }

            if (query.Length > 500)
                query = query.Substring(0, 500);

            return query;
        }
    }
}