/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
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
    private IList<object> cxItems = new List<object>();

    protected ConfigPageBase(IConfig config, CxTabListener? tabListener = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        TabListener = tabListener;// ?? throw new ArgumentNullException(nameof(tabListener));

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

    protected CompositeDisposable? Disposable { get; private set; } = new CompositeDisposable();

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
            x.MouseClick += OnMouseClick;
            x.Padding = new Padding(10, 5, 10, 5);
            x.Dock = DockStyle.Top;
            x.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            x.AutoSize = true;

            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            RegisterCxItem(x);

            x.Add<CxLabel>(0, 0, y =>
            {
                y.AutoSize = true;
                y.Margin = LabelMargin;
                y.Padding = new Padding(0, 10, 0, 0);
                y.Anchor = AnchorStyles.Left;
                y.ForeColor = Color.Wheat;
                y.Icon = icon;
                y.IconFont = IconFont;

                y.DisposeWith(Disposable!);
                RegisterCxItem(y);
            });

            x.Add<CxLabel>(1, 0, y =>
            {
                y.AutoSize = true;
                y.Margin = LabelMargin;
                y.Text = text;
                y.Anchor = AnchorStyles.Left;
                y.ForeColor = SystemColors.ControlDarkDark;
                y.UseClearType = true;

                y.DisposeWith(Disposable!);
                RegisterCxItem(y);
            });

            x.Add<CxToggle>(2, 0, y =>
            {
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

                y.DisposeWith(Disposable!);
                RegisterCxItem(y);
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

    protected override void Dispose(bool disposing)
    {
        Disposable?.Dispose();
        Disposable = null;

        base.Dispose(disposing);
    }

    protected void RegisterCxItem(object item)
    {
        if (item is CxLabel label)
        {
            label.TabListener = TabListener;
        }

        if (item is CxButtonBase button)
        {
            button.TabListener = TabListener;
        }

        cxItems.Add(item);
    }

    protected virtual void UpdateUI()
    {
        if (UIParameters == null)
        {
            return;
        }

        BackColor = UIParameters.BackgroundColor;

        foreach (var i in cxItems)
        {
            if (i is CxButtonBase b)
            {
                b.AccentColor = UIParameters.AccentColor;
                b.ForeColor = UIParameters.TextGrayColor;
                b.BackColor = UIParameters.BackgroundColor;
                b.FocusColor = UIParameters.FocusColor;
            }

            if (i is CxButton btn)
            {
                btn.TextColor = UIParameters.ButtonTextColor;
                btn.TextBrightColor = UIParameters.ButtonTextBrightColor;
                btn.BackColor = UIParameters.ButtonBackgroundColor;
            }

            if (i is CxLabel l)
            {
                l.ForeColor = UIParameters.TextColor;
                l.BackColor = UIParameters.MenuBackgroundColor;
            }

            if (i is CxGrid g)
            {
                g.ForeColor = UIParameters.TextColor;
                g.BackColor = UIParameters.PanelBackgroundColor;
            }

            if (i is CxContextMenu m)
            {
                m.BackgroundColor = UIParameters.MenuBackgroundColor;
                m.BackgroundHoverColor = UIParameters.MenuBackgroundHoverColor;
                m.TextColor = UIParameters.MenuTextColor;
                m.TextBrightColor = UIParameters.MenuTextBrightColor;
                m.TextDisabledColor = UIParameters.MenuTextDisabledColor;
            }
        }
    }
}
