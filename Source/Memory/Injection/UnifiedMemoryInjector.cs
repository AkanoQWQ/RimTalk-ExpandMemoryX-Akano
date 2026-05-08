using System.Collections.Generic;
using System.Linq;
using Verse;
using RimTalk.MemoryPatch;

namespace RimTalk.Memory.Injection
{
    /// <summary>
    /// 统一记忆注入调度器
    /// 职责：配额管理 + 管线调度
    /// 
    /// 解决的问题：
    /// 1. 对话记忆优先（ABMCollector 实现）
    /// 2. ABM 占用总配额（本类实现）
    /// 3. 序号连续（MemoryFormatter 实现）
    /// </summary>
    public static class UnifiedMemoryInjector
    {
        /// <summary>
        /// 注入记忆主入口
        /// 
        /// 流程：
        /// 1. 采集 ABM（对话优先 + 行为补位）- 使用 maxABMInjectionRounds
        /// 2. 计算剩余配额 = maxTotalMemories - ABM实际数
        /// 3. 采集 ELS/CLPA（关键词匹配）- 使用剩余配额
        /// 4. 统一编号输出
        /// </summary>
        /// <param name="pawn">目标 Pawn</param>
        /// <param name="dialogueContext">对话上下文（用于 ELS/CLPA 匹配）</param>
        /// <returns>格式化的记忆文本，序号连续</returns>
        public static string Inject(Pawn pawn, string dialogueContext)
        {
            if (pawn == null)
                return string.Empty;
            
            var settings = RimTalkMemoryPatchMod.Settings;
            int maxABMRounds = settings?.maxABMInjectionRounds ?? 3;
            int maxTotalMemories = settings?.maxInjectedMemories ?? 10;

            // Step 1: Collect ABM, then cap raw conversations
            // cap twice for more capacity
            var abmList = ABMCollector.Collect(pawn, maxABMRounds);
            int maxConv = settings?.maxConversationABM ?? 1;
            CapConversations(abmList, maxConv);

            if (Prefs.DevMode)
            {
                Log.Message($"[UnifiedMemoryInjector] ABM collected: {abmList.Count}/{maxABMRounds} for {pawn.LabelShort}");
            }
            
            // Step 2: Collect ELS/CLPA, then cap raw conversations (catches SCM)
            int remainingQuota = maxTotalMemories - abmList.Count;
            var elsList = new List<MemoryEntry>();
            if (remainingQuota > 0)
            {
                elsList = ELSCollector.Collect(pawn, dialogueContext, remainingQuota);

                if (Prefs.DevMode)
                {
                    Log.Message($"[UnifiedMemoryInjector] ELS/CLPA collected: {elsList.Count}/{remainingQuota} for {pawn.LabelShort}");
                }
            }
            
            // Step 3: Merge, cap again and format
            var allMemories = new List<MemoryEntry>();
            allMemories.AddRange(abmList);
            allMemories.AddRange(elsList);
            CapConversations(allMemories, maxConv);

            if (allMemories.Count == 0)
            {
                return "(No available memory due to filter)";
            }
            if (Prefs.DevMode)
            {
                Log.Message($"[UnifiedMemoryInjector] Total memories: {allMemories.Count}/{maxTotalMemories} for {pawn.LabelShort}");
            }
            return MemoryFormatter.Format(allMemories, startIndex: 1);
        }
        
        /// <summary>
        /// 仅采集 ABM（用于独立的 {{pawn.ABM}} 变量）
        /// 向后兼容：保留原有的 ABM 独立变量功能
        /// </summary>
        public static string InjectABMOnly(Pawn pawn)
        {
            if (pawn == null)
                return string.Empty;
            
            var settings = RimTalkMemoryPatchMod.Settings;
            int maxABMRounds = settings?.maxABMInjectionRounds ?? 3;
            
            var abmList = ABMCollector.Collect(pawn, maxABMRounds);

            // Cap conversation memories
            int maxConv = settings?.maxConversationABM ?? 1;
            CapConversations(abmList, maxConv);

            if (abmList.Count == 0)
            {
                return "(No ABM memories)";
            }
            
            return MemoryFormatter.Format(abmList, startIndex: 1);
        }

        // Have removed unused InjectWithDetails()

        // Cap raw conversation entries (Active/Situational layers only, ELS/CLPA summaries ignored)
        private static bool IsRawConversation(MemoryEntry m)
            => m.type == MemoryType.Conversation && (m.layer == MemoryLayer.Active || m.layer == MemoryLayer.Situational);

        private static void CapConversations(List<MemoryEntry> memories, int max)
        {
            if (max <= 0)
            {
                memories.RemoveAll(m => IsRawConversation(m));
                return;
            }
            var convs = memories.Where(m => IsRawConversation(m)).ToList();
            if (convs.Count > max)
            {
                var keep = convs.OrderBy(_ => Rand.Value).Take(max).ToHashSet();
                memories.RemoveAll(m => IsRawConversation(m) && !keep.Contains(m));
            }
        }
    }
}