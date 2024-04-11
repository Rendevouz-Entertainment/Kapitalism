using BepInEx;
using ShadowUtilityLIB;
using ShadowUtilityLIB.UI;
using Logger = ShadowUtilityLIB.logging.Logger;
using HarmonyLib;
using Newtonsoft.Json;
using KSP.Game;
using UnityEngine;
using TMPro;
using KSP.Sim.Definitions;
using UnityEngine.UIElements;
using UitkForKsp2.API;
using KSP.UI;
using KSP.OAB;
using KSP;
using static KSP.OAB.ObjectAssemblyBuilder;
using KSP.Game.Missions;
using KSP.Game.Load;
using KSP.Game.Missions.Definitions;
using KSP.IO;
using UnityEngine.TextCore.Text;
using TextAsset = UnityEngine.TextAsset;
using Shapes;
using KSP.Messages;
using KSP.Game.Science;
using System;
using KSP.Sim.impl;
using System.Runtime.CompilerServices;

namespace Kapitalism;
[BepInPlugin("com.shadowdev.kapitalism", "Kapitalism", "0.0.1")]
[BepInDependency(ShadowUtilityLIBMod.ModId, ShadowUtilityLIBMod.ModVersion)]
public class KapitalismMod : BaseUnityPlugin
{
    public static string ModId = K.ModId;
    public static string ModName = K.ModName;
    public static string ModVersion = K.ModVersion;

    private Logger logger = new Logger(ModName, ModVersion);//logger logger.log("stuff here")  logger.debug("only run with IsDev=true")  logger.error("error log")
    public static Manager manager;//ui manager

    void Start()
    {
        K.Start();
    }
    void Update()
    {
        K.Update();
    }
}
public class Kconfig
{
    public bool PartCostEditor = false;
}
public class KPartData
{
    public string partName;
    public int cost;

}
public enum AdministrationType
{
    Kapitalist,
    Materialist,
    Teknokratist,//thanks Safarte //updated name thanks Falki
    Anarkist//thanks Cheese
}
[Serializable]
public class SaveData
{
    public float Budget = 0;
    public float BudgetModifier = 1;
    public float Funds = 0;
    public bool SelectAdmin = true;
    public AdministrationType administrationType = AdministrationType.Kapitalist;
    public int CurrentYear = 0;
    public float DificultyScale = 1;
}

public static class K
{
    public static string ModId = "com.shadowdev.kapitalism";
    public static string ModName = "Kapitalism";
    public static string ModVersion = "0.0.1";
    private static Logger logger = new(ModName, ModVersion);
    public static Kconfig config = new();
    public static SaveData saveData = new();
    public static string BuffersaveData = "";
    public static string LaunchClickBuffersaveData = "";
    public static PanelSettings UIPanelSettings;

    public static List<GameObject> ValuesToUpdate = new List<GameObject>();
    public static List<KPartData> PartCostData = new List<KPartData>() { new KPartData() { cost = 10, partName = "test"} };

    public static UIDocument administrationPickerPopupWindow;


    public static bool justClicked = false;

    public static List<float> AdministrationBudgetMultiplier = new List<float>()
    {
        5,
        3,
        3,
        0.5f,
    };
    public static List<float> AdministrationMaterialMultiplier = new List<float>()
    {
        1,
        1.5f,
        0.75f,
        0.5f,
    };
    public static List<float> AdministrationScienceMultiplier = new List<float>()
    {
        1,
        0.75f,
        1.5f,
        0.25f,
    };
    public static float defaultBudget = 1000;
    private static IObjectAssemblyPart assemblyPart;
    private static bool showPartMenuUI;

    public static UIDocument PartPriceSetterPopupPopupWindow { get; private set; }

