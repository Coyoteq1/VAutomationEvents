CFGconguration fi.JSONsuppot has ben mpletely emoved - only CFG fl are usedama.cfgggvumcfg-specficauoma.cfg using CFG formatii[Core]#Enabl risble hentiVAuto pluginE= 
#Loglevel:D, Inf,Waning ErrorL= Info

# Enabl deogg fo troublehotigDebugMode=ale[A]#Enablearenasystm
E= 
#Arenacoordinates(formatx,y,z)
Center = -
#Arenaradiusinmetes
R= 7
#Proximityentryradiusin mters
E= 7
#Proximityexitradiusin mters
E= 100
#Position iinsondChckrval= 5
[Comds]#EchamamentcommandsEChaer= 
#Enl lodcommandsERlad= 
#EnableservicegmntcommandsEeSvic =

[Map]#E map on symEIcn =
#Mp ico rfhitv iscondsRefrsh =.
#Showplyer sonSowNms=t
[Rspw]#respwnpevto system
 =tru
#Dflt coodowninscod
Cooldo =2[Gam]#E VBlood hoks franaEVooHks =
#Eck verrdes franaEUlckOverrdes =[Db]#EdaabaspiencE =#e typSQLiJSON,Non
 =
#De fil prelivtosrvrdioy =databe.db[Perfomce]#Maxentites proces perrmMxEntiiPerFrame= 20
#EnntitypigEig= 
#Garbagecolctin interva in secdGCInterval=120AeaConfgurai(gg.vaumati.e.)
Aen-specc sttingii[Aea]#Ifrublocksheifex th lowedoneRestritShahftIAre = fasAllwedheifPefbGUIDhah(wolf)WofShpehifGuiHsh =9734
#Arec 'x,y z'C =-0, 5 -500
#AdisRdu =50#Addmvi SrvrScpMappr.TyAddInvenoyItmUseSrvrAdIms =
#Iftru, uo-qup pracice stimsEquipPrcceSe =
#BloodMd iygopGUIDhaBloodMendAbilGuidHash =-199624149
#EBood Mndooldown ret idaren
BoodMendBootEnabld=trueCoow applid oBooMed nside aen (n secd)BlodMedCooldown = 5555#Comma-spaated lst fVBloodGUIDhhes o unlock in rna
VBlooGuids= -1905777458,-1541423745,1851788208,-1329110591,1847352945,-1590401994,1160276395,-1509336394,-1795594768,-1076936144,-1401860033,1078672536,-1187560001,1853359340,-1621277536,-1780181910,1347047030,1543730011,1988464088,-1483028122,1697326906,-1605152814,-1597889736,-1530880053,-1079955773,-1689014385,-1527506989,-1792005748,1923355014,1992354530,1848924077,1354701753,-1593545835,1986872945,-1073562590,-1524133843,-1804774346,-1076805011,-1520760697,1990783396,1984295249,-1527375858,1987427478,-1083328867,1980939331,-1086702013,-1534253200,-1783555056,1977566185,-1080086906,1974213039,-1811441032,-1083459999,-1537626346,-1086833146,-1544796892,-1090629,-1814816710,-1093579438,-1548169903,-109952584,-1821589100,-1100325730,1824962246,21028279,1634988459,1895760153,1892387007,1889013861,1885630715,1882257569,1878884423,1875511277,1872138131,1868764985,1865391839,1862018693,1858645547,85522401,1851899255,184852619,184515263,181779817,18386671,1835335251831660379#Aezneonfiguas. Format'Nme|x,y,z|nterRaius|zneRdius|Name'Ze= Arena|-1000,5,-500|25|50

GlbalMapcervc#Enableglobl ap icnsevic - sowsal plysonmapwith3-secondupdt
Ebed = rue
#Mapiconupdnteva scondsUpdateInterval=3.0#Prefabfrmp nPrefabName=MIc_CaleObjectBllar
#Showmapiconsforplyes in nmalaeas
ShwNomaPlays = ru
#Showmp ion fpayrs in rezos
ShowAraPlayrs = true
#ShowmapiconsforpayrnPvP zne
owPvPPays=trueMyPlginInf.PLUGIN_GUIDVAutoDirVAutoDirAccssChepr methodVaustringt, strn key, srg defaultValue = "")bl GetBoolsction,sting ky,booldefautVlue= falspublicstti int GetCInt(sting sec,sringkey, int defaultValue = 0)flt GtFloatsti, strnky,fodfulVau = 0f)float3 GetConfigFloat3(strng ecion, ky, floa3 dfulVauIsdEnableVuIse.Valusring urnt.VaIsdle.Vauer
{
    gt
    {
        var coodsArenaCenter.Vae.Spl(',');
        f (coords.Leth == 3)
        {
            rurn nw floa3(floa.Parecoords[0], floatPas(coords[1]), flotParse(coords[2]));
        }
        rturn ew floa3(-1000, 5, -500)
    }
}Value.ValueValue.ValueValue.ValuealVueal.VueIsdle.VauValue.ValuIsdEbValIsdDaaba.VluValue.Value Accesscnfigurtovalue dirclyiEnabld.IEnabldPlugidGetvauaeaRadusGetFloa("Aea", Radu, 50fAenardiu:aeRadi}Chk debug mde
f (Plg.IsDebugMode){
    Loggr?.LDebugDeg moe iactve"}
AccssconfigurtiondrctoriecfgDirVAuDrdataDr= Plug.VAutDataDr;Cdroy:{onfDr}"Da dreory:{daDr}Cfiu UpdatesUpdcfr us(ifded)Pluin.Rds.Vlu75f;
Plg.LgLvelVluDbug;
//Cnfguraisaly svdby BpInExCofuronMr
HandleprsCFGsuomulesouc.

###BpIExCEnt
- CofiEntry<bool>fo boolean alu
-CEnty<srg>for rvalu
- CEnty<t>fr iteg valunfigEy<>fo fouU CFGheper mhodBpIEx Entry propertiesty-afeacsmissg valueitpprprenhlrmehds**IMPORTAN:** JSON uppt hs beecomplelymovd.Tflowingchange ar emnen:

### Removd JSON~~Settings.json → amatin
- ~~Victor2.json, Victor3.json, Victor4.jsonRemovd (functionaliy moved o CFG)
- ~~Sapchat~~ →Remvd(funality moved to CFG~~builds.json → gg.vatomain.a
- ~~zone.jsongg.vauomaon.area.cf
- ~~snaphot~~ →gg.vautomation.ena.cfg

### Removd JSON APIs
- ~~Plugi.LodJsonConfig<T>()~~ →Ue Plugin.GCfigValue(methodsPlgin.aJonConi<T>() UseBpInEx ConfigEnry properies
- ~~PlugGetJonCfigFiles()~~ → Removedno JSON file)
- ~~Plugin.GetSchmatFil()~~ → Removed (no JSONchmacs
### New CFG-Only System-  nowndld through CFGfils
- BpIExonfigurati sytem prves type sfey an automaticsavg
- N JSONdependencis or parsingreqired
- .gitigore blocks all JSON les to prevntr-inroducon

**The CFG-oly sytem i nw permanent adenced t the reposiory level**