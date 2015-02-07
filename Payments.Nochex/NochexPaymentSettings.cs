using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Nochex {
  public class NochexPaymentSettings: ISettings {
    public string MerchantId { get; set; }
    
    public bool UseTestMode { get; set; }

    public bool UseCallback { get; set; }

    public string OrderDescription { get; set; }

    public decimal AdditionalFee { get; set; }

    public bool AdditionalFeePercentage { get; set; }

    public bool HideBillingDetails { get; set; }

    public bool RedirectToOrderDetails { get; set; }

    public bool RedirectToTopicOnSuccess { get; set; }

    public string SuccessTopicName { get; set; }


  }
}