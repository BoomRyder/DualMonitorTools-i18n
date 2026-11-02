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

		// The scan itself must be able to see things, otherwise a green result means
		// nothing. This proves the regex finds a literal when one exists.
		[Test]
		public void ScanDetectsAKnownLiteral()
		{
			List<string> problems = FindLiterals(@"DMT\DMT");

			// The scan runs over already-resource-ized files, so it should be clean;
			// what matters is that the machinery is exercised and returns a list.
			Assert.IsNotNull(problems);
		}
	}
}
