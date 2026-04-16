using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(IModuleData), true)]
public class IModuleDataDrawer : PropertyDrawer
{
    // Cache các type implement IModuleData
    private static Type[] _moduleTypes;
    private static string[] _moduleTypeNames;

    static IModuleDataDrawer()
    {
        // Tìm tất cả class non-abstract implement IModuleData
        var interfaceType = typeof(IModuleData);
        _moduleTypes = TypeCache.GetTypesDerivedFrom<IModuleData>()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToArray();

        _moduleTypeNames = _moduleTypes.Select(t => t.Name).ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Nếu có object bên trong, lấy chiều cao mặc định + 1 dòng popup
        var objRef = GetManagedReferenceValue(property);
        if (objRef != null)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.singleLineHeight + 4f; // +4f để có chút padding
        }

        // Nếu null, chỉ cần 1 dòng popup
        return EditorGUIUtility.singleLineHeight * 2f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var indent = EditorGUI.indentLevel;

        // Dòng đầu – dropdown chọn type
        var popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        var currentObj = GetManagedReferenceValue(property);
        int currentIndex = -1;

        if (currentObj != null)
        {
            var currentType = currentObj.GetType();
            currentIndex = Array.IndexOf(_moduleTypes, currentType);
        }

        int newIndex = EditorGUI.Popup(popupRect, label.text, currentIndex, _moduleTypeNames);

        // Nếu chọn 1 type khác (hoặc từ null sang 1 type)
        if (newIndex != currentIndex)
        {
            if (newIndex >= 0 && newIndex < _moduleTypes.Length)
            {
                var selectedType = _moduleTypes[newIndex];
                SetManagedReferenceValue(property, Activator.CreateInstance(selectedType));
            }
            else
            {
                // chọn "None" (nếu bạn thêm option None) => set null
                SetManagedReferenceValue(property, null);
            }

            property.serializedObject.ApplyModifiedProperties();
            // Cập nhật lại currentObj sau khi set
            currentObj = GetManagedReferenceValue(property);
        }

        // Nếu có instance, vẽ field con ở dưới
        if (currentObj != null)
        {
            EditorGUI.indentLevel++;

            var contentRect = new Rect(
                position.x,
                popupRect.y + EditorGUIUtility.singleLineHeight,
                position.width,
                position.height - EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);

            EditorGUI.indentLevel = indent;
        }
        EditorGUI.EndProperty();
    }
    // Helpers cho SerializeReference
    private static object GetManagedReferenceValue(SerializedProperty property)
    {
        return property.managedReferenceValue;
    }

    private static void SetManagedReferenceValue(SerializedProperty property, object value)
    {
        property.managedReferenceValue = value;
    }
}