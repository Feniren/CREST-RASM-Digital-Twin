// Implement this on a class in an Editor folder to plug a training module into
// "Training/0 Build Everything". Implementations are discovered automatically
// (TypeCache) — no shared file needs editing to add a module.
//
// Start from Assets/Scripts/Training/Editor/Module_Builder_Template.cs.txt and
// see docs/VR_Modules/05_Module_Framework_HOWTO.md.
public interface ITraining_Module_Builder{
    // Build order and Bootstrap menu position (M1 = 0, M2 = 1, ...).
    int Order { get; }

    // Build the module's lesson asset(s) and scene, and call
    // Training_Builder_Core.RegisterModule(lesson, scenePath) so the Bootstrap
    // menu, build settings, and play redirect pick the module up.
    void Build();
}
