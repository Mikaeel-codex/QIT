namespace PointofSale.Models
{
    /// <summary>One line of a split payment e.g. "Gift Card R20" + "Cash R20".</summary>
    public class PaymentSplit
    {
        public string Method { get; set; } = "";
        public decimal Amount { get; set; }
        public int GiftCardId { get; set; } = 0;

        /// <summary>Display label shown in totals panel e.g. "Cash" or "Gift Card #2232".</summary>
        public string Label { get; set; } = "";
    }
}