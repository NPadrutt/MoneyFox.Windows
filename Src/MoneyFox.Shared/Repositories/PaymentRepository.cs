﻿using MoneyFox.Shared.Exceptions;
using MoneyFox.Shared.Helpers;
using MoneyFox.Shared.Interfaces;
using MoneyFox.Shared.Model;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using MoneyFox.Shared.Resources;

namespace MoneyFox.Shared.Repositories
{
    [ImplementPropertyChanged]
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IAccountRepository accountRepository;
        private readonly IRepository<Category> categoryRepository;
        private readonly IDataAccess<Payment> dataAccess;
        private readonly IDataAccess<RecurringPayment> recurringDataAccess;
        private readonly INotificationService notificationService;

        private ObservableCollection<Payment> data;

        /// <summary>
        ///     Creates a paymentRepository Object
        /// </summary>
        /// <param name="dataAccess">Instanced <see cref="IDataAccess{T}" /> for <see cref="Payment" /></param>
        /// <param name="recurringDataAccess">
        ///     Instanced <see cref="IDataAccess{T}" /> for <see cref="RecurringPayment" />
        /// </param>
        /// <param name="accountRepository">Instanced <see cref="IAccountRepository" /></param>
        /// <param name="categoryRepository">
        ///     Instanced <see cref="IRepository{T}" /> for <see cref="Category" />
        /// </param>
        public PaymentRepository(IDataAccess<Payment> dataAccess,
            IDataAccess<RecurringPayment> recurringDataAccess,
            IAccountRepository accountRepository,
            IRepository<Category> categoryRepository, INotificationService notificationService)
        {
            this.dataAccess = dataAccess;
            this.recurringDataAccess = recurringDataAccess;
            this.accountRepository = accountRepository;
            this.categoryRepository = categoryRepository;
            this.notificationService = notificationService;

            Data = new ObservableCollection<Payment>();
            Load();
        }

        /// <summary>
        ///     Cached accountToDelete data
        /// </summary>
        public ObservableCollection<Payment> Data
        {
            get { return data; }
            set
            {
                if (Equals(data, value))
                {
                    return;
                }
                data = value;
            }
        }

        /// <summary>
        ///     The currently selected Payment
        /// </summary>
        public Payment Selected { get; set; }

        /// <summary>
        ///     Save a new payment or update an existin one.
        /// </summary>
        /// <param name="payment">item to save</param>
        public void Save(Payment payment)
        {
            if (payment.ChargedAccountId == 0)
            {
                throw new AccountMissingException("charged accout is missing");
            }

            payment.IsCleared = payment.ClearPaymentNow;

            //delete recurring payment if isRecurring is no longer set.
            if (!payment.IsRecurring && payment.RecurringPaymentId != 0)
            {
                recurringDataAccess.DeleteItem(payment.RecurringPayment);
                payment.RecurringPaymentId = 0;
            }

            if (payment.Id == 0)
            {
                data.Add(payment);
            }
            if (dataAccess.SaveItem(payment))
            {
                notificationService.SendBasicNotification(Strings.ErrorTitleSave, Strings.ErrorMessageSave);
            } else
            {
                Settings.LastDatabaseUpdate = DateTime.Now;
            }
        }

        /// <summary>
        ///     Deletes the passed payment and removes the item from cache
        /// </summary>
        /// <param name="paymentToDelete">Payment to delete.</param>
        public void Delete(Payment paymentToDelete)
        {
            var payments = Data.Where(x => x.Id == paymentToDelete.Id).ToList();

            foreach (var payment in payments)
            {
                data.Remove(payment);
                dataAccess.DeleteItem(payment);
                if (dataAccess.DeleteItem(payment))
                {
                    notificationService.SendBasicNotification(Strings.ErrorTitleDelete, Strings.ErrorMessageDelete);
                } else
                {
                    Settings.LastDatabaseUpdate = DateTime.Now;
                }

                // If this accountToDelete was the last finacial accountToDelete for the linked recurring accountToDelete
                // delete the db entry for the recurring accountToDelete.
                DeleteRecurringPaymentIfLastAssociated(payment);
            }
        }

