using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListBoxItem : MonoBehaviour
{

    public Color CheckedColor;
    public Color UncheckedColor;
    public string Text;

    public Toggle toggle {get; private set;}
    private Text label;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(onValueChanged);
        label = transform.Find("Label").GetComponent<Text>();
        label.text = Text;

        var val = toggle.isOn;
        toggle.targetGraphic.color = val ? CheckedColor : UncheckedColor;
        label.fontStyle = val ? FontStyle.Bold : FontStyle.Normal;
    }

    void onValueChanged(bool val){
        toggle.targetGraphic.color = val ? CheckedColor : UncheckedColor;
        label.fontStyle = val ? FontStyle.Bold : FontStyle.Normal;
    }
}
