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
using ESRI.ArcGIS.Geodatabase;
using MongoDB.Bson;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;

namespace MongoDBPlugIn.Utilities
{
  /// <summary>
  /// Utilties helpful for loading Feature Class data into MongoDB
  /// </summary>
  static public class DataLoadUtilities
  {
    /// <summary>
    /// Given a set of fields creates a set of fields with necessary field name changes
    /// ie.: OID must be _id and SHAPE must be shape
    /// </summary>
    /// <param name="ipSrcFields">Fields from the table we are importing</param>
    /// <returns>A new fieldset with the correct fields</returns>
    static public IFields GetCreatableFields(IFields ipSrcFields)
    {
      // to-do: add field name verification

      IFieldsEdit ipFields = new FieldsClass();
      int count = ipSrcFields.FieldCount;
      bool timeAlreadyAdded =false;
      for (int i = 0; i < count; i++)
      {
        IClone ipClone = (IClone)ipSrcFields.get_Field(i);
        IFieldEdit ipField = (IFieldEdit)ipClone.Clone();
        switch (ipField.Type)
        {
          case esriFieldType.esriFieldTypeGeometry:
            ipField.Name_2 = CommonConst.SHAPEFIELD;
            break;
          case esriFieldType.esriFieldTypeOID:
            ipField.Name_2 = CommonConst.OID;
            break;
          case esriFieldType.esriFieldTypeDate:
            ipField.Name_2 = CommonConst.TIME;
            if (timeAlreadyAdded)
              throw new Exception("Only one time field allowed");
            timeAlreadyAdded = true;
            break;
          default:
            break;
        }
        ipFields.AddField((IField)ipField);
      }
      return ipFields;
    }

    /// <summary>
    /// helper class holding information about the fields
    /// </summary>
    private class FieldInfo
    {
      internal int Idx;
      internal string Name;
      internal esriFieldType Type;
    }

    /// <summary>
    /// During loading we place data in a buffer of BSON Documents
    /// This constant determines the size of that buffer
    /// </summary>
    private const int BUFFERLEN = 200;
    
    /// <summary>
    /// Pulls data from the source feature class and loads it into the new dataset
    /// </summary>
    /// <param name="ipSrc">The feature class we are loading from</param>
    /// <param name="ipTarget">The dataset we are loading to</param>
    public static void LoadData(IFeatureClass ipSrc, MongoDBDataset ipTarget)
    {
      // only bring fields which we know are compatible
      // at this point we do not bring BLOBs, GUIDs, etc
      // though that could be added in the future
      List<FieldInfo> fieldMap = new List<FieldInfo>();
      for (int i = 0; i < ipSrc.Fields.FieldCount; i++)
      {
        IField ipField = ipSrc.Fields.get_Field(i);
        switch (ipField.Type)
        {
          case esriFieldType.esriFieldTypeDate:
          case esriFieldType.esriFieldTypeDouble:
          case esriFieldType.esriFieldTypeInteger:
          case esriFieldType.esriFieldTypeSingle:
          case esriFieldType.esriFieldTypeSmallInteger:
          case esriFieldType.esriFieldTypeString:
            fieldMap.Add(new FieldInfo() { Name = ipField.Name, Idx = i, Type = ipField.Type });
            break;
          default:
            continue;
        }
      }

      // create the buffer of BSON documents for loading
      // and initialize
      BsonDocument[] buffer = new BsonDocument[BUFFERLEN];
      for (int i = 0; i < BUFFERLEN; i++)
        buffer[i] = new BsonDocument();


      IFeatureCursor ipFromCursor = ipSrc.Search(null, true);
      IFeature ipCurrent = ipFromCursor.NextFeature();
      int bufferIdx = -1;
      while (null != ipCurrent)
      {

        if (bufferIdx == (BUFFERLEN - 1)) // flush the buffer
        {
          bufferIdx = 0;
          ipTarget.InsertBlock(buffer);
        }
        else
          bufferIdx++;

        // load the current buffer from the feature
        LoadValues(ipCurrent, fieldMap, buffer[bufferIdx]);

        //move to next
        ipCurrent = null;
        ipCurrent = ipFromCursor.NextFeature();

      } // end while
      bufferIdx++;

      if (bufferIdx < BUFFERLEN) // if there are unwritten records, flush them now
      {
        BsonDocument[] remainingBuffer = new BsonDocument[bufferIdx];
        System.Array.Copy(buffer, remainingBuffer, bufferIdx);
        ipTarget.InsertBlock(remainingBuffer);
      }

      // keep indices fresh
      ipTarget.ReIndex();
    }

    /// <summary>
    /// Load a single feature's information into a BSON document
    /// </summary>
    /// <param name="ipSrc">The feature class we are loading from</param>
    /// <param name="fieldMap">Information on the fields</param>
    /// <param name="target">The BSON document we are loading into</param>
    private static void LoadValues(IFeature ipSrc, List<FieldInfo> fieldMap, BsonDocument target)
    {
      if ((ipSrc.Shape == null) || (ipSrc.Shape.IsEmpty))
        return;
      // get shape, transform it into a BSON value, and add it to the document
      IPoint pt = (IPoint)ipSrc.Shape;
      target.Set(CommonConst.SHAPEFIELD, BsonValue.Create(esriBsonUtilities.PointToBson(pt)));

      // loop the fields and pull values into the document
      // note that we filtered the fields based on field type earlier - no BLOBS, etc
      foreach (var field in fieldMap)
      {
        object val = ipSrc.get_Value(field.Idx);
        if (val != DBNull.Value)
        {
        switch (field.Type)
        {
          case esriFieldType.esriFieldTypeDate:
            DateTime dt = (DateTime)val;
            target.Set(CommonConst.TIME, BsonValue.Create(dt));
            break;
          default:
            target.Set(field.Name, BsonValue.Create(val));
            break;
        }
          }
      }
    }
  }
}
