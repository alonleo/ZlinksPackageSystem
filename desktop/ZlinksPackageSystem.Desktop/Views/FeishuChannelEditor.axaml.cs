using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class FeishuChannelEditor : UserControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> RemoveRequestedEvent =
            RoutedEvent.Register<FeishuChannelEditor, RoutedEventArgs>(
                nameof(RemoveRequested), RoutingStrategies.Bubble);

        public static readonly RoutedEvent<RoutedEventArgs> TestRequestedEvent =
            RoutedEvent.Register<FeishuChannelEditor, RoutedEventArgs>(
                nameof(TestRequested), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs>? RemoveRequested
        {
            add => AddHandler(RemoveRequestedEvent, value);
            remove => RemoveHandler(RemoveRequestedEvent, value);
        }

        public event EventHandler<RoutedEventArgs>? TestRequested
        {
            add => AddHandler(TestRequestedEvent, value);
            remove => RemoveHandler(TestRequestedEvent, value);
        }

        public FeishuChannelEditor()
        {
            InitializeComponent();
        }

        private void OnRemoveClicked(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RemoveRequestedEvent, this));
        }

        private void OnTestClicked(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(TestRequestedEvent, this));
        }
    }
}