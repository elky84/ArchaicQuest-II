﻿using System;
using ArchaicQuestII.GameLogic.Commands;
using System.Linq;
using System.Threading.Tasks;
using ArchaicQuestII.DataAccess;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Combat;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Core
{

    public class GameLoop : IGameLoop
    {


        private IWriteToClient _writeToClient;
        private ICache _cache;
        private ICommands _commands;
        private ICombat _combat;
        private IDataBase _db;
        private IDice _dice;
        private IUpdateClientUI _client;

        public GameLoop(IWriteToClient writeToClient, ICache cache, ICommands commands, ICombat combat, IDataBase database, IDice dice, IUpdateClientUI client)
        {
            _writeToClient = writeToClient;
            _cache = cache;
            _commands = commands;
            _combat = combat;
            _db = database;
            _dice = dice;
            _client = client;
        }


        public async Task UpdateTime()
        {
             
  
            Console.WriteLine("started loop");
            while (true)
            {
                //2 mins
                await Task.Delay(120000);
                var rooms = _cache.GetAllRoomsToRepop();
                var players = _cache.GetPlayerCache().Values.ToList();

                foreach (var room in rooms)
                {
                    var originalRoom = _db.GetById<Room>(room.Id, DataBase.Collections.Room);

                    foreach (var mob in originalRoom.Mobs)
                    {
                        // need to check if mob exists before adding
                        var mobExist = room.Mobs.FirstOrDefault(x => x.Id.Equals(mob.Id));

                        if (mobExist == null)
                        {
                            room.Mobs.Add(mob);
                        }
                        else
                        {
                            mobExist.Attributes.Attribute[EffectLocation.Hitpoints] += _dice.Roll(1, 2, 5) * mobExist.Level;
                            mobExist.Attributes.Attribute[EffectLocation.Mana] += _dice.Roll(1, 2, 5) * mobExist.Level;
                            mobExist.Attributes.Attribute[EffectLocation.Moves] += _dice.Roll(1, 2, 5) * mobExist.Level;
                        }
                    }

                    foreach (var item in originalRoom.Items)
                    {
                        // need to check if item exists before adding
                        var itemExist = room.Items.FirstOrDefault(x => x.Id.Equals(item.Id));

                        if (itemExist == null)
                        {
                            room.Items.Add(item);
                        }
                    }

                    // reset doors
                    room.Exits = originalRoom.Exits;

                    //set room clean
                    room.Clean = true;

                    foreach (var player in room.Players)
                    {
                        _writeToClient.WriteLine("<p>The hairs on your neck stand up.</p>", player.ConnectionId);
                    }

                }

                foreach (var player in players)
                {
                  
                    player.Attributes.Attribute[EffectLocation.Hitpoints] += _dice.Roll(1, 2, 5) * player.Level;
                    player.Attributes.Attribute[EffectLocation.Mana] += _dice.Roll(1, 2, 5) * player.Level;
                    player.Attributes.Attribute[EffectLocation.Moves] += _dice.Roll(1, 2, 5) * player.Level;

                    if (player.Attributes.Attribute[EffectLocation.Hitpoints] > player.MaxAttributes.Attribute[EffectLocation.Hitpoints])
                    {
                        player.Attributes.Attribute[EffectLocation.Hitpoints] = player.MaxAttributes.Attribute[EffectLocation.Hitpoints];
                    }

                    if (player.Attributes.Attribute[EffectLocation.Mana] > player.MaxAttributes.Attribute[EffectLocation.Mana])
                    {
                        player.Attributes.Attribute[EffectLocation.Mana] = player.MaxAttributes.Attribute[EffectLocation.Mana];
                    }

                    if (player.Attributes.Attribute[EffectLocation.Moves] > player.MaxAttributes.Attribute[EffectLocation.Moves])
                    {
                        player.Attributes.Attribute[EffectLocation.Moves] = player.MaxAttributes.Attribute[EffectLocation.Moves];
                    }

                    _client.UpdateHP(player);
                    _client.UpdateMana(player);
                    _client.UpdateMoves(player);
                    _client.UpdateScore(player);

                }
               
            }
        }

        public async Task UpdateCombat()
        {
 // create a combat cache to add mobs too so they can fight back
 // block movement while fighting
 // end fight if target is not there / dead
 // create flee commant
            Console.WriteLine("started combat loop");
            while (true)
            {
                await Task.Delay(4000);
                Console.WriteLine("combat loop");
                var players = _cache.GetCombatList();
                var validPlayers = players.Where(x => x.Status == CharacterStatus.Status.Fighting);

                foreach (var player in validPlayers)
                {
                    _combat.Fight(player, player.Target, _cache.GetRoom(player.RoomId), false);
                }
            }
        }

        public async Task UpdateRoomEmote()
        {

            while (true)
            {
                try
                {
                    await Task.Delay(45000).ConfigureAwait(false);

                    var rooms = _cache.GetAllRooms();

                    if (!rooms.Any())
                    {
                        continue;
                    }

                    foreach (var room in rooms)
                    {
                        if (!room.Emotes.Any())
                        {
                            continue;
                        }

                        var emote = room.Emotes[_dice.Roll(1, 0, room.Emotes.Count - 1)];

                        foreach (var player in room.Players)
                        {
                            _writeToClient.WriteLine($"<p class='room-emote'>{emote}</p>",
                                player.ConnectionId);
                        }
                    }
                }
                catch (Exception ex)
                {

                }

            }
            
        }

        public async Task UpdateMobEmote()
        {


            while (true)
            {
                try
                {
                    await Task.Delay(3000).ConfigureAwait(false);

                    var rooms = _cache.GetAllRooms().Where(x => x.Mobs.Any());

                    if (!rooms.Any())
                    {
                        continue;
                    }

                    foreach (var room in rooms)
                    {
                       
                        foreach (var mob in room.Mobs)
                        {
                            if (mob.Emotes.Any())
                            {

                                var emote = mob.Emotes[_dice.Roll(1, 0, room.Emotes.Count - 1)];
                                foreach (var player in room.Players)
                                {
                                    //example mob emote: Larissa flicks through her journal.
                                    if (emote == null)
                                    {
                                        continue;
                                    }
                                    _writeToClient.WriteLine($"<p class='mob-emote'>{mob.Name} {emote}</p>",
                                        player.ConnectionId);
                                }
                            }

 
                            if (!string.IsNullOrEmpty(mob.Commands) && mob.Buffer.Count == 0)
                            {
                                mob.RoomId = room.Id;
                                var commands = mob.Commands.Split(";");

                                foreach (var command in commands)
                                {
                                    mob.Buffer.Push(command);
                                }

                            }

                            if (mob.Buffer.Count > 0)
                            {
                                var mobCommand = mob.Buffer.Pop();
                                _commands.ProcessCommand(mobCommand, mob, room);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }

            }

        }

        public async Task UpdatePlayers()
        {
            while (true)
            {

                try
                {
                    await Task.Delay(125);
                    var players = _cache.GetPlayerCache();
                    var validPlayers = players.Where(x => x.Value.Buffer.Count > 0);

                    foreach (var player in validPlayers)
                    {

                        var command = player.Value.Buffer.Pop();
                        var room = _cache.GetRoom(player.Value.RoomId);

                        _commands.ProcessCommand(command, player.Value, room);

                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }


}