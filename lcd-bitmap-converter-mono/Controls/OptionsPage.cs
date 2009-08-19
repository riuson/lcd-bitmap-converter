﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace lcd_bitmap_converter_mono
{
	public partial class OptionsPage: TabPage
	{
        private OptionsControl mControl;

		public OptionsPage()
		{
			InitializeComponent();

            this.mControl = new OptionsControl();
            this.Controls.Add(this.mControl);
            this.mControl.Dock = DockStyle.Fill;
		}
	}
}
