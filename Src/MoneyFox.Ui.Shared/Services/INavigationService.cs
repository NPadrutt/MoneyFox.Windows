using System;
using System.Threading.Tasks;

namespace MoneyFox.Ui.Shared.Services
{
    public interface INavigationService
    {
        void Configure(Type viewModel, Type pageType);
        Task GoBack();
        Task NavigateModalAsync<TViewModel>(object parameter = null, bool animated = true);
        Task NavigateAsync<TViewModel>(object parameter = null, bool animated = true);
    }
}
