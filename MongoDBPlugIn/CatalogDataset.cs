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
using MongoDB.Bson;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDBPlugIn.Utilities;
using ESRI.ArcGIS.esriSystem;

namespace MongoDBPlugIn
{

  /// <summary>
  /// This class represents the metadata for a single dataset stored in MongoDB
  /// </summary>
  [ComVisible(false)]
  public class CatalogDatasetEntry
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="owner">Requires a hook back to the parent for saving</param>
    internal CatalogDatasetEntry(CatalogDataset owner)
    {
      m_Owner = owner;
    }

    /// <summary>
    /// Fields for the dataset
    /// </summary>
    internal IFields Fields
    {
      get;
      set;
    }

    /// <summary>
    /// Dataset's name
    /// </summary>
    internal string Name
    {
      get;
      set;
    }

    /// <summary>
    /// The geographic extent of the dataset - required for drawing
    /// </summary>
    internal IEnvelope Extent
    {
      get;
      set;
    }

    /// <summary>
    /// A collection of guarenteed non-clashing OIDs
    /// </summary>
    private Queue<int> availableOIDs = new Queue<int>();

    /// <summary>
    /// These OIDs are used during data loading to insure uniqueness
    /// </summary>
    /// <returns>the next OID that should be used</returns>
    internal int GetNextOID()
    {
      if (availableOIDs.Count == 0)
        availableOIDs = m_Owner.GetOIDSet(Name);
      return availableOIDs.Dequeue();
    }

    /// <summary>
    /// Serializes the dataset's metadata and stores it in MongoDB
    /// </summary>
    internal void Save()
    {
      List<BsonElement> elements = new List<BsonElement>();

      elements.Add(new BsonElement("Name", BsonValue.Create(Name)));

      if (null != Fields)
        elements.Add(new BsonElement("Fields", esriBsonUtilities.ObjectToBson(Fields)));
      else
        elements.Add(new BsonElement("Fields", ""));

      if (null != Extent)
        elements.Add(new BsonElement("Extent", esriBsonUtilities.ObjectToBson(Extent)));

      m_Owner.SetEntry(Name, elements);
    }

    /// <summary>
    /// Deserializes a metadata document
    /// </summary>
    internal void Load(BsonDocument doc)
    {
      this.Name = esriBsonUtilities.BsonToString(doc.GetElement("Name"));

      // load to fields
      BsonElement bElem = doc.FirstOrDefault(x => x.Name == "Fields");
      if (default(BsonElement) != bElem)
        Fields = (IFields)esriBsonUtilities.BsonToObject(bElem);
      else
        Fields = null;

      // load extent

      bElem = doc.FirstOrDefault(x => x.Name == "Extent");
      if (default(BsonElement) != bElem)
      {
        if (bElem.Value.IsBsonBinaryData)
          Extent = (IEnvelope)esriBsonUtilities.BsonToObject(bElem);
      }
      else
        Extent = null;
    }

