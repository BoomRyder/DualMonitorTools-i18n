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
using System.Text;
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

		static string IssPath
		{
			get { return Path.Combine(RepoRoot, @"installer\DualMonitorTools.iss"); }
		}

		static string ReadIss()
		{
			return File.ReadAllText(IssPath, Encoding.UTF8);
		}

		[Test]
		public void IssExists()
		{
			Assert.IsTrue(File.Exists(IssPath), "missing: installer\\DualMonitorTools.iss");
		}

		[Test]
		public void IssDefinesTheApplicationIdentity()
		{
			string iss = ReadIss();

			// The [Setup] directives below are written with {#MyAppName}, so the
			// literal product name lives here and nowhere else.
			Assert.IsTrue(iss.IndexOf("#define MyAppName \"Dual Monitor Tools\"",
					StringComparison.Ordinal) >= 0,
				"MyAppName must be \"Dual Monitor Tools\"");

			Assert.IsTrue(iss.IndexOf("#define MyAppVersion \"2.13\"",
					StringComparison.Ordinal) >= 0,
				"MyAppVersion must be \"2.13\"");
		}

		[TestCase(@"DefaultDirName={autopf}\{#MyAppName}")]
		[TestCase("DefaultGroupName={#MyAppName}")]
		[TestCase("PrivilegesRequired=admin")]
		[TestCase("ArchitecturesAllowed=x64compatible")]
		[TestCase("DisableProgramGroupPage=yes")]
		[TestCase("CloseApplications=yes")]
		public void IssDeclaresRequiredSetupDirective(string expectedLine)
		{
			string iss = ReadIss();

			Assert.IsTrue(iss.IndexOf(expectedLine, StringComparison.Ordinal) >= 0,
				"missing [Setup] directive: " + expectedLine);
		}

		[Test]
		public void IssDoesNotWriteTheInstalledMarker()
		{
			string iss = ReadIss();

			// Writing this value makes CheckUpdatesForm offer "install the new
			// version", and that button downloads and runs the upstream English
			// MSI, overwriting this Chinese build.
			Assert.IsFalse(iss.IndexOf("ValueName: \"installed\"", StringComparison.Ordinal) >= 0,
				"the 'installed' marker must never be written");
		}

		[TestCase("english.ErrNoDotNet=")]
		[TestCase("chinesesimplified.ErrNoDotNet=")]
		[TestCase("english.ErrLegacyMsi=")]
		[TestCase("chinesesimplified.ErrLegacyMsi=")]
		[TestCase("english.ErrDmtRunning=")]
		[TestCase("chinesesimplified.ErrDmtRunning=")]
		public void IssDeclaresBilingualPreflightMessages(string expectedPrefix)
		{
			string iss = ReadIss();

			Assert.IsTrue(iss.IndexOf(expectedPrefix, StringComparison.Ordinal) >= 0,
				"missing [CustomMessages] entry: " + expectedPrefix);
		}

		[TestCase("ErrNoDotNet")]
		[TestCase("ErrLegacyMsi")]
		[TestCase("ErrDmtRunning")]
		public void IssRoutesPreflightMessagesThroughCustomMessage(string name)
		{
			string iss = ReadIss();

			// A literal string in the Pascal block would ignore the wizard's
			// language; {cm:...} keeps the error box bilingual.
			Assert.IsTrue(iss.IndexOf("CustomMessage('" + name + "')", StringComparison.Ordinal) >= 0,
				"the " + name + " error box must use CustomMessage()");
		}

		[TestCase(@"DMT\DMT\bin\Release\DMT.exe")]
		[TestCase(@"DMT\DMT\bin\Release\DMT.exe.config")]
		[TestCase(@"DMT\DMT\bin\Release\zh-Hans\DMT.resources.dll")]
		[TestCase(@"DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe")]
		[TestCase(@"DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe.config")]
		[TestCase(@"DualWallpaper\DualWallpaper\bin\Release\zh-Hans\DualWallpaper.resources.dll")]
		[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe")]
		[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe.config")]
		[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\zh-Hans\DmtWallpaper.resources.dll")]
		[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.scr")]
		public void IssShipsEveryReleaseArtifact(string relativePath)
		{
			string iss = ReadIss();

			// The trailing quote keeps this exact: the bare "DMT.exe" would
			// also match the "DMT.exe.config" line.
			string expected = relativePath + "\"";

			Assert.IsTrue(iss.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
				"not shipped by the installer: " + relativePath);

			Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, relativePath)),
				"missing build output (run build.ps1 -Configuration Release first): " + relativePath);
		}
	}
}
