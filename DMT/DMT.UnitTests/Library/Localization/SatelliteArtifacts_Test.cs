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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using NUnit.Framework;

namespace DMT.UnitTests.Library.Localization
{
	/// <summary>
	/// Guards the Simplified Chinese satellite files and the language wiring of the
	/// two smaller executables, which cannot host their own test project.
	/// </summary>
	[TestFixture]
	public class SatelliteArtifacts_Test
	{
		static string RepoRoot
		{
			get
			{
				return Path.GetFullPath(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
			}
		}

		/// <summary>
		/// Reads the string entries of a resx, skipping image/binary references.
		/// A satellite only carries translations, never the ResXFileRef entries
		/// that point at icons and bitmaps; those fall back to the neutral file.
		/// </summary>
		static Dictionary<string, string> ReadStrings(string path)
		{
			Assert.IsTrue(File.Exists(path), "missing resource file: " + path);

			XmlDocument doc = new XmlDocument();
			doc.Load(path);

			Dictionary<string, string> result = new Dictionary<string, string>();
			XmlNodeList nodes = doc.SelectNodes("/root/data");
			if (nodes != null)
			{
				foreach (XmlNode node in nodes)
				{
					XmlAttribute nameAttribute = node.Attributes["name"];
					if (nameAttribute == null)
					{
						continue;
					}

					XmlAttribute typeAttribute = node.Attributes["type"];
					if (typeAttribute != null &&
						typeAttribute.Value.StartsWith("System.Resources.ResXFileRef", StringComparison.Ordinal))
					{
						continue;
					}

					XmlNode valueNode = node.SelectSingleNode("value");
					result[nameAttribute.Value] = valueNode == null ? string.Empty : valueNode.InnerText;
				}
			}

			return result;
		}

		static string PlaceholderSignature(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}

			List<string> found = new List<string>();
			foreach (Match match in Regex.Matches(text, @"\{\d+\}"))
			{
				found.Add(match.Value);
			}

			found.Sort(StringComparer.Ordinal);
			return string.Join(",", found.ToArray());
		}

		static void AssertSatelliteCovers(string projectRelativeResourcesPath)
		{
			string neutral = Path.Combine(RepoRoot, projectRelativeResourcesPath);
			string satellite = Path.Combine(
				Path.GetDirectoryName(neutral),
				Path.GetFileNameWithoutExtension(neutral) + ".zh-Hans.resx");

			Dictionary<string, string> english = ReadStrings(neutral);
			Dictionary<string, string> chinese = ReadStrings(satellite);

			List<string> missing = new List<string>();
			List<string> mismatch = new List<string>();
			foreach (KeyValuePair<string, string> pair in english)
			{
				string translated;
				if (!chinese.TryGetValue(pair.Key, out translated))
				{
					missing.Add(pair.Key);
					continue;
				}

				if (!string.Equals(
					PlaceholderSignature(pair.Value),
					PlaceholderSignature(translated),
					StringComparison.Ordinal))
				{
					mismatch.Add(pair.Key);
				}
			}

			Assert.IsEmpty(missing, Path.GetFileName(neutral) + ": untranslated keys: " + string.Join(", ", missing.ToArray()));
			Assert.IsEmpty(mismatch, Path.GetFileName(neutral) + ": placeholder mismatch: " + string.Join(", ", mismatch.ToArray()));
		}

		[Test]
		public void DualWallpaperSatelliteExposesChineseText()
		{
			string satellite = Path.Combine(
				RepoRoot, @"DualWallpaper\DualWallpaper\Properties\Resources.zh-Hans.resx");

			Dictionary<string, string> chinese = ReadStrings(satellite);

			bool anyCjk = false;
			foreach (string value in chinese.Values)
			{
				if (Regex.IsMatch(value, @"[\u4e00-\u9fff]"))
				{
					anyCjk = true;
					break;
				}
			}

			Assert.IsTrue(anyCjk, "DualWallpaper zh-Hans resource has no Chinese text");
		}

		[Test]
		public void DualWallpaperSatelliteCoversEveryStringKey()
		{
			AssertSatelliteCovers(@"DualWallpaper\DualWallpaper\Properties\Resources.resx");
		}

		[Test]
		public void DmtWallpaperSatelliteExposesChineseText()
		{
			string satellite = Path.Combine(
				RepoRoot, @"DmtWallpaper\DmtWallpaper\Properties\Resources.zh-Hans.resx");

			Dictionary<string, string> chinese = ReadStrings(satellite);

			bool anyCjk = false;
			foreach (string value in chinese.Values)
			{
				if (Regex.IsMatch(value, @"[\u4e00-\u9fff]"))
				{
					anyCjk = true;
					break;
				}
			}

			Assert.IsTrue(anyCjk, "DmtWallpaper zh-Hans resource has no Chinese text");
		}

		[Test]
		public void DmtWallpaperSatelliteCoversEveryStringKey()
		{
			AssertSatelliteCovers(@"DmtWallpaper\DmtWallpaper\Properties\Resources.resx");
		}

		// Every executable must resolve the language before it creates a window,
		// otherwise the first window is built in the previous language.
		[TestCase(@"DMT\DMT\Program.cs")]
		[TestCase(@"DualWallpaper\DualWallpaper\Program.cs")]
		[TestCase(@"DmtWallpaper\DmtWallpaper\Program.cs")]
		public void ProgramAppliesLanguageAtStartup(string programRelativePath)
		{
			string path = Path.Combine(RepoRoot, programRelativePath);
			Assert.IsTrue(File.Exists(path), "missing program file: " + path);

			string text = File.ReadAllText(path);

			StringAssert.Contains("UiCulture.Apply(LanguageCatalog.ResolveFromRegistry())", text,
				programRelativePath + " does not apply the saved language");
		}

