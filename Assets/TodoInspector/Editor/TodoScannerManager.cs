using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TodoInspector
{
    [InitializeOnLoad]
    internal static class TodoScannerManager
    {
        internal static event Action OnTodosUpdated;

        private static readonly TodoScanner Scanner;

        static TodoScannerManager()
        {
            string projectPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            string excludePath = Path.Combine(projectPath, "TodoInspector").Replace('\\', '/');

            Scanner = new TodoScanner(projectPath, excludePath);
            Scanner.OnScanCompleted += HandleScanCompleted;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += Cleanup;

            EditorApplication.delayCall += () => Scanner.StartFullScan();
        }

        internal static TodoScanner GetScanner()
        {
            return Scanner;
        }

        internal static void RequestFullScan()
        {
            Scanner.StartFullScan();
        }

        private static void HandleScanCompleted()
        {
            EditorApplication.delayCall += () => OnTodosUpdated?.Invoke();
        }

        private static void OnBeforeAssemblyReload()
        {
            Scanner.CancelScan();
        }

        private static void Cleanup()
        {
            Scanner.CancelScan();
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }
    }
}