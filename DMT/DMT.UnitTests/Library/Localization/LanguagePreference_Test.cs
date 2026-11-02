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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using NUnit.Framework;

using DMT.Library.Environment;
using DMT.Library.Localization;
using DMT.Modules.General;

namespace DMT.UnitTests.Library.Localization
{
	/// <summary>
	/// Guards the language preference round-trip through the registry and the
	/// behaviour of the language selection panel.
	/// </summary>
	[TestFixture]
	public class LanguagePreference_Test
	{
		// The tests below touch HKCU\SOFTWARE\Dual Monitor Tools\DMT\Language.
		// The original state is saved and restored so the machine is left as found,
		// including removing the key itself when it did not exist beforehand.
		string _savedLanguage;
		bool _keyExistedBefore;

		[SetUp]
		public void SavePreference()
		{
			_keyExistedBefore = DmtRegistry.DmtKeyExists();
			_savedLanguage = DmtRegistry.GetLanguage();
		}

		[TearDown]
		public void RestorePreference()
		{
			if (_savedLanguage == null)
			{
				DmtRegistry.DeleteLanguage();
			}
			else
			{
				DmtRegistry.SetLanguage(_savedLanguage);
			}

			// Creating the preference creates the key. If the key was not there
			// before the test, remove it again so the machine is unchanged.
			if (!_keyExistedBefore)
			{
				DmtRegistry.DeleteDmtKeyIfEmpty();
			}
		}

		[Test]
		public void SetLanguage_ThenGetLanguage_ReturnsSavedValue()
		{
			DmtRegistry.SetLanguage("zh-Hans");

			Assert.AreEqual("zh-Hans", DmtRegistry.GetLanguage());
		}

		[Test]
		public void DeleteLanguage_ThenGetLanguage_ReturnsNull()
		{
			DmtRegistry.SetLanguage("zh-Hans");

			DmtRegistry.DeleteLanguage();

			Assert.IsNull(DmtRegistry.GetLanguage());
		}

		[Test]
		public void ResolveFromRegistry_WhenAbsent_ReturnsEnglish()
		{
			DmtRegistry.DeleteLanguage();

			Assert.AreEqual("en", LanguageCatalog.ResolveFromRegistry());
		}

		[Test]
		public void ResolveFromRegistry_WhenSavedChinese_ReturnsChinese()
		{
			DmtRegistry.SetLanguage("zh-Hans");

			Assert.AreEqual("zh-Hans", LanguageCatalog.ResolveFromRegistry());
		}

		[Test]
		public void ResolveFromRegistry_WhenSavedNonsense_ReturnsEnglish()
		{
			DmtRegistry.SetLanguage("not-a-language");

			Assert.AreEqual("en", LanguageCatalog.ResolveFromRegistry());
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void Panel_ListsEveryLanguageInDisplayOrder()
		{
			using (LanguageOptionsPanel panel = new LanguageOptionsPanel())
			{
				Control[] found = panel.Controls.Find("listBoxLanguages", true);
				Assert.AreEqual(1, found.Length, "listBoxLanguages not found on the panel");

				ListBox list = (ListBox)found[0];
				Assert.AreEqual(LanguageCatalog.All.Count, list.Items.Count);

				for (int i = 0; i < LanguageCatalog.All.Count; i++)
				{
					Assert.AreEqual(LanguageCatalog.All[i].DisplayName, list.Items[i].ToString());
				}
			}
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void Panel_SelectsTheSavedLanguageInitially()
		{
			DmtRegistry.SetLanguage("zh-Hans");

			using (LanguageOptionsPanel panel = new LanguageOptionsPanel())
			{
				ListBox list = (ListBox)panel.Controls.Find("listBoxLanguages", true)[0];

				Assert.AreEqual(1, list.SelectedIndex, "the saved language was not pre-selected");
			}
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void Panel_ChoosingADifferentLanguage_SavesItAndAsksForRestart()
		{
			DmtRegistry.SetLanguage("en");

			using (LanguageOptionsPanel panel = new LanguageOptionsPanel())
			{
				ListBox list = (ListBox)panel.Controls.Find("listBoxLanguages", true)[0];

				list.SelectedIndex = 1;		// 简体中文

				Assert.AreEqual("zh-Hans", DmtRegistry.GetLanguage());
				Assert.IsTrue(panel.RestartHintNeeded,
					"the restart hint should appear after choosing a different language");
			}
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void Panel_RechoosingTheSameLanguage_DoesNotAskForRestart()
		{
			DmtRegistry.SetLanguage("en");

			using (LanguageOptionsPanel panel = new LanguageOptionsPanel())
			{
				ListBox list = (ListBox)panel.Controls.Find("listBoxLanguages", true)[0];

				list.SelectedIndex = 1;		// 简体中文
				list.SelectedIndex = 0;		// back to English

				Assert.AreEqual("en", DmtRegistry.GetLanguage());
				Assert.IsFalse(panel.RestartHintNeeded,
					"the restart hint should clear once the choice matches the running language");
			}
		}
	}
}
