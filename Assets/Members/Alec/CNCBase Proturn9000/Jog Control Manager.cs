using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

public class JogControlManager : MonoBehaviour
{
    [SerializeField] private GameObject toolCarriage;
    private float speed = 0.002117f, stepSize = 0.001f; //default to 5in and 0.001 step w/ continuous step on
    private bool contStep = true;
    private bool jogStatus = false;
    private Vector3 jogDirection;
    
    void Start()
    {
        // StartCoroutine(ContinousJog());
    }

    void Update()
    {
        if(jogStatus)
        {
            if(contStep)
            {
                StartCoroutine(ContinousJog());
                // yield return new WaitForSeconds(1f);
                // toolCarriage.transform.position += jogDirection;
                // print(jogDirection + " jog direction from update");
            }
            else
            {
                StopCoroutine(ContinousJog());
            }  
        }
    }

    IEnumerator ContinousJog()
    {
        while(true)
        {
            yield return new WaitForSeconds(1f);
            
            toolCarriage.transform.position += jogDirection;
        }
    }

    // inches to meters; 1 : 0.0254
    public void changeStepSize(float newStepSize)
    {
        stepSize = newStepSize;
    }
    public void changeSpeed(int newSpeed)
    {
        speed = newSpeed;
    }

    // SetJogStatus sets direction and jogStatus
    public void SetJogStatus(string direction, bool status)
    {
        print(direction);
        CarriageLimiter limiter = toolCarriage.GetComponent<CarriageLimiter>();

        jogStatus = status;
        float x = 0, z = 0;
        switch(direction)
        {
            case "Neg Z":
                z = limiter.zNegLock? 0 : -speed;
                break;
            case "Pos Z":
                z = limiter.zPosLock? 0 : speed;
                print(limiter.zPosLock + " " + z);
                break;
            case "Pos X":
                x = limiter.xPosLock? 0 : speed;
                break;
            case "Neg X":
                x = limiter.xNegLock? 0 : -speed;
                break;
            default:
                print("smth");
                break;
        }
        jogDirection = new Vector3(x, 0, z);
    }
}
