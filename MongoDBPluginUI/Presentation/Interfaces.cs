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

using System.Collections.Generic;
using System;

namespace MongoDBPluginUI
{
  public interface IMongoDbDialogVM
  {
    String ServerText
    {
      get;
      set;
    }
    String DatabaseText
    {
      get;
      set;
    }

    String File
    {
      get;
      set;
    }
    bool Dirty
    {
      get;
      set;
    }


    void ClearFCList();
    string GetSelectedFCName();
    void SetFCNames(IEnumerable<string> names);
    void SetBrowse(ButtonInfo bi);
    void SetOk(ButtonInfo bi);
    void SetCancel(ButtonInfo bi);
    void Close();
  }

  public interface IMongoDbLoaderDlgVm
      : IMongoDbDialogVM
  {
    void SetSourceBrowse(ButtonInfo bi);
    string FCConn
    {
      get;
      set;
    }
  }
}