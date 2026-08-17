using UnityEngine;

public class JogControlManager : MonoBehaviour
{
    private int speed;
    private float stepSize;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void changeStepSize(float newStepSize)
    {
        stepSize = newStepSize;
    }
    public void changeSpeed(int newSpeed)
    {
        speed = newSpeed;
    }

    public void Jog(string direction)
    {
        switch(direction)
        {
            case "Neg Z":
                break;
            case "Pos Z":
                break;
            case "Pos X":
                break;
            case "Neg X":
                break;

        }
    }
}
