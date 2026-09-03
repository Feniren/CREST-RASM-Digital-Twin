using System;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

using static GlobalVariables;

public class CarriageLimiter : MonoBehaviour
{
    /**
    ONLY CONSIDERING FREE JOG, NC RUN + VERIFY SIM WILL NOT REQUIRE THIS AS OF YET
    **/
    public Vector3 posLimit = GlobalVariables.posLimit , 
    negLimit = GlobalVariables.negLimit;
    private Vector3 carriagePosition;
    public bool xNegLock = false, xPosLock = false, zNegLock = false, zPosLock = false;
    // Update is called once per frame
    void Start()
    {
        carriagePosition = this.gameObject.transform.position;
        this.posLimit = GlobalVariables.posLimit + carriagePosition;
        this.negLimit = GlobalVariables.negLimit + carriagePosition;
    }
    void Update()
    {
        carriagePosition = this.gameObject.transform.position;
        LimitChecking();
    }

    // z and x axis locking can freeze with disregard for +/- but error messages still need differentiating if to be added
    private void LimitChecking()
    {
        if(carriagePosition.z > posLimit.z)
        {
           zPosLock = true; 
           LimitEventArgs args = new LimitEventArgs();
           args.direction = posZ;
           OnApplyLimits(args);
        }
        else
        {
            zPosLock = false;
        }

        if(carriagePosition.z < negLimit.z)
        {
           zNegLock = true; 
           LimitEventArgs args = new LimitEventArgs();
           args.direction = negZ;
           OnApplyLimits(args);
        }
        else
        {
            zNegLock = false;
        }

        if(carriagePosition.x > posLimit.x)
        {
           xPosLock = true; 
           LimitEventArgs args = new LimitEventArgs();
           args.direction = posX;
           OnApplyLimits(args);
        }
        else
        {
            xPosLock = false;
        }

        if(carriagePosition.x < negLimit.x)
        {
           xNegLock = true; 
           LimitEventArgs args = new LimitEventArgs();
           args.direction = negX;
           OnApplyLimits(args);
        }
        else
        {
            xNegLock = false;
        }
    }

    protected virtual void OnApplyLimits(LimitEventArgs e)
    {
        ApplyLimits?.Invoke(this, e);
    }

    public event EventHandler<LimitEventArgs>? ApplyLimits;
}

public class LimitEventArgs : EventArgs
{
    public string direction { get; set; }
}
