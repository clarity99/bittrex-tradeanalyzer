using Bittrex;
using BittrexTradeAnalyzer.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace BittrexTradeAnalyzer.Controllers
{
  public class HomeController : Controller
  {
    public HomeController()
    {
      CoinMarketData();
    }

    public ActionResult BalanceOne()
    {
      var v = new TradeInput();
      v.NoOfTrades = 200;
      return View(v);
    }

    [HttpPost]
    public ActionResult BalanceOne(TradeInput inp)
    {

      TradeData tradedata = CalculateTradeData(inp);
      //ViewBag.Result = outp;
      ViewBag.TradeData = tradedata;
      return View(inp);
    }

    
    public ActionResult Index()
    {
      var cookieKey = Request.Cookies["apikey"];
      var cookiesecret = Request.Cookies["apisecret"];
      var ti = new TradeInput
      {
        ApiKey = cookieKey?.Value,
        ApiSecret = cookiesecret?.Value
      };
      return View("GetAllBalances", ti);
    }

    [HttpPost]
    public ActionResult Index(string apikey, string apisecret, string baseCurrency, bool rememberKey)
    {
      Session.Add("apikey", apikey);
      Session.Add("apisecret", apisecret);
      if (rememberKey)
      {
        var cookieKey = new HttpCookie("apikey", apikey);
        cookieKey.Secure = true;
        cookieKey.HttpOnly = true;
        cookieKey.Expires = DateTime.Now.AddMonths(1);
        var cookieSecret = new HttpCookie("apisecret", apisecret);
        cookieSecret.Secure = true;
        cookieSecret.HttpOnly = true;
        cookieSecret.Expires = DateTime.Now.AddMonths(1);
        Response.Cookies.Add(cookieKey);
        Response.Cookies.Add(cookieSecret);
      } else
      {
        var cookieKey = new HttpCookie("apikey", "");
        cookieKey.Secure = true;
        cookieKey.HttpOnly = true;
        cookieKey.Expires = DateTime.Now.AddMonths(-1);
        var cookieSecret = new HttpCookie("apisecret", "");
        cookieSecret.Secure = true;
        cookieSecret.HttpOnly = true;
        cookieSecret.Expires = DateTime.Now.AddMonths(-1);
        Response.Cookies.Add(cookieKey);
        Response.Cookies.Add(cookieSecret);
      }
      var d = GetTradeDataForPortfolio(apikey, apisecret, baseCurrency);
      return View("GetAllBalancesPost", d.OrderByDescending(x=>x.BalanceEUR));
    }

    public ActionResult TradesPerCoin(string id, string basecurrency)
    {
      string apikey = (string)Session["apikey"];
      string apisecret = (string)Session["apisecret"];
      if (string.IsNullOrEmpty(apikey) || string.IsNullOrEmpty(apisecret))
      {
        return RedirectToAction("Index");
      }

      var inp = new TradeInput
      {
        ApiKey = apikey,
        ApiSecret = apisecret,
        Coin = id,
        BaseCurrency = basecurrency,
        NoOfTrades = 200
      };
      TradeData tradedata = CalculateTradeData(inp);
      //ViewBag.Result = outp;
      ViewBag.TradeData = tradedata;
      return View(inp);
    }

    private  IEnumerable<TotalPortfolioTradeData> GetTradeDataForPortfolio(string apikey, string apisecret, string quoteCurrency)
    {

      var bittrex = new Exchange();
      
      
      bittrex.Initialise(new ExchangeContext { ApiKey = apikey, Secret = apisecret, QuoteCurrency ="USDT", Simulate = false });
      var bal = bittrex.GetBalances();
      var l = new List<TotalPortfolioTradeData>();
      foreach (var b in bal.Where(x => x.Balance > 0).AsParallel().WithDegreeOfParallelism(10))
      {
        var r1 = GetTradeDataForBase(apikey, apisecret, b, "USDT");
        if (r1?.TradePerCoin.RealizedTrades.Any() == true || r1?.TradePerCoin.UnrealizedTrade != null)
          l.Add(r1);
        
        r1 = GetTradeDataForBase(apikey, apisecret, b, "BTC");
        if (r1?.TradePerCoin.RealizedTrades.Any() == true || r1?.TradePerCoin.UnrealizedTrade != null)
          l.Add(r1);
        r1 = GetTradeDataForBase(apikey, apisecret, b, "ETH");
        if (r1?.TradePerCoin.RealizedTrades.Any() == true || r1?.TradePerCoin.UnrealizedTrade != null)
          l.Add(r1);

      }
      
      return l;
    }

    Dictionary<string, CoinMarket> cmktall;

    public void CoinMarketData()
    {
      var cmktapi = new HttpClient().GetStringAsync("https://api.coinmarketcap.com/v1/ticker/?convert=EUR&limit=1800").Result;

      // var cmkt = new[] { new CoinMarket { Co = "", price_eur = (decimal)0.0, percent_change_1h = "", percent_change_24h = "" } };
      cmktall = JsonConvert.DeserializeObject<CoinMarket[]>(cmktapi).Distinct(new CMCompare()).ToDictionary(x => x.symbol);
      //cmktall.Add("MSP", new CoinMarket { price_eur = 0.03546M, symbol = "MSP", price_btc = 0.00002M });
      cmktall.Add("XTZ", new CoinMarket { price_eur = 0.0001666666666666667M * cmktall["BTC"].price_eur, symbol = "XTZ", price_btc = 0.0001666666666666667M });

    }

    private TotalPortfolioTradeData GetTradeDataForBase(string apikey, string apisecret, AccountBalance b, string baseCurrency)
    {
      try
      {
        var td = CalculateTradeData(new TradeInput
        {
          ApiKey = apikey,
          ApiSecret = apisecret,
          BaseCurrency = baseCurrency,
          Coin = b.Currency,
          NoOfTrades = 200
        });
        var btcvalue = b.Balance * cmktall[b.Currency].price_btc;
        var eurvalue = b.Balance * cmktall[b.Currency].price_eur;
        var usdvalue = b.Balance * cmktall[b.Currency].price_usd;
        var r = new TotalPortfolioTradeData
        {
          Coin = b.Currency,
          BaseCurrency = baseCurrency,
          Balance = b.Balance,
          BalanceBTC = btcvalue ?? 0,
          BalanceEUR = eurvalue ?? 0,
          BalanceUSD = usdvalue ?? 0,
          TradePerCoin = td
        };
        return r;
      }
      catch (Exception ex)
      {
        return null;
      }
    }

    private static TradeData CalculateTradeData(TradeInput inp)
    {
      var bittrex = new Exchange();
      var coin = inp.Coin.ToUpper();
      var basecurr = inp.BaseCurrency;
      bittrex.Initialise(new ExchangeContext { ApiKey = inp.ApiKey, Secret = inp.ApiSecret, QuoteCurrency = basecurr.ToUpper(), Simulate = false });
      var tick = bittrex.GetTicker(coin);
      var hist = bittrex.GetOrderHistory(coin, inp.NoOfTrades).OrderBy(x => x.TimeStamp).Take(inp.NoOfTrades);
      string outp = "";

      //    var buys = hist.Where(x=>x.OrderType == OpenOrderType.Limit_Buy);
      //    var sells = hist.Where(x=>x.OrderType == OpenOrderType.Limit_Sell);
      //    var buyQuantity = buys.Sum(x=>x.Quantity-x.QuantityRemaining);
      //    buyQuantity.Dump("buyq");
      decimal averageBuy = 0;
      decimal buyQuantity = 0;
      decimal totalBuyQuantity = 0;
      decimal averageSell = 0;

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
          totalBuyQuantity += h.Quantity - h.QuantityRemaining;
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
            
            var plValue2 = h.PricePerUnit*h.Quantity - (averageBuy * h.Quantity);
            buyQuantity -= sold;
          //  averageSell = (averageSell * prevBuyQ) / buyQuantity + (h.PricePerUnit * ((h.Quantity - h.QuantityRemaining) / buyQuantity));
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
              ProfitLossValue = plValue2,
              BuySell = "Sell"
            });
          }
        }

      }
      if (buyQuantity > 0)
      {
        decimal unrealized = ((tick.Last - averageBuy) / averageBuy) * 100;
        outp += $"<h3>Unrealized P/L: last:{tick.Last} {unrealized:0.##} Quantity: {buyQuantity} Value: {buyQuantity * tick.Last}</h3>";
        tradedata.UnrealizedTrade = new TotalTrade
        {
          ProfitLoss = unrealized,
          ProfitLossValue = (tick.Last * buyQuantity) - (averageBuy * buyQuantity),
          RemainingQuantity = buyQuantity,
          Value = buyQuantity * tick.Last,
          LastPrice = tick.Last
        };

      }
      // realized p/l
      //var soldTotalPrice = tradedata.RealizedTrades.Where(x => x.BuySell == "Sell").Sum(x => x.TotalPrice);
      //var soldTotalQuantity = tradedata.RealizedTrades.Where(x => x.BuySell == "Sell").Sum(x => x.Quantity);
      //var totalPriceFromAverageBuy = averageBuy * soldTotalQuantity;
      //var realizedPL = ((soldTotalPrice-totalPriceFromAverageBuy)/totalPriceFromAverageBuy)*100;

      IEnumerable<RealizedTradeData> sells = tradedata.RealizedTrades.Where(x => x.BuySell == "Sell").ToList();
      var soldTotalQuantity = sells.Sum(x => x.Quantity);
      var realizedPL = sells.Sum(x => x.ProfitLoss*(x.Quantity/soldTotalQuantity));
      var realizedPLValue = sells.Sum(x => x.ProfitLossValue);
      var value = sells.Sum(x => x.TotalPrice);
      tradedata.RealizedTrade = new TotalTrade
      {
        ProfitLoss = realizedPL,
        RemainingQuantity = soldTotalQuantity,
        Value = value,
        ProfitLossValue = realizedPLValue
      };
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