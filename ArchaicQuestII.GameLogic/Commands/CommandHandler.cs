﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Socials;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands
{
    /// <summary>
    /// This is to handle commands that can't fit in the switch case
    /// found in command.cs as a catch all this class will figure out what to 
    /// do next.
    /// This is to solve the issue of dynamic skills and socials
    /// It wasn't a problem for spells as they're preceded by c or cast 
    /// so was trivial to handle with the other commands.
    /// but when dealing with skills or socials with no prefix and are not hard coded
    /// it's a little more trickier 
    /// </summary>
    public class CommandHandler: ICommandHandler
    {
        private readonly ISocials _socials;
        private readonly ICache _cache;
        private readonly IWriteToClient _writeToClient;
        public CommandHandler(ISocials social, ICache cache, IWriteToClient writeToClient)
        {
            _socials = social;
            _cache = cache;
            _writeToClient = writeToClient;
        }
        public void HandleCommand(string key, string obj, string target, Player player, Room room)
        {
            var foundCommand = false;
            //check player skills
            if(false){}

            //check socials
            var social = _cache.GetSocials().Keys.FirstOrDefault(x => x.StartsWith(key));
            if (social != null)
            {
                foundCommand = true;
                var emoteTarget = key == obj ? "" : obj;
                _socials.EmoteSocial(player, room, _cache.GetSocials()[social], emoteTarget);
                return;
            }

            _writeToClient.WriteLine("That is not a command.");
        }
    }
}
