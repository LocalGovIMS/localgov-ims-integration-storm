﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class ProcessedTransactionModel
    {
        public string Reference { get; set; }
        public string InternalReference { get; set; }
        public string PspReference { get; set; }
        public string OfficeCode { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string AccountReference { get; set; }
        public int UserCode { get; set; }
        public string FundCode { get; set; }
        public string MopCode { get; set; }
        public decimal? Amount { get; set; }
        public string VatCode { get; set; }
        public float VatRate { get; set; }
        public decimal? VatAmount { get; set; }
        public string Narrative { get; set; }
        public string BatchReference { get; set; }

        public ProcessedTransactionModel() { }
    }
}
