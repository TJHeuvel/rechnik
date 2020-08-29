using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
class SerializedDictionaryDrawer : PropertyDrawer
{
    private ReorderableList listDrawer;

    private bool duplicateKeyMode;
    private (int, int) duplicateIndices;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded && listDrawer != null)
            height += listDrawer.GetHeight();

        return height;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

        if (property.isExpanded)
        {
            if (listDrawer == null)
                createListDrawer(property);

            var targetObject = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (duplicateKeyMode = ReflectionUtils.GetPrivate<bool>(targetObject, nameof(SerializedDictionary<int, int>.hasDuplicateKey)))
            {
                duplicateIndices = ReflectionUtils.GetPrivate<(int, int)>(targetObject, nameof(SerializedDictionary<int, int>.duplicateIndices));
            }

            position.y += EditorGUIUtility.singleLineHeight;

            GUI.enabled = !duplicateKeyMode;
            listDrawer.DoList(position);


            if (duplicateKeyMode)
            {
                GUI.enabled = true;
                position.y += listDrawer.GetHeight() - EditorGUIUtility.singleLineHeight;
                position.height = EditorGUIUtility.singleLineHeight;
                position.xMax -= 80f;
                EditorGUI.HelpBox(position, $"There are duplicate keys, please ensure they are unique!", MessageType.Error);

                //return;
            }
        }
    }

    private void createListDrawer(SerializedProperty property)
    {
        SerializedProperty keys = property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.serializedKeys)),
                                values = property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.serializedValues));

        listDrawer = new ReorderableList(property.serializedObject, keys, true, false, true, true);
        listDrawer.drawHeaderCallback += (rect) =>
        {
            var keyRect = rect;
            keyRect.x = rect.x + 15f;
            keyRect.width = rect.width / 2f - 4f;
            var valueRect = rect;
            valueRect.x = rect.width / 2f + 15f;
            valueRect.width = rect.width / 2f - 4f;

            GUI.Label(keyRect, "Keys");
            GUI.Label(valueRect, "Values");
        };
        listDrawer.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty key = keys.GetArrayElementAtIndex(index),
                                val = values.GetArrayElementAtIndex(index);
            GUIContent keyContent = new GUIContent(key.hasVisibleChildren ? key.displayName : string.Empty, key.tooltip),
                    valContent = new GUIContent(val.hasVisibleChildren ? val.displayName : string.Empty, val.tooltip);

            var keyRect = rect;
            keyRect.width = rect.width / 2f - 24f;
            var valueRect = rect;
            valueRect.x = rect.width / 2f + 24f;
            valueRect.width = rect.width / 2f + 13f;

            //They have a foldout, move a bit
            if (key.hasChildren)
            {
                keyRect.x += 14f;
                keyRect.width -= 14f;
            }
            if (val.hasChildren)
            {
                valueRect.x += 14f;
                valueRect.width -= 14f;
            }

            bool isDuplicate = duplicateKeyMode &&
                            (duplicateIndices.Item1 == index ||
                            duplicateIndices.Item2 == index);

            GUI.enabled = !duplicateKeyMode || isDuplicate;
            if (isDuplicate) GUI.color = Color.yellow;

            EditorGUIUtility.labelWidth = keyRect.width / 4f;
            EditorGUI.PropertyField(keyRect, key, keyContent, true);

            GUI.enabled = !duplicateKeyMode;
            GUI.color = Color.white;

            EditorGUIUtility.labelWidth = valueRect.width / 4f;
            EditorGUI.PropertyField(valueRect, val, valContent, true);
        };
        listDrawer.onAddDropdownCallback += (Rect btn, ReorderableList l) =>
        {
            PopupWindow.Show(btn, new AddElementDrawer(property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.editorAddKey)),
                property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.editorAddValue)),
                fieldInfo.GetValue(property.serializedObject.targetObject)));
        };
        listDrawer.onReorderCallbackWithDetails += (ReorderableList list, int oldIndex, int newIndex) =>
        {
            //For some reason we only have to move the values, not the keys. 
            values.MoveArrayElement(oldIndex, newIndex);
        };
        listDrawer.elementHeightCallback += (int index) =>
        {
            SerializedProperty key = keys.GetArrayElementAtIndex(index),
                                val = values.GetArrayElementAtIndex(index);

            return Mathf.Max(EditorGUI.GetPropertyHeight(key), EditorGUI.GetPropertyHeight(val)) + EditorGUIUtility.standardVerticalSpacing;
        };
    }
}

class AddElementDrawer : PopupWindowContent
{
    private readonly SerializedProperty keyProp, valProp;
    private object targetDictionary;

    public AddElementDrawer(SerializedProperty keyProp, SerializedProperty valProp, object targetDictionary)
    {
        this.targetDictionary = targetDictionary;
        this.keyProp = keyProp;
        this.valProp = valProp;

        keyProp.isExpanded = valProp.isExpanded = true;

        windowSize = new Vector2(300,
            EditorGUI.GetPropertyHeight(keyProp) +
            EditorGUI.GetPropertyHeight(valProp) +
            EditorGUIUtility.singleLineHeight * 2);
    }
    private readonly Vector2 windowSize;

    //todo: better size
    public override Vector2 GetWindowSize() => windowSize;

    public override void OnGUI(Rect rect)
    {
        EditorGUIUtility.labelWidth = 60f;
        GUI.SetNextControlName("Key");
        EditorGUILayout.PropertyField(keyProp, new GUIContent("Key", keyProp.tooltip), true);

        GUI.SetNextControlName("Value");
        EditorGUILayout.PropertyField(valProp, new GUIContent("Value", valProp.tooltip), true);

        ////Ensure we have focus
        //if (!new[] { "Key", "Value" }.Contains(GUI.GetNameOfFocusedControl()))
        //    GUI.FocusControl("Key");

        //Important, we need to apply the propertyfields to the actual object before reading it out!
        keyProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

        object key = ReflectionUtils.GetPrivate<object>(targetDictionary, nameof(SerializedDictionary<int, int>.editorAddKey)),
            value = ReflectionUtils.GetPrivate<object>(targetDictionary, nameof(SerializedDictionary<int, int>.editorAddValue));

        bool isDuplicateKey = ReflectionUtils.CallPublic<bool>(targetDictionary, nameof(SerializedDictionary<int, int>.ContainsKey), key);

        EditorGUILayout.Space();
        GUI.enabled = !isDuplicateKey;

        if (isDuplicateKey)
            GUI.color = Color.red;
        else
            GUI.color = Color.white;

        if (GUILayout.Button("Add") ||
            (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)) //add clicked or enter pressed
        {
            ReflectionUtils.CallPublic(targetDictionary, nameof(SerializedDictionary<int, int>.Add), key, value);

            editorWindow.Close();
        }
        GUI.enabled = true;
    }

    public override void OnOpen() { }
    public override void OnClose() { }
}

internal static class ReflectionUtils
{
    internal static T GetPrivate<T>(object obj, string name) => (T)obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(obj);
    internal static T CallPublic<T>(object obj, string name, params object[] args) => (T)obj.GetType().GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(obj, args);
    internal static void CallPublic(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(obj, args);

}