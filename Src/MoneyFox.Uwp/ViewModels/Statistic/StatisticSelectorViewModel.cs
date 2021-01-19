﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MoneyFox.Application.Resources;
using MoneyFox.Domain;
using MoneyFox.Ui.Shared.Services;
using MoneyFox.Ui.Shared.ViewModels.Statistics;
using MoneyFox.Uwp.ViewModels.Statistic;
using MoneyFox.Uwp.ViewModels.Statistic.StatisticCategorySummary;
using MoneyFox.ViewModels.Statistics;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable
namespace MoneyFox.Presentation.ViewModels.Statistic
{
    public class StatisticSelectorViewModel : ViewModelBase, IStatisticSelectorViewModel
    {
        private readonly INavigationService navigationService;

        /// <summary>
        ///     Constructor
        /// </summary>
        public StatisticSelectorViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        /// <summary>
        ///     All possible statistic to choose from
        /// </summary>
        public List<StatisticSelectorType> StatisticItems => new List<StatisticSelectorType>
        {
            new StatisticSelectorType
            {
                Name = Strings.CashflowLabel,
                Description = Strings.CashflowDescription,
                Type = StatisticType.Cashflow
            },
            new StatisticSelectorType
            {
                Name = Strings.MonthlyCashflowLabel,
                Description = Strings.MonthlyCashflowDescription,
                Type = StatisticType.MonthlyAccountCashFlow
            },
            new StatisticSelectorType
            {
                Name = Strings.CategorySpreadingLabel,
                Description = Strings.CategorieSpreadingDescription,
                Type = StatisticType.CategorySpreading
            },
            new StatisticSelectorType
            {
                Name = Strings.CategorySummaryLabel,
                Description = Strings.CategorySummaryDescription,
                Type = StatisticType.CategorySummary
            }
        };

        /// <summary>
        ///     Navigates to the statistic view and shows the selected statistic
        /// </summary>
        public RelayCommand<StatisticSelectorType> GoToStatisticCommand => new RelayCommand<StatisticSelectorType>(async (t) => await GoToStatistic(t));

        private async Task GoToStatistic(StatisticSelectorType item)
        {
            if(item.Type == StatisticType.Cashflow)
            {
                await navigationService.NavigateAsync<StatisticCashFlowViewModel>();
            }
            else if(item.Type == StatisticType.MonthlyAccountCashFlow)
            {
                await navigationService.NavigateAsync<StatisticAccountMonthlyCashflowViewModel>();
            }
            else if(item.Type == StatisticType.CategorySpreading)
            {
                await navigationService.NavigateAsync<StatisticCategorySpreadingViewModel>();
            }
            else if(item.Type == StatisticType.CategorySummary)
            {
                await navigationService.NavigateAsync<StatisticCategorySummaryViewModel>();
            }
        }
    }
}
