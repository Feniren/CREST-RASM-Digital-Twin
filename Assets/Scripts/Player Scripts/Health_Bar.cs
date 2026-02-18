using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health_Bar : MonoBehaviour{
    public Slider SliderReference;

    void Start(){
        SliderReference.value = 1.0f;
    }

    public void SetPercent(float Percent){
        SliderReference.value = Percent;
    }
}
