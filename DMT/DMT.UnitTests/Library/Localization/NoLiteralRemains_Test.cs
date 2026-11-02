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

using NUnit.Framework;

namespace DMT.UnitTests.Library.Localization
{
	/// <summary>
	/// Guards that no user-visible text is left as a literal in any designer, so a
	/// window cannot ship with one caption stuck in English.
	/// </summary>
	[TestFixture]
	public class NoLiteralRemains_Test
	{
		// Literals that may legitimately stay in a designer file.
		//   DMT_GUI_WINDOW - the single-instance handshake looked up by FindWindow
		//   label<N>       - design-time placeholders overwritten in the constructor
		//   "-"            - menu separator, not text
		static readonly string[] AllowedLiteralPatterns = new string[]
		{
			@"^DMT_GUI_WINDOW$",
			@"^label\d+$",
			@"^label[A-Za-z]+$",
			@"^-$"
		};

		static string RepoRoot
		{
			get
			{
				return Path.GetFullPath(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
			}
		}

		static bool IsAllowed(string value)
		{
			foreach (string pattern in AllowedLiteralPatterns)
			{
				if (Regex.IsMatch(value, pattern))
				{
					return true;
				}
			}

			return false;
		}

		static List<string> FindLiterals(string projectRelativePath)
		{
			string root = Path.Combine(RepoRoot, projectRelativePath);
			Assert.IsTrue(Directory.Exists(root), "project folder not found: " + root);

			return ScanDirectory(root);
		}

		/// <summary>
		/// Scans every *.Designer.cs under a folder and reports user-visible literals.
		/// Separated from the repo lookup so the scanner itself can be tested against
		/// a throwaway folder.
		/// </summary>
		static List<string> ScanDirectory(string root)
		{
			List<string> problems = new List<string>();

			string[] designerFiles = Directory.GetFiles(root, "*.Designer.cs", SearchOption.AllDirectories);
			foreach (string file in designerFiles)
			{
				// Resources\ and Properties\ hold the strongly-typed resource classes
				// themselves; they are not windows.
				if (Regex.IsMatch(file, @"\\(Resources|Properties)\\[^\\]+$"))
				{
					continue;
				}

				string[] lines = File.ReadAllLines(file, Encoding.UTF8);
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
					{
						continue;
					}

					string literal = ExtractAssignedLiteral(line, @"\.Text\s*=\s*");
					if (literal == null)
					{
						literal = ExtractAssignedLiteral(line, @"\.HeaderText\s*=\s*");
					}

					if (literal == null)
					{
						Match toolTip = Regex.Match(line, @"SetToolTip\([^,]+,\s*""(?<v>[^""]*)""");
						if (toolTip.Success)
						{
							literal = toolTip.Groups["v"].Value;
						}
					}

					if (literal != null && !IsAllowed(literal))
					{
						problems.Add(string.Format(
							"{0}:{1}: {2}",
							Path.GetFileName(file), i + 1, line.Trim()));
					}
				}
			}

			return problems;
		}

		// Text that reaches the screen from ordinary code, not from a designer.
		// Covers the three shapes that actually occur: a data grid row header, a
		// caption passed into a dialog constructor, and a provider's default
		// description in a config class. None of these is a string literal after
		// ".Text =", which is why the designer scan above cannot see them.
		static readonly string[] CodeLiteralPatterns = new string[]
		{
			@"HeaderCell\.Value\s*=\s*""(?<v>[^""]*)""",
			@"\bnew\s+EditNameListForm\(\s*""(?<v>[^""]*)""",
			@"string\s+caption\s*=\s*""(?<v>[^""]*)""",
			@"string\s+listText\s*=\s*""(?<v>[^""]*)""",
			@"""description""\s*,\s*""(?<v>[^""]*)"""
		};

		/// <summary>
		/// Scans ordinary source files, skipping designers because ScanDirectory
		/// already covered them.
		/// </summary>
		static List<string> ScanCodeFiles(string root)
		{
			List<string> problems = new List<string>();

			string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
			foreach (string file in files)
			{
				string name = Path.GetFileName(file);
				if (name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				// The resource classes and their generated accessors are not UI code.
				if (Regex.IsMatch(file, @"\\(Resources|Properties)\\[^\\]+$"))
				{
					continue;
				}

				string[] lines = File.ReadAllLines(file, Encoding.UTF8);
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
					{
						continue;
					}

					foreach (string pattern in CodeLiteralPatterns)
					{
						Match match = Regex.Match(line, pattern);
						if (!match.Success)
						{
							continue;
						}

						string literal = match.Groups["v"].Value;
						if (!IsAllowed(literal))
						{
							problems.Add(string.Format(
								"{0}:{1}: {2}",
								name, i + 1, line.Trim()));
						}
					}
				}
			}

			return problems;
		}

		static string ExtractAssignedLiteral(string line, string assignmentPattern)
		{
			Match match = Regex.Match(line, assignmentPattern + @"""(?<v>[^""]*)""");
			if (!match.Success)
			{
				return null;
			}

			return match.Groups["v"].Value;
		}

