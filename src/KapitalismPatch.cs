using HarmonyLib;
using I2.Loc.SimpleJSON;
using KSP.Assets;
using KSP.Game;
using KSP.Game.Flow;
using KSP.Game.Missions;
using KSP.Game.Missions.Definitions;
using KSP.Game.Science;
using KSP.Modules;
using KSP.OAB;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using KSP.Sim.ResourceSystem;
using KSP.UI;
using KSP.UI.Binding.Core;
using Newtonsoft.Json;
using ReLIB;
using Shapes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Logger = ReLIB.logging.Logger;

namespace Kapitalism;

public class KapitalismPatch
{
    private static Logger logger = new Logger(K.ModName, K.ModVersion);
    
    [HarmonyPatch(typeof(SaveLoadManager))]
    [HarmonyPatch("StartLoadOrSaveOperation")]
    [HarmonyPostfix]
    public static void SaveLoadManager_StartLoadOrSaveOperation(SaveLoadManager __instance,
        ref LoadOrSaveCampaignTicket loadOrSaveCampaignTicket)
    {
        try
        {
            logger.Log(__instance.ActiveCampaignFolderPath);
            logger.Log(loadOrSaveCampaignTicket._loadFileName);
            logger.Log(loadOrSaveCampaignTicket._saveFileName);
            logger.Log(GameManager.Instance.Game.SessionManager.ActiveCampaignName);
            if (loadOrSaveCampaignTicket._saveFileName.Length > 0)
            {
                K.Filename =
                    loadOrSaveCampaignTicket._saveFileName.Split("\\")[
                        loadOrSaveCampaignTicket._saveFileName.Split("\\").Length - 1];
            }

            if (loadOrSaveCampaignTicket._loadFileName.Length > 0)
            {
                K.Filename =
                    loadOrSaveCampaignTicket._loadFileName.Split("\\")[
                        loadOrSaveCampaignTicket._loadFileName.Split("\\").Length - 1];
            }

            string SaveLocation =
                $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism/{K.Filename}"; //fix file name issue for loadsave
            string SaveLocationM =
                $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/missions";
            logger.Log(SaveLocation);
            if (!Directory.Exists($"./ModSaveData"))
            {
                Directory.CreateDirectory($"./ModSaveData");
            }

            if (!Directory.Exists($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}"))
            {
                Directory.CreateDirectory(
                    $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}");
            }

            if (!Directory.Exists(
                    $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism"))
            {
                Directory.CreateDirectory(
                    $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism");
            }

            void LoadSave()
            {
                if (File.Exists(SaveLocation))
                {
                    K.saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SaveLocation));

                    K.UpdateDisplay();
                }
                else
                {


                    File.WriteAllText(SaveLocation, JsonConvert.SerializeObject(K.saveData));
                    K.UpdateDisplay();
                }
            }

            void DeleteSave()
            {
                try
                {
                    File.Delete(SaveLocation);
                    File.Delete(SaveLocationM);
                    K.UpdateDisplay();
                }
                catch (Exception e)
                {

                }


            }

            void SaveSave()
            {
                File.WriteAllText(SaveLocation, JsonConvert.SerializeObject(K.saveData));
                K.UpdateDisplay();
            }

            void SaveToBuffer()
            {
                K.BuffersaveData = JsonConvert.SerializeObject(K.saveData);
                K.UpdateDisplay();
            }

            void LoadFromBuffer()
            {
                if (K.LaunchClickBuffersaveData.Length > 0)
                {
                    K.saveData = JsonConvert.DeserializeObject<SaveData>(K.LaunchClickBuffersaveData);
                    K.LaunchClickBuffersaveData = "";
                }
                else
                {
                    K.saveData = JsonConvert.DeserializeObject<SaveData>(K.BuffersaveData);
                    K.BuffersaveData = "";
                }

                K.UpdateDisplay();
            }

            logger.Debug($"{loadOrSaveCampaignTicket._loadOrSaveCampaignOperation}");
            switch (loadOrSaveCampaignTicket._loadOrSaveCampaignOperation)
            {
                case LoadOrSaveCampaignOperation.None: break;
                case LoadOrSaveCampaignOperation.Load_StartNewCampaign:
                    K.saveData.SelectAdmin = true;
                    LoadSave();
                    break;
                case LoadOrSaveCampaignOperation.Load_StartExistingCampaignFromJsonString:
                    LoadSave();
                    break;
                case LoadOrSaveCampaignOperation.Load_LoadGameFromAsset:
                    LoadSave();
                    break;
                case LoadOrSaveCampaignOperation.Load_LoadGameFromAddressable:
                    LoadSave();
                    break;
                case LoadOrSaveCampaignOperation.Load_LoadGameFromFile:
                    LoadSave();
                    break;
                case LoadOrSaveCampaignOperation.Load_LoadGameFromBuffer:
                    LoadFromBuffer();
                    break;
                case LoadOrSaveCampaignOperation.Save_SaveGameToFile:
                    SaveSave();
                    break;
                case LoadOrSaveCampaignOperation.Save_SaveGameToMemory:
                    SaveToBuffer();
                    break;
                case LoadOrSaveCampaignOperation.Save_SaveSpecificPlayerToFile:
                    SaveSave();
                    break;
                case LoadOrSaveCampaignOperation.Save_SaveSpecificPlayerToMemory:
                    SaveSave();
                    break;
                case LoadOrSaveCampaignOperation.DeleteSaveFile:
                    DeleteSave();
                    break;
                case LoadOrSaveCampaignOperation.DeleteDirectory:
                    DeleteSave();
                    break;
                default: break;
            }
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }

    [HarmonyPatch(typeof(PartProvider))]
    [HarmonyPatch("AddPartData")]
    [HarmonyPrefix]
    public static bool PartProvider_AddPartData(PartProvider __instance, ref PartCore jsonData, ref string rawJson)
    {
       // File.WriteAllText($"./partdata/{jsonData.data.partName}", JsonConvert.SerializeObject(jsonData));
       
        string name = jsonData.data.partName;
        logger.Log("Loading part: " + name);
        try
        {
            KPartData tpart = K.PartCostData.First((part) => part.partName == name);
            if (tpart.partName == jsonData.data.partName)
            {
                jsonData.data.cost = tpart.cost;
            }
        }
        catch (Exception e)
        {
            //logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

        }

        return true;
    }
    
    [HarmonyPatch(typeof(PartInfoOverlay))]
    [HarmonyPatch("PopulateCoreInfoFromPart")]
    [HarmonyPostfix]
    public static void PartInfoOverlay_PopulateCoreInfoFromPart(ref List<KeyValuePair<string, string>> __result,
        PartInfoOverlay __instance, ref IObjectAssemblyAvailablePart IOBAPart)
    {
        try
        {
            __result.Add(new KeyValuePair<string, string>("Cost", $"£{IOBAPart.PartData.cost}"));
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

        }
    }

    [HarmonyPatch(typeof(ObjectAssemblyBuilder.ObjectAssemblyBuilderEventsManager))]
    [HarmonyPatch("IsClearedForLaunch")]
    [HarmonyPostfix]
    public static void ObjectAssemblyBuilderEventsManager_IsClearedForLaunch(ref bool __result,
        ObjectAssemblyBuilder.ObjectAssemblyBuilderEventsManager __instance)
    {
        try
        {
            float totalCost = 0;
            float resourceCost = 0;
            Dictionary<string, float> UsedResources = new Dictionary<string, float>();
            ResourceDefinitionDatabase rdd = GameManager.Instance._game.ResourceDefinitionDatabase;


            if (__instance.builder.Stats.HasMainAssembly)
            {
                __instance.builder.Stats.MainAssembly.Parts.ForEach(part =>
                {

                    totalCost += GameManager.Instance.Game.Parts._partData[part.PartName].data.cost;
                    try
                    {
                        ((Module_ResourceCapacities)part.Modules[typeof(PartComponentModule_ResourceCapacities)])
                            .dataResourceCapacities._propertyContextLookup.ForEach(rd =>
                            {
                                if (rdd.IsResourceRecipe(rdd.GetResourceIDFromName(rd.Key)))
                                {
                                    List<ResourceUnitsPair> unitsOfIngredients = new List<ResourceUnitsPair>();
                                    rdd.GetRecipeIngredientUnits(rdd.GetResourceIDFromName(rd.Key),
                                        ((ModuleProperty<float>)rd.Value.properties["EntryProperty"]).storedValue,
                                        ref unitsOfIngredients);
                                    unitsOfIngredients.ForEach(RUP =>
                                    {
                                        string trd = rdd.GetResourceNameFromID(RUP.resourceID);
                                        if (!UsedResources.ContainsKey(trd))
                                        {
                                            UsedResources.Add(trd, 0f);
                                        }

                                        UsedResources[trd] += (float)RUP.units;
                                    });
                                }
                                else
                                {
                                    if (!UsedResources.ContainsKey(rd.Key))
                                    {
                                        UsedResources.Add(rd.Key, 0f);
                                    }

                                    UsedResources[rd.Key] +=
                                        ((ModuleProperty<float>)rd.Value.properties["EntryProperty"]).storedValue;
                                }

                            });
                    }
                    catch (Exception e)
                    {
                        logger.Error(
                            $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                    }
                });
                UsedResources.ForEach(kv =>
                {

                    if (K.saveData.MaterialStorage[kv.Key] < kv.Value)
                    {
                        float amountToCalc = kv.Value - K.saveData.MaterialStorage[kv.Key];
                        ResourceDefinitionDatabase.ResourceDefinitionWrapper ResourceDef =
                            rdd._resourceDefinitionWrappers.Find(x =>
                                x.resourceID == rdd.GetResourceIDFromName(kv.Key));
                        resourceCost = amountToCalc *
                                       ((float)ResourceDef.originalResourceDefinition.Value.costPerUnit * 1000);
                    }
                });
            }

            K.LaunchClickBuffersaveData = JsonConvert.SerializeObject(K.saveData);
            if (K.UseFunding(totalCost + resourceCost))
            {
                K.UseResources(UsedResources);
                __result = true;
            }
            else
            {
                Utils.MessageUser("Requires more funding");
                __result = false;
            }
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            __result = false;
            Utils.MessageUser("Kapitalism error");
        }




        try
        {

        }
        catch (Exception e)
        {

        }
    }

    [HarmonyPatch(typeof(ScienceManager))]
    [HarmonyPatch("TrySubmitCompletedResearchReport")]
    [HarmonyPostfix]
    public static void ScienceManager_TrySubmitCompletedResearchReport(ref bool __result, ScienceManager __instance,
        ref CompletedResearchReport report)
    {
        try
        {
            float Funds = report.FinalScienceValue * (K.saveData.ScienceModifier * 76);
            K.UpdateFunds(Funds);
            K.UpdateDisplay();
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            __result = false;
            Utils.MessageUser("Kapitalism error");
        }

    }

    [HarmonyPatch(typeof(VesselComponent))]
    [HarmonyPatch("RecoverVessel")]
    [HarmonyPrefix]
    public static bool VesselComponent_RecoverVessel(VesselComponent __instance, ref IGGuid recoveryLocation)
    {
        try
        {
            ResourceDefinitionDatabase rdd = GameManager.Instance._game.ResourceDefinitionDatabase;
            float Funds = 0;
            KSP.Sim.Position KSC = GameManager.Instance.Game.UniverseModel.SimulationObjects
                .First(x => x.DebugName == "kerbin_KSC_Object").Position;
            Double DistanceToKSC = KSP.Sim.Position.Distance(KSC, __instance.SimulationObject.Position);
            DistanceToKSC = Math.Floor(DistanceToKSC / 1000);
            if (DistanceToKSC < 10)
            {
                DistanceToKSC = 10;
            }

            if (DistanceToKSC > 100)
            {
                Funds = -(Funds / 2);
            }
            else
            {
                Funds /= ((float)DistanceToKSC / 10);
                try
                {

                    __instance.SimulationObject.objVesselBehavior.PartOwner.Parts.ForEach(part =>
                    {
                        Funds += GameManager.Instance.Game.Parts._partData[part.Name].data.cost;
                        logger.Debug($"KeysList {part.Modules.KeysList.Join(x => x.Name, ",")}");
                        logger.Debug($"ValuesList {part.Modules.ValuesList.Join(x => x.GetType().Name, ",")}");
                        try
                        {
                            part.Model.PartResourceContainer.GetAllResourcesContainedData().ForEach(rd =>
                            {
                                try
                                {
                                    if (rdd.IsResourceRecipe(rd.ResourceID))
                                    {
                                        List<ResourceUnitsPair> unitsOfIngredients = new List<ResourceUnitsPair>();
                                        rdd.GetRecipeIngredientUnits(rd.ResourceID, rd.StoredUnits,
                                            ref unitsOfIngredients);
                                        unitsOfIngredients.ForEach(RUP =>
                                        {
                                            string trd = rdd.GetResourceNameFromID(RUP.resourceID);
                                            if (!K.saveData.MaterialStorage.ContainsKey(trd))
                                            {
                                                K.saveData.MaterialStorage.Add(trd, 0);
                                            }

                                            K.saveData.MaterialStorage[trd] +=
                                                (float)RUP.units / ((float)DistanceToKSC / 10);
                                        });
                                    }
                                    else
                                    {

                                        if (!K.saveData.MaterialStorage.ContainsKey(
                                                rdd.GetResourceNameFromID(rd.ResourceID)))
                                        {
                                            K.saveData.MaterialStorage.Add(rdd.GetResourceNameFromID(rd.ResourceID), 0);
                                        }

                                        K.saveData.MaterialStorage[rdd.GetResourceNameFromID(rd.ResourceID)] +=
                                            (float)rd.StoredUnits / ((float)DistanceToKSC / 10);
                                    }
                                }
                                catch (Exception e)
                                {
                                    logger.Error(
                                        $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                                }

                            });
                        }
                        catch (Exception e)
                        {

                        }
                    });
                }
                catch (Exception e)
                {
                    logger.Error(
                        $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                    Utils.MessageUser("Kapitalism error");
                }
            }


            logger.Debug($"KSC Distance: {DistanceToKSC} ");
            K.UpdateFunds(Funds, "KSC recovery reward");
            K.UpdateDisplay();


        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            Utils.MessageUser("Kapitalism error");
        }

        return true;

    }
    [HarmonyPatch(typeof(UIValue_ReadEnum_GraphicSet))]
    [HarmonyPatch("OnEnable")]
    [HarmonyPrefix]
    public static bool KSP2MissionManager_OnEnable(UIValue_ReadEnum_GraphicSet __instance)
    {
        try
        {
            logger.Debug($"pound enable");
            var gv = __instance.graphicValues.ToList();
            gv.Add(new UIValue_ReadEnum_GraphicSet.GraphicEntry()
            {
                color = Color.white,
                sprite = Sprite.Create(AssetManager.GetAsset("pound.png"),new Rect(0f, 0f, 64f, 64f),new Vector2(0f, 0f)),
                enumValue = "pound"
            });
            __instance.graphicValues = gv.ToArray();
            __instance.PopulateValueMap();
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
        return true;
    }
    
    [HarmonyPatch(typeof(KSP2MissionManager))]
    [HarmonyPatch("OnMissionDataItemLoaded")]
    [HarmonyPrefix]
    public static bool KSP2MissionManager_OnMissionDataItemLoaded(KSP2MissionManager __instance, TextAsset textAsset)
    {
        var missionData = JsonConvert.DeserializeObject<MissionData>(textAsset.text);
        GameManager.Instance.Game.UI.SetLoadingBarText($"Updating Mission {missionData.ID} with Kaptialism");
        try
        {
            var CompleteStage = missionData.missionStages.Find(stage => stage.MissionReward.MissionRewardDefinitions.Count > 0 );
            CompleteStage.MissionReward.MissionRewardDefinitions.Add(new MissionRewardDefinition()
            {
                MissionRewardType = (MissionRewardType)Enum.Parse(typeof(MissionRewardType), "Funds"),
                RewardAmount = (CompleteStage.MissionReward.MissionRewardDefinitions[0].RewardAmount * 112.5f) * K.saveData.DificultyModifier["fund"],
                RewardKey = "pound"
            });
            CompleteStage.MissionReward.MissionRewardDefinitions.Add(new MissionRewardDefinition()
            {
                MissionRewardType = (MissionRewardType)Enum.Parse(typeof(MissionRewardType), "Budget"),
                RewardAmount = (CompleteStage.MissionReward.MissionRewardDefinitions[0].RewardAmount / 100f) * K.saveData.DificultyModifier["budget"],
                RewardKey = "pound"
            });
            CompleteStage.MissionReward.MissionRewardDefinitions[0].RewardAmount *= K.saveData.DificultyModifier["science"];
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
        var flag = !string.IsNullOrEmpty(missionData.GameModeFeatureId) && !GameManager.Instance.GameModeManager.IsGameModeFeatureEnabled(missionData.GameModeFeatureId);
        if (!flag)
            SaveLoadMissionUtils.AddOrOverwriteMissionData(__instance.Game, __instance._missionDefinitions, missionData);
        __instance.Game.KeepAliveNetworkPump();
        return false;
    }
    [HarmonyPatch(typeof(PopulateResourceDefinitionDatabaseFlowAction))]
    [HarmonyPatch("OnResourceDataLoaded")]
    [HarmonyPrefix]
    public static bool PopulateResourceDefinitionDatabaseFlowAction_OnResourceDataLoaded(
        PopulateResourceDefinitionDatabaseFlowAction __instance, TextAsset asset)
    {
        GameManager.Instance.Game.UI.SetLoadingBarText("Loading Kapitalism resourceData");
        File.WriteAllText($"./{JsonConvert.DeserializeObject<ResourceCore>(asset.text).data.name}.json", asset.text);
        return true;
    }

    public static float[] GetKapitalismMissionData(MissionData missionData)
    {
        float[] Rewards = { 0, 0 };
        foreach (MissionStage missionStage in missionData.missionStages)
        {
            foreach (MissionRewardDefinition rewardDefinition in missionStage.MissionReward.MissionRewardDefinitions)
            {
                logger.Log(
                    $"{rewardDefinition.MissionRewardType} {rewardDefinition.MissionRewardType.Equals(Enum.Parse(typeof(MissionRewardType), "Budget"))}");
                if (rewardDefinition.MissionRewardType.Equals(Enum.Parse(typeof(MissionRewardType), "Budget")))
                {
                    Rewards[0] += rewardDefinition.RewardAmount;
                }

                if (rewardDefinition.MissionRewardType.Equals(Enum.Parse(typeof(MissionRewardType), "Funds")))
                {
                    Rewards[1] += rewardDefinition.RewardAmount;
                }
            }
        }

        return Rewards;
    }

    [HarmonyPatch(typeof(ScienceManager))]
    [HarmonyPatch("UpdateSciencePointCapacity")]
    [HarmonyPostfix]
    public static void ScienceManager_UpdateSciencePointCapacity(ScienceManager __instance)
    {
        try
        {
            float[] rewards = { 0, 0 };
            SaveLoadMissionUtils.TryGetMissionSaveDatas(__instance._currentGame, out List<MissionSaveData> _,
                out List<MissionSaveData> agencyMissionSaveData);
            logger.Log($"{agencyMissionSaveData.Count}");

            foreach (MissionData missionDefinition in
                     __instance._currentGame.KSP2MissionManager.GetMissionDefinitions())
            {
                logger.Log("GetMissionDefinitions");
                ProcessMissionDefinition(missionDefinition, agencyMissionSaveData, rewards);
            }

            if (rewards[1] > 0.1f)
            {
                K.UpdateFunds(rewards[1],"Science Fund Reward");
            }
            if (rewards[0] > 0.1f)
            {
                K.UpdateBudgetModifier(rewards[0]);
            }
        }
        catch (Exception e)
        {
            logger.Error(
                $"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }

    public static void ProcessMissionDefinition(MissionData missionDefinition,
        List<MissionSaveData> agencyMissionSaveData, float[] rewards)
    {
        MissionSaveData missionSaveData;
        if (missionDefinition.Owner == MissionOwner.Agency &&
            SaveLoadMissionUtils.TryGetMissionSaveData(agencyMissionSaveData, missionDefinition.ID,
                out missionSaveData) &&
            missionSaveData.TurnedIn &&
            !K.saveData.DoneMissions.Contains(missionDefinition.ID))
        {
            logger.Log("GetKapitalismMissionData " + missionSaveData.Completed);
            float[] temporaryRewards = GetKapitalismMissionData(missionDefinition);
            rewards[0] += temporaryRewards[0];
            rewards[1] += temporaryRewards[1];
            logger.Log($"Rewards[0] {rewards[0]} | Rewards[1] {rewards[1]}");
            K.saveData.DoneMissions.Add(missionDefinition.ID);
        }
    }
}