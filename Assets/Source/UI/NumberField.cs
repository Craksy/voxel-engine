using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NumberField : MonoBehaviour, ISettingsField
{

    public string Label, Placeholder;
    public bool EnableSlider;

    private System.Type contentType;

    private Text _label, _placeholder;
    public InputField _input;
    private Slider _slider;
    public System.Reflection.FieldInfo field;

    public RangeAttribute range;

    // Start is called before the first frame update
    void Start()
    {
        contentType = field.FieldType;
        _label = transform.Find("Label").GetComponent<Text>();
        _label.text = field.Name;
        _input = GetComponentInChildren<InputField>();
        _input.transform.Find("Placeholder").GetComponent<Text>().text = Placeholder;
        _input.contentType = contentType == typeof(int) ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber;

        range = (RangeAttribute)System.Attribute.GetCustomAttribute(field, typeof(RangeAttribute));
        _slider = GetComponentInChildren<Slider>();
        if(range!=null){
            _slider.minValue = range.min;
            _slider.maxValue = range.max;
            _slider.onValueChanged.AddListener(SliderValueChanged);
            _slider.value = float.Parse(_input.text);
            _slider.wholeNumbers = contentType == typeof(int);
        }else{
            _slider.gameObject.SetActive(false);
            ((RectTransform)transform).sizeDelta -= new Vector2Int(0, 20);
        }
    }

    private void SliderValueChanged(float value){
        _input.text = value.ToString();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public object GetValue()
    {
        if(contentType == typeof(float)){
            return float.Parse(_input.text);
        }
        return int.Parse(_input.text);
    }
}