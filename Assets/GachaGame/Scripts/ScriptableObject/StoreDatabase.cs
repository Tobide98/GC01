using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoreDatabase", menuName = "Store/Store Database")]
public class StoreDatabase : ScriptableObject
{
    public List<StoreObject> storeItems = new List<StoreObject>();
}