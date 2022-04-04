using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Ionic.Zlib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Zoltu.IO;
using CompressionMode = Ionic.Zlib.CompressionMode;
 
    public class BliveUtility {
        public static byte[] EncodeUserAuthentication(int roomId, string token) {
            var j = new JObject();
            j["roomid"] = roomId;
            j["key"] = token;
            return Encode(j.ToString(), BliveOp.UserAuthentication);
        }
        
        public static byte[] EncodeHeartbeat() { 
            return Encode("", BliveOp.Heartbeat);
        }
        
        public static byte[] Encode(string content, BliveOp op) {
            using var ms = new MemoryStream();
            using var bw = new BigEndianBinaryWriter(ms);
            var bytes = Encoding.UTF8.GetBytes(content);
            bw.Write(16 + bytes.Length);
            bw.Write((short) 16);
            bw.Write((short) 1);
            bw.Write((int) op);
            bw.Write(1);
            bw.Write(bytes);
            return ms.ToArray();
        }

        public static List<(string, BliveOp)> Decode(byte[] data) {
            var ret = new List<(string, BliveOp)>();
            using var ms = new MemoryStream(data);
            using var br = new BigEndianBinaryReader(ms);
            var len = br.ReadInt32();
            var headerLen = br.ReadInt16();
            var sub = br.ReadInt16();
            var op = (BliveOp) br.ReadInt32();
            var unk = br.ReadInt32(); 
            if (op == BliveOp.Message && sub > 0) {
                try {
                    using var process = new ZlibStream(ms, CompressionMode.Decompress);
                    using var ds = new MemoryStream();
                    process.CopyTo(ds);
                    ds.Position = 0;
                    using var br2 = new BigEndianBinaryReader(ds);
                    while (ds.Position != ds.Length) {
                        int len2 = br2.ReadInt32();
                        ds.Position -= 4;
                        ret.AddRange(Decode(br2.ReadBytes(len2))); 
                    }
                }
                catch (ZlibException ex) {
                    Debug.LogException(ex); 
                }  
            }
            else if (op == BliveOp.HeartbeatReply) {
                ret.Add((br.ReadInt32().ToString(), op));
            }
            else {
                string str = Encoding.UTF8.GetString(data[16..]); 
                ret.Add((str, op));
            }

            return ret;
        }
    } 