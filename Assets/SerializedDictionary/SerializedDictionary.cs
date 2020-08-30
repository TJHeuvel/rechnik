using UnityEngine;
using System.Collections.Generic;
using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SerializedDictionaryEditorAsmDef")]
[Serializable]
public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{

    /*
     * These are the keys and values stored by Unity, and we add them in the dictionary just after serialization.
     * 
     * Why KeyValuePair instead of arrays (TKey[], TValue[])?
     * Because with arrays you wouldnt be able to have an array of keys or values. E.g. given TValue of int[] it would end up as int[][], which Unity cant serialize.
     * One added benefit is that if you'd add a new row manually in the debug-inspector there is no mismatch between keys and values. 
     */
    [SerializeField] internal SerializedKeyValuePair<TKey, TValue>[] serializedData = default;

    //Was a duplicate key detected in the source data, in the editor? 
    [SerializeField] internal bool hasDuplicateKey;

#if UNITY_EDITOR
    //When the add popup is opened, these temporary fields are drawn. They are not part of the actual final data, and should not be used. 
    //Sadly we cant use HideInInspector because that also makes complex types impossible to draw. 
    //https://issuetracker.unity3d.com/issues/drawing-serialized-properties-with-hideininspector-attribute-only-works-for-some-variable-types
    [SerializeField] internal TKey editorAddKey;
    [SerializeField] internal TValue editorAddValue;

    internal (int, int) duplicateIndices;
#endif

    public void OnAfterDeserialize()
    {
        Clear();

        if (serializedData == null) return;

#if !UNITY_EDITOR
        if (hasDuplicateKey)
            Debug.LogWarning($"A serialized dictionary is loading that has duplicate keys configured in the inspector.\nThis is not supported, the duplicate value will be missing in a dictionary of type: '{this}'. Known keys and values:\n{string.Join("\n\t", serializedData)}");
#endif
        for (int i = 0; i < serializedData.Length; i++)
            if (!hasDuplicateKey || !ContainsKey(serializedData[i].Key))
                Add(serializedData[i].Key, serializedData[i].Value);
    }
    public void OnBeforeSerialize()
    {
        if (serializedData == null) return;

#if UNITY_EDITOR
        for (int i = 0; i < serializedData.Length; i++)
        {
            for (int j = 0; j < serializedData.Length; j++)
            {
                if (i == j) continue;

                if (serializedData[i].Key.Equals(serializedData[j].Key))
                {
                    hasDuplicateKey = true;
                    duplicateIndices = (i, j);
                    return;
                }
            }
        }
        hasDuplicateKey = false;
#endif

        serializedData = new SerializedKeyValuePair<TKey, TValue>[Count];
        int k = 0;
        foreach (var kvp in this)
            serializedData[k++] = kvp;
    }
}

[Serializable]
public struct SerializedKeyValuePair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public SerializedKeyValuePair(TKey key, TValue value)
    {
        this.Key = key;
        this.Value = value;
    }

    public static implicit operator KeyValuePair<TKey, TValue>(SerializedKeyValuePair<TKey, TValue> data) => new KeyValuePair<TKey, TValue>(data.Key, data.Value);
    public static implicit operator SerializedKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> data) => new SerializedKeyValuePair<TKey, TValue>(data.Key, data.Value);

    public override string ToString() => $"key: {Key}, value: {Value}";
}