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
using MongoDB.Driver;
using System.IO;
using ESRI.ArcGIS.esriSystem;

namespace MongoDBPlugIn.Utilities
{
  /// <summary>
  /// Helper class holding information necessary to connect to a MongoDB database
  /// </summary>
  public struct MongoDBConnInfo
  {
    public String Connection;
    public String DBName;
    public String ConnFileName;
    public MongoDBConnInfo(String cn, string dbName, String connFileName)
    {
      Connection = cn;
      DBName = dbName;
      ConnFileName = connFileName;
    }
  }

  /// <summary>
  /// Utilities for connecting to MongoDB
  /// </summary>
  static public class ConnectionUtilities
  {

    /// <summary>
    /// Given the file path for a connection file get the enclosed MongoDB connection string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string DecodeConnFile(string input)
    {
      if (!File.Exists(input))
        throw new COMException("File doesn't exist");
      byte[] props = File.ReadAllBytes(input);
      return Encoding.UTF8.GetString(props);
    }

    /// <summary>
    /// Given a connection string as stored in a conn file
    /// get a MongoDBConnInfo helper class
    /// </summary>
    /// <param name="input">the connection string</param>
    /// <returns>MongoDBConnInfo with all properties set</returns>
    public static MongoDBConnInfo ParseConnectionString(string input)
    {
      // format "mongodb:[IPADDRESS]?safe=true,[DATABASE NAME]"
      // Example: mongodb://127.0.0.1/?safe=true,test

      // linux ending causes issue
      input = input.Replace("/r/n", "/n");

      if (String.IsNullOrEmpty(input))
        throw new COMException("Bad connection string");

      int commaIdx = input.LastIndexOf(',');
      if ((-1 == commaIdx) || (commaIdx == input.Length))
        throw new COMException("Bad connection string");

      MongoDBConnInfo info;
      Uri testIsValid;
      try
      {
        testIsValid = new Uri(input.Substring(0, commaIdx) + ";connectTimeoutMS=240000;socketTimeoutMS=240000");
      }
      catch (Exception)
      {
        throw new COMException("Bad connection string");
      }

      if (!testIsValid.IsWellFormedOriginalString())
        throw new COMException("Bad connection string");

      info.Connection = input.Substring(0, commaIdx);
      info.DBName = input.Substring(commaIdx + 1);
      info.ConnFileName = input;

      return info;
    }

    /// <summary>
    /// Open a connection from a MongoDBConnInfo
    /// </summary>
    /// <param name="connInfo">holds all connectino information</param>
    /// <returns>reference to the database</returns>
    public static MongoDatabase OpenConnection(MongoDBConnInfo connInfo)    
    {
      MongoDatabase db;
      try      
      {
        // we add time-out info - this can also be overriden on individual requests to the db
        Uri conn = new Uri(connInfo.Connection + ";connectTimeoutMS=240000;socketTimeoutMS=2400000");
        MongoClient client = new MongoClient(conn.ToString());
        MongoServer server = client.GetServer();
                
        db = server.GetDatabase(connInfo.DBName);
      }
      catch (Exception e)      
      {
        throw new COMException(e.Message);
      }

      return db;
    }
  }
}