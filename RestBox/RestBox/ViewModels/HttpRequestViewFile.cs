using System.Windows;

namespace RestBox.ViewModels
{
    public class HttpRequestViewFile : HttpRequestFile
    {
        public HttpRequestViewFile()
            : base()
        {
            NameVisibility = Visibility.Visible;
            EditableNameVisibility = Visibility.Collapsed;     
        }

        private string icon;
        public string Icon
        {
            get { return icon; }
            set { icon = value; OnPropertyChanged("Icon"); }
        }

        private Visibility editableNameVisibility;
        public Visibility EditableNameVisibility
        {
            get { return editableNameVisibility; }
            set { editableNameVisibility = value; OnPropertyChanged("EditableNameVisibility"); }
        }

        private Visibility nameVisibility;
        public Visibility NameVisibility
        {
            get { return nameVisibility; }
            set { nameVisibility = value; OnPropertyChanged("NameVisibility"); }
        }
    }
}
