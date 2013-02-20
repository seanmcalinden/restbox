using AvalonDock;

namespace RestBox.Services
{
    public interface IApplicationLayout
    {
        void Load(DockingManager dockingManager);
        void Save(DockingManager dockingManager);
    }
}
