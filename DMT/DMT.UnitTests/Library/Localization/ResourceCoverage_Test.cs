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
using System.Globalization;
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
	/// Guards that every neutral resource string has a Simplified Chinese
	/// translation, that format placeholders survive translation, and that the
	/// satellite files are encoded the way the build expects.
	/// </summary>
	[TestFixture]
	public class ResourceCoverage_Test
	{
		// Resource classes that must each have a .zh-Hans.resx satellite.
		static readonly string[] DmtResourceClasses = new string[]
		{
			"AppStrings",
			"CommonStrings",
			"CursorStrings",
			"GeneralStrings",
			"LauncherStrings",
			"SnapStrings",
			"SwapScreenStrings",
			"WallpaperStrings"
		};

		// English values that may legitimately appear unchanged in Chinese:
		// product names, the licence file name and a few proper nouns.
		static readonly string[] UntranslatableValues = new string[]
		{
			"Dual Monitor Tools",
			"Dual Wallpaper",
			"DualWallpaper",
			"Dmt Wallpaper",
			"DmtWallpaper",
			"COPYING.txt",
			"Alt",
			"Ctrl",
			"Shift",
			"Win",
			"Win+<Up>",
			"Win+<Down>",
			"Win+<Left>",
			"Win+<Right>",
			"Win+Shift+<Left>",
			"Win+Shift+<Right>",
			"Win+<Home>",
			"X1",
			"X2",
			"SlickRun",
			// Escape-sequence legend on the Url provider panel: the syntax is the
			// message, so it reads the same in both languages.
			"%% -> %",
			// Units and separators that read the same in both languages.
			"0°",
			"90°",
			"180°",
			"270°",
			"1%",
			"5%",
			"20%",
			"+",
			// Dimensions separator between the width and height boxes,
			// e.g. "1920 x 1080".
			"x",
			// Names of third party services and of a control that never paints
			// its Text property.
			"Bing",
			"flickr",
			"Unsplash",
			"Url",
			"imageBlender"
		};

		/// <summary>
		/// Gets the repository root, found by walking up from the test output folder.
		/// </summary>
		static string RepoRoot
		{
			get
			{
				// bin\Release -> bin -> DMT.UnitTests -> DMT -> repo root
				return Path.GetFullPath(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
			}
		}

		static string DmtResourcesDir
		{
			get { return Path.Combine(RepoRoot, @"DMT\DMT\Resources"); }
		}

		static string NeutralPath(string resourceClass)
		{
			return Path.Combine(DmtResourcesDir, resourceClass + ".resx");
		}

		static string SatellitePath(string resourceClass)
		{
			return Path.Combine(DmtResourcesDir, resourceClass + ".zh-Hans.resx");
		}

		/// <summary>
		/// Reads a resx with an XML parser. Text matching is not usable here because
		/// the resx header comment demonstrates four sample data entries.
		/// </summary>
		static Dictionary<string, string> ReadStrings(string path)
		{
			Assert.IsTrue(File.Exists(path), "missing resource file: " + path);

			XmlDocument doc = new XmlDocument();
			doc.Load(path);

			Dictionary<string, string> result = new Dictionary<string, string>();
			XmlNodeList nodes = doc.SelectNodes("/root/data");
			if (nodes == null)
			{
				return result;
			}

			foreach (XmlNode node in nodes)
			{
				XmlAttribute nameAttribute = node.Attributes["name"];
				if (nameAttribute == null)
				{
					continue;
				}

				XmlNode valueNode = node.SelectSingleNode("value");
				string value = valueNode == null ? string.Empty : valueNode.InnerText;
				result[nameAttribute.Value] = value;
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

		static bool IsUntranslatable(string english)
		{
			if (english == null)
			{
				return true;
			}

			string trimmed = english.Trim();
			if (trimmed.Length == 0)
			{
				return true;
			}

			foreach (string allowed in UntranslatableValues)
			{
				if (string.Equals(trimmed, allowed, StringComparison.Ordinal))
				{
					return true;
				}
			}

			// File dialog filters keep their "description|pattern|description|pattern"
			// structure; only the description part is translated, so the value as a
			// whole differs. Nothing to whitelist there.

			// Pure format strings with no words, e.g. "{0}", need no translation.
			if (Regex.IsMatch(trimmed, @"^[\{\}\d\s\.,:/\-]+$"))
			{
				return true;
			}

			return false;
		}

		// A short word with no space is still a word. Without this test the guard
		// above could be loosened back into a length heuristic that silently waves
		// through a forgotten "Cancel" or "Apply".
		[TestCase("Cancel")]
		[TestCase("Apply")]
		[TestCase("About")]
		[TestCase("Options")]
		[TestCase("Rescan")]
		public void IsUntranslatable_DoesNotExemptShortRealWords(string english)
		{
			Assert.IsFalse(IsUntranslatable(english),
				"'" + english + "' is a real word and must be translated");
		}

		[TestCase("Alt")]
		[TestCase("Ctrl")]
		[TestCase("X1")]
		[TestCase("Bing")]
		[TestCase("flickr")]
		[TestCase("0°")]
		[TestCase("20%")]
		[TestCase("+")]
		[TestCase("{0}")]
		[TestCase("DMT_GUI_WINDOW")]
		public void IsUntranslatable_ExemptsKnownTechnicalTokens(string english)
		{
			if (string.Equals(english, "DMT_GUI_WINDOW", StringComparison.Ordinal))
			{
				// Not on the list on purpose: the whitelist must name what it means.
				Assert.IsFalse(IsUntranslatable(english));
				return;
			}

			Assert.IsTrue(IsUntranslatable(english),
				"'" + english + "' is a technical token and must stay as it is");
		}

		[Test]
		public void RepoRootIsResolvable()
		{
			Assert.IsTrue(Directory.Exists(DmtResourcesDir),
				"resource folder not found at " + DmtResourcesDir);
			Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "AGENTS.md")),
				"AGENTS.md not found at " + RepoRoot);
		}

		[Test]
		public void EveryResourceClassHasASatelliteFile(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			Assert.IsTrue(File.Exists(NeutralPath(resourceClass)),
				"missing neutral resx: " + NeutralPath(resourceClass));
			Assert.IsTrue(File.Exists(SatellitePath(resourceClass)),
				"missing zh-Hans resx: " + SatellitePath(resourceClass));
		}

		// Every neutral key must have a Chinese entry, and the satellite must not
		// carry keys the neutral file does not have. A missing translation is silent
		// at runtime because the CLR falls back to English, so this test is what
		// makes the gap visible.
		[Test]
		public void CoveragePerResourceClass(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			Dictionary<string, string> neutral = ReadStrings(NeutralPath(resourceClass));
			Dictionary<string, string> chinese = ReadStrings(SatellitePath(resourceClass));

			List<string> missing = new List<string>();
			foreach (string key in neutral.Keys)
			{
				if (!chinese.ContainsKey(key))
				{
					missing.Add(key);
				}
			}

			List<string> extra = new List<string>();
			foreach (string key in chinese.Keys)
			{
				if (!neutral.ContainsKey(key))
				{
					extra.Add(key);
				}
			}

			Assert.IsEmpty(missing,
				resourceClass + ": keys with no zh-Hans entry: " + string.Join(", ", missing.ToArray()));
			Assert.IsEmpty(extra,
				resourceClass + ": zh-Hans keys absent from neutral file: " + string.Join(", ", extra.ToArray()));
		}

		// A dropped or doubled {0} throws FormatException at run time, in a dialog
		// the user only reaches later.
		[Test]
		public void PlaceholderParity(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			Dictionary<string, string> neutral = ReadStrings(NeutralPath(resourceClass));
			Dictionary<string, string> chinese = ReadStrings(SatellitePath(resourceClass));

			List<string> mismatches = new List<string>();
			foreach (KeyValuePair<string, string> pair in neutral)
			{
				string translated;
				if (!chinese.TryGetValue(pair.Key, out translated))
				{
					continue;	// reported by CoveragePerResourceClass
				}

				string englishSignature = PlaceholderSignature(pair.Value);
				string chineseSignature = PlaceholderSignature(translated);
				if (!string.Equals(englishSignature, chineseSignature, StringComparison.Ordinal))
				{
					mismatches.Add(string.Format(
						"{0}: english has [{1}] but chinese has [{2}]",
						pair.Key, englishSignature, chineseSignature));
				}
			}

			Assert.IsEmpty(mismatches,
				resourceClass + ": placeholder mismatch: " + string.Join("; ", mismatches.ToArray()));
		}

		[Test]
		public void TranslationsAreNotNull(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			Dictionary<string, string> neutral = ReadStrings(NeutralPath(resourceClass));
			Dictionary<string, string> chinese = ReadStrings(SatellitePath(resourceClass));

			List<string> blanks = new List<string>();
			foreach (KeyValuePair<string, string> pair in neutral)
			{
				string translated;
				if (!chinese.TryGetValue(pair.Key, out translated))
				{
					continue;
				}

				// An English empty string means "this slot is unused"; Chinese must
				// be empty too rather than carrying stray text.
				bool englishBlank = pair.Value.Trim().Length == 0;
				bool chineseBlank = translated.Trim().Length == 0;
				if (englishBlank != chineseBlank)
				{
					blanks.Add(pair.Key);
				}
			}

			Assert.IsEmpty(blanks,
				resourceClass + ": blank/unblank mismatch: " + string.Join(", ", blanks.ToArray()));
		}

		[Test]
		public void TranslationsActuallyDifferFromEnglish(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			Dictionary<string, string> neutral = ReadStrings(NeutralPath(resourceClass));
			Dictionary<string, string> chinese = ReadStrings(SatellitePath(resourceClass));

			List<string> untranslated = new List<string>();
			foreach (KeyValuePair<string, string> pair in neutral)
			{
				string translated;
				if (!chinese.TryGetValue(pair.Key, out translated))
				{
					continue;
				}

				if (IsUntranslatable(pair.Value))
				{
					continue;
				}

				if (string.Equals(pair.Value, translated, StringComparison.Ordinal))
				{
					untranslated.Add(pair.Key);
				}
			}

			Assert.IsEmpty(untranslated,
				resourceClass + ": keys whose zh-Hans value is still the English text: " +
				string.Join(", ", untranslated.ToArray()));
		}

		[Test]
		public void ZhHansResxIsUtf8WithBom(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			byte[] head = ReadFirstBytes(SatellitePath(resourceClass), 3);

			Assert.AreEqual(0xEF, head[0], resourceClass + ": missing UTF-8 BOM byte 1");
			Assert.AreEqual(0xBB, head[1], resourceClass + ": missing UTF-8 BOM byte 2");
			Assert.AreEqual(0xBF, head[2], resourceClass + ": missing UTF-8 BOM byte 3");
		}

		[Test]
		public void ZhHansResxDeclaresUtf8(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			string text = File.ReadAllText(SatellitePath(resourceClass), Encoding.UTF8);

			StringAssert.Contains("encoding=\"utf-8\"", text,
				resourceClass + ": resx does not declare utf-8");
		}

		[Test]
		public void ZhHansResxContainsCjkCharacters(
			[ValueSource("DmtResourceClasses")] string resourceClass)
		{
			Dictionary<string, string> chinese = ReadStrings(SatellitePath(resourceClass));

			bool anyCjk = false;
			foreach (string value in chinese.Values)
			{
				if (Regex.IsMatch(value, @"[\u4e00-\u9fff]"))
				{
					anyCjk = true;
					break;
				}
			}

			Assert.IsTrue(anyCjk,
				resourceClass + ": no CJK characters found, the file is probably still English");
		}

		static byte[] ReadFirstBytes(string path, int count)
		{
			Assert.IsTrue(File.Exists(path), "missing file: " + path);

			byte[] buffer = new byte[count];
			using (FileStream stream = File.OpenRead(path))
			{
				int read = stream.Read(buffer, 0, count);
				Assert.AreEqual(count, read, "file too short: " + path);
			}

			return buffer;
		}
	}
}
