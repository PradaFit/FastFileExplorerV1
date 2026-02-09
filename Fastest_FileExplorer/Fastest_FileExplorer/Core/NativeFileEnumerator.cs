using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Fastest_FileExplorer.Core
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeFileEnumerator
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFindHandle FindFirstFileW(string lpFileName, out WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool FindNextFileW(SafeFindHandle hFindFile, out WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

        private const int ERROR_NO_MORE_FILES = 18;
        private const int ERROR_ACCESS_DENIED = 5;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeFindHandle() : base(true) { }

            protected override bool ReleaseHandle()
            {
                return FindClose(handle);
            }
        }

        public static void EnumerateDirectory(
            string path,
            Action<string, string, long, DateTime, DateTime, bool> onFile,
            Action<string, string> onDirectory,
            Func<bool> shouldCancel)
        {
            if (string.IsNullOrEmpty(path)) return;

            var searchPath = path.TrimEnd('\\') + "\\*";

            try
            {
                using (var handle = FindFirstFileW(searchPath, out var findData))
                {
                    if (handle.IsInvalid)
                    {
                        return;
                    }

                    do
                    {
                        if (shouldCancel != null && shouldCancel()) return;

                        var fileName = findData.cFileName;

                        if (fileName == "." || fileName == "..") continue;

                        var fullPath = path.TrimEnd('\\') + "\\" + fileName;
                        var isDirectory = (findData.dwFileAttributes & FileAttributes.Directory) != 0;

                        if (isDirectory)
                        {
                            if ((findData.dwFileAttributes & FileAttributes.ReparsePoint) != 0) continue;

                            onDirectory?.Invoke(fullPath, fileName);
                        }
                        else
                        {
                            var size = ((long)findData.nFileSizeHigh << 32) | findData.nFileSizeLow;
                            var lastWrite = FileTimeToDateTime(findData.ftLastWriteTime);
                            var created = FileTimeToDateTime(findData.ftCreationTime);

                            onFile?.Invoke(fullPath, fileName, size, lastWrite, created, false);
                        }
                    }
                    while (FindNextFileW(handle, out findData));
                }
            }
            catch { }
        }

        public static void EnumerateFilesRecursive(
            string path,
            Action<string, string, long, DateTime, DateTime> onFile,
            Func<string, bool> shouldSkipDirectory,
            Func<bool> shouldCancel,
            int maxDepth = 50)
        {
            EnumerateRecursiveInternal(path, onFile, shouldSkipDirectory, shouldCancel, 0, maxDepth);
        }

        private static void EnumerateRecursiveInternal(
            string path,
            Action<string, string, long, DateTime, DateTime> onFile,
            Func<string, bool> shouldSkipDirectory,
            Func<bool> shouldCancel,
            int currentDepth,
            int maxDepth)
        {
            if (currentDepth > maxDepth) return;
            if (shouldCancel != null && shouldCancel()) return;
            if (string.IsNullOrEmpty(path)) return;

            var directories = new List<string>(64);

            var searchPath = path.TrimEnd('\\') + "\\*";

            try
            {
                using (var handle = FindFirstFileW(searchPath, out var findData))
                {
                    if (handle.IsInvalid) return;

                    do
                    {
                        if (shouldCancel != null && shouldCancel()) return;

                        var fileName = findData.cFileName;
                        if (fileName == "." || fileName == "..") continue;

                        var fullPath = path.TrimEnd('\\') + "\\" + fileName;
                        var isDirectory = (findData.dwFileAttributes & FileAttributes.Directory) != 0;

                        if (isDirectory)
                        {
                            if ((findData.dwFileAttributes & FileAttributes.ReparsePoint) != 0) continue;
                            if ((findData.dwFileAttributes & FileAttributes.Hidden) != 0 &&
                                (findData.dwFileAttributes & FileAttributes.System) != 0) continue;

                            if (shouldSkipDirectory == null || !shouldSkipDirectory(fileName))
                            {
                                directories.Add(fullPath);
                            }
                        }
                        else
                        {
                            var size = ((long)findData.nFileSizeHigh << 32) | findData.nFileSizeLow;
                            var lastWrite = FileTimeToDateTime(findData.ftLastWriteTime);
                            var created = FileTimeToDateTime(findData.ftCreationTime);

                            onFile?.Invoke(fullPath, fileName, size, lastWrite, created);
                        }
                    }
                    while (FindNextFileW(handle, out findData));
                }
            }
            catch { }

            foreach (var dir in directories)
            {
                if (shouldCancel != null && shouldCancel()) return;
                EnumerateRecursiveInternal(dir, onFile, shouldSkipDirectory, shouldCancel, currentDepth + 1, maxDepth);
            }
        }

        private static DateTime FileTimeToDateTime(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            try
            {
                var fileTime = ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
                return DateTime.FromFileTimeUtc(fileTime);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}