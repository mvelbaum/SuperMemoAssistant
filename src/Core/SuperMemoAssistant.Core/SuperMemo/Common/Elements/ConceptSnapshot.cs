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
// Created On:   2018/09/04 19:55
// Modified On:  2018/09/04 19:56
// Modified By:  Alexis

#endregion




using System;
using SuperMemoAssistant.SMA;

namespace SuperMemoAssistant.SuperMemo.Common.Elements
{
  using System.Windows;
  using Anotar.Serilog;

  public sealed class ConceptSnapshot : IDisposable
  {
    #region Properties & Fields - Non-Public

    private int ConceptId { get; set; }

    #endregion




    #region Constructors

    public ConceptSnapshot()
    {
      ConceptId = Core.SM.UI.ElementWdw.CurrentConceptId;
    }

    /// <inheritdoc />
    public void Dispose()
    {
      try
      {
        if (Core.SM.UI.ElementWdw.CurrentConceptId != ConceptId)
          Core.SM.UI.ElementWdw.SetCurrentConcept(ConceptId);
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, "Failed to restore concept context back to element {ConceptId}.", ConceptId);
        MessageBox.Show($@"Failed to restore concept context back to element {ConceptId}..
Your concept might have been changed.

Exception: {ex}",
                        "Warning");
      }
    }

    #endregion
  }
}
