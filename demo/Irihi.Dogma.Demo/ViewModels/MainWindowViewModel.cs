using CommunityToolkit.Mvvm.ComponentModel;

namespace Irihi.Dogma.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>示例 AXAML 源码（演示用，覆盖元素/属性/绑定/MarkupExtension/注释）。</summary>
    public string SampleAxaml { get; } = """
        <Window xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:vm="using:Irihi.Dogma.Demo.ViewModels"
                x:Class="Irihi.Dogma.Demo.Views.MainWindow"
                x:DataType="vm:MainWindowViewModel"
                Title="Irihi.Dogma Demo" Width="900" Height="700">
            <!-- 主界面 -->
            <ScrollViewer>
                <StackPanel Spacing="12" Margin="12">
                    <TextBlock Text="{Binding Greeting}"
                               FontSize="16" FontWeight="Bold"/>
                    <Button Content="Click me"
                            Command="{Binding GreetCommand}"
                            IsVisible="{Binding !IsBusy}"/>
                </StackPanel>
            </ScrollViewer>
        </Window>
        """;

    /// <summary>示例 C# 源码（演示用，覆盖关键字/注释/字符串/插值/数字）。</summary>
    public string SampleCSharp { get; } = """
        using System;

        namespace Irihi.Dogma.Demo;

        /// <summary>示例服务。</summary>
        public sealed class Greeter
        {
            private const string Prefix = "Hello";

            // 返回问候语
            public string Greet(string name, int times)
            {
                var message = $"{Prefix}, {name}! x{times}";
                Console.WriteLine(message); // 输出
                return message;
            }
        }
        """;
}
