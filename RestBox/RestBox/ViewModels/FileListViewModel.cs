using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Prism.Events;
using RestBox.Domain.Entities;
using RestBox.Events;

namespace RestBox.ViewModels
{
    public abstract class FileListViewModel<TViewModel> : ViewModelBase<TViewModel>
    {
        #region Declarations
        
        private readonly IEventAggregator eventAggregator; 

        #endregion

        #region Constructor
     
        protected FileListViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
            SolutionLoadedVisibility = Visibility.Hidden;
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(SolutionClosedEvent);
            eventAggregator.GetEvent<DocumentChangedEvent>().Subscribe(DocumentChanged);
        } 

        #endregion
        
        #region Properties

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged("SolutionLoadedVisibility"); }
        } 

        #endregion

        #region Event Handlers

        protected virtual void DocumentChanged(LayoutData layoutData)
        {

        }

        protected virtual void SolutionClosedEvent(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Hidden;
        }

        protected virtual void SolutionLoadedEvent(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Visible;
        } 

        #endregion

        #region Protected Methods

        protected void RaiseDocumentChanged<TUserControl, TUserControlViewModel, TSelectItemEvent, TAddMenuItemsEvent>(
            LayoutData layoutData)
            where TUserControl : UserControl
            where TSelectItemEvent : CompositePresentationEvent<string>, new()
            where TAddMenuItemsEvent : CompositePresentationEvent<TUserControlViewModel>, new()
        {
            if (layoutData != null && layoutData.Content.GetType() == typeof(TUserControl))
            {
                if (layoutData.IsSelected)
                {
                    eventAggregator.GetEvent<TSelectItemEvent>().Publish(layoutData.ContentId);
                    eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutData.Content).DataContext);
                }
            }
        } 

        #endregion
    }
}
