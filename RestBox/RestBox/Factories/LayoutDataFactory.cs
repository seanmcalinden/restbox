using RestBox.ViewModels;

namespace RestBox.Factories
{
    public class LayoutDataFactory : ILayoutDataFactory
    {
        #region Public Methods

        public LayoutData Create(string contentId, object content, bool isActive, bool isSelected)
        {
            return new LayoutData
                       {
                           ContentId = contentId,
                           Content = content,
                           IsActive = isActive,
                           IsSelected = isSelected
                       };
        } 

        #endregion
    }
}
