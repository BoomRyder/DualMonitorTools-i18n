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

namespace DMT.Library.Localization
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Linq;
	using System.Text;

	using DMT.Library.Environment;

	/// <summary>
	/// One selectable language for the user interface.
	/// </summary>
	public class LanguageDefinition
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="LanguageDefinition" /> class.
		/// </summary>
		/// <param name="tag">Culture name as stored in the registry, e.g. "en" or "zh-Hans"</param>
		/// <param name="displayName">Name shown in the UI, written in the language's own script</param>
		public LanguageDefinition(string tag, string displayName)
		{
			Tag = tag;
			DisplayName = displayName;
		}

		/// <summary>
		/// Gets the culture name used to select satellite resource assemblies.
		/// </summary>
		public string Tag { get; private set; }

		/// <summary>
		/// Gets the name to display in the UI.
		/// This is written in the language's own script so it does not need translating.
		/// </summary>
		public string DisplayName { get; private set; }
	}

	/// <summary>
	/// The set of languages this build is able to display.
	/// To add a language, add its satellite .resx files and one entry to the
	/// list built in the static constructor below. No other code needs changing.
	/// </summary>
	public static class LanguageCatalog
	{
		// Culture name of the language used when no valid preference is stored.
		const string DefaultTag = "en";

		static readonly List<LanguageDefinition> _all;
		static readonly ReadOnlyCollection<LanguageDefinition> _allView;

		static LanguageCatalog()
		{
			_all = new List<LanguageDefinition>();
			_all.Add(new LanguageDefinition("en", "English"));
			_all.Add(new LanguageDefinition("zh-Hans", "简体中文"));
			_allView = new ReadOnlyCollection<LanguageDefinition>(_all);
		}

		/// <summary>
		/// Gets all supported languages, default language first.
		/// </summary>
		public static ReadOnlyCollection<LanguageDefinition> All
		{
			get { return _allView; }
		}

		/// <summary>
		/// Gets the language used when no valid preference is stored.
		/// </summary>
		public static LanguageDefinition Default
		{
			get { return _all[0]; }
		}

		/// <summary>
		/// Finds a supported language by its tag.
		/// </summary>
		/// <param name="tag">Culture name to look for, may be null</param>
		/// <returns>The matching language, or <see cref="Default" /> if not supported</returns>
		public static LanguageDefinition Find(string tag)
		{
			foreach (LanguageDefinition language in _all)
			{
				if (string.Equals(language.Tag, tag, StringComparison.OrdinalIgnoreCase))
				{
					return language;
				}
			}

			return Default;
		}

		/// <summary>
		/// Maps a stored preference onto a supported language tag.
		/// Regional Chinese variants map to "zh-Hans", except the traditional
		/// script regions which fall back to the default language.
		/// </summary>
		/// <param name="tagOrNull">Raw preference, may be null, blank or unknown</param>
		/// <returns>A tag that is guaranteed to appear in <see cref="All" /></returns>
		public static string Normalise(string tagOrNull)
		{
			if (tagOrNull == null)
			{
				return DefaultTag;
			}

			string trimmed = tagOrNull.Trim();
			if (trimmed.Length == 0)
			{
				return DefaultTag;
			}

			// Already an exact supported tag?
			foreach (LanguageDefinition language in _all)
			{
				if (string.Equals(language.Tag, trimmed, StringComparison.OrdinalIgnoreCase))
				{
					return language.Tag;
				}
			}

			// Chinese regional variants: use the script the culture actually resolves
			// to, not a bare "zh" prefix. The prefix test would send the legacy
			// traditional name zh-CHT, and any typo such as zhx, to zh-Hans.
			// zh-TW / zh-HK / zh-MO / zh-CHT / zh-Hant are traditional script, which
			// this build does not ship, so they fall through to the default language.
			string script = ResolveChineseScript(trimmed);
			if (script == "zh-Hans")
			{
				return "zh-Hans";
			}

			if (script == "traditional")
			{
				return DefaultTag;
			}

			return DefaultTag;
		}

		/// <summary>
		/// Works out which Chinese script a culture name refers to by walking the
		/// culture's parent chain, where .NET records the script as zh-Hans/zh-CHS
		/// for simplified and zh-Hant/zh-CHT for traditional.
		/// </summary>
		/// <param name="cultureName">Culture name to inspect</param>
		/// <returns>
		/// "zh-Hans" for simplified, "traditional" for traditional, or null when the
		/// name is not Chinese at all.
		/// </returns>
		static string ResolveChineseScript(string cultureName)
		{
			CultureInfo culture;
			try
			{
				culture = new CultureInfo(cultureName);
			}
			catch (ArgumentException)
			{
				return null;
			}

			// A bare "zh" is generic Chinese; this build offers Simplified only.
			bool sawGenericChinese = false;

			while (culture != null)
			{
				string name = culture.Name;

				// The invariant culture reports its own parent as itself, so stop
				// before walking into an endless loop.
				if (name.Length == 0)
				{
					break;
				}

				string lower = name.ToLowerInvariant();

				if (lower == "zh-hant" || lower == "zh-cht")
				{
					return "traditional";
				}

				if (lower == "zh-hans" || lower == "zh-chs")
				{
					return "zh-Hans";
				}

				if (lower == "zh")
				{
					sawGenericChinese = true;
				}

				CultureInfo parent = culture.Parent;
				if (parent == null || ReferenceEquals(parent, culture))
				{
					break;
				}

				culture = parent;
			}

			return sawGenericChinese ? "zh-Hans" : null;
		}

		/// <summary>
		/// Returns the culture to use for a language tag.
		/// Unknown tags resolve to the default language rather than throwing.
		/// </summary>
		/// <param name="tag">Language tag, may be null or unknown</param>
		/// <returns>The culture matching the normalised tag</returns>
		public static CultureInfo CultureFor(string tag)
		{
			return new CultureInfo(Normalise(tag));
		}

		/// <summary>
		/// Reads the saved language preference from the registry.
		/// </summary>
		/// <returns>The normalised preference, or the default tag when absent or invalid</returns>
		public static string ResolveFromRegistry()
		{
			return Normalise(DmtRegistry.GetLanguage());
		}
	}
}
