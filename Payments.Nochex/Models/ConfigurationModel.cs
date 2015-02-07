using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Nochex.Models {
  public class ConfigurationModel: BaseNopModel {
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.ApcUrl" )]
    public string ApcUrl { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.UseTestMode" )]
    public bool UseTestMode { get; set; }
    public bool UseTestMode_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.MerchantId" )]
    public string MerchantId { get; set; }
    public bool MerchantId_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.AdditionalFeePercentage" )]
    public bool AdditionalFeePercentage { get; set; }
    public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.AdditionalFee" )]
    public decimal AdditionalFee { get; set; }
    public bool AdditionalFee_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.OrderDescription" )]
    public string OrderDescription { get; set; }
    public bool OrderDescription_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.UseCallback" )]
    public bool UseCallback { get; set; }
    public bool UseCallback_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.RedirectToOrderDetails" )]
    public bool RedirectToOrderDetails { get; set; }
    public bool RedirectToOrderDetails_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.RedirectToTopicOnSuccess" )]
    public bool RedirectToTopicOnSuccess { get; set; }
    public bool RedirectToTopicOnSuccess_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.SuccessTopicName" )]
    public string SuccessTopicName { get; set; }
    public bool SuccessTopicName_OverrideForStore { get; set; }

    [NopResourceDisplayName( "Plugins.Payments.Nochex.Fields.HideBillingDetails" )]
    public bool HideBillingDetails { get; set; }
    public bool HideBillingDetails_OverrideForStore { get; set; }
  }
}