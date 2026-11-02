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
	partial class LanguageOptionsPanel
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.labelHeading = new System.Windows.Forms.Label();
			this.listBoxLanguages = new System.Windows.Forms.ListBox();
			this.labelHint = new System.Windows.Forms.Label();
			this.labelRestartNeeded = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// labelHeading
			// 
			this.labelHeading.AutoSize = true;
			this.labelHeading.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelHeading.Location = new System.Drawing.Point(25, 25);
			this.labelHeading.Name = "labelHeading";
			this.labelHeading.Size = new System.Drawing.Size(141, 20);
			this.labelHeading.TabIndex = 0;
			this.labelHeading.Text = global::DMT.Resources.GeneralStrings.LanguageOptionsPanel_labelHeading_Text;
			// 
			// listBoxLanguages
			// 
			this.listBoxLanguages.FormattingEnabled = true;
			this.listBoxLanguages.IntegralHeight = false;
			this.listBoxLanguages.Location = new System.Drawing.Point(25, 60);
			this.listBoxLanguages.Name = "listBoxLanguages";
			this.listBoxLanguages.Size = new System.Drawing.Size(300, 120);
			this.listBoxLanguages.TabIndex = 1;
			this.listBoxLanguages.SelectedIndexChanged += new System.EventHandler(this.listBoxLanguages_SelectedIndexChanged);
			// 
			// labelHint
			// 
			this.labelHint.AutoSize = true;
			this.labelHint.Location = new System.Drawing.Point(25, 195);
			this.labelHint.Name = "labelHint";
			this.labelHint.Size = new System.Drawing.Size(300, 13);
			this.labelHint.TabIndex = 2;
			this.labelHint.Text = global::DMT.Resources.GeneralStrings.LanguageOptionsPanel_labelHint_Text;
			// 
			// labelRestartNeeded
			// 
			this.labelRestartNeeded.AutoSize = true;
			this.labelRestartNeeded.Location = new System.Drawing.Point(25, 220);
			this.labelRestartNeeded.Name = "labelRestartNeeded";
			this.labelRestartNeeded.Size = new System.Drawing.Size(400, 13);
			this.labelRestartNeeded.TabIndex = 3;
			this.labelRestartNeeded.Text = global::DMT.Resources.GeneralStrings.LanguageOptionsPanel_labelRestartNeeded_Text;
			this.labelRestartNeeded.Visible = false;
			// 
			// LanguageOptionsPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.labelRestartNeeded);
			this.Controls.Add(this.labelHint);
			this.Controls.Add(this.listBoxLanguages);
			this.Controls.Add(this.labelHeading);
			this.Name = "LanguageOptionsPanel";
			this.Size = new System.Drawing.Size(500, 360);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelHeading;
		private System.Windows.Forms.ListBox listBoxLanguages;
		private System.Windows.Forms.Label labelHint;
		private System.Windows.Forms.Label labelRestartNeeded;
	}
}