    /// <summary>
    /// reference to the CatalogDataset used to access this Metadata
    /// </summary>
    private CatalogDataset m_Owner;
  }

  /// <summary>
  /// Provides access to metadata for datasets stored in MongoDB
  /// </summary>
  [ComVisible(false)]
  internal class CatalogDataset
  {

    /// <summary>
    /// Arbitrary object used for locking when creating collection
    /// </summary>
    static System.Object LOCK = new object();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="conn">The database this CatalogDataset supports</param>
    internal CatalogDataset(MongoDatabase conn)
    {
      m_Conn = conn;
      lock (this)
      {
        if (!m_Conn.CollectionExists(GDB_ITEMS))
        {

          m_Conn.CreateCollection(GDB_ITEMS);
          m_Conn[GDB_ITEMS].EnsureIndex(new string[] { "Name" });
        }
      }
    }

    /// <summary>
    /// Returns the metadata for a dataset
    /// </summary>
    /// <param name="Name">The name of the dataset</param>
    /// <returns>The metadata requested</returns>
    internal CatalogDatasetEntry GetEntry(string Name)    {
      IMongoQuery qry = Query.EQ("Name", BsonValue.Create(Name));
      BsonDocument result = m_Conn[GDB_ITEMS].FindOne(qry);
      if (default(ResultPortionInfo) == result)      {
        throw new COMException("No metadata for item: " + Name);
      }
      CatalogDatasetEntry retval = new CatalogDatasetEntry(this);
      retval.Load(result);
      return retval;
    }

    /// <summary>
    /// Internal method for Adding/Replacing metadata for a dataset
    /// Do not use - Call CatalogDatasetEntry.Save() instead 
    /// </summary>
    /// <param name="Name">The name of the dataset</param>
    /// <param name="vals"></param>
    internal void SetEntry(string Name, List<BsonElement> vals)
    {
      IMongoQuery query = Query.EQ("Name", BsonValue.Create(Name));
      var test = m_Conn[GDB_ITEMS].FindOne(query);
      if (default(BsonDocument) != test)
      {
        test = null;
        m_Conn[GDB_ITEMS].Update(query, new UpdateDocument(vals.ToArray()));
      }
      else
      {
        vals.Add(new BsonElement("NEXT_OID", BsonValue.Create(0)));
        m_Conn[GDB_ITEMS].Insert(new BsonDocument(vals.ToArray()));
      }
    }

    /// <summary>
    /// Get's Metadata for all datasets stored in the database
    /// </summary>
    /// <returns>Enumerable of CatalogDatasetEntries</returns>
    internal IEnumerable<CatalogDatasetEntry> GetAllEntries()
    {
      List<CatalogDatasetEntry> retVal = new List<CatalogDatasetEntry>();
      var results = m_Conn[GDB_ITEMS].Find(Query.Null);

      List<ObjectId> deleteThese = new List<ObjectId>();
      foreach (var result in results)
      {
        try
        {
          CatalogDatasetEntry tmp = new CatalogDatasetEntry(this);
          tmp.Load(result);
          if (!(String.IsNullOrEmpty(tmp.Name) || (tmp.Fields == null) || (tmp.Extent == null)))
            retVal.Add(tmp);
          else
            deleteThese.Add(result.GetElement(CommonConst.OID).Value.AsObjectId);
        }
        catch (Exception)
        { }
      }
      if (deleteThese.Count > 0)
      {
        BsonValue[] ids = new BsonValue[deleteThese.Count];
        int i = 0;
        foreach (var x in deleteThese)
          ids[i++] = BsonValue.Create(x);
        m_Conn[GDB_ITEMS].Remove(Query.In(CommonConst.OID, ids));
      }
      return retVal;
    }

    /// <summary>
    /// Internal utility for obtaining blocks of unique OIDs for insertion
    /// Call CatalogDatasetEntry.GetNextOID() to get OIDs if writing a dataloader
    /// </summary>
    /// <param name="Name">Name of the Dataset</param>
    /// <param name="count">the size of the block requested</param>
    /// <returns>A FIFO block of OIDs</returns>
    internal Queue<int> GetOIDSet(string Name, int count = OIDCHECKOUT)
    {
      IMongoQuery query = Query.EQ("Name", BsonValue.Create(Name));

      int currCount = 0;
      int oidCheckOutTry = 0;

      // we are going to try and get and update the shared OID resource
      // this is a necessary choke point, but the implementation can be improved
      // particularly for multi-client high volume inserts
      // we repeat try a few times and then fail for good - can't get a lock on
      // the oid set
      for (; oidCheckOutTry < OIDCHECKOUTTRYLIMIT; oidCheckOutTry++)
      {
        try
        {
          BsonDocument doc = m_Conn[GDB_ITEMS].FindOne(query);
          if (default(BsonDocument) == doc)
            throw new COMException("missing metadata doc " + Name);

          try
          {
            var next_oid = doc.GetElement("NEXT_OID");
            currCount = esriBsonUtilities.BsonToInt(next_oid);
          }
          catch (Exception)
          {
            currCount = 0;
          }
          
          // include prev corr oid as condition 
          IMongoQuery query2 = Query.EQ("NEXT_OID", BsonValue.Create(currCount));
          int newCount = currCount + count;
          query = Query.And(new IMongoQuery[2] { query, query2 });

          // atomic find and update - fails if the NEXT_OID has been updated by another client
          FindAndModifyResult res = m_Conn[GDB_ITEMS].FindAndModify(query, null, Update.Set("NEXT_OID", BsonValue.Create(newCount)), true, false);
          
          // if it's been updated out from under us
          if (default(FindAndModifyResult) == res)
          {
            System.Threading.Thread.Sleep(SLEEPTIME_OIDCHECKOUT);
            continue;
          }
          break;
        }
        catch (Exception) // if it's been updated out from under us
        {
          System.Threading.Thread.Sleep(SLEEPTIME_OIDCHECKOUT);
          continue;
        }
      }

      // we failed to get a block of new oids
      if (oidCheckOutTry == OIDCHECKOUTTRYLIMIT)
        throw new COMException("Could not secure OIDs");

      Queue<int> retVal = new Queue<int>(count);
      for (int i = currCount; i < (currCount + count); i++)
        retVal.Enqueue(i);

      return retVal;
    }

    private MongoDatabase m_Conn;
    private const string GDB_ITEMS = "GDB_ITEMS";
    private const string GDB_PREFIX = "GDB_CD_";
    private const int OIDCHECKOUT = 200;
    private const int OIDCHECKOUTTRYLIMIT = 10;
    private const int SLEEPTIME_OIDCHECKOUT = 2000;
  }
}