    public static void Start()
    {
        ShadowUtilityLIBMod.EnableDebugMode();
        UIPanelSettings = Manager.PanelSettings;
        try
        {
            Harmony.CreateAndPatchAll(typeof(Kpatch));
            PartCostData = JsonConvert.DeserializeObject<List<KPartData>>(File.ReadAllText($"./BepInEx/plugins/Kapitalsim/PartData.json"));
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
        try
        {
            if (Directory.Exists("./Config")) { } else
            {
                Directory.CreateDirectory("./Config");
            }
            if(File.Exists("./Config/Kapitalism.config")){
                config = JsonConvert.DeserializeObject<Kconfig>(File.ReadAllText("./Config/Kapitalism.config"));
            }
            else
            {
                File.WriteAllText("./Config/Kapitalism.config", JsonConvert.SerializeObject(config));
            }
            
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            
        }
        try
        {
            AdministrationPickerPopup();
            PartPriceSetterPopup();
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

        }
    }
    public static void UpdateDisplay()
    {
        ValuesToUpdate.ForEach((FundsObject) =>
        {
            try
            {
                FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
            }
            catch (Exception e)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }
        });
    }
    public static void UpdateBudgetModifier(float value)
    {
        saveData.BudgetModifier += value;
        ValuesToUpdate.ForEach((FundsObject) =>
        {
            try
            {
                FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
            }
            catch (Exception e)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }
        });
    }
    public static void UpdateBudget()
    {
        saveData.Budget = defaultBudget * saveData.BudgetModifier;
        ValuesToUpdate.ForEach((FundsObject) =>
        {
            try
            {
                FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
            }
            catch (Exception e)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }
        });
    }
    public static void UpdateFunds(float value)
    {
        saveData.Funds += value;
        ValuesToUpdate.ForEach((FundsObject) =>
        {
            try
            {
                FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
            }
            catch (Exception e)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }
        });
    } 

    public static bool UseFunding(float value)
    {
        if(saveData.Funds + saveData.Budget >= value){
            if (saveData.Budget >= value)
            {
                saveData.Budget -= value;
            }
            else
            {
                value -= saveData.Budget;
                saveData.Budget = 0;
                saveData.Funds -= value;
            }
            ValuesToUpdate.ForEach((FundsObject) =>
            {
                try
                {
                    FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
                }
                catch (Exception e)
                {
                    logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                }
            });
            return true;
        }
        ValuesToUpdate.ForEach((FundsObject) =>
        {
            try
            {
                FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
            }
            catch (Exception e)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }
        });
        return false;
    }
    public static void AdministrationPickerPopup()
    {
        try
        {
            int Width = 1920;
            int Height = 1080;

            VisualElement Kapitalism_AdministrationPicker = Element.Root("Kapitalism_AdministrationPicker");
            IStyle style_Kapitalism_AdministrationPicker = Kapitalism_AdministrationPicker.style;
            style_Kapitalism_AdministrationPicker.width = Width;
            style_Kapitalism_AdministrationPicker.height = Height;
            style_Kapitalism_AdministrationPicker.backgroundImage = AssetManager.GetAsset("administrationbg.png");
            style_Kapitalism_AdministrationPicker.position = Position.Absolute;
            style_Kapitalism_AdministrationPicker.left = 0;
            style_Kapitalism_AdministrationPicker.top = 0;
            //margin
            style_Kapitalism_AdministrationPicker.marginBottom = 0;
            style_Kapitalism_AdministrationPicker.marginTop = 0;
            style_Kapitalism_AdministrationPicker.marginLeft = 0;
            style_Kapitalism_AdministrationPicker.marginRight = 0;
            //padding
            style_Kapitalism_AdministrationPicker.paddingBottom = 0;
            style_Kapitalism_AdministrationPicker.paddingTop = 0;
            style_Kapitalism_AdministrationPicker.paddingLeft = 0;
            style_Kapitalism_AdministrationPicker.paddingRight = 0;


            Label Kapitalism_AdministrationPickerTitle = Element.Label("AdministrationPicker", "Administration Picker");
            Kapitalism_AdministrationPicker.Add(Kapitalism_AdministrationPickerTitle);
            Kapitalism_AdministrationPickerTitle.style.position = Position.Absolute;
            Kapitalism_AdministrationPickerTitle.style.top = 10;
            Kapitalism_AdministrationPickerTitle.style.width = Width;
            Kapitalism_AdministrationPickerTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            Kapitalism_AdministrationPickerTitle.style.fontSize = 20;

            Label Kapitalism_AdministrationPickerSubtext = Element.Label("AdministrationPickerst", "Pick your administration type below, you cannot change this later so pick carefully");
            Kapitalism_AdministrationPicker.Add(Kapitalism_AdministrationPickerSubtext);
            Kapitalism_AdministrationPickerSubtext.style.position = Position.Absolute;
            Kapitalism_AdministrationPickerSubtext.style.top = 40;
            Kapitalism_AdministrationPickerSubtext.style.width = Width;
            Kapitalism_AdministrationPickerSubtext.style.unityTextAlign = TextAnchor.MiddleCenter;
            Kapitalism_AdministrationPickerSubtext.style.fontSize = 15;

            VisualElement Kapitalism_AdministrationPickerPopup = new VisualElement();
            Kapitalism_AdministrationPicker.Add(Kapitalism_AdministrationPickerPopup);
            IStyle style_Kapitalism_AdministrationPickerPopup = Kapitalism_AdministrationPickerPopup.style;
            style_Kapitalism_AdministrationPickerPopup.width = Width;
            style_Kapitalism_AdministrationPickerPopup.height = Height - 120;
            style_Kapitalism_AdministrationPickerPopup.backgroundImage = AssetManager.GetAsset("administrationbg.png");
            style_Kapitalism_AdministrationPickerPopup.display = DisplayStyle.Flex;
            style_Kapitalism_AdministrationPickerPopup.flexDirection = FlexDirection.Row;
            style_Kapitalism_AdministrationPickerPopup.position = Position.Absolute;
            style_Kapitalism_AdministrationPickerPopup.left = 0;
            style_Kapitalism_AdministrationPickerPopup.top = 120;

            //margin
            style_Kapitalism_AdministrationPickerPopup.marginBottom = 0;
            style_Kapitalism_AdministrationPickerPopup.marginTop = 0;
            style_Kapitalism_AdministrationPickerPopup.marginLeft = 0;
            style_Kapitalism_AdministrationPickerPopup.marginRight = 0;
            //padding
            style_Kapitalism_AdministrationPickerPopup.paddingBottom = 0;
            style_Kapitalism_AdministrationPickerPopup.paddingTop = 0;
            style_Kapitalism_AdministrationPickerPopup.paddingLeft = 50;
            style_Kapitalism_AdministrationPickerPopup.paddingRight = 0;

            void AddAdministrationType(AdministrationType administrationType)
            {
                float Gap = 20;
                float ATwidth = (Width / Enum.GetValues(typeof(AdministrationType)).Length) - (Gap * 2);
                VisualElement Kapitalism_AdministrationPickerPopup_AddAdministrationType = new VisualElement();
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.name = $"Kapitalism_AdministrationPickerPopup_AddAdministrationType_{administrationType}";
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.width = ATwidth;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.marginRight = Gap;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.marginLeft = Gap;
                
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.display = DisplayStyle.Flex;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.flexDirection = FlexDirection.Column;
                Label Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name = Element.Label($"Kapitalism_AdministrationPickerPopup_AddAdministrationType_{administrationType}_Name", $"{administrationType}");
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.marginTop = 350;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.width = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.unityTextAlign = TextAnchor.MiddleCenter;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.fontSize = 15;
                
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name);
                VisualElement Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon = new VisualElement();
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon.style.width = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon.style.height = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon.style.backgroundImage = AssetManager.GetAsset($"{administrationType}.png");
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon);
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.RegisterCallback<ClickEvent>((click) =>
                {
                    saveData.SelectAdmin = false;
                    saveData.administrationType = administrationType;
                    administrationPickerPopupWindow.rootVisualElement.visible = false;
                    saveData.BudgetModifier = AdministrationBudgetMultiplier[(int)administrationType];
                    UpdateBudget();
                    File.WriteAllText($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism.json", JsonConvert.SerializeObject(K.saveData));
                });
                void AddMultiplierText(List<float> Multiplier,string mtype)
                {
                    Label Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer = Element.Label
                    ($"Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer{administrationType}_Name_{mtype}",
                    $"{mtype}: {Multiplier[(int)administrationType]}x");
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer.style.width = 250;
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer.style.unityTextAlign = TextAnchor.MiddleCenter;
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer.style.fontSize = 15;
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer);
                }
                AddMultiplierText(AdministrationBudgetMultiplier,"Budget");
                AddMultiplierText(AdministrationMaterialMultiplier, "Material");
                AddMultiplierText(AdministrationScienceMultiplier, "Science");

                Label Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget = Element.Label
                    ($"Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget{administrationType}",
                    $"Budget: £{defaultBudget * AdministrationBudgetMultiplier[(int)administrationType]}");
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget.style.width = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget.style.unityTextAlign = TextAnchor.MiddleCenter;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget.style.fontSize = 15;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget);
                Kapitalism_AdministrationPickerPopup.Add(Kapitalism_AdministrationPickerPopup_AddAdministrationType);

            }
            foreach (AdministrationType administrationType in Enum.GetValues(typeof(AdministrationType)))
            {
                AddAdministrationType(administrationType);
            }
            administrationPickerPopupWindow = Window.CreateFromElement(Kapitalism_AdministrationPicker);
            // administrationPickerPopupWindow.panelSettings = UIPanelSettings;
            administrationPickerPopupWindow.rootVisualElement.visible = false;
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }
    public static void PartPriceSetterPopup()
    {
        try
        {
            int Width = 500;
            int Height = 300;

            VisualElement Kapitalism_PartPriceSetterPopup = Element.Root("Kapitalism_PartPriceSetterPopup");
            IStyle style_Kapitalism_PartPriceSetterPopup = Kapitalism_PartPriceSetterPopup.style;
            style_Kapitalism_PartPriceSetterPopup.width = Width;
            style_Kapitalism_PartPriceSetterPopup.height = Height;
            style_Kapitalism_PartPriceSetterPopup.backgroundImage = AssetManager.GetAsset("administrationbg.png");
            style_Kapitalism_PartPriceSetterPopup.position = Position.Absolute;
            style_Kapitalism_PartPriceSetterPopup.left = 700;
            style_Kapitalism_PartPriceSetterPopup.top = 0;
            //margin
            style_Kapitalism_PartPriceSetterPopup.marginBottom = 0;
            style_Kapitalism_PartPriceSetterPopup.marginTop = 0;
            style_Kapitalism_PartPriceSetterPopup.marginLeft = 0;
            style_Kapitalism_PartPriceSetterPopup.marginRight = 0;
            //padding
            style_Kapitalism_PartPriceSetterPopup.paddingBottom = 0;
            style_Kapitalism_PartPriceSetterPopup.paddingTop = 0;
            style_Kapitalism_PartPriceSetterPopup.paddingLeft = 0;
            style_Kapitalism_PartPriceSetterPopup.paddingRight = 0;

            Label Kapitalism_PartPriceSetterPopup_selectedPart = Element.Label("Kapitalism_PartPriceSetterPopup_selectedPart", "selected part");
            Kapitalism_PartPriceSetterPopup.Add(Kapitalism_PartPriceSetterPopup_selectedPart);

            TextField Kapitalism_PartPriceSetterPopup_selectedPart_Price = Element.TextField("Kapitalism_PartPriceSetterPopup_selectedPart_Price", "0");
            Kapitalism_PartPriceSetterPopup.Add(Kapitalism_PartPriceSetterPopup_selectedPart_Price);

            Button Kapitalism_PartPriceSetterPopup_accept = Element.Button("Kapitalism_PartPriceSetterPopup_accept", "Set");
            Kapitalism_PartPriceSetterPopup_accept.clickable = new Clickable(() => {

                PartCostData.First(p => p.partName == Kapitalism_PartPriceSetterPopup_selectedPart.text).cost = int.Parse(Kapitalism_PartPriceSetterPopup_selectedPart_Price.value);
                File.WriteAllText($"./BepInEx/plugins/Kapitalsim/PartData.json", JsonConvert.SerializeObject(PartCostData));
                PartPriceSetterPopupPopupWindow.rootVisualElement.visible = false;
            });
            Kapitalism_PartPriceSetterPopup.Add(Kapitalism_PartPriceSetterPopup_accept);
            PartPriceSetterPopupPopupWindow = Window.CreateFromElement(Kapitalism_PartPriceSetterPopup);
            // PartPriceSetterPopupPopupWindow.panelSettings = UIPanelSettings;
            PartPriceSetterPopupPopupWindow.rootVisualElement.visible = false;
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }
    public static void Update()
    {
        try
        {
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter && saveData.SelectAdmin && administrationPickerPopupWindow.rootVisualElement.visible == false)
            {
                administrationPickerPopupWindow.rootVisualElement.visible = true;
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter && GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/Colony/GlobalHeaderScienceTotal/Funds") == null)
            {
                GameObject FundsObject = GameObject.Instantiate(
                    GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/Colony/GlobalHeaderScienceTotal"),
                    GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/Colony/GlobalHeaderScienceTotal").transform);
                try
                {
                    FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
                    FundsObject.name = "Funds";
                    FundsObject.GetChild("Science Icon").DestroyGameObject();
                    ValuesToUpdate.Add(FundsObject);
                }
                catch (Exception e)
                {
                    try
                    {
                        FundsObject.DestroyGameObject();
                    }
                    catch (Exception ee)
                    {
                        //logger.Error($"{ee}\n{ee.Message}\n{ee.InnerException}\n{ee.Source}\n{ee.Data}\n{ee.HelpLink}\n{ee.HResult}\n{ee.StackTrace}\n{ee.TargetSite}\n{ee.GetBaseException()}");
                    }
                    //logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                }

            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.MissionControl && GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/GHMissionControl/GlobalHeaderScienceTotal/Funds") == null)
            {
                GameObject FundsObject = GameObject.Instantiate(
                    GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/GHMissionControl/GlobalHeaderScienceTotal"),
                    GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/GHMissionControl/GlobalHeaderScienceTotal").transform);

                try
                {
                    FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
                    FundsObject.GetChild("Science Icon").DestroyGameObject();
                    FundsObject.name = "Funds";
                    ValuesToUpdate.Add(FundsObject);
                }
                catch (Exception e)
                {
                    try
                    {
                        FundsObject.DestroyGameObject();
                    }
                    catch (Exception ee)
                    {
                        //logger.Error($"{ee}\n{ee.Message}\n{ee.InnerException}\n{ee.Source}\n{ee.Data}\n{ee.HelpLink}\n{ee.HResult}\n{ee.StackTrace}\n{ee.TargetSite}\n{ee.GetBaseException()}");
                    }
                    //logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                }
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.ResearchAndDevelopment && GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/ResearchDevelopment/GlobalHeaderScienceTotal/Funds") == null)
            {
                GameObject FundsObject = GameObject.Instantiate(
                    GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/ResearchDevelopment/GlobalHeaderScienceTotal"),
                    GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/Contextual/ResearchDevelopment/GlobalHeaderScienceTotal").transform);

                try
                {
                    FundsObject.GetChild("Player Science").GetComponent<TextMeshProUGUI>().text = $"£{saveData.Budget + saveData.Funds}";
                    FundsObject.GetChild("Science Icon").DestroyGameObject();
                    FundsObject.name = "Funds";
                    ValuesToUpdate.Add(FundsObject);
                }
                catch (Exception e)
                {
                    try
                    {
                        FundsObject.DestroyGameObject();
                    }
                    catch (Exception ee)
                    {
                        //logger.Error($"{ee}\n{ee.Message}\n{ee.InnerException}\n{ee.Source}\n{ee.Data}\n{ee.HelpLink}\n{ee.HResult}\n{ee.StackTrace}\n{ee.TargetSite}\n{ee.GetBaseException()}");
                    }
                    //logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
                }
            }
            try
            {
                KSP.UI.Binding.UIValue_ReadNumber_DateTime.DateTime dateTime = KSP.UI.Binding.UIValue_ReadNumber_DateTime.ComputeDateTime(GameManager.Instance.Game.UniverseModel.UniverseTime, 6, 425);
                if (dateTime.Years > saveData.CurrentYear)
                {
                    saveData.CurrentYear = dateTime.Years;
                    UpdateBudget();
                }
            }
            catch (Exception e)
            {
                //logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }

            if (Input.GetMouseButtonDown(1) && config.PartCostEditor)
            {

                justClicked = true;
                GameStateConfiguration gameStateConfiguration = GameManager.Instance.Game.GlobalGameState.GetGameState();
                
                if (gameStateConfiguration.IsObjectAssembly && GameManager.Instance.Game.OAB.Current.ActivePartTracker.partGrabbed == null)
                {
                    var tempobj = GameObject.Find("OAB(Clone)");
                    if (tempobj.GetComponent<ObjectAssemblyBuilderInstance>().ActivePartTracker.PartsUnderCursor.Length > 0)
                    {
                        assemblyPart = tempobj.GetComponent<ObjectAssemblyBuilderInstance>().ActivePartTracker.PartsUnderCursor.Last().Key;
                        PartPriceSetterPopupPopupWindow.rootVisualElement.Q<Label>("Kapitalism_PartPriceSetterPopup_selectedPart").text = assemblyPart.PartName;
                        PartPriceSetterPopupPopupWindow.rootVisualElement.Q<TextField>("Kapitalism_PartPriceSetterPopup_selectedPart_Price").value = $"{PartCostData.First(p => p.partName == assemblyPart.PartName).cost}";
                        showPartMenuUI = true;
                        PartPriceSetterPopupPopupWindow.rootVisualElement.visible = true;
                    }
                }


            }
            if (Input.GetMouseButtonDown(2))
            {
                PartPriceSetterPopupPopupWindow.rootVisualElement.visible = false;
            }
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }
   
}

public static class Kpatch
{
    private static Logger logger = new Logger(K.ModName, K.ModVersion);
    [HarmonyPatch(typeof(LoadOrSaveCampaignTicket))]
    [HarmonyPatch("StartLoadOrSaveOperation")]
    [HarmonyPostfix]
    public static void LoadOrSaveCampaignTicket_StartLoadOrSaveOperation(LoadOrSaveCampaignTicket __instance)
    {
        try
        {
            logger.Log(GameManager.Instance.Game.SessionManager.ActiveCampaignName);
            string SaveLocation = $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism.json";
            string SaveLocationM = $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/missions";
            logger.Log(SaveLocation);
            if (!Directory.Exists($"./ModSaveData"))
            {
                Directory.CreateDirectory($"./ModSaveData");
            }
            if (!Directory.Exists($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}"))
            {
                Directory.CreateDirectory($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}");
            }
            void LoadSave()
            {
                if (File.Exists(SaveLocation))
                {
                    K.saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SaveLocation));
                }
                else
                {
                    File.WriteAllText(SaveLocation, JsonConvert.SerializeObject(K.saveData));
                }
            }
            void DeleteSave()
            {
                try
                {
                    File.Delete(SaveLocation);
                    File.Delete(SaveLocationM);
                }
                catch(Exception e)
                {

                }
                

            }
            void SaveSave()
            {
                File.WriteAllText(SaveLocation, JsonConvert.SerializeObject(K.saveData));
            }

            void SaveToBuffer()
            {
                K.BuffersaveData = JsonConvert.SerializeObject(K.saveData);
            }
            void LoadFromBuffer()
            {
                if(K.LaunchClickBuffersaveData.Length > 0) {
                    K.saveData = JsonConvert.DeserializeObject<SaveData>(K.LaunchClickBuffersaveData);
                    K.LaunchClickBuffersaveData = "";
                } else
                {
                    K.saveData = JsonConvert.DeserializeObject<SaveData>(K.BuffersaveData);
                    K.BuffersaveData = "";
                }
            }
            logger.Debug($"{__instance._loadOrSaveCampaignOperation}");
            switch (__instance._loadOrSaveCampaignOperation)
            {
                case LoadOrSaveCampaignOperation.None: break;
                case LoadOrSaveCampaignOperation.Load_StartNewCampaign:
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
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }
    [HarmonyPatch(typeof(PartProvider))]
    [HarmonyPatch("AddPartData")]
    [HarmonyPrefix]
    public static bool PartProvider_AddPartData(PartProvider __instance, ref PartCore jsonData, ref string rawJson)
    {
        string name = jsonData.data.partName;
        try
        {
            KPartData tpart = K.PartCostData.First((part) => part.partName == name);
            if (tpart.partName == jsonData.data.partName) {
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
    public static void PartInfoOverlay_PopulateCoreInfoFromPart(ref List<KeyValuePair<string, string>> __result,PartInfoOverlay __instance,ref IObjectAssemblyAvailablePart IOBAPart)
    {
        try
        {
            __result.Add(new KeyValuePair<string, string>("Cost", $"£{IOBAPart.PartData.cost}"));
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

        }
    }
    [HarmonyPatch(typeof(ObjectAssemblyBuilderEventsManager))]
    [HarmonyPatch("IsClearedForLaunch")]
    [HarmonyPostfix]
    public static void ObjectAssemblyBuilderEventsManager_IsClearedForLaunch(ref bool __result, ObjectAssemblyBuilderEventsManager __instance)
    {
        try
        {
            float totalCost = 0;
            if (__instance.builder.Stats.HasMainAssembly)
            {
                __instance.builder.Stats.MainAssembly.Parts.ForEach(part =>
                {
                    totalCost += GameManager.Instance.Game.Parts._partData[part.PartName].data.cost;
                });
            }
            if (K.UseFunding(totalCost))
            {
                K.LaunchClickBuffersaveData = JsonConvert.SerializeObject(K.saveData);
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
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            __result = false;
            Utils.MessageUser("Kapitalism error");
        }
        
    }
    //public class MissionRewardUpdate
    //{
    //    public string MissionID;
    //    public int RewardStage;
    //    public float SciencePoints;
    //    public float Funds;
    //    public float Budget;
    //}
    //[HarmonyPatch(typeof(LoadKSP2MissionBaseFlowAction))]
    //[HarmonyPatch("DoAction")]
    //[HarmonyPostfix]
    //public static void LoadKSP2MissionBaseFlowAction_DoAction(LoadKSP2MissionBaseFlowAction __instance,ref Action resolve,ref Action<string> reject)
    //{
    //    try
    //    {
    //        __instance._game.UI.SetLoadingBarText("Loading Kapitalism Missions"); 
    //        __instance._resolve = resolve;

    //        Directory.GetFiles($"./BepInEx/plugins/Kapitalsim/assets/missions").ForEach(file =>
    //        {
    //            __instance._game.UI.SetLoadingBarText($"Loading Kapitalism Mission {file}");
    //            __instance._game.KSP2MissionManager.OnMissionDataItemLoaded(new TextAsset(File.ReadAllText(file)));
    //        });
    //        Directory.GetFiles($"./BepInEx/plugins/Kapitalsim/assets/updatemissions").ForEach(file =>
    //        {
    //            __instance._game.UI.SetLoadingBarText($"Loading Kapitalism Mission reward patches {file}");
    //            MissionRewardUpdate rewardChanges = JsonConvert.DeserializeObject<MissionRewardUpdate>(File.ReadAllText(file));
    //            __instance._game.KSP2MissionManager._missionDefinitions.Find(mission => mission.ID == rewardChanges.MissionID).missionStages.Find(stage => stage.StageID == rewardChanges.RewardStage).MissionReward.MissionRewardDefinitions.Clear();
    //            __instance._game.KSP2MissionManager._missionDefinitions.Find(mission => mission.ID == rewardChanges.MissionID).missionStages.Find(stage => stage.StageID == rewardChanges.RewardStage).MissionReward.MissionRewardDefinitions.Add(new MissionRewardDefinition()
    //            {
    //                MissionRewardType = (MissionRewardType)Enum.Parse(typeof(MissionRewardType), "SciencePoints"),
    //                RewardAmount = rewardChanges.SciencePoints,
    //                RewardKey = null
    //            });
    //            __instance._game.KSP2MissionManager._missionDefinitions.Find(mission => mission.ID == rewardChanges.MissionID).missionStages.Find(stage => stage.StageID == rewardChanges.RewardStage).MissionReward.MissionRewardDefinitions.Add(new MissionRewardDefinition()
    //            {
    //                MissionRewardType = (MissionRewardType)Enum.Parse(typeof(MissionRewardType), "Funds"),
    //                RewardAmount = rewardChanges.Funds,
    //                RewardKey = null
    //            });
    //            __instance._game.KSP2MissionManager._missionDefinitions.Find(mission => mission.ID == rewardChanges.MissionID).missionStages.Find(stage => stage.StageID == rewardChanges.RewardStage).MissionReward.MissionRewardDefinitions.Add(new MissionRewardDefinition()
    //            {
    //                MissionRewardType = (MissionRewardType)Enum.Parse(typeof(MissionRewardType), "Budget"),
    //                RewardAmount = rewardChanges.Budget,
    //                RewardKey = null
    //            });
    //        });
    //        try
    //        {
    //            if (Directory.Exists($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/missions/"))
    //            {
    //                Directory.GetFiles($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/missions/").ForEach(file =>
    //                {
    //                    __instance._game.UI.SetLoadingBarText($"Loading Kapitalism Save Generated Mission {file}");
    //                    __instance._game.KSP2MissionManager.OnMissionDataItemLoaded(new TextAsset(File.ReadAllText(file)));
    //                });
    //            }
               
    //        }
    //        catch (Exception e)
    //        {
    //            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

    //        }
            
    //        __instance._resolve();
    //    }
    //    catch (Exception e)
    //    {
    //        logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

    //    }
    //}
    public static float[] GetKapitalismMissionData(MissionData missionData)
    {
        float[] Rewards = {0,0 };
        foreach (MissionStage missionStage in missionData.missionStages)
        {
            foreach (MissionRewardDefinition rewardDefinition in missionStage.MissionReward.MissionRewardDefinitions)
            {
                logger.Log($"{rewardDefinition.MissionRewardType} {rewardDefinition.MissionRewardType.Equals(Enum.Parse(typeof(MissionRewardType), "Budget"))}");
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
            float[] Rewards = { 0, 0 };
            List<MissionSaveData> agencyMissionSaveData;
            SaveLoadMissionUtils.TryGetMissionSaveDatas(__instance._currentGame, out List<MissionSaveData> _, out agencyMissionSaveData);
            logger.Log($"{agencyMissionSaveData.Count}");
            foreach (MissionData missionDefinition in __instance._currentGame.KSP2MissionManager.GetMissionDefinitions())
            {
                logger.Log("GetMissionDefinitions");
                MissionSaveData missionSaveData;
                if (missionDefinition.Owner == MissionOwner.Agency && SaveLoadMissionUtils.TryGetMissionSaveData(agencyMissionSaveData, missionDefinition.ID, out missionSaveData) && missionSaveData.TurnedIn)
                {
                    logger.Log("GetKapitalismMissionData");
                    float[] tmpRewards = GetKapitalismMissionData(missionDefinition);
                    Rewards[0] += tmpRewards[0];
                    Rewards[1] += tmpRewards[1];
                    logger.Log($"Rewards[0] {Rewards[0]} | Rewards[1] {Rewards[1]}");
                }
            }
            K.UpdateFunds(Rewards[1]);
            K.UpdateBudgetModifier(Rewards[0]);
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

        }
    }
}