using UnityEngine;
using System.Collections.Generic;
using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SerializedDictionaryEditorAsmDef")]
[Serializable]
public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    //These are the keys and values stored by Unity, and we add them in the dictionary just after serialization
    [SerializeField] internal TKey[] serializedKeys = default;
    [SerializeField] internal TValue[] serializedValues = default;

#if UNITY_EDITOR
    //When the add popup is opened, these temporary fields are drawn. They are not part of the actual final data, and should not be used. 
    //Sadly we cant use HideInInspector because that also makes complex types impossible to draw. 
    [SerializeField] internal TKey editorAddKey;
    [SerializeField] internal TValue editorAddValue;
#endif

    public void OnAfterDeserialize()
    {
        if (serializedKeys != null && serializedValues != null && serializedKeys.Length != serializedValues.Length)
        {
            //If we via debug mode change the count we need to find out which one is right
            //The internal count is an old value, whatever doesnt matches must be the new one
            int newLen = serializedKeys.Length == Count ? serializedValues.Length : serializedKeys.Length;

            Array.Resize(ref serializedKeys, newLen);
            Array.Resize(ref serializedValues, newLen);
        }

        Clear();

        if (serializedKeys != null && serializedValues != null)
        {
            for (int i = 0; i < serializedKeys.Length; i++)
            {
#if UNITY_EDITOR
                if (!ContainsKey(serializedKeys[i]))
#endif
                    Add(serializedKeys[i], serializedValues[i]);
            }
        }
    }

    internal bool hasDuplicateKey = false;
    internal (int,int) duplicateIndices;

    public void OnBeforeSerialize()
    {
        if(serializedKeys != null)
        {
            hasDuplicateKey = false;
            for (int i = 0; i < serializedKeys.Length; i++)
            {
                for (int j = 0; j < serializedKeys.Length; j++)
                {
                    if (i == j) continue;

                    if (serializedKeys[i].Equals(serializedKeys[j]))
                    {
                        hasDuplicateKey = true;

                        duplicateIndices = (i, j);
                        
                        editorAddKey = serializedKeys[i];
                        editorAddValue = serializedValues[i];
                        return;
                    }
                }
            }

        }
        
        serializedKeys = new TKey[Count];
        serializedValues = new TValue[Count];

        int k = 0;
        foreach (var kvp in this)
        {
            serializedKeys[k] = kvp.Key;
            serializedValues[k] = kvp.Value;
            k++;
        }
    }


}
