using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Nochex.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Nochex.Controllers {
  public class PaymentNochexController: BasePaymentController {
    private readonly NochexPaymentSettings _settings;

    private readonly IOrderService _orderService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly ILogger _logger;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ILocalizationService _localizationService;
    private readonly IWebHelper _webHelper;

    private const string NochexApcUrl = "https://www.nochex.com/nochex.dll/apc/apc";

    public PaymentNochexController( 
      NochexPaymentSettings settings, 
      IOrderService orderService, 
      IOrderProcessingService orderProcessingService, 
      ILogger logger, 
      IStoreContext storeContext, 
      IWorkContext workContext, 
      ISettingService settingService,
      IStoreService storeService, 
      ILocalizationService localizationService, 
      IWebHelper webHelper 
    ) {
      _settings = settings;
      _orderService = orderService;
      _orderProcessingService = orderProcessingService;
      _logger = logger;
      _storeContext = storeContext;
      _workContext = workContext;
      _settingService = settingService;
      _storeService = storeService;
      _localizationService = localizationService;
      _webHelper = webHelper;
    }

    public ActionResult APCHandler() {
      ApplyApcCallback( Request.Form.ToString() );
      return Content( "" );
    }

    public ActionResult SuccessOrder() {
      if ( _settings.RedirectToTopicOnSuccess ) {
        // Redirect to success topic
        return RedirectToAction( _settings.SuccessTopicName, "t", new { area = "" } );
      }

      if ( _settings.RedirectToOrderDetails ) {
        var lastOrder = _orderService
          .SearchOrders( storeId : _storeContext.CurrentStore.Id, customerId : _workContext.CurrentCustomer.Id, pageSize : 1 )
          .FirstOrDefault();

        if ( lastOrder != null ) {
          // Redirect to order details
          return RedirectToRoute( "OrderDetails", new { orderId = lastOrder.Id } );
        }
      }

      // Redirect to the home page
      return RedirectToAction( "Index", "Home", new { area = "" } );
    }

    public ActionResult CancelOrder( FormCollection form ) {
      if ( _settings.RedirectToOrderDetails ) {
        var lastOrder = _orderService
          .SearchOrders( storeId : _storeContext.CurrentStore.Id, customerId : _workContext.CurrentCustomer.Id, pageSize : 1 )
          .FirstOrDefault();

        if ( lastOrder != null ) {
          // Redirect to order details
          return RedirectToRoute( "OrderDetails", new { orderId = lastOrder.Id } );
        }
      }

      // Redirect to the home page
      return RedirectToAction( "Index", "Home", new { area = "" } );
    }

    [AdminAuthorize]
    [ChildActionOnly]
    public ActionResult Configure() {
      var storeScope = GetActiveStoreScopeConfiguration( _storeService, _workContext );
      var settings = _settingService.LoadSetting<NochexPaymentSettings>( storeScope );

      var model = new ConfigurationModel {
        ActiveStoreScopeConfiguration = storeScope,
        ApcUrl = _webHelper.GetStoreLocation( false ) + NochexPaymentProcessor.CallbackUrl,
        UseTestMode = settings.UseTestMode,
        MerchantId = settings.MerchantId,
        AdditionalFeePercentage = settings.AdditionalFeePercentage,
        AdditionalFee = settings.AdditionalFee,
        OrderDescription = settings.OrderDescription,
        UseCallback = settings.UseCallback,
        RedirectToOrderDetails = settings.RedirectToOrderDetails,
        RedirectToTopicOnSuccess = settings.RedirectToTopicOnSuccess,
        SuccessTopicName = settings.SuccessTopicName,
        HideBillingDetails = settings.HideBillingDetails
      };

      if ( storeScope > 0 ) {
        model.UseTestMode_OverrideForStore = _settingService.SettingExists( settings, x => x.UseTestMode, storeScope );
        model.MerchantId_OverrideForStore = _settingService.SettingExists( settings, x => x.MerchantId );
        model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists( settings, x => x.AdditionalFeePercentage );
        model.AdditionalFee_OverrideForStore = _settingService.SettingExists( settings, x => x.AdditionalFee );
        model.OrderDescription_OverrideForStore = _settingService.SettingExists( settings, x => x.OrderDescription );
        model.UseCallback_OverrideForStore = _settingService.SettingExists( settings, x => x.UseCallback );
        model.RedirectToOrderDetails_OverrideForStore = _settingService.SettingExists( settings, x => x.RedirectToOrderDetails );
        model.RedirectToTopicOnSuccess_OverrideForStore = _settingService.SettingExists( settings, x => x.RedirectToTopicOnSuccess );
        model.SuccessTopicName_OverrideForStore = _settingService.SettingExists( settings, x => x.SuccessTopicName );
        model.HideBillingDetails_OverrideForStore = _settingService.SettingExists( settings, x => x.HideBillingDetails );
      }

      return View( "~/Plugins/Payments.Nochex/Views/PaymentNochex/Configure.cshtml", model );
    }

    [HttpPost]
    [AdminAuthorize]
    [ChildActionOnly]
    public ActionResult Configure( ConfigurationModel model ) {
      if ( !ModelState.IsValid ) {
        return Configure();
      }

      var storeScope = GetActiveStoreScopeConfiguration( _storeService, _workContext );
      var settings = _settingService.LoadSetting<NochexPaymentSettings>( storeScope );

      settings.UseTestMode = model.UseTestMode;
      settings.MerchantId = model.MerchantId;
      settings.AdditionalFeePercentage = model.AdditionalFeePercentage;
      settings.AdditionalFee = model.AdditionalFee;
      settings.OrderDescription = model.OrderDescription;
      settings.UseCallback = model.UseCallback;
      settings.RedirectToOrderDetails = model.RedirectToOrderDetails;
      settings.RedirectToTopicOnSuccess = model.RedirectToTopicOnSuccess;
      settings.SuccessTopicName = model.SuccessTopicName;
      settings.HideBillingDetails = model.HideBillingDetails;

      if ( model.UseTestMode_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.UseTestMode, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.UseTestMode, storeScope );
      }
      if ( model.MerchantId_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.MerchantId, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.MerchantId, storeScope );
      }
      if ( model.AdditionalFeePercentage_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.AdditionalFeePercentage, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.AdditionalFeePercentage, storeScope );
      }
      if ( model.AdditionalFee_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.AdditionalFee, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.AdditionalFee, storeScope );
      }
      if ( model.OrderDescription_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.OrderDescription, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.OrderDescription, storeScope );
      }
      if ( model.UseCallback_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.UseCallback, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.UseCallback, storeScope );
      }
      if ( model.RedirectToOrderDetails_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.RedirectToOrderDetails, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.RedirectToOrderDetails, storeScope );
      }
      if ( model.RedirectToTopicOnSuccess_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.RedirectToTopicOnSuccess, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.RedirectToTopicOnSuccess, storeScope );
      }
      if ( model.SuccessTopicName_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.SuccessTopicName, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.SuccessTopicName, storeScope );
      }
      if ( model.HideBillingDetails_OverrideForStore || storeScope == 0 ) {
        _settingService.SaveSetting( settings, x => x.HideBillingDetails, storeScope, false );
      } else if ( storeScope > 0 ) {
        _settingService.DeleteSetting( settings, x => x.HideBillingDetails, storeScope );
      }

      _settingService.ClearCache();
      SuccessNotification( _localizationService.GetResource( "admin.configuration.updated" ) );

      return Configure();
    }

    [ChildActionOnly]
    public ActionResult PaymentInfo() {
      return View( "~/Plugins/Payments.Nochex/Views/PaymentNochex/PaymentInfo.cshtml" );
    }

    private void ApplyApcCallback( string requestMessage ) {
      var apcMessage = ParseApcMessage( requestMessage );
      if ( apcMessage == null ) {
        _logger.InsertLog( LogLevel.Warning, "Nochex APC Message is invalid", requestMessage );
        return;
      }

      var orderGuid = apcMessage["custom"];
      var transactionId = apcMessage["transaction_id"];
      var status = apcMessage["status"];

      if ( orderGuid == null || transactionId == null || status == null ) {
        _logger.InsertLog( LogLevel.Warning, "Nochex APC: Missing required data", requestMessage );
        return;
      }

      var order = _orderService.GetOrderByGuid( new Guid( orderGuid ) );
      if ( order == null ) {
        _logger.InsertLog( LogLevel.Warning, "Nochex APC: Order not found: " + orderGuid, requestMessage );
        return;
      }

      var orderNote = new OrderNote {
        Note = "Nochex APC Message Received: \r\n\r\n" + BuildFriendlyApcMessage( apcMessage ),
        DisplayToCustomer = false,
        CreatedOnUtc = DateTime.UtcNow
      };
      order.OrderNotes.Add( orderNote );
      _orderService.UpdateOrder( order );

      var currentStatus = _settings.UseTestMode ? "test" : "live";
      if ( !string.Equals( status, currentStatus, StringComparison.InvariantCultureIgnoreCase ) ) {
        _logger.InsertLog( LogLevel.Warning, "Nochex APC: Received wrong status " + status, requestMessage );
        return;
      }
      if ( !_orderProcessingService.CanMarkOrderAsPaid( order ) ) {
        _logger.InsertLog( LogLevel.Warning, "Nochex APC: Cannot mark order as paid: " + orderGuid, requestMessage );
        return;
      }

      order.AuthorizationTransactionId = transactionId;
      _orderService.UpdateOrder( order );
      _orderProcessingService.MarkOrderAsPaid( order );
    }

    private static string BuildFriendlyApcMessage( NameValueCollection apcMessage ) {
      return string.Join( "\r\n", apcMessage.Keys.Cast<string>().Select( i => i + " = " + apcMessage[i] ) );
    }

    private static NameValueCollection ParseApcMessage( string message ) {
      var request = (HttpWebRequest) WebRequest.Create( NochexApcUrl );
      request.Method = "POST";
      request.ContentType = "application/x-www-form-urlencoded";
      request.ContentLength = message.Length;

      var requestStream = request.GetRequestStream();
      requestStream.Write( Encoding.UTF8.GetBytes( message ), 0, message.Length );
      requestStream.Close();

      var responseStream = request.GetResponse().GetResponseStream();
      if ( responseStream == null ) {
        // No response
        return null;
      }

      using ( var reader = new StreamReader( responseStream ) ) {
        var responseText = HttpUtility.UrlDecode( reader.ReadToEnd() );
        if ( !string.Equals( responseText, "AUTHORISED", StringComparison.InvariantCultureIgnoreCase ) ) {
          // Not authorised
          return null;
        }
      }

      return HttpUtility.ParseQueryString( message );
    }

    #region Payment Controller Overrides
    [NonAction]
    public override IList<string> ValidatePaymentForm( FormCollection form ) {
      return new List<string>();
    }

    [NonAction]
    public override ProcessPaymentRequest GetPaymentInfo( FormCollection form ) {
      return new ProcessPaymentRequest();
    }
    #endregion
  }
}