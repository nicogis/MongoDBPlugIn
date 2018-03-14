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

using Microsoft.Win32;
using System;
namespace MongoDBPluginUI
{
  public static class UIUtils
  {
    public static string BrowseToFile(string currFile, string filePattern, bool multiselect)
    {
      string retVal = null;
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = filePattern;
      ofd.Multiselect = multiselect;
      if (!string.IsNullOrEmpty(currFile))
        ofd.FileName = currFile;

      bool? succ = ofd.ShowDialog();
      if (succ.HasValue && succ.Value)
        retVal = ofd.FileName;

      return retVal;
    }

    public static IMongoDbDialogVM GetDialogVM()
    {
      return new MongoDbDialogVM();
    }

    public static IMongoDbLoaderDlgVm GetLoaderVM()
    {
      return new MongoLoaderDlgVM();
    }

    public static void DisplayLoaderDialog(IMongoDbLoaderDlgVm mod)
    {
      MongoLoaderDlgVM model = (MongoLoaderDlgVM)mod;
      MongoDbLoaderDLG dlg = new MongoDbLoaderDLG();
      EventHandler tmp = (o, e) => dlg.Close();
      dlg.DataContext = model;
      model.Closing += tmp;
      dlg.ShowDialog();
      model.Closing -= tmp;
      dlg.DataContext = null;
    }

    public static void DisplayMongoBrowseDialog(IMongoDbDialogVM mod)
    {
      MongoDbDialogVM model = (MongoDbDialogVM)mod;
      MongoBrowserDlg dlg = new MongoBrowserDlg();
      EventHandler tmp = (o, e) => dlg.Close();
      dlg.DataContext = model;
      model.Closing += tmp;
      dlg.ShowDialog();
      model.Closing -= tmp;
      dlg.DataContext = null;
    }
  }
}