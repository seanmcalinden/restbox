using System.Windows.Controls;
using RestBox.UserControls;

namespace RestBox.ViewModels.NoLayoutFiles
{
    public class NoLayoutUserControl : UserControl, ITabUserControlBase
    {
        public string FilePath { get; set; }
    }
}