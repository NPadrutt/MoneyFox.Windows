using MoneyFox.Ui.Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MoneyFox.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Stack<NavigationPage> navigationPageStack = new Stack<NavigationPage>();
        private NavigationPage CurrentNavigationPage => navigationPageStack.Peek();

        private static readonly ConcurrentDictionary<Type, Type> viewModelMap = new ConcurrentDictionary<Type, Type>();

        public static void Register<TViewModel, TView>() where TView : Page
        {
            if(!viewModelMap.TryAdd(typeof(TViewModel), typeof(TView)))
            {
                throw new InvalidOperationException($"ViewModel already registered '{typeof(TViewModel).FullName}'");
            }
        }

        public Page SetRootPage(Type rootVmType)
        {
            Page? rootPage = GetPage(rootVmType);
            navigationPageStack.Clear();
            var mainPage = new NavigationPage(rootPage);
            navigationPageStack.Push(mainPage);
            return mainPage;
        }

        /// <summary>
        ///     On Xamarin Forms always returns true;
        /// </summary>
        public bool CanGoBack => true;

        /// <summary>
        ///     On Xamarin Forms always returns false;
        /// </summary>
        public bool CanGoForward => false;

        public async Task GoBack()
        {
            INavigation? navigationStack = CurrentNavigationPage.Navigation;
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
            Page? page = GetPage(typeof(TViewModel), parameter);
            NavigationPage.SetHasNavigationBar(page, false);
            var modalNavigationPage = new NavigationPage(page);
            await CurrentNavigationPage.Navigation.PushModalAsync(modalNavigationPage, animated);
            navigationPageStack.Push(modalNavigationPage);
        }

        public async Task NavigateAsync<TViewModel>(object parameter = null, bool animated = true)
            => await NavigateAsync(typeof(TViewModel));

        public async Task NavigateAsync(Type viewModel, object parameter = null, bool animated = true)
        {
            Page? page = GetPage(viewModel, parameter);
            await CurrentNavigationPage.Navigation.PushAsync(page, animated);
        }

        public static Type GetView(Type viewModel)
        {
            if(viewModelMap.TryGetValue(viewModel, out Type view))
            {
                return view;
            }
            throw new InvalidOperationException($"View not registered for ViewModel '{viewModel.FullName}'");
        }

        private Page GetPage(Type viewModelType, object parameter = null)
        {

            if(!viewModelMap.TryGetValue(viewModelType, out Type type))
            {
                throw new InvalidOperationException($"View not registered for ViewModel '{viewModelType.FullName}'");

            }

            ConstructorInfo constructor;
            object[] parameters;

            if(parameter == null)
            {
                constructor = type.GetTypeInfo()
                    .DeclaredConstructors
                    .FirstOrDefault(c => !c.GetParameters().Any());

                parameters = new object[] { };
            }
            else
            {
                constructor = type.GetTypeInfo()
                    .DeclaredConstructors
                    .FirstOrDefault(
                        c =>
                        {
                            ParameterInfo[]? p = c.GetParameters();
                            return p.Length == 1 && p[0].ParameterType == parameter.GetType();
                        });

                parameters = new[] { parameter };
            }

            if(constructor == null)
            {
                throw new InvalidOperationException(
                    "No suitable constructor found for page " + viewModelType);
            }

            var page = constructor.Invoke(parameters) as Page;
            return page;
        }

        public Task GoForward() => throw new NotImplementedException();
    }
}
