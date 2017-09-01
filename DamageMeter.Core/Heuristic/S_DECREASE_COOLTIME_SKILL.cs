﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.Game.Messages;

namespace DamageMeter.Heuristic
{
    class S_DECREASE_COOLTIME_SKILL : AbstractPacketHeuristic
    {
        public static S_DECREASE_COOLTIME_SKILL Instance => _instance ?? (_instance = new S_DECREASE_COOLTIME_SKILL());
        private static S_DECREASE_COOLTIME_SKILL _instance;

        public S_DECREASE_COOLTIME_SKILL() : base(OpcodeEnum.S_DECREASE_COOLTIME_SKILL) { }

        public new void Process(ParsedMessage message)
        {
            base.Process(message);
            if (IsKnown || OpcodeFinder.Instance.IsKnown(message.OpCode)) return;
            if(message.Payload.Count != 8) return;
            if (!OpcodeFinder.Instance.IsKnown(OpcodeEnum.C_START_SKILL)) return;
            if (!OpcodeFinder.Instance.IsKnown(OpcodeEnum.S_START_COOLTIME_SKILL)) return;
            if (OpcodeFinder.Instance.KnowledgeDatabase.TryGetValue(OpcodeFinder.KnowledgeDatabaseItem.LoggedCharacter, out var res))
            {
                var ch = (LoggedCharacter) res.Item2;
                if (ch.Model != 11012) return; //return if not a ninja
                if (C_START_SKILL.Instance.LatestSkill > 11270 || C_START_SKILL.Instance.LatestSkill < 10100) return; //return if it's not ninja's combo attack
            }
            var skill = Reader.ReadUInt32(); //could add a check on all other ninja skills affected by this mechanic
            var cd = Reader.ReadUInt32(); //could add a max cd threshold


            OpcodeFinder.Instance.SetOpcode(message.OpCode, OPCODE);
        }
    }
}