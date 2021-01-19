using System;
using System.Threading.Tasks;

namespace MoneyFox.Ui.Shared.Services
{
    public interface INavigationService
    {
        bool CanGoBack { get; }
        bool CanGoForward { get; }

        Task GoBack();
        Task GoForward();
        Task NavigateModalAsync<TViewModel>(object parameter = null, bool animated = true);

        Task NavigateAsync<TViewModel>(object parameter = null, bool animated = true);
        Task NavigateAsync(Type viewModelType, object parameter = null, bool animated = true);
    }
}
