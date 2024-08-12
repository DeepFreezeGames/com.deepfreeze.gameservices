
using System;

namespace GameServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RegisterServiceAttribute : Attribute
    {
        public int SortOrder { get; }

        public RegisterServiceAttribute(int sortOrder)
        {
            SortOrder = sortOrder;
        }
    }
}