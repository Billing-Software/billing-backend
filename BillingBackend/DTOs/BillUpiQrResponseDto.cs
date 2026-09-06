namespace BillingBackend.DTOs
{
    public class BillUpiQrResponseDto
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string UpiVpa { get; set; } = string.Empty;
        public string PayeeName { get; set; } = string.Empty;
        public string TransactionNote { get; set; } = string.Empty;

        /// <summary>
        /// Complete NPCI standard deep link: upi://pay?pa=...&pn=...&am=...&cu=INR&tn=...&tr=...
        /// Ready to be encoded directly into QR codes or opened in UPI PSP apps (GPay, PhonePe, Paytm).
        /// </summary>
        public string UpiUri { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
    }
}
