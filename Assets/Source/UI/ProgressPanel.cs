using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressPanel : MonoBehaviour
{
    public Slider slider;
    public Text label;

    public void UpdateProgress(float progress){
        slider.value = progress;
    }

    public void UpdateProgress(float progress, string message){
        slider.value = progress;
        label.text = message;
    }
}
