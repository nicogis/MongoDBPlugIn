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
using MongoDB.Bson;
using ESRI.ArcGIS.Geometry;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace MongoDBPlugIn.Utilities
{
  /// <summary>
  /// utilities pertaining to putting data into BSON or taking it out again
  /// </summary>
  static class esriBsonUtilities
  {

    /// <summary>
    /// Gets a string value from its BSON Element 
    ///  ie. { Name : 'Jones'} returns "Jones"
    /// </summary>
    /// <param name="stringStuff">The element</param>
    /// <returns>The string value</returns>
    internal static String BsonToString(BsonElement stringStuff)
    {
      try
      {
        return stringStuff.Value.AsString;
      }
      catch (InvalidCastException)
      {
        throw new COMException("Incorrect value type");
      }
    }

    /// <summary>
    /// Gets a datetime value from its BSON Element
    /// </summary>
    /// <param name="dateStuff">The element</param>
    /// <returns>The DateTime Value</returns>
    internal static DateTime BsonToDateTime(BsonElement dateStuff)
    {
      try
      {
        return  dateStuff.Value.AsDateTime;
      }
      catch (InvalidCastException)
      {
        throw new COMException("Incorrect value type");
      }
    }

    /// <summary>
    /// Gets an integer value from its BSON Element
    /// </summary>
    /// <param name="intStuff">The element</param>
    /// <returns>The integer value</returns>
    internal static int BsonToInt(BsonElement intStuff)
    {
      try
      {
        return intStuff.Value.AsInt32;
      }
      catch (InvalidCastException)
      {
        throw new COMException("Incorrect value type");
      }
    }

    /// <summary>
    /// Gets an double value from its BSON Element
    /// </summary>
    /// <param name="doubleStuff">The element</param>
    /// <returns>the double value</returns>
    internal static double BsonToDouble(BsonElement doubleStuff)
    {
      try
      {
        return doubleStuff.Value.AsDouble;
      }
      catch (InvalidCastException)
      {
        throw new COMException("Incorrect value type");
      }
    }

    /// <summary>
    /// Transforms an ArcObjects IPoint into a BSON Value consisting of an array
    /// i.e. {shape=[-28.460325240999964, 26.267370224000047]}
    /// </summary>
    /// <param name="pt">The point to transform</param>
    /// <returns>BSON value representing point</returns>
    internal static BsonValue PointToBson(IPoint pt)
    {
      // stored as Y, X order because of MongoDB spatial indexing
      return BsonArray.Create(new double[2] { pt.X, pt.Y });
    }

    /// <summary>
    /// Populates an ArcObjects IPoint X and Y from a BSON Value consisting of an array
    /// i.e. {shape=[-28.460325240999964, 26.267370224000047]}
    /// </summary>
    /// <param name="elem">The BSON Element holding the x and y</param>
    /// <param name="pGeometry">The Geometry to update</param>
    internal static void BsonToGeometry(BsonElement elem, IGeometry pGeometry)    
    {
      try      
      {
        BsonValue[] shapeBuffer = elem.Value.AsBsonArray.ToArray();
        // stored as Y, X order because of MongoDB spatial indexing
        if (pGeometry is IPoint)        
          (((IPoint)pGeometry)).PutCoords(shapeBuffer[0].AsDouble, shapeBuffer[1].AsDouble);
        else
          throw new COMException("non-point geometries unsupported");
      }
      catch (InvalidCastException e)      
      {
        throw new COMException("Corrupt shape buffer");
      }
    }

    /// <summary>
    /// Gets an object serialized using ArcObjects xml serialization from a BSON Element
    /// Used by the CatalogDataset to extract metadata
    /// </summary>
    /// <param name="byteStuff">The BSON element containing the bytes</param>
    /// <returns>The object deserialized</returns>
    internal static System.Object BsonToObject(BsonElement byteStuff)
    {
      try
      {
        byte[] bytes = byteStuff.Value.AsByteArray;
        IXMLStream ipXmlStream = new XMLStreamClass();
        ipXmlStream.LoadFromBytes(ref bytes);
        IXMLReader ipXmlReader = new XMLReaderClass();
        ipXmlReader.ReadFrom((IStream)ipXmlStream);
        IXMLSerializer ipXmlSer = new XMLSerializerClass();
        return ipXmlSer.ReadObject(ipXmlReader, null, null);
      }
      catch (Exception)
      {
        throw new COMException("Value expected as byte array isn't");
      }
    }

    /// <summary>
    /// Serializes an object using ArcObjects xml serialization into a BSON Element
    /// Used by the CatalogDataset to store metadata
    /// </summary>
    /// <param name="ipItem">The object to serialize</param>
    /// <returns>The BSON element containing the bytes</returns>
    internal static BsonValue ObjectToBson(System.Object ipItem)
    {
      IXMLStream ipXmlStream = new XMLStreamClass();
      IXMLWriter ipXmlWriter = new XMLWriterClass();
      ipXmlWriter.WriteTo((IStream)ipXmlStream);
      IXMLSerializer ipXmlSer = new XMLSerializerClass();
      ipXmlSer.WriteObject(ipXmlWriter, null, null, "Test", "Test", ipItem);
      byte[] bytes = ipXmlStream.SaveToBytes();
      return BsonValue.Create(bytes);
    }
  }
}
