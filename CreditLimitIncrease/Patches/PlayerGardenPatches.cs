using BreakInfinity;
using CreditLimitIncrease.Utils;
using HarmonyLib;
using PolyLabs;
using TMPro;
using UnityEngine;

namespace CreditLimitIncrease.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(PlayerGarden))]
    internal class PlayerGardenPatches
    {

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(ref Animator ___cellsAnim, ref ParticleSystem ___creditsParticles)
        {
            BigCreditsManager.Instance.gardenInstance = PlayerGarden.instance;
            BigCreditsManager.Instance.anim = ___cellsAnim;
            BigCreditsManager.Instance.particles = ___creditsParticles;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(PlayerGarden __instance, ref float ___timeToChargeByUkelele, ref float ___timeGetNearItems)
        {
            /*
            if (ArtifactsManager.instance.GetArtifactTier(0) >= 0)
            {
                __instance.artifact_TimeToAddCredit -= Time.deltaTime;
                if (__instance.artifact_TimeToAddCredit <= 0f)
                {
                    __instance.artifact_TimeToAddCredit = 5f;
                    BigCreditsManager.Instance.AddCredits((int)ArtifactsManager.instance.GetArtifactEfficiency(0));
                }
            }
            if (ArtifactsManager.instance.GetArtifactTier(12) >= 0)
            {
                ___timeToChargeByUkelele -= Time.deltaTime;
                if (___timeToChargeByUkelele <= 0f)
                {
                    ___timeToChargeByUkelele = 60f;
                    PlayerWeaponManager.instance.Shoot_ChargeNearbyBuilds_Ukelele();
                    if (UpgradesData.instance.GetUpgrade_Bool(49))
                    {
                        __instance.SpawnRing();
                    }
                }
            }
            ___timeGetNearItems -= Time.deltaTime;
            if (___timeGetNearItems < 0f)
            {
                ___timeGetNearItems = UnityEngine.Random.Range(0f, 0.25f);
                int num = 1;
                num += (int)ArtifactsManager.instance.GetArtifactEfficiency(5);
                num += StatusManager.instance.GetEffectState_Int(11);
                Collider[] array = Physics.OverlapSphere(__instance.transform.position, (float)num, LayerMask.GetMask(new string[] { "Coin" }));
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].GetComponent<ItemPrefab>().PickupItemFromPlayer(__instance.transform);
                }
            }
            */
        }

        [HarmonyPatch(nameof(PlayerGarden.AddCredits))]
        [HarmonyPrefix]
        public static bool AddCredits_Prefix(ref int _quantity)
        {
            BigCreditsManager.Instance.AddCredits(_quantity);
            return false;
        }

        [HarmonyPatch(nameof(PlayerGarden.RemoveCredits))]
        [HarmonyPrefix]
        public static bool RemoveCredits_Prefix(ref int _quantity)
        {
            BigCreditsManager.Instance.RemoveCredits(_quantity);
            return false;
        }

        /// <summary>
        /// Patches the PlayerGarden UpdateCreditsText method with prefix code.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(PlayerGarden.UpdateCreditsText))]
        [HarmonyPrefix]
        public static bool UpdateCreditsText_Prefix(ref TextMeshProUGUI ___cellsText, ref GameObject ___creditsGroupGO)
        {
            PlayerGarden instance = PlayerGarden.instance;
            ___creditsGroupGO.SetActive(true);
            // Update credits with the new BigDouble format if max value
            BigDouble creds = BigCreditsManager.Instance.credits;
            //CreditLimitIncreasePlugin.Log.LogInfo("Big Credits: " + creds);
            TMP_Text tmp_Text = ___cellsText;
            tmp_Text.text = creds.ToString("G" + CreditLimitIncreasePlugin.CreditsMantissaLength);

            return false;
        }

        /// <summary>
        /// Patches the PlayerGarden UpdateCreditsText method with postfix code.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(PlayerGarden.UpdateCreditsText))]
        [HarmonyPostfix]
        public static void UpdateCreditsText_Postfix()
        {
            PlayerGarden instance = PlayerGarden.instance;
        }
    }
}