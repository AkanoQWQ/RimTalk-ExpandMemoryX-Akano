using HarmonyLib;
using RimTalk.MemoryPatch;
using Verse;

namespace RimTalk.Memory.Patches
{
    // Postfix on RimTalk's PawnUtil.GetActivity() to append missing job target labels
    [HarmonyPatch(typeof(global::RimTalk.Util.PawnUtil), "GetActivity")]
    static class Patch_PawnUtil_GetActivity
    {
        static void Postfix(object[] __args, ref string __result)
        {
            if (!(RimTalkMemoryPatchMod.Settings?.enableActivityTargetPostfix ?? false))
                return;

            var pawn = __args[0] as Pawn;
            if (pawn == null || __result == null || pawn.CurJob == null)
                return;

            AppendTarget(pawn.CurJob.targetA, ref __result);
            AppendTarget(pawn.CurJob.targetB, ref __result);
            AppendTarget(pawn.CurJob.targetC, ref __result);
        }

        private static void AppendTarget(LocalTargetInfo target, ref string result)
        {
            if (!target.IsValid) return;

            string label = target.Thing?.LabelShort;
            if (string.IsNullOrEmpty(label)) return;

            if (result.Contains(label)) return;

            result = $"{result} (target:{label})";
        }
    }
}
