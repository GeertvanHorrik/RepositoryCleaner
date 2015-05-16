﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="CatenaLogic">
//   Copyright (c) 2014 - 2015 CatenaLogic. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace RepositoryCleaner.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Collections;
    using Catel.Configuration;
    using Catel.Data;
    using Catel.Logging;
    using Catel.MVVM;
    using Catel.Reflection;
    using Catel.Services;
    using Models;
    using Services;

    internal class MainViewModel : ViewModelBase
    {
        #region Constants
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly ICleanerService _cleanerService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IConfigurationService _configurationService;
        private readonly IRepositoryService _repositoryService;
        #endregion

        #region Constructors
        public MainViewModel(ICleanerService cleanerService, IDispatcherService dispatcherService,
            IConfigurationService configurationService, IRepositoryService repositoryService)
        {
            Argument.IsNotNull(() => cleanerService);
            Argument.IsNotNull(() => dispatcherService);
            Argument.IsNotNull(() => configurationService);
            Argument.IsNotNull(() => repositoryService);

            _cleanerService = cleanerService;
            _dispatcherService = dispatcherService;
            _configurationService = configurationService;
            _repositoryService = repositoryService;

            Repositories = new ObservableCollection<Repository>();

            Analyze = new Command(OnAnalyzeExecute, OnAnalyzeCanExecute);
            CleanUp = new Command(OnCleanUpExecute, OnCleanUpCanExecute);

            var entryAssembly = AssemblyHelper.GetEntryAssembly();
            Title = string.Format("{0} - v{1}", entryAssembly.Title(), entryAssembly.InformationalVersion());
        }
        #endregion

        #region Properties
        public string RepositoriesRoot { get; set; }

        public ObservableCollection<Repository> Repositories { get; private set; }

        public bool IsBusy { get; private set; }
        #endregion

        #region Commands
        public Command Analyze { get; private set; }

        private bool OnAnalyzeCanExecute()
        {
            if (string.IsNullOrWhiteSpace(RepositoriesRoot))
            {
                return false;
            }

            if (IsBusy)
            {
                return false;
            }

            return true;
        }

        private async void OnAnalyzeExecute()
        {
            await FindRepositories();
        }

        public Command CleanUp { get; private set; }

        private bool OnCleanUpCanExecute()
        {
            if (Repositories == null || Repositories.Count == 0)
            {
                return false;
            }

            if (IsBusy)
            {
                return false;
            }

            return true;
        }

        private async void OnCleanUpExecute()
        {
            using (CreateIsBusyScope())
            {
                var repositories = Repositories.ToArray();

                await _cleanerService.CleanAsync(repositories);

                ViewModelCommandManager.InvalidateCommands(true);
            }
        }
        #endregion

        #region Methods
        protected override async Task Initialize()
        {
            RepositoriesRoot = _configurationService.GetValue<string>(Settings.Application.LastRepositoriesRoot);

            await FindRepositories();
        }

        private async Task FindRepositories()
        {
            var repositoriesRoot = RepositoriesRoot;
            if (string.IsNullOrWhiteSpace(repositoriesRoot))
            {
                return;
            }

            using (CreateIsBusyScope())
            {
                var repositories = await _repositoryService.FindRepositoriesAsync(repositoriesRoot);
                if (repositories.Count() > 0)
                {
                    Repositories.AddRange(repositories);
                }
                else
                {
                    Repositories = null;
                }

                _configurationService.SetValue(Settings.Application.LastRepositoriesRoot, repositoriesRoot);
            }
        }

        private IDisposable CreateIsBusyScope()
        {
            return new DisposableToken<MainViewModel>(this, x => x.Instance.IsBusy = true, x => x.Instance.IsBusy = false);
        }
        #endregion
    }
}