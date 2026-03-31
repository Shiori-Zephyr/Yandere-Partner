using Dalamud.Configuration;

namespace YanderePartner;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Enabled = true;
    public bool PopupEnabled = true;
    public bool Dissociation = false;

    public string PartnerName = "Yuno";
    public float MessageCooldown = 30f;

    public bool SeparationAnxiety = true;
    public bool SepTerritoryChanged = true;
    public bool SepLogout = true;
    public bool SepBetweenAreas = true;
    public bool SepMounted = true;
    public bool SepMountedDismount = true;
    public bool SepInFlight = true;

    public bool Possessiveness = true;
    public bool PosTellReceived = true;
    public bool PosPartyChanged = true;
    public bool PosCfPop = true;
    public bool PosDutyStarted = true;
    public bool PosRepairRequest = true;
    public bool PosEmoteReceived = true;

    public bool Evaluation = true;
    public bool EvaDutyCompleted = true;
    public bool EvaDeath = true;
    public bool EvaDutyWiped = true;
    public bool EvaDutyRecommenced = true;
    public bool EvaLootObtained = true;

    public bool Surveillance = true;
    public bool SurFishing = true;
    public bool SurCrafting = true;
    public bool SurCraftFinished = true;
    public bool SurGathering = true;
    public bool SurGPose = true;
    public bool SurPerformance = true;
    public bool SurGearsetChange = true;
    public bool SurGearsetUpdate = true;
    public bool SurGlamour = true;
    public bool SurSummoningBell = true;
    public bool SurRetainerSale = true;
    public bool SurCutscene = true;
    public bool SurTripleTriad = true;
    public bool SurWeatherChange = true;

    public bool Outburst = true;
    public bool OutPvpKill = false;
    public bool OutCritDh = true;
    public bool OutHealOther = true;
    public bool OutHealCrit = true;
    public bool OutFateEnter = true;
    public bool OutFateLeave = true;

    public bool SpecialContent = true;
    public bool SpcDeepDungeon = true;
    public bool SpcOceanFishing = true;
    public bool SpcChocoboRacing = true;
    public bool SpcGcTurnin = true;
    public bool SpcLeve = true;
    public bool SpcIslandSanctuary = true;
    public bool SpcCosmicExploration = true;
    public bool SpcFCWorkshop = true;
    public bool SpcSpectralCurrent = true;

    public bool Equipment = true;
    public bool EqpLowDurability = true;
    public bool EqpRepair = true;
    public bool EqpSpiritbondFull = true;
}
