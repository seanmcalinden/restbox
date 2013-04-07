using AvalonDock;

namespace RestBox.ApplicationServices
{
    public interface ILayoutApplicationService
    {
        void Load(DockingManager dockingManager);
        void Save(DockingManager dockingManager);
        void Delete();
    }
}
