using System;
using TeamNimbus.CloudMeadow.Story.QuestSystem;
using TeamNimbus.CloudMeadow.Managers;

namespace CloudMeadow.CreativeMode
{
    internal static class GameApiQuest
    {
        public static TeamNimbus.CloudMeadow.Persistence.GameStatus GS { get { return TeamNimbus.CloudMeadow.Managers.GameManager.Status; } }

        public static System.Collections.Generic.List<QuestData> GetActiveQuestLog()
        {
            try { return GS.QuestsDataLog; } catch { return new System.Collections.Generic.List<QuestData>(); }
        }

        public static string[] DebugDumpActiveQuestLog()
        {
            var lines = new System.Collections.Generic.List<string>();
            try
            {
                var list = GetActiveQuestLog();
                lines.Add("QuestsDataLog count=" + (list != null ? list.Count.ToString() : "null"));
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var qd = list[i];
                        if (qd == null) { lines.Add(i + ": (null)"); continue; }
                        string id = qd.id.ToString();
                        string status = "";
                        try { status = ((QuestStatus)qd.status).ToString(); } catch { status = qd.status.ToString(); }
                        int steps = 0; try { steps = qd.QuestStepDataStorage != null ? qd.QuestStepDataStorage.Count : 0; } catch { }
                        lines.Add(string.Format("{0}: id={1} status={2} steps={3}", i, id, status, steps));
                    }
                }
            }
            catch (Exception e)
            {
                lines.Add("DebugDump error: " + e.Message);
            }
            return lines.ToArray();
        }

        public static QuestInfo ResolveQuestInfoByNameOrId(string name, TeamNimbus.Common.Utility.SerializableGuid? id = null)
        {
            try
            {
                var all = GetAllQuests();
                // Try by id
                if (id.HasValue)
                {
                    for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].QuestID.Equals(id.Value)) return all[i];
                }
                // Fallback by name
                for (int i = 0; i < all.Length; i++) if (all[i] != null && string.Equals(all[i].Name, name, StringComparison.OrdinalIgnoreCase)) return all[i];
            }
            catch { }
            return null;
        }
        public static QuestManager QM { get { return QuestManager.Instance; } }
        
        // Accessors
        public static QuestInfo[] GetAllQuests()
        {
            // Returns union of: QuestDatabase quest infos, any serialized fields (lists/singletons), and any SOs loaded.

            try
            {
                var qm = QM; if (qm == null) return new QuestInfo[0];
                var list = new System.Collections.Generic.List<QuestInfo>();
                var t = typeof(QuestManager);
                // Collect lists by acts
                string[] listFields = { "act1Quests", "act2Quests" };
                for (int i = 0; i < listFields.Length; i++)
                {
                    var f = t.GetField(listFields[i], System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var val = f != null ? f.GetValue(qm) as System.Collections.IEnumerable : null;
                    if (val != null) foreach (object o in val) { var qi = o as QuestInfo; if (qi != null) list.Add(qi); }
                }
                // Collect singleton quest references if present
                string[] singleFields = { "summerEventQuest", "f6CampQuest", "pendingGameUpdateQuest" };
                for (int i = 0; i < singleFields.Length; i++)
                {
                    var f = t.GetField(singleFields[i], System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var val = f != null ? f.GetValue(qm) as QuestInfo : null; if (val != null) list.Add(val);
                }
                // Add anything in the current database
                foreach (var kv in qm.QuestDatabase) if (kv.Value != null && kv.Value.info != null) list.Add(kv.Value.info);
                // Explicitly load all QuestInfo from Resources/Quests (canonical DB source)
                try
                {
                    var res = UnityEngine.Resources.LoadAll<QuestInfo>("Quests");
                    if (res != null) { for (int i = 0; i < res.Length; i++) if (res[i] != null) list.Add(res[i]); }
                }
                catch { }
                // Also include any QuestInfo already loaded in memory
                try
                {
                    var all = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(QuestInfo)) as QuestInfo[];
                    if (all != null) { for (int i = 0; i < all.Length; i++) if (all[i] != null) list.Add(all[i]); }
                }
                catch { }
                // De-dup by QuestID
                var uniq = new System.Collections.Generic.Dictionary<string, QuestInfo>();
                for (int i = 0; i < list.Count; i++) { var qi = list[i]; if (qi == null) continue; var key = qi.QuestID.ToString(); if (!uniq.ContainsKey(key)) uniq[key] = qi; }
                var arr = new QuestInfo[uniq.Values.Count]; uniq.Values.CopyTo(arr, 0); return arr;
            }
            catch { return new QuestInfo[0]; }
        }

        public static bool IsQuestCompleted(QuestInfo qi) { try { return QM.IsQuestCompleted(qi.QuestID); } catch { return false; } }
        public static bool IsQuestActive(QuestInfo qi) { try { return QM.IsQuestActive(qi.QuestID); } catch { return false; } }

        public static QuestStepInfo[] GetQuestSteps(QuestInfo qi)
        {
            try { return qi.Steps != null ? qi.Steps.ToArray() : new QuestStepInfo[0]; } catch { return new QuestStepInfo[0]; }
        }

        // Build dependency plan (topologically) for Safe Jump to target step
        public static QuestStepInfo[] PlanSafeJump(QuestInfo quest, QuestStepInfo target)
        {
            var plan = new System.Collections.Generic.List<QuestStepInfo>();
            var visited = new System.Collections.Generic.HashSet<QuestStepInfo>();
            BuildPlanDFS(quest, target, plan, visited);
            return plan.ToArray();
        }
        private static void BuildPlanDFS(QuestInfo quest, QuestStepInfo step, System.Collections.Generic.List<QuestStepInfo> plan, System.Collections.Generic.HashSet<QuestStepInfo> visited)
        {
            if (step == null || visited.Contains(step)) return;
            visited.Add(step);
            var reqs = step.RequiredSteps;
            if (reqs != null) for (int i = 0; i < reqs.Length; i++) BuildPlanDFS(quest, reqs[i], plan, visited);
            plan.Add(step);
        }
        // Safe Jump To: стартует все требуемые шаги (grant rewards), затем целевой шаг
        public static void SafeJumpTo(QuestInfo quest, QuestStepInfo targetStep = null)
        {
            try
            {
                var qm = QuestManager.Instance; if (qm == null || quest == null) return;
                // Ensure quest exists
                if (!qm.IsQuestActiveOrCompleted(quest.QuestID))
                {
                    qm.StartQuest(quest);
                }
                if (targetStep == null)
                {
                    // Jump to quest start: активируем все автозапускаемые шаги первого уровня
                    foreach (var step in quest.Steps)
                    {
                        if (step.RequiredSteps.Length == 0 && step.StepTrigger == QuestStepTrigger.Automatic)
                        {
                            qm.StartQuestStep(step, quest, skipSteps: false, grantRewardsForSkippedSteps: true);
                        }
                    }
                    LogBuffer.Add("Safe Jump: Started quest '" + quest.Name + "'");
                    return;
                }
                // Resolve and complete required chain
                ResolveAndCompleteRequirements(qm, quest, targetStep);
                // Start target if manual
                if (targetStep.StepTrigger == QuestStepTrigger.Manual && !qm.IsQuestStepActive(targetStep, quest))
                {
                    qm.StartQuestStep(targetStep, quest, skipSteps: false, grantRewardsForSkippedSteps: true);
                }
                LogBuffer.Add("Safe Jump: Reached step '" + targetStep.Description + "'");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SafeJumpTo failed: " + e.Message); }
        }

        private static void ResolveAndCompleteRequirements(QuestManager qm, QuestInfo quest, QuestStepInfo step)
        {
            try
            {
                var reqs = step.RequiredSteps;
                if (reqs != null)
                {
                    for (int i = 0; i < reqs.Length; i++)
                    {
                        var r = reqs[i]; if (r == null) continue;
                        ResolveAndCompleteRequirements(qm, quest, r);
                        if (!qm.IsQuestStepCompleted(r, quest))
                        {
                            qm.CompleteQuestStep(r, quest, skipSteps: false, grantStepRewards: true);
                        }
                    }
                }
            }
            catch { }
        }

        // Experimental: насильная установка прогресса квеста/шага
        public static void SetQuestStage(QuestInfo quest, QuestStepInfo step = null)
        {
            try
            {
                var qm = QuestManager.Instance; if (qm == null || quest == null) return;
                if (step == null)
                {
                    // Завершить всё
                    foreach (var s in quest.Steps)
                    {
                        TryComplete(qm, quest, s);
                    }
                    qm.CompleteQuest(quest);
                    LogBuffer.Add("Quest forced complete: " + quest.Name);
                }
                else
                {
                    // Насильно проставить шаг как завершённый, предварительно закрыв зависимости
                    ResolveAndCompleteRequirements(qm, quest, step);
                    TryComplete(qm, quest, step);
                    LogBuffer.Add("Step forced complete: " + step.Description);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetQuestStage failed: " + e.Message); }
        }

        private static void TryComplete(QuestManager qm, QuestInfo quest, QuestStepInfo step)
        {
            try
            {
                if (!qm.IsQuestActiveOrCompleted(quest.QuestID)) qm.StartQuest(quest);
                if (!qm.IsQuestStepCompleted(step, quest))
                {
                    // Ensure active
                    if (!qm.IsQuestStepActive(step, quest))
                    {
                        qm.StartQuestStep(step, quest, skipSteps: false, grantRewardsForSkippedSteps: true);
                    }
                    // Теперь завершить
                    qm.CompleteQuestStep(step, quest, skipSteps: false, grantStepRewards: true);
                }
            }
            catch { }
        }
    }
}
