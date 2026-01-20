Automation API Signatures v2.0

AutomationAPI.cs

Lifecycle
void Initialize()
void Cleanup()

Player Management
ArenaResult Join(Entity userEntity, int arenaId = 0)
ArenaResult Leave(Entity userEntity)
ArenaResult Spectate(Entity userEntity, int arenaId)

Match Control
ArenaResult StartMatch(int arenaId)
ArenaResult ResetMatch(int arenaId)
ArenaResult ForceEndMatch(int arenaId)

Environment
ArenaResult SpawnPracticeTarget(int arenaId, string prefabName)
ArenaResult ClearEntities(int arenaId)

Loadouts
ArenaResult ApplyLoadout(Entity userEntity, string buildName)
ArenaResult SaveCurrentAsLoadout(Entity userEntity, string customName)
ArenaResult ApplyRandomLoadout(Entity userEntity)
List<LoadoutSummary> GetAvailableLoadouts()
string GetActiveLoadoutName(Entity userEntity)

Team Management
ArenaResult CreateTeam(Entity leaderEntity, string teamName = "")
ArenaResult InviteToTeam(Entity leaderEntity, Entity targetEntity)
ArenaResult JoinTeam(Entity userEntity, int teamId)
ArenaResult LeaveTeam(Entity userEntity)
ArenaResult AutoBalanceTeams(int arenaId)
TeamSummary GetTeamDetails(int teamId)
int GetPlayerTeamId(Entity userEntity)
List<TeamSummary> GetTeamsInArena(int arenaId)

Queries
ArenaStateSummary GetStatus(int arenaId)
List<ArenaSummary> GetAll()

WorldAutomationService.cs

Rule Management
void AddRule(string ruleName, AutomationRule rule)
void RemoveRule(string ruleName)
Dictionary<string, AutomationRule> GetRules()
void ClearAllRules()

Trigger Management
void RegisterTrigger(Entity triggerEntity, string triggerName, float radius = 5f)
void UnregisterTrigger(Entity triggerEntity)
void SetTriggerEnabled(Entity triggerEntity, bool enabled)

Update
void Update(float deltaTime)

AutomationCommands.cs

Script Commands
void ScriptCommand(ChatCommandContext ctx, string action, string args = "")

Workflow Commands
void WorkflowCommand(ChatCommandContext ctx, string action, string args = "")

Smart Commands
void SmartCommand(ChatCommandContext ctx, string action, string target = "", string options = "")

Batch Commands
void BatchCommand(ChatCommandContext ctx, string action, string targets = "", string options = "")

Conditional Commands
void IfCommand(ChatCommandContext ctx, string condition, string action, string elseAction = "")
void WhenCommand(ChatCommandContext ctx, string trigger, string action, string cooldown = "60")

Data Structures

ArenaResult
struct ArenaResult
{
    bool Success;
    string Message;
    static ArenaResult Ok(string message = "Success");
    static ArenaResult Fail(string message);
}

ArenaStateSummary
struct ArenaStateSummary
{
    int ArenaId;
    int PlayerCount;
    string Status;
    float Timer;
}

ArenaSummary
struct ArenaSummary
{
    int ArenaId;
    string Name;
    int PlayerCount;
    bool IsActive;
}

LoadoutSummary
struct LoadoutSummary
{
    string Name;
    string Category;
    string Description;
    string IconPrefab;
}

TeamSummary
struct TeamSummary
{
    int TeamId;
    string TeamName;
    List<ulong> MemberPlatformIds;
    int Score;
    bool IsReady;
}

AutomationRule
class AutomationRule
{
    string TriggerName { get; set; }
    List<AutomationAction> Actions { get; set; } = new();
}

AutomationAction
class AutomationAction
{
    AutomationActionType Type { get; set; }
    string Target { get; set; }
    string EventName { get; set; }
    float3 Position { get; set; }
    string PrefabName { get; set; }
    WorldObjectType ObjectType { get; set; }
    float DelaySeconds { get; set; }
    Dictionary<string, object> Parameters { get; set; } = new();

    float GetDelaySeconds();
    float3 GetSpawnPosition();
    string GetPrefabName();
    WorldObjectType GetObjectType();
}

AutomationActionType
enum AutomationActionType
{
    OpenDoor,
    CloseDoor,
    ToggleDoor,
    LockDoor,
    UnlockDoor,
    SpawnObject,
    DestroyObject,
    Delay,
    TriggerEvent
}

Script
class Script
{
    string Name { get; set; }
    string Description { get; set; }
    string Commands { get; set; }
    bool IsRunning { get; set; }
    DateTime CreatedAt { get; set; }
}

ScriptManager
static class ScriptManager
{
    static Script GetScript(string name);
    static List<Script> GetAllScripts();
    static void CreateScript(string name, string commands);
    static void ExecuteScript(Script script);
}