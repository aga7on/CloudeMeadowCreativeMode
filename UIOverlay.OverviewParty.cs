using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private void DrawOverview()
        {
            GUILayout.Label("Detected roots: " + _roots.Count);
            if (GameApi.Ready)
            {
                GUILayout.Label(GameApi.BuildQuickStatus());
            }
            for (int i = 0; i < _roots.Count; i++)
            {
                var r = _roots[i];
                if (!PassesFilter(r)) continue;
                GUILayout.Label("- " + r.GetType().FullName);
            }
        }

        private bool PassesFilter(object obj)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            var name = (obj != null && obj.GetType() != null && obj.GetType().FullName != null) ? obj.GetType().FullName : "";
            return name.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
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
