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
	using System.Globalization;
	using System.Reflection;
	using System.Text;
	using System.Threading;

	/// <summary>
	/// Applies the selected UI language to the current thread and to every
	/// strongly-typed resource class in this assembly.
	/// </summary>
	/// <remarks>
	/// The target framework is .NET Framework 4.0, which has no
	/// CultureInfo.DefaultThreadCurrentUICulture, and this application reads
	/// resources from worker threads as well as the UI thread. Setting each
	/// resource class's static Culture property directly is what keeps every
	/// thread reporting the same language.
	///
	/// This file is compiled into all three executables. typeof(UiCulture).Assembly
	/// is therefore whichever executable this copy belongs to, which is exactly the
	/// set of resource classes that executable owns.
	/// </remarks>
	public static class UiCulture
	{
		/// <summary>
		/// Applies a language to the current thread and to all resource classes
		/// in this assembly.
		/// </summary>
		/// <param name="languageTag">
		/// Language tag to apply. Unsupported or null values select the default
		/// language instead of failing.
		/// </param>
		public static void Apply(string languageTag)
		{
			CultureInfo culture = LanguageCatalog.CultureFor(languageTag);

			Thread.CurrentThread.CurrentUICulture = culture;

			Type[] types;
			try
			{
				types = typeof(UiCulture).Assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				// Partial load: work with whatever types did resolve.
				types = ex.Types;
			}

			if (types == null)
			{
				return;
			}

			foreach (Type type in types)
			{
				if (type == null)
				{
					continue;
				}

				try
				{
					PropertyInfo property = type.GetProperty(
						"Culture",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

					if (property == null)
					{
						continue;
					}

					if (property.PropertyType != typeof(CultureInfo))
					{
						continue;
					}

					if (!property.CanWrite)
					{
						continue;
					}

					property.SetValue(null, culture, null);
				}
				catch (Exception)
				{
					// A resource class that refuses the change must not stop the rest.
				}
			}
		}
	}
}
