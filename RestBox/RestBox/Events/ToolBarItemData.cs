using System.Windows;
using System.Windows.Input;

namespace RestBox.Events
{
    public class ToolBarItemData
    {
        public ToolBarItemType ToolBarItemType { get; set; }
        public Visibility Visibility { get; set; }
        public ICommand Command { get; set; }
    }
}