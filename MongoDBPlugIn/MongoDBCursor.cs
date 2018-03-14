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
using MongoDBPlugIn.Utilities;
using MongoDB.Driver.Builders;

namespace MongoDBPlugIn
{

  /// <summary>
  /// Implementation of IPlugInCursorHelper
  /// Mediates between MongoDBs cursor system and ArcGIS Plug-in datasource architecture
  /// </summary>
  [ComVisible(false)]
  internal class MongoDBCursor
      : IPlugInCursorHelper
  {
    /// <summary>
    /// This constructor is only used when querying for a single feature by its OID
    /// </summary>
    /// <param name="docs">the collection of documents we will be querying</param>
    /// <param name="OID">the oid of the desired feature</param>
    /// <param name="shapeName">the name of the shape field</param>
    /// <param name="fieldMapping">the mapping of which fields should be returned</param>
    /// <param name="fields">the fieldset associated with the dataset</param>
    internal MongoDBCursor(MongoCollection<BsonDocument> docs, int OID, string shapeName, System.Array fieldMapping, IFields fields)
    {
      List<string> includedFields = new List<string>();
      for (int i = 0; i < fieldMapping.Length; i++)
      {
        if (!fieldMapping.GetValue(i).Equals(-1))
          includedFields.Add(fields.get_Field(i).Name);
      }
      // always include oid
      if (!includedFields.Contains(CommonConst.OID))
        includedFields.Add(CommonConst.OID);

      // look for feature with the submitted object id
      QueryDocument QD = new QueryDocument();
      QD.Add(new BsonElement(CommonConst.OID, BsonValue.Create(OID)));
      MongoCursor<BsonDocument> cursor = docs.Find(QD);
      
      cursor.SetFields(includedFields.ToArray());
      m_Enumerator = cursor.ToList().GetEnumerator();
      
      // the expectation is that the cursor will be set to the first item after construction
      try
      {
        NextRecord();
      }
      catch (Exception)
      { 
        // we need to be exception safe
        // if NextRecord failed the cursor should simply be set to done
      }
      m_ShapeName = shapeName;
      m_FieldMapping = fieldMapping;
    }

    /// <summary>
    /// use this constructors for all queries other than those
    ///  based on finding a single feature by oid
    /// </summary>
    /// <param name="docs">the collection of documents that will be queried</param>
    /// <param name="env">An optional envelope for simple spatial queries</param>
    /// <param name="query">An optional query supporting non-spatial queries</param>
    /// <param name="shapeName">The name of the shape field</param>
    /// <param name="fieldMapping">which fields should be included</param>
    /// <param name="fields">field set for the dataset being queried</param>
    internal MongoDBCursor(MongoCollection<BsonDocument> docs, IEnvelope env, 
                                            IMongoQuery query, string shapeName, 
                                            Array fieldMapping, IFields fields)    
    {
      //build list of included fields
      List <string> includedFields = new List <string>();
      for (int i = 0; i < fieldMapping.Length; i++) 
      {
        if (! fieldMapping.GetValue(i).Equals(-1))
          includedFields.Add(fields.get_Field(i).Name);
      }
      // always include OID
      if (! includedFields.Contains(CommonConst.OID))
        includedFields.Add(CommonConst.OID);

      MongoCursor<BsonDocument> cursor;
      if ((env != null) && (query != null))
      {
        var spatClause = Query.WithinRectangle(shapeName, env.XMin, env.YMin, env.XMax, env.YMax);
        var qry = Query.And(spatClause, query);
        cursor = docs.Find(qry);
      }
      else if (env != null)
      {
        var qry = Query.WithinRectangle(shapeName, env.XMin, env.YMin, env.XMax, env.YMax);
        cursor = docs.Find(qry);
      } // if query is null or populated - this will work in either case
      else cursor = docs.Find(query);

      cursor.SetFields(includedFields.ToArray());
      m_Enumerator = cursor.GetEnumerator();

      // the expectation is that the cursor will be set to the first item after construction
      try
      {
        NextRecord();
      }
      catch (Exception)
      {
        // we need to be exception safe
        // if NextRecord failed the cursor should simply be set to done
      }
      m_ShapeName = shapeName;
      m_FieldMapping = fieldMapping;
    }


    /// <summary>
    /// The cursor needs to release its associated resources
    /// </summary>
    ~MongoDBCursor()
    {
      CursorDone();
    }

    #region IPlugInCursorHelper
    /// <summary>
    /// Have we iterated through all features
    /// </summary>
    /// <returns>Boolean</returns>
    public bool IsFinished()
    {
      return m_Done;
    }

    /// <summary>
    /// Attempts to advance to the next feature
    /// throws exception on failure
    /// </summary>
    public void NextRecord()
    {
      if (m_Done || (null == m_Enumerator))
      {
        m_CurrDoc = null;
        throw new COMException();
      }

      try
      {
        m_Done = !m_Enumerator.MoveNext();
      }
      catch (Exception)
      {
        //This isn't an error
        CursorDone();
      }
      if (m_Done)
      {
        m_CurrDoc = null;
        CursorDone();
        throw new COMException();
      }
      else
        m_CurrDoc = m_Enumerator.Current;
    }

    /// <summary>
    /// updates the geometry param with x and y from current document
    /// </summary>
    /// <param name="pGeometry">The geometry to update</param>
    public void QueryShape(IGeometry pGeometry)    
    {
      if (m_CurrDoc == null)
        throw new COMException();
      BsonElement shapeElem = m_CurrDoc.GetElement(m_ShapeName);
      esriBsonUtilities.BsonToGeometry(shapeElem, pGeometry);
    }

    /// <summary>
    /// loads a row with information in current document
    /// </summary>
    /// <param name="Row"></param>
    /// <returns></returns>
    public int QueryValues(IRowBuffer Row)
    {
      if (m_CurrDoc == null)
        throw new COMException();

      int retVal = -1;
      try
      {
        IFields fieldSet = Row.Fields;

        for (int i = 0; i < m_FieldMapping.GetLength(0); i++)
        {
          if (m_FieldMapping.GetValue(i).Equals(-1))
            continue;

          IField valField = fieldSet.get_Field(i);
          
          switch (valField.Type)
          {
            case esriFieldType.esriFieldTypeOID:
            {
              retVal = esriBsonUtilities.BsonToInt(m_CurrDoc.GetElement(CommonConst.OID));
              break;
            }
            case esriFieldType.esriFieldTypeInteger:
            case esriFieldType.esriFieldTypeSmallInteger:
            {
              BsonElement elem;
              if (!m_CurrDoc.TryGetElement(valField.Name, out elem))
                Row.set_Value(i, null);
              else
              {
                if (elem.Value is BsonNull)
                  Row.set_Value(i, null);
                else
                {
                  int val = esriBsonUtilities.BsonToInt(elem);
                  Row.set_Value(i, val);
                }
              }
              break;
            }
            case esriFieldType.esriFieldTypeDouble:
            case esriFieldType.esriFieldTypeSingle:
            {
              BsonElement elem;
              if (!m_CurrDoc.TryGetElement(valField.Name, out elem))
                Row.set_Value(i, null);
              else
              {
                if (elem.Value is BsonNull)
                  Row.set_Value(i, null);
                else
                {
                  double val = esriBsonUtilities.BsonToDouble(m_CurrDoc.GetElement(valField.Name));
                  Row.set_Value(i, val);
                }
              }
              break;
            }
            case esriFieldType.esriFieldTypeDate:
            {
              BsonElement elem;
              if (!m_CurrDoc.TryGetElement(valField.Name, out elem))
                Row.set_Value(i, null);
              else
              {
                if (elem.Value is BsonNull)
                  Row.set_Value(i, null);
                else
                {
                  DateTime val = esriBsonUtilities.BsonToDateTime(m_CurrDoc.GetElement(valField.Name));
                  Row.set_Value(i, val);
                }
              }
              break;
            }
            case esriFieldType.esriFieldTypeString:
            {
              BsonElement elem;
              if (!m_CurrDoc.TryGetElement(valField.Name, out elem))
                Row.set_Value(i, null);
              else
              {
                if (elem.Value is BsonNull)
                  Row.set_Value(i, null);
                else
                {
                  string val = esriBsonUtilities.BsonToString(m_CurrDoc.GetElement(valField.Name));
                  Row.set_Value(i, val);
                }
              }
              break;
            }
          }
        }
      }
      catch (COMException e)
      {
        // simply rethrow
        throw e;
      }
      catch (InvalidOperationException e)
      {
        CursorDone();
        return -1;
      }
      catch (Exception e)
      {
        // probably at the end
        throw new COMException(e.Message);
      }

      return retVal;
    }
    #endregion
    #region private
    /// <summary>
    /// Local utility to release the enumerator's resources when we are done with them
    /// </summary>
    private void CursorDone()
    {
      m_Done = true;
      if (m_Enumerator != null)
      {
        m_Enumerator.Dispose();
        m_Enumerator = null;
      }
    }

    BsonDocument m_CurrDoc;
    IEnumerator<BsonDocument> m_Enumerator;
    bool m_Done = false;
    string m_ShapeName;
    Array m_FieldMapping;
    #endregion
  }
}
