using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.ViewModels;
using Irihi.Dogma.Docs;
using Irihi.Lingua;

namespace Irihi.Dogma.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LanguageManager.Instance.UpdateCulture(new  CultureInfo("zh-Hans"));
        Controls.Localizations.LanguageManager.Instance.UpdateCulture(new  CultureInfo("zh-Hans"));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 文档站集成：项目自己的 DocSite 实例（非全局单例）
        // 注册 SG 生成的页面/分类
        GeneratedDocPages.Register(DemoDocSite.Default);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 外壳整体来自 Irihi.Dogma.Controls（DemoShellWindow 演示菜单图标配置）；
            // demo 只提供站点、语言、文化与 VM→View 定位器
            desktop.MainWindow = new DemoShellWindow
            {
                DataContext = new DocShellViewModel(DemoDocSite.Default, LanguageManager.Instance)
                {
                    Title = LanguageManager.Instance.App_Title,
                    ViewLocator = new GeneratedViewLocator(),
                    Cultures =
                    [
                        new LinguaCulture { Culture = new CultureInfo("en-US"), DisplayName = "English" },
                        new LinguaCulture { Culture = new CultureInfo("zh-Hans"), DisplayName = "中文" }
                    ],
                    Managers = [
                        LanguageManager.Instance,
                        Controls.Localizations.LanguageManager.Instance,
                    ]
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
