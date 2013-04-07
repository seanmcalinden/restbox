namespace RestBox.ViewModels
{
    public interface ISave
    {
        void SaveAs(string id, object content);
        void Save(string id, object content);
        bool IsDirty { get; set; }
    }
}
