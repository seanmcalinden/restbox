using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Factories;
using RestBox.UserControls;
using RestBox.Utilities;
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
        private readonly IRestBoxStateService restBoxStateService;
        private readonly IEventAggregator eventAggregator;
        private readonly ShellViewModel shellViewModel;
        private LayoutAnchorable httpRequestFilesLayout;
        private LayoutAnchorable environmentsLayout;
        private LayoutAnchorable requestExtensions;
        private LayoutAnchorable sequenceFiles;
        private bool resetLayoutRequested;

        public Shell(
            ShellViewModel shellViewModel, 
            IEventAggregator eventAggregator, 
            IMainMenuApplicationService mainMenuApplicationService,
            ILayoutDataFactory layoutDataFactory,
            ILayoutApplicationService layoutApplicationService,
            IRestBoxStateService restBoxStateService)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.layoutDataFactory = layoutDataFactory;
            this.layoutApplicationService = layoutApplicationService;
            this.restBoxStateService = restBoxStateService;
            this.eventAggregator = eventAggregator;
            this.shellViewModel = shellViewModel;
            DataContext = shellViewModel;
            shellViewModel.ApplicationTitle = "REST Box";
            
            InitializeComponent();

            layoutApplicationService.Load(dockingManager);

            httpRequestFilesLayout = GetLayoutAnchorableById("HttpRequestFilesLayoutId");
            environmentsLayout = GetLayoutAnchorableById("EnvironmentsLayoutId");
            requestExtensions = GetLayoutAnchorableById("RequestExtensionsId");
            sequenceFiles = GetLayoutAnchorableById("SequenceFilesId");
            
            httpRequestFilesLayout.Content = ServiceLocator.Current.GetInstance<HttpRequestFiles>();
            environmentsLayout.Content = ServiceLocator.Current.GetInstance<RequestEnvironments>();
            requestExtensions.Content = ServiceLocator.Current.GetInstance<RequestExtensions>();
            sequenceFiles.Content = ServiceLocator.Current.GetInstance<HttpRequestSequenceFiles>();   
         
            eventAggregator.GetEvent<AddInputBindingEvent>().Subscribe(AddInputBinding);
            eventAggregator.GetEvent<RemoveInputBindingEvent>().Subscribe(RemoveInputBindings);
            eventAggregator.GetEvent<RemoveTabEvent>().Subscribe(RemoveTabById);
            eventAggregator.GetEvent<IsDirtyEvent>().Subscribe(IsDirtyHandler);

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Subscribe(AddLayoutDocument);
            eventAggregator.GetEvent<UpdateTabTitleEvent>().Subscribe(UpdateTabTitle);
            eventAggregator.GetEvent<GetLayoutDataEvent>().Subscribe(GetLayoutContent);

            eventAggregator.GetEvent<ShowErrorEvent>().Subscribe(ShowError);

            eventAggregator.GetEvent<ShowLayoutEvent>().Subscribe(ShowLayout);
            eventAggregator.GetEvent<UpdateViewMenuItemChecksEvent>().Subscribe(UpdateViewMenuChecks);

            eventAggregator.GetEvent<SaveAllEvent>().Subscribe(SaveAllHandler);

            eventAggregator.GetEvent<ResetLayoutEvent>().Subscribe(ResetLayoutOnRestart);
            eventAggregator.GetEvent<SaveRestBoxStateEvent>().Subscribe(SaveRestBoxState);
            eventAggregator.GetEvent<CreateStartPageEvent>().Subscribe(CreateStartPage);
            
            Closing += OnClosing;

            CreateStartPage(true);
        }

        private void SaveRestBoxState(RestBoxStateFile restBoxStateFile)
        {
            restBoxStateService.SaveState(restBoxStateFile);
        }

        private void ResetLayoutOnRestart(bool shouldReset)
        {
            resetLayoutRequested = shouldReset;
        }

        private void CreateStartPage(bool obj)
        {
            var startPage = new LayoutDocument
                {
                    Title = "Start Page",
                    ContentId = "StartPage",
                    Content = ServiceLocator.Current.GetInstance<StartPage>(),
                    IconSource = new BitmapImage(LayoutDocumentUtilities.GetImageUri(LayoutDocumentType.StartPage))
                };
            startPage.IsActiveChanged += StartPageDocumentIsActiveChanged;
            AddLayoutDocument(startPage);
        }

        public void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            OnClosing(sender,cancelEventArgs, null);
        }

        public void OnClosing(object sender, CancelEventArgs cancelEventArgs, Action<bool> callBack)
        {
            var documents = GetDocuments();
            foreach (var layoutDocument in documents)
            {
                if (layoutDocument.Content is UserControl)
                {
                    ISave context = ((UserControl) layoutDocument.Content).DataContext as ISave;
                    if (context != null)
                    {
                        if (context.IsDirty)
                        {
                            var result = MessageBox.Show(string.Format("Do you want to save {0}?", layoutDocument.Title),
                                            string.Format("Save {0}?", layoutDocument.Title),
                                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                            if (result == MessageBoxResult.Cancel)
                            {
                                cancelEventArgs.Cancel = true;
                                if (callBack != null)
                                {
                                    callBack(true);
                                }
                                return;
                            }

                            if (result == MessageBoxResult.No)
                            {
                                layoutDocument.Close();
                            }
                            else if (result == MessageBoxResult.Yes)
                            {
                                context.Save(layoutDocument.ContentId, layoutDocument.Content);
                                layoutDocument.Close();
                            }
                        }
                        else
                        {
                            layoutDocument.Close();
                        }
                    }
                }
            }

            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);

            var startPage = GetDocumentById("StartPage");
            if (startPage != null)
            {
                startPage.Close();
            }

            if (!resetLayoutRequested)
            {
                layoutApplicationService.Save(dockingManager);
            }
            else
            {
                layoutApplicationService.Delete();
            }

            mainMenuApplicationService.DisableSolutionMenus();

            if (callBack != null)
            {
                callBack(false);
            }
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

            if (httpRequestFilesLayout.IsVisible)
            {
                ((MenuItem)viewMenu.Items[0]).IsChecked = true;
            }

            if (environmentsLayout.IsVisible)
            {
                ((MenuItem)viewMenu.Items[1]).IsChecked = true;
            }

            if (requestExtensions.IsVisible)
            {
                ((MenuItem)viewMenu.Items[2]).IsChecked = true;
            }

            if (sequenceFiles.IsVisible)
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
                    if (httpRequestFilesLayout.IsVisible)
                    {
                        httpRequestFilesLayout.IsVisible = false;
                        ((MenuItem) viewMenu.Items[0]).IsChecked = false;
                    }
                    else
                    {
                        httpRequestFilesLayout.IsVisible = true;
                        ((MenuItem)viewMenu.Items[0]).IsChecked = true;
                    }
                    break;
                case LayoutType.Environments:
                    if (environmentsLayout.IsVisible)
                    {
                        environmentsLayout.IsVisible = false;
                        ((MenuItem)viewMenu.Items[1]).IsChecked = false;
                    }
                    else
                    {
                        environmentsLayout.IsVisible = true;
                        ((MenuItem)viewMenu.Items[1]).IsChecked = true;
                    }
                    break;
                case LayoutType.RequestExtensions:
                    if (requestExtensions.IsVisible)
                    {
                        requestExtensions.IsVisible = false;
                        ((MenuItem)viewMenu.Items[2]).IsChecked = false;
                    }
                    else
                    {
                        requestExtensions.IsVisible = true;
                        ((MenuItem)viewMenu.Items[2]).IsChecked = true;
                    }
                    break;
                case LayoutType.Sequences:
                    if (sequenceFiles.IsVisible)
                    {
                        sequenceFiles.IsVisible = false;
                        ((MenuItem)viewMenu.Items[3]).IsChecked = false;
                    }
                    else
                    {
                        sequenceFiles.IsVisible = true;
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
                if (layoutDocument.Content != null && layoutDocument.Content.GetType() == layoutDataRequest.UserControlType)
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

        private LayoutAnchorable GetLayoutAnchorableById(string contentId)
        {
            return dockingManager.Layout.Descendents().OfType<LayoutAnchorable>().FirstOrDefault(d => d.ContentId == contentId);
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
            var existingLayoutDocumentPane = dockingManager.Layout.RootPanel.Children.FirstOrDefault(x => x.GetType() == typeof (LayoutDocumentPane));

            if (existingLayoutDocumentPane != null)
            {
                 dockingManager.Layout.RootPanel.Children.Remove(existingLayoutDocumentPane);
            }

            var layoutPanel = dockingManager.Layout.RootPanel.Descendents().OfType<LayoutPanel>().FirstOrDefault();

            if (layoutPanel == null)
            {
                layoutPanel = new LayoutPanel(new LayoutDocumentPane());
                dockingManager.Layout.RootPanel.Children.Add(layoutPanel);
            }

            var layoutDocumentPane = layoutPanel.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();

            if (layoutDocumentPane == null)
            {
                layoutDocumentPane = new LayoutDocumentPane();
                layoutPanel.Children.Add(layoutDocumentPane);
            }

            var correspondingTab = GetDocumentById(layoutDocument.ContentId);
            if (correspondingTab != null)
            {
                correspondingTab.IsActive = true;
                correspondingTab.IsSelected = true;
            }
            else
            {
                layoutDocumentPane.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
                layoutDocument.IsActive = true;
                layoutDocument.IsActiveChanged += DocumentIsActiveChanged;
            }
        }

        private void IsDirtyHandler(IsDirtyData isDirtyData)
        {
            var documents = GetDocuments();

            if (isDirtyData.Document is NativeActivity)
            {
                foreach (var layoutDocument in documents)
                {
                    if (layoutDocument.Content is HttpRequestSequence)
                    {
                        var mainSequence = ((layoutDocument.Content as HttpRequestSequence).DataContext as HttpRequestSequenceViewModel).MainSequence;
                        var isThisSequence = InspectActivity(isDirtyData.Document, mainSequence, 0);

                        if (isThisSequence)
                        {
                            var control = ((ISave)((UserControl)layoutDocument.Content).DataContext);
                            control.IsDirty = isDirtyData.IsDirty;
                            if (isDirtyData.IsDirty)
                            {
                                if (!layoutDocument.Title.EndsWith(" *"))
                                {
                                    layoutDocument.Title = layoutDocument.Title + " *";
                                    control.IsDirty = true;
                                }
                            }
                            else
                            {
                                layoutDocument.Title = layoutDocument.Title.Replace(" *", "");
                                control.IsDirty = false;
                            }
                        }
                    }
                }
                return;
            }

            foreach (var layoutDocument in documents)
            {
                if (layoutDocument.Content is UserControl)
                {
                    if (((UserControl)layoutDocument.Content).DataContext == isDirtyData.Document)
                    {
                        var control = ((ISave)((UserControl)layoutDocument.Content).DataContext);
                        control.IsDirty = isDirtyData.IsDirty;
                        if (isDirtyData.IsDirty)
                        {
                            if (!layoutDocument.Title.EndsWith(" *"))
                            {
                                layoutDocument.Title = layoutDocument.Title + " *";
                                control.IsDirty = true;
                            }
                        }
                        else
                        {
                            layoutDocument.Title = layoutDocument.Title.Replace(" *", "");
                            control.IsDirty = false;
                        }
                    }
                }
            }
        }

        static bool InspectActivity(object activityToCheck, Activity root, int indent)
        {
            // Inspect the activity tree using WorkflowInspectionServices.
            IEnumerator<Activity> activities =
                WorkflowInspectionServices.GetActivities(root).GetEnumerator();

            var isActivityToCheck = false;

            while (activities.MoveNext())
            {
                if (activityToCheck == activities.Current)
                {
                    isActivityToCheck = true;
                    break;
                }
                return InspectActivity(activityToCheck, activities.Current, indent + 2);
            }
            return isActivityToCheck;
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
            OnClosing(this, new CancelEventArgs(false));
        }

        private void DocumentIsActiveChanged(object sender, EventArgs eventArgs)
        {
            RemoveInputBindings(true);

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

        private void StartPageDocumentIsActiveChanged(object sender, EventArgs eventArgs)
        {
            RemoveInputBindings(true);

            SetCloseSolutionState();

            var currentSelectedTab = GetDocumentById("StartPage");

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
