using MoneyFox.Ui.Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

#nullable enable
namespace MoneyFox.Uwp.Services
{
    public class NavigationService : INavigationService
    {
        private static readonly ConcurrentDictionary<Type, Type> viewModelMap = new ConcurrentDictionary<Type, Type>();

        static NavigationService()
        {
            MainViewId = ApplicationView.GetForCurrentView().Id;
        }

        public static int MainViewId { get; }

        public static void Register<TViewModel, TView>() where TView : Page
        {
            if(!viewModelMap.TryAdd(typeof(TViewModel), typeof(TView)))
            {
                throw new InvalidOperationException($"ViewModel already registered '{typeof(TViewModel).FullName}'");
            }
        }

        public static Type GetView<TViewModel>() => GetView(typeof(TViewModel));

        public static Type GetView(Type viewModel)
        {
            if(viewModelMap.TryGetValue(viewModel, out Type view))
            {
                return view;
            }
            throw new InvalidOperationException($"View not registered for ViewModel '{viewModel.FullName}'");
        }

        public static Type GetViewModel(Type view)
        {
            Type? type = viewModelMap.Where(r => r.Value == view).Select(r => r.Key).FirstOrDefault();
            if(type == null)
            {
                throw new InvalidOperationException($"View not registered for ViewModel '{view.FullName}'");
            }
            return type;
        }

        public Frame? Frame { get; private set; }

        public bool CanGoBack => Frame?.CanGoBack ?? false;

        public bool CanGoForward => Frame?.CanGoForward ?? false;

        public Task GoBack()
        {
            if(CanGoBack)
            {
                Frame?.GoBack();
            }

            return Task.CompletedTask;
        }

        public Task GoForward()
        {
            if(CanGoForward)
            {
                Frame?.GoForward();
            }

            return Task.CompletedTask;
        }

        public void Initialize(object frame) => Frame = (Frame)frame;

        public async Task NavigateAsync<TViewModel>(object parameter = null, bool animated = true)
            => await NavigateAsync(typeof(TViewModel), parameter);

        public Task NavigateAsync(Type viewModelType, object parameter = null, bool animated = true)
        {
            if(Frame == null)
            {
                throw new InvalidOperationException("Navigation frame not initialized.");
            }
            Frame.Navigate(GetView(viewModelType), parameter);
            return Task.CompletedTask;
        }

        public void Configure(Type viewModel, Type pageType) => throw new NotImplementedException();

        public async Task NavigateModalAsync<TViewModel>(object parameter = null, bool animated = true) => await NavigateAsync(typeof(TViewModel), parameter);
    }
}
