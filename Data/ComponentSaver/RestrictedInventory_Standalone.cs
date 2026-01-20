using ProjectM;
using Stunlock.Core;
using System.Text.Json;
using Unity.Entities;

namespace VAutomation.ComponentSaver;

[ComponentType(typeof(RestrictedInventory))]
class RestrictedInventory_Saver : ComponentSaver
{
    struct RestrictedInventory_Save
    {
        public PrefabGUID? RestrictedItemType { get; set; }
        public int? RestrictedItemCategory { get; set; }
    }

    public override object DiffComponents(Entity prefab, Entity entity, EntityMapper entityMapper)
    {
        try
        {
            var prefabData = prefab.Read<RestrictedInventory>();
            var entityData = entity.Read<RestrictedInventory>();

            var saveData = new RestrictedInventory_Save();
            
            bool hasChanges = false;
            
            if (prefabData.RestrictedItemType != entityData.RestrictedItemType)
            {
                saveData.RestrictedItemType = entityData.RestrictedItemType;
                hasChanges = true;
            }
            
            if (prefabData.RestrictedItemCategory != entityData.RestrictedItemCategory)
            {
                saveData.RestrictedItemCategory = (int)entityData.RestrictedItemCategory;
                hasChanges = true;
            }

            return hasChanges ? saveData : null;
        }
        catch (System.Exception ex)
        {
            // Use direct ProjectM logging if available, otherwise silent fail
            ProjectM.Plugin.Logger?.LogError($"Error diffing RestrictedInventory components: {ex.Message}");
            return null;
        }
    }

    public override object SaveComponent(Entity entity, EntityMapper entityMapper)
    {
        try
        {
            var data = entity.Read<RestrictedInventory>();

            var saveData = new RestrictedInventory_Save()
            {
                RestrictedItemType = data.RestrictedItemType,
                RestrictedItemCategory = (int)data.RestrictedItemCategory
            };

            return saveData;
        }
        catch (System.Exception ex)
        {
            ProjectM.Plugin.Logger?.LogError($"Error saving RestrictedInventory component: {ex.Message}");
            return null;
        }
    }

    public override void ApplyComponentData(Entity entity, JsonElement jsonData, Entity[] entitiesBeingLoaded)
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                WriteIndented = false 
            };
            
            var saveData = JsonSerializer.Deserialize<RestrictedInventory_Save>(jsonData.GetRawText(), options);

            if (saveData == null)
            {
                ProjectM.Plugin.Logger?.LogWarning("Failed to deserialize RestrictedInventory save data");
                return;
            }

            if (!entity.Has<RestrictedInventory>())
            {
                entity.Add<RestrictedInventory>();
            }

            var data = entity.Read<RestrictedInventory>();

            // Safe application of RestrictedItemType
            if (saveData.RestrictedItemType.HasValue)
            {
                data.RestrictedItemType = saveData.RestrictedItemType.Value;
            }

            // Safe application of RestrictedItemCategory with enum validation
            if (saveData.RestrictedItemCategory.HasValue)
            {
                var categoryValue = saveData.RestrictedItemCategory.Value;
                if (Enum.IsDefined(typeof(ItemCategory), categoryValue))
                {
                    data.RestrictedItemCategory = (ItemCategory)categoryValue;
                }
                else
                {
                    ProjectM.Plugin.Logger?.LogWarning($"Invalid ItemCategory value: {categoryValue}, using default");
                    data.RestrictedItemCategory = default;
                }
            }
            else
            {
                // Ensure we have a valid default if no category was specified
                data.RestrictedItemCategory = default;
            }

            entity.Write(data);
        }
        catch (JsonException ex)
        {
            ProjectM.Plugin.Logger?.LogError($"Failed to deserialize RestrictedInventory JSON data: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            ProjectM.Plugin.Logger?.LogError($"Error applying RestrictedInventory component data: {ex.Message}");
        }
    }
}












