﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DamageMeter.Sniffing;
using Tera.Game;
using Tera.Game.Abnormality;
using Tera.Game.Messages;
using Message = Tera.Message;
using OpcodeId = System.UInt16;
using Tera.PacketLog;
using System.Globalization;
using System.Diagnostics;

namespace DamageMeter
{
    public class NetworkController
    {
        public delegate void ConnectedHandler(string serverName);

        public delegate void GuildIconEvent(Bitmap icon);
        public delegate void UpdateUiHandler(Tuple<List<ParsedMessage>, Dictionary<OpcodeId, OpcodeEnum>, int> message);
        public event UpdateUiHandler TickUpdated;
        public event Action ResetUi;
        private static NetworkController _instance;

        private bool _keepAlive = true;
        internal MessageFactory MessageFactory = new MessageFactory();
        internal bool NeedInit = true;
        public Server Server;
        internal UserLogoTracker UserLogoTracker = new UserLogoTracker();

        private NetworkController()
        {
            TeraSniffer.Instance.NewConnection += HandleNewConnection;
            TeraSniffer.Instance.EndConnection += HandleEndConnection;
            var packetAnalysis = new Thread(PacketAnalysisLoop);
            packetAnalysis.Start();
        }

        public PlayerTracker PlayerTracker { get; internal set; }

        public NpcEntity Encounter { get; private set; }
        public NpcEntity NewEncounter { get; set; }

        public bool TimedEncounter { get; set; }

        public string LoadFileName { get; set; }
        public bool NeedToSave { get; set; }
        public string LoadOpcodeCheck { get; set; }

        public static NetworkController Instance => _instance ?? (_instance = new NetworkController());

        public EntityTracker EntityTracker { get; internal set; }
        public bool SendFullDetails { get; set; }

        public event GuildIconEvent GuildIconAction;

        public void Exit()
        {
            _keepAlive = false;
            TeraSniffer.Instance.Enabled = false;
            Thread.Sleep(500);
            Application.Exit();
        }

        internal void RaiseConnected(string message)
        {
            Connected?.Invoke(message);
        }
            
        public event ConnectedHandler Connected;

        protected virtual void HandleEndConnection()
        {
            NeedInit = true;
            MessageFactory = new MessageFactory();
            Connected?.Invoke("no server");
            OnGuildIconAction(null);
        }

        protected virtual void HandleNewConnection(Server server)
        {
            Server = server;
            NeedInit = true;
            MessageFactory = new MessageFactory();
            Connected?.Invoke(server.Name);
        }
        public Dictionary<OpcodeId, OpcodeEnum> UiUpdateKnownOpcode;
        public List<ParsedMessage> UiUpdateData;
        private void UpdateUi()
        {
            var currentLastPacket = OpcodeFinder.Instance.PacketCount;
            TickUpdated?.Invoke(new Tuple<List<ParsedMessage>, Dictionary<OpcodeId, OpcodeEnum>, int> (UiUpdateData, UiUpdateKnownOpcode, TeraSniffer.Instance.Packets.Count));
            UiUpdateData = new List<ParsedMessage>();
            UiUpdateKnownOpcode = new Dictionary<OpcodeId, OpcodeEnum>();
        }

        public uint Version { get; private set; }

        private void SaveLog()
        {
            if (!NeedToSave) { return; }
            NeedToSave = false;
            if(AnalysisType  == AnalysisTypeEnum.Unknown) { return; }
            if (AnalysisType == AnalysisTypeEnum.LogFile)
            {
                MessageBox.Show("Saving saved log is retarded");
                return;
            }
            var header = new LogHeader { Region =  Version.ToString()};
            PacketLogWriter writer = new PacketLogWriter(string.Format("{0}.TeraLog", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_"+Version, CultureInfo.InvariantCulture)), header);
            foreach(var message in OpcodeFinder.Instance.AllPackets)
            {
                writer.Append(message.Value);
            }
            writer.Dispose();
            MessageBox.Show("Saved");
        }
        public enum AnalysisTypeEnum
        {
            Unknown = 0,
            Network = 1,
            LogFile = 2
        }

        public bool StrictCheck = false;
        private AnalysisTypeEnum AnalysisType = 0;
        private void PacketAnalysisLoop()
        {
     
            while (_keepAlive)
            {
                LoadFile();
                SaveLog();
                if (LoadOpcodeCheck != null) {
                    if (OpcodeFinder.Instance.OpcodePartialMatch())
                    {
                        MessageBox.Show("Partial match: SUCCESS");
                    }
                    else
                    {
                        MessageBox.Show("Partial match: FAIL");
                    }
                }

                // Update the UI at every packet if the backend it not overload & if we are recording the network
                if (AnalysisType == AnalysisTypeEnum.Network && TeraSniffer.Instance.Packets.Count < 2000)
                {
                    UpdateUi();
                }
                // If loading log file, wait until completion before display
                if (AnalysisType == AnalysisTypeEnum.LogFile && TeraSniffer.Instance.Packets.Count == 0)
                {
                    UpdateUi();
                }
                var successDequeue = TeraSniffer.Instance.Packets.TryDequeue(out var obj);
                if (!successDequeue)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                // Network
                if (AnalysisType == AnalysisTypeEnum.Unknown) { AnalysisType = AnalysisTypeEnum.Network; }

                if(AnalysisType == AnalysisTypeEnum.LogFile && TeraSniffer.Instance.Connected)
                {
                    throw new Exception("Not allowed to record network while reading log file");
                }

                var message = MessageFactory.Create(obj);
                message.PrintRaw();

                if(message is C_CHECK_VERSION)
                {
                    Version = (message as C_CHECK_VERSION).Versions[0];
                    // TODO reset backend & UI

                }
                OpcodeFinder.Instance.Find(message);
            }
        }

     

        internal virtual void OnGuildIconAction(Bitmap icon)
        {
            GuildIconAction?.Invoke(icon);
        }

        void LoadFile()
        {
            if (LoadFileName == null) { return; }
            if(AnalysisType == AnalysisTypeEnum.Network) { throw new Exception("Not allowed to load a log file while recording in the network"); }
            AnalysisType = AnalysisTypeEnum.LogFile;
            OpcodeFinder.Instance.Reset();
            ResetUi?.Invoke();
            LogReader.LoadLogFromFile(LoadFileName).ForEach(x => TeraSniffer.Instance.Packets.Enqueue(x));
            LoadFileName = null;

        }
    }
}