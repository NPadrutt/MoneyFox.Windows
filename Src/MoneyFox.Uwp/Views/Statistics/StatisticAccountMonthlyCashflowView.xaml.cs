﻿using MoneyFox.Application.Resources;
using MoneyFox.ViewModels.Statistics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

#nullable enable
namespace MoneyFox.Uwp.Views.Statistics
{
    public sealed partial class StatisticAccountMonthlyCashflowView
    {
        public StatisticAccountMonthlyCashflowViewModel ViewModel => (StatisticAccountMonthlyCashflowViewModel)DataContext;

        public override string Header => Strings.MonthlyCashflowTitle;

        public StatisticAccountMonthlyCashflowView()
        {
            InitializeComponent();
            DataContext = ViewModelLocator.StatisticAccountMonthlyCashflowVm;
        }

        private void OpenFilterFlyout(object sender, RoutedEventArgs e) => FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
    }
}
