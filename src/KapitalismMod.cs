using BepInEx;
using ShadowUtilityLIB;
using ShadowUtilityLIB.UI;
using Logger = ShadowUtilityLIB.logging.Logger;
using HarmonyLib;
using Newtonsoft.Json;
using KSP.Game;
using UnityEngine;
using KSP.Sim.Definitions;
using UnityEngine.UIElements;
using UitkForKsp2.API;
using KSP.UI;
using KSP.OAB;
using static KSP.OAB.ObjectAssemblyBuilder;
using KSP.Game.Missions;
using KSP.Game.Missions.Definitions;
using KSP.Game.Science;
 
namespace Kapitalism;
[BepInPlugin("com.shadowdev.kapitalism", "Kapitalism", "0.0.3")]
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
    public float ScienceModifier = 1;
    public float MaterialModifier = 1;
    public float Funds = 0;
    public bool SelectAdmin = true;
    public AdministrationType administrationType = AdministrationType.Kapitalist;
    public int CurrentYear = 0;
    public float DificultyScale = 1;
    public List<string> DoneMissions = new List<string>();
}

public static class K
{
    public static string ModId = "com.shadowdev.kapitalism";
    public static string ModName = "Kapitalism";
    public static string ModVersion = "0.0.2.1";
    private static Logger logger = new(ModName, ModVersion);
    public static Kconfig config = new();
    public static SaveData saveData = new();
    public static string BuffersaveData = "";
    public static string LaunchClickBuffersaveData = "";
    public static PanelSettings UIPanelSettings;

    public static string Filename = "";

    public static List<GameObject> ValuesToUpdate = new List<GameObject>();
    public static List<KPartData> PartCostData = new List<KPartData>() { new KPartData() { cost = 10, partName = "test"} };

