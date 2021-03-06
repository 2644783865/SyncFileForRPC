﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Remoting;

namespace SysncEntity
{
    public class Helper
    {
        /// <summary></summary>
        /// <param name="pklist">链包</param>
        /// <param name="rootPath"></param>
        public static void Write(IList<Packet> pklist, String rootPath)
        {
            var fname = pklist.FirstOrDefault().ToStr();
            var path = Path.Combine(rootPath, fname);

            using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                pklist.RemoveAt(0);

                foreach (var pk in pklist)
                {
                    var ms = pk.GetStream();
                    fs.Write(ms.ReadBytes());
                }
            }
        }

        /// <summary></summary>
        /// <param name="pklist"></param>
        /// <param name="rootPath"></param>
        public static void Write(Packet pklist, String rootPath)
        {
            var fname = pklist.Data.ToStr();
            var path = Path.Combine(rootPath, fname);

            using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                while (true)
                {
                    pklist = pklist.Next;
                    if (pklist == null) break;

                    var ms = pklist.GetStream();
                    fs.Write(ms.ReadBytes());
                }
            }
        }

        /// <summary></summary>
        /// <param name="pk"></param>
        /// <param name="rootPath"></param>
        public static void Write2(Packet pk, String rootPath)
        {
            var s = pk.GetStream();
            var br = new BinaryReader(s);
            var fname = br.ReadString();

            var path = Path.Combine(rootPath, fname);
            // 处理文件夹避免后续无法正常写入文件
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            if (s.Position == s.Length) return;
            //pk = pk.Next;
            //if (pk == null) return;

            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Position = fs.Length;
                fs.Write(s.ReadBytes());
                fs.Flush();
            }
        }

        /// <summary>从文件写入Packet</summary>
        /// <param name="pathFile">文件路径</param>
        /// <param name="name">自定义文件名</param>
        /// <param name="size">默认1024k</param>
        /// <returns></returns>
        public static IList<Byte[]> Read(String pathFile, String name = "", Int32 size = 1024 * 1024)
        {
            if (pathFile.IsNullOrEmpty() || !File.Exists(pathFile)) return new List<Byte[]>();

            using (var fs = new FileStream(pathFile, FileMode.Open, FileAccess.Read))
            {
                var pklist = new List<Byte[]>();
                var fname = name.IsNullOrEmpty() ? (new FileInfo(pathFile)).Name : name;
                //if (fname.LastIndexOf(".") > -1) fname = fname.Substring(0, fname.LastIndexOf("."));

                /*
                 * 写入顺序
                 * 1.文件名
                 * 2.数据
                 */
                pklist.Add(fname.GetBytes());

                while (true)
                {
                    if (fs.Position == fs.Length) break;

                    var buffer = (fs.Length - fs.Position < size) ? new Byte[fs.Length - fs.Position] : new Byte[size];
                    fs.Read(buffer, 0, buffer.Length);

                    var pk = buffer;
                    pklist.Add(pk);
                }

                return pklist;
            }
        }

        /// <summary>从文件写入Packet</summary>
        /// <param name="pathFile">文件路径</param>
        /// <param name="name">自定义文件名</param>
        /// <param name="size">默认1024k</param>
        /// <returns></returns>
        public static Packet ReadPK(String pathFile, String name = "", Int32 size = 1024 * 1024)
        {
            if (pathFile.IsNullOrEmpty() || !File.Exists(pathFile)) return null;

            using (var fs = new FileStream(pathFile, FileMode.Open, FileAccess.Read))
            {
                var fname = name.IsNullOrEmpty() ? (new FileInfo(pathFile)).Name : name;

                /*
                 * 写入顺序
                 * 1.文件名
                 * 2.数据
                 */
                var pk = new Packet(fname.GetBytes());

                while (true)
                {
                    if (fs.Position == fs.Length) break;

                    var buffer = (fs.Length - fs.Position < size) ? new Byte[fs.Length - fs.Position] : new Byte[size];
                    fs.Read(buffer, 0, buffer.Length);
                    pk.Append(buffer);
                }

                return pk;
            }
        }

        /// <summary>从文件写入Packet列表</summary>
        /// <param name="pathFile"></param>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static IList<Packet> ReadPK2(String pathFile, String name = "", Int32 size = 1024 * 1024)
        {
            if (pathFile.IsNullOrEmpty() || !File.Exists(pathFile)) return null;

            using (var fs = new FileStream(pathFile, FileMode.Open, FileAccess.Read))
            {
                var fname = name.IsNullOrEmpty() ? (new FileInfo(pathFile)).Name : name;
                /*
                 * 写入顺序
                 * 1.文件名
                 * 2.数据
                 */
                var list = new List<Packet>();
                while (true)
                {
                    if (fs.Position == fs.Length) break;

                    using (var sm = new MemoryStream())
                    {
                        var br = new BinaryWriter(sm);
                        br.Write(fname);

                        var buffer = (fs.Length - fs.Position < size - sm.Length) ? new Byte[fs.Length - fs.Position] : new Byte[size - sm.Length];


                        fs.Read(buffer, 0, buffer.Length);
                        sm.Write(buffer);
                        sm.Position = 0;

                        var pk = new Packet(sm);

                        list.Add(pk);
                    }
                }

                return list;
            }
        }

        /// <summary>异步读写</summary>
        /// <param name="pathFile"></param>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Boolean ReadAndWrite(ApiClient client, String pathFile, String name = "", Int32 size = 1024 * 1024)
        {
            if (pathFile.IsNullOrEmpty() || !File.Exists(pathFile)) return false;

            using (var fs = new FileStream(pathFile, FileMode.Open, FileAccess.Read))
            {
                var fname = name.IsNullOrEmpty() ? (new FileInfo(pathFile)).Name : name;
                /*
                 * 写入顺序
                 * 1.文件名
                 * 2.数据
                 */
                //var list = new List<Packet>();
                var flag = true;
                while (flag)
                {
                    if (fs.Position == fs.Length) break;

                    using (var sm = new MemoryStream())
                    {
                        var br = new BinaryWriter(sm);
                        br.Write(fname);

                        var buffer = (fs.Length - fs.Position < size - sm.Length) ? new Byte[fs.Length - fs.Position] : new Byte[size - sm.Length];

                        fs.Read(buffer, 0, buffer.Length);
                        sm.Write(buffer);
                        sm.Position = 0;

                        var pk = new Packet(sm);

                        flag = client.Invoke<Boolean>("Sync/SendFilePK2", pk);

                        //if (!flag) break;
                        //list.Add(pk);
                    }
                }

                return flag;
            }
        }
    }
}
