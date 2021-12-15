using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class LoadMenuScript : MonoBehaviour, IMenuPage
{

    public ListBox worldList;

    // Start is called before the first frame update
    void Start()
    {
        UpdateWorldList();
    }

    private void UpdateWorldList(){
        worldList.ClearItems();
        foreach(var world in GetSavedWorlds()){
            worldList.AddItem(world);
        }
    }

    public void SwitchTo(){
        UpdateWorldList();
    }

    public void LoadWorld(){
        var world = worldList.GetSelected();
        Debug.Log("Loading world at " + world);
        SaveManager.CurrentSavePath = world;
        SceneManager.LoadScene("InstancingTest");
    }

    public void OnListValueChange(int val){
        Debug.Log(val);
    }

    private string[] GetSavedWorlds(){
        return Directory.GetDirectories(SaveManager.SavesBasePath);
    }
}
