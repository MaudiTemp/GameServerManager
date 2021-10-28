﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using System.Collections.Generic;
using GameServer.Core;
using GameServer.Core.Command;
using GameServer.Core.Database;
using GameServer.Core.Settings;
using GameServer.Worker;
using GameServer.Data;
using GameServer.Core.Daemon;
using GameServer.Core.Logger;
using GameServer.Core.Daemon.Config;

namespace GameServer.Main
{
    public class ServerWorker : IDisposable
    {
        private Dictionary<string, Action<string[]>> CommandMap;
        public bool Running { get; private set; } = true;
        public CommandQueue CommandQueue { get; }
        private IDataProvider _dataProvider;
        private IDaemonWorker _daemonWorker;
        private IPerformanceLogger _performanceLogger;

        public ServerWorker(CommandQueue commandQueue, GameServerSettings settings)
        {
            CommandQueue = commandQueue;
            CommandMap = new Dictionary<string, Action<string[]>>{
                {"echo", Echo },
                {"start", Start },
                {"stop", Stop},
                {"server", Server },
                {"allserver", AllServer },
                {"import", ImportServer },
                {"exit", Exit }
            };

            _dataProvider = new MongoDBProvider(settings.ProviderSettings);
            _daemonWorker = new DockerWorker(settings.DaemonSettings, _dataProvider);
            //_performanceLogger = new PerformanceLogger(settings.LoggingSettings, _dataProvider);
        }

        private async void ImportServer(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }
            var config = ContainerConfig.FromFile(args[0]);
            await _daemonWorker.ImportServer(config);
        }

        private async void AllServer(string[] args)
        {
            if (args.Length != 0)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var servers = await _daemonWorker.GetAllServer();
            Console.WriteLine($"Names: ID");
            foreach (var server in servers)
            {
                var status = await server.GetStatus();
                Console.WriteLine($"{String.Join('|', server.Names)}: {server.ID}");
                Console.WriteLine($"   ->State: {status.State}");
                Console.WriteLine($"   ->Status: {status.Status}");
            }
        }

        private async void Server(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var server = await _daemonWorker.GetServer(args[0]);

            var status = await server.GetStatus();
            Console.WriteLine($"{String.Join('|', server.Names)}: {server.ID}");
            Console.WriteLine($"   ->State: {status.State}");
            Console.WriteLine($"   ->Status: {status.Status}");
        }

        private async void Stop(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            await _daemonWorker.StopServer(args[0]);
        }

        private async void Start(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }
             
            await _daemonWorker.StartServer(args[0]);
        }

        private async void DisplayHelp(string message)
        {
            Console.WriteLine(message);
        }

        public void Start()
        {
            CommandQueue.NewCommand += OnNewCommand;
            _dataProvider.Connect();
        }

        private void OnNewCommand(object sender, SampleEventArgs e)
        {
            var c = new Command(e.Command);
            var contains = false;
            if (String.IsNullOrEmpty(c.Name))
            {
                DisplayHelp("Command was empty");
                return ;
            }
                
            contains = CommandMap.TryGetValue(c.Name.ToLower(), out var action);
            if (!contains)
            {
                DisplayHelp("Command not Found");
                return ;
            }
            action.Invoke(c.Args.ToArray());
        }

        private async void Exit(string[] args)
        {
            this.Running = false;
        }
        private void Echo(string[] args)
        {
            Console.WriteLine(string.Join(" ", args));
        }

        public void Dispose()
        {
            while(CommandQueue?.IsEmpty == false){}
            Exit(null);
        }
    }
}