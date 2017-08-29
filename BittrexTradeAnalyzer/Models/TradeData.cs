using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BittrexTradeAnalyzer.Models
{

  public class RealizedTradeData
  {
    public string BuySell { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal Quantity { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal RemainingQuantity { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal PricePerUnit { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal AverageBuy { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal TotalPrice { get; set; }
    public decimal ProfitLoss { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal ProfitLossValue { get; set; }
    public DateTime TimeStamp { get; set; }
  }

  public class TotalTrade
  {
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal RemainingQuantity { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal Value { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.##}", ApplyFormatInEditMode = true)]
    public decimal ProfitLoss { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal ProfitLossValue { get; set; }
    [DisplayFormat(DataFormatString = "{0:#.########}", ApplyFormatInEditMode = true)]
    public decimal LastPrice { get; set; }

  }

  public class TradeData
  {
    public IList<RealizedTradeData> RealizedTrades { get; set; }
    public TotalTrade UnrealizedTrade { get; set; }
    public TotalTrade RealizedTrade { get; set; }
  }

  public class TotalPortfolioTradeData
  {
    public string Coin { get; set; }
    public string BaseCurrency { get; set; }
    public decimal Balance { get; set; }
    public decimal Value { get; set; }
    public TradeData TradePerCoin { get; set; }
  }

}