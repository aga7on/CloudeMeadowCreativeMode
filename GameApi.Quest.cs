using System;
using TeamNimbus.CloudMeadow.Story.QuestSystem;
using TeamNimbus.CloudMeadow.Managers;

namespace CloudMeadow.CreativeMode
{
    internal static class GameApiQuest
    {
        public static TeamNimbus.CloudMeadow.Persistence.GameStatus GS { get { return TeamNimbus.CloudMeadow.Managers.GameManager.Status; } }
        private static QuestInfo[] _cachedAllQuests = new QuestInfo[0];
        private static System.Collections.Generic.List<QuestData> _cachedActiveQuestLog = new System.Collections.Generic.List<QuestData>();
        private static long _cachedAllQuestsAtTicks;
        private static long _cachedActiveQuestAtTicks;
        private static bool _allQuestsDirty = true;
        private static bool _activeQuestDirty = true;
        private const long QuestCacheTtlTicks = TimeSpan.TicksPerSecond * 2;

        public static void MarkQuestCacheDirty()
        {
            _allQuestsDirty = true;
            _activeQuestDirty = true;
        }

        public static System.Collections.Generic.List<QuestData> GetActiveQuestLog()
        {
            try
            {
                long now = DateTime.UtcNow.Ticks;
                if (_activeQuestDirty || (now - _cachedActiveQuestAtTicks) > QuestCacheTtlTicks)
                {
                    var src = GS.QuestsDataLog;
                    _cachedActiveQuestLog = src != null ? new System.Collections.Generic.List<QuestData>(src) : new System.Collections.Generic.List<QuestData>();
                    _cachedActiveQuestAtTicks = now;
                    _activeQuestDirty = false;
                }
                return new System.Collections.Generic.List<QuestData>(_cachedActiveQuestLog);
            }
            catch { return new System.Collections.Generic.List<QuestData>(); }
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
                long now = DateTime.UtcNow.Ticks;
                if (!_allQuestsDirty && (now - _cachedAllQuestsAtTicks) <= QuestCacheTtlTicks) return _cachedAllQuests;

                var qm = QM; if (qm == null || qm.QuestDatabase == null) return new QuestInfo[0];
                var list = new System.Collections.Generic.List<QuestInfo>();
                // QuestDatabase is the canonical runtime set created by the game from
                // Resources/Quests. Scanning every QuestManager field used to leak
                // helper/stale ScriptableObjects into the UI and made paging unstable.
                foreach (var kv in qm.QuestDatabase) if (kv.Value != null && kv.Value.info != null) list.Add(kv.Value.info);
                // Include newly loaded resources only if the runtime database missed an
                // ID (useful after asset-bundle additions), never replace canonical data.
                try
                {
                    var res = UnityEngine.Resources.LoadAll<QuestInfo>("Quests");
                    if (res != null) { for (int i = 0; i < res.Length; i++) if (res[i] != null) list.Add(res[i]); }
                }
                catch { }
                // De-dup by QuestID
                var uniq = new System.Collections.Generic.Dictionary<string, QuestInfo>();
                for (int i = 0; i < list.Count; i++) { var qi = list[i]; if (qi == null) continue; var key = qi.QuestID.ToString(); if (!uniq.ContainsKey(key)) uniq[key] = qi; }
                var ordered = new System.Collections.Generic.List<QuestInfo>(uniq.Values);
                ordered.Sort(delegate(QuestInfo a, QuestInfo b) {
                    int sa = QuestStatusSort(a), sb = QuestStatusSort(b); if (sa != sb) return sa.CompareTo(sb);
                    int byName = string.Compare(a != null ? a.Name : string.Empty, b != null ? b.Name : string.Empty, StringComparison.OrdinalIgnoreCase);
                    if (byName != 0) return byName;
                    return string.Compare(GetQuestId(a), GetQuestId(b), StringComparison.Ordinal);
                });
                var arr = ordered.ToArray();
                _cachedAllQuests = arr;
                _cachedAllQuestsAtTicks = now;
                _allQuestsDirty = false;
                return arr;
            }
            catch { return new QuestInfo[0]; }
        }

