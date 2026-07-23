using System;
using UnityEngine;

// Base class for a module's demo-step driver. The shared Lesson_Sequencer calls
// Play() for every step kind that isn't Info / Select_Component / Panel_Action;
// the module's subclass runs its demo (animation, coroutine, ...) and calls
// Raise_Demo_Finished() when done so the sequencer advances.
public abstract class Training_Demo_Controller : MonoBehaviour{
    public event Action Demo_Finished;

    public abstract void Play(Lesson_Step step);

    protected void Raise_Demo_Finished(){
        Demo_Finished?.Invoke();
    }
}
