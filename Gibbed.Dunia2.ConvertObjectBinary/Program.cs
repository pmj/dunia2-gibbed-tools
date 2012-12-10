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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Gibbed.Dunia2.FileFormats;
using NDesk.Options;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            string baseName = "";
            bool showHelp = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {
                    "fcb",
                    "convert XML to FCB",
                    v => mode = v != null ? Mode.ToFcb : mode
                    },
                {
                    "xml",
                    "convert FCB to XML",
                    v => mode = v != null ? Mode.ToXml : mode
                    },
                {
                    "b|base-name=",
                    "when converting FCB to XML, use specified base name instead of file name",
                    v => baseName = v
                    },
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                    },
                {
                    "h|help",
                    "show this message and exit",
                    v => showHelp = v != null
                    },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (mode == Mode.Unknown &&
                extras.Count >= 1)
            {
                var extension = Path.GetExtension(extras[0]);

                if (string.IsNullOrEmpty(extension) == false)
                {
                    extension = extension.ToLowerInvariant();
                }

                if (extension == ".fcb" ||
                    extension == ".obj" ||
                    extension == ".lib")
                {
                    mode = Mode.ToXml;
                }
                else if (extension == ".xml")
                {
                    mode = Mode.ToFcb;
                }
            }

            if (showHelp == true ||
                mode == Mode.Unknown ||
                extras.Count < 1 ||
                extras.Count > 2)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input [output]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (verbose == true)
            {
                Console.WriteLine("Loading project...");
            }

            var manager = ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
                return;
            }

            var project = manager.ActiveProject;
            var config = Configuration.Load(project);

            if (mode == Mode.ToFcb)
            {
                string inputPath = extras[0];
                string outputPath;
                string basePath;

                if (extras.Count > 1)
                {
                    outputPath = extras[1];
                }
                else
                {
                    outputPath = Path.ChangeExtension(inputPath, null);
                    outputPath += "_converted.fcb";
                }

                basePath = Path.ChangeExtension(inputPath, null);

                if (string.IsNullOrEmpty(baseName) == true)
                {
                    baseName = Path.GetFileNameWithoutExtension(inputPath);

                    if (string.IsNullOrEmpty(baseName) == false &&
                        baseName.EndsWith("_converted") == true)
                    {
                        baseName = baseName.Substring(0, baseName.Length - 10);
                    }
                }

                inputPath = Path.GetFullPath(inputPath);
                outputPath = Path.GetFullPath(outputPath);
                basePath = Path.GetFullPath(basePath);

                var bof = new BinaryObjectFile();

                using (var input = File.OpenRead(inputPath))
                {
                    if (verbose == true)
                    {
                        Console.WriteLine("Loading XML...");
                    }

                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var root = nav.SelectSingleNode("/object");
                    if (root == null)
                    {
                        throw new FormatException();
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("Reading XML...");
                    }

                    var objectFileDef = config.GetObjectFileDefinition(baseName);
                    if (objectFileDef == null)
                    {
                        Console.WriteLine("Warning: could not find binary object definition '{0}'", baseName);
                    }

                    var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;
                    bof.Root = ReadNodeInternal(config, objectDef, basePath, root);
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing FCB...");
                }

                using (var output = File.Create(outputPath))
                {
                    bof.Serialize(output);
                }
            }
            else if (mode == Mode.ToXml)
            {
                string inputPath = extras[0];
                string outputPath;
                string basePath;

                if (extras.Count > 1)
                {
                    outputPath = extras[1];
                    basePath = Path.ChangeExtension(outputPath, null);
                }
                else
                {
                    outputPath = Path.ChangeExtension(inputPath, null);
                    outputPath += "_converted";
                    basePath = outputPath;
                    outputPath += ".xml";
                }

                if (string.IsNullOrEmpty(baseName) == true)
                {
                    baseName = Path.GetFileNameWithoutExtension(inputPath);
                }

                if (string.IsNullOrEmpty(baseName) == true)
                {
                    throw new InvalidOperationException();
                }

                inputPath = Path.GetFullPath(inputPath);
                outputPath = Path.GetFullPath(outputPath);
                basePath = Path.GetFullPath(basePath);

                if (verbose == true)
                {
                    Console.WriteLine("Reading binary...");
                }

                var bof = new BinaryObjectFile();
                using (var input = File.OpenRead(inputPath))
                {
                    bof.Deserialize(input);
                }

                var settings = new XmlWriterSettings();
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                settings.CheckCharacters = false;
                settings.OmitXmlDeclaration = false;

                const uint entityLibrariesHash = 0xBCDD10B4; // crc32(EntityLibraries)
                const uint entityLibraryHash = 0xE0BDB3DB; // crc32(EntityLibrary)
                const uint nameHash = 0xFE11D138; // crc32(Name);

                if (bof.Root.Values.Count == 0 &&
                    bof.Root.TypeHash == entityLibrariesHash &&
                    bof.Root.Children.Any(c => c.TypeHash != entityLibraryHash) == false)
                {
                    var objectFileDef = config.GetObjectFileDefinition(baseName);
                    var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;

                    Configuration.ClassDefinition classDef = null;
                    if (classDef == null && objectDef != null)
                    {
                        classDef = objectDef.ClassDefinition;
                    }

                    using (var writer = XmlWriter.Create(outputPath, settings))
                    {
                        writer.WriteStartDocument();

                        var root = bof.Root;
                        {
                            writer.WriteStartElement("object");
                            writer.WriteAttributeString("name", "EntityLibraries");

                            int counter = 0;
                            int padLength = root.Children.Count.ToString(CultureInfo.InvariantCulture).Length;
                            foreach (var child in root.Children)
                            {
                                counter++;

                                string childName = counter.ToString(CultureInfo.InvariantCulture).PadLeft(padLength, '0');

                                // name
                                if (child.Values.ContainsKey(nameHash) == true)
                                {
                                    var value = child.Values[nameHash];
                                    childName += "_" + Encoding.UTF8.GetString(value, 0, value.Length - 1);
                                }

                                Directory.CreateDirectory(basePath);

                                var childPath = Path.Combine(basePath, childName + ".xml");
                                using (var childWriter = XmlWriter.Create(childPath, settings))
                                {
                                    var childObjectDef = GetChildObjectDefinition(objectDef, classDef, child.TypeHash);

                                    childWriter.WriteStartDocument();
                                    WriteNode(config,
                                              childWriter,
                                              child,
                                              childObjectDef);
                                    childWriter.WriteEndDocument();
                                }

                                writer.WriteStartElement("object");
                                writer.WriteAttributeString("external", Path.GetFileName(childPath));
                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();
                        }

                        writer.WriteEndDocument();
                    }
                }
                else
                {
                    var objectFileDef = config.GetObjectFileDefinition(baseName);
                    var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;

                    using (var writer = XmlWriter.Create(outputPath, settings))
                    {
                        writer.WriteStartDocument();
                        WriteNode(config, writer, bof.Root, objectDef);
                        writer.WriteEndDocument();
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static void LoadNameAndHash(XPathNavigator node, out string name, out uint hash)
        {
            var _name = node.GetAttribute("name", "");
            var _hash = node.GetAttribute("hash", "");

            if (string.IsNullOrWhiteSpace(_name) == true &&
                string.IsNullOrWhiteSpace(_hash) == true)
            {
                throw new FormatException();
            }

            name = string.IsNullOrWhiteSpace(_name) == false ? _name : null;
            hash = name != null ? CRC32.Hash(name) : uint.Parse(_hash, NumberStyles.AllowHexSpecifier);
        }

        private static BinaryObject ReadNode(Configuration config,
                                             Configuration.ObjectDefinition objectDef,
                                             Configuration.ClassDefinition classDef,
                                             string basePath,
                                             XPathNavigator nav)
        {
            string className;
            uint classNameHash;

            LoadNameAndHash(nav, out className, out classNameHash);

            if (classDef == null || classDef.DynamicNestedClasses == false)
            {
                var childObjectDef = GetChildObjectDefinition(objectDef, classDef, classNameHash);
                return ReadNodeInternal(config, childObjectDef, basePath, nav);
            }

            if (classDef.DynamicNestedClasses == true)
            {
                Configuration.ObjectDefinition childObjectDef = null;

                var nestedClassDef = config.GetClassDefinition(classNameHash);
                if (nestedClassDef != null)
                {
                    childObjectDef = new Configuration.ObjectDefinition(nestedClassDef.Name,
                                                                        nestedClassDef.Hash,
                                                                        nestedClassDef,
                                                                        null,
                                                                        null,
                                                                        null);
                }

                return ReadNodeInternal(config, childObjectDef, basePath, nav);
            }

            throw new InvalidOperationException();
        }

        private static BinaryObject ReadNodeInternal(Configuration config,
                                                     Configuration.ObjectDefinition objectDef,
                                                     string basePath,
                                                     XPathNavigator nav)
        {
            string className;
            uint classNameHash;

            LoadNameAndHash(nav, out className, out classNameHash);

            Configuration.ClassDefinition classDef = null;

            if (objectDef != null &&
                objectDef.ClassFieldHash.HasValue == true)
            {
                var hash = GetClassDefinitionByField(objectDef.ClassFieldName, objectDef.ClassFieldHash, nav);
                if (hash.HasValue == false)
                {
                    throw new InvalidOperationException();
                }

                classDef = config.GetClassDefinition(hash.Value);
            }

            if (classDef == null &&
                objectDef != null)
            {
                classDef = objectDef.ClassDefinition;

                if (classDef != null &&
                    classDef.ClassFieldHash.HasValue == true)
                {
                    var hash = GetClassDefinitionByField(classDef.ClassFieldName, classDef.ClassFieldHash, nav);
                    if (hash.HasValue == false)
                    {
                        throw new InvalidOperationException();
                    }

                    classDef = config.GetClassDefinition(hash.Value);
                }
            }

            var parent = new BinaryObject();
            parent.TypeHash = classNameHash;

            var fields = nav.Select("field");
            while (fields.MoveNext() == true)
            {
                if (fields.Current == null)
                {
                    throw new InvalidOperationException();
                }

                string fieldName;
                uint fieldNameHash;

                LoadNameAndHash(fields.Current, out fieldName, out fieldNameHash);

                FieldType fieldType;
                var fieldTypeName = fields.Current.GetAttribute("type", "");
                if (Enum.TryParse(fieldTypeName, true, out fieldType) == false)
                {
                    throw new InvalidOperationException();
                }

                var fieldDef = classDef != null ? classDef.GetFieldDefinition(fieldNameHash) : null;

                if (parent.Values.ContainsKey(0x9C39465B) &&
                    BitConverter.ToUInt64(parent.Values[0x9C39465B], 0) == 0xAD4E5BF7E4616AD3)
                {
                }

                byte[] data = FieldTypeSerializers.Serialize(fieldDef, fieldType, fields.Current);
                parent.Values.Add(fieldNameHash, data);
            }

            var children = nav.Select("object");
            while (children.MoveNext() == true)
            {
                parent.Children.Add(LoadNode(config, objectDef, classDef, basePath, children.Current));
            }

            return parent;
        }

        private static uint? GetClassDefinitionByField(string classFieldName, uint? classFieldHash, XPathNavigator nav)
        {
            uint? hash = null;

            if (string.IsNullOrEmpty(classFieldName) == false)
            {
                var fieldByName = nav.SelectSingleNode("field[@name=\"" + classFieldName + "\"]");
                if (fieldByName != null)
                {
                    uint temp;
                    if (
                        uint.TryParse(fieldByName.Value,
                                      NumberStyles.AllowHexSpecifier,
                                      CultureInfo.InvariantCulture,
                                      out temp) == false)
                    {
                        throw new InvalidOperationException();
                    }
                    hash = temp;
                }
            }

            if (hash.HasValue == false)
            {
                var fieldByHash =
                    nav.SelectSingleNode("field[@hash=\"" +
                                         classFieldHash.Value.ToString("X8", CultureInfo.InvariantCulture) +
                                         "\"]");
                if (fieldByHash == null)
                {
                    uint temp;
                    if (
                        uint.TryParse(fieldByHash.Value,
                                      NumberStyles.AllowHexSpecifier,
                                      CultureInfo.InvariantCulture,
                                      out temp) == false)
                    {
                        throw new InvalidOperationException();
                    }
                    hash = temp;
                }
            }

            return hash;
        }

        private static BinaryObject LoadNode(Configuration config,
                                             Configuration.ObjectDefinition objectDef,
                                             Configuration.ClassDefinition classDef,
                                             string basePath,
                                             XPathNavigator node)
        {
            var external = node.GetAttribute("external", "");
            if (string.IsNullOrWhiteSpace(external) == true)
            {
                return ReadNode(config, objectDef, classDef, basePath, node);
            }

            var inputPath = Path.Combine(basePath, external);
            using (var input = File.OpenRead(inputPath))
            {
                //Console.WriteLine("Loading object from '{0}'...", Path.GetFileName(inputPath));

                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/object");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                return ReadNode(config, objectDef, classDef, Path.ChangeExtension(inputPath, null), root);
            }
        }

        private static void WriteNode(Configuration config,
                                      XmlWriter writer,
                                      BinaryObject node,
                                      Configuration.ObjectDefinition objectDef)
        {
            Configuration.ClassDefinition classDef = null;

            if (objectDef != null &&
                objectDef.ClassFieldHash.HasValue == true)
            {
                if (node.Values.ContainsKey(objectDef.ClassFieldHash.Value) == true)
                {
                    var bytes = node.Values[objectDef.ClassFieldHash.Value];
                    var hash = BitConverter.ToUInt32(bytes, 0);
                    classDef = config.GetClassDefinition(hash);
                }
            }

            if (classDef == null &&
                objectDef != null)
            {
                classDef = objectDef.ClassDefinition;

                if (classDef != null &&
                    classDef.ClassFieldHash.HasValue == true)
                {
                    if (node.Values.ContainsKey(classDef.ClassFieldHash.Value) == true)
                    {
                        var bytes = node.Values[classDef.ClassFieldHash.Value];
                        var hash = BitConverter.ToUInt32(bytes, 0);
                        classDef = config.GetClassDefinition(hash);
                    }
                }
            }

            writer.WriteStartElement("object");

            if (classDef != null && classDef.Name != null && classDef.Hash == node.TypeHash)
            {
                writer.WriteAttributeString("name", classDef.Name);
            }
            else if (objectDef != null && objectDef.Name != null && objectDef.Hash == node.TypeHash)
            {
                writer.WriteAttributeString("name", objectDef.Name);
            }
            else
            {
                writer.WriteAttributeString("hash", node.TypeHash.ToString("X8"));
            }

            if (node.Values != null &&
                node.Values.Count > 0)
            {
                foreach (var kv in node.Values)
                {
                    writer.WriteStartElement("field");

                    var fieldDef = classDef != null ? classDef.GetFieldDefinition(kv.Key) : null;

                    if (fieldDef != null && fieldDef.Name != null)
                    {
                        writer.WriteAttributeString("name", fieldDef.Name);
                    }
                    else
                    {
                        writer.WriteAttributeString("hash", kv.Key.ToString("X8"));
                    }

                    if (fieldDef == null)
                    {
                        writer.WriteAttributeString("type", FieldType.BinHex.ToString());
                        writer.WriteBinHex(kv.Value, 0, kv.Value.Length);
                    }
                    else
                    {
                        writer.WriteAttributeString("type", fieldDef.Type.ToString());

                        if (fieldDef.Type == FieldType.Enum &&
                            fieldDef.EnumDefinition != null)
                        {
                            writer.WriteAttributeString("enum", fieldDef.EnumDefinition.Name);
                        }

                        FieldTypeDeserializers.Deserialize(writer, fieldDef, kv.Value);
                    }

                    writer.WriteEndElement();
                }
            }

            if (node.Children.Count > 0)
            {
                if (classDef == null || classDef.DynamicNestedClasses == false)
                {
                    foreach (var child in node.Children)
                    {
                        var childObjectDef = GetChildObjectDefinition(objectDef, classDef, child.TypeHash);
                        WriteNode(config, writer, child, childObjectDef);
                    }
                }
                else if (classDef.DynamicNestedClasses == true)
                {
                    foreach (var child in node.Children)
                    {
                        Configuration.ObjectDefinition childObjectDef = null;

                        var nestedClassDef = config.GetClassDefinition(child.TypeHash);
                        if (nestedClassDef != null)
                        {
                            childObjectDef = new Configuration.ObjectDefinition(nestedClassDef.Name,
                                                                                nestedClassDef.Hash,
                                                                                nestedClassDef,
                                                                                null,
                                                                                null,
                                                                                null);
                        }
                        else
                        {
                            //Console.WriteLine("Wanted a dynamic class with has {0:X8}", child.TypeHash);
                        }

                        WriteNode(config, writer, child, childObjectDef);
                    }
                }
            }

            writer.WriteEndElement();
        }

        private static Configuration.ObjectDefinition GetChildObjectDefinition(Configuration.ObjectDefinition objectDef,
                                                                               Configuration.ClassDefinition classDef,
                                                                               uint typeHash)
        {
            Configuration.ObjectDefinition childObjectDef = null;

            if (classDef != null)
            {
                var nestedClassDef = classDef.GetNestedClassDefinition(typeHash);
                if (nestedClassDef != null)
                {
                    childObjectDef = new Configuration.ObjectDefinition(nestedClassDef.Name,
                                                                        nestedClassDef.Hash,
                                                                        nestedClassDef,
                                                                        null,
                                                                        null,
                                                                        null);
                }
            }

            if (childObjectDef == null && objectDef != null)
            {
                childObjectDef = objectDef.GetNestedObjectDefinition(typeHash);
            }

            return childObjectDef;
        }
    }
}
