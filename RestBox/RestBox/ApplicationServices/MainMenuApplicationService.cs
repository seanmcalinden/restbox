﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using RestBox.Domain.Entities;
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.UserControls;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class MainMenuApplicationService : IMainMenuApplicationService
    {
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IEventAggregator eventAggregator;

        public MainMenuApplicationService(
            IFileService fileService, 
            IJsonSerializer jsonSerializer,
            IEventAggregator eventAggregator)
        {
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            this.eventAggregator = eventAggregator;
        }

        public void CreateInitialMenuItems(ShellViewModel shellViewModel)
        {
            shellViewModel.MenuItems.Clear();
            var fileMenu = new MenuItem();
            fileMenu.Header = "_File";

            var newMenu = new MenuItem();
            newMenu.Header = "_New";

            var openMenu = new MenuItem();
            openMenu.Header = "_Open";


            var newSolution = new MenuItem();
            newSolution.Command = new DelegateCommand(CreateNewSolution);
            newSolution.Header = "_Solution";

            var newRequest = new MenuItem();
            newRequest.Command = new DelegateCommand(CreateNewRequest);
            newRequest.Header = "_Http Request";

            var openSolution = new MenuItem();
            openSolution.Command = new DelegateCommand(OpenSolution);
            openSolution.Header = "_Solution";

            var openRequest = new MenuItem();
            openRequest.Command = new DelegateCommand(OpenRequest);
            openRequest.Header = "_Http Request";

            var closeSolution = new MenuItem();
            closeSolution.Command = new DelegateCommand(CloseSolution);
            closeSolution.Header = "_Close Solution";
            closeSolution.IsEnabled = false;

            var exitMenu = new MenuItem();
            exitMenu.Command = shellViewModel.ExitApplicationCommand;
            exitMenu.Header = "E_xit";

            newMenu.Items.Add(newSolution);
            newMenu.Items.Add(newRequest);
            openMenu.Items.Add(openSolution);
            openMenu.Items.Add(openRequest);

            fileMenu.Items.Add(newMenu);
            fileMenu.Items.Add(openMenu);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(closeSolution);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(exitMenu);
            
            shellViewModel.MenuItems.Add(fileMenu);
        }

        public void InsertSeparator(MenuItem parent, int position)
        {
            parent.Items.Insert(position, new Separator());
        }

        public void InsertTopLevelMenuItem(ShellViewModel shellViewModel, MenuItem menuItem, int position)
        {
            shellViewModel.MenuItems.Insert(position, menuItem);
        }

        public void InsertMenuItem(MenuItem parent, MenuItem menuItem, int position)
        {
            parent.Items.Insert(position, menuItem);
        }

        public void RemoveItem(MenuItem parent, int position)
        {
            parent.Items.RemoveAt(position);
        }

        public MenuItem Get(MenuItem parent, string headerText)
        {
            foreach (var item in parent.Items)
            {
                if (item is MenuItem && ((MenuItem)item).Header.ToString() == headerText)
                {
                    return (MenuItem) item;
                }
            }
            return null;
        }

        private void CreateNewSolution()
        {
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
            var saveFileDialog = new SaveFileDialog
                                     {
                                         Filter = "Rest Box Solution (*.rsln)|*.rsln",
                                         FileName = "Untitled",
                                         Title = "Create Solution File"
                                     };
            if (saveFileDialog.ShowDialog() == true)
            {
                Solution.Current.Clear();
                Solution.Current.Name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                Solution.Current.FilePath = saveFileDialog.FileName;
                fileService.SaveSolution();
                eventAggregator.GetEvent<NewSolutionEvent>().Publish(true);
            }
        }

        private void CreateNewRequest()
        {
            eventAggregator.GetEvent<NewHttpRequestEvent>().Publish("StandaloneHttpRequest");
        }

        private void OpenRequest()
        {
            var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Rest Box Http Request (*.rhrq)|*.rhrq",
                                         Title = "Open Http Request File"
                                     };
            if (openFileDialog.ShowDialog() == true)
            {
                var httpRequest = fileService.Load<HttpRequestItemFile>(openFileDialog.FileName);
                var httpRequestControl = ServiceLocator.Current.GetInstance<HttpRequest>();
                httpRequestControl.FilePath = openFileDialog.FileName;
                var viewModel = httpRequestControl.DataContext as HttpRequestViewModel;

                viewModel.RequestUrl = httpRequest.Url;
                viewModel.RequestVerb = viewModel.RequestVerbs.First(x => x.Content.ToString() == httpRequest.Verb);
                viewModel.RequestHeaders = httpRequest.Headers;
                viewModel.RequestBody = httpRequest.Body;

                var layoutDocument = new LayoutDocument
                {
                    Title = Path.GetFileNameWithoutExtension(openFileDialog.FileName),
                    ContentId = "StandaloneHttpRequest",
                    Content = httpRequestControl,
                    IsSelected = true,
                    CanFloat = true
                };

                eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
            }
        }

        private void OpenSolution()
        {
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
            var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Rest Box Solution (*.rsln)|*.rsln",
                                         Title = "Open Solution File"
                                     };
            if(openFileDialog.ShowDialog() == true)
            {
                Solution.Current.Clear();

                using (var fileStream = openFileDialog.OpenFile())
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var fileContent = reader.ReadToEnd();
                        Solution.Current = jsonSerializer.FromJsonString<Solution>(fileContent);
                    }
                    
                }
                eventAggregator.GetEvent<OpenSolutionEvent>().Publish(true);
            }
        }


        private void CloseSolution()
        {
            Solution.Current.Clear();
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
        }
    }
}
