using BepInEx;
using ReLIB;
using Logger = ReLIB.logging.Logger;

namespace Kapitalism;
[BepInPlugin("com.rendevouzrs_entertainment.kapitalism", "Kapitalism", "0.0.3")]
[BepInDependency(ReLIBMod.ModId, ReLIBMod.ModVersion)]
public class KapitalismMod : BaseUnityPlugin
{
    public static string ModId = K.ModId;
    public static string ModName = K.ModName;
    public static string ModVersion = K.ModVersion;

    private Logger logger = new Logger(ModName, ModVersion);//logger logger.log("stuff here")  logger.debug("only run with IsDev=true")  logger.error("error log")

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
    public Dictionary<string, float> MaterialStorage = new Dictionary<string, float>();
    public Dictionary<string,float> DificultyModifier = new Dictionary<string, float>()
    {
        {"budget",1f},
        {"fund",1f},
        {"science",1f}
    };
}