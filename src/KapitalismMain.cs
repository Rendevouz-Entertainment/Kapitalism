using System.Collections;
using HarmonyLib;
using KSP.Game;
using KSP.Modules;
using KSP.OAB;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using KSP.Sim.ResourceSystem;
using Newtonsoft.Json;
using ReLIB;
using ReLIB.UI;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = ReLIB.logging.Logger;
using Position = UnityEngine.UIElements.Position;
using Shapes;

namespace Kapitalism;

public static class K
{
    public static string ModId = "com.rendevouzrs_entertainment.kapitalism";
    public static string ModName = "Kapitalism";
    public static string ModVersion = "0.0.3.0";
    private static Logger logger = new(ModName, ModVersion);
    public static Kconfig config = new();
    public static SaveData saveData = new();
    public static string BuffersaveData = "";
    public static string LaunchClickBuffersaveData = "";
    public static PanelSettings UIPanelSettings;

    public static string Filename = "";

    public static List<GameObject> ValuesToUpdate = new List<GameObject>();
    public static List<KPartData> PartCostData = new List<KPartData>() { new KPartData() { cost = 10, partName = "test" } };

    public static UIDocument administrationPickerPopupWindow;
    public static UIDocument KapitalismStatsWindow;
    public static UIDocument KapitalismAdministrationWindow;
    public static UIDocument KapitalismResourceswindow;

    public static bool justClicked = false;