        public static bool IsQuestCompleted(QuestInfo qi) { try { return QM.IsQuestCompleted(qi.QuestID); } catch { return false; } }
        public static bool IsQuestActive(QuestInfo qi) { try { return QM.IsQuestActive(qi.QuestID); } catch { return false; } }
        private static int QuestStatusSort(QuestInfo qi) { string s = GetQuestStatus(qi); return s == "Active" ? 0 : (s == "Inactive" ? 1 : 2); }
        public static string GetQuestStatus(QuestInfo qi)
        {
            try { if (qi == null || QM == null) return "Missing"; var q = QM.GetQuestById(qi.QuestID); return q != null ? q.status.ToString() : "Missing"; }
            catch { return "Missing"; }
        }
        public static string GetQuestId(QuestInfo qi) { try { return qi != null ? qi.QuestID.ToString() : "(null)"; } catch { return "(invalid)"; } }
        public static string GetQuestSearchText(QuestInfo qi)
        {
            if (qi == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            try { sb.Append(qi.Name).Append(' ').Append(qi.QuestID).Append(' '); } catch { }
            try
            {
                if (qi.Steps != null) foreach (var step in qi.Steps)
                {
                    if (step == null) continue;
                    sb.Append(step.Description).Append(' ').Append(step.QuestStepID).Append(' ').Append(step.StepTrigger).Append(' ');
                }
            }
            catch { }
            return sb.ToString();
        }
        public static string GetQuestRuntimeSummary(QuestInfo qi)
        {
            try
            {
                var q = QM.GetQuestById(qi.QuestID); if (q == null) return "Runtime Quest: missing";
                int active = 0, completed = 0; foreach (var s in qi.Steps) { if (QM.IsQuestStepActive(s, qi)) active++; if (QM.IsQuestStepCompleted(s, qi)) completed++; }
                return "Runtime: OK | Status: " + q.status + " | Active steps: " + active + " | Completed: " + completed + "/" + qi.Steps.Count;
            }
            catch (Exception e) { return "Runtime: ERROR — " + e.Message; }
        }
        public static bool HasFullRestartProfile(QuestInfo qi)
        {
            if (qi == null || string.IsNullOrEmpty(qi.Name)) return false; string n = qi.Name.ToLowerInvariant();
            string[] profiles = { "holstaur", "sunglade", "creamery", "vaskin", "forest", "echo stone", "tro", "savannah", "lucia", "construction", "phantasmus", "kidnap", "rook" };
            for (int i = 0; i < profiles.Length; i++) if (n.IndexOf(profiles[i]) >= 0) return true; return false;
        }

        public static string GetQuestStepInspectorSummary(QuestInfo quest, QuestStepInfo step)
        {
            if (quest == null || step == null) return "Step unavailable";
            try
            {
                bool active = QM.IsQuestStepActive(step, quest), complete = QM.IsQuestStepCompleted(step, quest);
                int required = step.RequiredSteps != null ? step.RequiredSteps.Length : 0;
                string item = step.ItemToCollect != null ? step.ItemToCollect.ToString() : "(none)";
                return "ID: " + step.QuestStepID + " | Active: " + active + " | Completed: " + complete + " | Completion: " + step.CompletionValue + " | Requires: " + required + " | Item: " + item;
            }
            catch (Exception e) { return "Step runtime unavailable: " + e.Message; }
        }

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
                            qm.StartQuestStep(step, quest, skipSteps: true, grantRewardsForSkippedSteps: true);
                        }
                    }
                    MarkQuestCacheDirty();
                    LogBuffer.Add("Safe Jump: Started quest '" + quest.Name + "'");
                    return;
                }
                // Resolve and complete required chain
                ResolveAndCompleteRequirements(qm, quest, targetStep);
                // Explicitly activate the target after its prerequisites. Depending on
                // scene listeners alone left automatic targets inactive when repairing
                // a save outside their original scene.
                if (!qm.IsQuestStepCompleted(targetStep, quest) && !qm.IsQuestStepActive(targetStep, quest))
                {
                    qm.StartQuestStep(targetStep, quest, skipSteps: false, grantRewardsForSkippedSteps: true);
                }
                LogBuffer.Add("Safe Jump: Reached step '" + targetStep.Description + "'");
                MarkQuestCacheDirty();
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
                            qm.CompleteQuestStep(r, quest, skipSteps: true, grantStepRewards: true);
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
                if (qm.IsQuestCompleted(quest.QuestID))
                {
                    LogBuffer.Add("Quest completion skipped (already completed): " + quest.Name);
                    return;
                }
                if (step == null)
                {
                    // Завершить всё
                    foreach (var s in quest.Steps)
                    {
                        TryComplete(qm, quest, s);
                    }
                    qm.CompleteQuest(quest);
                    MarkQuestCacheDirty();
                    LogBuffer.Add("Quest forced complete: " + quest.Name);
                }
                else
                {
                    // Насильно проставить шаг как завершённый, предварительно закрыв зависимости
                    ResolveAndCompleteRequirements(qm, quest, step);
                    TryComplete(qm, quest, step);
                    MarkQuestCacheDirty();
                    LogBuffer.Add("Step forced complete: " + step.Description);
                }
                MarkQuestCacheDirty();
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
                        qm.StartQuestStep(step, quest, skipSteps: true, grantRewardsForSkippedSteps: true);
                    }
                    // Теперь завершить
                    qm.CompleteQuestStep(step, quest, skipSteps: false, grantStepRewards: true);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("Quest step completion failed: " + (step != null ? step.Description : "(null)") + " — " + e.Message); }
        }

        public static void RestartQuest(QuestInfo info)
        {
            try
            {
                if (info == null || QM == null) return;
                QM.ClearQuest(info);
                QM.StartQuest(info); MarkQuestCacheDirty(); LogBuffer.Add("Quest restarted: " + info.Name);
            }
            catch (Exception e) { Plugin.Log.LogWarning("RestartQuest failed: " + e.Message); }
        }

        public static void SoftRestartStep(QuestInfo info, QuestStepInfo step)
        {
            try
            {
                if (info == null || step == null || QM == null) return;
                Quest runtime = QM.GetQuestById(info.QuestID); if (runtime == null) return;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var activeField = runtime.GetType().GetField("_activeSteps", flags);
                var storageField = runtime.GetType().GetField("_questStepDataStorage", flags);
                var active = activeField != null ? activeField.GetValue(runtime) as System.Collections.IDictionary : null;
                var storage = storageField != null ? storageField.GetValue(runtime) as System.Collections.IList : null;
                if (active != null && active.Contains(step.QuestStepID)) active.Remove(step.QuestStepID);
                if (storage != null) for (int i = storage.Count - 1; i >= 0; i--) { var d = storage[i] as QuestStepData; if (d != null && d.id.Equals(step.QuestStepID)) storage.RemoveAt(i); }
                QM.StartQuestStep(step, info, false, false); QM.UpdateQuestData(info.QuestID); MarkQuestCacheDirty();
                LogBuffer.Add("Quest step soft restarted: " + step.Description);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SoftRestartStep failed: " + e.Message); }
        }

        public static void RepairQuestLog()
        {
            try
            {
                // Preserve the most advanced data instead of arbitrarily dropping one
                // side of a duplicate. Duplicate step records are merged by step ID.
                var retained = new System.Collections.Generic.Dictionary<string, QuestData>();
                var retainedOrder = new System.Collections.Generic.List<QuestData>();
                for (int i = 0; i < GS.QuestsDataLog.Count; i++)
                {
                    var data = GS.QuestsDataLog[i]; if (data == null) continue;
                    string key = data.id.ToString(); QuestData current;
                    if (!retained.TryGetValue(key, out current)) { retained[key] = data; retainedOrder.Add(data); }
                    else MergeQuestData(current, data);
                }
                GS.QuestsDataLog.Clear();
                for (int i = 0; i < retainedOrder.Count; i++) GS.QuestsDataLog.Add(retainedOrder[i]);
                for (int i = 0; i < retainedOrder.Count; i++) NormalizeQuestStepStorage(retainedOrder[i]);
                int synced = ResyncQuestRuntimeAfterRepair();
                MarkQuestCacheDirty(); LogBuffer.Add("Quest log safely merged; entries=" + GS.QuestsDataLog.Count + ", runtime synced=" + synced);
            }
            catch (Exception e) { Plugin.Log.LogWarning("RepairQuestLog failed: " + e.Message); }
        }

        private static void MergeQuestData(QuestData target, QuestData source)
        {
            if (target == null || source == null) return;
            if (target.QuestStepDataStorage == null) target.QuestStepDataStorage = new System.Collections.Generic.List<QuestStepData>();
            if (source.QuestStepDataStorage != null)
            {
                var known = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < target.QuestStepDataStorage.Count; i++) if (target.QuestStepDataStorage[i] != null) known.Add(target.QuestStepDataStorage[i].id.ToString());
                for (int i = 0; i < source.QuestStepDataStorage.Count; i++)
                {
                    var step = source.QuestStepDataStorage[i];
                    if (step != null && known.Add(step.id.ToString())) target.QuestStepDataStorage.Add(step);
                }
            }
            // Completed is terminal; otherwise Active is more informative than Failed/Inactive.
            if (source.status == QuestStatus.Completed || (target.status != QuestStatus.Completed && source.status == QuestStatus.Active)) target.status = source.status;
        }

        public static int ResyncQuestRuntimeFromSaveLog()
        {
            int synced = ResyncQuestRuntimeAfterRepair();
            MarkQuestCacheDirty();
            return synced;
        }

        private static int ResyncQuestRuntimeAfterRepair()
        {
            int synced = 0;
            try
            {
                var qm = QM; if (qm == null || qm.QuestDatabase == null) return 0;
                var load = qm.GetType().GetMethod("LoadQuestData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (load == null) return 0;
                for (int i = 0; i < GS.QuestsDataLog.Count; i++)
                {
                    var data = GS.QuestsDataLog[i]; if (data == null || !qm.QuestDatabase.ContainsKey(data.id)) continue;
                    try
                    {
                        var runtime = qm.GetQuestById(data.id);
                        if (runtime != null) runtime.ClearQuest();
                        load.Invoke(qm, new object[] { data });
                        runtime = qm.GetQuestById(data.id);
                        if (runtime != null) qm.OnQuestUpdated.Trigger(runtime);
                        synced++;
                    }
                    catch (Exception e) { Plugin.Log.LogWarning("Quest runtime resync skipped " + data.id + ": " + e.Message); }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("Quest runtime resync failed: " + e.Message); }
            return synced;
        }

        private static void NormalizeQuestStepStorage(QuestData data)
        {
            if (data == null) return;
            if (data.QuestStepDataStorage == null) data.QuestStepDataStorage = new System.Collections.Generic.List<QuestStepData>();
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = data.QuestStepDataStorage.Count - 1; i >= 0; i--)
            {
                var step = data.QuestStepDataStorage[i];
                if (step == null || !seen.Add(step.id.ToString())) data.QuestStepDataStorage.RemoveAt(i);
            }
        }

        public static string[] AuditQuestIntegrity()
        {
            var lines = new System.Collections.Generic.List<string>();
            try
            {
                var known = new System.Collections.Generic.HashSet<string>(); foreach (var q in GetAllQuests()) known.Add(q.QuestID.ToString());
                var seen = new System.Collections.Generic.HashSet<string>(); int nulls = 0, duplicates = 0, unknown = 0, badSteps = 0;
                var log = GS.QuestsDataLog;
                for (int i = 0; i < log.Count; i++)
                {
                    var data = log[i]; if (data == null) { nulls++; continue; }
                    string id = data.id.ToString(); if (!seen.Add(id)) duplicates++; if (!known.Contains(id)) unknown++;
                    if (data.QuestStepDataStorage == null) { badSteps++; continue; }
                    var stepSeen = new System.Collections.Generic.HashSet<string>();
                    for (int j = 0; j < data.QuestStepDataStorage.Count; j++) if (data.QuestStepDataStorage[j] == null || !stepSeen.Add(data.QuestStepDataStorage[j].id.ToString())) badSteps++;
                }
                lines.Add("Canonical database: " + known.Count + " quests"); lines.Add("Save log: " + log.Count + " entries");
                lines.Add("Null records: " + nulls + " | Duplicate quests: " + duplicates + " | Unknown IDs: " + unknown + " | Invalid/duplicate steps: " + badSteps);
                lines.Add((nulls + duplicates + badSteps == 0 ? "Integrity: OK" : "Integrity: REPAIR RECOMMENDED") + (unknown > 0 ? " | unknown IDs preserved" : ""));
            }
            catch (Exception e) { lines.Add("Audit failed: " + e.Message); }
            return lines.ToArray();
        }

        public static bool IsActiveQuestItem(object entry)
        {
            try
            {
                object def = GameApi.GetEntryDefinitionForUI(entry); if (def == null) return false;
                foreach (var quest in GetAllQuests()) if (quest != null && IsQuestActive(quest)) foreach (var step in quest.Steps)
                    if (step != null && step.StepType == QuestStepType.ItemCollection && step.ItemToCollect != null && object.Equals(step.ItemToCollect, def) && !QM.IsQuestStepCompleted(step, quest)) return true;
            }
            catch { }
            return false;
        }

        public static string[] GetMissingQuestItems(bool repair)
        {
            var lines = new System.Collections.Generic.List<string>(); int missingKinds = 0;
            try
            {
                var entries = GameApi.GetInventoryEntries();
                foreach (var quest in GetAllQuests()) if (quest != null && IsQuestActive(quest)) foreach (var step in quest.Steps)
                {
                    if (step == null || step.StepType != QuestStepType.ItemCollection || step.ItemToCollect == null || QM.IsQuestStepCompleted(step, quest)) continue;
                    int have = 0; for (int i = 0; i < entries.Length; i++) if (object.Equals(GameApi.GetEntryDefinitionForUI(entries[i]), step.ItemToCollect)) have += GameApi.GetEntryQuantityForUI(entries[i]);
                    int need = Math.Max(1, step.CompletionValue); if (have >= need) continue; int missing = need - have; missingKinds++;
                    lines.Add(quest.Name + " | " + step.Description + " | missing " + missing + " x " + step.ItemToCollect);
                    if (repair) GameApi.AddItemByDefinition(step.ItemToCollect, missing, 0);
                }
                if (missingKinds == 0) lines.Add("No missing active quest items detected"); else if (repair) lines.Insert(0, "Repaired quest item stacks: " + missingKinds);
            }
            catch (Exception e) { lines.Add("Quest item scan failed: " + e.Message); }
            return lines.ToArray();
        }
    }
}
