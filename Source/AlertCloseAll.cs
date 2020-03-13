﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CloseAll
{
    public class AlertCloseAll: Alert
    {
        public AlertCloseAll()
        {
            defaultLabel = "CloseAll".Translate();
            defaultPriority = AlertPriority.High;
            defaultExplanation = "CloseAllDesc".Translate();
        }

        protected override Color BGColor => new Color(1f, 1f, 1f, 0.2f);

        protected override void OnClick()
        {
            Find.LetterStack.LettersListForReading.ListFullCopy().ForEach(letter => Find.LetterStack.RemoveLetter(letter));
        }

        public override AlertReport GetReport()
        {
            return Find.LetterStack.LettersListForReading.Count > 0;
        }
    }

    
    [HarmonyPatch(typeof(Alert))]
    [HarmonyPatch("Notify_Started")]
    public static class Alert_Notify_Started_Patch
    {
        static readonly MethodInfo _levelLoad = AccessTools.Property(typeof(Time), "timeSinceLevelLoad").GetMethod;
        static readonly MethodInfo _checkType =
            SymbolExtensions.GetMethodInfo(() => CheckType(null));
        
        static bool CheckType(Alert alert)
        {
            return alert.GetType() == typeof(AlertCloseAll);
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var injected = false;
            var foundLabel = false;
            var label = new Label?();
            foreach (var instruction in instructions)
            {
                if (!foundLabel && instruction.Branches(out label)) foundLabel = true;
                if (!injected && instruction.Calls(_levelLoad) && label.HasValue)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, _checkType);
                    yield return new CodeInstruction(OpCodes.Brtrue, label.Value);

                    injected = true;
                }
                yield return instruction;
            }
        }
    }
}