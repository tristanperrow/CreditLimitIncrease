using BreakInfinity;
using CreditLimitIncrease.Utils;
using HarmonyLib;
using MyPooler;
using PolyLabs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreditLimitIncrease.Patches
{

    // ADD PATCHES

    [HarmonyPatch(typeof(Build_Market))]
    internal class BuildMarketPatches
    {

        [HarmonyPatch(nameof(Build_Market.ReturnItemsToPlayer))]
        [HarmonyPrefix]
        public static bool ReturnItemsToPlayer_Prefix(Build_Market __instance)
        {
            if (UpgradesData.instance.GetUpgrade_Bool(37))
                BigCreditsManager.Instance.AddCredits(__instance.currCredits);
            return false;
        }

        [HarmonyPatch(nameof(Build_Market.TakeOutItems))]
        [HarmonyPrefix]
        public static bool TakeOutItems_Prefix(Build_Market __instance)
        {
            if (__instance.currCredits == 0 || !UpgradesData.instance.GetUpgrade_Bool(37))
            {
                AudioManager2.instance.PlaySound("Cancel");
                return false;
            }
            AudioManager2.instance.PlaySound_Local("Build_SoilMiner_TakeItems", __instance.transform.position, 1f, 5f);
            BigCreditsManager.Instance.AddCredits(__instance.currCredits);
            __instance.doSimpleTween.Tween();
            __instance.currCredits = 0;
            __instance.clickerReceiver.canClick = false;
            return false;
        }

        [HarmonyPatch(nameof(Build_Market.TakeOutItemsFromClicker))]
        [HarmonyPrefix]
        public static bool TakeOutItemsFromClicker_Prefix(Build_Market __instance)
        {
            AudioManager2.instance.PlaySound_Local("Build_SoilMiner_TakeItems", __instance.transform.position, 1f, 5f);
            BigCreditsManager.Instance.AddCredits(__instance.currCredits);
            __instance.doSimpleTween.Tween();
            __instance.currCredits = 0;
            __instance.clickerReceiver.canClick = false;
            return false;
        }

    }

    [HarmonyPatch(typeof(Build_Research))]
    internal class BuildResearchPatches
    {
        [HarmonyPatch(nameof(Build_Research.ReturnItemsToPlayer))]
        [HarmonyPrefix]
        public static bool ReturnItemsToPlayer_Prefix(Build_Research __instance)
        {
            if (__instance._currItem == null)
                return false;
            BigCreditsManager.Instance.AddCredits(__instance._currItem.creditsToResearch);
            return false;
        }

        [HarmonyPatch(nameof(Build_Research.SetItemToCraft))]
        [HarmonyPrefix]
        public static bool SetItemToCraft_Prefix(Build_Research __instance, int _currRecipeIndex, bool _fromLoading)
        {
            __instance.currRecipeIndex = _currRecipeIndex;
            __instance._currItem = __instance.craftRecipesList[__instance.currRecipeIndex].recipesInfo[0];
            __instance.timeToCraft = (float)__instance._currItem.secondsToResearch - (float)__instance._currItem.secondsToResearch * 0.15f * (float)UpgradesData.instance.GetUpgrade_Int(3);
            __instance._timeToCraft = __instance.timeToCraft;
            __instance.onWorkEvent.Invoke();
            if (!_fromLoading)
            {
                BigCreditsManager.Instance.RemoveCredits(__instance._currItem.creditsToResearch);
                __instance.UpdateKeyTips();
                InteractManager.instance.UpdateKeyTips();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(CreditOrb))]
    internal class CreditOrbPatches
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool Update_Prefix(CreditOrb __instance, ref bool ___canGoToTarget, ref Rigidbody ___rb, ref bool ___oneTime)
        {
            if (!___canGoToTarget)
            {
                return false;
            }
            ___rb.AddForce((__instance.target.position - __instance.transform.position).normalized * __instance.speed * Time.deltaTime);
            if (Vector3.Distance(__instance.transform.position, __instance.target.position) < 0.5f && !___oneTime)
            {
                ___oneTime = true;
                BigCreditsManager.Instance.AddCredits(__instance.quantity);
                AudioManager2.instance.PlaySound_Pitch("AddPoint", 0.15f);
                __instance.DiscardToPool();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(CrystalShard))]
    internal class CrystalShardPatches
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        public static bool OnTriggerEnter(CrystalShard __instance, Collider other, ref Rigidbody ___rb, ref bool ___followPlayer)
        {
            if (other.GetComponent<PlayerGarden>() != null)
            {
                if (!___followPlayer)
                {
                    __instance.Invoke("EnableCanGoToTarget", __instance.delayToGo);
                    ___rb.useGravity = false;
                    ___rb.AddForce(new Vector3(UnityEngine.Random.Range(-1f, 1f), 1f, UnityEngine.Random.Range(-1f, 1f)) * __instance.initialForce, ForceMode.Impulse);
                    ___followPlayer = true;
                    __instance.sphereCollider.radius = 0.3f;
                    return false;
                }
                UnityEngine.Object.Instantiate<GameObject>(__instance.smokeParticles, __instance.transform.position, Quaternion.identity);
                BigCreditsManager.Instance.AddCredits(1);
                MusicManager.instance.AddVolume(1f, 0.5f);
                UnityEngine.Object.Destroy(__instance.gameObject);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(EnemyHealth))]
    internal class EnemyHealthPatches
    {

        [HarmonyPatch(nameof(EnemyHealth.DestroyEnemy))]
        [HarmonyPrefix]
        public static bool DestroyEnemy_Prefix(EnemyHealth __instance, bool _showBar, ref int ___xpWhenDestroy)
        {
            ItemInfo toolItemInfo = InventoryManager.instance.toolItemInfo;
            if (toolItemInfo != null && toolItemInfo.replaceEnemyDrops && InventoryManager.instance.toolIndex != -1)
            {
                __instance.itemPoolGroups = toolItemInfo.extraDropsReplceWhenKillEnemies;
            }
            EnemyHealth ___base = __instance;

            __instance.SpawnDrops(1f, ___base.transform.position);
            if (UpgradesData.instance.GetUpgrade_Bool(13))
            {
                ItemList.instance.SpawnItemPrefab(ItemList.instance.itemList[63], ___base.transform.position, 2);
            }
            if (_showBar)
            {
                CameraShake.instance.Add(0.5f, 0.2f, 0.2f, CameraShakeTarget.Position, CameraShakeAmplitudeCurve.FadeInOut25);

                Type type = typeof(EnemyHealth);
                MethodInfo method = AccessTools.Method(type, "CheckExtraDrops");

                if (method != null)
                {
                    method.Invoke(__instance, null);
                }

                __instance.onDeathEvent.Invoke();
                if (UpgradesData.instance.GetUpgrade_Bool(46))
                {
                    PlayerGarden.instance.AddStat_Energy((float)(2 * UpgradesData.instance.GetUpgrade_Int(46)));
                }
                if (toolItemInfo != null && toolItemInfo.itemID == 85)
                {
                    SteamIntegration.instance.UnlockAchievement("Your soul is mine", 11);
                }
                GameStats.instance.AddStat_CreatureKills();
            }
            if (__instance.shiny && UpgradesData.instance.GetUpgrade_Bool(42) && UnityEngine.Random.value < 0.05f * (float)UpgradesData.instance.GetUpgrade_Int(42))
            {
                foreach (Collider collider in Physics.OverlapSphere(___base.transform.position, 10f, LayerMask.GetMask(new string[]
                {
                "Enemy"
                })))
                {
                    if (collider.gameObject != ___base.gameObject)
                    {
                        EnemyHealth component = collider.GetComponent<EnemyHealth>();
                        if (component != null && component.canBeShiny)
                        {
                            component.ConvertToShiny(__instance.isleGeneratorParent.makePropShinyParticlesGO, __instance.isleGeneratorParent.shinyParticlesGO);
                            break;
                        }
                    }
                }
            }
            GameStats.instance.AddKilledCreature(__instance.creatureProperties.creatureIndex);
            UnityEngine.Object.Instantiate<GameObject>(__instance.particles_death, ___base.transform.position, Quaternion.identity);
            AudioManager2.instance.PlaySound_Local(__instance.SFX_death, ___base.transform.position, 3f, 20f);
            AudioManager2.instance.PlaySound_Local("Enemy_Death", ___base.transform.position, 3f, 20f);
            BigCreditsManager.Instance.AddCredits(___xpWhenDestroy);
            PlayerGarden.instance.spawnCreditsText(___base.transform.position, __instance.particlesOffset, ___xpWhenDestroy);
            if (__instance.isleGeneratorParent != null)
            {
                __instance.isleGeneratorParent.ReduceCooldownCreature();
            }
            InteractManager.instance.ResetPropNameInfo();
            UnityEngine.Object.Destroy(___base.gameObject);
            return false;
        }
        static bool flag = false;

        [HarmonyPatch(nameof(EnemyHealth.TryKill_Structure))]
        [HarmonyPrefix]
        public static bool TryKill_Structure_Prefix(EnemyHealth __instance, float times, ref int ___xpWhenDestroy)
        {
            __instance.currHealth -= times;
            flag = __instance.currHealth <= 0f;
            if (!flag)
            {
                AudioManager2.instance.PlaySound_Local(__instance.SFX_hit, __instance.transform.position, 3f, 20f);
                AudioManager2.instance.PlaySound_Local("Enemy_Hit", __instance.transform.position, 3f, 20f);
                __instance.onHitEvent.Invoke();
                __instance.matTintColor.FlashMe();
                return false;
            }
            if (PauseUI.instance.save_GifOpenFolder == 1)
            {
                ObjectPooler.Instance.GetFromPool("Particle_HitProp", __instance.transform.position + __instance.particlesOffset, __instance.particles_hit.transform.rotation);
            }
            AudioManager2.instance.PlaySound_Local(__instance.SFX_death, __instance.transform.position, 3f, 20f);
            AudioManager2.instance.PlaySound_Local("Enemy_Death", __instance.transform.position, 3f, 20f);
            BigCreditsManager.Instance.AddCredits(___xpWhenDestroy);
            UnityEngine.Object.Destroy(__instance.gameObject);
            return false;
        }
    }

    [HarmonyPatch(typeof(GardenItem))]
    internal class GardenItemPatches
    {
        [HarmonyPatch(nameof(GardenItem.PlaceItemToWorld))]
        [HarmonyPrefix]
        public static bool PlaceItemToWorld_Prefix(GardenItem __instance, IsleParentGenerator _IPG) {
            __instance.IPG = _IPG;
            MusicManager.instance.AddVolume(1f, 0.5f);
            __instance.doSimpleTween.Tween();
            __instance.objectPlaced = true;
            __instance.UseResources();
            UnityEngine.Object.Destroy(__instance.gardenItemColl);
            UnityEngine.Object.Destroy(__instance.rotateStructureIfBuilding);
            PlayerGardenInventory.instance.AddQuantityToBuilts(__instance.gardenItemInfo, 1);
            __instance.onPlacedEvent.Invoke();
            AudioManager2.instance.PlaySound_Local("PlaceObject", __instance.transform.position, 1f, 15f);
            AudioManager2.instance.PlaySound_Local(__instance.gardenItemInfo.soundWhenPlaced, __instance.transform.position, 1f, 15f);
            BigCreditsManager.Instance.AddCredits(__instance.gardenItemInfo.creditsWhenBuild);
            CameraShake.instance.Add(0.5f, 0.2f, 0.2f, CameraShakeTarget.Position, CameraShakeAmplitudeCurve.FadeInOut25);
            CameraShake.instance.Add(0.2f, 0.2f, 0.2f, CameraShakeTarget.Rotation, CameraShakeAmplitudeCurve.FadeInOut25);
            __instance.PasteBuild();
            if (__instance.interactable != null)
            {
                __instance.interactable.canInteract = true;
            }
            if (__instance.gardenItemInfo.buildIndex == 0)
            {
                TasksManager.instance.FinishTask(1, -1, 1f);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ItemInfo))]
    internal class ItemInfoPatches
    {
        [HarmonyPatch(nameof(ItemInfo.Eat))]
        [HarmonyPrefix]
        public static bool Eat_Prefix(ItemInfo __instance)
        {
            if (InventoryManager.instance.itemsInInv[__instance.itemID] <= 0)
            {
                AudioManager2.instance.PlaySound("Cancel");
                return false;
            }
            if (__instance.Can_Eat())
            {
                InventoryManager.instance.RemoveItemFromInv(__instance, 1, false);
                StatusManager.instance.AddEffect(__instance.effectID, __instance);
                if (__instance.foodEnergy != 0f)
                {
                    PlayerGarden.instance.AddStat_Energy(__instance.foodEnergy);
                }
                if (__instance.foodStamina != 0f)
                {
                    PlayerGarden.instance.AddStat_Stamina(__instance.foodStamina);
                }
                AudioManager2.instance.PlaySound(__instance.eatSound);
                if (UpgradesData.instance.GetUpgrade_Bool(41))
                {
                    BigCreditsManager.Instance.AddCredits(__instance.creditsWhenSell * UpgradesData.instance.GetUpgrade_Int(41));
                    return false;
                }
            }
            else
            {
                AudioManager2.instance.PlaySound("Cancel");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerFishingManager))]
    internal class PlayerFishingManagerPatches
    {
        [HarmonyPatch("CatchFish")]
        [HarmonyPrefix]
        public static bool CatchFish_Prefix(PlayerFishingManager __instance, ref ItemInfo ___baitInfo, ref ItemInfo ____baitInfo, ref bool ___treasureCollected)
        {
            ObjectPooler.Instance.GetFromPool("Particle_HitProp", __instance.fishingRodBait.transform.position, Quaternion.identity);
            AudioManager2.instance.PlaySound("Tool_FishingRod_CatchFish");
            BiomeFishingInfo biomeFishingInfo = (!MineManager.instance.inMine) ? IslandsManager.instance.currIsleParentGenerator.biomeInfo.biomeFishingInfo : MineManager.instance.currRoomInWorld.mineRoomInfo.biomeFishingInfo;
            BiomeFishingInfo.ItemDrop[] pool = (DayNightCycle.instance.timeOfDay == DayNightCycle.TimeOfDay.Day || MineManager.instance.inMine) ? biomeFishingInfo.fishLootPool_Day : biomeFishingInfo.fishLootPool_Night;
            bool flag = false;
            if (UpgradesData.instance.GetUpgrade_Bool(20))
            {
                flag = (UnityEngine.Random.value < 0.2f);
            }
            int num = 1 + Mathf.RoundToInt(UnityEngine.Random.Range(__instance.fishingRod_minMaxItems_Fish.x, __instance.fishingRod_minMaxItems_Fish.y));
            for (int i = 0; i < num; i++)
            {
                BiomeFishingInfo.ItemDrop itemFishLoot = biomeFishingInfo.GetItemFishLoot(pool);
                ItemInfo[] itemsInfo = itemFishLoot.itemsInfo;
                int num2 = Mathf.RoundToInt(UnityEngine.Random.Range(itemFishLoot.minMaxQuantity.x, itemFishLoot.minMaxQuantity.y));
                if (flag)
                {
                    num2 *= UpgradesData.instance.GetUpgrade_Int(20);
                }
                int num3 = UnityEngine.Random.Range(0, itemsInfo.Length);
                ItemList.instance.SpawnItemPrefab(itemsInfo[num3], __instance.fishingRodBait.transform.position + new Vector3(0f, 0f, 0f), num2).GetComponent<ItemPrefab>().FirstContact(PlayerGarden.instance.transform);
                ____baitInfo = ___baitInfo;
                float num4 = (float)(itemsInfo[num3].CalculateTotalCreditsValue() * num2 * __instance.itemInfo.efficiency) * biomeFishingInfo.creditsWhenCatchMult;
                if (____baitInfo != null && ____baitInfo.itemID == 46)
                {
                    num4 *= 1.5f;
                }
                BigCreditsManager.Instance.AddCredits(Mathf.RoundToInt(num4));
                PlayerGarden.instance.spawnCreditsText(__instance.fishingRodBait.transform.position, new Vector3(0f, 1f, 0f), (int)num4);
            }
            MusicManager.instance.AddVolume(10f, 0.5f);
            if (___treasureCollected)
            {
                ___treasureCollected = false;
                __instance.Invoke("CatchTresure", 0.5f);

                Type type = typeof(PlayerFishingManager);
                MethodInfo method = AccessTools.Method(type, "CatchTresure");

                CatchTresure_Prefix(__instance, __instance.fishingRodBait.transform.position, ref ____baitInfo);
            }
            return false;
        }

        [HarmonyPatch("CatchTresure")]
        [HarmonyPrefix]
        public static bool CatchTresure_Prefix(PlayerFishingManager __instance, Vector3 _pos, ref ItemInfo ____baitInfo)
        {
            __instance.StartCoroutine(CatchTresureCoroutine(__instance, _pos, ____baitInfo));
            return false;
        }

        static IEnumerator CatchTresureCoroutine(PlayerFishingManager __instance, Vector3 _pos, ItemInfo _baitInfo)
        {
            yield return new WaitForSeconds(0.3f);
            ObjectPooler.Instance.GetFromPool("Particle_HitProp", _pos, Quaternion.identity);
            AudioManager2.instance.PlaySound("Tool_FishingRod_OpenTresure");
            BiomeFishingInfo biomeFishingInfo = (!MineManager.instance.inMine)
                ? IslandsManager.instance.currIsleParentGenerator.biomeInfo.biomeFishingInfo
                : MineManager.instance.currRoomInWorld.mineRoomInfo.biomeFishingInfo;
            bool flag = false;
            if (UpgradesData.instance.GetUpgrade_Bool(20))
            {
                flag = (UnityEngine.Random.value < 0.2f);
            }
            int num = 1 + Mathf.RoundToInt(UnityEngine.Random.Range(__instance.fishingRod_minMaxItems_Treasure.x, __instance.fishingRod_minMaxItems_Treasure.y));
            for (int i = 0; i < num; i++)
            {
                BiomeFishingInfo.ItemDrop itemFishLoot = biomeFishingInfo.GetItemFishLoot(biomeFishingInfo.fishLootPool_Tresure);
                ItemInfo[] itemsInfo = itemFishLoot.itemsInfo;
                float num2 = UnityEngine.Random.Range(itemFishLoot.minMaxQuantity.x, itemFishLoot.minMaxQuantity.y);
                num2 += num2 * ArtifactsManager.instance.GetArtifactEfficiency(3);
                if (flag)
                {
                    num2 *= (float)UpgradesData.instance.GetUpgrade_Int(20);
                }
                int num3 = UnityEngine.Random.Range(0, itemsInfo.Length);
                var spawnedItem = ItemList.instance.SpawnItemPrefab(itemsInfo[num3], _pos, Mathf.RoundToInt(num2));
                spawnedItem.GetComponent<ItemPrefab>().FirstContact(PlayerGarden.instance.transform);

                float num4 = (float)itemsInfo[num3].CalculateTotalCreditsValue() * num2 * (float)__instance.itemInfo.efficiency * biomeFishingInfo.creditsWhenCatchMult;

                if (_baitInfo != null && _baitInfo.itemID == 46)
                {
                    num4 *= 1.5f;
                }

                BigCreditsManager.Instance.AddCredits(Mathf.RoundToInt(num4));
                PlayerGarden.instance.spawnCreditsText(_pos, new Vector3(0f, 2f, 0f), (int)num4);
            }
        }
    }

    [HarmonyPatch(typeof(Prop_BuyNextBiomePart))]
    internal class Prop_BuyNextBiomePartPatches
    {
        [HarmonyPatch(nameof(Prop_BuyNextBiomePart.AddItemToBundle))]
        [HarmonyPrefix]
        public static bool AddItemToBundle_Prefix(Prop_BuyNextBiomePart __instance, BuyNextBiomeOrb _bundleOrb, int _quantity, ref List<BuyNextBiomeOrb> ___orbsInWorld)
        {
            AudioManager2.instance.PlaySound_Local("Prop_BundleOrbPickup", __instance.transform.position, 1f, 5f);
            ___orbsInWorld.Remove(_bundleOrb);
            __instance.buyNextBiome.currQuantityLeft[__instance.partIndex] -= _quantity;
            __instance.UpdateText(__instance.buyNextBiome.currQuantityLeft[__instance.partIndex]);

            if (__instance.buyNextBiome.currQuantityLeft[__instance.partIndex] <= 0)
            {
                MusicManager.instance.AddVolume(10f, 0.5f);
                __instance.buyNextBiome.currQuantityLeft[__instance.partIndex] = 0;
                AudioManager2.instance.PlaySound_Local("Prop_Hit", __instance.transform.position, 3f, 20f);
                ObjectPooler.Instance.GetFromPool("Particle_HitProp", __instance.transform.position, Quaternion.identity);
                AudioManager2.instance.PlaySound_Local("Prop_BuyNextBiome_BreakPart", __instance.transform.position, 1f, 5f);

                int num = __instance.buyNextBiome.isleParentGenerator.biomeInfo.islePartsArray.Length - 1;
                int num2 = __instance.buyNextBiome.isleParentGenerator.biomeInfo.islePartsArray[num].pointsToBuyPart / 4;

                PlayerGarden.instance.spawnCreditsText(__instance.transform.position, new Vector3(0f, 0.25f, 0f), num2);

                BigCreditsManager.Instance.AddCredits(num2);

                BiomeInfo.ItemRequired itemRequired = __instance.buyNextBiome.isleParentGenerator.biomeInfo.itemsRequiredBuyNextBiome[__instance.partIndex];

                if (itemRequired.itemAsReward != null)
                {
                    ItemList.instance.SpawnItemPrefab(itemRequired.itemAsReward, __instance.transform.position, itemRequired.itemAsRewardQuantity).GetComponent<ItemPrefab>().FirstContact(PlayerGarden.instance.transform);
                }

                SteamIntegration.instance.UnlockAchievement("Bundle breaker", 35);
                __instance.gameObject.SetActive(false);
                CameraShake.instance.Add(0.3f, 0.2f, 0.2f, CameraShakeTarget.Position, CameraShakeAmplitudeCurve.FadeInOut25);
                __instance.buyNextBiome.UpdateGems();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(TakeOutResource))]
    internal class TakeOutResourcePatches
    {
        [HarmonyPatch("CheckIfDead")]
        [HarmonyPrefix]
        internal static bool CheckIfDead_Prefix(TakeOutResource __instance, bool _state, ref int ___xpWhenDestroy)
        {
            TakeOutResource Base = __instance;
            if (!_state)
            {
                AudioManager2.instance.PlaySound_Local(__instance.SFX_hit, Base.transform.position, 2f, 10f);
                AudioManager2.instance.PlaySound_Local("Prop_Hit", Base.transform.position, 2f, 10f);
                AudioManager2.instance.PlaySound_Local("Prop_Hit2", Base.transform.position, 2f, 10f);
                __instance.doSimpleTween.Tween();
                __instance.matTintColorMesh.FlashMe();
                return false;
            }
            if (PauseUI.instance.save_particlesDestroy == 1)
            {
                ObjectPooler.Instance.GetFromPool("Particle_HitProp", Base.transform.position + __instance.particlesOffset, __instance.particles_hit.transform.rotation);
            }
            AudioManager2.instance.PlaySound_Local(__instance.SFX_death, Base.transform.position, 2f, 10f);
            AudioManager2.instance.PlaySound_Local("Prop_Death2", Base.transform.position, 2f, 10f);
            int num = 0;
            if (UnityEngine.Random.value <= 0.3f)
            {
                num = Mathf.RoundToInt((float)___xpWhenDestroy * 0.2f * (float)UpgradesData.instance.GetUpgrade_Int(11));
            }
            PlayerGarden.instance.AddCredits(___xpWhenDestroy + num);
            __instance.onDestroyEvent.Invoke();
            UnityEngine.Object.Destroy(__instance.gameObject);
            return false;
        }

        [HarmonyPatch(nameof(TakeOutResource.TryTakeOut_General))]
        [HarmonyPrefix]
        public static bool TryTakeOut_General_Prefix(TakeOutResource __instance, float times, bool _showBar, bool showXPText, ref int ___xpWhenDestroy)
        {
            float num = (ArtifactsManager.instance.GetArtifactTier(2) >= 0) ? ArtifactsManager.instance.GetArtifactEfficiency(2) : 0f;
            times += times * num / 100f;
            if (__instance.propName == "Big Stone" && __instance.currHealth == __instance.health && __instance.currHealth - times <= 0f && InventoryManager.instance.toolIndex == -1)
            {
                SteamIntegration.instance.UnlockAchievement("One punch man", 48);
            }
            __instance.currHealth -= times;
            AudioManager2.instance.PlaySound_Local(__instance.SFX_hit, __instance.transform.position, 2f, 10f);
            AudioManager2.instance.PlaySound_Local("Prop_Hit", __instance.transform.position, 2f, 10f);
            AudioManager2.instance.PlaySound_Local("Prop_Hit2", __instance.transform.position, 2f, 10f);

            if (PauseUI.instance.save_GifOpenFolder == 1)
            {
                ObjectPooler.Instance.GetFromPool("Particle_HitProp", __instance.transform.position + __instance.particlesOffset, __instance.particles_hit.transform.rotation);
            }

            if (__instance.currHealth > 0f)
            {
                if (_showBar)
                {
                    InteractManager.instance.SetPropName(__instance.propName, __instance.currHealth, __instance.health);
                    InteractManager.instance.UpdatePropHealthBar(__instance.currHealth, __instance.health);
                }

                __instance.doSimpleTween.Tween();
                __instance.matTintColorMesh.FlashMe();
                return false;
            }

            AudioManager2.instance.PlaySound_Local(__instance.SFX_death, __instance.transform.position, 2f, 10f);
            AudioManager2.instance.PlaySound_Local("Prop_Death2", __instance.transform.position, 2f, 10f);
            __instance.SpawnDrops(1f);

            int num2 = 0;
            if (UnityEngine.Random.value <= 0.3f)
            {
                num2 = Mathf.RoundToInt((float)___xpWhenDestroy * 0.2f * (float)UpgradesData.instance.GetUpgrade_Int(11));
            }

            if (__instance.shiny)
            {
                GameStats.instance.AddStat_ResourceDestroyShiny();

                if (UpgradesData.instance.GetUpgrade_Bool(42) && UnityEngine.Random.value < 0.05f * (float)UpgradesData.instance.GetUpgrade_Int(42))
                {
                    foreach (Collider collider in Physics.OverlapSphere(__instance.transform.position, 10f, LayerMask.GetMask(new string[] { "Interact" })))
                    {
                        if (collider.gameObject != __instance.gameObject)
                        {
                            TakeOutResource component = collider.GetComponent<TakeOutResource>();
                            if (component != null && component.canBeShiny)
                            {
                                component.ConvertToShiny(__instance.makePropShinyParticlesGO, __instance.shinyParticlesGO);
                                break;
                            }
                        }
                    }
                }
            }

            float num3 = (float)(___xpWhenDestroy + num2);
            num3 *= __instance.mult_PropFromCrop;

            if (_showBar)
            {
                CameraShake.instance.Add(0.3f, 0.2f, 0.2f, CameraShakeTarget.Position, CameraShakeAmplitudeCurve.FadeInOut25);
                InteractManager.instance.ResetPropNameInfo();

                Type type = typeof(TakeOutResource);
                MethodInfo method = AccessTools.Method(type, "CheckExtraDrops");

                if (method != null)
                    method.Invoke(__instance, null);
                MusicManager.instance.AddVolume(2f, 0.5f);
                num3 += num3 * StatusManager.instance.GetEffectState_Float(13);

                if (__instance.propName == "Tree")
                {
                    TasksManager.instance.FinishTask(0, 1, 4f);
                }
            }

            BigCreditsManager.Instance.AddCredits((int)num3);

            if (showXPText)
            {
                PlayerGarden.instance.spawnCreditsText(__instance.transform.position, __instance.particlesOffset, (int)num3);
            }

            __instance.onDestroyEvent.Invoke();
            UnityEngine.Object.Destroy(__instance.gameObject);

            return false;
        }
    }

    [HarmonyPatch(typeof(TresureChest))]
    internal class TresureChestPatches
    {
        [HarmonyPatch(nameof(TresureChest.DamageChest))]
        [HarmonyPrefix]
        public static bool DamageChest_Prefix(TresureChest __instance, ref float ___health, ref TresureChest.ItemDropGroup[] ___dropPool, ref float ___extraRange, ref int ___xpWhenDestroy)
        {
            int num = (InventoryManager.instance.toolIndex != -1 && InventoryManager.instance.toolIndex == __instance.toolType) ? InventoryManager.instance.toolItemInfo.efficiency : 0;
            float num2 = (float)(1 + num);
            num2 += (float)StatusManager.instance.GetEffectState_Int(1);
            num2 += num2 * 0.15f * (float)UpgradesData.instance.GetUpgrade_Int(10);
            num2 += num2 * InteractManager.instance.GetClickCombo();
            float num3 = 0.01f + (float)UpgradesData.instance.GetUpgrade_Int(6) * 0.04f;
            float num4 = (float)StatusManager.instance.GetEffectState_Int(0) * 0.1f;
            if (UnityEngine.Random.value <= num3 + num4)
            {
                num2 *= 1.5f + (float)UpgradesData.instance.GetUpgrade_Int(7) * 0.12f;
                UnityEngine.Object.Instantiate<GameObject>(__instance.particles_hitCrit, __instance.transform.position + __instance.particlesOffset, __instance.particles_hit.transform.rotation);
                AudioManager2.instance.PlaySound("Prop_Crit");
            }
            __instance.currHealth -= num2;
            __instance.anim.Play("Hit", -1, 0f);
            AudioManager2.instance.PlaySound(__instance.SFX_hit);
            AudioManager2.instance.PlaySound("Prop_Hit2");
            UnityEngine.Object.Instantiate<GameObject>(__instance.particles_hit, __instance.transform.position + __instance.particlesOffset, __instance.particles_hit.transform.rotation);
            InteractManager.instance.SetPropName(__instance.propName, __instance.currHealth, ___health);
            InteractManager.instance.UpdatePropHealthBar(__instance.currHealth, ___health);
            if (__instance.currHealth <= 0f)
            {
                AudioManager2.instance.PlaySound(__instance.SFX_death);
                AudioManager2.instance.PlaySound("Prop_Death2");
                for (int i = 0; i < ___dropPool.Length; i++)
                {
                    int num5 = Mathf.RoundToInt(UnityEngine.Random.Range(___dropPool[i].quantityToSpawn.x, ___dropPool[i].quantityToSpawn.y) * ___extraRange);
                    num5 += Mathf.RoundToInt((float)num5 * 0.5f * (float)UpgradesData.instance.GetUpgrade_Int(19));

                    Type type = typeof(TresureChest);
                    MethodInfo method = AccessTools.Method("GetItemInfo");
                    object[] parameters = new object[1];
                    parameters[0] = i;

                    for (int j = 0; j < num5; j++)
                    {
                        ItemList.instance.SpawnItemPrefab((ItemInfo) method.Invoke(__instance, parameters), __instance.transform.position, 1);
                    }
                }
                InteractManager.instance.ResetPropNameInfo();
                PlayerGarden.instance.AddCredits(___xpWhenDestroy);
                MusicManager.instance.AddVolume(1f, 0.5f);
                UnityEngine.Object.Destroy(__instance.gameObject);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(UI_MarketManager))]
    internal class UI_MarketManagerPatches
    {
        [HarmonyPatch(nameof(UI_MarketManager.TryDoTrade))]
        [HarmonyPrefix]
        public static bool TryDoTrade_Prefix(UI_MarketManager __instance, int _listIndex, ItemInfo _itemInfo, int _quantity)
        {
            if (InventoryManager.instance.itemsInInv[_itemInfo.itemID] >= _quantity)
            {
                int num = (int)((float)_itemInfo.CalculateTotalCreditsValue() * (1f + 0.1f * UpgradesData.instance.GetUpgrade_Float(48)));
                BigCreditsManager.Instance.AddCredits(num * _quantity);
            }
            return true;
        }

        // remove patch

        [HarmonyPatch(nameof(UI_MarketManager.ResetTrades))]
        [HarmonyPrefix]
        public static bool ResetTrades_Prefix(UI_MarketManager __instance)
        {
            Type type = typeof(UI_MarketManager);
            MethodInfo method = AccessTools.Method("GetPriceReset");

            int priceReset = 0;
            if (method != null)
            {
                priceReset = (int) method.Invoke(__instance, null);
            }
            if (BigCreditsManager.Instance.credits >= priceReset)
            {
                AudioManager2.instance.PlaySound("Build_TradePost_BuyReset");
                BigCreditsManager.Instance.RemoveCredits(priceReset);
                __instance.build_Market.ResetTrades();
                if (priceReset >= 10000)
                {
                    SteamIntegration.instance.UnlockAchievement("Market Resurrector", 77);
                    return false;
                }
            }
            else
            {
                AudioManager2.instance.PlaySound("Cancel");
            }
            return false;
        }
    }

    // REMOVE PATCHES

    [HarmonyPatch(typeof(Prop_BiomeExpansion))]
    internal class Prop_BiomeExpansionPatches
    {
        [HarmonyPatch("AddPoints")]
        [HarmonyPrefix]
        public static bool AddPoints_Prefix(Prop_BiomeExpansion __instance)
        {
            if (__instance.isAddingPoints)
            {
                return false;
            }
            __instance.anim.Play("Hit", -1, 0f);
            if (BigCreditsManager.Instance.credits > 0)
            {
                if (BigCreditsManager.Instance.credits + __instance.currCreditsSpent >= __instance.maxPoints)
                {
                    int quantity = __instance.maxPoints - __instance.currCreditsSpent;
                    BigCreditsManager.Instance.RemoveCredits(quantity);
                    __instance.currCreditsSpentTarget = __instance.maxPoints;
                }
                else
                {
                    // IMPORTANT CODE
                    // will need to be changed if want to change to long type
                    BigDouble credits = BigCreditsManager.Instance.credits;
                    BigCreditsManager.Instance.RemoveCredits(credits);
                    __instance.currCreditsSpentTarget += (int)(credits.Mantissa * Mathf.Pow(10, credits.Exponent));
                }
                ((MonoBehaviour)__instance).StartCoroutine("Lerp");
                __instance.isAddingPoints = true;
                return false;
            }
            AudioManager2.instance.PlaySound("Cancel");
            return false;
        }
    }

    [HarmonyPatch(typeof(Prop_BiomeTeleporter))]
    internal class Prop_BiomeTeleporterPatches
    {
        [HarmonyPatch("AddPoints")]
        [HarmonyPrefix]
        public static bool AddPoints_Prefix(Prop_BiomeTeleporter __instance)
        {
            if (__instance.isAddingPoints)
            {
                return false;
            }
            __instance.anim.Play("Hit", -1, 0f);
            if (BigCreditsManager.Instance.credits > 0)
            {
                if (BigCreditsManager.Instance.credits + __instance.currCreditsSpent >= __instance.maxPointsToBuyNextBiome)
                {
                    int quantity = __instance.maxPointsToBuyNextBiome - __instance.currCreditsSpent;
                    BigCreditsManager.Instance.RemoveCredits(quantity);
                    __instance.currCreditsSpentTarget = __instance.maxPointsToBuyNextBiome;
                }
                else
                {
                    // IMPORTANT CODE
                    // will need to be changed if want to change to long type
                    BigDouble credits = BigCreditsManager.Instance.credits;
                    BigCreditsManager.Instance.RemoveCredits(credits);
                    __instance.currCreditsSpentTarget += (int) (credits.Mantissa * Mathf.Pow(10, credits.Exponent));
                }
                ((MonoBehaviour)__instance).StartCoroutine("Lerp");
                __instance.isAddingPoints = true;
                return false;
            }
            AudioManager2.instance.PlaySound("Cancel");
            return false;
        }
    }

    [HarmonyPatch(typeof(UI_CreditShopManager))]
    internal class UI_CreditShopManagerPatches
    {
        [HarmonyPatch(nameof(UI_CreditShopManager.BuyUpgrade))]
        [HarmonyPrefix]
        public static bool BuyUpgrade(UI_CreditShopManager __instance, ref Build_CreditShop ___creditShop,ref UI_ShopSlot ___currShopSlot)
        {
            int num = __instance.upgradeInfo.costList[UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex]];
            if (BigCreditsManager.Instance.credits >= num)
            {
                BigCreditsManager.Instance.RemoveCredits(num);
                UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex]++;
                UnityEngine.Object.Instantiate<GameObject>(__instance.buyUpgradeParticles, PlayerGarden.instance.transform.position, __instance.buyUpgradeParticles.transform.rotation);
                ___creditShop.MakeBounce();

                Type type = typeof(UI_CreditShopManager);
                MethodInfo method = AccessTools.Method("UpdateUpgradesPoints");
                MethodInfo method2 = AccessTools.Method("UpdateUpgradeDescription");

                if (method != null)
                {
                    method.Invoke(__instance, null);
                }
                if (method2 != null)
                {
                    method2.Invoke(__instance, null);
                }

                if (UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex] == 1)
                {
                    AudioManager2.instance.PlaySound("Build_CreditShop_BuyUpgradeLearn");
                }
                else
                {
                    AudioManager2.instance.PlaySound("Build_CreditShop_BuyUpgrade");
                }
                __instance.upgradeInfo.onUpdateSkillEvent.Invoke();
                ___currShopSlot.UpdateBuyBGIcon();
                bool state = __instance.CanBuyAnyUpgrade();
                __instance.canBuySkills = state;
                for (int i = 0; i < __instance.build_CreditShops.Count; i++)
                {
                    __instance.build_CreditShops[i].ToggleEffects(state);
                }
                ((MonoBehaviour) __instance).StartCoroutine("UpdateShop");
                if (__instance.upgradeInfo.costList.Length >= 7 && UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex] >= __instance.upgradeInfo.costList.Length)
                {
                    SteamIntegration.instance.UnlockAchievement("Mastering a skill", 39);
                }
                bool flag = true;
                for (int j = 0; j < __instance.upgradeGroups.Length; j++)
                {
                    for (int k = 0; k < __instance.upgradeGroups[j].upgradesList.Length; k++)
                    {
                        if (UpgradesData.instance.skillsTiers[__instance.upgradeGroups[j].upgradesList[k].perkInfo.upgradeIndex] < __instance.upgradeGroups[j].upgradesList[k].perkInfo.costList.Length)
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if (flag)
                {
                    SteamIntegration.instance.UnlockAchievement("Maxed out", 38);
                    return false;
                }
            }
            else
            {
                AudioManager2.instance.PlaySound("Cancel");
            }
            return false;
        }

        // read patches

        [HarmonyPatch(nameof(UI_CreditShopManager.CanBuyAnyUpgrade))]
        [HarmonyPostfix]
        public static void CanBuyAnyUpgrade_Postfix(UI_CreditShopManager __instance, ref bool __result)
        {
            for (int i = 0; i < __instance.upgradeGroups.Length; i++)
            {
                UI_CreditShopManager.SkillPerk[] upgradesList = __instance.upgradeGroups[i].upgradesList;
                for (int j = 0; j < upgradesList.Length; j++)
                {
                    UpgradeInfo perkInfo = upgradesList[j].perkInfo;
                    if (UpgradesData.instance.skillsUnlocked[perkInfo.upgradeIndex] && UpgradesData.instance.skillsTiers[perkInfo.upgradeIndex] < perkInfo.costList.Length)
                    {
                        int num = perkInfo.costList[UpgradesData.instance.skillsTiers[perkInfo.upgradeIndex]];
                        if (BigCreditsManager.Instance.credits >= num)
                        {
                            __result = true;
                            return;
                        }
                    }
                }
            }
            __result = false;
        }
        
        [HarmonyPatch("UpdateUpgradesPoints")]
        [HarmonyPrefix]
        public static bool UpdateUpgradesPoints_Prefix(UI_CreditShopManager __instance)
        {
            if (!UpgradesData.instance.skillsUnlocked[__instance.upgradeInfo.upgradeIndex])
            {
                for (int i = 0; i < __instance.pointsArray.Length; i++)
                {
                    __instance.pointsArray[i].SetActive(false);
                }
                __instance.priceText.gameObject.SetActive(false);
                return false;
            }
            if (UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex] < __instance.upgradeInfo.costList.Length)
            {
                __instance.priceText.gameObject.SetActive(true);
                TMP_Text tmp_Text = __instance.priceText;
                int value = __instance.upgradeInfo.costList[UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex]];
                int num = 3;
                int num2 = 10000;
                bool flag = true;
                tmp_Text.text = ShortScale.ParseInt(value, num, num2, flag);
                int num3 = __instance.upgradeInfo.costList[UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex]];
                __instance.priceText.color = ((BigCreditsManager.Instance.credits >= num3) ? Color.white : Color.grey);
            }
            else
            {
                __instance.priceText.gameObject.SetActive(false);
            }
            for (int j = 0; j < __instance.pointsArray.Length; j++)
            {
                __instance.pointsArray[j].SetActive(false);
                __instance.pointsArray[j].transform.GetChild(1).gameObject.SetActive(false);
            }
            for (int k = 0; k < __instance.upgradeInfo.costList.Length; k++)
            {
                __instance.pointsArray[k].SetActive(true);
            }
            for (int l = 0; l < UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex]; l++)
            {
                __instance.pointsArray[l].transform.GetChild(1).gameObject.SetActive(true);
                __instance.pointsArray[l].transform.GetChild(1).GetComponent<Image>().color = ((!UpgradesData.instance.blockedSkillsList.Contains(__instance.upgradeInfo.upgradeIndex)) ? Color.white : Color.grey);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(UI_HotbarSlot))]
    internal class UI_HotbarSlotPatches
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool Update_Prefix(UI_HotbarSlot __instance, ref float ___timeLeftClick, ref float ___timeRightClick)
        {
            switch (__instance.slotState)
            {
                case UI_HotbarSlot.State.Normal:
                    if (__instance.cardMoveTrans.localPosition != Vector3.zero)
                    {
                        __instance.iconMoveTrans.localPosition = Vector3.Lerp(__instance.iconMoveTrans.localPosition, new Vector3(0f, 0f, 0f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localPosition = Vector3.Lerp(__instance.cardMoveTrans.localPosition, Vector3.zero, Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localRotation = Quaternion.Lerp(__instance.cardMoveTrans.localRotation, Quaternion.Euler(0f, 0f, -10f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localScale = Vector3.Lerp(__instance.cardMoveTrans.localScale, new Vector3(1f, 1f, 1f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardRectTransform.sizeDelta = Vector2.Lerp(__instance.cardRectTransform.sizeDelta, new Vector3(80f, 80f), Time.unscaledDeltaTime * 5f);
                    }
                    break;
                case UI_HotbarSlot.State.Preview:
                    if (__instance.cardMoveTrans.localRotation != Quaternion.identity)
                    {
                        __instance.iconMoveTrans.localPosition = Vector3.Lerp(__instance.iconMoveTrans.localPosition, new Vector3(0f, 0f, 0f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localPosition = Vector3.Lerp(__instance.cardMoveTrans.localPosition, new Vector3(0f, 10f, 0f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localRotation = Quaternion.Lerp(__instance.cardMoveTrans.localRotation, Quaternion.identity, Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localScale = Vector3.Lerp(__instance.cardMoveTrans.localScale, new Vector3(1.4f, 1.4f, 1f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardRectTransform.sizeDelta = Vector2.Lerp(__instance.cardRectTransform.sizeDelta, __instance.tooltipSize, Time.unscaledDeltaTime * 5f);
                    }
                    if (PlayerGarden.instance.inMenu == -1)
                    {
                        if (___timeLeftClick <= 0f && CharacterInput.instance.GetKeyState_Pressed("Click"))
                        {
                            if (PlayerGardenInventory.instance.itemInHand == null)
                            {
                                bool flag = __instance.itemInfo.weaponCreditsOnUse == 0 || BigCreditsManager.Instance.credits >= __instance.itemInfo.weaponCreditsOnUse;
                                bool flag2 = __instance.itemInfo.weaponAmmoItem == null || InventoryManager.instance.itemsInInv[__instance.itemInfo.weaponAmmoItem.itemID] >= __instance.itemInfo.weaponAmmoQuantity;
                                bool flag3 = __instance.itemInfo.weaponUseStamina == 0 || PlayerGarden.instance.statCurrStamina >= (float)__instance.itemInfo.weaponUseStamina;
                                if (flag && flag2 && flag3)
                                {
                                    if (__instance.itemInfo.weaponCreditsOnUse != 0)
                                    {
                                        BigCreditsManager.Instance.RemoveCredits(__instance.itemInfo.weaponCreditsOnUse);
                                    }
                                    if (__instance.itemInfo.weaponAmmoItem != null && UnityEngine.Random.value <= 1f - ArtifactsManager.instance.GetArtifactEfficiency(10))
                                    {
                                        InventoryManager.instance.RemoveItemFromInv(__instance.itemInfo.weaponAmmoItem, __instance.itemInfo.weaponAmmoQuantity, true);
                                    }
                                    if (__instance.itemInfo.weaponUseStamina != 0)
                                    {
                                        PlayerGarden.instance.AddStat_Stamina((float)(-(float)__instance.itemInfo.weaponUseStamina));
                                    }
                                    __instance.itemInfo.onLeftClickEvent.Invoke();
                                    ___timeLeftClick = __instance.timeToAction;
                                }
                            }
                        }
                        else if (Input.GetButton("Right Click"))
                        {
                            if (PlayerGardenInventory.instance.itemInHand == null)
                            {
                                ___timeRightClick += Time.deltaTime;
                                if (___timeRightClick >= __instance.timeToAction)
                                {
                                    ___timeRightClick = 0f;
                                    __instance.itemInfo.onRightClickEvent.Invoke();
                                }
                            }
                        }
                        else if (Input.GetButtonUp("Right Click"))
                        {
                            ___timeRightClick = 0f;
                        }
                        __instance.rightClickBar.fillAmount = ___timeRightClick / __instance.timeToAction;
                    }
                    break;
                case UI_HotbarSlot.State.Hover:
                    if (__instance.cardMoveTrans.localRotation != Quaternion.identity)
                    {
                        __instance.iconMoveTrans.localPosition = Vector3.Lerp(__instance.iconMoveTrans.localPosition, new Vector3(0f, 0f, 0f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localPosition = Vector3.Lerp(__instance.cardMoveTrans.localPosition, new Vector3(0f, 10f, 0f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localRotation = Quaternion.Lerp(__instance.cardMoveTrans.localRotation, Quaternion.identity, Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localScale = Vector3.Lerp(__instance.cardMoveTrans.localScale, new Vector3(1.2f, 1.2f, 1f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardRectTransform.sizeDelta = Vector2.Lerp(__instance.cardRectTransform.sizeDelta, new Vector3(100f, 80f), Time.unscaledDeltaTime * 5f);
                    }
                    break;
                case UI_HotbarSlot.State.Holding:
                    {
                        Vector3 mousePosition = Input.mousePosition;
                        Vector3 b = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0.1f));
                        __instance.iconMoveTrans.position = Vector3.Lerp(__instance.iconMoveTrans.position, b, Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localPosition = Vector3.Lerp(__instance.cardMoveTrans.localPosition, new Vector3(0f, 10f, 0f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localRotation = Quaternion.Lerp(__instance.cardMoveTrans.localRotation, Quaternion.identity, Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardMoveTrans.localScale = Vector3.Lerp(__instance.cardMoveTrans.localScale, new Vector3(1.4f, 1.4f, 1f), Time.unscaledDeltaTime * __instance.animationSpeed);
                        __instance.cardRectTransform.sizeDelta = Vector2.Lerp(__instance.cardRectTransform.sizeDelta, __instance.tooltipSize, Time.unscaledDeltaTime * 5f);
                        break;
                    }
            }
            if (___timeLeftClick >= 0f)
            {
                ___timeLeftClick -= Time.deltaTime;
                __instance.leftClickBar.fillAmount = ___timeLeftClick / __instance.timeToAction;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(UI_TradePostManager))]
    internal class UI_TradePostManagerPatches
    {
        [HarmonyPatch(nameof(UI_TradePostManager.ResetTrades))]
        [HarmonyPrefix]
        public static bool ResetTrades_Prefix(UI_TradePostManager __instance)
        {
            Type type = typeof(UI_TradePostManager);
            MethodInfo method = AccessTools.Method("GetPriceReset");

            int priceReset = 0;
            if (method != null)
            {
                priceReset = (int)method.Invoke(__instance, null);
            }
            if (BigCreditsManager.Instance.credits >= priceReset)
            {
                AudioManager2.instance.PlaySound("Build_TradePost_BuyReset");
                BigCreditsManager.Instance.RemoveCredits(priceReset);
                __instance.build_tradePost.ResetTrades();
                if (priceReset >= 10000)
                {
                    SteamIntegration.instance.UnlockAchievement("Market Resurrector", 77);
                    return false;
                }
            }
            else
            {
                AudioManager2.instance.PlaySound("Cancel");
            }
            return false;
        }
    }

    // credits read patches

    [HarmonyPatch(typeof(ResearchManager))]
    internal class ResearchManagerPatches
    {
        [HarmonyPatch("CheckCanResearch")]
        [HarmonyPrefix]
        public static bool CheckCanResearch_Prefix(ResearchManager __instance, ref int ___currRecipeIndex)
        {
            if (BigCreditsManager.Instance.credits < __instance.buildResearch.craftRecipesList[___currRecipeIndex].recipesInfo[0].creditsToResearch)
            {
                __instance.craftItemButton.interactable = false;
                return false;
            }
            __instance.craftItemButton.interactable = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(UI_ShopSlot))]
    internal class UI_ShopSlotPatches
    {
        [HarmonyPatch(nameof(UI_ShopSlot.UpdateBuyBGIcon))]
        [HarmonyPrefix]
        public static bool UpdateBuyBGIcon_Prefix(UI_ShopSlot __instance)
        {
            if (__instance.upgradeInfo == null)
            {
                if (__instance.slotImageBG != null)
                {
                    __instance.slotImageBG.sprite = __instance.slotBGIcon_empty;
                }
                return false;
            }
            if (UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex] < __instance.upgradeInfo.costList.Length)
            {
                int num = __instance.upgradeInfo.costList[UpgradesData.instance.skillsTiers[__instance.upgradeInfo.upgradeIndex]];
                if (BigCreditsManager.Instance.credits >= num)
                {
                    if (__instance.slotImageBG != null)
                    {
                        __instance.slotImageBG.sprite = __instance.slotBGIcon_canBuy;
                        return false;
                    }
                }
                else if (__instance.slotImageBG != null)
                {
                    __instance.slotImageBG.sprite = __instance.slotBGIcon_cantBuy;
                    return false;
                }
            }
            else if (__instance.slotImageBG != null)
            {
                __instance.slotImageBG.sprite = __instance.slotBGIcon_completed;
            }
            return false;
        }
    }

    // save patches

    [HarmonyPatch(typeof(SaveDataGarden))]
    internal class SaveDataGarden_Patches
    {
        [HarmonyPatch("DelaySetup")]
        [HarmonyPrefix]
        public static bool DelaySetup_Patches(SaveDataGarden __instance)
        {
            if (__instance.loadThings)
            {
                __instance.saveSlotIndex = LoadDataToGame.saveSlotIndex;
                try
                {
                    __instance.LoadData(__instance.saveSlotIndex);
                    goto IL_61;
                }
                catch (Exception ex)
                {
                    __instance.CreateFailedText(1, ex.ToString(), __instance.saveSlotIndex);
                    AudioManager2.instance.PlaySound("Cancel");
                    Process.Start(Application.persistentDataPath);
                    SceneManager.LoadScene("Scene_MainMenu");
                    goto IL_61;
                }
            }

            Type type = typeof(SaveDataGarden);
            MethodInfo newIsleMethod = AccessTools.Method("NewIsle");

            newIsleMethod.Invoke(__instance, null);
        IL_61:

            MethodInfo loadLastMethod = AccessTools.Method("LoadLastExitTime");
            MethodInfo CalculateAccumulateMethod = AccessTools.Method("CalculateAccumulatedPoints");

            loadLastMethod.Invoke(__instance, null);
            CalculateAccumulateMethod.Invoke(__instance, null);
            __instance.Invoke("AddLastLetter", 3f);
            if (ArtifactsManager.instance.GetArtifactEfficiency(4) >= 0f)
            {
                BigCreditsManager.Instance.credits += __instance.accumulatedPoints;
                BigCreditsManager.Instance.UpdateCreditsText();
                if (__instance.accumulatedPoints != 0)
                {
                    __instance.Invoke("AlertPlayerAboutCredits", 5f);
                }
            }
            MethodInfo saveCurrMethod = AccessTools.Method("SaveCurrentTime");
            saveCurrMethod.Invoke(__instance, null);

            return false;
        }

    }

}