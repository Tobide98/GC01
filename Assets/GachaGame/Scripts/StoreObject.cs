using UnityEngine;

[System.Serializable]
public class StoreObject
{
    public Sprite icon;
    public string title;
    [TextArea] public string description;
    public int value;
    public int price;
}