using UnityEngine;
using UnityEngine.InputSystem;

public class ASRSArmTester : MonoBehaviour
{
    [SerializeField] private ASRSArmController armController;

    [Header("Targets")]
    [SerializeField] private float zTarget = 2f;
    [SerializeField] private float yTarget = 1f;
    [SerializeField] private float yawTarget = 90f;
    [SerializeField] private float xTarget = 1f;

    [Header("Reset")]
    [SerializeField] private float resetPosition = 0f;

    private void Update()
    {
        if (Keyboard.current == null || armController == null)
            return;

        // Z Axis
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("Moving Z");
            armController.MoveZ(zTarget);
        }

        // Y Axis
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("Moving Y");
            armController.MoveY(yTarget);
        }

        // Rotation around Y
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Rotating Yaw");
            armController.RotateY(yawTarget);
        }

        // X Axis
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("Moving X");
            armController.MoveX(xTarget);
        }

        // Reset everything
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Resetting");

            armController.MoveZ(resetPosition);
            armController.MoveY(resetPosition);
            armController.RotateY(0f);
            armController.MoveX(resetPosition);
        }

        // Stop all movement
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Stopping");
            armController.StopAll();
        }
    }
}