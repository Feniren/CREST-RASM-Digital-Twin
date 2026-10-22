using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Screen_Fader : MonoBehaviour{
    [SerializeField] private Image FadeImage;
    [SerializeField] private float Duration = 0.4f;

    public IEnumerator Fade_Out(){
        yield return Fade_To(1f);
    }

    public IEnumerator Fade_In(){
        yield return Fade_To(0f);
    }

    private IEnumerator Fade_To(float target){
        FadeImage.enabled = true;
        Color color = FadeImage.color;
        float start = color.a;
        float time = 0f;

        while (time < Duration){
            time += Time.deltaTime;
            color.a = Mathf.Lerp(start, target, time / Duration);
            FadeImage.color = color;
            yield return null;
        }

        color.a = target;
        FadeImage.color = color;
        FadeImage.enabled = target > 0f;
    }
}
