﻿using AutoMapper;
using MediatR;
using MoneyFox.Application.Accounts.Commands.CreateAccount;
using MoneyFox.Application.Accounts.Queries.GetIfAccountWithNameExists;
using MoneyFox.Application.Common.Interfaces;
using MoneyFox.Application.Resources;
using MoneyFox.Ui.Shared.Services;
using MoneyFox.Ui.Shared.Utilities;
using MoneyFox.Ui.Shared.ViewModels.Accounts;
using System.Threading.Tasks;

#nullable enable
namespace MoneyFox.Uwp.ViewModels
{
    public class AddAccountViewModel : ModifyAccountViewModel
    {
        private readonly IMediator mediator;

        public AddAccountViewModel(IMediator mediator,
                                   IMapper mapper,
                                   IDialogService dialogService,
                                   INavigationService navigationService) : base(dialogService, navigationService)
        {
            this.mediator = mediator;

            Title = Strings.AddAccountTitle;
        }

        protected override Task InitializeAsync()
        {
            SelectedAccount = new AccountViewModel();
            AmountString = HelperFunctions.FormatLargeNumbers(SelectedAccount.CurrentBalance);

            return Task.CompletedTask;
        }

        protected override async Task SaveAccountAsync()
        {
            if(await mediator.Send(new GetIfAccountWithNameExistsQuery(SelectedAccount.Name)))
            {
                await DialogService.ShowMessageAsync(Strings.DuplicatedNameTitle, Strings.DuplicateAccountMessage);
                return;
            }

            await mediator.Send(new CreateAccountCommand(SelectedAccount.Name,
                                                         SelectedAccount.CurrentBalance,
                                                         SelectedAccount.Note,
                                                         SelectedAccount.IsExcluded));
        }
    }
}
