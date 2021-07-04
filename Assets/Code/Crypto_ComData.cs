using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.WebSockets;
using UnityEngine;

public class Crypto_ComData : MonoBehaviour
{
    private const string API_KEY = "YOUR_API_KEY";
    private const string API_SECRET = "YOUR_API_SECRET";

    //private static string GetSign(Dictionary Request)
    //{
    //    Dictionary Params = Request.Params;

    //    // Ensure the params are alphabetically sorted by key
    //    string ParamString = string.Join("", Params.Keys.OrderBy(key => key).Select(key => key + Params[key]));

    //    string SigPayload = Request.method + Request.id + API_KEY + ParamString + Request.nonce;

    //    var hash = new HMACSHA256(API_SECRET);
    //    var ComputedHash = hash.ComputeHash(SigPayload);
    //    return ToHex(ComputedHash, false);
    //}
}
