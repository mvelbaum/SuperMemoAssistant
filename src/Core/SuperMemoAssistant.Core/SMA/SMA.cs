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




namespace SuperMemoAssistant.SMA
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Runtime.Remoting;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Configs;
  using Exceptions;
  using global::Extensions.System.IO;
  using Interop.SMA;
  using Interop.SMA.Notifications;
  using Interop.SuperMemo;
  using Interop.SuperMemo.Core;
  using PluginManager.Interop.Sys;
  using Process.NET;
  using SuperMemo;
  using SuperMemo.Common;
  using SuperMemo.Common.Content.Layout;
  using SuperMemo.SuperMemo17;
  using Utils;

  /// <summary>
  ///   Wrapper around a SM management instance that handles SuperMemo App lifecycle events (start, exit, ...) and provides a
  ///   safe interface to interact with SuperMemo
  /// </summary>
  [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
  public sealed partial class SMA
    : PerpetualMarshalByRefObject,
      ISuperMemoAssistant,
      IDisposable
  {
    #region Properties & Fields - Non-Public

    private SuperMemoCore _sm;

    #endregion




    #region Constructors

    static SMA()
    {
      RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
    }

    /// <summary>Create an instance of the wrapper that will start a SM instance and attach the management engine.</summary>
    public SMA() { }


    /// <inheritdoc />
    public void Dispose()
    {
      _sm?.Dispose();
    }

    #endregion




    #region Properties & Fields - Public

    public IProcess SMProcess => _sm?.SMProcess;

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public IEnumerable<string> Layouts => LayoutManager.Instance.Layouts
                                                       .Select(l => l.Name)
                                                       .OrderBy(n => n)
                                                       .ToList();
    /// <inheritdoc />
    public INotificationManager NotificationMgr => Core.NotificationMgr;

    /// <inheritdoc />
    public ISuperMemo SM => _sm;

    #endregion






    #region Methods

    //
    // Collection loading management

    public async Task<Exception> StartAsync(
      NativeDataCfg nativeDataCfg,
      SMCollection  collection)
    {
      try
      {
        if (_sm != null)
          throw new InvalidOperationException("_sm is already instantiated");

        // Load collection config
        await LoadConfigAsync(collection).ConfigureAwait(false);

        // Make sure the SuperMemo version is compatible and instantiate it
        var nativeData = CheckSuperMemoExecutable(nativeDataCfg);

        _sm = GetSuperMemoFactory(collection, nativeData.SMVersion);

        // Notify Plugins of selected collection
        await OnCollectionSelectedAsync(collection).ConfigureAwait(false);

        await _sm.StartAsync(nativeData).ConfigureAwait(false);
        // TODO: Ensure opened collection (windows title) matches parameter
      }
      catch (Exception ex)
      {
        if (ex is SMAException)
          LogTo.Warning(ex, "Failed to start SM.");

        else
          LogTo.Error(ex, "Failed to start SM.");

        _sm?.Dispose();
        _sm = null;

        // TODO: Handle exception

        return ex;
      }

      return null;
    }

    private static SuperMemoCore GetSuperMemoFactory(SMCollection collection, Version smVersion)
    {
      if (SM17.Versions.Contains(smVersion))
        return new SM17(collection, CoreConfig.SuperMemo.SMBinPath);

      throw new SMAException($"Unsupported SM version {smVersion}");
    }

    private static NativeData CheckSuperMemoExecutable(NativeDataCfg nativeDataCfg)
    {
      var smFile = new FilePath(CoreConfig.SuperMemo.SMBinPath);

      if (SuperMemoFinder.CheckSuperMemoExecutable(nativeDataCfg, smFile, out var nativeData, out var ex) == false)
        throw ex;

      LogTo.Information("SuperMemo version {SMVersion} detected", nativeData.SMVersion);

      return nativeData;
    }

    #endregion
  }
}
