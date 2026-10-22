using System.Text;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Domain.Entities;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class MemoService : IMemoService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<MemoService> _logger;

    public MemoService(AccountDbContext context, ILogger<MemoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateTransactionMemoHtmlAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Customer)
            .Include(t => t.Items)
            .Include(t => t.CostEntries)
            .Include(t => t.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} was not found.");
        }

        var customer = transaction.Customer;
        decimal previousBalance = customer != null ? Math.Max(0m, customer.CurrentBalance - transaction.BalanceDue) : 0m;
        decimal totalOutstanding = previousBalance + transaction.BalanceDue;

        var settings = await _context.AppSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
        string businessName = settings.GetValueOrDefault("BusinessName", "রূপালী প্রেস অ্যান্ড প্যাকেজিং (Rupali Press & Packaging)");
        string address = settings.GetValueOrDefault("Address", "ঢাকা, বাংলাদেশ (Dhaka, Bangladesh)");
        string phone = settings.GetValueOrDefault("Phone", "+880 1700-000000");

        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"bn\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>Cash Memo - {transaction.TransactionNumber}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        * { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, 'Hind Siliguri', sans-serif; background-color: #f1f5f9; color: #0f172a; padding: 20px; }");
        sb.AppendLine("        .memo-container { max-width: 820px; margin: 0 auto; background: #ffffff; border: 1.5px solid #08294d; border-radius: 8px; padding: 32px 36px; box-shadow: 0 4px 16px rgba(0,0,0,0.08); position: relative; }");
        sb.AppendLine("        .actions-bar { max-width: 820px; margin: 0 auto 16px auto; display: flex; justify-content: space-between; align-items: center; }");
        sb.AppendLine("        .btn { padding: 9px 18px; border-radius: 6px; font-weight: bold; font-size: 13px; cursor: pointer; border: none; text-decoration: none; display: inline-flex; align-items: center; gap: 6px; }");
        sb.AppendLine("        .btn-print { background-color: #059669; color: #ffffff; }");
        sb.AppendLine("        .btn-print:hover { background-color: #047857; }");
        sb.AppendLine("        .btn-close { background-color: #e2e8f0; color: #0f172a; }");
        sb.AppendLine("        .header { text-align: center; border-bottom: 2px solid #08294d; padding-bottom: 14px; margin-bottom: 18px; }");
        sb.AppendLine("        .bismillah { font-size: 11px; color: #64748b; margin-bottom: 4px; }");
        sb.AppendLine("        .biz-title-bn { font-size: 26px; font-weight: 800; color: #08294d; letter-spacing: 0.5px; }");
        sb.AppendLine("        .biz-title-en { font-size: 14px; font-weight: 700; color: #3d73b0; letter-spacing: 1.5px; margin-top: 2px; }");
        sb.AppendLine("        .biz-desc { font-size: 12px; color: #334155; margin-top: 4px; }");
        sb.AppendLine("        .biz-contact { font-size: 11px; color: #475569; margin-top: 4px; }");
        sb.AppendLine("        .memo-badge-wrap { text-align: center; margin: -10px 0 16px 0; }");
        sb.AppendLine("        .memo-badge { display: inline-block; background: #08294d; color: #ffffff; font-size: 12px; font-weight: bold; padding: 4px 18px; border-radius: 20px; letter-spacing: 1px; }");
        sb.AppendLine("        .meta-grid { display: flex; justify-content: space-between; margin-bottom: 18px; font-size: 13px; line-height: 1.6; }");
        sb.AppendLine("        .meta-col { flex: 1; }");
        sb.AppendLine("        .meta-col-right { text-align: right; }");
        sb.AppendLine("        .meta-item { display: flex; margin-bottom: 2px; }");
        sb.AppendLine("        .meta-label { font-weight: 600; color: #475569; width: 130px; }");
        sb.AppendLine("        .meta-val { font-weight: 700; color: #08294d; }");
        sb.AppendLine("        .table { width: 100%; border-collapse: collapse; margin-bottom: 16px; font-size: 12px; }");
        sb.AppendLine("        .table th { background-color: #08294d; color: #ffffff; padding: 8px 10px; font-weight: 700; text-align: left; border: 1px solid #08294d; }");
        sb.AppendLine("        .table td { padding: 8px 10px; border: 1px solid #cbd5e1; color: #0f172a; }");
        sb.AppendLine("        .table tr:nth-child(even) td { background-color: #f8fafc; }");
        sb.AppendLine("        .text-center { text-align: center; }");
        sb.AppendLine("        .text-right { text-align: right; }");
        sb.AppendLine("        .font-bold { font-weight: bold; }");
        sb.AppendLine("        .section-header td { background-color: #e2e8f0; font-weight: bold; color: #08294d; }");
        sb.AppendLine("        .calc-grid { display: flex; justify-content: space-between; margin-bottom: 16px; }");
        sb.AppendLine("        .words-col { flex: 1; padding-right: 20px; font-size: 12px; }");
        sb.AppendLine("        .words-box { background: #f8fafc; border: 1px dashed #94a3b8; padding: 10px 14px; border-radius: 6px; margin-top: 6px; color: #08294d; font-weight: bold; }");
        sb.AppendLine("        .totals-table { width: 340px; border-collapse: collapse; font-size: 12px; }");
        sb.AppendLine("        .totals-table td { padding: 6px 10px; border: 1px solid #cbd5e1; }");
        sb.AppendLine("        .totals-table .label-col { font-weight: 600; color: #334155; }");
        sb.AppendLine("        .totals-table .val-col { text-align: right; font-weight: 700; color: #0f172a; }");
        sb.AppendLine("        .totals-table .highlight-bill td { background: #f1f5f9; font-size: 13px; color: #08294d; }");
        sb.AppendLine("        .totals-table .highlight-paid td { background: #ecfdf5; color: #059669; }");
        sb.AppendLine("        .totals-table .highlight-due td { background: #fffbeb; color: #d97706; font-size: 13px; }");
        sb.AppendLine("        .totals-table .highlight-total td { background: #08294d; color: #ffffff; font-size: 14px; }");
        sb.AppendLine("        .notes-box { font-size: 11px; color: #64748b; line-height: 1.5; margin-bottom: 24px; border-top: 1px solid #e2e8f0; padding-top: 8px; }");
        sb.AppendLine("        .signatures { display: flex; justify-content: space-between; margin-top: 40px; padding: 0 20px; }");
        sb.AppendLine("        .sig-block { text-align: center; width: 190px; }");
        sb.AppendLine("        .sig-line { border-top: 1px solid #475569; margin-bottom: 6px; }");
        sb.AppendLine("        .sig-text { font-size: 12px; font-weight: 600; color: #334155; }");
        sb.AppendLine("        @media print {");
        sb.AppendLine("            body { background: #ffffff; padding: 0; }");
        sb.AppendLine("            .memo-container { border: 1.5px solid #000; box-shadow: none; padding: 20px 24px; }");
        sb.AppendLine("            .no-print { display: none !important; }");
        sb.AppendLine("        }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Action Toolbar (Hidden during print)
        sb.AppendLine("    <div class=\"actions-bar no-print\">");
        sb.AppendLine("        <div style=\"font-size: 13px; font-weight: bold; color: #08294d;\">📄 Cash Memo Preview - Rupali Press &amp; Packaging</div>");
        sb.AppendLine("        <div style=\"display: flex; gap: 8px;\">");
        sb.AppendLine("            <button class=\"btn btn-print\" onclick=\"window.print()\">🖨️ Print Memo | প্রিন্ট করুন</button>");
        sb.AppendLine("            <button class=\"btn btn-close\" onclick=\"window.close()\">✕ Close</button>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");

        // Main Memo Container
        sb.AppendLine("    <div class=\"memo-container\">");

        // Header
        sb.AppendLine("        <div class=\"header\">");
        sb.AppendLine("            <div class=\"bismillah\">বিস্‌মিল্লাহির রাহ্‌মানির রাহিম</div>");
        sb.AppendLine($"            <div class=\"biz-title-bn\">{businessName}</div>");
        sb.AppendLine("            <div class=\"biz-title-en\">RUPALI PRESS &amp; PACKAGING</div>");
        sb.AppendLine("            <div class=\"biz-desc\">অফসেট প্রিন্টিং, ফয়েল প্রিন্টিং, ডাই-কাটিং ও সকল প্রকার বক্স প্যাকেজিং এর নির্ভরযোগ্য প্রতিষ্ঠান</div>");
        sb.AppendLine($"            <div class=\"biz-contact\">📍 {address} • 📞 {phone}</div>");
        sb.AppendLine("        </div>");

        // Memo Ribbon
        sb.AppendLine("        <div class=\"memo-badge-wrap\">");
        sb.AppendLine("            <span class=\"memo-badge\">ক্যাশ মেমো / বিল চালান (CASH MEMO)</span>");
        sb.AppendLine("        </div>");

        // Metadata Grid (Customer & Invoice Info)
        sb.AppendLine("        <div class=\"meta-grid\">");
        sb.AppendLine("            <div class=\"meta-col\">");
        sb.AppendLine($"                <div class=\"meta-item\"><span class=\"meta-label\">গ্রাহকের নাম (Customer):</span><span class=\"meta-val\">{customer?.Name ?? "General Customer"}</span></div>");
        if (!string.IsNullOrWhiteSpace(customer?.ContactPerson))
            sb.AppendLine($"                <div class=\"meta-item\"><span class=\"meta-label\">যোগাযোগ (Contact):</span><span>{customer.ContactPerson}</span></div>");
        if (!string.IsNullOrWhiteSpace(customer?.PhoneNumber))
            sb.AppendLine($"                <div class=\"meta-item\"><span class=\"meta-label\">মোবাইল (Phone):</span><span>{customer.PhoneNumber}</span></div>");
        if (!string.IsNullOrWhiteSpace(customer?.Address))
            sb.AppendLine($"                <div class=\"meta-item\"><span class=\"meta-label\">ঠিকানা (Address):</span><span>{customer.Address}</span></div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"meta-col meta-col-right\">");
        sb.AppendLine($"                <div><strong>মেমো নং (Memo No):</strong> <span style=\"color:#08294d; font-size:14px; font-weight:bold;\">{transaction.TransactionNumber}</span></div>");
        sb.AppendLine($"                <div><strong>তারিখ (Date):</strong> {transaction.TransactionDate:dd-MM-yyyy}</div>");
        sb.AppendLine($"                <div><strong>কাজের ধরণ (Work):</strong> {transaction.WorkDescription}</div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        // Line Items Table
        sb.AppendLine("        <table class=\"table\">");
        sb.AppendLine("            <thead>");
        sb.AppendLine("                <tr>");
        sb.AppendLine("                    <th style=\"width: 40px;\" class=\"text-center\">ক্র.নং</th>");
        sb.AppendLine("                    <th>বিবরণ ও আইটেম (Description &amp; Specifications)</th>");
        sb.AppendLine("                    <th style=\"width: 100px;\" class=\"text-center\">সাইজ / GSM</th>");
        sb.AppendLine("                    <th style=\"width: 80px;\" class=\"text-center\">পরিমাণ</th>");
        sb.AppendLine("                    <th style=\"width: 90px;\" class=\"text-right\">দর (৳)</th>");
        sb.AppendLine("                    <th style=\"width: 110px;\" class=\"text-right\">মোট টাকা (৳)</th>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("            </thead>");
        sb.AppendLine("            <tbody>");

        int sl = 1;
        foreach (var item in transaction.Items)
        {
            string sizeGsm = $"{item.SizeSnapshot ?? "-"} / {item.GsmSnapshot ?? "-"}";
            sb.AppendLine("                <tr>");
            sb.AppendLine($"                    <td class=\"text-center\">{sl++}</td>");
            sb.AppendLine($"                    <td class=\"font-bold\">{item.MaterialNameSnapshot}</td>");
            sb.AppendLine($"                    <td class=\"text-center\">{sizeGsm}</td>");
            sb.AppendLine($"                    <td class=\"text-center\">{item.Quantity:N0} {item.Unit}</td>");
            sb.AppendLine($"                    <td class=\"text-right\">{item.UnitRateSnapshot:N2}</td>");
            sb.AppendLine($"                    <td class=\"text-right font-bold\">{item.TotalMaterialCost:N2}</td>");
            sb.AppendLine("                </tr>");
        }

        // Additional Production Cost Entries (Plate, Lamination, Die-cutting, etc.)
        if (transaction.CostEntries.Count > 0)
        {
            sb.AppendLine("                <tr class=\"section-header\">");
            sb.AppendLine("                    <td colspan=\"6\">উৎপাদন ও অন্যান্য প্রসেসিং খরচ (Production &amp; Finishing Charges)</td>");
            sb.AppendLine("                </tr>");

            foreach (var cost in transaction.CostEntries)
            {
                sb.AppendLine("                <tr>");
                sb.AppendLine($"                    <td class=\"text-center\">{sl++}</td>");
                sb.AppendLine($"                    <td colspan=\"4\">{cost.Category} - {cost.Description}</td>");
                sb.AppendLine($"                    <td class=\"text-right font-bold\">{cost.Amount:N2}</td>");
                sb.AppendLine("                </tr>");
            }
        }

        sb.AppendLine("            </tbody>");
        sb.AppendLine("        </table>");

        // Calculations & Amount in Words
        sb.AppendLine("        <div class=\"calc-grid\">");
        sb.AppendLine("            <div class=\"words-col\">");
        sb.AppendLine("                <div><strong>কথায় (In Words):</strong></div>");
        sb.AppendLine($"                <div class=\"words-box\">{ToBengaliWords((long)Math.Round(transaction.TotalBillAmount))} টাকা মাত্র। ({ToEnglishWords((long)Math.Round(transaction.TotalBillAmount))} Taka Only)</div>");
        if (!string.IsNullOrWhiteSpace(transaction.Notes))
        {
            sb.AppendLine($"                <div style=\"margin-top:10px; font-size:11px;\"><strong>মন্তব্য / নোট:</strong> {transaction.Notes}</div>");
        }
        sb.AppendLine("            </div>");

        // Totals Box
        sb.AppendLine("            <table class=\"totals-table\">");
        sb.AppendLine("                <tr>");
        sb.AppendLine("                    <td class=\"label-col\">উপ-মোট (Sub Total):</td>");
        sb.AppendLine($"                    <td class=\"val-col\">৳ {transaction.SubTotal:N2}</td>");
        sb.AppendLine("                </tr>");
        if (transaction.Discount > 0)
        {
            sb.AppendLine("                <tr>");
            sb.AppendLine("                    <td class=\"label-col\">ছাড় (Discount):</td>");
            sb.AppendLine($"                    <td class=\"val-col\" style=\"color:#dc2626;\">- ৳ {transaction.Discount:N2}</td>");
            sb.AppendLine("                </tr>");
        }
        sb.AppendLine("                <tr class=\"highlight-bill\">");
        sb.AppendLine("                    <td class=\"label-col font-bold\">মোট বিল (Net Bill Amount):</td>");
        sb.AppendLine($"                    <td class=\"val-col font-bold\">৳ {transaction.TotalBillAmount:N2}</td>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("                <tr class=\"highlight-paid\">");
        sb.AppendLine("                    <td class=\"label-col font-bold\">জমা / অগ্রিম (Paid / Advance):</td>");
        sb.AppendLine($"                    <td class=\"val-col font-bold\">৳ {transaction.TotalPaidAmount:N2}</td>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("                <tr class=\"highlight-due\">");
        sb.AppendLine("                    <td class=\"label-col font-bold\">চালান বকেয়া (Invoice Due):</td>");
        sb.AppendLine($"                    <td class=\"val-col font-bold\">৳ {transaction.BalanceDue:N2}</td>");
        sb.AppendLine("                </tr>");

        if (previousBalance > 0)
        {
            sb.AppendLine("                <tr>");
            sb.AppendLine("                    <td class=\"label-col\">পূর্বের বকেয়া (Previous Balance):</td>");
            sb.AppendLine($"                    <td class=\"val-col\">৳ {previousBalance:N2}</td>");
            sb.AppendLine("                </tr>");
            sb.AppendLine("                <tr class=\"highlight-total\">");
            sb.AppendLine("                    <td class=\"label-col font-bold\">মোট পাওনা (Total Outstanding):</td>");
            sb.AppendLine($"                    <td class=\"val-col font-bold\">৳ {totalOutstanding:N2}</td>");
            sb.AppendLine("                </tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        // Terms
        sb.AppendLine("        <div class=\"notes-box\">");
        sb.AppendLine("            <div>• মালামাল ডেলিভারির সময় ভালো করে যাচাই করে বুঝে নিন। পরবর্তীতে কোনো আপত্তি গ্রহণযোগ্য হবে না।</div>");
        sb.AppendLine("            <div>• কম্পিউটারে মুদ্রিত এই মেমোতে কোনো প্রকার কাটাকাটি গ্রহণযোগ্য নহে। রূপালী প্রেস অ্যান্ড প্যাকেজিং এর সাথে থাকার জন্য ধন্যবাদ।</div>");
        sb.AppendLine("        </div>");

        // Signatures
        sb.AppendLine("        <div class=\"signatures\">");
        sb.AppendLine("            <div class=\"sig-block\">");
        sb.AppendLine("                <div class=\"sig-line\"></div>");
        sb.AppendLine("                <div class=\"sig-text\">গ্রাহকের স্বাক্ষর<br><span style=\"font-size:10px; color:#64748b;\">Customer Signature</span></div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"sig-block\">");
        sb.AppendLine("                <div class=\"sig-line\"></div>");
        sb.AppendLine("                <div class=\"sig-text\">কর্তৃপক্ষের স্বাক্ষর<br><span style=\"font-size:10px; color:#64748b;\">Authorized Signature</span></div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public async Task<string> GenerateCustomerStatementMemoHtmlAsync(Guid customerId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer {customerId} was not found.");
        }

        var transactionsQuery = _context.Transactions
            .Where(t => t.CustomerId == customerId);
        var paymentsQuery = _context.Payments
            .Where(p => p.CustomerId == customerId);

        if (fromDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate >= fromDate.Value);
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate <= toDate.Value);
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= toDate.Value);
        }

        var txnList = await transactionsQuery.OrderBy(t => t.TransactionDate).ToListAsync(cancellationToken);
        var payList = await paymentsQuery.OrderBy(p => p.PaymentDate).ToListAsync(cancellationToken);

        decimal totalBilled = txnList.Sum(t => t.TotalBillAmount);
        decimal totalPaid = payList.Sum(p => p.Amount);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"bn\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine($"    <title>Customer Statement - {customer.Name}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        * { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, 'Hind Siliguri', sans-serif; background-color: #f1f5f9; color: #0f172a; padding: 20px; }");
        sb.AppendLine("        .container { max-width: 860px; margin: 0 auto; background: #ffffff; border: 1.5px solid #08294d; border-radius: 8px; padding: 30px; box-shadow: 0 4px 16px rgba(0,0,0,0.08); }");
        sb.AppendLine("        .header { text-align: center; border-bottom: 2px solid #08294d; padding-bottom: 12px; margin-bottom: 16px; }");
        sb.AppendLine("        .title-bn { font-size: 24px; font-weight: 800; color: #08294d; }");
        sb.AppendLine("        .title-en { font-size: 13px; font-weight: 700; color: #3d73b0; letter-spacing: 1.5px; }");
        sb.AppendLine("        .badge { display: inline-block; background: #08294d; color: #ffffff; font-size: 12px; font-weight: bold; padding: 4px 16px; border-radius: 20px; margin-top: 8px; }");
        sb.AppendLine("        .cust-bar { background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 6px; padding: 12px 16px; margin-bottom: 16px; font-size: 13px; display: flex; justify-content: space-between; }");
        sb.AppendLine("        .table { width: 100%; border-collapse: collapse; margin-bottom: 16px; font-size: 12px; }");
        sb.AppendLine("        .table th { background-color: #08294d; color: #ffffff; padding: 8px 10px; font-weight: 700; border: 1px solid #08294d; }");
        sb.AppendLine("        .table td { padding: 8px 10px; border: 1px solid #cbd5e1; }");
        sb.AppendLine("        .text-right { text-align: right; }");
        sb.AppendLine("        .text-center { text-align: center; }");
        sb.AppendLine("        .font-bold { font-weight: bold; }");
        sb.AppendLine("        .summary-box { background: #08294d; color: #ffffff; border-radius: 6px; padding: 14px 20px; display: flex; justify-content: space-between; font-size: 14px; font-weight: bold; }");
        sb.AppendLine("        .no-print { max-width: 860px; margin: 0 auto 14px auto; display: flex; justify-content: space-between; }");
        sb.AppendLine("        .btn { padding: 9px 18px; border-radius: 6px; font-weight: bold; font-size: 13px; cursor: pointer; border: none; }");
        sb.AppendLine("        .btn-print { background-color: #059669; color: #ffffff; }");
        sb.AppendLine("        @media print { body { background: #fff; padding: 0; } .container { border: 1.5px solid #000; box-shadow: none; } .no-print { display: none !important; } }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"no-print\"><button class=\"btn btn-print\" onclick=\"window.print()\">🖨️ Print Statement / খতিয়ান প্রিন্ট করুন</button></div>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <div class=\"header\">");
        sb.AppendLine("            <div class=\"title-bn\">রূপালী প্রেস অ্যান্ড প্যাকেজিং</div>");
        sb.AppendLine("            <div class=\"title-en\">RUPALI PRESS &amp; PACKAGING • CUSTOMER ACCOUNT STATEMENT</div>");
        sb.AppendLine("            <div><span class=\"badge\">গ্রাহকের খতিয়ান হিসাব বিবরণী (LEDGER STATEMENT)</span></div>");
        sb.AppendLine("        </div>");

        sb.AppendLine("        <div class=\"cust-bar\">");
        sb.AppendLine($"            <div><strong>গ্রাহক:</strong> {customer.Name} | {customer.PhoneNumber ?? "N/A"}</div>");
        sb.AppendLine($"            <div><strong>ঠিকানা:</strong> {customer.Address ?? "Dhaka"}</div>");
        sb.AppendLine($"            <div><strong>তারিখ:</strong> {DateTime.Today:dd-MM-yyyy}</div>");
        sb.AppendLine("        </div>");

        // Transactions Table
        sb.AppendLine("        <div style=\"font-size:13px; font-weight:bold; color:#08294d; margin-bottom:6px;\">১. কাজের বিবরণ ও বিলের তালিকা (Billed Jobs):</div>");
        sb.AppendLine("        <table class=\"table\">");
        sb.AppendLine("            <thead><tr><th class=\"text-center\">তারিখ</th><th>চালান নং</th><th>কাজের বিবরণ</th><th class=\"text-right\">বিল (৳)</th><th class=\"text-right\">জমা (৳)</th><th class=\"text-right\">বকেয়া (৳)</th></tr></thead>");
        sb.AppendLine("            <tbody>");
        foreach (var t in txnList)
        {
            sb.AppendLine($"                <tr><td class=\"text-center\">{t.TransactionDate:dd-MM-yyyy}</td><td class=\"font-bold\">{t.TransactionNumber}</td><td>{t.WorkDescription}</td><td class=\"text-right font-bold\">{t.TotalBillAmount:N2}</td><td class=\"text-right\">{t.TotalPaidAmount:N2}</td><td class=\"text-right font-bold\" style=\"color:#d97706;\">{t.BalanceDue:N2}</td></tr>");
        }
        sb.AppendLine("            </tbody>");
        sb.AppendLine("        </table>");

        // Payments Table
        if (payList.Count > 0)
        {
            sb.AppendLine("        <div style=\"font-size:13px; font-weight:bold; color:#059669; margin:16px 0 6px 0;\">২. প্রাপ্ত জমা ভাউচারসমূহ (Collections / Payments Received):</div>");
            sb.AppendLine("        <table class=\"table\">");
            sb.AppendLine("            <thead><tr><th class=\"text-center\">তারিখ</th><th>পেমেন্ট নং</th><th>পদ্ধতি</th><th>রেফারেন্স</th><th class=\"text-right\">জমা টাকা (৳)</th></tr></thead>");
            sb.AppendLine("            <tbody>");
            foreach (var p in payList)
            {
                sb.AppendLine($"                <tr><td class=\"text-center\">{p.PaymentDate:dd-MM-yyyy}</td><td class=\"font-bold\">{p.PaymentNumber}</td><td>{p.PaymentMethod}</td><td>{p.ReferenceNumber ?? "-"}</td><td class=\"text-right font-bold\" style=\"color:#059669;\">৳ {p.Amount:N2}</td></tr>");
            }
            sb.AppendLine("            </tbody>");
            sb.AppendLine("        </table>");
        }

        // Summary Bar
        sb.AppendLine("        <div class=\"summary-box\">");
        sb.AppendLine($"            <div>মোট কাজের বিল: ৳ {totalBilled:N2}</div>");
        sb.AppendLine($"            <div>মোট আদায়কৃত জমা: ৳ {totalPaid:N2}</div>");
        sb.AppendLine($"            <div>বর্তমানে মোট অবশিষ্ট পাওনা: ৳ {customer.CurrentBalance:N2}</div>");
        sb.AppendLine("        </div>");

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public async Task<string> SaveMemoHtmlToFileAsync(string htmlContent, string fileNamePrefix, CancellationToken cancellationToken = default)
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RupaliAccount", "Memos");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string safePrefix = string.Join("_", fileNamePrefix.Split(Path.GetInvalidFileNameChars()));
        string fileName = $"{safePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        string filePath = Path.Combine(directory, fileName);

        await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8, cancellationToken);
        _logger.LogInformation("Saved Memo HTML to {FilePath}", filePath);

        return filePath;
    }

    private static string ToEnglishWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + ToEnglishWords(Math.Abs(number));

        string words = "";

        if ((number / 10000000) > 0)
        {
            words += ToEnglishWords(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words += ToEnglishWords(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words += ToEnglishWords(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += ToEnglishWords(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            string[] unitsMap = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tensMap = { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words.Trim();
    }

    private static string ToBengaliWords(long number)
    {
        if (number == 0) return "শূন্য";
        if (number < 0) return "মাইনাস " + ToBengaliWords(Math.Abs(number));

        string words = "";

        if ((number / 10000000) > 0)
        {
            words += ToBengaliWords(number / 10000000) + " কোটি ";
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words += ToBengaliWords(number / 100000) + " লক্ষ ";
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words += ToBengaliWords(number / 1000) + " হাজার ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += ToBengaliWords(number / 100) + " শত ";
            number %= 100;
        }

        if (number > 0)
        {
            words += NumberToBengaliDigits(number);
        }

        return words.Trim();
    }

    private static string NumberToBengaliDigits(long number)
    {
        // Fallback converter for simple numbers
        string[] bengaliDigits = { "০", "১", "২", "৩", "৪", "৫", "৬", "৭", "৮", "৯" };
        string s = number.ToString();
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (c >= '0' && c <= '9')
                sb.Append(bengaliDigits[c - '0']);
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
