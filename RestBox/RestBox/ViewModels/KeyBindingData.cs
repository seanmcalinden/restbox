using System.Windows.Input;

namespace RestBox.ViewModels
{
    public class KeyBindingData
    {
        public ICommand Command { get; set; }
        public KeyGesture KeyGesture { get; set; }
    }
}
