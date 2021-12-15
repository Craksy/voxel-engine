using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Vox;

public class MainMenuController : MonoBehaviour
{
    // Start is called before the first frame update

    public CanvasGroup CurrentMenu;

    void Start() {
        GridManager.ChunkShape = new Vector3Int(128, 128, 128);
        CurrentMenu.alpha = 1f;
        CurrentMenu.interactable = true;
        CurrentMenu.blocksRaycasts = true;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ChangeMenu(CanvasGroup menu){
        menu.alpha = 1f;
        menu.interactable = true;
        menu.blocksRaycasts = true;
        CurrentMenu.alpha = 0f;
        CurrentMenu.interactable = false;
        CurrentMenu.blocksRaycasts = false;
        CurrentMenu = menu;
        ExecuteEvents.Execute<IMenuPage>(CurrentMenu.gameObject, null, (x,y) => x.SwitchTo());
    }

    private string[] GetSavedWorlds(){
        var path = Application.dataPath + "/saves/";
        return Directory.GetDirectories(path); 
    }

    public void ExitGame(){
        Application.Quit(0);
    }

}

public interface IMenuPage : IEventSystemHandler
{
    void SwitchTo();
}