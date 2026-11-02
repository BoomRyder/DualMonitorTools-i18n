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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using DMT.Library.Localization;

namespace DMT.UnitTests.Library.Localization
{
	/// <summary>
	/// Guards that applying a language actually reaches every resource class in the
	/// application assembly, and that an English selection is not overridden by a
	/// Chinese operating system.
	/// </summary>
	[TestFixture]
	public class UiCulture_Test
	{
		// Every strongly-typed resource class DMT.exe ships.
		static readonly string[] ResourceClassNames = new string[]
		{
			"DMT.Properties.Resources",
			"DMT.Resources.AppStrings",
			"DMT.Resources.CommonStrings",
			"DMT.Resources.CursorStrings",
			"DMT.Resources.GeneralStrings",
			"DMT.Resources.LauncherStrings",
			"DMT.Resources.SnapStrings",
			"DMT.Resources.SwapScreenStrings",
			"DMT.Resources.WallpaperStrings"
		};

		/// <summary>
		/// The DMT application assembly, which holds the resource classes under test.
		/// </summary>
		static Assembly ApplicationAssembly
		{
			get { return typeof(UiCulture).Assembly; }
		}

		static CultureInfo ReadCulture(string typeFullName)
		{
			Type type = ApplicationAssembly.GetType(typeFullName, false);
			Assert.IsNotNull(type, "resource class not found: " + typeFullName);

			PropertyInfo property = type.GetProperty(
				"Culture",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(property, "no static Culture property on " + typeFullName);

			return (CultureInfo)property.GetValue(null, null);
		}

		[SetUp]
		public void ResetToEnglish()
		{
			UiCulture.Apply("en");
		}

		[TearDown]
		public void RestoreEnglish()
		{
			// Leave the process in the default language for subsequent fixtures.
			UiCulture.Apply("en");
		}

		// An English selection must win even when the operating system culture is
		// Chinese. Without an explicit set, ResourceManager would fall back to the
		// thread culture and the user would get Chinese despite choosing English.
		[Test]
		public void Apply_EnglishWinsOverSystemChineseCulture()
		{
			CultureInfo saved = Thread.CurrentThread.CurrentUICulture;
			try
			{
				Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");

				UiCulture.Apply("en");

				Assert.AreEqual("en", Thread.CurrentThread.CurrentUICulture.Name);
				Assert.AreEqual("en", ReadCulture("DMT.Resources.AppStrings").Name);
			}
			finally
			{
				Thread.CurrentThread.CurrentUICulture = saved;
			}
		}

		[Test]
		public void Apply_SetsCultureOnEveryResourceClass()
		{
			UiCulture.Apply("zh-Hans");

			foreach (string typeFullName in ResourceClassNames)
			{
				CultureInfo culture = ReadCulture(typeFullName);
				Assert.IsNotNull(culture, typeFullName + ": Culture was null after Apply");
				Assert.AreEqual("zh-Hans", culture.Name, typeFullName + ": culture not applied");
			}
		}

		[Test]
		public void Apply_UnknownTagFallsBackToEnglish()
		{
			UiCulture.Apply("kl-GL");

			Assert.AreEqual("en", ReadCulture("DMT.Resources.AppStrings").Name);
		}

		[Test]
		public void Apply_NullTagFallsBackToEnglish()
		{
			UiCulture.Apply(null);

			Assert.AreEqual("en", ReadCulture("DMT.Resources.AppStrings").Name);
		}

		// Applying a language must actually change what the strongly-typed
		// properties return, not just the Culture field.
		[Test]
		public void Apply_ChangesStringsReturnedByResourceClass()
		{
			UiCulture.Apply("en");
			string english = ReadString("DMT.Resources.CommonStrings", "MyDescription");

			UiCulture.Apply("zh-Hans");
			string chinese = ReadString("DMT.Resources.CommonStrings", "MyDescription");

			Assert.AreNotEqual(english, chinese,
				"CommonStrings.MyDescription returned the same text for en and zh-Hans");
		}

		/// <summary>
		/// Reads a strongly-typed resource property through reflection, because the
		/// resource classes are internal to the application assembly.
		/// </summary>
		static string ReadString(string typeFullName, string propertyName)
		{
			Type type = ApplicationAssembly.GetType(typeFullName, false);
			Assert.IsNotNull(type, "resource class not found: " + typeFullName);

			PropertyInfo property = type.GetProperty(
				propertyName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(property, "no property " + propertyName + " on " + typeFullName);

			return (string)property.GetValue(null, null);
		}

		[Test]
		public void Apply_IsIdempotent()
		{
			UiCulture.Apply("zh-Hans");
			CultureInfo first = ReadCulture("DMT.Resources.AppStrings");

			UiCulture.Apply("zh-Hans");
			CultureInfo second = ReadCulture("DMT.Resources.AppStrings");

			Assert.AreEqual(first.Name, second.Name);
		}
	}
}
