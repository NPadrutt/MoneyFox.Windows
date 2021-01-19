﻿using CommonServiceLocator;
using MoneyFox.Ui.Shared.Services;
using MoneyFox.Uwp.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

#nullable enable
namespace MoneyFox.Uwp.Activation
{
    internal class DefaultLaunchActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
    {
        private readonly Type startupViewModel;

        public static INavigationService NavigationService => ServiceLocator.Current.GetInstance<INavigationService>();

        public DefaultLaunchActivationHandler(Type startupViewModel)
        {
            this.startupViewModel = startupViewModel;
        }

        protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
        {
            // When the navigation stack isn't restored, navigate to the first page and configure
            // the new page by passing required information in the navigation parameter
            await NavigationService.NavigateAsync(startupViewModel, args.Arguments);
            await Task.CompletedTask;
        }

        protected override bool CanHandleInternal(LaunchActivatedEventArgs args) =>
            // None of the ActivationHandlers has handled the app activation
            startupViewModel != null;
    }
}
