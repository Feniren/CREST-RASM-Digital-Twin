// Implemented by whatever in a module shows the trainee a component instead of asking
// them to find it — Module 1 glides the part in from the parts table. The highlighters
// keep quiet for those components so the demonstration is the only thing drawing the eye;
// components with no demonstration still get their marker highlight as usual.
//
// Found with GetComponent, so the implementor sits on the same object as the highlighter
// (the module's Lesson_Manager).
public interface Step_Demonstrator{
    bool Demonstrates(string markerId);
}
