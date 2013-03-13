using System.Windows.Controls;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public interface IMainMenuApplicationService
    {
        void CreateInitialMenuItems(ShellViewModel shellViewModel);
        void InsertSeparator(MenuItem parent, int position);
        void InsertMenuItem(MenuItem parent, MenuItem menuItem, int position);
        void InsertTopLevelMenuItem(ShellViewModel shellViewModel, MenuItem menuItem, int position);
        void RemoveItem(MenuItem parent, int position);
        MenuItem Get(MenuItem parent, string headerText);
    }
}
