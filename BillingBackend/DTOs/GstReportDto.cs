using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BillingBackend.DTOs
{
    public class TaxSlabSummaryDto
    {
        [JsonPropertyName("taxRate")]
        public decimal TaxRate { get; set; }

        [JsonPropertyName("taxableValue")]
        public decimal TaxableValue { get; set; }

        [JsonPropertyName("cgstAmount")]
        public decimal CGSTAmount { get; set; }

        [JsonPropertyName("sgstAmount")]
        public decimal SGSTAmount { get; set; }

        [JsonPropertyName("igstAmount")]
        public decimal IGSTAmount { get; set; }

        [JsonPropertyName("totalTaxAmount")]
        public decimal TotalTaxAmount { get; set; }

        [JsonPropertyName("lineItemsCount")]
        public int LineItemsCount { get; set; }
    }

    public class HsnSacReportSummaryDto
    {
        [JsonPropertyName("hsnSacCode")]
        public string HsnSacCode { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Goods"; // Goods or Services

        [JsonPropertyName("totalQuantity")]
        public int TotalQuantity { get; set; }

        [JsonPropertyName("taxableValue")]
        public decimal TaxableValue { get; set; }

        [JsonPropertyName("cgstAmount")]
        public decimal CGSTAmount { get; set; }

        [JsonPropertyName("sgstAmount")]
        public decimal SGSTAmount { get; set; }

        [JsonPropertyName("igstAmount")]
        public decimal IGSTAmount { get; set; }

        [JsonPropertyName("totalTaxAmount")]
        public decimal TotalTaxAmount { get; set; }
    }

    public class DailyGstSalesTrendDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("billsCount")]
        public int BillsCount { get; set; }

        [JsonPropertyName("taxableValue")]
        public decimal TaxableValue { get; set; }

        [JsonPropertyName("gstCollected")]
        public decimal GSTCollected { get; set; }

        [JsonPropertyName("totalSales")]
        public decimal TotalSales { get; set; }
    }

    public class GstReportSummaryDto
    {
        [JsonPropertyName("businessId")]
        public int BusinessId { get; set; }

        [JsonPropertyName("businessName")]
        public string BusinessName { get; set; } = string.Empty;

        [JsonPropertyName("gstIn")]
        public string GstIn { get; set; } = string.Empty;

        [JsonPropertyName("periodLabel")]
        public string PeriodLabel { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("totalBillsCount")]
        public int TotalBillsCount { get; set; }

        [JsonPropertyName("totalGrossSales")]
        public decimal TotalGrossSales { get; set; }

        [JsonPropertyName("totalDiscountAmount")]
        public decimal TotalDiscountAmount { get; set; }

        [JsonPropertyName("totalTaxableValue")]
        public decimal TotalTaxableValue { get; set; }

        [JsonPropertyName("totalCGST")]
        public decimal TotalCGST { get; set; }

        [JsonPropertyName("totalSGST")]
        public decimal TotalSGST { get; set; }

        [JsonPropertyName("totalIGST")]
        public decimal TotalIGST { get; set; }

        [JsonPropertyName("totalCess")]
        public decimal TotalCess { get; set; }

        [JsonPropertyName("totalGSTCollected")]
        public decimal TotalGSTCollected { get; set; }

        [JsonPropertyName("totalGrandSales")]
        public decimal TotalGrandSales { get; set; }

        // B2B vs B2C Split
        [JsonPropertyName("b2bBillsCount")]
        public int B2BBillsCount { get; set; }

        [JsonPropertyName("b2bTaxableValue")]
        public decimal B2BTaxableValue { get; set; }

        [JsonPropertyName("b2bGstCollected")]
        public decimal B2BGSTCollected { get; set; }

        [JsonPropertyName("b2cBillsCount")]
        public int B2CBillsCount { get; set; }

        [JsonPropertyName("b2cTaxableValue")]
        public decimal B2CTaxableValue { get; set; }

        [JsonPropertyName("b2cGstCollected")]
        public decimal B2CGSTCollected { get; set; }

        [JsonPropertyName("taxSlabs")]
        public List<TaxSlabSummaryDto> TaxSlabs { get; set; } = new();

        [JsonPropertyName("hsnSacBreakdown")]
        public List<HsnSacReportSummaryDto> HsnSacBreakdown { get; set; } = new();

        [JsonPropertyName("dailyTrends")]
        public List<DailyGstSalesTrendDto> DailyTrends { get; set; } = new();

        [JsonPropertyName("paymentMethodDistribution")]
        public Dictionary<string, decimal> PaymentMethodDistribution { get; set; } = new();
    }
}
