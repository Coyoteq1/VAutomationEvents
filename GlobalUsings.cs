// Global using directives to make extensions and stubs available throughout the project
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Unity.Entities;
global using Unity.Mathematics;
global using Unity.Transforms;
global using ProjectM;
global using ProjectM.Network;
global using PrefabGUID = ProjectM.PrefabGUID;
global using FixedString64Bytes = Unity.Collections.FixedString64Bytes;
global using BuildData = ProjectM.Build.BuildData;
global using BuildTemplate = ProjectM.Build.BuildTemplate;
global using StructureData = ProjectM.Structures.StructureData;
global using StructureStatus = ProjectM.Structures.StructureStatus;
global using PermissionLevel = ProjectM.Permissions.PermissionLevel;

// VRising mod extensions and stubs
global using VAuto.Extensions;

// Core namespaces
global using VAuto.Core;
global using VAuto.Services;
global using VAuto.Services.Systems;
global using VAuto.Services.Interfaces;
global using VAuto.Automation;
global using VAuto.Commands;