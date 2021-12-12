﻿#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion




// ReSharper disable RedundantTypeArgumentsOfMethod

namespace SuperMemoAssistant.Plugins
{
  using System;
  using System.ComponentModel;
  using System.Diagnostics;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Threading;
  using Anotar.Serilog;
  using Extensions;
  using global::Extensions.System.IO;
  using Interop;
  using Interop.SMA;
  using Interop.SuperMemo.Core;
  using Microsoft.QueryStringDotNET;
  using Microsoft.Toolkit.Uwp.Notifications;
  using Models;
  using NuGet.Configuration;
  using NuGet.Versioning;
  using PluginManager.Contracts;
  using PluginManager.Logger;
  using PluginManager.Models;
  using PluginManager.PackageManager.Models;
  using PluginManager.PackageManager.NuGet;
  using SMA;
  using SMA.Configs;
  using Sys.Windows;
  using TPluginManager =
    PluginManager.PluginManagerBase<SMAPluginManager, Models.PluginInstance, Models.PluginMetadata,
      PluginManager.Interop.Contracts.IPluginManager<Interop.SMA.ISuperMemoAssistant>, Interop.SMA.ISuperMemoAssistant,
      Interop.Plugins.ISMAPlugin
    >;

  /// <inheritdoc cref="TPluginManager" />
  public partial class SMAPluginManager : TPluginManager, IPluginLocations
  {
    #region Constants & Statics

    public static SMAPluginManager Instance { get; } = new SMAPluginManager();

    public const string ToastActionRestartAfterCrash = "PluginRestartAfterCrash";
    public const string ToastActionParameterPluginId = "PluginId";

    private const string MinInteropVersion = "2.1.0-beta.11";

    #endregion




    #region Properties & Fields - Non-Public

    private readonly PluginManagerLogAdapter          _logAdapter = new PluginManagerLogAdapter();
    private          DispatcherSynchronizationContext _uiSynchronizationContext;

    #endregion




    #region Constructors

    private SMAPluginManager()
    {
      Core.SMA.OnSMStoppedInternalEvent += OnSMStopped;
    }

    #endregion




    #region Properties & Fields - Public

    public PluginInstance this[Guid sessionGuid] => RunningPluginMap.SafeGet(sessionGuid);

    #endregion




    #region Methods

    public async Task InitializeAsync()
    {
      await base.Initialize(false).ConfigureAwait(false);

      Core.CoreConfig.Updates.PropertyChanged += OnUpdatesConfigChanged;
      OnUpdatesConfigChanged(null, new PropertyChangedEventArgs(nameof(UpdateCfg.CoreUpdateChannel)));

      // ReSharper disable once AssignNullToNotNullAttribute
      _uiSynchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher);
    }

    private void OnSMStopped(object sender, SMProcessEventArgs e)
    {
      base.Cleanup();
    }

