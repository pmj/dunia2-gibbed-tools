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

using System.Xml.Serialization;

namespace Gibbed.Dunia2.ConvertObjectBinary.Definitions.Raw
{
    [XmlRoot("file")]
    public class ObjectFileDefinition
    {
        private string _Path;
        private string _Name;
        private ObjectDefinition _ObjectDefinition;

        [XmlIgnore]
        public string Path
        {
            get { return this._Path; }
            set { this._Path = value; }
        }

        [XmlAttribute("name")]
        public string Name
        {
            get { return this._Name; }
            set { this._Name = value; }
        }

        [XmlElement("object")]
        public ObjectDefinition ObjectDefinition
        {
            get { return this._ObjectDefinition; }
            set { this._ObjectDefinition = value; }
        }
    }
}
