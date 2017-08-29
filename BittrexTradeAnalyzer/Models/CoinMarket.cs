using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BittrexTradeAnalyzer.Models
{
  public class CoinMarket
  {
    public string symbol { get; set; }
    public string id { get; set; }
    public decimal? price_eur { get; set; }
    public decimal? price_usd { get; set; }
    public decimal? price_btc { get; set; }
    public decimal? percent_change_1h { get; set; }
    public decimal? percent_change_24h { get; set; }
    public decimal? percent_change_7d { get; set; }
  }

  class CMCompare : IEqualityComparer<CoinMarket>
  {

    public bool Equals(CoinMarket x, CoinMarket y)
    {
      return x.symbol == y.symbol;
    }

    public int GetHashCode(CoinMarket obj)
    {
      return obj.symbol.GetHashCode();
    }
  }

}