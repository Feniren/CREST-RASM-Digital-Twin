using System.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

using static GlobalVariables;

public class JogControlManager : MonoBehaviour
{
    [SerializeField] private GameObject toolCarriage;
    private float speed = 0.05f, stepSize = 0.001f; //default to 5in and 0.001 step w/ continuous step on
    private bool contStep = true;
    private bool jogStatus = false;
    private Vector3 jogDirection = new Vector3(0,0,0);
    private CarriageLimiter limiter;

    void Start()
    {
        limiter = toolCarriage.GetComponent<CarriageLimiter>();
    }
    void OnEnable()
    {
        StartCoroutine(ContinousJog());
        limiter.ApplyLimits += ApplyCarriageLimit;
    }

    void OnDisable()
    {
        StopCoroutine(ContinousJog());
        limiter.ApplyLimits -= ApplyCarriageLimit;
    }

    public void ApplyCarriageLimit(object? sender, LimitEventArgs e)
    {
        float 
        x = toolCarriage.transform.position.x,
        y = toolCarriage.transform.position.y,
        z = toolCarriage.transform.position.z;

        switch(e.direction)
        {
            case negZ:
                jogDirection.z = 0;
                z = limiter.negLimit.z;
                break;
            case posZ:
                jogDirection.z = 0;
                z = limiter.posLimit.z;
                break;
            case negX:
                jogDirection.x = 0;
                x = limiter.negLimit.x;
                break;
            case posX:
                jogDirection.x = 0;
                x = limiter.posLimit.x;
                break;
        }
        toolCarriage.transform.position = new Vector3(x,y,z);
    }

    IEnumerator ContinousJog()
    {
        while(true)
        {
            yield return new WaitForSeconds(1f);
            
            if(jogStatus)
            {
                if(contStep)
                {
                    // yield return new WaitForSeconds(1f);
                    toolCarriage.transform.position += jogDirection;
                }
                else
                {
                    //handles non continous step
                    toolCarriage.transform.position += jogDirection;
                    jogDirection = new Vector3(0,0,0);
                }
            }
        }
    }

    // inches to meters; 1 : 0.0254
    public void ChangeStepSize(string newStepSize)
    {
        if(newStepSize.Equals(cont))
        {
            contStep = true;
        }
        else if(float.TryParse(newStepSize, out float floatVal))
        {
            contStep = false;
            stepSize = floatVal;
        }
        else
        {
            contStep = false;
            stepSize = 0f;
        }
    }
    public void ChangeSpeed(int newSpeed)
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
            case negZ:
                z = limiter.zNegLock? 0 : -speed;
                break;
            case posZ:
                z = limiter.zPosLock? 0 : speed;
                print(limiter.zPosLock + " " + z);
                break;
            case posX:
                x = limiter.xPosLock? 0 : speed;
                break;
            case negX:
                x = limiter.xNegLock? 0 : -speed;
                break;
            default:
                print("smth");
                break;
        }
        jogDirection = new Vector3(x, 0, z);
    }
}
