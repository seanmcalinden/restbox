using System.Windows.Controls;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for RequestExtensions.xaml
    /// </summary>
    public partial class RequestExtensions
    {
        #region Constructor

        public RequestExtensions(RequestExtensionFilesViewModel requestExtensionFilesViewModel)
        {
            DataContext = requestExtensionFilesViewModel;
            InitializeComponent();
        } 

        #endregion
    }
}
