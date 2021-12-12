﻿namespace SuperMemoAssistant.UI.Settings.Models
{
  /// <summary>
  /// Current status/operation being processed by the Plugin Manager
  /// </summary>
  public enum PluginManagerStatus
  {
    Error,
    Display,
    Refresh,
    Install,
    Uninstall,
    Update,
  }
}
