using MoneyFox.Ui.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MoneyFox.Services
{
    public class NavigationService : INavigationService
    {
        private readonly object sync = new object();
        private readonly Dictionary<Type, Type> pagesByKey = new Dictionary<Type, Type>();
        private readonly Stack<NavigationPage> navigationPageStack = new Stack<NavigationPage>();
        private NavigationPage CurrentNavigationPage => navigationPageStack.Peek();

        public void Configure(Type viewModel, Type pageType)
        {
            lock(sync)
            {
                if(pagesByKey.ContainsKey(viewModel))
                {
                    pagesByKey[viewModel] = pageType;
                }
                else
                {
                    pagesByKey.Add(viewModel, pageType);
                }
            }
        }

        public Page SetRootPage(Type rootVmType)
        {
            var rootPage = GetPage(rootVmType);
            navigationPageStack.Clear();
            var mainPage = new NavigationPage(rootPage);
            navigationPageStack.Push(mainPage);
            return mainPage;
        }

        public async Task GoBack()
        {
            var navigationStack = CurrentNavigationPage.Navigation;
            if(navigationStack.NavigationStack.Count > 1)
            {
                await CurrentNavigationPage.PopAsync();
                return;
            }

            if(navigationPageStack.Count > 1)
            {
                navigationPageStack.Pop();
                await CurrentNavigationPage.Navigation.PopModalAsync();
                return;
            }

            await CurrentNavigationPage.PopAsync();
        }

        public async Task NavigateModalAsync<TViewModel>(object parameter = null, bool animated = true)
        {
            var page = GetPage(typeof(TViewModel), parameter);
            NavigationPage.SetHasNavigationBar(page, false);
            var modalNavigationPage = new NavigationPage(page);
            await CurrentNavigationPage.Navigation.PushModalAsync(modalNavigationPage, animated);
            navigationPageStack.Push(modalNavigationPage);
        }

        public async Task NavigateAsync<TViewModel>(object parameter = null, bool animated = true)
        {
            var page = GetPage(typeof(TViewModel), parameter);
            await CurrentNavigationPage.Navigation.PushAsync(page, animated);
        }

        private Page GetPage(Type viewModelType, object parameter = null)
        {

            lock(sync)
            {
                if(!pagesByKey.ContainsKey(viewModelType))
                {
                    throw new ArgumentException(
                        $"No such page: {viewModelType}. Did you forget to call NavigationService.Configure?");
                }

                var type = pagesByKey[viewModelType];
                ConstructorInfo constructor;
                object[] parameters;

                if(parameter == null)
                {
                    constructor = type.GetTypeInfo()
                        .DeclaredConstructors
                        .FirstOrDefault(c => !c.GetParameters().Any());

                    parameters = new object[]
                    {
                    };
                }
                else
                {
                    constructor = type.GetTypeInfo()
                        .DeclaredConstructors
                        .FirstOrDefault(
                            c =>
                            {
                                var p = c.GetParameters();
                                return p.Length == 1
                                       && p[0].ParameterType == parameter.GetType();
                            });

                    parameters = new[]
                    {
                    parameter
                };
                }

                if(constructor == null)
                {
                    throw new InvalidOperationException(
                        "No suitable constructor found for page " + viewModelType);
                }

                var page = constructor.Invoke(parameters) as Page;
                return page;
            }
        }
    }
}
