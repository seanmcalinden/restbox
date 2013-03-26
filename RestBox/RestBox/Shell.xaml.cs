using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Factories;
using RestBox.UserControls;
using RestBox.ViewModels;

namespace RestBox
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell
    {
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly ILayoutDataFactory layoutDataFactory;
        private readonly IEventAggregator eventAggregator;
        private readonly ShellViewModel shellViewModel;

        public Shell(
            ShellViewModel shellViewModel, 
            IEventAggregator eventAggregator, 
            IMainMenuApplicationService mainMenuApplicationService,
            ILayoutDataFactory layoutDataFactory)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.layoutDataFactory = layoutDataFactory;
            this.eventAggregator = eventAggregator;
            this.shellViewModel = shellViewModel;
            DataContext = shellViewModel;
            shellViewModel.ApplicationTitle = "REST Box";
            
            InitializeComponent();

            HttpRequestFilesLayout.Content = ServiceLocator.Current.GetInstance<HttpRequestFiles>();
            EnvironmentsLayout.Content = ServiceLocator.Current.GetInstance<RequestEnvironments>();
            RequestExtensions.Content = ServiceLocator.Current.GetInstance<RequestExtensions>();
            SequenceFiles.Content = ServiceLocator.Current.GetInstance<HttpRequestSequenceFiles>();            
            
            eventAggregator.GetEvent<AddInputBindingEvent>().Subscribe(AddInputBinding);
            eventAggregator.GetEvent<RemoveInputBindingEvent>().Subscribe(RemoveInputBindings);
            eventAggregator.GetEvent<RemoveTabEvent>().Subscribe(RemoveTabById);
            eventAggregator.GetEvent<IsDirtyEvent>().Subscribe(IsDirtyHandler);

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Subscribe(AddLayoutDocument);
            eventAggregator.GetEvent<UpdateTabTitleEvent>().Subscribe(UpdateTabTitle);
            eventAggregator.GetEvent<GetLayoutDataEvent>().Subscribe(GetLayoutContent);

            eventAggregator.GetEvent<ShowErrorEvent>().Subscribe(ShowError);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(CloseSolution);
        }

        private void GetLayoutContent(LayoutDataRequest layoutDataRequest)
        {
            var layoutDocuments = GetDocuments();

            foreach (var layoutDocument in layoutDocuments)
            {
                if(layoutDocument.Content.GetType() == layoutDataRequest.UserControlType)
                {
                    if (((UserControl)layoutDocument.Content).DataContext.Equals(layoutDataRequest.DataContext))
                    {
                        layoutDataRequest.Action(layoutDocument.ContentId, layoutDocument.Content);
                    }
                }
            }
        }

        public void ShowError(KeyValuePair<string, string> errorMessage)
        {
            MessageBox.Show(errorMessage.Value, errorMessage.Key, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void UpdateTabTitle(TabHeader tabHeader)
        {
            var correspondingTab = GetDocumentById(tabHeader.Id);
            if (correspondingTab != null)
            {
                correspondingTab.Title = tabHeader.Title;
            }
        }

        private LayoutDocument GetDocumentById(string contentId)
        {
            return dockingManager.Layout.Descendents().OfType<LayoutDocument>().FirstOrDefault(d => d.ContentId == contentId);
        }

        private List<LayoutDocument> GetDocuments()
        {
            return dockingManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
        }

        private void AddLayoutDocument(LayoutDocument layoutDocument)
        {
            var correspondingTab = GetDocumentById(layoutDocument.ContentId);
            if (correspondingTab != null)
            {
                correspondingTab.IsActive = true;
                correspondingTab.IsSelected = true;
            }
            else
            {
                DocumentsPane.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
                layoutDocument.IsActive = true;
                layoutDocument.IsActiveChanged += DocumentIsActiveChanged;
            }
        }

        private void IsDirtyHandler(bool isDirty)
        {
            var selectedDocument = dockingManager.Layout.ActiveContent;
            if (selectedDocument == null || !(selectedDocument is LayoutDocument))
            {
                return;
            }
            IsDirty(selectedDocument, isDirty);
        }

        private void IsDirty(LayoutContent layoutContent, bool isDirty)
        {
            if (layoutContent == null)
            {
                return;
            }

            if (isDirty)
            {
                if (!layoutContent.Title.EndsWith(" *"))
                {
                    layoutContent.Title = layoutContent.Title + " *";
                }
            }
            else
            {
                layoutContent.Title = layoutContent.Title.Replace(" *", "");
            }
        }

        private void RemoveTabById(string id)
        {
            var documentPane = GetDocumentById(id);
            if(documentPane != null)
            {
                documentPane.Close();
            }
        }

        private void CloseSolution(bool obj)
        {
            var documents = GetDocuments();

            for(var i = 0; i < documents.Count; i++)
            {
                documents[i].Close();
            }
            documents.Clear();
        }

        private void DocumentIsActiveChanged(object sender, EventArgs eventArgs)
        {
            RemoveInputBindings(true);

            mainMenuApplicationService.CreateInitialMenuItems();

            SetCloseSolutionState();

            var currentSelectedTab = dockingManager.Layout.ActiveContent;

            eventAggregator.GetEvent<DocumentChangedEvent>().Publish(
                layoutDataFactory.Create(
                currentSelectedTab.ContentId,
                currentSelectedTab.Content, 
                currentSelectedTab.IsActive,
                currentSelectedTab.IsSelected));
        }

        private void SetCloseSolutionState()
        {
            if (Solution.Current.FilePath != null)
            {
                foreach (var item in shellViewModel.MenuItems[0].Items)
                {
                    if (item is MenuItem)
                    {
                        var menuItem = (MenuItem) item;
                        if (menuItem.Header.ToString() == "_Close Solution")
                        {
                            menuItem.IsEnabled = true;
                            break;
                        }
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            CloseSolution(true);
            base.OnClosed(e);
        }

        private void AddInputBinding(KeyBindingData keyBindingData)
        {
            InputBindings.Add(new KeyBinding(keyBindingData.Command, keyBindingData.KeyGesture));
        }

        private void RemoveInputBindings(bool obj)
        {
            InputBindings.Clear();
        }

        private void SequenceTitleChanged(object sender, PropertyChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
