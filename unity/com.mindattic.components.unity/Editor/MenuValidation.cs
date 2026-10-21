using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MindAttic.Components.Unity.Editor
{
    /// <summary>
    /// MindAttic > Components > Validate Menus In Open Scenes: runs every <see cref="MenuScreen"/> (active or not)
    /// through the menu rules (a title, a focusable item, labelled controls, no duplicate button labels).
    /// </summary>
    public static class MenuValidation
    {
        /// <summary>Every issue in every menu screen of the loaded scenes, each prefixed with the screen's path.</summary>
        public static List<string> ValidateOpenScenes()
        {
            var issues = new List<string>();
            foreach (var screen in Object.FindObjectsByType<MenuScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (string issue in screen.Validate()) issues.Add(Path(screen.transform) + ": " + issue);
            }
            return issues;
        }

        [MenuItem("MindAttic/Components/Validate Menus In Open Scenes")]
        static void Run()
        {
            var issues = ValidateOpenScenes();
            if (issues.Count == 0) Debug.Log("[MindAttic.Components] Every menu screen passes.");
            foreach (string i in issues) Debug.LogWarning("[MindAttic.Components] " + i);
        }

        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
