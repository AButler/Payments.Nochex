using System.Text;
using Nop.Core.Domain.Common;

namespace Nop.Plugin.Payments.Nochex {
  internal static class Extensions {
    public static string FullName( this Address address ) {
      return ( address.FirstName + " " + address.LastName ).Trim();
    }

    public static string Lines(this Address address ) {
      var fullAddress = new StringBuilder( address.Address1 );

      if ( !string.IsNullOrWhiteSpace( address.Address2 ) ) {
        fullAddress.Append( ", " );
        fullAddress.Append( address.Address2 );
      }

      return fullAddress.ToString();
    }
  }
}
