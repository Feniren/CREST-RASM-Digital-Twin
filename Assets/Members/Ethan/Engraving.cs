using System;
using UnityEngine;

public class Engraving : MonoBehaviour
{
	public String text_data = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void Receive(string EngraveText)
	{
		if(text_data == null)
			text_data = EngraveText;
		else
			text_data += EngraveText;
	}
}
