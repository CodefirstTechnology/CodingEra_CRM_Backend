using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;

namespace ERP.Infrastructure.Accounting
{
    public class AccountingFoundationService : IAccountingFoundationService
    {
        private readonly CustomerLedgerNumberingService _customerLedgerNumbering;
        private readonly GstNumberingService _gstNumbering;
        private readonly PaymentNumberingService _paymentNumbering;
        private readonly ReceiptNumberingService _receiptNumbering;
        private readonly BankReconNumberingService _bankReconNumbering;

        public AccountingFoundationService(
            CustomerLedgerNumberingService customerLedgerNumbering,
            GstNumberingService gstNumbering,
            PaymentNumberingService paymentNumbering,
            ReceiptNumberingService receiptNumbering,
            BankReconNumberingService bankReconNumbering)
        {
            _customerLedgerNumbering = customerLedgerNumbering;
            _gstNumbering = gstNumbering;
            _paymentNumbering = paymentNumbering;
            _receiptNumbering = receiptNumbering;
            _bankReconNumbering = bankReconNumbering;
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AccountingPermissions.All);
        }

        public Task<string> GenerateCustomerLedgerNumberAsync(CancellationToken cancellationToken = default)
        {
            return _customerLedgerNumbering.GenerateNumberAsync(cancellationToken);
        }

        public Task<string> GenerateGstTransactionNumberAsync(CancellationToken cancellationToken = default)
        {
            return _gstNumbering.GenerateNumberAsync(cancellationToken);
        }

        public Task<string> GeneratePaymentNumberAsync(CancellationToken cancellationToken = default)
        {
            return _paymentNumbering.GenerateNumberAsync(cancellationToken);
        }

        public Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken = default)
        {
            return _receiptNumbering.GenerateNumberAsync(cancellationToken);
        }

        public Task<string> GenerateBankReconNumberAsync(CancellationToken cancellationToken = default)
        {
            return _bankReconNumbering.GenerateNumberAsync(cancellationToken);
        }
    }
}
