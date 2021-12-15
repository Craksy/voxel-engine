using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vox.WorldGeneration;

public class CreateWorldEvent : MonoBehaviour
{

    public InputField WorldTxt;
    public Dropdown WorldType;
    public InputField Seed;
    public MainMenuController Main;

    public void CreateWorld(){
        WorldGenConfig conf = new WorldGenConfig()
        {
            WorldName = FormatWorldName(WorldTxt.text),
            WorldType = WorldType.options[WorldType.value].text,
        };
    }

    private string FormatWorldName(string name){
        return name.Trim(' ');
    }
}
