using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryScript : MonoBehaviour
{

    public ToolBarScript ToolbarUI;

    private int currentSelection = 0;

    public byte CurrentBlock => (byte)(currentSelection+1);

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(GameManager.gamePaused)
            return;
        var scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if(scroll != 0){
            currentSelection += (int)(scroll*10);
            currentSelection = (currentSelection % 10 +10)%10;
            ToolbarUI.ChangeSelection(currentSelection);
        }
    }
}
