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
			// MSI, overwriting this Chinese build. Registry value names are
			// case-insensitive, so compare case-insensitively.
			Assert.IsFalse(iss.IndexOf("valuename: \"installed\"", StringComparison.OrdinalIgnoreCase) >= 0,
				"the 'installed' marker must never be written from [Registry]");

			// Reading the value is exactly how the legacy check must work; only
			// a write is forbidden.
			foreach (string line in iss.Split('\n'))
			{
				string lowered = line.ToLowerInvariant();
				if (lowered.IndexOf("'installed'", StringComparison.Ordinal) < 0)
				{
					continue;
				}

				Assert.IsFalse(lowered.IndexOf("regwrite", StringComparison.Ordinal) >= 0,
					"the 'installed' marker must never be written from [Code]: " + line.Trim());
			}
		}

		[Test]
		public void IssDetectsTheLegacyMsiByTheInstalledValueNotTheKey()
		{
			string iss = ReadIss();

			// This installer writes ScreenSaverInstalled into LegacyInstalledKey
			// itself. Testing that key for existence would make InitializeSetup
			// refuse to run a second time - a reinstall or an upgrade - and tell
			// the user to uninstall an "original MSI" that is not there.
			Assert.IsFalse(iss.IndexOf("RegKeyExists(HKLM, LegacyInstalledKey)", StringComparison.Ordinal) >= 0,
				"legacy detection must not test LegacyInstalledKey for existence");
			Assert.IsFalse(iss.IndexOf("RegKeyExists(HKLM64, LegacyInstalledKey)", StringComparison.Ordinal) >= 0,
				"legacy detection must not test LegacyInstalledKey for existence");

			Assert.IsTrue(iss.IndexOf("RegQueryDWordValue(HKLM, LegacyInstalledKey, 'installed'", StringComparison.Ordinal) >= 0,
				"legacy detection must query the 'installed' value");
		}

		[Test]
		public void IssInstallsTheDataDirectoryRedirectBesideTheExecutables()
		{
			string iss = ReadIss();

			// Without this file DMT falls back to portable mode and tries to
			// write its settings under Program Files, where a normal user has
			// no write access.
			Assert.IsTrue(iss.IndexOf(@"installer\DmtFileLocations.xml""; DestDir: ""{app}""", StringComparison.Ordinal) >= 0,
				"DmtFileLocations.xml must be installed next to DMT.exe");
		}

		[Test]
		public void IssInstallsSatelliteAssembliesIntoTheZhHansSubdirectory()
		{
			string iss = ReadIss();
			string marker = @"; DestDir: ""{app}\zh-Hans""";

			int count = 0;
			int at = 0;
			while ((at = iss.IndexOf(marker, at, StringComparison.Ordinal)) >= 0)
			{
				count++;
				at += marker.Length;
			}

			// A satellite dropped into {app} instead of {app}\zh-Hans falls back
			// to the neutral resources and the UI silently stays English.
			Assert.AreEqual(3, count, "all three zh-Hans satellites must land in {app}\\zh-Hans");
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

		[Test]
		public void IssInstallsScreenSaverIntoTheRealSystem32()
		{
			string iss = ReadIss();

			// A 32-bit installer writing "{sys}" is redirected to SysWOW64 by
			// WOW64, where the 64-bit desk.cpl never lists the screen saver.
			// Only {sysnative} reaches the real System32.
			Assert.IsTrue(iss.IndexOf(@"DestDir: ""{sysnative}""", StringComparison.Ordinal) >= 0,
				"the screen saver must be installed with DestDir {sysnative}");

			Assert.IsFalse(iss.IndexOf(@"DestDir: ""{sys}""", StringComparison.Ordinal) >= 0,
				"DestDir {sys} lands in SysWOW64 and hides the screen saver");
		}

		[TestCase("CurUninstallStepChanged")]
		[TestCase("usPostUninstall")]
		[TestCase("ClearWallpaperIfOurs")]
		[TestCase("RegDeleteKeyIncludingSubkeys(HKCU, DmtProductKey)")]
		[TestCase("RegDeleteKeyIncludingSubkeys(HKCU, DmtContextMenuKey)")]
		[TestCase("RegDeleteKeyIfEmpty(HKLM, LegacyVendorKey)")]
		[TestCase("RegDeleteValue(HKCU, AutoStartRunKey, AutoStartValueName)")]
		[TestCase("'SCRNSAVE.EXE'")]
		[TestCase("DelTree(DataDir, True, True, True)")]
		[TestCase(@"{userappdata}\' + DataFolderName")]
		[TestCase(@"DualMonitorTools.msi")]
		[TestCase("SPIF_UPDATEINIFILE or SPIF_SENDCHANGE")]
		public void IssCleansUpRuntimeArtefactsOnUninstall(string expectedFragment)
		{
			string iss = ReadIss();

			Assert.IsTrue(iss.IndexOf(expectedFragment, StringComparison.Ordinal) >= 0,
				"missing uninstall step: " + expectedFragment);
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
		[TestCase(@"installer\DmtFileLocations.xml")]
		[TestCase(@"COPYING.txt")]
		[TestCase(@"README.md")]
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
