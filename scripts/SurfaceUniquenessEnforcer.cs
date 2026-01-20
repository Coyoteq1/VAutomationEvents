using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;

namespace VAuto.Scripts
{
    /// <summary>
    /// Surface Uniqueness Enforcement System
    /// Ensures one surface per target method at all times
    /// </summary>
    public static class SurfaceUniquenessEnforcer
    {
        private static ManualLogSource Log => Plugin.Logger;
        
        public class SurfaceReport
        {
            public string Surface { get; set; }
            public string Target { get; set; }
            public string Reason { get; set; }
            public DateTime LastModified { get; set; }
        }
        
        public class CleanupReport
        {
            public List<SurfaceReport> DuplicatesRemoved { get; set; } = new();
            public List<SurfaceReport> SurfacesKept { get; set; } = new();
            public List<SurfaceReport> Conflicts { get; set; } = new();
        }
        
        /// <summary>
        /// Scan entire project for surface definitions and enforce uniqueness
        /// </summary>
        public static CleanupReport EnforceSurfaceUniqueness(string projectRoot)
        {
            var report = new CleanupReport();
            var surfaces = ScanForSurfaces(projectRoot);
            var methodMap = GroupByTarget(surfaces);
            
            Log?.LogInfo($"[SurfaceEnforcer] Found {surfaces.Count} surfaces targeting {methodMap.Count} methods");
            
            foreach (var kvp in methodMap)
            {
                var targetMethod = kvp.Key;
                var duplicates = kvp.Value;
                
                if (duplicates.Count == 1)
                {
                    report.SurfacesKept.Add(duplicates[0]);
                    Log?.LogDebug($"[SurfaceEnforcer] Kept unique surface: {duplicates[0].Surface} -> {targetMethod}");
                    continue;
                }
                
                // Resolve conflict - keep newest
                try
                {
                    var newest = duplicates.OrderByDescending(s => s.LastModified).First();
                    var older = duplicates.Where(s => s != newest).ToList();
                    
                    foreach (var surface in older)
                    {
                        DeleteSurface(surface);
                        report.DuplicatesRemoved.Add(new SurfaceReport
                        {
                            Surface = surface.Surface,
                            Target = surface.Target,
                            Reason = "older",
                            LastModified = surface.LastModified
                        });
                        Log?.LogInfo($"[SurfaceEnforcer] Removed duplicate surface: {surface.Surface} (older than {newest.Surface})");
                    }
                    
                    report.SurfacesKept.Add(newest);
                    Log?.LogInfo($"[SurfaceEnforcer] Kept newest surface: {newest.Surface} -> {targetMethod}");
                }
                catch (Exception ex)
                {
                    report.Conflicts.Add(new SurfaceReport
                    {
                        Surface = duplicates[0].Surface,
                        Target = targetMethod,
                        Reason = $"resolution_error: {ex.Message}",
                        LastModified = DateTime.UtcNow
                    });
                    Log?.LogError($"[SurfaceEnforcer] Conflict resolving {targetMethod}: {ex.Message}");
                }
            }
            
            return report;
        }
        
        /// <summary>
        /// Scan for all surface definitions in project
        /// </summary>
        private static List<SurfaceReport> ScanForSurfaces(string projectRoot)
        {
            var surfaces = new List<SurfaceReport>();
            
            // Scan for Harmony patches
            var patchFiles = Directory.GetFiles(Path.Combine(projectRoot, "Patches"), "*Patch*.cs", SearchOption.AllDirectories);
            foreach (var file in patchFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("[HarmonyPatch("))
                    {
                        var surface = ExtractSurfaceFromLine(line, file, i);
                        if (surface != null)
                        {
                            surfaces.Add(surface);
                        }
                    }
                }
            }
            
            return surfaces;
        }
        
        /// <summary>
        /// Group surfaces by their target method
        /// </summary>
        private static Dictionary<string, List<SurfaceReport>> GroupByTarget(List<SurfaceReport> surfaces)
        {
            var methodMap = new Dictionary<string, List<SurfaceReport>>();
            
            foreach (var surface in surfaces)
            {
                if (!methodMap.ContainsKey(surface.Target))
                {
                    methodMap[surface.Target] = new List<SurfaceReport>();
                }
                methodMap[surface.Target].Add(surface);
            }
            
            return methodMap;
        }
        
        /// <summary>
        /// Extract surface information from a HarmonyPatch line
        /// </summary>
        private static SurfaceReport ExtractSurfaceFromLine(string line, string file, int lineNumber)
        {
            try
            {
                // Extract target method from [HarmonyPatch(typeof(Type), nameof(Method))]
                var start = line.IndexOf("nameof(") + 7;
                var end = line.IndexOf(")", start);
                if (start > 0 && end > start)
                {
                    var targetMethod = line.Substring(start, end - start);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new FileInfo(file);
                    
                    return new SurfaceReport
                    {
                        Surface = fileName,
                        Target = targetMethod,
                        LastModified = fileInfo.LastWriteTime
                    };
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            
            return null;
        }
        
        /// <summary>
        /// Delete a surface file
        /// </summary>
        private static void DeleteSurface(SurfaceReport surface)
        {
            try
            {
                var patchFiles = Directory.GetFiles("Patches", $"{surface.Surface}*.cs", SearchOption.AllDirectories);
                foreach (var file in patchFiles)
                {
                    File.Delete(file);
                    Log?.LogInfo($"[SurfaceEnforcer] Deleted file: {file}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SurfaceEnforcer] Failed to delete {surface.Surface}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if a surface already exists for target method
        /// </summary>
        public static bool SurfaceExists(string targetMethod, string projectRoot = null)
        {
            projectRoot ??= Directory.GetCurrentDirectory();
            var surfaces = ScanForSurfaces(projectRoot);
            return surfaces.Any(s => s.Target == targetMethod);
        }
    }
}
