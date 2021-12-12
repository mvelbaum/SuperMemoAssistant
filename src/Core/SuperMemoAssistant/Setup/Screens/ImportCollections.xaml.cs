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
// 
// 
// Created On:   2020/03/29 00:20
// Modified On:  2020/04/10 14:17
// Modified By:  Alexis

#endregion






// ReSharper disable RedundantNameQualifier

namespace SuperMemoAssistant.Setup.Screens
{
  using System;
  using System.Collections.ObjectModel;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using Anotar.Serilog;
  using global::Extensions.System.IO;
  using Interop.SuperMemo.Core;
  using Models;
  using SMA.Configs;
  using Sys.IO;

  /// <summary>Reads supermemo.ini and list the collections used by the user in SuperMemo.</summary>
  public partial class ImportCollections : SMASetupScreenBase
  {
    #region Properties & Fields - Non-Public

    private bool HasBeenShown { get; set; } = false;

    #endregion




    #region Constructors

    public ImportCollections(CoreCfg startupCfg)
    {
      StartupCfg = startupCfg;

      InitializeComponent();
    }

    #endregion




    #region Properties & Fields - Public

    public CoreCfg StartupCfg { get; }

    /// <summary>Proxy for the HasImportedCollections, used to raise property changed on <see cref="IsSetup" /></summary>
    public bool HasImportedCollections
    {
      get => StartupCfg.HasImportedCollections;
      set
      {
        StartupCfg.HasImportedCollections = value;
        OnPropertyChanged(nameof(IsSetup));
      }
    }

    /// <summary>All available collection for import</summary>
    public ObservableCollection<SMImportCollection> Collections { get; } = new ObservableCollection<SMImportCollection>();

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override bool IsSetup => HasImportedCollections || HasBeenShown;

    /// <inheritdoc />
    public override string ListTitle => "Importing";

    /// <inheritdoc />
    public override string WindowTitle => "Import your collections";

    /// <inheritdoc />
    public override string Description { get; } =
      "Select which SuperMemo collections you wish to use with SuperMemo Assistant. The selected collections will be added to your collection list in SMA.";

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override void OnDisplayed()
    {
      HasBeenShown = true;

      var smBinPath = StartupCfg.SuperMemo.SMBinPath;

      if (string.IsNullOrWhiteSpace(smBinPath) || File.Exists(smBinPath) == false)
      {
        LogTo.Warning("ImportCollections.OnDisplayed called with a null or non-existent file, for smBinPath: {SmBinPath}", smBinPath);
        return;
      }

      var smDir         = new FilePath(smBinPath).Directory;
      var smIniFilePath = smDir.CombineFile("bin\\supermemo.ini");

      if (smIniFilePath.Exists() == false)
      {
        LogTo.Warning("supermemo.ini doesn't exist at '{FullPathWin}'. smBinPath: {SmBinPath}", smIniFilePath.FullPathWin, smBinPath);
        return;
      }

      IniFile smIniFile = new IniFile();
      smIniFile.Load(smIniFilePath.FullPathWin, true);

      var colDictionary = StartupCfg.Collections.ToDictionary(c => c.GetKnoFilePath().ToLower(CultureInfo.InvariantCulture));
      var systemSection = smIniFile["Systems"];

      foreach (var systemLine in systemSection)
      {
        if (string.IsNullOrWhiteSpace(systemLine.Key) || systemLine.Key.StartsWith("System", StringComparison.OrdinalIgnoreCase) == false)
        {
          LogTo.Warning("Found an unknown key in supermemo.ini's [Systems] section: {SystemLine}", systemLine);
          continue;
        }

        var collectionPath = systemLine.Value?.ToString();
        if (string.IsNullOrWhiteSpace(collectionPath) || Directory.Exists(collectionPath) == false
          || File.Exists(collectionPath + ".kno")                                         == false)
        {
          LogTo.Warning("Found a non-existing collection path in supermemo.ini's [Systems] section: '{SystemLine}'", systemLine);
          continue;
        }

        var smImportCol              = new SMImportCollection(collectionPath);
        var smImportColAlreadyExists = colDictionary.ContainsKey(smImportCol.GetKnoFilePath().ToLower(CultureInfo.InvariantCulture));

        smImportCol.IsEnabled = smImportColAlreadyExists == false;
        smImportCol.IsChecked = smImportColAlreadyExists;

        Collections.Add(smImportCol);
      }
    }

    /// <inheritdoc />
    public override void OnNext()
    {
      var colDictionary = StartupCfg.Collections.ToDictionary(c => c.GetKnoFilePath().ToLower(CultureInfo.InvariantCulture));

      foreach (var smImportCol in Collections.Where(ic => ic.IsChecked))
      {
        if (colDictionary.ContainsKey(smImportCol.GetKnoFilePath().ToLower(CultureInfo.InvariantCulture)))
          continue;

        StartupCfg.Collections.Add(new SMCollection(smImportCol.GetKnoFilePath(), DateTime.Now));
      }

      HasImportedCollections = true;
      SuperMemoAssistant.SMA.Core.Configuration.Save<CoreCfg>(StartupCfg);
    }

    #endregion




    #region Methods

    private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      var collection = (SMImportCollection)lbCollections.SelectedItem;

      if (collection.IsEnabled == false)
        return;

      collection.IsChecked = !collection.IsChecked;
    }

    #endregion
  }
}
