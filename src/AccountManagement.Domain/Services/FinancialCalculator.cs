namespace AccountManagement.Domain.Services;

public static class FinancialCalculator
{
    /// <summary>
    /// Calculates the cost of a material item based on quantity and rate per unit.
    /// Rounded to 2 decimal places using MidpointRounding.AwayFromZero.
    /// </summary>
    public static decimal CalculateMaterialCost(decimal quantity, decimal unitRate)
    {
        if (quantity < 0 || unitRate < 0)
        {
            throw new ArgumentOutOfRangeException("Quantity and unit rate must be non-negative.");
        }

        return Math.Round(quantity * unitRate, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates total direct costs by summing material costs and production cost entries.
    /// </summary>
    public static decimal CalculateTotalDirectCost(IEnumerable<decimal> materialCosts, IEnumerable<decimal> productionCosts)
    {
        decimal sumMaterial = materialCosts.Sum();
        decimal sumProduction = productionCosts.Sum();
        return Math.Round(sumMaterial + sumProduction, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates final bill amount after applying optional discount.
    /// Total bill cannot be negative.
    /// </summary>
    public static decimal CalculateTotalBill(decimal subTotal, decimal discount = 0m)
    {
        if (subTotal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subTotal), "SubTotal must be non-negative.");
        }

        if (discount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discount), "Discount must be non-negative.");
        }

        return Math.Max(0m, Math.Round(subTotal - discount, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Calculates remaining outstanding balance: Total Bill - Total Payments Allocated.
    /// </summary>
    public static decimal CalculateOutstandingBalance(decimal totalBill, decimal totalPaid)
    {
        if (totalBill < 0 || totalPaid < 0)
        {
            throw new ArgumentOutOfRangeException("Amounts must be non-negative.");
        }

        return Math.Max(0m, Math.Round(totalBill - totalPaid, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Calculates gross profit: Revenue (Total Bill) - Direct Costs.
    /// </summary>
    public static decimal CalculateGrossProfit(decimal totalBill, decimal totalDirectCost)
    {
        return Math.Round(totalBill - totalDirectCost, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Standard paper converting: converts total sheets to reams (typically 500 sheets = 1 ream).
    /// </summary>
    public static decimal ConvertSheetsToReams(decimal sheets, int sheetsPerReam = 500)
    {
        if (sheetsPerReam <= 0) throw new ArgumentOutOfRangeException(nameof(sheetsPerReam));
        return Math.Round(sheets / sheetsPerReam, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Converts reams to sheets.
    /// </summary>
    public static decimal ConvertReamsToSheets(decimal reams, int sheetsPerReam = 500)
    {
        if (sheetsPerReam <= 0) throw new ArgumentOutOfRangeException(nameof(sheetsPerReam));
        return Math.Round(reams * sheetsPerReam, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates paper rate for 1000 pcs of a given cut size derived from 18x23 base rate.
    /// Standard printing press cut ratios:
    /// - 18/23: 1 cut (Divisor 1.0)
    /// - 9/14: 3 cuts per sheet (Effective divisor 2.95 with trim margin, or 3.0 exact)
    /// - 9/11.5: 4 cuts per sheet (Quarter Demy / 4 Ta, exact divisor 4.0)
    /// - 7.5/10: 5 cuts per sheet (Divisor 5.0, or 4.90-4.95 with trim margin)
    /// - 7.5/9: 6 cuts per sheet (Effective divisor 5.9 with trim margin, or 6.0 exact)
    /// </summary>
    public static decimal CalculateCutSizePrice(decimal base1000Rate, string cutSize, bool useTrimMargin = true)
    {
        if (base1000Rate < 0) throw new ArgumentOutOfRangeException(nameof(base1000Rate));
        if (string.IsNullOrWhiteSpace(cutSize)) return base1000Rate;

        string normalized = cutSize.Trim().Replace("x", "/").Replace("X", "/").Replace(" ", "");

        decimal divisor = normalized switch
        {
            "18/23" => 1.0m,
            "9/14" => useTrimMargin ? 2.95m : 3.0m,
            "9/11.5" => 4.0m,
            "7.5/10" => useTrimMargin ? 4.95m : 5.0m,
            "7.5/9" => useTrimMargin ? 5.9m : 6.0m,
            _ => 1.0m
        };

        return Math.Round(base1000Rate / divisor, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates offset plate cost based on plate type and quantity.
    /// Standard Rupali press rates:
    /// - Single Plate: 70 BDT
    /// - Full Plate: 140 BDT
    /// - Demy Plate: 200 BDT
    /// </summary>
    public static decimal CalculatePlateCost(string plateType, int quantity)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(plateType)) return 0m;

        decimal unitRate = plateType.Trim().ToLowerInvariant() switch
        {
            var p when p.Contains("single") || p.Contains("সিঙ্গেল") => 70m,
            var p when p.Contains("full") || p.Contains("ফুল") => 140m,
            var p when p.Contains("demy") || p.Contains("ডিমাই") => 200m,
            _ => 70m
        };

        return Math.Round(unitRate * quantity, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates printing cost (ছাপার বিল) per 1000 sheets at 150 BDT per color.
    /// 1 Color = 150 BDT, 2 Color = 300 BDT, 3 Color = 450 BDT, 4 Color = 600 BDT per 1000 sheets.
    /// </summary>
    public static decimal CalculatePrintingCost(int colorCount, decimal sheetCount, decimal ratePerColorPerThousand = 150m)
    {
        if (colorCount < 0) throw new ArgumentOutOfRangeException(nameof(colorCount));
        if (sheetCount < 0) throw new ArgumentOutOfRangeException(nameof(sheetCount));

        // In offset printing, charges are typically billed per 1000 sheets or fraction thereof
        decimal thousands = Math.Max(1m, Math.Ceiling(sheetCount / 1000m));
        return Math.Round(thousands * colorCount * ratePerColorPerThousand, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates Cash Memo binding bill (ক্যাশ মেমো বাইন্ডিং).
    /// Standard rate: 12 BDT per book (2-part / 2 পাতা ১টি বই).
    /// </summary>
    public static decimal CalculateCashMemoBindingCost(int bookCount, decimal ratePerBook = 12m)
    {
        if (bookCount < 0) throw new ArgumentOutOfRangeException(nameof(bookCount));
        return Math.Round(bookCount * ratePerBook, 2, MidpointRounding.AwayFromZero);
    }
}

