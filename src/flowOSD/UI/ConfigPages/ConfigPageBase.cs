﻿/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of flowOSD.
 *
 *  flowOSD is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  flowOSD is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with flowOSD. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using flowOSD.Extensions;
using flowOSD.UI.Components;
using System.Drawing.Drawing2D;
using System.Reactive.Disposables;

internal class ConfigPageBase : TableLayoutPanel
{
    protected static readonly Padding CheckBoxMargin = new Padding(20, 5, 0, 5);
    protected static readonly Padding LabelMargin = new Padding(15, 10, 0, 15);

    private UIParameters? uiParameters;

    protected ConfigPageBase(IConfig config, CxTabListener? tabListener = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        TabListener = tabListener ?? throw new ArgumentNullException(nameof(tabListener));

        Dock = DockStyle.Top;
        AutoScroll = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Padding = new Padding(3);
        IconFont = new Font(UIParameters.IconFontName, 16, GraphicsUnit.Point);

        MouseClick += OnMouseClick;
    }

    public UIParameters? UIParameters
    {
        get => uiParameters;
        set
        {
            uiParameters = value;
            UpdateUI();
        }
    }

    protected IConfig Config { get; }

    protected CxTabListener? TabListener { get; }

    protected Font IconFont { get; }

    protected void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (TabListener != null)
        {
            TabListener.ShowKeyboardFocus = false;
        }
    }

    protected void AddConfig(string icon, string text, string propertyName)
    {
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        this.Add<CxGrid>(0, RowStyles.Count - 1, x =>
        {
            x.TabListener = TabListener;
            x.Padding = new Padding(10, 5, 10, 5);
            x.Dock = DockStyle.Top;
            x.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            x.AutoSize = true;

            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            if (!string.IsNullOrEmpty(icon))
            {
                x.Add<CxLabel>(0, 0, y =>
                {
                    y.TabListener = TabListener;
                    y.AutoSize = true;
                    y.Margin = LabelMargin;
                    y.Padding = new Padding(0, 10, 0, 0);
                    y.Anchor = AnchorStyles.Left;
                    y.ForeColor = Color.Wheat;
                    y.Icon = icon;
                    y.IconFont = IconFont;
                });
            }

            x.Add<CxLabel>(1, 0, y =>
            {
                y.TabListener = TabListener;
                y.AutoSize = true;
                y.Margin = LabelMargin;
                y.Text = text;
                y.Anchor = AnchorStyles.Left;
                y.ForeColor = SystemColors.ControlDarkDark;
                y.UseClearType = true;
            });

            x.Add<CxToggle>(2, 0, y =>
            {
                y.TabListener = TabListener;
                y.BackColor = SystemColors.Control;
                y.ForeColor = SystemColors.WindowText;
                y.Margin = CheckBoxMargin;
                y.Anchor = AnchorStyles.Right;
                y.DataBindings.Add(
                    "IsChecked",
                    Config.UserConfig,
                    propertyName,
                    false,
                    DataSourceUpdateMode.OnPropertyChanged);
            });
        });
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        if (Visible)
        {
            UpdateUI();
        }

        base.OnVisibleChanged(e);
    }

    protected virtual void UpdateUI()
    {
        if (UIParameters == null)
        {
            return;
        }

        BackColor = UIParameters.BackgroundColor;

        CxTheme.Apply(this, UIParameters);
    }
}