		[TestCase(@"DMT\DMT")]
		[TestCase(@"DualWallpaper\DualWallpaper")]
		[TestCase(@"DmtWallpaper\DmtWallpaper")]
		public void NoUserVisibleLiteralRemainsInDesigners(string projectRelativePath)
		{
			List<string> problems = FindLiterals(projectRelativePath);

			Assert.IsEmpty(problems,
				projectRelativePath + " still has user-visible designer literals:" +
				Environment.NewLine + string.Join(Environment.NewLine, problems.ToArray()));
		}

		// Text that reaches the screen from ordinary code is just as visible as text
		// set in a designer, but the designer scan cannot see it. This covers data
		// grid row headers, dialog captions and provider default descriptions.
		[TestCase(@"DMT\DMT")]
		public void NoUserVisibleLiteralRemainsInCode(string projectRelativePath)
		{
			string root = Path.Combine(RepoRoot, projectRelativePath);
			Assert.IsTrue(Directory.Exists(root), "project folder not found: " + root);

			List<string> problems = ScanCodeFiles(root);

			Assert.IsEmpty(problems,
				projectRelativePath + " still has user-visible code literals:" +
				Environment.NewLine + string.Join(Environment.NewLine, problems.ToArray()));
		}

		// The code scan must be able to see things too, in each of its three shapes.
		[Test]
		public void ScanCodeDetectsKnownLiterals()
		{
			string temp = Path.Combine(Path.GetTempPath(), "dmt_code_scan_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(temp);
			try
			{
				File.WriteAllText(
					Path.Combine(temp, "HasGrid.cs"),
					"class A { void M() { grid.Rows[0].HeaderCell.Value = \"SHOULD BE FOUND\"; } }" + Environment.NewLine,
					Encoding.UTF8);

				File.WriteAllText(
					Path.Combine(temp, "HasCaption.cs"),
					"class B { void M() { string caption = \"ALSO FOUND\"; } }" + Environment.NewLine,
					Encoding.UTF8);

				File.WriteAllText(
					Path.Combine(temp, "HasDescription.cs"),
					"class C { void M() { X = H(config, \"description\", \"FOUND TOO\"); } }" + Environment.NewLine,
					Encoding.UTF8);

				File.WriteAllText(
					Path.Combine(temp, "IsClean.cs"),
					"class D { void M() { X = H(config, \"description\", Strings.SomeDescription); } }" + Environment.NewLine,
					Encoding.UTF8);

				List<string> problems = ScanCodeFiles(temp);

				Assert.AreEqual(3, problems.Count,
					"expected three problems, got: " + string.Join(" | ", problems.ToArray()));
				StringAssert.Contains("SHOULD BE FOUND", string.Join("|", problems.ToArray()));
				StringAssert.Contains("ALSO FOUND", string.Join("|", problems.ToArray()));
				StringAssert.Contains("FOUND TOO", string.Join("|", problems.ToArray()));
			}
			finally
			{
				Directory.Delete(temp, true);
			}
		}

		// The scan itself must be able to see things, otherwise a green result above
		// means nothing. This builds a throwaway folder holding one designer with a
		// literal and one that references a resource, then checks the scanner reports
		// exactly the first.
		[Test]
		public void ScanDetectsAKnownLiteral()
		{
			string temp = Path.Combine(Path.GetTempPath(), "dmt_literal_scan_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(temp);
			try
			{
				File.WriteAllText(
					Path.Combine(temp, "HasLiteral.Designer.cs"),
					"class A { void M() { this.button.Text = \"SHOULD BE FOUND\"; } }" + Environment.NewLine,
					Encoding.UTF8);

				File.WriteAllText(
					Path.Combine(temp, "IsClean.Designer.cs"),
					"class B { void M() { this.button.Text = global::DMT.Resources.AppStrings.Foo; } }" + Environment.NewLine,
					Encoding.UTF8);

				List<string> problems = ScanDirectory(temp);

				Assert.AreEqual(1, problems.Count,
					"expected exactly one problem, got: " + string.Join(" | ", problems.ToArray()));
				StringAssert.Contains("SHOULD BE FOUND", problems[0]);
				StringAssert.Contains("HasLiteral.Designer.cs", problems[0]);
			}
			finally
			{
				Directory.Delete(temp, true);
			}
		}

		// An allowed literal must not be reported, or the guard would block every build.
		[Test]
		public void ScanAllowsWhitelistedLiterals()
		{
			string temp = Path.Combine(Path.GetTempPath(), "dmt_literal_allow_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(temp);
			try
			{
				File.WriteAllText(
					Path.Combine(temp, "Allowed.Designer.cs"),
					"class C { void M() {" + Environment.NewLine +
					"  this.Text = \"DMT_GUI_WINDOW\";" + Environment.NewLine +
					"  this.label1.Text = \"label7\";" + Environment.NewLine +
					"  this.separator.Text = \"-\";" + Environment.NewLine +
					"} }" + Environment.NewLine,
					Encoding.UTF8);

				Assert.IsEmpty(ScanDirectory(temp));
			}
			finally
			{
				Directory.Delete(temp, true);
			}
		}
	}
}
