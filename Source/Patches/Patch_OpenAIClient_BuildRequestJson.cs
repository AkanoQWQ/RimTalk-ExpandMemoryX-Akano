using HarmonyLib;
using RimTalk.MemoryPatch;

namespace RimTalk.Memory.Patches
{
    [HarmonyPatch(typeof(global::RimTalk.Client.OpenAI.OpenAIClient), "BuildRequestJson")]
    static class Patch_OpenAIClient_BuildRequestJson
    {
        static void Postfix(ref string __result)
        {
            var fields = RimTalkMemoryPatchMod.Settings?.originalCustomApiFields?.Trim();
            if (!string.IsNullOrEmpty(fields))
            {
                int lastBrace = __result.LastIndexOf('}');
                if (lastBrace > 0)
                {
                    __result = __result.Insert(lastBrace, "," + fields);
                }
            }
        }
    }
}
