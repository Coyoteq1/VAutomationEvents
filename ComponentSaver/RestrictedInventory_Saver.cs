using KindredSchematics.Services;
using ProjectM;
using Stunlock.Core;
using System.Text.Json;
using Unity.Entities;

namespace KindredSchematics.ComponentSaver;

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
                Plugin.Logger?.LogDebug($"RestrictedItemType changed: {entityData.RestrictedItemType}");
            }
            
            if (prefabData.RestrictedItemCategory != entityData.RestrictedItemCategory)
            {
                saveData.RestrictedItemCategory = (int)entityData.RestrictedItemCategory;
                hasChanges = true;
                Plugin.Logger?.LogDebug($"RestrictedItemCategory changed: {entityData.RestrictedItemCategory}");
            }

            return hasChanges ? saveData : null;
        }
        catch (System.Exception ex)
        {
            Plugin.Logger?.LogError($"Error diffing RestrictedInventory components: {ex.Message}");
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

            Plugin.Logger?.LogDebug($"Saved RestrictedInventory: Type={data.RestrictedItemType}, Category={data.RestrictedItemCategory}");
            return saveData;
        }
        catch (System.Exception ex)
        {
            Plugin.Logger?.LogError($"Error saving RestrictedInventory component: {ex.Message}");
            return null;
        }
    }

    public override void ApplyComponentData(Entity entity, JsonElement jsonData, Entity[] entitiesBeingLoaded)
    {
        try
        {
            var saveData = jsonData.Deserialize<RestrictedInventory_Save>(SchematicService.GetJsonOptions());

            if (saveData == null)
            {
                Plugin.Logger?.LogWarning("Failed to deserialize RestrictedInventory save data");
                return;
            }

            if (!entity.Has<RestrictedInventory>())
            {
                entity.Add<RestrictedInventory>();
                Plugin.Logger?.LogDebug("Added RestrictedInventory component to entity");
            }

            var data = entity.Read<RestrictedInventory>();

            // Safe application of RestrictedItemType
            if (saveData.RestrictedItemType.HasValue)
            {
                data.RestrictedItemType = saveData.RestrictedItemType.Value;
                Plugin.Logger?.LogDebug($"Applied RestrictedItemType: {saveData.RestrictedItemType.Value}");
            }

            // Safe application of RestrictedItemCategory with enum validation
            if (saveData.RestrictedItemCategory.HasValue)
            {
                var categoryValue = saveData.RestrictedItemCategory.Value;
                if (Enum.IsDefined(typeof(ItemCategory), categoryValue))
                {
                    data.RestrictedItemCategory = (ItemCategory)categoryValue;
                    Plugin.Logger?.LogDebug($"Applied RestrictedItemCategory: {(ItemCategory)categoryValue}");
                }
                else
                {
                    Plugin.Logger?.LogWarning($"Invalid ItemCategory value: {categoryValue}, using default");
                    data.RestrictedItemCategory = default;
                }
            }
            else
            {
                // Ensure we have a valid default if no category was specified
                data.RestrictedItemCategory = default;
            }

            entity.Write(data);
            Plugin.Logger?.LogDebug("Successfully applied RestrictedInventory component data");
        }
        catch (JsonException ex)
        {
            Plugin.Logger?.LogError($"Failed to deserialize RestrictedInventory JSON data: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying RestrictedInventory component data: {ex.Message}");
        }
    }
}












