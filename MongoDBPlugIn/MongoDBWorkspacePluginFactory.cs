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
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using System.IO;
using System.Runtime.InteropServices;
using MongoDB.Driver;
using MongoDBPlugIn.Utilities;
using ESRI.ArcGIS.ADF.CATIDs;
using System.Windows.Forms;
using System.Linq;

namespace MongoDBPlugIn
{
    /// <summary>
    /// Provides starting point for the plug-in datasource architecture to consume our
    /// plug in helpers. Workspace helpers are created here
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("3CC2CAC3-DDE8-4B66-80AB-E1FB0E49C88E")]
    [ProgId("MongoDBPlugIn.MongoDBWorkspacePluginFactory")]
    [ComVisible(true)]
    public class MongoDBWorkspacePluginFactory : IPlugInWorkspaceFactoryHelper, IPlugInCreateWorkspace
    {
        #region "Component Category Registration"

        [ComRegisterFunction()]
        public static void RegisterFunction(String regKey)
        {
            PlugInWorkspaceFactoryHelpers.Register(regKey);
        }

        [ComUnregisterFunction()]
        public static void UnregisterFunction(String regKey)
        {
            PlugInWorkspaceFactoryHelpers.Unregister(regKey);
        }
        #endregion

        /// <summary>
        /// Attempts opening a MongoDBWorkspace from its connection file path
        /// </summary>
        /// <param name="wksString">the path to a connection file</param>
        /// <returns>a mongodbworkspace</returns>
        public MongoDBWorkspace OpenMongoDBWorkspace(string wksString)
        {
            
            MongoDatabase mDB;
            try
            {
                string connString = ConnectionUtilities.DecodeConnFile(wksString);
                MongoDBConnInfo connInfo = ConnectionUtilities.ParseConnectionString(connString);
                mDB = ConnectionUtilities.OpenConnection(connInfo);
            }
            catch (Exception e)
            {
                throw new COMException(e.Message);
            }

            return new MongoDBWorkspace(mDB);
        }

        #region IPlugInWorkspaceFactoryHelper methods
        /// <summary>
        /// whether we support SQL - currently no
        /// </summary>
        public bool CanSupportSQL
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// whether a particular directory contains a workspace
        /// </summary>
        /// <param name="parentDirectory">the directory to check</param>
        /// <param name="fileNames">contained files</param>
        /// <returns>whether it contains a workspace</returns>
        public bool ContainsWorkspace(string parentDirectory, IFileNames fileNames)
        {
            //return this.IsWorkspace(parentDirectory);

            if (fileNames == null)
            {
                return this.IsWorkspace(parentDirectory);
            }

            if (!Directory.Exists(parentDirectory))
            {
                return false;
            }

            string fileName;

            while ((fileName = fileNames.Next()) != null)
            {

                if (fileNames.IsDirectory())
                {
                    continue;
                }

                if (Path.GetExtension(fileName).Equals(".mongoconn"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// name of the datasource
        /// Note: this progid "esriGeoDatabase.[DatasourceName]WorkspaceFactory"
        /// Is used to create the workspace factory wrapping this plug in helper
        /// </summary>
        public string DataSourceName
        {
            get
            {
                return "MongoDBPlugin";
            }
        }

        /// <summary>
        /// Gets a workspace string representing the workspace
        /// </summary>
        /// <param name="parentDirectory">directory</param>
        /// <param name="fileNames">files</param>
        /// <returns>string</returns>
        public string GetWorkspaceString(string parentDirectory, IFileNames fileNames)
        {

            //return the path to the workspace location if 

            if (!Directory.Exists(parentDirectory))
            {
                return null;
            }

            if (fileNames == null)
            {
                return parentDirectory;
            }


            string fileName;

            bool fileFound = false;

            while ((fileName = fileNames.Next()) != null)
            {
                if (fileNames.IsDirectory())
                {
                    continue;
                }

                if (Path.GetExtension(fileName).Equals(".mongoconn"))
                {
                    fileFound = true;
                    fileNames.Remove();
                    break;
                }
            }



            if (fileFound)
                return Path.Combine(parentDirectory, fileName);
            else
                return null;

            //if (this.IsWorkspace(parentDirectory))
            //     return parentDirectory;

            // return null;
        }

        /// <summary>
        /// Determins whether a string represents a workspace
        /// Note that workspace strings are file paths to conn files in this plugin
        /// </summary>
        /// <param name="wksString">the string (i.e. path to conn file)</param>
        /// <returns>whether it is or not</returns>
        public bool IsWorkspace(string wksString)
        {
            bool retVal = false;
            try
            {
                if (Directory.Exists(wksString))
                {
                    return Directory.GetFiles(wksString, "*.mongoconn").Length > 0;
                }

                string connString = ConnectionUtilities.DecodeConnFile(wksString);
                MongoDBConnInfo connInfo = ConnectionUtilities.ParseConnectionString(connString);
                ConnectionUtilities.OpenConnection(connInfo);
                retVal = true;
            }
            catch (Exception)
            {
                // this just means this isn't a proper string
            }

            return retVal;
        }

        /// <summary>
        /// Opens a IPlugInWorkspaceHelper for a particualar connection file
        /// </summary>
        /// <param name="wksString">connection file path</param>
        /// <returns>IPlugInWorkspaceHelper</returns>
        public IPlugInWorkspaceHelper OpenWorkspace(string wksString)
        {
            return (IPlugInWorkspaceHelper)OpenMongoDBWorkspace(wksString);
        }

        /// <summary>
        /// A mock UID for the factory - necessary for the plug-in factory to 
        /// masquerade as an ordianry workspace factory
        /// </summary>
        public UID WorkspaceFactoryTypeID
        {
            get
            {
                UID proxyUID = new UIDClass();
                proxyUID.Value = "{11EDE71D-928E-4546-B51F-E4A343C32D9E}";
                return proxyUID;
            }
        }

        /// <summary>
        /// the sort of workspace. As we are storing conn info in 
        /// conn files, this is file system
        /// </summary>
        public esriWorkspaceType WorkspaceType
        {
            get
            {
                return esriWorkspaceType.esriRemoteDatabaseWorkspace;
            }
        }

        /// <summary>
        /// Gets names of various datasets
        /// note we only support feature classes currently
        /// </summary>
        /// <param name="DatasetType"></param>
        /// <returns></returns>
        public string get_DatasetDescription(esriDatasetType DatasetType)
        {
            switch (DatasetType)
            {
                case esriDatasetType.esriDTFeatureClass:
                    return "MongoDB Feature Class";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets a description of the workspace
        /// </summary>
        /// <param name="plural">Whether we want the plural or ordinary form</param>
        /// <returns>string</returns>
        public string get_WorkspaceDescription(bool plural)
        {
            if (plural)
                return "MongoDB repostories";
            else
                return "MongoDB repository";
        }

        public string MakeWorkspaceString(string parentDirectory, string file, IPropertySet ConnectionProperties)
        {

            try
            {
                string[] values = Prompt.ShowDialog("Connection (example: 'mongodb://127.0.0.1/?safe=true,namedb')", "New Mongo Connection");

                if ((values.Length == 0) || (values.Any(x => string.IsNullOrWhiteSpace(x))))
                {
                    throw new Exception("Values wrong!");
                }

                string c = Path.Combine(parentDirectory, Path.ChangeExtension(values[1], "mongoconn"));
                File.WriteAllText(c, values[0]);
                MessageBox.Show("Connection created!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return c;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return null;
            
        }

        public void CreateWorkspace(string workspaceString)
        {
            throw new NotImplementedException();
        }
        #endregion

    }

    public static class Prompt
    {
        public static string[] ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 300,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Width =400 };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Label textLabelNameConn = new Label() { Left = 50, Top = 80, Text = "Name connection", Width=400};
            TextBox textBoxNameConn = new TextBox() { Left = 50, Top = 110, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 140, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textLabelNameConn);
            prompt.Controls.Add(textBoxNameConn);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? new string[] { textBox.Text, textBoxNameConn.Text } : new string[] { };
        }
    }
}
