using UnityEngine;

namespace VAuto.UI
{
    public static class ArenaUIManager
    {
        private static bool _uiVisible = false;

        public static void ShowArenaUI()
        {
            if (_uiVisible) return;
            _uiVisible = true;
            MouseBuildingSystem.Instance.EnableBuildMode(
                MouseBuildingSystem.BuildType.Tile, 
                0, 
                "Arena Tile", 
                UISettings.GetColor("Accent")
            );
            Plugin.Logger?.LogInfo("[ArenaUI] Arena UI shown");
        }

        public static void HideArenaUI()
        {
            if (!_uiVisible) return;
            _uiVisible = false;
            MouseBuildingSystem.Instance.DisableBuildMode();
            Plugin.Logger?.LogInfo("[ArenaUI] Arena UI hidden");
        }

        public static bool IsUIVisible => _uiVisible;
    }
}
