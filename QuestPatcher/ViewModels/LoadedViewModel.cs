﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using QuestPatcher.Core;
using QuestPatcher.Core.Models;
using QuestPatcher.ViewModels.Modding;
using ReactiveUI;
using Serilog;

namespace QuestPatcher.ViewModels
{
    public class LoadedViewModel : ViewModelBase
    {
        public string SelectedAppText => $"Modding {Config.AppId}";

        public PatchingViewModel PatchingView { get; }

        public ManageModsViewModel ManageModsView { get; }

        public LoggingViewModel LoggingView { get; }

        public ToolsViewModel ToolsView { get; }

        public OtherItemsViewModel OtherItemsView { get; }

        private string AppName
        {
            get
            {
                var now = DateTime.Now;
                bool isAprilFools = now.Month == 4 && now.Day == 1;
                return isAprilFools ? "QuestCorrupter" : "QuestPatcher";
            }
        }

        public string WelcomeText => $"Welcome to {AppName} 2";

        public Config Config { get; }
        public ApkInfo AppInfo
        {
            get
            {
                Debug.Assert(_installManager.InstalledApp != null);
                return _installManager.InstalledApp;
            }
        }

        public bool NeedsPatchingView => PatchingView.IsPatchingInProgress || !AppInfo.IsModded;

        private readonly InstallManager _installManager;
        private readonly BrowseImportManager _browseManager;

        public LoadedViewModel(PatchingViewModel patchingView, ManageModsViewModel manageModsView, LoggingViewModel loggingView, ToolsViewModel toolsView, OtherItemsViewModel otherItemsView, Config config, InstallManager installManager, BrowseImportManager browseManager)
        {
            PatchingView = patchingView;
            LoggingView = loggingView;
            ToolsView = toolsView;
            ManageModsView = manageModsView;
            OtherItemsView = otherItemsView;

            Config = config;
            _installManager = installManager;
            _browseManager = browseManager;

            _installManager.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(_installManager.InstalledApp) && _installManager.InstalledApp != null)
                {
                    this.RaisePropertyChanged(nameof(AppInfo));
                    this.RaisePropertyChanged(nameof(NeedsPatchingView));
                    this.RaisePropertyChanged(nameof(SelectedAppText));
                }
            };

            patchingView.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PatchingView.IsPatchingInProgress))
                {
                    this.RaisePropertyChanged(nameof(NeedsPatchingView));
                }
            };
        }

        public async void OnDragAndDrop(object? sender, DragEventArgs args)
        {
            Log.Debug("Handling drag and drop on LoadedViewModel");

            // Sometimes a COMException gets thrown if the items can't be parsed for whatever reason.
            // We need to handle this to avoid crashing QuestPatcher.
            try
            {
                var fileNames = args.Data.GetFileNames();
                if (fileNames == null) // Non-file items dragged
                {
                    Log.Debug("Drag and drop contained no file names");
                    return;
                }

                Log.Debug("Files found in drag and drop. Processing . . .");
                await _browseManager.AttemptImportFiles(fileNames.ToList(), OtherItemsView.SelectedFileCopy);
            }
            catch (COMException)
            {
                Log.Error("Failed to parse dragged items");
            }
        }
    }
}