		/// <summary>
		/// Collects the resource keys that a designer pulls out with
		/// resources.GetString("key"), which is how the designer stores text that
		/// is too long to inline as a string literal: paragraphs and tooltips.
		/// </summary>
		static Dictionary<string, List<string>> DesignerGetStringKeys(string projectRelativePath)
		{
			string root = Path.Combine(RepoRoot, projectRelativePath);
			Assert.IsTrue(Directory.Exists(root), "missing project folder: " + root);

			Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

			foreach (string file in Directory.GetFiles(root, "*.Designer.cs", SearchOption.AllDirectories))
			{
				string text = File.ReadAllText(file);

				List<string> keys = new List<string>();
				foreach (Match match in Regex.Matches(text, @"resources\.GetString\(\s*""([^""]+)""\s*\)"))
				{
					keys.Add(match.Groups[1].Value);
				}

				if (keys.Count > 0)
				{
					result[file] = keys;
				}
			}

			return result;
		}

		/// <summary>
		/// Every string a designer fetches with resources.GetString must have a
		/// Chinese counterpart, otherwise that paragraph or tooltip stays English.
		/// The literal scanner in NoLiteralRemains_Test cannot see these, because
		/// the text is not a literal.
		/// </summary>
		[Test]
		public void EveryDesignerGetStringKeyHasASatellite()
		{
			Dictionary<string, List<string>> usage = DesignerGetStringKeys(@"DMT\DMT");

			Assert.IsNotEmpty(usage, "no resources.GetString call sites were found; the scan is broken");

			List<string> problems = new List<string>();

			foreach (KeyValuePair<string, List<string>> pair in usage)
			{
				string designer = pair.Key;
				string neutral = designer.Substring(0, designer.Length - ".Designer.cs".Length) + ".resx";
				string satellite = designer.Substring(0, designer.Length - ".Designer.cs".Length) + ".zh-Hans.resx";

				if (!File.Exists(neutral))
				{
					problems.Add(Path.GetFileName(designer) + ": neutral resx missing");
					continue;
				}

				if (!File.Exists(satellite))
				{
					problems.Add(Path.GetFileName(satellite) + ": satellite missing for "
						+ string.Join(", ", pair.Value.ToArray()));
					continue;
				}

				Dictionary<string, string> chinese = ReadStrings(satellite);

				foreach (string key in pair.Value)
				{
					string translated;
					if (!chinese.TryGetValue(key, out translated))
					{
						problems.Add(Path.GetFileName(satellite) + ": no entry for " + key);
					}
					else if (!Regex.IsMatch(translated ?? string.Empty, @"[\u4e00-\u9fff]"))
					{
						problems.Add(Path.GetFileName(satellite) + ": " + key + " has no Chinese text");
					}
				}
			}

			Assert.IsEmpty(problems, string.Join("; ", problems.ToArray()));
		}

		/// <summary>
		/// A designer satellite may only carry text entries. Importing the neutral
		/// file's layout entries would give the form a second layout source, which
		/// is exactly the trap EditNumericValueForm was built to avoid.
		/// </summary>
		[Test]
		public void DesignerSatelliteCarriesTextEntriesOnly()
		{
			string[] satellites = Directory.GetFiles(
				Path.Combine(RepoRoot, @"DMT\DMT\Modules"), "*.zh-Hans.resx", SearchOption.AllDirectories);

			Assert.IsNotEmpty(satellites, "no designer satellites found; the scan is broken");

			List<string> problems = new List<string>();

			foreach (string satellite in satellites)
			{
				foreach (string key in ReadStrings(satellite).Keys)
				{
					if (!key.EndsWith(".Text", StringComparison.Ordinal) &&
						!key.EndsWith(".ToolTip", StringComparison.Ordinal))
					{
						problems.Add(Path.GetFileName(satellite) + ": non-text entry " + key);
					}
				}
			}

			Assert.IsEmpty(problems, string.Join("; ", problems.ToArray()));
		}

		// The satellite must sit beside the neutral resource in the build output,
		// in a folder named after the culture. The configuration is taken from the
		// test assembly's own path so that a Debug run is checked against Debug
		// output; build.ps1 defaults to Debug, and hard coding Release made
		// ".\build.ps1 -Test" fail for a reason unrelated to the product.
		[TestCase(@"DMT\DMT\bin\{0}\zh-Hans\DMT.resources.dll")]
		[TestCase(@"DualWallpaper\DualWallpaper\bin\{0}\zh-Hans\DualWallpaper.resources.dll")]
		[TestCase(@"DmtWallpaper\DmtWallpaper\bin\{0}\zh-Hans\DmtWallpaper.resources.dll")]
		public void SatelliteAssemblyIsBuiltIntoTheCultureFolder(string relativePathFormat)
		{
			string relativePath = string.Format(relativePathFormat, BuildConfiguration);

			string path = Path.Combine(RepoRoot, relativePath);

			Assert.IsTrue(File.Exists(path),
				"satellite assembly not built: " + relativePath);
		}

		/// <summary>
		/// Gets the configuration this test assembly was built as, so the expected
		/// satellite path matches the configuration that was actually produced.
		/// </summary>
		static string BuildConfiguration
		{
			get
			{
				// ...\<Project>\bin\<Configuration>\<Assembly>.dll
				DirectoryInfo bin = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

				if (bin.Parent != null && bin.Parent.Name == "bin" && bin.Name.Length > 0)
				{
					return bin.Name;
				}

				return "Release";
			}
		}
	}
}
