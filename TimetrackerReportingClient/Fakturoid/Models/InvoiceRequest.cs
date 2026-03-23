using System.Collections.Generic;
using Newtonsoft.Json;

namespace TimetrackerReportingClient.Fakturoid.Models
{
    public class InvoiceRequest
    {
        /// <summary>
        /// Fakturoid subject (customer) ID.
        /// </summary>
        [JsonProperty("subject_id")]
        public int SubjectId { get; set; }

        /// <summary>
        /// Invoice issue date: yyyy-MM-dd
        /// </summary>
        [JsonProperty("issued_on")]
        public string IssuedOn { get; set; }

        /// <summary>
        /// Invoice due date: yyyy-MM-dd
        /// </summary>
        [JsonProperty("due_on")]
        public string DueOn { get; set; }

        /// <summary>
        /// Payment method. Use "bank_transfer" for wire transfer.
        /// </summary>
        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; } = "bank_transfer";

        [JsonProperty("lines")]
        public List<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    }

    public class InvoiceLine
    {
        /// <summary>
        /// Line item description (work item name).
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Quantity (hours, rounded to 2 decimals).
        /// </summary>
        [JsonProperty("quantity")]
        public string Quantity { get; set; }

        /// <summary>
        /// Unit label shown on the invoice.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = "hod";

        /// <summary>
        /// Price per unit (hourly rate).
        /// </summary>
        [JsonProperty("unit_price")]
        public string UnitPrice { get; set; }

        /// <summary>
        /// VAT rate. 0 for non-VAT payers (Neplátce DPH).
        /// </summary>
        [JsonProperty("vat_rate")]
        public int VatRate { get; set; } = 0;
    }
}
