using System.Windows.Controls;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for RequestExtensions.xaml
    /// </summary>
    public partial class RequestExtensions : UserControl
    {
        #region Constructor

        public RequestExtensions(RequestExtensionsViewModel requestExtensionsViewModel)
        {
            DataContext = requestExtensionsViewModel;
            InitializeComponent();
        } 

        #endregion
    }
}
