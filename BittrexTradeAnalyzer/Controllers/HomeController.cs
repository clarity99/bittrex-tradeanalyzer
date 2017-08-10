using Bittrex;
using BittrexTradeAnalyzer.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BittrexTradeAnalyzer.Controllers
{
  public class HomeController : Controller
  {
    public ActionResult Index()
    {
      var v = new TradeInput();
      v.NoOfTrades = 20;
      return View(v);
    }

    [HttpPost]
    public ActionResult Index(TradeInput inp)
    {

      TradeData tradedata = CalculateTradeData(inp);
      //ViewBag.Result = outp;
      ViewBag.TradeData = tradedata;
      return View(inp);
    }

    private static TradeData CalculateTradeData(TradeInput inp)
    {
      var bittrex = new Exchange();
      var coin = inp.Coin.ToUpper();
      var basecurr = inp.BaseCurrency;
      bittrex.Initialise(new ExchangeContext { ApiKey = inp.ApiKey, Secret = inp.ApiSecret, QuoteCurrency = basecurr.ToUpper(), Simulate = false });
      var tick = bittrex.GetTicker(coin);
      var hist = bittrex.GetOrderHistory(coin, inp.NoOfTrades).OrderBy(x => x.TimeStamp);
      string outp = "";

      //    var buys = hist.Where(x=>x.OrderType == OpenOrderType.Limit_Buy);
      //    var sells = hist.Where(x=>x.OrderType == OpenOrderType.Limit_Sell);
      //    var buyQuantity = buys.Sum(x=>x.Quantity-x.QuantityRemaining);
      //    buyQuantity.Dump("buyq");
      decimal averageBuy = 0;
      decimal buyQuantity = 0;
      bool first = true;
      var tradedata = new TradeData()
      {
        RealizedTrades = new List<RealizedTradeData>()
      };
      foreach (var h in hist)
      {
        if (h.OrderType == OpenOrderType.Limit_Buy)
        {
          decimal prevBuyQ = buyQuantity;
          buyQuantity += h.Quantity - h.QuantityRemaining;
          averageBuy = (averageBuy * prevBuyQ) / buyQuantity + (h.PricePerUnit * ((h.Quantity - h.QuantityRemaining) / buyQuantity));
          //outp += $"Bought {h.Quantity - h.QuantityRemaining} {coin} at {h.PricePerUnit} - averagebuy: {averageBuy:0.#########} Value:{h.Price} date:{h.TimeStamp}<br/>";
          tradedata.RealizedTrades.Add(new RealizedTradeData
          {
            Quantity = h.Quantity - h.QuantityRemaining,
            AverageBuy = averageBuy,
            PricePerUnit = h.PricePerUnit,
            TotalPrice = h.Price,
            TimeStamp = h.TimeStamp,
            BuySell = "Buy"
          });
          first = false;
        }
        else
        {
          var sold = h.Quantity - h.QuantityRemaining;
          if (first)
          {
            //outp += $"First order is sell, ignoring!!! Sold {sold} at {h.PricePerUnit} date: {h.TimeStamp}<br/>";
          }
          else
          {
            var pl = ((h.PricePerUnit - averageBuy) / averageBuy) * 100;
            buyQuantity -= sold;
            // outp += $"Sold {sold} at {h.PricePerUnit} P/L:{pl:0.##}% remaining:{buyQuantity} Value: {h.Price} date: {h.TimeStamp}<br/>";
            tradedata.RealizedTrades.Add(new RealizedTradeData
            {
              Quantity = sold,
              RemainingQuantity = buyQuantity,
              AverageBuy = averageBuy,
              ProfitLoss = pl,
              PricePerUnit = h.PricePerUnit,
              TotalPrice = h.Price,
              TimeStamp = h.TimeStamp,
              BuySell = "Sell"
            });
          }
        }

      }
      if (buyQuantity > 0)
      {
        decimal unrealized = ((tick.Last - averageBuy) / averageBuy) * 100;
        outp += $"<h3>Unrealized P/L: last:{tick.Last} {unrealized:0.##} Quantity: {buyQuantity} Value: {buyQuantity * tick.Last}</h3>";
        tradedata.UnrealizedTrade = new UnrealizedTrade
        {
          ProfitLoss = unrealized,
          ProfitLossValue = (tick.Last * buyQuantity) - (averageBuy * buyQuantity),
          RemainingQuantity = buyQuantity,
          Value = buyQuantity * tick.Last,
          LastPrice = tick.Last
        };

      }

      return tradedata;
    }

    public ActionResult About()
    {
      ViewBag.Message = "Your application description page.";

      return View();
    }

    public ActionResult Contact()
    {
      ViewBag.Message = "Your contact page.";

      return View();
    }
  }
}