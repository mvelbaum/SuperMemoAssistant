#region License & Metadata

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




namespace SuperMemoAssistant.Setup.Screens
{
  using System.Collections.Specialized;
  using System.Windows.Controls;
  using Plugins;
  using UI.DataTemplates;
  using UI.ViewModels;

  /// <summary>Interaction logic for PluginSetup.xaml</summary>
  public partial class PluginSetup : SMASetupScreenBase
  {
    #region Properties & Fields - Non-Public

    /// <summary>
    ///   This View Model is a singleton to preserve the state of the package manager across UI navigation operations
    /// </summary>
    private PluginManagerViewModel ViewModel => PluginManagerViewModel.Instance;

    #endregion




    #region Constructors

    public PluginSetup()
    {
      InitializeComponent();

      Resources.MergedDictionaries.Add(new OnlinePluginPackageDataTemplate().Resources);
      Resources.MergedDictionaries.Add(new LocalPluginPackageDataTemplate().Resources);

      //teOperationLogs.TextArea.DefaultInputHandler.NestedInputHandlers.Remove(
      //  teOperationLogs.TextArea.DefaultInputHandler.CaretNavigation);
    }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override bool IsSetup => SMAPluginManager.Instance.AllPlugins.Count > 0;

    /// <inheritdoc />
    public override string ListTitle => "Plugins";

    /// <inheritdoc />
    public override string WindowTitle => "Plugins";

    /// <inheritdoc />
    public override string Description =>
      "*SuperMemo Assistant* adds new functionalities to SM through its **plugins**.\nInstall one or a few plugins to use SMA.";

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override void OnDisplayed()
    {
      // Delay instantiation of the ViewModel as long as possible
      DataContext ??= ViewModel;

      ((INotifyCollectionChanged)SMAPluginManager.Instance.AllPlugins).CollectionChanged += PluginCollectionChanged;
    }

    /// <inheritdoc />
    public override void OnNext()
    {
      ((INotifyCollectionChanged)SMAPluginManager.Instance.AllPlugins).CollectionChanged -= PluginCollectionChanged;
    }

    #endregion




    #region Methods

    /// <summary>Monitor <see cref="SMAPluginManager.AllPlugins" /> for installed plugins</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PluginCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      OnPropertyChanged(nameof(IsSetup));
    }

    #endregion

    private void teOperationLogs_TextChanged(object sender, System.EventArgs e)
    {
      if (teOperationLogs.Document != null && teOperationLogs.CaretOffset >= teOperationLogs.Document.TextLength)
      {
        teOperationLogs.ScrollToEnd();
        teOperationLogs.CaretOffset = teOperationLogs.Document.TextLength;
      }
    }
  }
}
