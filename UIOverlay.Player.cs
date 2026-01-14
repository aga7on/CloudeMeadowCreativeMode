using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private string _editName = null;
        private string _editLevel = null;
        private System.Collections.Generic.Dictionary<string, string> _editStats = new System.Collections.Generic.Dictionary<string, string>();

        private void DrawPlayerUI()
        {
            try
            {
                var s = TeamNimbus.CloudMeadow.Managers.GameManager.Status;
                var p = s.ProtagonistStats;
                GUILayout.Label("Protagonist");
                GUILayout.BeginVertical(GUI.skin.box);
                // Name
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(60));
                if (_editName == null) _editName = p.Name;
                _editName = GUILayout.TextField(_editName, GUILayout.Width(180));
                if (GUILayout.Button("Set", GUILayout.Width(50))) TrySetMember(p, "Name", _editName);
                GUILayout.EndHorizontal();
                // Level (clamped to GameManager.MaxLevel)
                GUILayout.BeginHorizontal();
                GUILayout.Label("Level:", GUILayout.Width(60));
                if (_editLevel == null) _editLevel = p.Level.ToString();
                _editLevel = GUILayout.TextField(_editLevel, GUILayout.Width(80));
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    int lvl;
                    if (int.TryParse(_editLevel, out lvl))
                    {
                        try
                        {
                            int maxLvl = TeamNimbus.CloudMeadow.Managers.GameManager.MaxLevel;
                            if (lvl < 1) lvl = 1; if (lvl > maxLvl) lvl = maxLvl;
                            TrySetMember(p, "Level", lvl);
                        }
                        catch { TrySetMember(p, "Level", lvl); }
                    }
                }
                GUILayout.Label(p.IsMaxLevel ? "(Max)" : "", GUILayout.Width(60));
                GUILayout.EndHorizontal();
                // Gender
                GUILayout.BeginHorizontal();
                GUILayout.Label("Gender:", GUILayout.Width(60));
                if (GUILayout.Button("Male", GUILayout.Width(60))) TrySetEnum(p, "Gender", "Male");
                if (GUILayout.Button("Female", GUILayout.Width(60))) TrySetEnum(p, "Gender", "Female");
                GUILayout.Label("Current: " + p.Gender, GUILayout.Width(120));
                GUILayout.EndHorizontal();
                // LVL/HP/XP
                GUILayout.BeginHorizontal();
                GUILayout.Label("LVL " + p.Level, GUILayout.Width(70));
                var hpCur = ReadStat(p, new string[] { "HPCurrent", "CurrentHP", "HP" });
                var hpMax = ReadStat(p, new string[] { "HPMax", "MaxHP", "MaxHealth" });
                GUILayout.Label("HP: " + (hpCur != null ? hpCur.ToString() : "-") + "/" + (hpMax != null ? hpMax.ToString() : "-"), GUILayout.Width(180));
                var xp = ReadStat(p, new string[] { "CurrentXP", "XP", "Experience" });
                GUILayout.Label("XP: " + (xp != null ? xp.ToString() : "-"), GUILayout.Width(120));
                GUILayout.Label("XP Next: " + p.XPNeededForNextLevel, GUILayout.Width(120));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.Label("Primary Stats");
                GUILayout.Label("Note: Primary stats are limited by Growth + Max Custom (non-monster cap is 500 custom). Values set here respect those caps.", GUI.skin.box);
                GUILayout.BeginVertical(GUI.skin.box);
                StatRow("Physique", p, "Physique");
                StatRow("Stamina", p, "Stamina");
                StatRow("Intuition", p, "Intuition");
                StatRow("Swiftness", p, "Swiftness");
                GUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Korona: " + s.KoronaBalance, GUILayout.Width(160));
                if (GUILayout.Button("+1000", GUILayout.Width(60))) GameApi.AddKorona(1000);
                if (GUILayout.Button("+100000", GUILayout.Width(80))) GameApi.AddKorona(100000);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Shards: " + s.NumUpgradeShards, GUILayout.Width(160));
                if (GUILayout.Button("+10", GUILayout.Width(60))) GameApi.AddShards(10);
                if (GUILayout.Button("+100", GUILayout.Width(60))) GameApi.AddShards(100);
                GUILayout.EndHorizontal();
            }
            catch (System.Exception e)
            {
                GUILayout.Label("Player UI error: " + e.Message);
            }
        }

    }
}
