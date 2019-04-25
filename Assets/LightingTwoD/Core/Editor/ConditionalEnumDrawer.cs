using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LightingTwoD.Core.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalEnumAttribute))]
    public class ConditionalEnumDrawer : PropertyDrawer
    {

        private Func<Enum, bool> checkMethod = null; 
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.type != "Enum")
            {
                EditorGUI.HelpBox(position, "Only Enum-types are supported", MessageType.Error);
                return;
            }

            if(checkMethod == null)
            {
                var o = property.serializedObject.targetObject;
                var containingClass = o.GetType();
                var info = containingClass.GetMethod(((ConditionalEnumAttribute) attribute).checkMethodName, 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                if(info == null)
                {
                    EditorGUI.HelpBox(position, "Check-Method could not be found", MessageType.Error);
                    return;
                }

                if (info.ReturnType != typeof(bool))
                {
                    EditorGUI.HelpBox(position, "Check-Method does not return a bool", MessageType.Error);
                    return;
                }


                checkMethod = e => (bool)info.Invoke(o, new object[]{e});
            }
            
            var enumValue = (Enum)Enum.Parse(fieldInfo.FieldType, property.enumNames[property.enumValueIndex]);
            property.enumValueIndex = (int)(object)EditorGUI.EnumPopup(position, label, enumValue, checkMethod);
        }
    }
}