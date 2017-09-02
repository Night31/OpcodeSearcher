﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.Game.Messages;

namespace DamageMeter.Heuristic
{
    class S_PLAYER_CHANGE_FLIGHT_ENERGY : AbstractPacketHeuristic
    {
        public static S_PLAYER_CHANGE_FLIGHT_ENERGY Instance => _instance ?? (_instance = new S_PLAYER_CHANGE_FLIGHT_ENERGY());
        private static S_PLAYER_CHANGE_FLIGHT_ENERGY _instance;

        public S_PLAYER_CHANGE_FLIGHT_ENERGY() : base(OpcodeEnum.S_PLAYER_CHANGE_FLIGHT_ENERGY) { }
        public float LastEnergy;
        public new void Process(ParsedMessage message)
        {
            base.Process(message);
            if (IsKnown || OpcodeFinder.Instance.IsKnown(message.OpCode))
            {
                if (OpcodeFinder.Instance.GetOpcode(OPCODE) == message.OpCode) { Parse(); }
                return;
            }
            if (message.Payload.Count != 4) return;
            var energy = Reader.ReadSingle();
            if (energy > 1000) return;
            if(!OpcodeFinder.Instance.IsKnown(OpcodeEnum.C_PLAYER_FLYING_LOCATION)) return;
            OpcodeFinder.Instance.SetOpcode(message.OpCode, OPCODE);
            LastEnergy = energy;
        }

        private void Parse()
        {
            LastEnergy = Reader.ReadSingle();
        }
    }
}
