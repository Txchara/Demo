using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Tool.UI;

public partial class MainWindow
{
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        const string iconPath = @"D:\Temp\MyDemo\convertico-Icon.ico";
        if (!File.Exists(iconPath))
        {
            return;
        }

        try
        {
            Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
        catch
        {
        }
    }
}
