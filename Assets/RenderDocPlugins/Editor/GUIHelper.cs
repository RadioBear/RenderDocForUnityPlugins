using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace RenderDocPlugins
{
    internal static class EnumDataUtility
    {
        internal struct EnumData
        {
            public float maxWidth;
            public string[] displayNames;
        }

        private static readonly Dictionary<Type, EnumData> s_EnumData = new();

        internal static EnumData GetCachedEnumData<T>()
        {
            return GetCachedEnumData(typeof(T));
        }
        internal static EnumData GetCachedEnumData(Type enumType)
        {
            if (s_EnumData.TryGetValue(enumType, out EnumData value))
            {
                return value;
            }

            EnumData enumData = default;
            value = enumData;
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            List<FieldInfo> list = new(fields);
            if (!list.Any())
            {
                string[] array = new string[1] { "" };
                value.displayNames = array;
                return value;
            }

            try
            {
                string location = list.First().Module.Assembly.Location;
                if (!string.IsNullOrEmpty(location))
                {
                    list = list.OrderBy((FieldInfo f) => f.MetadataToken).ToList();
                }
            }
            catch
            {
            }

            value.displayNames = list.Select((FieldInfo f) => EnumNameFromEnumField(f)).ToArray();
            if (value.displayNames.Distinct().Count() != value.displayNames.Length)
            {
                Debug.LogWarning("Enum " + enumType.Name + " has multiple entries with the same display name, this prevents selection in EnumPopup.");
            }

            float maxWidth = 0.0f;
            foreach(var name in value.displayNames)
            {
                var width = EditorStyles.popup.CalcSize(new GUIContent(name)).x;
                maxWidth = Mathf.Max(maxWidth, width);
            }
            value.maxWidth = maxWidth;

            s_EnumData[enumType] = value;
            return value;
        }

        private static string EnumNameFromEnumField(FieldInfo field)
        {
            object[] customAttributes = field.GetCustomAttributes(typeof(InspectorNameAttribute), inherit: false);
            if (customAttributes.Length != 0)
            {
                return ((InspectorNameAttribute)customAttributes.First()).displayName;
            }

            if (field.IsDefined(typeof(ObsoleteAttribute), inherit: false))
            {
                return $"{ObjectNames.NicifyVariableName(field.Name)} (Obsolete)";
            }

            return ObjectNames.NicifyVariableName(field.Name);
        }
    }

    public class GUIHelper
    {
        private static float s_LastLabelWidth;
        private static float s_LastFieldWidth;

        public static float CalcEnumPopupWidth<T>() where T : System.Enum
        {
            var enumData = EnumDataUtility.GetCachedEnumData<T>();
            return enumData.maxWidth;
        }

        public static void PushEnum(GUIContent content, Type enumType)
        {
            var enumData = EnumDataUtility.GetCachedEnumData(enumType);
            s_LastLabelWidth = EditorGUIUtility.labelWidth;
            s_LastFieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(content).x;
            EditorGUIUtility.fieldWidth = enumData.maxWidth;
        }

        public static void PopEnum()
        {
            EditorGUIUtility.labelWidth = s_LastLabelWidth;
            EditorGUIUtility.fieldWidth = s_LastFieldWidth;
        }

        public static void PushGUIWidth(GUIContent content, float fieldWidth)
        {
            s_LastLabelWidth = EditorGUIUtility.labelWidth;
            s_LastFieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(content).x;
            EditorGUIUtility.fieldWidth = fieldWidth;
        }

        public static void PopGUIWidth()
        {
            EditorGUIUtility.labelWidth = s_LastLabelWidth;
            EditorGUIUtility.fieldWidth = s_LastFieldWidth;
        }

        public static T LayoutEnumPopup<T>(GUIContent content, T val, params GUILayoutOption[] options) where T : System.Enum
        {
            PushEnum(content, val.GetType());
            val = (T)EditorGUILayout.EnumPopup(content, val, options);
            PopEnum();
            return val;
        }


        public static void LayoutLineHorizontal(float size, float alpha)
        {
            var rect = GUILayoutUtility.GetLastRect();
            rect = GUILayoutUtility.GetRect(rect.width, size);
            Color c = EditorStyles.label.normal.textColor;
            c.a = alpha;
            EditorGUI.DrawRect(rect, c);
        }

        public static void LayoutTitleDot(GUIStyle style)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, style);
            float min = Mathf.Min(rect.width, rect.height);
            Rect drawRect = new(rect.x + (rect.width - min) * 0.5f, rect.y + (rect.height - min) * 0.5f, min, min);
            EditorGUI.DrawRect(drawRect, style.normal.textColor);
        }
    }
}