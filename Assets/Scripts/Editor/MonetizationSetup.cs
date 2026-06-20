using UnityEditor;
using UnityEngine;

namespace TimeAura.Editor
{
    [InitializeOnLoad]
    public class MonetizationSetup
    {
        static MonetizationSetup()
        {
            string define = "MONETIZATION_APPLOVIN";
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            
            if (!defines.Contains(define))
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines + ";" + define);
                Debug.Log($"[MonetizationSetup] Added {define} to Scripting Define Symbols for {targetGroup}");
            }
        }
    }
}
