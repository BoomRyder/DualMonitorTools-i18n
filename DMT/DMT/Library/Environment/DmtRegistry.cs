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
		const string ProductKey = @"SOFTWARE\Dual Monitor Tools";
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

		/// <summary>
		/// Removes the saved UI language preference.
		/// Used when restoring a machine to its pre-test state.
		/// </summary>
		public static void DeleteLanguage()
		{
			try
			{
				RegistryKey key = Registry.CurrentUser.OpenSubKey(DmtKey, true);
				if (key != null)
				{
					key.DeleteValue(LanguageValueName, false);
					key.Close();
				}
			}
			catch (Exception)
			{
				// Nothing to remove, or no permission: leave the key alone.
			}
		}

		/// <summary>
		/// Reports whether the DMT settings key exists at all.
		/// </summary>
		/// <returns>True when the key is present.</returns>
		public static bool DmtKeyExists()
		{
			try
			{
				RegistryKey key = Registry.CurrentUser.OpenSubKey(DmtKey);
				if (key == null)
				{
					return false;
				}

				key.Close();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Deletes the DMT settings key when it holds no values and no subkeys,
		/// then deletes the parent product key if that has become empty too.
		/// Creating a preference value creates the whole key chain as a side
		/// effect, so this puts the registry back exactly as it was before.
		/// </summary>
		public static void DeleteDmtKeyIfEmpty()
		{
			try
			{
				RegistryKey key = Registry.CurrentUser.OpenSubKey(DmtKey, true);
				if (key != null)
				{
					bool empty = key.ValueCount == 0 && key.SubKeyCount == 0;
					key.Close();

					if (empty)
					{
						Registry.CurrentUser.DeleteSubKey(DmtKey, false);
					}
					else
					{
						// Something is still stored here; leave the chain alone.
						return;
					}
				}

				RegistryKey parent = Registry.CurrentUser.OpenSubKey(ProductKey, true);
				if (parent != null)
				{
					bool parentEmpty = parent.ValueCount == 0 && parent.SubKeyCount == 0;
					parent.Close();

					if (parentEmpty)
					{
						Registry.CurrentUser.DeleteSubKey(ProductKey, false);
					}
				}
			}
			catch (Exception)
			{
				// Leave it alone rather than risk removing something in use.
			}
		}
	}
}
