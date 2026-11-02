#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2025  The Dual Monitor Tools Chinese translation project
// 
// Dual Monitor Tools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace DMT.UnitTests.Installer
{
	public class InstallerAssets_Test
	{
		static string RepoRoot
		{
			get
			{
				return Path.GetFullPath(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
			}
		}

		static string FileLocationsXml
		{
			get { return Path.Combine(RepoRoot, @"installer\DmtFileLocations.xml"); }
		}

		[Test]
		public void FileLocationsXmlExists()
		{
			Assert.IsTrue(File.Exists(FileLocationsXml),
				"missing: installer\\DmtFileLocations.xml");
		}

		[Test]
		public void FileLocationsXmlIsUtf8WithBom()
		{
			byte[] head = new byte[3];
			using (FileStream s = File.OpenRead(FileLocationsXml))
			{
				Assert.AreEqual(3, s.Read(head, 0, 3), "file too short");
			}

			Assert.AreEqual(0xEF, head[0], "missing UTF-8 BOM");
			Assert.AreEqual(0xBB, head[1], "missing UTF-8 BOM");
			Assert.AreEqual(0xBF, head[2], "missing UTF-8 BOM");
		}

		[Test]
		public void FileLocationsXmlIsWellFormed()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(FileLocationsXml);   // throws on malformed XML
			Assert.IsNotNull(doc.DocumentElement);
		}

		[Test]
		public void FileLocationsXmlRedirectsDataDirectoryToAppData()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(FileLocationsXml);

			XmlNodeList nodes = doc.SelectNodes("/filelocations/filelocation");
			Assert.AreEqual(1, nodes.Count,
				"expected exactly one active filelocation entry");

			Assert.AreEqual("datadirectory", nodes[0].Attributes["name"].Value);
			Assert.AreEqual(@"%APPDATA%\Dual Monitor Tools",
				nodes[0].Attributes["value"].Value);
		}
	}
}
