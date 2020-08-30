using UnityEngine;

class Test : MonoBehaviour
{
    [System.Serializable] public class Person { public string Name; public int Age; }

    private enum Colors { Red, Green, Blue }

    public SerializedDictionary<string, int> strIntDic;
    public SerializedDictionary<string, int[]> strIntArDic;

    public SerializedKeyValuePair<string, int>[] kvp;
    public (int, int) Tuple;

    //public SerializedDictionary<Person, int> persIntDic;
    //[SerializeField] private SerializedDictionary<Colors, int> colorIntDic;

    //public SerializedDictionary<Person, int[]> persIntArrayDict;
    //[SerializeField] private int[] ar;

    //void Start()
    //{
    //    Debug.Log(colorIntDic.Count);
    //    foreach (var kvp in colorIntDic)
    //        Debug.Log($"{kvp.Key} : {kvp.Value}");

    //}
}