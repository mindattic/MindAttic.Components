using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MindAttic.Components.Unity.Editor
{
    /// <summary>
    /// TextMesh Pro needs its Essential Resources (TMP Settings, the SDF shaders, the default font) in the
    /// project before it can draw text. Unity asks for them with a pop-up the first time a TMP object is made in
    /// the editor; this imports them quietly on the first editor load instead, so a game that adds this package
    /// works without that step (also in batch mode). Turn it off with
    /// <c>EditorPrefs.SetBool("MindAttic.Components.AutoImportTmp", false)</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class TmpEssentials
    {
        const string AutoImportPref = "MindAttic.Components.AutoImportTmp";
        static bool s_importing;

        static TmpEssentials()
        {
            if (!EditorPrefs.GetBool(AutoImportPref, true)) return;
            EditorApplication.delayCall += () =>
            {
                if (!s_importing && !Present) Import(log: true);
            };
        }

        /// <summary>True when the project has TMP Settings.</summary>
        public static bool Present => Resources.Load<TMP_Settings>("TMP Settings") != null;

        /// <summary>The Essential Resources package inside com.unity.ugui, or null.</summary>
        public static string PackagePath
        {
            get
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
                string root = info != null ? info.resolvedPath : Path.GetFullPath("Packages/com.unity.ugui");
                string path = Path.Combine(root, "Package Resources", "TMP Essential Resources.unitypackage");
                return File.Exists(path) ? path : null;
            }
        }

        /// <summary>Imports the Essential Resources when missing; returns true when they are present afterwards.</summary>
        [MenuItem("MindAttic/Components/Import TMP Essentials")]
        public static bool ImportIfMissing() => Present || Import(log: true);

        /// <summary>For batch mode: <c>-executeMethod MindAttic.Components.Unity.Editor.TmpEssentials.ImportForBatch</c>.</summary>
        public static void ImportForBatch()
        {
            if (Present)
            {
                Debug.Log("[MindAttic.Components] TMP Essential Resources present");
                EditorApplication.Exit(0);
                return;
            }
            // ImportPackage finishes on a later editor update, so stay open (run without -quit) until it reports.
            AssetDatabase.importPackageCompleted += _ => Finish(true);
            AssetDatabase.importPackageFailed += (_, error) =>
            {
                Debug.LogError("[MindAttic.Components] TMP Essential Resources import failed: " + error);
                Finish(false);
            };
            if (!Import(log: true) && PackagePath == null) EditorApplication.Exit(1);
        }

        static void Finish(bool ok)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[MindAttic.Components] TMP Essential Resources " + (ok && Present ? "imported" : "MISSING"));
            EditorApplication.Exit(ok && Present ? 0 : 1);
        }

        static bool Import(bool log)
        {
            string path = PackagePath;
            if (path == null)
            {
                if (log) Debug.LogWarning("[MindAttic.Components] Could not find TMP Essential Resources.unitypackage in com.unity.ugui.");
                return false;
            }
            if (log) Debug.Log("[MindAttic.Components] Importing TextMesh Pro Essential Resources (needed to draw text).");
            s_importing = true;
            AssetDatabase.ImportPackage(path, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return Present;
        }
    }
}
