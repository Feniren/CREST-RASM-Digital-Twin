using Unity.VisualScripting;
using UnityEngine;

public class CarriageLimiter : MonoBehaviour
{
    /**
    ONLY CONSIDERING FREE JOG, NC RUN + VERIFY SIM WILL NOT REQUIRE THIS AS OF YET
    **/
    private Vector3 posLimit = new Vector3(0,0,0), negLimit = new Vector3(-3.35f, 0, -12.25f);
    private Vector3 carriagePosition;
    public bool xNegLock = false, xPosLock = false, zNegLock = false, zPosLock = false;
    // Update is called once per frame
    void Start()
    {
        carriagePosition = this.gameObject.transform.position;
    }
    void Update()
    {
        // zPosLock = (carriagePosition.z > posLimit.z)? true : false;
        // zNegLock = (carriagePosition.z < negLimit.z)? true : false;
        // xPosLock = (carriagePosition.x > posLimit.x)? true : false;
        // xNegLock = (carriagePosition.x < negLimit.x)? true : false;
    }

    private void LimitCheck()
    {
    }
}
