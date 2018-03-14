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
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using MongoDB.Driver;
using ESRI.ArcGIS.Geometry;
using MongoDBPlugIn.Utilities;

namespace MongoDBPlugIn
{


  /// <summary>
  /// Mediates between IPlugInWorkspaceHelper clients and a MongoDatabase
  /// </summary>
  [ComVisible(false)]
  public class MongoDBWorkspace
      : IPlugInWorkspaceHelper
    {


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conn">The database to use</param>
        internal MongoDBWorkspace(MongoDatabase conn)
    {
      m_Connection = conn;
      m_CatalogDS = new CatalogDataset(conn);
    }
    
    /// <summary>
    /// Internal dataset creator function. Creates all necessary metadata
    /// </summary>
    /// <param name="Name">Name of the dataset</param>
    /// <param name="FieldSet">Fields associated with the dataset</param>
    /// <param name="extent">The geographic extent of the dataset</param>
    /// <returns>A newly created MongoDBDataset</returns>
    public MongoDBDataset CreateDataset(string Name, IFields FieldSet, IEnvelope extent)
    {
      if (m_Connection.GetCollectionNames().Contains(Name))
        throw new COMException("Dataset " + Name + " already exists");

      m_Connection.CreateCollection(Name);
      
      var bsonIdex = new IndexKeysDocument { { CommonConst.SHAPEFIELD, "2d" } };
      m_Connection[Name].EnsureIndex(bsonIdex);

      CatalogDatasetEntry dsEntry = new CatalogDatasetEntry(this.m_CatalogDS);
      dsEntry.Name = Name;
      dsEntry.Extent = extent;
      dsEntry.Fields = FieldSet;
      dsEntry.Save();
      return new MongoDBDataset(dsEntry, m_Connection);
    }
    

    #region IPlugInWorkspaceHelper
    /// <summary>
    /// Checkes whether the OID is a record number
    /// </summary>
    public bool OIDIsRecordNumber
    {
      get
      { 
        return true; 
      }
    }

    /// <summary>
    /// Opens a dataset given its name
    /// </summary>
    /// <param name="localName">the name of the dataset</param>
    /// <returns>a new IPlugInDatasetHelper</returns>
    public IPlugInDatasetHelper OpenDataset(string localName)    
    {
      CatalogDatasetEntry entry = m_CatalogDS.GetEntry(localName);
      return new MongoDBDataset(entry, m_Connection);
    }

    /// <summary>
    /// Whether the plug-in architecture needs to perform row counts through brute force
    /// iteration - we use MongoDB's count function, so return false
    /// </summary>
    public bool RowCountIsCalculated
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Gets a list of all dataset names of the particular dataset type
    /// We only support feature classes currently
    /// </summary>
    /// <param name="DatasetType">the type of dataset</param>
    /// <returns>an array of IPlugInDatasetInfo</returns>
    public IArray get_DatasetNames(esriDatasetType DatasetType)
    {
      if (DatasetType != esriDatasetType.esriDTAny &&
          DatasetType != esriDatasetType.esriDTFeatureClass)
        return null;

      IArray retVal = new ArrayClass();

      var names = m_CatalogDS.GetAllEntries();
      foreach (var name in names)
        retVal.Add(new MongoDBDataset(name, m_Connection));

      return retVal;
    }

    /// <summary>
    /// The native type if supported
    /// </summary>
    /// <param name="DatasetType">type of dataset</param>
    /// <param name="localName">local name</param>
    /// <returns>it's native type</returns>
    public INativeType get_NativeType(esriDatasetType DatasetType, string localName)
    {
      return null;
    }
    #endregion
    
    #region private members
    MongoDatabase m_Connection;
    CatalogDataset m_CatalogDS;
    #endregion

  }
}
