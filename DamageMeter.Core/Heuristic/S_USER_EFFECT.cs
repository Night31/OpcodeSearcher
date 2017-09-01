﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.Game.Messages;

namespace DamageMeter.Heuristic
{
    class S_USER_EFFECT : AbstractPacketHeuristic
    {
        public static S_USER_EFFECT Instance => _instance ?? (_instance = new S_USER_EFFECT());
        private static S_USER_EFFECT _instance;

        public S_USER_EFFECT() : base(OpcodeEnum.S_USER_EFFECT) { }

        public new void Process(ParsedMessage message)
        {
            base.Process(message);
            if (IsKnown || OpcodeFinder.Instance.IsKnown(message.OpCode)) return;
            if(message.Payload.Count != 8+8+4+4) return;

            var target = Reader.ReadUInt64();
            var source = Reader.ReadUInt64();
            var circle = Reader.ReadUInt32();
            var operation = Reader.ReadUInt32();

            if(circle != 2 && circle != 3) return;
            if(operation != 1 && operation != 2) return;
            if(!DbUtils.IsNpcSpawned(source)) return;
            if(!DbUtils.IsUserSpawned(target) && DbUtils.GetPlayercId() != target) return;

            OpcodeFinder.Instance.SetOpcode(message.OpCode, OPCODE);
        }
    }
}