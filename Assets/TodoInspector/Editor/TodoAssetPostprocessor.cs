using System.IO;
using UnityEditor;

namespace TodoInspector
{
    internal class TodoAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            TodoScanner scanner = TodoScannerManager.GetScanner();
            if (scanner == null)
                return;

            bool changed = false;

            changed |= RemoveFiles(scanner, deletedAssets);
            changed |= RemoveFiles(scanner, movedFromAssetPaths);
            changed |= ScanFiles(scanner, importedAssets);
            changed |= ScanFiles(scanner, movedAssets);

            if (changed)
                scanner.NotifyScanCompleted();
        }

        private static bool ScanFiles(TodoScanner scanner, string[] assetPaths)
        {
            bool processed = false;

            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (!assetPaths[i].EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string fullPath = Path.GetFullPath(assetPaths[i]).Replace('\\', '/');
                scanner.ScanFileImmediate(fullPath);
                processed = true;
            }

            return processed;
        }

        private static bool RemoveFiles(TodoScanner scanner, string[] assetPaths)
        {
            bool processed = false;

            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (!assetPaths[i].EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string fullPath = Path.GetFullPath(assetPaths[i]).Replace('\\', '/');
                scanner.RemoveFile(fullPath);
                processed = true;
            }

            return processed;
        }
    }
}