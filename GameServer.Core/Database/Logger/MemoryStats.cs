﻿namespace GameServer.Core.Database.Logger
{
    public class MemoryStats
    {
        public ulong UsedMemory { get; set; }
        public ulong AvailableMemory { get; set; }
        public double MemoryUsage => UsedMemory / AvailableMemory * 100.0;
    }
}