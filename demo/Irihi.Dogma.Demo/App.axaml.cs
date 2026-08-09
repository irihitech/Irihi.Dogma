using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Irihi.Dogma.Demo.ViewModels;
using Irihi.Dogma.Demo.Views;
using Irihi.Dogma.Docs;

namespace Irihi.Dogma.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 文档站集成：注册 SG 生成的页面/分类 + Lingua 文本源 + ViewLocator
        GeneratedDocPages.Register(DocSite.Instance);
        DocSite.Instance.LinguaManager = LanguageManager.Instance;
        DataTemplates.Add(new GeneratedViewLocator());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
