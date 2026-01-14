using System;
using System.Collections.Generic;
using UnityEngine;
using TeamNimbus.CloudMeadow.Story.QuestSystem;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private Vector2 _questsScroll;
        private Dictionary<string, bool> _questFold = new Dictionary<string, bool>();
        private Dictionary<string, bool> _stepFold = new Dictionary<string, bool>();
        private bool _questEventsHooked;
        private bool _questsDirty;
        private bool _showActiveOnly = true;

        private void EnsureQuestEventsHooked()
        {
            if (_questEventsHooked) return;
            try
            {
                var qm = QuestManager.Instance; if (qm == null) return;
                qm.OnQuestUpdated.RegisterHandler(new Func<TeamNimbus.CloudMeadow.Story.QuestSystem.Quest, bool>(OnQuestChanged));
                qm.OnQuestStepStarted.RegisterHandler(new Func<TeamNimbus.Common.Utility.SerializableGuid, TeamNimbus.CloudMeadow.Story.QuestSystem.QuestStep, bool>(OnQuestStepStarted));
                qm.OnQuestStepComplete.RegisterHandler(new Func<TeamNimbus.Common.Utility.SerializableGuid, TeamNimbus.Common.Utility.SerializableGuid, bool>(OnQuestStepCompleted));
                qm.OnQuestComplete.RegisterHandler(new Func<TeamNimbus.Common.Utility.SerializableGuid, bool>(OnQuestCompleted));
                _questEventsHooked = true;
            }
            catch { }
        }
        private bool OnQuestChanged(TeamNimbus.CloudMeadow.Story.QuestSystem.Quest q) { _questsDirty = true; return true; }
        private bool OnQuestStepStarted(TeamNimbus.Common.Utility.SerializableGuid qid, TeamNimbus.CloudMeadow.Story.QuestSystem.QuestStep s) { _questsDirty = true; return true; }
        private bool OnQuestStepCompleted(TeamNimbus.Common.Utility.SerializableGuid qid, TeamNimbus.Common.Utility.SerializableGuid sid) { _questsDirty = true; return true; }
        private bool OnQuestCompleted(TeamNimbus.Common.Utility.SerializableGuid qid) { _questsDirty = true; return true; }

        private string _planPopupText;
        private bool _showPlan;
        private Vector2 _planScroll;

        private void ShowPlanPopup(QuestStepInfo[] plan)
        {
            _planPopupText = "Plan to reach step:\n";
            if (plan != null)
            {
                for (int i = 0; i < plan.Length; i++)
                {
                    var s = plan[i]; if (s == null) continue;
                    _planPopupText += string.Format("{0}. {1}\n", (i + 1), s.Description);
                }
            }
            _showPlan = true;
        }

        private void DrawQuestsUI()
        {
            try
            {
                EnsureQuestEventsHooked();
                var qm = QuestManager.Instance;
                if (qm == null)
                {
                    GUILayout.Label("QuestManager not available — showing QuestsDataLog only");
                    // Minimal fallback: render QuestsDataLog from GameStatus even without QuestManager
                    var activeOnly = _showActiveOnly; // same toggle
                    var list = GameApiQuest.GetActiveQuestLog();
                    _questsScroll = GUILayout.BeginScrollView(_questsScroll);
                    for (int ai = 0; ai < list.Count; ai++)
                    {
                        var qd = list[ai]; if (qd == null) continue;
                        string id = qd.id.ToString();
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        bool open = _questFold.ContainsKey(id) ? _questFold[id] : false;
                        bool newOpen = GUILayout.Toggle(open, "", GUILayout.Width(18));
                        if (newOpen != open) _questFold[id] = newOpen;
                        GUILayout.Label("Active (unknown): " + id + " (" + qd.status + ")", GUILayout.Width(360));
                        GUI.enabled = false;
                        GUILayout.Button("Safe Jump To", GUILayout.Width(120));
                        GUILayout.Button("Set this stage", GUILayout.Width(120));
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                    return;
                }
                if (_questsDirty) { _questsDirty = false; }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quests (tree)");
                if (GUILayout.Button("Dump Active", GUILayout.Width(110)))
                {
                    var dump = GameApiQuest.DebugDumpActiveQuestLog();
                    for (int di = 0; di < dump.Length; di++) LogBuffer.Add("Q: " + dump[di]);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                _questsScroll = GUILayout.BeginScrollView(_questsScroll);
                // Controls: Refresh button
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh", GUILayout.Width(90))) { _questsDirty = true; }
                if (GUILayout.Button("Show Active", GUILayout.Width(110))) { _showActiveOnly = true; _questsDirty = true; }
                if (GUILayout.Button("Show All", GUILayout.Width(90))) { _showActiveOnly = false; _questsDirty = true; }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                var quests = GameApiQuest.GetAllQuests();

                // Build Active overlay from QuestsDataLog
                var active = GameApiQuest.GetActiveQuestLog();

                if (_showActiveOnly)
                {
                    // Drive UI strictly from player's QuestsDataLog
                    for (int ai = 0; ai < active.Count; ai++)
                    {
                        var qd = active[ai]; if (qd == null) continue;
                        string id = qd.id.ToString();

                        QuestInfo info = null;
                        TeamNimbus.CloudMeadow.Story.QuestSystem.Quest questObj = null;
                        try
                        {
                            // Try resolve via QuestManager database first (even if not in our aggregated list)
                            if (qm.QuestDatabase.ContainsKey(qd.id))
                            {
                                questObj = qm.QuestDatabase[qd.id];
                                info = questObj != null ? questObj.info : null;
                            }
                        }
                        catch { }
                        if (info == null)
                        {
                            // Fallback to our aggregated search
                            info = GameApiQuest.ResolveQuestInfoByNameOrId(null, qd.id);
                            if (info != null)
                            {
                                try { questObj = qm.GetQuestById(info.QuestID); } catch { questObj = null; }
                            }
                        }

                        if (info == null)
                        {
                            // Unknown quest (no QuestInfo loaded) — render minimal row
                            GUILayout.BeginHorizontal(GUI.skin.box);
                            bool open2 = _questFold.ContainsKey(id) ? _questFold[id] : false;
                            bool newOpen2 = GUILayout.Toggle(open2, "", GUILayout.Width(18));
                            if (newOpen2 != open2) _questFold[id] = newOpen2;
                            GUILayout.Label("Active (unknown): " + id + " (" + qd.status + ")", GUILayout.Width(360));
                            GUI.enabled = false;
                            GUILayout.Button("Set this stage", GUILayout.Width(120));
                            GUI.enabled = true;
                            GUILayout.EndHorizontal();
                            if (_questFold.ContainsKey(id) && _questFold[id])
                            {
                                GUILayout.BeginVertical(GUI.skin.box);
                                GUILayout.Label("No QuestInfo loaded. This quest may be in AssetBundles or not loaded yet.");
                                GUILayout.EndVertical();
                            }
                            continue;
                        }

                        // Known quest: render full UI
                        string qkey = info.QuestID.ToString();
                        bool open = _questFold.ContainsKey(qkey) ? _questFold[qkey] : false;
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        bool newOpen = GUILayout.Toggle(open, "", GUILayout.Width(18));
                        if (newOpen != open) _questFold[qkey] = newOpen;
                        var statusLabel = (questObj != null ? questObj.status.ToString() : qd.status.ToString());
                        GUILayout.Label(info.Name + " (" + statusLabel + ")", GUILayout.Width(360));
                        if (GUILayout.Button("Set this stage", GUILayout.Width(120))) { GameApiQuest.SetQuestStage(info); }
                        GUILayout.EndHorizontal();

                        if (_questFold.ContainsKey(qkey) && _questFold[qkey])
                        {
                            var steps = info.Steps;
                            for (int i = 0; i < steps.Count; i++)
                            {
                                var step = steps[i]; if (step == null) continue;
                                string skey = qkey + ":" + step.QuestStepID.ToString();
                                bool sOpen = _stepFold.ContainsKey(skey) ? _stepFold[skey] : false;
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                bool sNewOpen = GUILayout.Toggle(sOpen, "", GUILayout.Width(18));
                                if (sNewOpen != sOpen) _stepFold[skey] = sNewOpen;
                                GUILayout.Label("- " + step.Description + " [" + step.StepType + "]", GUILayout.Width(540));
                                if (GUILayout.Button("Plan", GUILayout.Width(60))) { var plan = GameApiQuest.PlanSafeJump(info, step); ShowPlanPopup(plan); }
                                if (GUILayout.Button("Safe Jump To", GUILayout.Width(100))) { GameApiQuest.SafeJumpTo(info, step); }
                                if (GUILayout.Button("Set this stage", GUILayout.Width(120))) { GameApiQuest.SetQuestStage(info, step); }
                                GUILayout.EndHorizontal();
                                if (_stepFold.ContainsKey(skey) && _stepFold[skey])
                                {
                                    GUILayout.BeginVertical(GUI.skin.box);
                                    GUILayout.Label("Trigger: " + step.StepTrigger);
                                    GUILayout.Label("Required: " + (step.RequiredSteps != null ? step.RequiredSteps.Length.ToString() : "0"));
                                    GUILayout.Label("CompletionValue: " + step.CompletionValue);
                                    GUILayout.EndVertical();
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Show All: old behavior, order by priority, with no Active filter
                    Array.Sort(quests, (a,b)=> a.Priority.CompareTo(b.Priority));
                    for (int qi = 0; qi < quests.Length; qi++)
                    {
                        var info = quests[qi]; if (info == null) continue;
                        var quest = qm.GetQuestById(info.QuestID);
                        string qkey = info.QuestID.ToString();
                        bool open = _questFold.ContainsKey(qkey) ? _questFold[qkey] : false;
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        bool newOpen = GUILayout.Toggle(open, "", GUILayout.Width(18));
                        if (newOpen != open) _questFold[qkey] = newOpen;
                        GUILayout.Label(info.Name + " (" + quest.status + ")", GUILayout.Width(360));
                        if (GUILayout.Button("Set this stage", GUILayout.Width(120))) { GameApiQuest.SetQuestStage(info); }
                        GUILayout.EndHorizontal();

                        if (_questFold.ContainsKey(qkey) && _questFold[qkey])
                        {
                            var steps = info.Steps;
                            for (int i = 0; i < steps.Count; i++)
                            {
                                var step = steps[i]; if (step == null) continue;
                                string skey = qkey + ":" + step.QuestStepID.ToString();
                                bool sOpen = _stepFold.ContainsKey(skey) ? _stepFold[skey] : false;
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                bool sNewOpen = GUILayout.Toggle(sOpen, "", GUILayout.Width(18));
                                if (sNewOpen != sOpen) _stepFold[skey] = sNewOpen;
                                GUILayout.Label("- " + step.Description + " [" + step.StepType + "]", GUILayout.Width(540));
                                if (GUILayout.Button("Plan", GUILayout.Width(60))) { var plan = GameApiQuest.PlanSafeJump(info, step); ShowPlanPopup(plan); }
                                if (GUILayout.Button("Safe Jump To", GUILayout.Width(100))) { GameApiQuest.SafeJumpTo(info, step); }
                                if (GUILayout.Button("Set this stage", GUILayout.Width(120))) { GameApiQuest.SetQuestStage(info, step); }
                                GUILayout.EndHorizontal();
                                if (_stepFold.ContainsKey(skey) && _stepFold[skey])
                                {
                                    GUILayout.BeginVertical(GUI.skin.box);
                                    GUILayout.Label("Trigger: " + step.StepTrigger);
                                    GUILayout.Label("Required: " + (step.RequiredSteps != null ? step.RequiredSteps.Length.ToString() : "0"));
                                    GUILayout.Label("CompletionValue: " + step.CompletionValue);
                                    GUILayout.EndVertical();
                                }
                            }
                        }
                    }
                }

                // Render Active (unknown) quests that are in the player's log but have no loaded QuestInfo
                if (_showActiveOnly)
                {
                    var known = new System.Collections.Generic.HashSet<string>();
                    for (int i2 = 0; i2 < quests.Length; i2++) { var qi2 = quests[i2]; if (qi2 != null) known.Add(qi2.QuestID.ToString()); }
                    for (int ai2 = 0; ai2 < active.Count; ai2++)
                    {
                        var qd2 = active[ai2]; if (qd2 == null) continue;
                        string id2 = qd2.id.ToString();
                        if (known.Contains(id2)) continue; // already displayed above

                        GUILayout.BeginHorizontal(GUI.skin.box);
                        bool open2 = _questFold.ContainsKey(id2) ? _questFold[id2] : false;
                        bool newOpen2 = GUILayout.Toggle(open2, "", GUILayout.Width(18));
                        if (newOpen2 != open2) _questFold[id2] = newOpen2;
                        GUILayout.Label("Active (unknown): " + id2 + " (" + qd2.status + ")", GUILayout.Width(360));
                        GUI.enabled = false;
                        GUILayout.Button("Safe Jump To", GUILayout.Width(120));
                        GUILayout.Button("Set this stage", GUILayout.Width(120));
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                        if (_questFold.ContainsKey(id2) && _questFold[id2])
                        {
                            GUILayout.BeginVertical(GUI.skin.box);
                            GUILayout.Label("No QuestInfo loaded. This quest may live in AssetBundles or is not yet loaded into memory.");
                            GUILayout.EndVertical();
                        }
                    }
                }

                GUILayout.EndScrollView();

                if (_showPlan)
                {
                    GUILayout.BeginVertical(GUI.skin.window);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Safe Jump Plan");
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", GUILayout.Width(60))) _showPlan = false;
                    GUILayout.EndHorizontal();
                    _planScroll = GUILayout.BeginScrollView(_planScroll, GUILayout.Height(200));
                    GUILayout.Label(_planPopupText ?? "(empty)");
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
            }
            catch (Exception e)
            {
                GUILayout.Label("Quests UI error: " + e.Message);
            }
        }
    }
}
