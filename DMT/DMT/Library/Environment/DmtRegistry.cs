using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DMT.Library.Environment
{
	public static class DmtRegistry
	{
		static string _lastDmtWallpaperFilename = null;

		const string DmtKey = @"SOFTWARE\Dual Monitor Tools\DMT";
		const string DmtWallpaperPathName = "DmtWallpaperPath";
		const string LanguageValueName = "Language";

		public static void SetDmtWallpaperFilename(string dmtWallpaperFilename)
		{
			if (dmtWallpaperFilename != _lastDmtWallpaperFilename)
			{
				RegistryKey key = Registry.CurrentUser.CreateSubKey(DmtKey);
				key.SetValue(DmtWallpaperPathName, dmtWallpaperFilename);
				key.Close();

				_lastDmtWallpaperFilename = dmtWallpaperFilename;
			}
		}

		/// <summary>
		/// Gets the saved UI language preference.
		/// </summary>
		/// <returns>The saved value, or null when it has never been set.</returns>
		public static string GetLanguage()
		{
			try
			{
				RegistryKey key = Registry.CurrentUser.OpenSubKey(DmtKey);
				if (key == null)
				{
					return null;
				}

				object value = key.GetValue(LanguageValueName);
				key.Close();

				if (value == null)
				{
					return null;
				}

				return value.ToString();
			}
			catch (Exception)
			{
				// No permission or a damaged value: fall back to having no preference.
				return null;
			}
		}

		/// <summary>
		/// Saves the UI language preference.
		/// </summary>
		/// <param name="languageTag">Language tag to save, e.g. "en" or "zh-Hans"</param>
		public static void SetLanguage(string languageTag)
		{
			RegistryKey key = Registry.CurrentUser.CreateSubKey(DmtKey);
			key.SetValue(LanguageValueName, languageTag);
			key.Close();
		}
	}
}
