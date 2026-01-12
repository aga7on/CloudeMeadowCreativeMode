using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private void DrawOverview()
        {
            GUILayout.Label("Cloud Meadow Creative Mode", GUI.skin.label);
            GUILayout.Space(3);
            GUILayout.Label("Utilities for easier testing and modding. Use tabs to navigate features.");
            GUILayout.Space(8);
            GUILayout.Label("Author: AGA7ON");
        }

        private bool PassesFilter(object obj)
        {
            // Filter hidden; always true
            return true;
        }

        private void DrawPartyUI()
        {
            try
            {
                GUILayout.Label("Party Members");
                GUILayout.BeginVertical(GUI.skin.box);
                int shown = 0;
                for (int i = 0; i < _roots.Count; i++)
                {
                    var obj = _roots[i]; if (obj == null) continue;
                    var tn = obj.GetType().FullName;
                    if (tn != null && tn.IndexOf("Party", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        GUILayout.Label("== " + tn + " ==");
                        int budget = 80;
                        ReflectionUtil.DumpObject(obj, line => GUILayout.Label(line), 2, budget);
                        GUILayout.Space(5);
                        shown++;
                        if (shown > 5) break;
                    }
                }
                GUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Level party to 20")) GameApi.LevelCompanions(20);
                if (GUILayout.Button("Recruit all L15")) GameApi.RecruitAllCompanions(15);
                GUILayout.EndHorizontal();
            }
            catch (System.Exception e)
            {
                GUILayout.Label("Party UI error: " + e.Message);
            }
        }
    }
}
