using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Claw.TData;
//using Claw.Save;
//using Claw.Sample;
//using FlatBuffers;
using System.Linq;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    private void OnEnable()
    {
        instance = this;
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    void Save()
    {

    }
}
