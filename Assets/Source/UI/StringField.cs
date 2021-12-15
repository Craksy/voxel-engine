using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StringField : MonoBehaviour, ISettingsField
{
    public string Label, Placeholder;

    private Text _label;
    public InputField _input;

    public string GetValue => _input.text;

    object ISettingsField.GetValue()
    {
        return _input.text;
    }

    // Start is called before the first frame update
    void Start()
    {
        _label = GetComponentInChildren<Text>();
        _input = GetComponentInChildren<InputField>();
        _label.text = Label;
        _input.transform.Find("Placeholder").gameObject.GetComponent<Text>().text = Placeholder;
    }
}
