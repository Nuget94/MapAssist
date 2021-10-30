/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

namespace MapAssist.Types
{
    public static class Offsets
    {
        public static int UnitTable = 0x2027660;
        public static int InGameMap = 0x2037322;

        public static int Player = 0x10;           //0x10
        public static int Act = 0x20;              //0x20
        public static int MapSeed = 0x14;          //0x14
        public static int Room1 = 0x20;            //0x20
        public static int Room2 = 0x18;            //0x18
        public static int ActUnk1 = 0x70;          //0x70
        public static int GameDifficulty = 0x830;  //0x830
        public static int Level = 0x90;            //0x90
        public static int LevelId = 0x1F8;         //0x1F8
        public static int Path = 0x38;             //0x38
        public static int PosXAdress = 0x02;        //0x02
        public static int PosYAdress = 0x06;        //0x06
    }
}
