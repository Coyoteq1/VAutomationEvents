using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using VAuto.Core;
using VAuto.Utilities;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Build JSON service for managing arena building data with JSON persistence
    /// Integrates with the service architecture and provides build lifecycle management
    /// </summary>
    public class BuildJsonService
    {
        private static BuildJsonService _instance;
        private readonly Dictionary<string, BuildData> _activeBuilds;
        private readonly Dictionary<string, BuildTemplate> _templates;
        private readonly string _buildDataPath;
        private readonly string _templatesPath;

        public static BuildJsonService Instance => _instance ??= new BuildJsonService();

        private BuildJsonService()
        {
            _activeBuilds = new Dictionary<string, BuildData>();
            _templates = new Dictionary<string, BuildTemplate>();
            _buildDataPath = Path.Combine(Plugin.JsonPath, "Builds");
            _templatesPath = Path.Combine(Plugin.JsonPath, "Templates");
            
            InitializeDirectories();
            LoadExistingData();
        }

        #region Initialization

        /// <summary>
        /// Initialize required directories
        /// </summary>
        private void InitializeDirectories()
        {
            try
            {
                if (!Directory.Exists(_buildDataPath))
                {
                    Directory.CreateDirectory(_buildDataPath);
                    Plugin.Logger?.LogInfo($"[BuildJsonService] Created builds directory: {_buildDataPath}");
                }

                if (!Directory.Exists(_templatesPath))
                {
                    Directory.CreateDirectory(_templatesPath);
                    Plugin.Logger?.LogInfo($"[BuildJsonService] Created templates directory: {_templatesPath}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error initializing directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Load existing build data and templates
        /// </summary>
        private void LoadExistingData()
        {
            try
            {
                // Load active builds
                if (Directory.Exists(_buildDataPath))
                {
                    var buildFiles = Directory.GetFiles(_buildDataPath, "*.json");
                    foreach (var file in buildFiles)
                    {
                        var buildData = BuildJson.LoadBuildData(file);
                        if (buildData != null)
                        {
                            _activeBuilds[buildData.Id] = buildData;
                        }
                    }
                    Plugin.Logger?.LogInfo($"[BuildJsonService] Loaded {_activeBuilds.Count} active builds");
                }

                // Load templates
                if (Directory.Exists(_templatesPath))
                {
                    var templateFiles = Directory.GetFiles(_templatesPath, "*.json");
                    foreach (var file in templateFiles)
                    {
                        var template = BuildJson.LoadTemplate(file);
                        if (template != null)
                        {
                            _templates[template.Id] = template;
                        }
                    }
                    Plugin.Logger?.LogInfo($"[BuildJsonService] Loaded {_templates.Count} templates");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error loading existing data: {ex.Message}");
            }
        }

        #endregion

        #region Build Management

        /// <summary>
        /// Create new build data
        /// </summary>
        public BuildData CreateBuild(string arenaId, string buildName, Entity user)
        {
            try
            {
                var buildData = BuildJson.CreateBuildData(arenaId, buildName, user);
                _activeBuilds[buildData.Id] = buildData;

                // Save to file
                var filePath = Path.Combine(_buildDataPath, $"{buildData.Id}.json");
                BuildJson.SaveBuildData(buildData, filePath);

                Plugin.Logger?.LogInfo($"[BuildJsonService] Created build '{buildName}' for arena '{arenaId}'");
                return buildData;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error creating build: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get build data by ID
        /// </summary>
        public BuildData GetBuild(string buildId)
        {
            return _activeBuilds.TryGetValue(buildId, out var build) ? build : null;
        }

        /// <summary>
        /// Get all builds for an arena
        /// </summary>
        public List<BuildData> GetArenaBuilds(string arenaId)
        {
            var builds = new List<BuildData>();
            foreach (var build in _activeBuilds.Values)
            {
                if (build.ArenaId == arenaId)
                {
                    builds.Add(build);
                }
            }
            return builds;
        }

        /// <summary>
        /// Update build data
        /// </summary>
        public bool UpdateBuild(BuildData buildData)
        {
            try
            {
                if (!_activeBuilds.ContainsKey(buildData.Id))
                    return false;

                // Validate data
                if (!BuildJson.ValidateBuildData(buildData))
                {
                    Plugin.Logger?.LogWarning($"[BuildJsonService] Invalid build data for build '{buildData.Id}'");
                    return false;
                }

                _activeBuilds[buildData.Id] = buildData;

                // Save to file
                var filePath = Path.Combine(_buildDataPath, $"{buildData.Id}.json");
                BuildJson.SaveBuildData(buildData, filePath);

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error updating build: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete build data
        /// </summary>
        public bool DeleteBuild(string buildId)
        {
            try
            {
                if (!_activeBuilds.Remove(buildId))
                    return false;

                // Delete file
                var filePath = Path.Combine(_buildDataPath, $"{buildId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                Plugin.Logger?.LogInfo($"[BuildJsonService] Deleted build '{buildId}'");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error deleting build: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add structure to build
        /// </summary>
        public bool AddStructure(string buildId, StructureData structure)
        {
            var build = GetBuild(buildId);
            if (build == null)
                return false;

            BuildJson.AddStructure(build, structure);
            return UpdateBuild(build);
        }

        /// <summary>
        /// Remove structure from build
        /// </summary>
        public bool RemoveStructure(string buildId, string structureId)
        {
            var build = GetBuild(buildId);
            if (build == null)
                return false;

            var removed = BuildJson.RemoveStructure(build, structureId);
            return removed && UpdateBuild(build);
        }

        /// <summary>
        /// Update structure status
        /// </summary>
        public bool UpdateStructureStatus(string buildId, string structureId, StructureStatus status)
        {
            var build = GetBuild(buildId);
            if (build == null)
                return false;

            var updated = BuildJson.UpdateStructureStatus(build, structureId, status);
            return updated && UpdateBuild(build);
        }

        #endregion

        #region Template Management

        /// <summary>
        /// Create build template
        /// </summary>
        public BuildTemplate CreateTemplate(BuildData buildData, string templateName, string description = "")
        {
            try
            {
                var template = BuildJson.CreateTemplate(buildData, templateName, description);
                _templates[template.Id] = template;

                // Save to file
                var filePath = Path.Combine(_templatesPath, $"{template.Id}.json");
                BuildJson.SaveTemplate(template, filePath);

                Plugin.Logger?.LogInfo($"[BuildJsonService] Created template '{templateName}'");
                return template;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error creating template: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get template by ID
        /// </summary>
        public BuildTemplate GetTemplate(string templateId)
        {
            return _templates.TryGetValue(templateId, out var template) ? template : null;
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        public List<BuildTemplate> GetAllTemplates()
        {
            return new List<BuildTemplate>(_templates.Values);
        }

        /// <summary>
        /// Get templates for arena
        /// </summary>
        public List<BuildTemplate> GetArenaTemplates(string arenaId)
        {
            var templates = new List<BuildTemplate>();
            foreach (var template in _templates.Values)
            {
                if (template.ArenaId == arenaId)
                {
                    templates.Add(template);
                }
            }
            return templates;
        }

        /// <summary>
        /// Apply template to build
        /// </summary>
        public bool ApplyTemplate(string buildId, string templateId)
        {
            var build = GetBuild(buildId);
            var template = GetTemplate(templateId);

            if (build == null || template == null)
                return false;

            try
            {
                BuildJson.ApplyTemplate(build, template);
                return UpdateBuild(build);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error applying template: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete template
        /// </summary>
        public bool DeleteTemplate(string templateId)
        {
            try
            {
                if (!_templates.Remove(templateId))
                    return false;

                // Delete file
                var filePath = Path.Combine(_templatesPath, $"{templateId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                Plugin.Logger?.LogInfo($"[BuildJsonService] Deleted template '{templateId}'");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error deleting template: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Statistics and Reporting

        /// <summary>
        /// Get build statistics
        /// </summary>
        public BuildStatistics GetStatistics()
        {
            var stats = new BuildStatistics
            {
                TotalBuilds = _activeBuilds.Count,
                TotalTemplates = _templates.Count,
                ActiveBuilds = 0,
                CompletedBuilds = 0,
                FailedBuilds = 0
            };

            foreach (var build in _activeBuilds.Values)
            {
                switch (build.Status)
                {
                    case BuildStatus.InProgress:
                        stats.ActiveBuilds++;
                        break;
                    case BuildStatus.Completed:
                        stats.CompletedBuilds++;
                        break;
                    case BuildStatus.Failed:
                        stats.FailedBuilds++;
                        break;
                }
            }

            return stats;
        }

        /// <summary>
        /// Get arena build statistics
        /// </summary>
        public BuildStatistics GetArenaStatistics(string arenaId)
        {
            var arenaBuilds = GetArenaBuilds(arenaId);
            var stats = new BuildStatistics
            {
                TotalBuilds = arenaBuilds.Count,
                TotalTemplates = GetArenaTemplates(arenaId).Count,
                ActiveBuilds = 0,
                CompletedBuilds = 0,
                FailedBuilds = 0
            };

            foreach (var build in arenaBuilds)
            {
                switch (build.Status)
                {
                    case BuildStatus.InProgress:
                        stats.ActiveBuilds++;
                        break;
                    case BuildStatus.Completed:
                        stats.CompletedBuilds++;
                        break;
                    case BuildStatus.Failed:
                        stats.FailedBuilds++;
                        break;
                }
            }

            return stats;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup old builds and templates
        /// </summary>
        public void CleanupOldData(int maxAgeDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
                var removedCount = 0;

                // Cleanup old builds
                var buildsToRemove = new List<string>();
                foreach (var build in _activeBuilds.Values)
                {
                    if (build.CreatedAt < cutoffDate && 
                        (build.Status == BuildStatus.Completed || build.Status == BuildStatus.Failed))
                    {
                        buildsToRemove.Add(build.Id);
                    }
                }

                foreach (var buildId in buildsToRemove)
                {
                    if (DeleteBuild(buildId))
                        removedCount++;
                }

                // Cleanup old templates (keep public templates)
                var templatesToRemove = new List<string>();
                foreach (var template in _templates.Values)
                {
                    if (template.CreatedAt < cutoffDate && !template.IsPublic)
                    {
                        templatesToRemove.Add(template.Id);
                    }
                }

                foreach (var templateId in templatesToRemove)
                {
                    if (DeleteTemplate(templateId))
                        removedCount++;
                }

                if (removedCount > 0)
                {
                    Plugin.Logger?.LogInfo($"[BuildJsonService] Cleaned up {removedCount} old items");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Save all data to files
        /// </summary>
        public void SaveAllData()
        {
            try
            {
                // Save all builds
                foreach (var build in _activeBuilds.Values)
                {
                    var filePath = Path.Combine(_buildDataPath, $"{build.Id}.json");
                    BuildJson.SaveBuildData(build, filePath);
                }

                // Save all templates
                foreach (var template in _templates.Values)
                {
                    var filePath = Path.Combine(_templatesPath, $"{template.Id}.json");
                    BuildJson.SaveTemplate(template, filePath);
                }

                Plugin.Logger?.LogInfo($"[BuildJsonService] Saved all data to files");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildJsonService] Error saving all data: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Build statistics data
    /// </summary>
    public class BuildStatistics
    {
        public int TotalBuilds { get; set; }
        public int TotalTemplates { get; set; }
        public int ActiveBuilds { get; set; }
        public int CompletedBuilds { get; set; }
        public int FailedBuilds { get; set; }
    }
}
