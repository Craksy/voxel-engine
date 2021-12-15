using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolBarScript : MonoBehaviour
{

    public List<Transform> Slots;
    public Transform SelectionHighlight;
    public RenderTexture texture;
    public IconMesh iconMesh;

    private int currentSelection;

    // Start is called before the first frame update
    void Start()
    {
        SelectionHighlight.position = Slots[currentSelection].position;
        iconMesh.Init();
        for(int i = 0; i<Slots.Count;i++){
            Slots[i].GetComponentInChildren<RawImage>().texture = iconMesh.staticTextures[i];
        }
        ChangeSelection(0);
    }

    public void ChangeSelection(int n){
        Slots[currentSelection].GetComponentInChildren<RawImage>().texture = iconMesh.staticTextures[currentSelection];
        currentSelection = n;
        iconMesh.Redraw(currentSelection+1);
        SelectionHighlight.SetParent(Slots[currentSelection]);
        ((RectTransform)SelectionHighlight).anchorMin = Vector2.zero;
        ((RectTransform)SelectionHighlight).anchorMax = Vector2.one;
        ((RectTransform)SelectionHighlight).anchoredPosition = Vector2.zero;
        ((RectTransform)SelectionHighlight).sizeDelta = Vector2.zero;
        Slots[currentSelection].GetComponentInChildren<RawImage>().texture = iconMesh.rtext;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