    public static UIDocument administrationPickerPopupWindow;
    public static UIDocument KapitalismStatsWindow;

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
            KapitalismStats();
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

        }
    }
    public static void UpdateSpendDisplay(float value)
    {
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend").text = $"spend £{value}";
    }
    public static void setSpendDisplayMode(bool enable)
    {
        if (enable)
        {
            KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend").visible = true;
            KapitalismStatsWindow.rootVisualElement.style.height = 50;
        }
        else
        {
            KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend").visible = false;
            KapitalismStatsWindow.rootVisualElement.style.height = 25;
        }
    }
    public static void UpdateDisplay()
    {
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Cost").text = $"£{saveData.Budget + saveData.Funds}";
    }
    public static void UpdateBudgetModifier(float value)
    {
        saveData.BudgetModifier += value;
        UpdateDisplay();
    }
    public static void UpdateBudget()
    {
        saveData.Budget = defaultBudget * saveData.BudgetModifier;
        GameManager.Instance.Game.UI.NotificationProvider.PushAlertNotification(new NotificationData()
        {
            TimeStamp = GameManager.Instance.Game.UniverseModel.Time.UniverseTime,
            AlertTitle = new NotificationLineItemData()
            {
                LocKey = $"New Budget"
            },
            FirstLine = new NotificationLineItemData()
            {
                LocKey = $"Your new budget is {saveData.Budget}"
            },
            Importance = NotificationImportance.Medium,
            TimerDuration = 20f

        });
        UpdateDisplay();
    }
    public static void UpdateFunds(float value)
    {
        GameManager.Instance.Game.UI.NotificationProvider.PushAlertNotification(new NotificationData()
        {
            TimeStamp = GameManager.Instance.Game.UniverseModel.Time.UniverseTime,
            AlertTitle = new NotificationLineItemData()
            {
                LocKey = $"Funds updated"
            },
            FirstLine = new NotificationLineItemData()
            {
                LocKey = $"Funds added {value}"
            },
            Importance = NotificationImportance.Medium,
            TimerDuration = 20f

        }) ;
        saveData.Funds += value;
        UpdateDisplay();
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
            UpdateDisplay();
            return true;
        }
        UpdateDisplay();
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
                    saveData.ScienceModifier = AdministrationScienceMultiplier[(int)administrationType];
                    saveData.MaterialModifier = AdministrationMaterialMultiplier[(int)administrationType];
                    UpdateBudget();
                    File.WriteAllText($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism.json", JsonConvert.SerializeObject(K.saveData));
                    KapitalismStatsWindow.rootVisualElement.visible = true;
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
    public static void KapitalismStats()
    {
        try
        {
            int Width = 200;
            int Height = 25;

            VisualElement KapitalismStats_window = Element.Root("KapitalismStats_window");
            IStyle style_KapitalismStats_window = KapitalismStats_window.style;
            style_KapitalismStats_window.width = Width;
            style_KapitalismStats_window.height = Height;
            style_KapitalismStats_window.position = Position.Absolute;
            style_KapitalismStats_window.left = 1920 - 200;
            style_KapitalismStats_window.top = 40;
            //margin
            style_KapitalismStats_window.marginBottom = 0;
            style_KapitalismStats_window.marginTop = 0;
            style_KapitalismStats_window.marginLeft = 0;
            style_KapitalismStats_window.marginRight = 0;
            //padding
            style_KapitalismStats_window.paddingBottom = 0;
            style_KapitalismStats_window.paddingTop = 0;
            style_KapitalismStats_window.paddingLeft = 0;
            style_KapitalismStats_window.paddingRight = 0;

            style_KapitalismStats_window.backgroundColor = new StyleColor(new Color(0f,0f,0f,0.9f));

            Label KapitalismStats_window_Cost = Element.Label("KapitalismStats_window_Cost", $"£{saveData.Budget + saveData.Funds}");
            KapitalismStats_window.Add(KapitalismStats_window_Cost);
            Label KapitalismStats_window_Spend = Element.Label("KapitalismStats_window_Spend", $"Spend £{0}");
            KapitalismStats_window_Spend.visible = false;
            KapitalismStats_window.Add(KapitalismStats_window_Spend);

            KapitalismStatsWindow = Window.CreateFromElement(KapitalismStats_window);
            KapitalismStatsWindow.rootVisualElement.visible = false;
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
    }
    public static void Update()
    {
        //dont run update if not loaded
        try
        {
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter)
            {

            }
        }
        catch (Exception e)
        {
            return;
        }
        try
        {
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter && saveData.SelectAdmin && administrationPickerPopupWindow.rootVisualElement.visible == false)
            {
                administrationPickerPopupWindow.rootVisualElement.visible = true;
                KapitalismStatsWindow.rootVisualElement.visible = false;
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter && KapitalismStatsWindow.rootVisualElement.visible == false)
            {

                KapitalismStatsWindow.rootVisualElement.visible = true;
                setSpendDisplayMode(false);
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.MissionControl && KapitalismStatsWindow.rootVisualElement.visible == false)
            {
                KapitalismStatsWindow.rootVisualElement.visible = true;
                setSpendDisplayMode(false);
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.ResearchAndDevelopment && KapitalismStatsWindow.rootVisualElement.visible == false)
            {
                KapitalismStatsWindow.rootVisualElement.visible = true;
                setSpendDisplayMode(false);
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.FlightView && KapitalismStatsWindow.rootVisualElement.visible == true)
            {
                KapitalismStatsWindow.rootVisualElement.visible = false;
                setSpendDisplayMode(false);
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
            if(GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.VehicleAssemblyBuilder)
            {
                if (KapitalismStatsWindow.rootVisualElement.visible == false)
                {
                    KapitalismStatsWindow.rootVisualElement.visible = true;
                    
                }
                setSpendDisplayMode(true);
                float totalCost = 0;
                try{
                    GameManager.Instance.Game.OAB.Current.eventsManager.builder.Stats.MainAssembly.Parts.ForEach(part =>
                    {
                        totalCost += GameManager.Instance.Game.Parts._partData[part.PartName].data.cost;
                    });
                }
                catch(Exception e)
                {
                    
                }
                UpdateSpendDisplay(totalCost);
                if (Input.GetMouseButtonDown(1) && config.PartCostEditor)
                {
                    justClicked = true;
                    if (GameManager.Instance.Game.OAB.Current.ActivePartTracker.partGrabbed == null)
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
            else
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
    [HarmonyPatch(typeof(SaveLoadManager))]
    [HarmonyPatch("StartLoadOrSaveOperation")]
    [HarmonyPostfix]
    public static void SaveLoadManager_StartLoadOrSaveOperation(SaveLoadManager __instance, ref LoadOrSaveCampaignTicket loadOrSaveCampaignTicket)
    {
        try
        {
            logger.Log(__instance.ActiveCampaignFolderPath);
            logger.Log(loadOrSaveCampaignTicket._loadFileName);
            logger.Log(loadOrSaveCampaignTicket._saveFileName);
            logger.Log(GameManager.Instance.Game.SessionManager.ActiveCampaignName);
            if (loadOrSaveCampaignTicket._saveFileName.Length > 0)
            {
                K.Filename = loadOrSaveCampaignTicket._saveFileName.Split("\\")[loadOrSaveCampaignTicket._saveFileName.Split("\\").Length - 1];
            }
            if (loadOrSaveCampaignTicket._loadFileName.Length > 0)
            {
                K.Filename = loadOrSaveCampaignTicket._loadFileName.Split("\\")[loadOrSaveCampaignTicket._loadFileName.Split("\\").Length - 1];
            }
            
            string SaveLocation = $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism/{K.Filename}";
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
            if (!Directory.Exists($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism"))
            {
                Directory.CreateDirectory($"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism");
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
            K.LaunchClickBuffersaveData = JsonConvert.SerializeObject(K.saveData);
            if (K.UseFunding(totalCost))
            {
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
    [HarmonyPatch(typeof(ScienceManager))]
    [HarmonyPatch("TrySubmitCompletedResearchReport")]
    [HarmonyPostfix]
    public static void ScienceManager_TrySubmitCompletedResearchReport(ref bool __result, ScienceManager __instance, ref CompletedResearchReport report)
    {
        try
        {
            float Funds =  report.FinalScienceValue * (K.saveData.ScienceModifier * 76);
            K.UpdateFunds(Funds);
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
                    if (K.saveData.DoneMissions.Contains(missionDefinition.ID)) { } else
                    {
                        logger.Log("GetKapitalismMissionData " + missionSaveData.Completed);
                        float[] tmpRewards = GetKapitalismMissionData(missionDefinition);
                        Rewards[0] += tmpRewards[0];
                        Rewards[1] += tmpRewards[1];
                        logger.Log($"Rewards[0] {Rewards[0]} | Rewards[1] {Rewards[1]}");
                        K.saveData.DoneMissions.Add(missionDefinition.ID);
                    }
                    
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
    //[HarmonyPatch(typeof(NestedPrefabSpawner))]
    //[HarmonyPatch("Awake")]
    //[HarmonyPrefix]
    //public static bool NestedPrefabSpawner_Awake(NestedPrefabSpawner __instance)
    //{
    //    __instance.Prefabs.Add(__instance.Prefabs[0]);
        
    //    return true;
    //}
}