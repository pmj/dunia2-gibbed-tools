﻿/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.IO;
using System.Text;
using Gibbed.IO;

namespace Gibbed.FarCry3.FileFormats.Map
{
    public class Info
    {
        public uint Unknown2;
        public uint Unknown3;
        public uint Unknown4;

        public long Unknown5;
        public string Creator;
        public long Unknown7;
        public string Author;
        public string Name;
        public MapId Id;
        public Guid VersionId;
        public DateTime TimeModified;
        public DateTime TimeCreated;
        public MapSize Size;
        public PlayerRange PlayerRange;
        public uint Unknown16;
        public byte Unknown17;

        public void Deserialize(Stream input, Endian endian)
        {
            this.Unknown2 = input.ReadValueU32(endian);
            this.Unknown3 = input.ReadValueU32(endian);
            this.Unknown4 = input.ReadValueU32(endian);

            this.Unknown5 = input.ReadValueS64(endian);
            this.Creator = input.ReadString(input.ReadValueU32(endian), Encoding.UTF8);
            this.Unknown7 = input.ReadValueS64(endian);
            this.Author = input.ReadString(input.ReadValueU32(endian), Encoding.UTF8);
            this.Name = input.ReadString(input.ReadValueU32(endian), Encoding.UTF8);
            this.Id = MapId.Deserialize(input, endian);
            this.VersionId = Helpers.ReadMungedGuid(input, endian);
            this.TimeModified = (DateTime)Helpers.ReadTime(input, endian);
            this.TimeCreated = (DateTime)Helpers.ReadTime(input, endian);
            this.Size = input.ReadValueEnum<MapSize>(endian);
            this.PlayerRange = input.ReadValueEnum<PlayerRange>(endian);
            this.Unknown16 = input.ReadValueU32(endian);
            this.Unknown17 = input.ReadValueU8();
        }

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }
    }
}
