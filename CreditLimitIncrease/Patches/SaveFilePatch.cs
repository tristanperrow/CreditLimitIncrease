using BreakInfinity;
using CreditLimitIncrease.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreditLimitIncrease.Patches
{
    [HarmonyPatch(typeof(UI_SaveFile))]
    internal class SaveFilePatch
    {
        [HarmonyPatch(nameof(UI_SaveFile.SetSaveSlot))]
        [HarmonyPostfix]
        public static void SetSaveSlot(UI_SaveFile __instance)
        {
            if (__instance.currMiniSaveSlot != null && __instance.currMiniSaveSlot.slotIconImage.gameObject.activeSelf)
            {
                // get save file
                string str = "SaveFile";
                int num = __instance.currMiniSaveSlot.slotIndex + 1;
                ES3File es3File = new ES3File(str + num.ToString() + ".es3");

                // get value for text
                double mant = es3File.Load<double>(CreditLimitIncreasePlugin.mantissaString, 0);
                long expo = es3File.Load<long>(CreditLimitIncreasePlugin.expString, 0);

                if (mant > 0)
                {
                    BigDouble value = new BigDouble(mant, expo);

                    // replace text
                    __instance.moneyText.text = value.ToString("G" + CreditLimitIncreasePlugin.CreditsMantissaLength);
                }
            }
        }
    }
}
