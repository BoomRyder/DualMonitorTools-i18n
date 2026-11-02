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

namespace DMT.Modules.General
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using DMT.Library.Environment;
	using DMT.Library.Localization;

	using DMT.Resources;

	/// <summary>
	/// Options panel that lets the user choose the language used for the
	/// user interface.
	/// </summary>
	/// <remarks>
	/// The choice is saved immediately, but only takes effect the next time the
	/// program starts: every window builds its text from resources when it is
	/// created, so switching on the fly would leave a half translated window.
	/// </remarks>
	partial class LanguageOptionsPanel : UserControl
	{
		// Language that was in force when this panel was created.
		// Used to decide whether a restart is still needed.
		string _startupLanguageTag;

		// Prevents the selection handler from firing while the list is populated.
		bool _initialising;

		/// <summary>
		/// Initialises a new instance of the <see cref="LanguageOptionsPanel" /> class.
		/// </summary>
		public LanguageOptionsPanel()
		{
			_initialising = true;

			InitializeComponent();

			_startupLanguageTag = LanguageCatalog.ResolveFromRegistry();

			listBoxLanguages.BeginUpdate();
			foreach (LanguageDefinition language in LanguageCatalog.All)
			{
				listBoxLanguages.Items.Add(language.DisplayName);
			}

			listBoxLanguages.EndUpdate();

			listBoxLanguages.SelectedIndex = IndexOfLanguage(_startupLanguageTag);

			UpdateRestartHint();

			_initialising = false;
		}

		/// <summary>
		/// Gets a value indicating whether the user has chosen a language that
		/// differs from the one this process started with.
		/// </summary>
		public bool RestartHintNeeded
		{
			get { return labelRestartNeeded.Visible; }
		}

		/// <summary>
		/// Gets the language tag that this process is currently running with.
		/// </summary>
		public string StartupLanguageTag
		{
			get { return _startupLanguageTag; }
		}

		static int IndexOfLanguage(string languageTag)
		{
			string wanted = LanguageCatalog.Normalise(languageTag);

			for (int i = 0; i < LanguageCatalog.All.Count; i++)
			{
				if (string.Equals(LanguageCatalog.All[i].Tag, wanted, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return 0;
		}

		void UpdateRestartHint()
		{
			string selectedTag = SelectedLanguageTag();

			labelRestartNeeded.Visible = !string.Equals(
				selectedTag, _startupLanguageTag, StringComparison.OrdinalIgnoreCase);
		}

		string SelectedLanguageTag()
		{
			int index = listBoxLanguages.SelectedIndex;
			if (index < 0 || index >= LanguageCatalog.All.Count)
			{
				return LanguageCatalog.Default.Tag;
			}

			return LanguageCatalog.All[index].Tag;
		}

		private void listBoxLanguages_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_initialising)
			{
				return;
			}

			DmtRegistry.SetLanguage(SelectedLanguageTag());

			UpdateRestartHint();
		}
	}
}
