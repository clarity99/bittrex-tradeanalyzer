using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BittrexTradeAnalyzer.Models
{
  public class TradeInput
  {
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
    public bool RememberKey { get; set; }
    public string Coin { get; set; }
    public string BaseCurrency { get; set; }
    public int NoOfTrades { get; set; }

  }
}