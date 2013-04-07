using System.Windows;
using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class ViewFile : File
    {
        public ViewFile()
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
