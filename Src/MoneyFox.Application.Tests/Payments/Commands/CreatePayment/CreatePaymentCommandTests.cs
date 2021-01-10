﻿using FluentAssertions;
using MoneyFox.Application.Common;
using MoneyFox.Application.Common.CloudBackup;
using MoneyFox.Application.Common.Facades;
using MoneyFox.Application.Common.Interfaces;
using MoneyFox.Application.Payments.Commands.CreatePayment;
using MoneyFox.Application.Tests.Infrastructure;
using MoneyFox.Domain;
using MoneyFox.Domain.Entities;
using MoneyFox.Persistence;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace MoneyFox.Application.Tests.Payments.Commands.CreatePayment
{
    [ExcludeFromCodeCoverage]
    public class CreatePaymentCommandTests : IDisposable
    {
        private readonly EfCoreContext context;
        private readonly Mock<IContextAdapter> contextAdapterMock;
        private readonly Mock<IBackupService> backupServiceMock;
        private readonly Mock<ISettingsFacade> settingsFacadeMock;

        public CreatePaymentCommandTests()
        {
            context = InMemoryEfCoreContextFactory.Create();

            contextAdapterMock = new Mock<IContextAdapter>();
            contextAdapterMock.SetupGet(x => x.Context).Returns(context);

            backupServiceMock = new Mock<IBackupService>();
            backupServiceMock.Setup(x => x.UploadBackupAsync(BackupMode.Automatic))
                             .Returns(Task.CompletedTask);

            settingsFacadeMock = new Mock<ISettingsFacade>();
            settingsFacadeMock.SetupSet(x => x.LastDatabaseUpdate = It.IsAny<DateTime>());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) => InMemoryEfCoreContextFactory.Destroy(context);

        [Fact]
        public async Task CreatePayment_PaymentSaved()
        {
            // Arrange
            var account = new Account("test", 80);
            context.Add(account);
            context.SaveChanges();

            var payment1 = new Payment(DateTime.Now, 20, PaymentType.Expense, account);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object)
                                          .Handle(new CreatePaymentCommand(payment1), default);

            // Assert
            Assert.Single(context.Payments);
            (await context.Payments.FindAsync(payment1.Id)).Should().NotBeNull();
        }

        [Fact]
        public async Task BackupUploaded()
        {
            // Arrange
            var account = new Account("test", 80);
            context.Add(account);
            context.SaveChanges();

            var payment1 = new Payment(DateTime.Now, 20, PaymentType.Expense, account);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object).Handle(new CreatePaymentCommand(payment1), default);

            // Assert
            backupServiceMock.Verify(x => x.UploadBackupAsync(BackupMode.Automatic), Times.Once);
            settingsFacadeMock.VerifySet(x => x.LastDatabaseUpdate = It.IsAny<DateTime>(), Times.Once);
        }

        [Theory]
        [InlineData(PaymentType.Expense, 60)]
        [InlineData(PaymentType.Income, 100)]
        public async Task CreatePayment_AccountCurrentBalanceUpdated(PaymentType paymentType, decimal newCurrentBalance)
        {
            // Arrange
            var account = new Account("test", 80);
            context.Add(account);
            await context.SaveChangesAsync();

            var payment1 = new Payment(DateTime.Now, 20, paymentType, account);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object).Handle(new CreatePaymentCommand(payment1), default);

            // Assert
            Account loadedAccount = await context.Accounts.FindAsync(account.Id);
            loadedAccount.Should().NotBeNull();
            loadedAccount.CurrentBalance.Should().Be(newCurrentBalance);
        }

        [Fact]
        public async Task CreatePaymentWithRecurring_PaymentSaved()
        {
            // Arrange
            var account = new Account("test", 80);
            context.Add(account);
            context.SaveChanges();

            var payment1 = new Payment(DateTime.Now, 20, PaymentType.Expense, account);

            payment1.AddRecurringPayment(PaymentRecurrence.Monthly);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object).Handle(new CreatePaymentCommand(payment1), default);

            // Assert
            Assert.Single(context.Payments);
            Assert.Single(context.RecurringPayments);
            (await context.Payments.FindAsync(payment1.Id)).Should().NotBeNull();
            (await context.RecurringPayments.FindAsync(payment1.RecurringPayment.Id)).Should().NotBeNull();
        }

        [Fact]
        public async Task AccountBalanceCorrectlySet()
        {
            // Arrange
            var account = new Account("test", 80);
            context.Add(account);
            context.SaveChanges();

            var payment1 = new Payment(DateTime.Now, 20, PaymentType.Expense, account);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object)
                                          .Handle(new CreatePaymentCommand(payment1), default);

            // Assert
            (await context.Payments.FindAsync(payment1.Id)).AccountBalance.Should().Be(60);
        }

        [Fact]
        public async Task AccountBalanceCorrectlySetOnTransfer()
        {
            // Arrange
            var account1 = new Account("test1", 100);
            var account2 = new Account("test2", 200);
            context.Add(account1);
            context.Add(account2);
            context.SaveChanges();

            var payment1 = new Payment(DateTime.Now, 60, PaymentType.Transfer, account1, account2);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object)
                                          .Handle(new CreatePaymentCommand(payment1), default);

            // Assert
            (await context.Payments.FindAsync(payment1.Id)).AccountBalance.Should().Be(40);
        }

        [Fact]
        public async Task AccountBalanceCorrectlySetOnFollowingPayments()
        {
            // Arrange
            var account = new Account("test", 80);
            var payment1 = new Payment(DateTime.Now, 20, PaymentType.Expense, account);
            context.Add(account);
            context.Add(payment1);
            context.SaveChanges();

            var payment2 = new Payment(DateTime.Now.AddDays(-1), 30, PaymentType.Expense, account);

            // Act
            await new CreatePaymentCommand.Handler(contextAdapterMock.Object,
                                                   backupServiceMock.Object,
                                                   settingsFacadeMock.Object)
                                          .Handle(new CreatePaymentCommand(payment2), default);

            // Assert
            (await context.Payments.FindAsync(payment1.Id)).AccountBalance.Should().Be(30);
        }
    }
}
