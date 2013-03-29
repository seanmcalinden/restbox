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
        private readonly ILayoutApplicationService layoutApplicationService;
        private readonly IEventAggregator eventAggregator;
        private readonly ShellViewModel shellViewModel;

        public Shell(
            ShellViewModel shellViewModel, 
            IEventAggregator eventAggregator, 
            IMainMenuApplicationService mainMenuApplicationService,
            ILayoutDataFactory layoutDataFactory,
            ILayoutApplicationService layoutApplicationService)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.layoutDataFactory = layoutDataFactory;
            this.layoutApplicationService = layoutApplicationService;
            this.eventAggregator = eventAggregator;
            this.shellViewModel = shellViewModel;
            DataContext = shellViewModel;
            shellViewModel.ApplicationTitle = "REST Box";
            
            InitializeComponent();

            //layoutApplicationService.Load(dockingManager);

            if (!(DocumentsPane.Children.Any(x => x.ContentId == "StartPage")))
            {
                CreateStartPage();
            }

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

            eventAggregator.GetEvent<ShowLayoutEvent>().Subscribe(ShowLayout);
            eventAggregator.GetEvent<UpdateViewMenuItemChecksEvent>().Subscribe(UpdateViewMenuChecks);

            eventAggregator.GetEvent<SaveAllEvent>().Subscribe(SaveAllHandler);
            this.Closing += OnClosing;
        }

        private void CreateStartPage()
        {
            var startPage = new LayoutDocument
                {
                    Title = "Start Page",
                    ContentId = "StartPage"
                };
            startPage.IsActiveChanged += DocumentIsActiveChanged;
            DocumentsPane.Children.Add(startPage);
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            var documents = GetDocuments();
            foreach (var layoutDocument in documents)
            {
                if (layoutDocument.Content is UserControl)
                {
                    if (((UserControl)layoutDocument.Content).DataContext is ISave)
                    {
                       layoutDocument.Close();
                    }
                }
            }

            CreateStartPage();
            //layoutApplicationService.Save(dockingManager);
        }

        private void SaveAllHandler(bool obj)
        {
            var documents = GetDocuments();
            foreach (var layoutDocument in documents)
            {
                if (layoutDocument.Content is UserControl)
                {
                    if (((UserControl) layoutDocument.Content).DataContext is ISave)
                    {
                        var control = ((ISave) ((UserControl) layoutDocument.Content).DataContext);
                        if (control.IsDirty)
                        {
                            control.Save(layoutDocument.ContentId, layoutDocument.Content);
                        }
                    }
                }
            }
        }

        private void UpdateViewMenuChecks(bool obj)
        {
            var viewMenu = shellViewModel.MenuItems.First(x => x.Header.ToString() == "_View");

            if (HttpRequestFilesLayout.IsVisible)
            {
                ((MenuItem)viewMenu.Items[0]).IsChecked = true;
            }

            if (EnvironmentsLayout.IsVisible)
            {
                ((MenuItem)viewMenu.Items[1]).IsChecked = true;
            }

            if (RequestExtensions.IsVisible)
            {
                ((MenuItem)viewMenu.Items[2]).IsChecked = true;
            }

            if (SequenceFiles.IsVisible)
            {
                ((MenuItem)viewMenu.Items[3]).IsChecked = true;
            }
        }

        private void ShowLayout(LayoutType layoutType)
        {
            var viewMenu = shellViewModel.MenuItems.First(x => x.Header.ToString() == "_View");

            switch (layoutType)
            {
                case LayoutType.HttpRequests :
                    if (HttpRequestFilesLayout.IsVisible)
                    {
                        HttpRequestFilesLayout.IsVisible = false;
                        ((MenuItem) viewMenu.Items[0]).IsChecked = false;
                    }
                    else
                    {
                        HttpRequestFilesLayout.IsVisible = true;
                        ((MenuItem)viewMenu.Items[0]).IsChecked = true;
                    }
                    break;
                case LayoutType.Environments:
                    if (EnvironmentsLayout.IsVisible)
                    {
                        EnvironmentsLayout.IsVisible = false;
                        ((MenuItem)viewMenu.Items[1]).IsChecked = false;
                    }
                    else
                    {
                        EnvironmentsLayout.IsVisible = true;
                        ((MenuItem)viewMenu.Items[1]).IsChecked = true;
                    }
                    break;
                case LayoutType.RequestExtensions:
                    if (RequestExtensions.IsVisible)
                    {
                        RequestExtensions.IsVisible = false;
                        ((MenuItem)viewMenu.Items[2]).IsChecked = false;
                    }
                    else
                    {
                        RequestExtensions.IsVisible = true;
                        ((MenuItem)viewMenu.Items[2]).IsChecked = true;
                    }
                    break;
                case LayoutType.Sequences:
                    if (SequenceFiles.IsVisible)
                    {
                        SequenceFiles.IsVisible = false;
                        ((MenuItem)viewMenu.Items[3]).IsChecked = false;
                    }
                    else
                    {
                        SequenceFiles.IsVisible = true;
                        ((MenuItem)viewMenu.Items[3]).IsChecked = true;
                    }
                    break;
            }
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

            ISave control = null;

            var documents = GetDocuments();
            foreach (var layoutDocument in documents)
            {
                if (layoutDocument.Content is UserControl)
                {
                    if (((UserControl)layoutDocument.Content).DataContext is ISave)
                    {
                        control = ((ISave)((UserControl)layoutDocument.Content).DataContext);
                    }
                }
            }

            if (control == null)
            {
                return;
            }

            if (isDirty)
            {
                if (!layoutContent.Title.EndsWith(" *"))
                {
                    layoutContent.Title = layoutContent.Title + " *";
                    control.IsDirty = true;
                }
            }
            else
            {
                layoutContent.Title = layoutContent.Title.Replace(" *", "");
                control.IsDirty = false;
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

            if (currentSelectedTab != null)
            {

                eventAggregator.GetEvent<DocumentChangedEvent>().Publish(
                    layoutDataFactory.Create(
                        currentSelectedTab.ContentId,
                        currentSelectedTab.Content,
                        currentSelectedTab.IsActive,
                        currentSelectedTab.IsSelected));
            }
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
    }
}
