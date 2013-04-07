using RestBox.ViewModels;

namespace RestBox.Factories
{
    public interface ILayoutDataFactory
    {
        LayoutData Create(string contentId, object content, bool isActive, bool isSelected);
    }
}