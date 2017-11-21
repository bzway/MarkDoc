﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using DotLiquid;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Bzway.Writer.App
{
    public class Program
    {
        static Dictionary<string, Action<string[]>> dictionary = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);
        public static void Main(string[] args)
        {
            //预览文章
            dictionary.Add("view", View);
            dictionary.Add("v", View);

            //发布文档
            dictionary.Add("public", Generate);
            dictionary.Add("p", Generate);

            //输出文档
            dictionary.Add("generate", Generate);
            dictionary.Add("g", Generate);

            //创建或编辑一篇文章
            dictionary.Add("edit", Edit);

            //启动服务
            dictionary.Add("run", Run);
            dictionary.Add("r", Run);

            var cmd = args.FirstOrDefault();

            if (string.IsNullOrEmpty(cmd) || !dictionary.ContainsKey(cmd))
            {
                Run(args);
                return;
            }
            dictionary[cmd].Invoke(args.Skip(1).ToArray());
        }
        public static void Generate(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            Site site = new Site(root);
            site.Generate();
            Console.WriteLine("generate site OK");
        }
        public static void View(string[] args)
        {
            var server = new Server();
            var filePath = args.FirstOrDefault();
            if (string.IsNullOrEmpty(filePath))
            {
                Process.Start(server.Broswer, server.HostUrl);
                return;
            }
            Process.Start(server.Broswer, server.HostUrl + filePath);
        }

        public static void Edit(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var cmd = args.FirstOrDefault();
            if (string.IsNullOrEmpty(cmd))
            {
                return;
            }

            var docPath = cmd.Trim('/', '\\');
            var site = new Site(root);
            var filePath = site.Create(docPath);
            Server server = new Server();
            Process.Start(server.Editor, filePath);
        }
        public static void Run(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var server = new Server();
            bool canRun = !File.Exists(server.ProcessFile);
            if (!canRun)
            {
                using (var stream = File.OpenText(server.ProcessFile))
                {
                    int pid = 0;
                    if (int.TryParse(stream.ReadLine(), out pid))
                    {
                        var process = Process.GetProcesses().FirstOrDefault(m => m.Id == pid);
                        canRun = (process == null);
                    }
                }
            }
            if (canRun)
            {
                using (var stream = File.CreateText(server.ProcessFile))
                {
                    stream.Write(Process.GetCurrentProcess().Id);
                };
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls(server.HostUrl)
                    .UseContentRoot(root)
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .UseApplicationInsights()
                    .Build();
                host.Run();
                File.Delete(server.ProcessFile);
            }
        }
    }
}