    private void OnUpdatesConfigChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != nameof(UpdateCfg.CoreUpdateChannel))
        return;

      SetAlphaRepoEnabledState(PackageManager.SourceRepositories);
    }

    /// <summary>Adds an additional handler for <see cref="SMAPluginManager" /> log output</summary>
    /// <param name="logger">The log handler</param>
    public void AddLogger(Action<string> logger) => _logAdapter.AddLogger(logger);

    /// <summary>Removes <paramref name="logger" /> from list of log output handlers</summary>
    /// <param name="logger">The log handler</param>
    public void RemoveLogger(Action<string> logger) => _logAdapter.RemoveLogger(logger);

    private void SetAlphaRepoEnabledState(SourceRepositoryProvider srp)
    {
      var alphaRepo = srp.Keys.FirstOrDefault(ps => ps.Name == UpdateCfg.PluginsAlphaRepositoryUrl);

      if (alphaRepo == null)
      {
        LogTo.Error("Alpha repository is unavailable.");
        return;
      }

      // Only enable alpha repository when SMA core channel is set to Nightly or Test
      alphaRepo.IsEnabled = Core.CoreConfig.Updates.CoreUpdateChannel == UpdateCfg.CoreNightlyChannel
        || Core.CoreConfig.Updates.CoreUpdateChannel.Equals("Test", StringComparison.OrdinalIgnoreCase);
    }

    #endregion




    #region IPluginManagerBase

    public override SourceRepositoryProvider CreateSourceRepositoryProvider(ISettings s)
    {
      var srp = new SourceRepositoryProvider(s, Core.CoreConfig.Updates.PluginsUpdateNuGetUrls);

      SetAlphaRepoEnabledState(srp);

      return srp;
    }

    /// <inheritdoc />
    protected override void OnPluginStartFailed(
      PluginInstance     pluginInstance,
      PluginStartFailure reason,
      string             errMsg)
    {
      base.OnPluginStartFailed(pluginInstance, reason, errMsg);

      errMsg.ShowDesktopNotification();
    }

    /// <inheritdoc />
    protected override void OnPluginCrashed(PluginInstance pluginInstance)
    {
      $"{pluginInstance.ToString().CapitalizeFirst()} has crashed.".ShowDesktopNotification(
        // Restart action
        new ToastButton("Restart",
                        new QueryString
                        {
                          { "action", ToastActionRestartAfterCrash },
                          { ToastActionParameterPluginId, pluginInstance.Package.Id }
                        }.ToString())
        {
          ActivationType = ToastActivationType.Background
        },

        // Open logs folder action
        new ToastButton("Open the logs folder", SMAFileSystem.LogDir.FullPathWin)
        {
          ActivationType = ToastActivationType.Protocol
        }
      );
    }

    /// <inheritdoc />
    public override string GetPluginHostTypeAssemblyName(PluginInstance pluginInstance)
    {
      return "SuperMemoAssistant.Interop";
    }

    /// <inheritdoc />
    public override NuGetVersion GetPluginHostTypeAssemblyMinimumVersion(PluginInstance pluginInstance)
    {
      return NuGetVersion.Parse(MinInteropVersion); // NuGetVersion.Parse(typeof(SMAConst).GetAssemblyVersion());
    }

    /// <inheritdoc />
    public override string GetPluginHostTypeQualifiedName(PluginInstance pluginInstance)
    {
      return "SuperMemoAssistant.Interop.Plugins.PluginHost";
    }

    /// <inheritdoc />
    public override ISuperMemoAssistant GetCoreInstance()
    {
      return Core.SMA;
    }

    /// <inheritdoc />
    public override PluginInstance CreatePluginInstance(LocalPluginPackage<PluginMetadata> package)
    {
      return new PluginInstance(package);
    }

    /// <inheritdoc />
    public override PluginMetadata CreateDevMetadata(string packageName, FileVersionInfo fileVersionInfo)
    {
      return new PluginMetadata
      {
        Enabled       = true,
        DisplayName   = fileVersionInfo.ProductName,
        PackageName   = packageName,
        Description   = "Development plugin",
        IsDevelopment = true,
      };
    }

    /// <inheritdoc />
    public override IPluginLocations Locations => this;

    /// <inheritdoc />
    public override ILogAdapter LogAdapter => _logAdapter;

    /// <inheritdoc />
    public override SynchronizationContext UISynchronizationContext => _uiSynchronizationContext;

    #endregion




    #region IPluginLocations

    /// <inheritdoc />
    public DirectoryPath PluginDir => SMAFileSystem.PluginDir;
    /// <inheritdoc />
    public DirectoryPath PluginHomeDir => SMAFileSystem.PluginHomeDir;
    /// <inheritdoc />
    public DirectoryPath PluginPackageDir => SMAFileSystem.PluginPackageDir;
    /// <inheritdoc />
    public DirectoryPath PluginDevelopmentDir => SMAFileSystem.PluginDevelopmentDir;
    /// <inheritdoc />
    public FilePath PluginHostExeFile => SMAFileSystem.PluginHostExeFile;
    /// <inheritdoc />
    public FilePath PluginConfigFile => SMAFileSystem.PluginConfigFile;

    #endregion
  }
}
