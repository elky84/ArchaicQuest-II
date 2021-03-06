﻿using System;
using System.Collections.Generic;
using System.Text;
using ArchaicQuestII.GameLogic.Character;

namespace ArchaicQuestII.GameLogic.World.Room
{
   public interface IRoomActions
    {
        void Look(string target, Room room, Player player);
        void LookInContainer(string target, Room room, Player player);
        void ExamineObject(string target, Room room, Player player);
        void SmellObject(string target, Room room, Player player);
        void TasteObject(string target, Room room, Player player);
        void TouchObject(string target, Room room, Player player);
        string FindValidExits(Room room);
        string DisplayItems(Room room);
        string DisplayMobs(Room room);
    }
}
