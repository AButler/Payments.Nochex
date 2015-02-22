using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Nochex.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.Nochex {
  public class NochexPaymentProcessor: BasePlugin, IPaymentMethod {
    private const string NochexUrl = "https://secure.nochex.com/";

    private const string SuccessOrderUrl = "Plugins/PaymentNochex/SuccessOrder";
    private const string CancelUrl = "Plugins/PaymentNochex/CancelOrder";
    internal const string CallbackUrl = "Plugins/PaymentNochex/APCHandler";

    private readonly NochexPaymentSettings _settings;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly ITokenizer _tokenizer;

    public NochexPaymentProcessor( NochexPaymentSettings settings, ISettingService settingService, IWebHelper webHelper, IOrderTotalCalculationService orderTotalCalculationService, ITokenizer tokenizer ) {
      _settings = settings;
      _settingService = settingService;
      _webHelper = webHelper;
      _orderTotalCalculationService = orderTotalCalculationService;
      _tokenizer = tokenizer;
    }

    public ProcessPaymentResult ProcessPayment( ProcessPaymentRequest processPaymentRequest ) {
      // Payment processing is done "Post" order
      return new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
    }

    public void PostProcessPayment( PostProcessPaymentRequest postProcessPaymentRequest ) {
      var storeUrl = _webHelper.GetStoreLocation( false );
      var order = postProcessPaymentRequest.Order;
      var orderTotal = Math.Round( order.OrderTotal, 2 );
      var orderDescription = GetOrderDescription( order );

      var post = new RemotePost { Url = NochexUrl };

      // Merchant Id
      post.Add( "merchant_id", _settings.MerchantId );

      // Urls
      post.Add( _settings.UseTestMode ? "test_success_url" : "success_url", storeUrl + SuccessOrderUrl );
      post.Add( "cancel_url", storeUrl + CancelUrl );

      // Order Details
      post.Add( "order_id", order.Id.ToString( CultureInfo.InvariantCulture ) );
      post.Add( "optional_1", order.OrderGuid.ToString() );
      post.Add( "description", orderDescription );
      post.Add( "amount", orderTotal.ToString( "0.00", CultureInfo.InvariantCulture ) );

      // Billing Address
      var billingAddress = order.BillingAddress;
      post.Add( "billing_fullname", billingAddress.FullName() );
      post.Add( "billing_address", billingAddress.Lines() );
      post.Add( "billing_city", billingAddress.City );
      post.Add( "billing_postcode", billingAddress.ZipPostalCode );
      post.Add( "email_address", billingAddress.Email );
      post.Add( "customer_phone_number", billingAddress.PhoneNumber );

      // Shipping Address
      if ( order.ShippingStatus != ShippingStatus.ShippingNotRequired ) {
        var shippingAddress = order.ShippingAddress;
        post.Add( "delivery_fullname", shippingAddress.FullName() );
        post.Add( "delivery_address", shippingAddress.Lines() );
        post.Add( "delivery_city", billingAddress.City );
        post.Add( "delivery_postcode", shippingAddress.ZipPostalCode );
      }

      if ( _settings.UseTestMode ) {
        post.Add( "test_transaction", "100" );
      }
      if ( _settings.UseCallback ) {
        post.Add( "callback_url", storeUrl + CallbackUrl );
      }
      if ( _settings.HideBillingDetails ) {
        post.Add( "hide_billing_details", "true" );
      }

      post.Post();
    }

    public bool HidePaymentMethod( IList<ShoppingCartItem> cart ) {
      return false;
    }

    public decimal GetAdditionalHandlingFee( IList<ShoppingCartItem> cart ) {
      return this.CalculateAdditionalFee( _orderTotalCalculationService, cart, _settings.AdditionalFee, _settings.AdditionalFeePercentage );
    }

    public bool CanRePostProcessPayment( Order order ) {
      if ( order == null ) {
        throw new ArgumentNullException( "order" );
      }

      if ( order.PaymentStatus != PaymentStatus.Pending ) {
        return false;
      }

      var timeSinceOrderCreated = DateTime.UtcNow - order.CreatedOnUtc;
      if ( timeSinceOrderCreated.TotalMinutes < 1.0 ) {
        return false;
      }
      return true;
    }

    public void GetConfigurationRoute( out string actionName, out string controllerName, out RouteValueDictionary routeValues ) {
      actionName = "Configure";
      controllerName = "PaymentNochex";
      routeValues = new RouteValueDictionary {
        { "Namespaces", "Nop.Plugin.Payments.Nochex.Controllers" },
        { "area", null }
      };
    }

    public void GetPaymentInfoRoute( out string actionName, out string controllerName, out RouteValueDictionary routeValues ) {
      actionName = "PaymentInfo";
      controllerName = "PaymentNochex";
      routeValues = new RouteValueDictionary {
        { "Namespaces", "Nop.Plugin.Payments.Nochex.Controllers" },
        { "area", null }
      };
    }

    public Type GetControllerType() {
      return typeof( PaymentNochexController );
    }

    public override void Install() {
      var defaultSettings = new NochexPaymentSettings {
        UseTestMode = true,
        MerchantId = "MerchantId",
        UseCallback = true,
        OrderDescription = "Order Number %OrderNumber%",
        AdditionalFee = 0,
        AdditionalFeePercentage = false,
        RedirectToOrderDetails = true,
        RedirectToTopicOnSuccess = false,
        SuccessTopicName = "ordersuccess",
        HideBillingDetails = false
      };

      // Add default settings
      _settingService.SaveSetting( defaultSettings );

      // Add default resources
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.RedirectionTip",
        "You will be redirected to the Nochex site to complete the order."
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.NochexInfo",
        "To use this payment provider you need to set up a merchant account with Nochex. Go to <a target='_blank' href='http://www.nochex.com'>www.nochex.com</a> to register for an account."
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.ApcUrl",
        "APC Url"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.ApcUrl.Hint",
        "Enter this URL in your Merchant Account on Nochex."
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.UseTestMode",
        "Enable Test Mode"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.UseTestMode.Hint",
        "When Test Mode is enabled no real transactions will occur so you can test the integration works correctly"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.MerchantId",
        "Merchant ID"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.UseCallback",
        "Enable APC"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.UseCallback.Hint",
        "If enabled, Nochex's Automatic Payment Confirmation will automatically callback to the store and mark the order as paid (Note: needs to be enable in your Nochex account)"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.MerchantId.Hint",
        "The Merchant ID issued by Nochex which identifies which account to use"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.OrderDescription",
        "Order Description"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.OrderDescription.Hint",
        "Enter a description that will appear on Nochex that describes the order (Note: use %OrderNumber% to insert the order number)"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.AdditionalFee",
        "Additional Fee"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.AdditionalFee.Hint",
        "Enter an additional fee to charge your customers when using Nochex"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.AdditionalFeePercentage",
        "Additional fee. Use Percentage"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.AdditionalFeePercentage.Hint",
        "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used."
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.RedirectToOrderDetails",
        "Return to Order Details"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.RedirectToOrderDetails.Hint",
        "If enabled, when a customer returns from Nochex, they will be directed to the order details page, if not, to the store home page."
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.RedirectToTopicOnSuccess",
        "Redirect to Topic on Success"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.RedirectToTopicOnSuccess.Hint",
        "If enabled, when a customer returns from Nochex after paying successfully, they will be directed to a topic page."
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.SuccessTopicName",
        "Success Topic Name"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.SuccessTopicName.Hint",
        "The name of the topic to redirect to on successful payment"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.HideBillingDetails",
        "Hide Billing Details"
      );
      this.AddOrUpdatePluginLocaleResource(
        "Plugins.Payments.Nochex.Fields.HideBillingDetails.Hint",
        "If enabled, the customer won't be able to modify their billing details from the Nochex payment pages (Use to ensure the billing details provided are correct)"
      );

      base.Install();
    }

    public override void Uninstall() {
      // Delete settings
      _settingService.DeleteSetting<NochexPaymentSettings>();

      // Delete resources
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.RedirectionTip" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.NochexInfo" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.ApcUrl" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.ApcUrl.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.UseTestMode" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.UseTestMode.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.MerchantId" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.MerchantId.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.UseCallback" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.UseCallback.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.OrderDescription" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.OrderDescription.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.AdditionalFee" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.AdditionalFee.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.AdditionalFeePercentage" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.AdditionalFeePercentage.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.RedirectToOrderDetails" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.RedirectToOrderDetails.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.RedirectToTopicOnSuccess" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.RedirectToTopicOnSuccess.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.SuccessTopicName" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.SuccessTopicName.Hint" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.HideBillingDetails" );
      this.DeletePluginLocaleResource( "Plugins.Payments.Nochex.Fields.HideBillingDetails.Hint" );

      base.Uninstall();
    }

    private string GetOrderDescription( Order order ) {
      var tokens = new List<Token> {
        new Token( "OrderNumber", order.Id.ToString( CultureInfo.InvariantCulture ) )
      };

      return _tokenizer.Replace( _settings.OrderDescription, tokens, false );
    }

    #region Not Supported Methods
    public CapturePaymentResult Capture( CapturePaymentRequest capturePaymentRequest ) {
      var result = new CapturePaymentResult();
      result.AddError( "Capture method not supported" );
      return result;
    }

    public RefundPaymentResult Refund( RefundPaymentRequest refundPaymentRequest ) {
      var result = new RefundPaymentResult();
      result.AddError( "Refund method not supported" );
      return result;
    }

    public VoidPaymentResult Void( VoidPaymentRequest voidPaymentRequest ) {
      var result = new VoidPaymentResult();
      result.AddError( "Void method not supported" );
      return result;
    }

    public ProcessPaymentResult ProcessRecurringPayment( ProcessPaymentRequest processPaymentRequest ) {
      var result = new ProcessPaymentResult();
      result.AddError( "Recurring payment not supported" );
      return result;
    }

    public CancelRecurringPaymentResult CancelRecurringPayment( CancelRecurringPaymentRequest cancelPaymentRequest ) {
      var result = new CancelRecurringPaymentResult();
      result.AddError( "Recurring payment not supported" );
      return result;
    }
    #endregion

    #region Payment Processor Properties
    public bool SupportCapture {
      get { return false; }
    }

    public bool SupportPartiallyRefund {
      get { return false; }
    }

    public bool SupportRefund {
      get { return false; }
    }

    public bool SupportVoid {
      get { return false; }
    }

    public RecurringPaymentType RecurringPaymentType {
      get { return RecurringPaymentType.NotSupported; }
    }

    public PaymentMethodType PaymentMethodType {
      get { return PaymentMethodType.Redirection; }
    }

    public bool SkipPaymentInfo {
      get { return false; }
    }
    #endregion
  }
}
