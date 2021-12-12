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
// Created On:   2019/01/19 04:23
// Modified On:  2019/01/19 04:38
// Modified By:  Alexis

#endregion




using System;
using System.Windows;
using Anotar.Serilog;
using Process.NET.Utilities;

namespace SuperMemoAssistant.Sys.UIAutomation
{
  public sealed class FocusSnapshot : IDisposable
  {
    private readonly bool _useDispatcher;




    #region Constructors

    /// <summary>New Instance</summary>
    /// <param name="useDispatcher">Whether to restore on the UI thread</param>
    public FocusSnapshot(bool useDispatcher = false)
    {
      _useDispatcher = useDispatcher;
      WindowHandle = WindowHelper.GetForegroundWindow();
      //FocusedElement = Core.UIAutomation.FocusedElement();
    }

    /// <inheritdoc />
    public void Dispose()
    {
      try
      {
        //FocusedElement?.Focus();
        if (_useDispatcher)
          Application.Current.Dispatcher.InvokeAsync(() => WindowHelper.SetForegroundWindow(WindowHandle));

        else
          WindowHelper.SetForegroundWindow(WindowHandle);
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, "Failed to restore window {WindowHandle}", WindowHandle);
      }
    }

    #endregion




    #region Properties & Fields - Public

    public IntPtr WindowHandle { get; }
    //public AutomationElement FocusedElement { get; set; }

    #endregion
  }
}
