using System;

namespace Unity.Entities
{
    /// <summary>
    /// UpdateInGroupAttribute stub for compilation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UpdateInGroupAttribute : Attribute
    {
        public Type GroupType { get; }

        public UpdateInGroupAttribute(Type groupType)
        {
            GroupType = groupType;
        }
    }

    /// <summary>
    /// UpdateAfterAttribute stub for compilation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute
    {
        public Type SystemType { get; }

        public UpdateAfterAttribute(Type systemType)
        {
            SystemType = systemType;
        }
    }

    /// <summary>
    /// UpdateBeforeAttribute stub for compilation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute
    {
        public Type SystemType { get; }

        public UpdateBeforeAttribute(Type systemType)
        {
            SystemType = systemType;
        }
    }
}
