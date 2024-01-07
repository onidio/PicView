using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace PicView.Avalonia.Views.UC;

public partial class StartUpMenu : UserControl
{
    public StartUpMenu()
    {
        InitializeComponent();
        SizeChanged += (_, e) => ResponsiveSize(e.NewSize.Width);
    }

    public void ResponsiveSize(double width)
    {
        const int breakPoint = 900;
        const int bottomMargin = 16;
        switch (width)
        {
            case < breakPoint:
                if (this.TryFindResource("Icon", ThemeVariant.Default, out var icon))
                    Logo.Source = icon as DrawingImage;
                LogoViewbox.Width = 350;
                Buttons.Margin = new Thickness(0, 0, 0, bottomMargin);
                Buttons.VerticalAlignment = VerticalAlignment.Bottom;
                break;

            case > breakPoint:
                if (this.TryFindResource("Logo", ThemeVariant.Default, out var logo))
                    Logo.Source = logo as DrawingImage;
                LogoViewbox.Width = double.NaN;
                Buttons.Margin = new Thickness(0, 220, 25, bottomMargin - 100);
                Buttons.VerticalAlignment = VerticalAlignment.Center;
                break;
        }
    }
}