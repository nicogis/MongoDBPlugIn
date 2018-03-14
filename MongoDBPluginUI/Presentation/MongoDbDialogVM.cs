//  Copyright 2012 ESRI

//  All rights reserved under the copyright laws of the United States.

//  You may freely redistribute and use this sample code, with or without modification.

//  Disclaimer: THE SAMPLE CODE IS PROVIDED "AS IS" AND ANY EXPRESS OR IMPLIED
//  WARRANTIES, INCLUDING THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
//  FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL ESRI OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY,
//  OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
//  SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
//  INTERRUPTION) SUSTAINED BY YOU OR A THIRD PARTY, HOWEVER CAUSED AND ON ANY
//  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT ARISING IN ANY
//  WAY OUT OF THE USE OF THIS SAMPLE CODE, EVEN IF ADVISED OF THE POSSIBILITY OF
//  SUCH DAMAGE.

//  For additional information contact: Environmental Systems Research Institute, Inc.
//  Attn: Contracts Dept.

//  380 New York Street

//  Redlands, California, U.S.A. 92373

//  Email: contracts@esri.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MongoDBPluginUI
{
  public class FeatureClassList 
    : ObservableCollection<string>
  { }

  public class MongoDbDialogVM
      : MongoDbBrowserVM,
        IMongoDbDialogVM
  {
    public MongoDbDialogVM()
    {
      FeatureClasses = new FeatureClassList();
    }

    public void Close()
    {
      Closing(this, new EventArgs());
    }
    public EventHandler Closing;

    public ICommand OnOk
    {
      get;
      private set;
    }

    public ICommand OnCancel
    {
      get;
      private set;
    }

    public void SetOk(ButtonInfo bi)
    {
      OnOk = new ButtonCmd(bi);
    }

    public void SetCancel(ButtonInfo bi)
    {
      OnCancel = new ButtonCmd(bi);
    }

    FeatureClassList _FeatureClasses;
    public FeatureClassList FeatureClasses
    {
      get
      {
        return _FeatureClasses;
      }
      set
      {
        if (value != _FeatureClasses)
        {
          _FeatureClasses = value;
          OnPropertyChanged("FeatureClasses");

        }
      }
    }
    
    int _selectedFC;
    public int SelectedFC
    {
      get
      {
        return _selectedFC;
      }
      set
      {
        if (value != _selectedFC)
        {
          _selectedFC = value;
          OnPropertyChanged("SelectedFC");
        }
      }

    }

    public void ClearFCList()
    {
      FeatureClasses.Clear();

      OnPropertyChanged("SelectedFC");

    }

    public string GetSelectedFCName()
    {
      if (SelectedFC == -1)
        return null;

      return FeatureClasses[SelectedFC];
    }

    public void SetFCNames(IEnumerable<string> names)
    {
      foreach (var name in names)
        FeatureClasses.Add(name);
    }

  }

}
