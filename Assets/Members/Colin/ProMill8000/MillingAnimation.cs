using System.Collections;
using UnityEngine;

namespace ProMill8000
{
    public class MillingAnimation : MonoBehaviour
    {
        [Header("Axis References")]
        [SerializeField] private AxisMovement spindleY;
        [SerializeField] private AxisMovement worktableX;
        [SerializeField] private AxisMovement worktableZ;

        [Header("Simulation Parameters")]
        [SerializeField] private float plungeDepth = -0.15f;
        [SerializeField] private float squareSize = 0.05f;

        private Coroutine _runningSequence;

        public bool IsPlaying => _runningSequence != null;

        public Transform WorktableXTransform => worktableX != null ? worktableX.transform : null;

        public void Play()
        {
            if (_runningSequence != null)
                return;
            _runningSequence = StartCoroutine(MillSequence());
        }

        public void Stop()
        {
            if (_runningSequence != null)
            {
                StopCoroutine(_runningSequence);
                _runningSequence = null;
            }
            spindleY.Stop();
            worktableX.Stop();
            worktableZ.Stop();
        }

        [ContextMenu("Play")]
        private void PlayFromMenu() => Play();

        [ContextMenu("Stop and Reset")]
        private void StopAndReset()
        {
            Stop();
            spindleY.ResetToOrigin();
            worktableX.ResetToOrigin();
            worktableZ.ResetToOrigin();
        }

        private IEnumerator MillSequence()
        {
            // Spindle plunge down
            spindleY.MoveToOffset(plungeDepth);
            yield return WaitUntilStopped(spindleY);

            // Worktable square: forward
            worktableZ.MoveToOffset(squareSize);
            yield return WaitUntilStopped(worktableZ);

            // Worktable square: left
            worktableX.MoveToOffset(-squareSize);
            yield return WaitUntilStopped(worktableX);

            // Worktable square: back
            worktableZ.MoveToOffset(0f);
            yield return WaitUntilStopped(worktableZ);

            // Worktable square: right
            worktableX.MoveToOffset(0f);
            yield return WaitUntilStopped(worktableX);

            // Spindle retract up
            spindleY.MoveToOffset(0f);
            yield return WaitUntilStopped(spindleY);

            _runningSequence = null;
        }

        private IEnumerator WaitUntilStopped(AxisMovement axis)
        {
            while (axis.IsMoving)
                yield return null;
        }
    }
}
