#if UNITY_EDITOR_LINUX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using VRC.SDK3.Editor.Builder;
using UnityEditor;


namespace VRCSDKonLinux
{
    [InitializeOnLoad]
    public static class SdkPatchBase
    {
        private static Harmony _harmony;

        static SdkPatchBase()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony("pl.barkk.vrcsdk_linux");
            }

            _harmony.UnpatchAll();
            _harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(VRCWorldAssetExporter))]
    [HarmonyPatch("ExportCurrentSceneResource")]
    [HarmonyPatch(new Type[] { typeof(bool), typeof(Action<string>), typeof(Action<object>) })]
    class ExportCurrentSceneResourcePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr)
                {
                    if (codes[i].operand.ToString().Contains(".vrcw"))
                    {
                        Debug.LogWarning("FOUND i = " + i);
                        i += 3;
                        var str2 = codes[i].operand;
                        i++;
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_S, str2));
                        codes.Insert(i++, CodeInstruction.Call(typeof(String), "ToLower"));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Stloc_S, str2));
                        break;
                    }
                }
            }

            return codes.AsEnumerable();
        }
    }

    [InitializeOnLoad]
    public static class ControlPanelPatcher
    {
        public static string _steamPath;

        static ControlPanelPatcher()
        {
            _steamPath = "";
            if (EditorPrefs.HasKey("VRC_steamappsPath"))
                _steamPath = EditorPrefs.GetString("VRC_steamappsPath");
        }
    }


    [HarmonyPatch(typeof(VRCSdkControlPanel))]
    [HarmonyPatch("OnVRCInstallPathGUI")]
    class ControlPanelPatch
    {
        static void Prefix()
        {
            EditorGUILayout.LabelField("Steamapps directory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Steamapps directory containing VRChat");
            EditorGUILayout.LabelField("Locaton: ", ControlPanelPatcher._steamPath);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("");
            if (GUILayout.Button("Edit"))
            {
                string initPath = "";
                if (!string.IsNullOrEmpty(ControlPanelPatcher._steamPath))
                    initPath = ControlPanelPatcher._steamPath;

                ControlPanelPatcher._steamPath = EditorUtility.OpenFolderPanel("Choose steamapps directory", initPath, "");

                EditorPrefs.SetString("VRC_steamappsPath", ControlPanelPatcher._steamPath);
                // window.OnConfigurationChanged();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif