using RestBox.ViewModels;

namespace RestBox.Events
{
    public class IsDirtyData
    {
        public object Document { get; set; }
        public bool IsDirty { get; set; }

        public IsDirtyData(object document, bool isDirty)
        {
            Document = document;
            IsDirty = isDirty;
        }
    }
}