using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListBox : MonoBehaviour
{
    public List<string> Items;
    public GameObject ListBoxItemPrefab;

    private ToggleGroup toggleGroup;

    private List<GameObject> listBoxItems;

    void Start()
    {
        listBoxItems = new List<GameObject>();
        toggleGroup = GetComponent<ToggleGroup>();
        foreach(string item in Items){
            CreateListboxControl(item);
        }
    }

    public void AddItem(string item){
        Items.Add(item);
        CreateListboxControl(item);
    }

    public string GetSelected(){
        for(int i = 0; i<listBoxItems.Count; i++){
            if(listBoxItems[i].GetComponent<Toggle>().isOn){
                return Items[i];
            }
        }

        return null;
    }

    public void ClearItems(){
        foreach(GameObject item in listBoxItems){
            Destroy(item);
        }
        listBoxItems.Clear();
        Items.Clear();
    }

    private void CreateListboxControl(string item){
        var lbi = Instantiate(ListBoxItemPrefab);
        listBoxItems.Add(lbi);
        lbi.transform.SetParent(transform.Find("Viewport/Content").transform);
        lbi.transform.Find("Label").GetComponent<Text>().text = item;
        ListBoxItem listbox = lbi.GetComponent<ListBoxItem>();
        lbi.GetComponent<Toggle>().group = toggleGroup;
        listbox.Text = PathToName(item);
    }

    private string PathToName(string path){
        return System.IO.Path.GetFileName(path).Replace('_', ' ');
    }
}
