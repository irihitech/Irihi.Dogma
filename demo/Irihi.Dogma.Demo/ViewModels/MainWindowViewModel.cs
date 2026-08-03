using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Dogma.Controls;

namespace Irihi.Dogma.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>是否使用亮色主题（演示 Avalonia 原生 RequestedThemeVariant 切换）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequestedTheme))]
    private bool _useLightTheme;

    public ThemeVariant RequestedTheme => UseLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>示例 AXAML 源码（覆盖元素/属性/绑定/MarkupExtension/嵌套扩展/注释）。</summary>
    public string SampleAxaml { get; } = """
        <Window xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:vm="using:Irihi.Dogma.Demo.ViewModels"
                x:Class="Irihi.Dogma.Demo.Views.MainWindow"
                x:DataType="vm:MainWindowViewModel"
                Title="Irihi.Dogma Demo" Width="900" Height="700">
            <!-- 主界面 -->
            <Window.Styles>
                <Style Selector="Button">
                    <Setter Property="CornerRadius" Value="4"/>
                </Style>
            </Window.Styles>
            <ScrollViewer>
                <StackPanel Spacing="12" Margin="12">
                    <TextBlock Text="{Binding Greeting}"
                               FontSize="16" FontWeight="Bold"/>
                    <Button Content="Click me"
                            Command="{Binding GreetCommand}"
                            IsVisible="{Binding !IsBusy}"/>
                    <TextBlock Text="{Binding Path=SampleAxaml, Mode=OneWay}"/>
                </StackPanel>
            </ScrollViewer>
        </Window>
        """;

    /// <summary>示例 C# 源码（覆盖关键字/类型名/插值字符串/verbatim 字符串/region/注释）。</summary>
    public string SampleCSharp { get; } = """
        using System;
        using System.Collections.Generic;

        namespace Irihi.Dogma.Demo;

        /// <summary>示例服务：展示代码高亮。</summary>
        public sealed class Greeter
        {
            private const string Prefix = "Hello";

            // 泛型集合 + 插值字符串
            public List<string> Greet(string name, int times)
            {
                var list = new List<string>();
                for (var i = 0; i < times; i++)
                {
                    var message = $"{Prefix}, {name}! #{i}";
                    Console.WriteLine(message); // 输出问候
                    list.Add(message);
                }
                return list;
            }

            public string RawPath => @"C:\tmp\demo\path";

            #region 反射工具
            public static Type TypeOf() => typeof(Greeter);
            #endregion
        }
        """;
}
