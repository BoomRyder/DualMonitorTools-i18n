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
	/// Guards that user-visible text in the DMT windows comes from resources, and
	/// that the one string that must never be translated stays as it is.
	/// </summary>
	[TestFixture]
	public class DesignerLiteral_Test
	{
		static string RepoRoot
		{
			get
			{
				return Path.GetFullPath(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
			}
		}

		static string ReadSource(string relativePath)
		{
			string path = Path.Combine(RepoRoot, relativePath);
			Assert.IsTrue(File.Exists(path), "missing source file: " + path);
			return File.ReadAllText(path);
		}

		// The window title is the single-instance handshake: CommandMessaging looks
		// for a window with exactly this caption. Translating it would let a second
		// copy of DMT start alongside the first, both owning the tray icon.
		[Test]
		public void AppFormWindowTitleStaysUntranslated()
		{
			string source = ReadSource(@"DMT\DMT\AppForm.Designer.cs");

			StringAssert.Contains(@"this.Text = ""DMT_GUI_WINDOW"";", source,
				"the single-instance window title must keep its literal value");
		}

		// Same guarantee at the point of use, so a future edit that changes the
		// designer literal is caught alongside the lookup.
		[Test]
		public void SingleInstanceLookupUsesTheSameLiteral()
		{
			string messaging = ReadSource(@"DMT\DMT\Library\Command\CommandMessaging.cs");

			StringAssert.Contains("DMT_GUI_WINDOW", messaging,
				"the single-instance lookup no longer references DMT_GUI_WINDOW");
		}

		// The tray menu and the two dialogs must take their captions from resources.
		[TestCase(@"DMT\DMT\AppForm.Designer.cs", "optionsToolStripMenuItem")]
		[TestCase(@"DMT\DMT\OptionsForm.Designer.cs", "buttonClose")]
		[TestCase(@"DMT\DMT\AboutForm.Designer.cs", "")]
		public void MainWindowTextComesFromResources(string relativePath, string controlName)
		{
			string source = ReadSource(relativePath);

			if (controlName.Length > 0)
			{
				Regex literal = new Regex(
					@"this\." + Regex.Escape(controlName) + @"\.Text\s*=\s*""");
				Assert.IsFalse(literal.IsMatch(source),
					relativePath + ": " + controlName + ".Text is still a literal");
			}

			StringAssert.Contains("AppStrings.", source,
				relativePath + " does not reference AppStrings");
		}

		// AboutForm composes two captions at run time; the format strings must be
		// resources too, and must keep their placeholders.
		[Test]
		public void AboutFormFormatStringsAreResources()
		{
			string source = ReadSource(@"DMT\DMT\AboutForm.cs");

			Assert.IsFalse(
				Regex.IsMatch(source, @"string\.Format\(\s*"""),
				"AboutForm.cs still formats a literal string");
		}

		// AppForm adds four tray menu items at run time.
		[Test]
		public void TrayMenuItemsComeFromResources()
		{
			string source = ReadSource(@"DMT\DMT\AppForm.cs");

			foreach (string literal in new string[] { "About", "Visit Website", "Exit" })
			{
				Regex bad = new Regex(@"AddMenuItem\(\s*""" + Regex.Escape(literal) + @"""");
				Assert.IsFalse(bad.IsMatch(source),
					"AppForm still adds the tray item \"" + literal + "\" as a literal");
			}

			// The separator is not text and stays a literal.
			StringAssert.Contains(@"AddMenuItem(""-""", source,
				"the menu separator should remain a literal");
		}
	}
}