    public static List<float> AdministrationBudgetMultiplier = new List<float>()
    {
        20,
        10,
        10,
        5f,
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
    
    public static void TryCatch(Action action, bool Log = true)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception e)
        {
            if (Log == true)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            }
        }
    }
    
    public static UIDocument PartPriceSetterPopupPopupWindow { get; private set; }

    public static IEnumerator UpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            TryCatch(() =>
            {
                if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.VehicleAssemblyBuilder)
                {
                    //UpdateKapitalismResourcesWindow();
                }
            },false);
        }
       
    }

    public static void Start()
    {
        ReLIBMod.EnableDebugMode();
        ReLIBMod.RunCr(UpdateLoop());
        UIPanelSettings = Manager.PanelSettings;
        
        TryCatch(() =>
        {
            Harmony.CreateAndPatchAll(typeof(KapitalismPatch));
            PartCostData = JsonConvert.DeserializeObject<List<KPartData>>(File.ReadAllText($"./BepInEx/plugins/Kapitalism/PartData.json"));
        });
        TryCatch(() =>
        {
            if (Directory.Exists("./Config")) { }
            else
            {
                Directory.CreateDirectory("./Config");
            }
            if (File.Exists("./Config/Kapitalism.config"))
            {
                config = JsonConvert.DeserializeObject<Kconfig>(File.ReadAllText("./Config/Kapitalism.config"));
            }
            else
            {
                File.WriteAllText("./Config/Kapitalism.config", JsonConvert.SerializeObject(config));
            }

        });
        TryCatch(() =>
        {
            AdministrationPickerPopup();
            PartPriceSetterPopup();
            KapitalismStats();
            KapitalismResources();
            
        });
    }
    public static void UpdateSpendDisplay(float value, float resourceCost = 0)
    {
       
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_amount").text = $"£{(value + resourceCost):n2}";
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Part_amount").text = $"£{value:n2}";
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Material_amount").text = $"£{resourceCost:n2}";
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Left_amount").text = $"£{((saveData.Funds + saveData.Budget) - (resourceCost + value)):n2}";
    }
    public static void setSpendDisplayMode(bool enable)
    {
        TryCatch(() =>
        {
            if (enable)
            {
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend_Part").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend_Material").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend_Left").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Sep_1").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Sep_2").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Sep_3").style.visibility = Visibility.Visible;
                KapitalismStatsWindow.rootVisualElement.style.height = 145;
            }
            else
            {
                logger.Log("disableSpendDisplay");
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend_Part").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend_Material").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.Q<VisualElement>("KapitalismStats_window_Spend_Left").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Sep_1").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Sep_2").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Spend_Sep_3").style.visibility = Visibility.Hidden;
                KapitalismStatsWindow.rootVisualElement.style.height = 25;
            }
        },true);
    }
    public static void UpdateDisplay()
    {
        KapitalismStatsWindow.rootVisualElement.Q<Label>("KapitalismStats_window_Cost_amount").text = $"£{(saveData.Budget + saveData.Funds):n2}";
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
    public static void UpdateFunds(float value, string LocKey = $"Funds updated")
    {
        if (value > 0.1f)
        {
            GameManager.Instance.Game.UI.NotificationProvider.PushAlertNotification(new NotificationData()
            {
                TimeStamp = GameManager.Instance.Game.UniverseModel.Time.UniverseTime,
                AlertTitle = new NotificationLineItemData()
                {
                    LocKey = LocKey
                },
                FirstLine = new NotificationLineItemData()
                {
                    LocKey = $"Funds added {value}"
                },
                Importance = NotificationImportance.Medium,
                TimerDuration = 20f

            });
            saveData.Funds += value;
        }
        UpdateDisplay();
    }

    public static bool UseFunding(float value)
    {
        if (saveData.Funds + saveData.Budget >= value)
        {
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

    public static void UseResources(Dictionary<string, float> ResourcesToUse)
    {
        ResourcesToUse.ForEach(kv =>
        {
            if (K.saveData.MaterialStorage[kv.Key] < kv.Value)
            {
                K.saveData.MaterialStorage[kv.Key] = 0;
            }
            else
            {
                K.saveData.MaterialStorage[kv.Key] -= kv.Value;
            }
        });

    }
    public static void AdministrationPickerPopup()
    {
        TryCatch(() =>
        {
            int Width = 1920;
            int Height = 1080;
            var Kapitalism_AdministrationPickerRoot = Element.Root("Kapitalism_AdministrationPicker");

            IStyle style_Kapitalism_AdministrationPicker = Kapitalism_AdministrationPickerRoot.style;
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
            Kapitalism_AdministrationPickerRoot.Add(Kapitalism_AdministrationPickerTitle);
            Kapitalism_AdministrationPickerTitle.style.position = Position.Absolute;
            Kapitalism_AdministrationPickerTitle.style.top = 10;
            Kapitalism_AdministrationPickerTitle.style.width = Width;
            Kapitalism_AdministrationPickerTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            Kapitalism_AdministrationPickerTitle.style.fontSize = 20;

            Label Kapitalism_AdministrationPickerSubtext = Element.Label("AdministrationPickerst",
                "Pick your administration type below, you cannot change this later so pick carefully");

            Kapitalism_AdministrationPickerRoot.Add(Kapitalism_AdministrationPickerSubtext);
            Kapitalism_AdministrationPickerSubtext.style.position = Position.Absolute;
            Kapitalism_AdministrationPickerSubtext.style.top = 40;
            Kapitalism_AdministrationPickerSubtext.style.width = Width;
            Kapitalism_AdministrationPickerSubtext.style.unityTextAlign = TextAnchor.MiddleCenter;
            Kapitalism_AdministrationPickerSubtext.style.fontSize = 15;

            VisualElement Kapitalism_AdministrationPickerPopup = new VisualElement();
            Kapitalism_AdministrationPickerRoot.Add(Kapitalism_AdministrationPickerPopup);
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
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.name =
                    $"Kapitalism_AdministrationPickerPopup_AddAdministrationType_{administrationType}";
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.width = ATwidth;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.marginRight = Gap;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.marginLeft = Gap;

                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.display = DisplayStyle.Flex;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.style.flexDirection = FlexDirection.Column;
                Label Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name = Element.Label(
                    $"Kapitalism_AdministrationPickerPopup_AddAdministrationType_{administrationType}_Name",
                    $"{administrationType}");
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.marginTop = 350;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.width = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.unityTextAlign =
                    TextAnchor.MiddleCenter;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name.style.fontSize = 15;

                Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Name);
                VisualElement Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon = new VisualElement();
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon.style.width = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon.style.height = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon.style.backgroundImage =
                    AssetManager.GetAsset($"{administrationType}.png");
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Icon);
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.RegisterCallback<ClickEvent>((click) =>
                {
                    K.saveData.SelectAdmin = false;
                    K.saveData.administrationType = administrationType;
                    K.administrationPickerPopupWindow.rootVisualElement.visible = false;
                    K.saveData.BudgetModifier = K.AdministrationBudgetMultiplier[(int)administrationType];
                    K.saveData.ScienceModifier = K.AdministrationScienceMultiplier[(int)administrationType];
                    K.saveData.MaterialModifier = K.AdministrationMaterialMultiplier[(int)administrationType];
                    K.UpdateBudget();
                    File.WriteAllText(
                        $"./ModSaveData/{GameManager.Instance.Game.SessionManager.ActiveCampaignName}/kapitalism.json",
                        JsonConvert.SerializeObject(K.saveData));
                    K.KapitalismStatsWindow.rootVisualElement.visible = true;
                    K.UpdateDisplay();
                });

                void AddMultiplierText(List<float> Multiplier, string mtype)
                {
                    Label Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer = Element.Label
                    ($"Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer{administrationType}_Name_{mtype}",
                        $"{mtype}: {Multiplier[(int)administrationType]}x");
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer.style.width = 250;
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer.style.unityTextAlign =
                        TextAnchor.MiddleCenter;
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer.style.fontSize = 15;
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(
                        Kapitalism_AdministrationPickerPopup_AddAdministrationType_Multiplyer);
                }

                AddMultiplierText(K.AdministrationBudgetMultiplier, "Budget");
                AddMultiplierText(K.AdministrationMaterialMultiplier, "Material");
                AddMultiplierText(K.AdministrationScienceMultiplier, "Science");

                Label Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget = Element.Label
                ($"Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget{administrationType}",
                    $"Budget: £{K.defaultBudget * K.AdministrationBudgetMultiplier[(int)administrationType]}");
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget.style.width = 250;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget.style.unityTextAlign =
                    TextAnchor.MiddleCenter;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget.style.fontSize = 15;
                Kapitalism_AdministrationPickerPopup_AddAdministrationType.Add(
                    Kapitalism_AdministrationPickerPopup_AddAdministrationType_Starting_Budget);
                Kapitalism_AdministrationPickerPopup.Add(Kapitalism_AdministrationPickerPopup_AddAdministrationType);

            }

            foreach (AdministrationType administrationType in Enum.GetValues(typeof(AdministrationType)))
            {
                AddAdministrationType(administrationType);
            }

            logger.Debug("Admin popup created");
            administrationPickerPopupWindow = Window.CreateFromElement(Kapitalism_AdministrationPickerRoot);
            //administrationPickerPopupWindow.panelSettings = UIPanelSettings;
            administrationPickerPopupWindow.rootVisualElement.visible = false;
        });
    }
    public static void PartPriceSetterPopup()
    {
        TryCatch(() =>
        {
            int Width = 500;
            int Height = 300;

            VisualElement Kapitalism_PartPriceSetterPopup = Element.Root("Kapitalism_PartPriceSetterPopup");
            IStyle style_Kapitalism_PartPriceSetterPopup = Kapitalism_PartPriceSetterPopup.style;
            style_Kapitalism_PartPriceSetterPopup.width = Width;
            style_Kapitalism_PartPriceSetterPopup.height = Height;
            style_Kapitalism_PartPriceSetterPopup.backgroundImage = AssetManager.GetAsset("administrationbg.png");
            style_Kapitalism_PartPriceSetterPopup.position = UnityEngine.UIElements.Position.Absolute;
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
            Kapitalism_PartPriceSetterPopup_accept.clickable = new Clickable(() =>
            {

                PartCostData.First(p => p.partName == Kapitalism_PartPriceSetterPopup_selectedPart.text).cost = int.Parse(Kapitalism_PartPriceSetterPopup_selectedPart_Price.value);
                File.WriteAllText($"./BepInEx/plugins/Kapitalism/PartData.json", JsonConvert.SerializeObject(PartCostData));
                PartPriceSetterPopupPopupWindow.rootVisualElement.visible = false;
            });
            Kapitalism_PartPriceSetterPopup.Add(Kapitalism_PartPriceSetterPopup_accept);
            PartPriceSetterPopupPopupWindow = Window.CreateFromElement(Kapitalism_PartPriceSetterPopup);
            // PartPriceSetterPopupPopupWindow.panelSettings = UIPanelSettings;
            PartPriceSetterPopupPopupWindow.rootVisualElement.visible = false;
        });
    }
    public static void KapitalismStats()
    {
        TryCatch(() =>
        {
            int Width = 200;
            int Height = 25;
            
            VisualElement KapitalismStats_window = Element.Root("KapitalismStats_window");
            IStyle style_KapitalismStats_window = KapitalismStats_window.style;
            style_KapitalismStats_window.width = Width;
            style_KapitalismStats_window.height = Height;
            style_KapitalismStats_window.position = Position.Absolute;
            style_KapitalismStats_window.left = 1920 - 200;
            style_KapitalismStats_window.top = 35;
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

            style_KapitalismStats_window.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 1f));
            style_KapitalismStats_window.borderBottomColor = new StyleColor(new Color(0f, 0f, 0f, 1f));
            style_KapitalismStats_window.borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 1f));
            style_KapitalismStats_window.borderLeftColor = new StyleColor(new Color(0f, 0f, 0f, 1f));
            style_KapitalismStats_window.borderRightColor = new StyleColor(new Color(0f, 0f, 0f, 1f));


            void AddValue(string MainElement, string LabelElement,string LabelText, string ValueElement)
            {
                VisualElement KapitalismStatsValueElement = Element.VisualElement(MainElement);
                KapitalismStatsValueElement.style.width = Width;
                KapitalismStatsValueElement.style.height = Height;
                KapitalismStats_window.Add(KapitalismStatsValueElement);
                KapitalismStatsValueElement.style.flexDirection = FlexDirection.Row;
            
                Label KapitalismStatsValueElementlabel = Element.Label(LabelElement, LabelText);
                KapitalismStatsValueElement.Add(KapitalismStatsValueElementlabel);
                KapitalismStatsValueElementlabel.style.color = new StyleColor(new Color(0f, 0f, 0f, 1f));
                KapitalismStatsValueElementlabel.style.width = 105;
                KapitalismStatsValueElementlabel.style.fontSize = 12;
            
                Label KapitalismStatsValueElementValue = Element.Label(ValueElement, $"£{0:n2}");
                KapitalismStatsValueElement.Add(KapitalismStatsValueElementValue);
                KapitalismStatsValueElementValue.style.color = new StyleColor(new Color(0f, 0f, 0f, 1f));
                KapitalismStatsValueElementValue.style.width = 50;
                KapitalismStatsValueElementValue.style.fontSize = 10;
                KapitalismStatsValueElementValue.style.marginTop = 1.5f;
            }

            AddValue("KapitalismStats_window_Cost", "KapitalismStats_window_Cost_amount_label", "Your balance", "KapitalismStats_window_Cost_amount");
            
            //sep
            
            Label KapitalismStats_window_Spend_Sep_1 = Element.Label("KapitalismStats_window_Spend_Sep_1", $"-------------------------");
            KapitalismStats_window_Spend_Sep_1.visible = false;
            KapitalismStats_window_Spend_Sep_1.style.marginTop = 0;
            KapitalismStats_window.Add(KapitalismStats_window_Spend_Sep_1);
            KapitalismStats_window_Spend_Sep_1.style.color = new StyleColor(new Color(0f, 0f, 0f, 1f));
            KapitalismStats_window_Spend_Sep_1.style.marginBottom = -10;
            
            AddValue("KapitalismStats_window_Spend_Part", "KapitalismStats_window_Spend_Part_label", "Part costs", "KapitalismStats_window_Spend_Part_amount");
            AddValue("KapitalismStats_window_Spend_Material", "KapitalismStats_window_Spend_Material_label", "Material costs", "KapitalismStats_window_Spend_Material_amount");
            
            //sep
            
            Label KapitalismStats_window_Spend_Sep_2 = Element.Label("KapitalismStats_window_Spend_Sep_2", $"-------------------------");
            KapitalismStats_window_Spend_Sep_2.visible = false;
            KapitalismStats_window.Add(KapitalismStats_window_Spend_Sep_2);
            KapitalismStats_window_Spend_Sep_2.style.color = new StyleColor(new Color(0f, 0f, 0f, 1f));
            KapitalismStats_window_Spend_Sep_2.style.marginTop = 0;
            KapitalismStats_window_Spend_Sep_2.style.marginBottom = -10;
            
            AddValue("KapitalismStats_window_Spend", "KapitalismStats_window_Spend_amount_Label", "Total", "KapitalismStats_window_Spend_amount");

            //sep
            
            Label KapitalismStats_window_Spend_Sep_3 = Element.Label("KapitalismStats_window_Spend_Sep_3", $"-------------------------");
            KapitalismStats_window_Spend_Sep_3.visible = false;
            KapitalismStats_window.Add(KapitalismStats_window_Spend_Sep_3);
            KapitalismStats_window_Spend_Sep_3.style.color = new StyleColor(new Color(0f, 0f, 0f, 1f));
            KapitalismStats_window_Spend_Sep_3.style.marginTop = 0;
            KapitalismStats_window_Spend_Sep_3.style.marginBottom = -10;
            
            AddValue("KapitalismStats_window_Spend_Left", "KapitalismStats_window_Spend_Left_Label", "New Balance", "KapitalismStats_window_Spend_Left_amount");
            
            KapitalismStatsWindow = Window.CreateFromElement(KapitalismStats_window);
            KapitalismStatsWindow.rootVisualElement.visible = false;
        });
    }
    public static void KapitalismResources()
    {
        TryCatch(() =>
        {
            int Width = 300;
            int Height = 500;

            VisualElement KapitalismResources_window = Element.Root("KapitalismResources_window");
            IStyle style_KapitalismResources_window = KapitalismResources_window.style;
            style_KapitalismResources_window.width = Width;
            style_KapitalismResources_window.height = Height;
            style_KapitalismResources_window.position = Position.Absolute;
            style_KapitalismResources_window.left = 1920 - 500;
            style_KapitalismResources_window.top = 40;
            //margin
            style_KapitalismResources_window.marginBottom = 0;
            style_KapitalismResources_window.marginTop = 0;
            style_KapitalismResources_window.marginLeft = 0;
            style_KapitalismResources_window.marginRight = 0;
            //padding
            style_KapitalismResources_window.paddingBottom = 0;
            style_KapitalismResources_window.paddingTop = 0;
            style_KapitalismResources_window.paddingLeft = 0;
            style_KapitalismResources_window.paddingRight = 0;

            style_KapitalismResources_window.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.9f));

            KapitalismResourceswindow = Window.CreateFromElement(KapitalismResources_window);
            KapitalismResourceswindow.rootVisualElement.visible = false;
        });
    }
    public static void UpdateKapitalismResourcesWindow()
    {
        KapitalismResourceswindow.rootVisualElement.Clear();
        KapitalismResourceswindow.rootVisualElement.visible = false;
        ResourceDefinitionDatabase rdd = GameManager.Instance._game.ResourceDefinitionDatabase;
        int pos = 0;
        rdd._resourceDefinitionDataCache.ForEach(rd =>
        {
            VisualElement ResourceHolderElement = new VisualElement();
            TryCatch(() =>
            {
                
                ResourceHolderElement.style.top = pos;
                pos += 50;
                ResourceHolderElement.style.width = 280;
                ResourceHolderElement.style.height = 50;
                ResourceHolderElement.style.flexDirection = FlexDirection.Column;
                KapitalismResourceswindow.rootVisualElement.Add(ResourceHolderElement);
                Label ResourceHolderElement_Name = Element.Label($"ResourceHolderElement_{rd.DisplayName}_Name", $"{rd.DisplayName} ");
                ResourceHolderElement.Add(ResourceHolderElement_Name);
                if (K.saveData.MaterialStorage.ContainsKey(rd.name))
                {
                    Label ResourceHolderElement_Amount = Element.Label($"ResourceHolderElement_{rd.DisplayName}_Amount", $"Storage: {K.saveData.MaterialStorage[rd.name]}");
                    ResourceHolderElement.Add(ResourceHolderElement_Amount);
                }
                else
                {
                    Label ResourceHolderElement_Amount = Element.Label($"ResourceHolderElement_{rd.DisplayName}_Amount", $"Storage: 0");
                    ResourceHolderElement.Add(ResourceHolderElement_Amount);
                }

                VisualElement ResourceHolderElement_buySell = new VisualElement();
                ResourceHolderElement_buySell.style.flexDirection = FlexDirection.Row;
                ResourceHolderElement.Add(ResourceHolderElement_buySell);
                ResourceHolderElement_buySell.style.width = 280;
                ResourceHolderElement_buySell.style.height = 50;


                //logger.Debug(JsonConvert.SerializeObject(rd));
                float BuyVal = 1 * ((float)rd.resourceProperties.costPerUnit * 1000);
                Button ResourceHolderElement_buy = Element.Button($"ResourceHolderElement_{rd.DisplayName}_Buy", $"Buy 1 unit £{BuyVal}");
                TryCatch(() =>
                {
                    logger.Log("Create clickable");
                    ResourceHolderElement_buy.clickable = new Clickable(() =>
                {
                    TryCatch(() =>
                    {
                        logger.Log("buy resource");
                        if (!K.saveData.MaterialStorage.ContainsKey(rd.name))
                        {
                            K.saveData.MaterialStorage.Add(rd.name, 0);
                        }
                        if (K.UseFunding(BuyVal))
                        {
                            logger.Log("brought");
                            K.saveData.MaterialStorage[rd.name] += 1;
                        }
                    });
                });
                });
                ResourceHolderElement_buySell.Add(ResourceHolderElement_buy);
            });

        });

    }
    public static void KapitalismAdministrationMenu()
    {
        TryCatch(() =>
        {
            int Width = 1920;
            int Height = 1080;

            VisualElement KapitalismAdministration_window = Element.Root("KapitalismAdministration_window");
            IStyle style_KapitalismAdministration_window = KapitalismAdministration_window.style;
            style_KapitalismAdministration_window.width = Width;
            style_KapitalismAdministration_window.height = Height;
            style_KapitalismAdministration_window.position = Position.Absolute;
            style_KapitalismAdministration_window.left = 0;
            style_KapitalismAdministration_window.top = 0;
            //margin
            style_KapitalismAdministration_window.marginBottom = 0;
            style_KapitalismAdministration_window.marginTop = 0;
            style_KapitalismAdministration_window.marginLeft = 0;
            style_KapitalismAdministration_window.marginRight = 0;
            //padding
            style_KapitalismAdministration_window.paddingBottom = 0;
            style_KapitalismAdministration_window.paddingTop = 0;
            style_KapitalismAdministration_window.paddingLeft = 0;
            style_KapitalismAdministration_window.paddingRight = 0;

            style_KapitalismAdministration_window.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f));

            VisualElement KapitalismAdministration_window_Panel = new VisualElement();
            IStyle style_KapitalismAdministration_window_Panel = KapitalismAdministration_window_Panel.style;
            style_KapitalismAdministration_window_Panel.width = 1880;
            style_KapitalismAdministration_window_Panel.height = 1040;
            style_KapitalismAdministration_window_Panel.left = 20;
            style_KapitalismAdministration_window_Panel.top = 20;
            style_KapitalismAdministration_window_Panel.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.90f));

            KapitalismAdministration_window.Add(KapitalismAdministration_window_Panel);

            Label KapitalismAdministration_window_Panel_Title = Element.Label("KapitalismAdministration_window_Panel_Title", $"Administration");

            KapitalismAdministrationWindow = Window.CreateFromElement(KapitalismAdministration_window);
            KapitalismAdministrationWindow.rootVisualElement.visible = false;
        });
    }
    public static void Update()
    {
        //dont run update if not loaded
        TryCatch(() =>
        {
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter)
            {
                GameManager.Instance._game.ResourceDefinitionDatabase._resourceDefinitionWrappers.ForEach(w =>
                {
                    TryCatch(() =>
                    {
                        K.saveData.MaterialStorage.TryAdd(w.originalResourceDefinition.Value.name,0f);
                    },false);
                });
            }
        },false);
        TryCatch(() =>
        {
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter && saveData.SelectAdmin && administrationPickerPopupWindow.rootVisualElement.visible == false)
            {
                administrationPickerPopupWindow.rootVisualElement.visible = true;
                KapitalismStatsWindow.rootVisualElement.visible = false;
                setSpendDisplayMode(false);
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.KerbalSpaceCenter)
            {

                KapitalismStatsWindow.rootVisualElement.visible = true;
                setSpendDisplayMode(false);
                UpdateDisplay();
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.MissionControl)
            {
                KapitalismStatsWindow.rootVisualElement.visible = true;
                setSpendDisplayMode(false);
                UpdateDisplay();
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.ResearchAndDevelopment)
            {
                KapitalismStatsWindow.rootVisualElement.visible = true;
                setSpendDisplayMode(false);
                UpdateDisplay();
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.FlightView && KapitalismStatsWindow.rootVisualElement.visible == true)
            {
                KapitalismStatsWindow.rootVisualElement.visible = false;
                setSpendDisplayMode(false);
                UpdateDisplay();
            }
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.MainMenu)
            {
                KapitalismStatsWindow.rootVisualElement.visible = false;
                setSpendDisplayMode(false);
                UpdateDisplay();
            }
            TryCatch(() =>
            {
                KSP.UI.Binding.UIValue_ReadNumber_DateTime.DateTime dateTime = KSP.UI.Binding.UIValue_ReadNumber_DateTime.ComputeDateTime(GameManager.Instance.Game.UniverseModel.UniverseTime, 6, 425);
                if (dateTime.Years > saveData.CurrentYear)
                {
                    saveData.CurrentYear = dateTime.Years;
                    UpdateBudget();
                    UpdateDisplay();
                }
            },false);
            if (GameManager.Instance.Game.GlobalGameState.GetGameState().GameState == GameState.VehicleAssemblyBuilder)
            {
                if (KapitalismStatsWindow.rootVisualElement.visible == false)
                {
                    KapitalismStatsWindow.rootVisualElement.visible = true;
                    UpdateDisplay();

                }
                setSpendDisplayMode(true);
                float totalCost = 0;
                float resourceCost = 0;
                Dictionary<string, float> UsedResources = new Dictionary<string, float>();

                TryCatch(() =>
                {
                    ResourceDefinitionDatabase rdd = GameManager.Instance._game.ResourceDefinitionDatabase;
                    GameManager.Instance.Game.OAB.Current.eventsManager.builder.Stats.MainAssembly.Parts.ForEach(part =>
                    {

                        totalCost += GameManager.Instance.Game.Parts._partData[part.PartName].data.cost;
                        TryCatch(() =>
                        {
                            ((Module_ResourceCapacities)part.Modules[typeof(PartComponentModule_ResourceCapacities)]).dataResourceCapacities._propertyContextLookup.ForEach(rd =>
                            {
                                TryCatch(() =>
                                {
                                    if (rdd.IsResourceRecipe(rdd.GetResourceIDFromName(rd.Key)))
                                    {
                                        List<ResourceUnitsPair> unitsOfIngredients = new List<ResourceUnitsPair>();
                                        rdd.GetRecipeIngredientUnits(rdd.GetResourceIDFromName(rd.Key), ((ModuleProperty<float>)rd.Value.properties["EntryProperty"]).storedValue, ref unitsOfIngredients);
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
                                        UsedResources[rd.Key] += ((ModuleProperty<float>)rd.Value.properties["EntryProperty"]).storedValue;
                                    }
                                },false);

                            });
                        },false);

                    });
                    UsedResources.ForEach(kv =>
                    {
                        if (saveData.MaterialStorage[kv.Key] < kv.Value)
                        {
                            float amountToCalc = kv.Value - saveData.MaterialStorage[kv.Key];
                            ResourceDefinitionDatabase.ResourceDefinitionWrapper ResourceDef = rdd._resourceDefinitionWrappers.Find(x => x.resourceID == rdd.GetResourceIDFromName(kv.Key));
                            resourceCost += amountToCalc * ((float)ResourceDef.originalResourceDefinition.Value.costPerUnit * 1000);
                        }
                    });
                    resourceCost = (float)Math.Floor((double)resourceCost);
                },true);
                UpdateSpendDisplay(totalCost, resourceCost);
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

        },false);
    }



}