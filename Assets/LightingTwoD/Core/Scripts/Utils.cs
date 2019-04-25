using UnityEngine;

namespace LightingTwoD.Core
{
    public class ReadOnlyAttribute : PropertyAttribute{}

    public class ConditionalEnumAttribute : PropertyAttribute
    {
        public readonly string checkMethodName;

        public ConditionalEnumAttribute(string checkMethodName)
        {
            this.checkMethodName = checkMethodName;
        }
    }

    /// <summary>
    /// Used to mark an 'int' field as a sorting layer so it will use the SortingLayerDrawer to display in the Inspector window.
    /// </summary>
    public class SortingLayerAttribute : PropertyAttribute
    {
        public bool isReadOnly = false;
    }
}