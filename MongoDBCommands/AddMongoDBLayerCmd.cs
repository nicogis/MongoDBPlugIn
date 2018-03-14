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
using ESRI.ArcGIS.ADF.BaseClasses;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Controls;
using MongoDBPluginUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using MongoDBPlugIn.Utilities;

namespace MongoDBPlugIn
{
  [Guid("8F18AC6A-60EB-48DB-BDF1-6B125D04CF11")]
  [ClassInterface(ClassInterfaceType.None)]
  [ProgId("MongoDBPlugIn.AddMongoDBLayerCmd")]
  [ComVisible(true)]
  public class AddMongoDBLayerCmd
       : BaseCommand
  {

    #region COM Registration Function(s)
    [ComRegisterFunction()]
    [ComVisible(false)]
    static void RegisterFunction(Type registerType)
    {
      ArcGISCategoryRegistration(registerType);
    }

    [ComUnregisterFunction()]
    [ComVisible(false)]
    static void UnregisterFunction(Type registerType)
    {
      ArcGISCategoryUnregistration(registerType);
    }

    #region ArcGIS Component Category Registrar generated code
    /// <summary>
    /// Required method for ArcGIS Component Category registration -
    /// Do not modify the contents of this method with the code editor.
    /// </summary>
    private static void ArcGISCategoryRegistration(Type registerType)
    {
      string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
      MxCommands.Register(regKey);
      ControlsCommands.Register(regKey);
    }
    /// <summary>
    /// Required method for ArcGIS Component Category unregistration -
    /// Do not modify the contents of this method with the code editor.
    /// </summary>
    private static void ArcGISCategoryUnregistration(Type registerType)
    {
      string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
      MxCommands.Unregister(regKey);
      ControlsCommands.Unregister(regKey);
    }

    #endregion
    #endregion

    private IHookHelper m_hookHelper = null;
    public AddMongoDBLayerCmd()
    {
      base.m_category = "Mongo PlugIn Commands";
      base.m_caption = "Add MongoDB data layer";
      base.m_message = "Add MongoDB data layer to the map";
      base.m_toolTip = "Add MongoDB data layer";
      base.m_name = "MongoDBPlugIn_AddMongoDBLayerCmd";

      try
      {
        //string bitmapResourceName = GetType().Name + ".bmp";
        //base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
      }
    }

    #region Overriden Class Methods

    /// <summary>
    /// Occurs when this command is created
    /// </summary>
    /// <param name="hook">Instance of the application</param>
    public override void OnCreate(object hook)
    {
      if (hook == null)
        return;

      try
      {
        m_hookHelper = new HookHelperClass();
        m_hookHelper.Hook = hook;
        if (m_hookHelper.ActiveView == null)
          m_hookHelper = null;
      }
      catch
      {
        m_hookHelper = null;
      }

      if (m_hookHelper == null)
        base.m_enabled = false;
      else
        base.m_enabled = true;
    }



    /// <summary>
    /// Occurs when this command is clicked
    /// </summary>
    public override void OnClick()
    {
      try
      {
        IMongoDbDialogVM dbDialog = UIUtils.GetDialogVM();

        ButtonInfo okBtn;
        okBtn.OnClick = (() =>
        {
          string connString = dbDialog.File;

          if (String.IsNullOrEmpty(connString))
            return;

          string selectedFC = dbDialog.GetSelectedFCName();

          if (String.IsNullOrEmpty(selectedFC))
            return;

          //get the type using the ProgID
          Type t = Type.GetTypeFromProgID("esriGeoDatabase.MongoDBPluginWorkspaceFactory");
          //Use activator in order to create an instance of the workspace factory
          IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(t);
          IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(connString, 0);

          //get a featureclass from the workspace
          IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(selectedFC);
          //create a new feature layer and add it to the map
          IFeatureLayer featureLayer = new FeatureLayerClass();
          featureLayer.Name = featureClass.AliasName;
          featureLayer.FeatureClass = featureClass;
          m_hookHelper.FocusMap.AddLayer((ILayer)featureLayer);
          dbDialog.Close();
        });

        okBtn.IsEnabled = null;
        dbDialog.SetOk(okBtn);

        ButtonInfo cancelBtn;
        cancelBtn.OnClick = () =>
        {
          dbDialog.Close();
        };
        cancelBtn.IsEnabled = null;
        dbDialog.SetCancel(cancelBtn);

        ButtonInfo browseBtn;
        browseBtn.OnClick = () =>
        {
          string result = UIUtils.BrowseToFile(null, "Connection File to MongoDB (.mongoconn)|*.mongoconn", false);
          if (String.IsNullOrEmpty(result))
            return;

          string connInfoStr = ConnectionUtilities.DecodeConnFile(result);
          MongoDBConnInfo connInfo = ConnectionUtilities.ParseConnectionString(connInfoStr);
          dbDialog.DatabaseText = connInfo.DBName;
          dbDialog.ServerText = connInfo.Connection.ToString();
          dbDialog.File = result;

          //get the type using the ProgID
          Type t = Type.GetTypeFromProgID("esriGeoDatabase.MongoDBPluginWorkspaceFactory");
          //Use activator in order to create an instance of the workspace factory
          IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(t);
          IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(result, 0);

          IEnumDatasetName ipNames = ((IWorkspace)featureWorkspace).get_DatasetNames(esriDatasetType.esriDTFeatureClass);

          List<string> dsNames = new List<string>();
          IDatasetName ipCurr = ipNames.Next();
          while (ipCurr != null)
          {
            dsNames.Add(ipCurr.Name);
            ipCurr = null;
            ipCurr = ipNames.Next();
          }

          dbDialog.ClearFCList();
          if (dsNames.Count > 0)
            dbDialog.SetFCNames(dsNames);
        };
        browseBtn.IsEnabled = null;
        dbDialog.SetBrowse(browseBtn);

        UIUtils.DisplayMongoBrowseDialog(dbDialog);


      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine(ex.Message);
      }
    }

    #endregion
  }

}

