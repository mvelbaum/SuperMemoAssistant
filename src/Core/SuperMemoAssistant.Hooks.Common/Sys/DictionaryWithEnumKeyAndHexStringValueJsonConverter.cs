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
// Created On:   2020/01/11 15:22
// Modified On:  2020/01/11 15:50
// Modified By:  Alexis

#endregion




using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SuperMemoAssistant.Sys
{
  public sealed class DictionaryWithEnumKeyAndHexStringValueJsonConverter : JsonConverter
  {
    #region Properties Impl - Public

    public override bool CanRead => true;

    public override bool CanWrite => false;

    #endregion




    #region Methods Impl

    public override bool CanConvert(Type objectType)
    {
      return true;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      throw new NotSupportedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.Null) return null;
      
      var genericArguments = objectType.GetGenericArguments();
      var keyType          = genericArguments[0];

      // read to a dictionary with string key
      var intermediateDictionary = new Dictionary<string, string>();

      serializer.Populate(reader, intermediateDictionary);

      // convert to a dictionary with enum key
      var finalDictionary = (IDictionary)Activator.CreateInstance(objectType);

      foreach (var keyVal in intermediateDictionary)
      {
        (string keyStr, string valueStr) = (keyVal.Key, keyVal.Value);

        // Convert key to enum member
        var key = ToEnum(keyType, keyStr);

        // Convert value from hex to int
        if (string.IsNullOrWhiteSpace(valueStr) || !valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
          throw new JsonSerializationException($"Expected hexadecimal string, got {valueStr}");

        var value = Convert.ToInt32(valueStr.Substring(2), 16);

        // Add key/value to dictionary
        finalDictionary.Add(key, value);
      }

      return finalDictionary;
    }

    #endregion




    #region Methods

    private static object ToEnum(Type enumType, string str)
    {
      return Enum.Parse(enumType, str);
    }

    #endregion
  }
}
