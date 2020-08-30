using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;
using System.Text;

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
                duplicateIndices = ReflectionUtils.GetPrivate<(int, int)>(targetObject, nameof(SerializedDictionary<int, int>.duplicateIndices));

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
        var serializedDataProp = property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.serializedData));
        listDrawer = new ReorderableList(property.serializedObject, serializedDataProp, true, false, true, true);
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
            SerializedProperty kvpProp = serializedDataProp.GetArrayElementAtIndex(index),
                keyProp = kvpProp.FindPropertyRelative(nameof(SerializedKeyValuePair<int, int>.Key)),
                valProp = kvpProp.FindPropertyRelative(nameof(SerializedKeyValuePair<int, int>.Value));

            GUIContent keyContent = new GUIContent(keyProp.hasVisibleChildren ? keyProp.displayName : string.Empty, keyProp.tooltip),
                    valContent = new GUIContent(valProp.hasVisibleChildren ? valProp.displayName : string.Empty, valProp.tooltip);

            var keyRect = rect;
            keyRect.width = rect.width / 2f - 24f;
            var valueRect = rect;
            valueRect.x = rect.width / 2f + 24f;
            valueRect.width = rect.width / 2f + 13f;

            //They have a foldout, move a bit
            if (keyProp.hasChildren)
            {
                keyRect.x += 14f;
                keyRect.width -= 14f;
            }
            if (valProp.hasChildren)
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
            EditorGUI.PropertyField(keyRect, keyProp, keyContent, true);

            GUI.enabled = !duplicateKeyMode;
            GUI.color = Color.white;

            EditorGUIUtility.labelWidth = valueRect.width / 4f;
            EditorGUI.PropertyField(valueRect, valProp, valContent, true);
        };
        listDrawer.onAddDropdownCallback += (Rect btn, ReorderableList l) =>
        {
            SerializedProperty editorAddKeyProp = property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.editorAddKey)),
                                editorAddValProp = property.FindPropertyRelative(nameof(SerializedDictionary<int, int>.editorAddValue));

            PopupWindow.Show(btn, new AddElementDrawer(
                editorAddKeyProp, editorAddValProp,
                fieldInfo.GetValue(property.serializedObject.targetObject)));
        };
        listDrawer.elementHeightCallback += (int index) =>
        {
            SerializedProperty kvpProp = serializedDataProp.GetArrayElementAtIndex(index),
                keyProp = kvpProp.FindPropertyRelative(nameof(SerializedKeyValuePair<int, int>.Key)),
                valProp = kvpProp.FindPropertyRelative(nameof(SerializedKeyValuePair<int, int>.Value));

            return Mathf.Max(EditorGUI.GetPropertyHeight(keyProp), EditorGUI.GetPropertyHeight(valProp)) + EditorGUIUtility.standardVerticalSpacing;
        };
    }
}

class AddElementDrawer : PopupWindowContent
{
    private readonly SerializedProperty keyProp, valProp;
    private object targetDictionary;
    private readonly Vector2 minWindowSize;
    

    public AddElementDrawer(SerializedProperty keyProp, SerializedProperty valProp, object targetDictionary)
    {
        this.targetDictionary = targetDictionary;
        this.keyProp = keyProp;
        this.valProp = valProp;

        keyProp.isExpanded = valProp.isExpanded = true;

        minWindowSize = new Vector2(350,
            EditorGUI.GetPropertyHeight(keyProp) +
            EditorGUI.GetPropertyHeight(valProp) +
            EditorGUIUtility.singleLineHeight * 2);
    }

    //todo: better size
    public override Vector2 GetWindowSize() => Vector2.Max(minWindowSize, new Vector2(minWindowSize.x,
            EditorGUI.GetPropertyHeight(keyProp) +
            EditorGUI.GetPropertyHeight(valProp) +
            EditorGUIUtility.singleLineHeight * 2));

    public override void OnGUI(Rect rect)
    {
        EditorGUIUtility.labelWidth = 75f;
        GUI.SetNextControlName("Key");
        EditorGUILayout.PropertyField(keyProp, new GUIContent("Key", keyProp.tooltip), true);

        GUI.SetNextControlName("Value");
        EditorGUILayout.PropertyField(valProp, new GUIContent("Value", valProp.tooltip), true);

        //Important, we need to apply the propertyfields to the actual object before reading it out!
        keyProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

        object key = ReflectionUtils.GetPrivate<object>(targetDictionary, nameof(SerializedDictionary<int, int>.editorAddKey)),
            value = ReflectionUtils.GetPrivate<object>(targetDictionary, nameof(SerializedDictionary<int, int>.editorAddValue));

        bool isDuplicateKey = key != null && ReflectionUtils.CallPublic<bool>(targetDictionary, nameof(SerializedDictionary<int, int>.ContainsKey), key);

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
    
        if(firstOnGUI)
        {
            firstOnGUI = false;
            GUI.FocusControl("Key");
        }
    }

    private bool firstOnGUI;
    public override void OnOpen() { firstOnGUI = true; }
    public override void OnClose() { }
}

internal static class ReflectionUtils
{
    internal static T GetPrivate<T>(object obj, string name) => (T)obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(obj);
    internal static T CallPublic<T>(object obj, string name, params object[] args) => (T)obj.GetType().GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(obj, args);
    internal static void CallPublic(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(obj, args);

    internal static string DumpObjectProps(SerializedProperty prop)
    {
        var sb = new StringBuilder();
        sb.Append($"{prop.name}:\n");
        prop.Next(true);

        do
        {
            for (int i = 0; i < prop.depth; i++)
                sb.Append('\t');
            sb.Append($"{prop.name} {prop.isArray} ({prop.type})\n");
        } while (prop.Next(true));
        return sb.ToString();
    }
}