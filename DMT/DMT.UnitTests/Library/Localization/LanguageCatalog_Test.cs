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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using DMT.Library.Localization;

namespace DMT.UnitTests.Library.Localization
{
	[TestFixture]
	public class LanguageCatalog_Test
	{
		[Test]
		public void All_HasEnglishFirstAsDefault()
		{
			Assert.AreEqual("en", LanguageCatalog.All[0].Tag);
			Assert.AreEqual("en", LanguageCatalog.Default.Tag);
			Assert.AreEqual("English", LanguageCatalog.Default.DisplayName);
		}

		[Test]
		public void All_ContainsSimplifiedChinese()
		{
			LanguageDefinition zh = LanguageCatalog.Find("zh-Hans");

			Assert.IsNotNull(zh);
			Assert.AreEqual("zh-Hans", zh.Tag);
			Assert.AreEqual("简体中文", zh.DisplayName);
		}

		[Test]
		public void Find_UnknownTag_ReturnsDefault()
		{
			Assert.AreEqual("en", LanguageCatalog.Find("kl-GL").Tag);
			Assert.AreEqual("en", LanguageCatalog.Find(null).Tag);
			Assert.AreEqual("en", LanguageCatalog.Find(string.Empty).Tag);
		}

		[Test]
		public void Find_IsCaseInsensitive()
		{
			Assert.AreEqual("zh-Hans", LanguageCatalog.Find("ZH-HANS").Tag);
		}

		// A null, blank or odd preference must land on "en", never on the
		// machine's regional culture.
		[TestCase(null, "en")]
		[TestCase("", "en")]
		[TestCase("   ", "en")]
		[TestCase("en", "en")]
		[TestCase("EN", "en")]
		[TestCase("zh-Hans", "zh-Hans")]
		[TestCase("zh-hans", "zh-Hans")]
		[TestCase("ZH-HANS", "zh-Hans")]
		[TestCase("zh-CN", "zh-Hans")]
		[TestCase("zh-SG", "zh-Hans")]
		[TestCase("zh-TW", "en")]
		[TestCase("zh-HK", "en")]
		[TestCase("zh-MO", "en")]
		[TestCase("zh-CHT", "en")]
		[TestCase("zh-Hant", "en")]
		[TestCase("ZH-HANT", "en")]
		[TestCase("zhx", "en")]
		[TestCase("zh-Hans-CN", "zh-Hans")]
		[TestCase("kl-GL", "en")]
		[TestCase("  zh-Hans  ", "zh-Hans")]
		public void Normalise_MapsPreferenceToSupportedTag(string input, string expected)
		{
			Assert.AreEqual(expected, LanguageCatalog.Normalise(input));
		}

		[Test]
		public void CultureFor_ReturnsRequestedCulture()
		{
			Assert.AreEqual("zh-Hans", LanguageCatalog.CultureFor("zh-Hans").Name);
			Assert.AreEqual("en", LanguageCatalog.CultureFor("en").Name);
			Assert.AreEqual("en", LanguageCatalog.CultureFor("kl-GL").Name);
			Assert.AreEqual("en", LanguageCatalog.CultureFor(null).Name);
		}

		[Test]
		public void All_CanBeEnumeratedWithoutMutation()
		{
			int count = LanguageCatalog.All.Count;

			Assert.GreaterOrEqual(count, 2);
			Assert.AreEqual(count, LanguageCatalog.All.Count);

			// The returned collection must not be castable to a mutable list.
			Assert.IsFalse(LanguageCatalog.All is List<LanguageDefinition>);
		}
	}
}