        /// <summary>
        ///     Deletes the passed recurring payment
        /// </summary>
        /// <param name="paymentToDelete">Recurring payment to delete.</param>
        public void DeleteRecurring(Payment paymentToDelete)
        {
            var payments = Data.Where(x => x.Id == paymentToDelete.Id).ToList();

            recurringDataAccess.DeleteItem(paymentToDelete.RecurringPayment);

            foreach (var payment in payments)
            {
                payment.RecurringPayment = null;
                payment.IsRecurring = false;
                Save(payment);
            }
        }

        /// <summary>
        ///     Loads all payments from the database to the data collection
        /// </summary>
        public void Load(Expression<Func<Payment, bool>> filter = null)
        {
            Data.Clear();
            var payments = dataAccess.LoadList(filter);
            var recurringTransactions = recurringDataAccess.LoadList();

            foreach (var payment in payments)
            {
                payment.ChargedAccount = accountRepository.Data.FirstOrDefault(x => x.Id == payment.ChargedAccountId);
                payment.TargetAccount = accountRepository.Data.FirstOrDefault(x => x.Id == payment.TargetAccountId);

                payment.Category = categoryRepository.Data.FirstOrDefault(x => x.Id == payment.CategoryId);

                if (payment.IsRecurring)
                {
                    payment.RecurringPayment =
                        recurringTransactions.FirstOrDefault(x => x.Id == payment.RecurringPaymentId);
                }

                Data.Add(payment);
            }
        }

        /// <summary>
        ///     Returns all uncleared payments up to today
        /// </summary>
        /// <returns>list of uncleared payments</returns>
        public IEnumerable<Payment> GetUnclearedPayments() => GetUnclearedPayments(DateTime.Today);

        /// <summary>
        ///     Returns all uncleared payments up to the passed date from the database.
        /// </summary>
        /// <returns>list of uncleared payments</returns>
        public IEnumerable<Payment> GetUnclearedPayments(DateTime date)
            => Data
            .Where(x => !x.IsCleared)
            .Where(x => x.Date.Date <= date.Date)
            .ToList();

        /// <summary>
        ///     returns a list with payments who are related to this account
        /// </summary>
        /// <param name="account">account to search the related</param>
        /// <returns>List of payments</returns>
        public IEnumerable<Payment> GetRelatedPayments(Account account)
            => Data.Where(x => x.ChargedAccountId == account.Id || x.TargetAccountId == account.Id)
                   .OrderByDescending(x => x.Date)
                   .ToList();

        /// <summary>
        ///     returns a list with payments who recure in a given timeframe
        /// </summary>
        /// <returns>list of recurring payments</returns>
        public IEnumerable<Payment> LoadRecurringList(Func<Payment, bool> filter = null)
        {
            var list = Data
                .Where(x => x.IsRecurring && x.RecurringPaymentId != 0)
                .Where(x => (x.RecurringPayment.IsEndless ||
                             x.RecurringPayment.EndDate >= DateTime.Now.Date)
                            && (filter == null || filter.Invoke(x)))
                .ToList();

            return list
                .Select(x => x.RecurringPaymentId)
                .Distinct()
                .Select(id => list.Where(x => x.RecurringPaymentId == id)
                    .OrderByDescending(x => x.Date)
                    .Last())
                .ToList();
        }

        private void DeleteRecurringPaymentIfLastAssociated(Payment item)
        {
            if (Data.All(x => x.RecurringPaymentId != item.RecurringPaymentId))
            {
                var recurringList = recurringDataAccess.LoadList(x => x.Id == item.RecurringPaymentId).ToList();

                foreach (var recTrans in recurringList)
                {
                    if (recurringDataAccess.DeleteItem(recTrans))
                    {
                        notificationService.SendBasicNotification(Strings.ErrorTitleDelete, Strings.ErrorMessageDelete);
                    } else
                    {
                        Settings.LastDatabaseUpdate = DateTime.Now;
                    }
                }
            }
        }
    }
}