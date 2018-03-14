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
using ESRI.ArcGIS.Geometry;
using MongoDB.Driver;
using MongoDB.Bson;
using ESRI.ArcGIS.esriSystem;
using MongoDBPlugIn.Utilities;
using MongoDB.Driver.Builders;

namespace MongoDBPlugIn
{
  /// <summary>
  /// Mediates between MongoDB collections and ArcGIS Plug-in Datasource architecture
  /// </summary>
  [ComVisible(false)]
  public class MongoDBDataset
      : IPlugInDatasetHelper,
        IPlugInDatasetHelper2,
        IPlugInDatasetInfo,
        IPlugInFastRowCount
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="entry">metadata (i.e. extent and fields)</param>
    /// <param name="conn">Database it is located in</param>
    public MongoDBDataset(CatalogDatasetEntry entry, MongoDatabase conn)
    {
      m_Connection = conn;
      m_Entry = entry;
    }

    /// <summary>
    /// Provides support for buffered insert
    /// </summary>
    /// <param name="buffer">the block of documents to insert</param>
    internal void InsertBlock(IEnumerable<BsonDocument> buffer)
    {
      // we need to update their OIDs with unique values
      foreach (var curr in buffer)
        curr.SetElement(new BsonElement(CommonConst.OID, m_Entry.GetNextOID()));

      m_Connection[m_Entry.Name].InsertBatch(buffer);
    }

    /// <summary>
    /// Refigures indexes on the collections
    /// </summary>
    public void ReIndex()
    {
      m_Connection[m_Entry.Name].ReIndex();
    }

    #region IPlugInFastRowCount
    /// <summary>
    /// provides a fast count of features
    /// not necessary but improves performance significantly
    /// </summary>
    public int RowCount
    {
      get
      {
        long rowCount = m_Connection[m_Entry.Name].Count();
        if (int.MaxValue < rowCount)
          return int.MaxValue;
        return (int) rowCount;
      }
    }
    #endregion

    #region IPlugInDatasetHelper
    /// <summary>
    /// Gets the extent of the dataset. Serialized in metadata
    /// </summary>
    public IEnvelope Bounds
    {
      get
      {
        return m_Entry.Extent;
      }
    }

    /// <summary>
    /// As feature datasets are not supported
    /// Always return 1
    /// </summary>
    public int ClassCount
    {
      get
      {
        return 1;
      }
    }

    /// <summary>
    /// Gets a cursor helper given a filter
    /// </summary>
    /// <param name="classIndex">ignore - feature datasets unsupported</param>
    /// <param name="env">Optional bounding box</param>
    /// <param name="strictSearch">ignore this parameter</param>
    /// <param name="whereClause">Optional where clause - unsupported</param>
    /// <param name="fidSet">Optional list of OIDs to select</param>
    /// <param name="fieldMap">Which fields are included in results</param>
    /// <returns></returns>
    public IPlugInCursorHelper FetchWithFilter(int classIndex, IEnvelope env, bool strictSearch, string WhereClause, IFIDSet fidSet, object fieldMap)
    {
      // without a OID list perform fetch by envelope
      if (null == fidSet)
        return this.FetchByEnvelope(classIndex, env, strictSearch, WhereClause, fieldMap);

      //to-do write SQL to IMongoQuery translator and assign to conditions if where clause is non-null
      IMongoQuery conditions = null;

      // build a mongo IN query of all OIDs
      int count = fidSet.Count();
      BsonArray ba = new BsonArray(count);
      fidSet.Reset();
      for (int i = 0; i < count; i++)
      {
        int oid;
        fidSet.Next(out oid);
        ba.Add(BsonValue.Create(oid));
      }

      if (conditions == null)
        conditions = Query.In(CommonConst.OID, ba);
      else
        conditions = Query.And(new IMongoQuery[] { Query.In(CommonConst.OID, ba), conditions });

      return new MongoDBCursor(m_Connection[m_Entry.Name], env, conditions, CommonConst.SHAPEFIELD, (System.Array)fieldMap, m_Entry.Fields);
    }

    /// <summary>
    /// Get all rows
    /// </summary>
    /// <param name="ClassIndex">Ignore - Feature Datasets unsupported</param>
    /// <param name="WhereClause">Currently unsupported</param>
    /// <param name="FieldMap">Which fields are included in results</param>
    /// <returns></returns>
    public IPlugInCursorHelper FetchAll(int ClassIndex, string whereClause, object fieldMap)
    {
      IMongoQuery conditions = null;
      //to-do write SQL to IMongoQuery translator and assign to conditions if where clause is non-null

      return new MongoDBCursor(m_Connection.GetCollection<BsonDocument>(m_Entry.Name), null, conditions, CommonConst.SHAPEFIELD, (System.Array)fieldMap, m_Entry.Fields);
    }

