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

using MapAssist.Types;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Numerics;

namespace MapAssist.Helpers
{
    class GameMemory
    {
        private static string ProcessName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });
        private static IntPtr AdrPlayerUnit = IntPtr.Zero;
        private static IntPtr PtrPlayerUnit = IntPtr.Zero;
        private static IntPtr ProcessHandle = IntPtr.Zero;

        public static GameData GetGameData()
        {
            // Clean up and organize, add better exception handling.
            try
            {
                Process[] process = Process.GetProcessesByName(ProcessName);
                Process gameProcess = process.Length > 0 ? process[0] : null;

                if (gameProcess == null)
                {
                    throw new Exception("Game process not found.");
                }

                ProcessHandle =
                    WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, gameProcess.Id);
                IntPtr processAddress = gameProcess.MainModule.BaseAddress;

                if (PtrPlayerUnit == IntPtr.Zero)
                {
                    var pUnitTable = IntPtr.Add(processAddress, Offsets.UnitTable);
                    for (var i = 0; i < 128; i++)
                    {
                        var pUnit = IntPtr.Add(pUnitTable, i * 8);
                        IntPtr aUnit = ReadIntPtr(pUnit);

                        if (aUnit != IntPtr.Zero)
                        {
                            var aPlayerUnitCheck = IntPtr.Add(aUnit, 0xB8);
                            var playerUnitCheck = ReadLong(aPlayerUnitCheck);
                            if (playerUnitCheck == 0x0000000000000100)
                            {
                                AdrPlayerUnit = aUnit;
                                PtrPlayerUnit = pUnit;
                                break;
                            }
                        }
                    }
                }

                AdrPlayerUnit = ReadIntPtr(PtrPlayerUnit);
                var pPlayer = IntPtr.Add(AdrPlayerUnit, Offsets.Player);
                var pAct = IntPtr.Add(AdrPlayerUnit, Offsets.Act);
                var pPath = IntPtr.Add(AdrPlayerUnit, Offsets.Path);

                if (AdrPlayerUnit == IntPtr.Zero)
                {
                    PtrPlayerUnit = IntPtr.Zero;
                    throw new Exception("Player pointer is zero.");
                }

                IntPtr aPlayer = ReadIntPtr(pPlayer);

                var playerName = ReadString(aPlayer);

                IntPtr aAct = ReadIntPtr(pAct);
                var aMapSeed = IntPtr.Add(aAct, Offsets.MapSeed);
                var pActUnk1 = IntPtr.Add(aAct, Offsets.ActUnk1);

                IntPtr aActUnk2 = ReadIntPtr(pActUnk1);
                var aGameDifficulty = IntPtr.Add(aActUnk2, Offsets.GameDifficulty);

                var gameDifficulty = ReadUShort(aGameDifficulty);
                if (gameDifficulty >= 3)
                {
                    throw new Exception("Difficulty is invalid. Expected value should be between 0 and 2 (inclusive)");
                }

                IntPtr aPath = ReadIntPtr(pPath);
                var aPositionX = IntPtr.Add(aPath, Offsets.PosXAdress);
                var aPositionY = IntPtr.Add(aPath, Offsets.PosYAdress);
                var pRoom1 = IntPtr.Add(aPath, Offsets.Room1);

                var positionX = ReadUShort(aPositionX);
                var positionY = ReadUShort(aPositionY);

                IntPtr aRoom1 = ReadIntPtr(pRoom1);
                var pRoom2 = IntPtr.Add(aRoom1, Offsets.Room2);

                IntPtr aRoom2 = ReadIntPtr(pRoom2);
                var pLevel = IntPtr.Add(aRoom2, Offsets.Level);

                IntPtr aLevel = ReadIntPtr(pLevel);
                var aLevelId = IntPtr.Add(aLevel, Offsets.LevelId);

                if (aLevel == IntPtr.Zero)
                {
                    throw new Exception("Level address is zero.");
                }
 
                var levelId = ReadUInt32(aLevelId);

                var mapSeed = ReadUInt32(aMapSeed);

                var aUiSettingsPath = IntPtr.Add(processAddress, Offsets.InGameMap);
                var mapShown = ReadBool(aUiSettingsPath);

                return new GameData
                {
                    PlayerPosition = new Point(positionX, positionY),
                    MapSeed = mapSeed,
                    Area = (Area)levelId,
                    Difficulty = (Difficulty)gameDifficulty,
                    MapShown = mapShown,
                    MainWindowHandle = gameProcess.MainWindowHandle
                };
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (ProcessHandle != IntPtr.Zero)
                {
                    WindowsExternal.CloseHandle(ProcessHandle);
                }
            }
        }

        private static uint ReadDWord(IntPtr lpBaseAddress)
        {
            return BitConverter.ToUInt32(ReadBuffer(lpBaseAddress, sizeof(uint)), 0);
        }

        private static uint ReadUInt32(IntPtr lpBaseAddress)
        {
            return ReadDWord(lpBaseAddress);
        }

        private static IntPtr ReadIntPtr(IntPtr lpBaseAddress)
        {
            return (IntPtr)BitConverter.ToInt64(ReadBuffer(lpBaseAddress, sizeof(long)), 0);
        }

        private static long ReadLong(IntPtr lpBaseAddress)
        {
            return BitConverter.ToInt64(ReadBuffer(lpBaseAddress, sizeof(long)), 0);
        }

        private static string ReadString(IntPtr lpBaseAddress, byte bufferSize = 16)
        {
            return Encoding.ASCII.GetString(ReadBuffer(lpBaseAddress, bufferSize));
        }

        private static byte ReadByte(IntPtr lpBaseAddress)
        {
            return ReadBuffer(lpBaseAddress, 1)[0];
        }

        private static bool ReadBool(IntPtr lpBaseAddress)
        {
            return ReadBuffer(lpBaseAddress, 1)[0] == 1;
        }

        private static ushort ReadUShort(IntPtr lpBaseAddress)
        {
            return BitConverter.ToUInt16(ReadBuffer(lpBaseAddress, sizeof(ushort)), 0);
        }

        private static byte[] ReadBuffer(IntPtr lpBaseAddress, byte bufferSize)
        {
            var buffer = new byte[bufferSize];
            IntPtr result;
            if (!WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, lpBaseAddress, buffer,
                buffer.Length, out _))
            {
                throw new Exception("Wrong Offset. No Data at this address!");
            }
            return buffer;
        }

    }
}
