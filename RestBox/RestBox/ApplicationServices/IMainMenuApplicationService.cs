using System.Windows.Controls;

namespace RestBox.ApplicationServices
{
    public interface IMainMenuApplicationService
    {
        void CreateInitialMenuItems();
        void InsertSeparator(MenuItem parent, int position);
        void InsertMenuItem(MenuItem parent, MenuItem menuItem, int position);
        void InsertTopLevelMenuItem(MenuItem menuItem, int position);
        void RemoveItem(MenuItem parent, int position);
        MenuItem Get(string name);
        MenuItem Get(MenuItem parent, string headerText);
        MenuItem GetChild(MenuItem parent, string name);
        void LoadSolutionMenus();
        void DisableSolutionMenus();
        void ResetFileMenu();
    }
}