    /// <summary>
    /// Returns all features within a bounding box
    /// </summary>
    /// <param name="classIndex">Ignore - Feature Datasets unsupported</param>
    /// <param name="env">The bounding box used for searching</param>
    /// <param name="strictSearch">Ignore</param>
    /// <param name="whereClause">Currently unsupported</param>
    /// <param name="fieldMap">Which fields are included in results</param>
    /// <returns></returns>
    public IPlugInCursorHelper FetchByEnvelope(int classIndex, IEnvelope env, bool strictSearch, string whereClause, object fieldMap)    
    {
      IMongoQuery conditions = null;
      return new MongoDBCursor(m_Connection[m_Entry.Name], env, conditions, CommonConst.SHAPEFIELD, (System.Array)fieldMap, m_Entry.Fields);
    }

    /// <summary>
    /// REturns a cursor pointing to a single feature with a particular oid
    /// </summary>
    /// <param name="classIndex">Ignore - feature datasets are ignored</param>
    /// <param name="oid">object id of the returned feature</param>
    /// <param name="fieldMap">Which fields are included in results</param>
    /// <returns></returns>
    public IPlugInCursorHelper FetchByID(int classIndex, int oid, object fieldMap)
    {
      return new MongoDBCursor(m_Connection.GetCollection<BsonDocument>(m_Entry.Name), oid, CommonConst.SHAPEFIELD, (System.Array)fieldMap, m_Entry.Fields);
    }

    /// <summary>
    /// FIX
    /// </summary>
    /// <param name="Name"></param>
    /// <returns></returns>
    public int get_ClassIndex(string Name)
    {
      return 0;
    }

    /// <summary>
    /// FIX
    /// </summary>
    /// <param name="Index"></param>
    /// <returns></returns>
    public string get_ClassName(int Index)
    {
      return m_Entry.Name;
    }

    /// <summary>
    /// Gets the fields associated with the dataset
    /// </summary>
    /// <param name="ClassIndex">Unsupported - feature datasets unsupported in this implementation</param>
    /// <returns>The dataset's set of fields</returns>
    public IFields get_Fields(int ClassIndex)
    {
      if (m_HashFieldTypes == null)
      {
        m_HashFieldTypes = new Dictionary<string, esriFieldType>();
        int fieldCnt = m_Entry.Fields.FieldCount;
        for (int i = 0; i < fieldCnt; i++)
          m_HashFieldTypes.Add(m_Entry.Fields.get_Field(i).Name, m_Entry.Fields.get_Field(i).Type);
      }
      return m_Entry.Fields;
    }

    /// <summary>
    /// The index of the object id field in the fieldset of the dataset
    /// </summary>
    /// <param name="ClassIndex">Unsupported - feature datasets unsupported in this implementation</param>
    /// <returns>The index</returns>
    public int get_OIDFieldIndex(int ClassIndex)
    {
      for (int i = 0; i < m_Entry.Fields.FieldCount; i++)
        if (m_Entry.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeOID)
          return i;
      return -1;
    }

    /// <summary>
    /// The index of the shape field in the fieldset of the dataset 
    /// </summary>
    /// <param name="ClassIndex">Unsupported - feature datasets unsupported in this implementation</param>
    /// <returns>The index</returns>
    public int get_ShapeFieldIndex(int ClassIndex)
    {
      for (int i = 0; i < m_Entry.Fields.FieldCount; i++)
        if (m_Entry.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeGeometry)
          return i;
      return -1;
    }

    #endregion
    #region IPlugInDatasetInfo
    
    /// <summary>
    /// the type of dataset - only feature classes supported for now
    /// </summary>
    public esriDatasetType DatasetType
    {
      get
      {
        return esriDatasetType.esriDTFeatureClass;
      }
    }

    /// <summary>
    /// the type of geometry supported - only points are supported for now
    /// </summary>
    public esriGeometryType GeometryType
    {
      get
      {
        return esriGeometryType.esriGeometryPoint;
      }
    }

    /// <summary>
    /// The name of the dataset
    /// </summary>
    public string LocalDatasetName
    {
      get
      {
        return m_Entry.Name;
      }
    }

    /// <summary>
    /// The name of the shape field
    /// </summary>
    public string ShapeFieldName
    {
      get
      {
        return CommonConst.SHAPEFIELD;
      }
    }
    #endregion

    #region private members
    MongoDatabase m_Connection;
    Dictionary<string, esriFieldType> m_HashFieldTypes;
    CatalogDatasetEntry m_Entry;
    #endregion
  }
}
