using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.ServiceModel.Web;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;

namespace ConsoleApplication1
{
    class Program
    {

        static void Main(string[] args)
        {
            IWebDriver driver = new ChromeDriver();
            #region 航线数据
            string[] _info = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Data", "AirLines.txt"), Encoding.Default);
            string[] _infoE = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Data", "CityComparsion.txt"), Encoding.Default);
            #endregion

            #region 淘宝登录
            driver.Navigate().GoToUrl("https://login.taobao.com/");
            Thread.Sleep(5000);
            driver.FindElement(By.Id("J_Quick2Static")).Click();
            driver.FindElement(By.Id("TPL_username_1")).SendKeys("16view04");
            driver.FindElement(By.Id("TPL_password_1")).SendKeys("16view");
            Thread.Sleep(1000);
            driver.FindElement(By.Id("J_SubmitStatic")).Click();
            #endregion
            Thread.Sleep(5000);
            //搜索次数--t
            for (int t = 0; t < 100; t++)
            {
                #region 取每周四的数据
                for (int i = 0; i < _info.Length; i++)
                {

                    bool flag = false;
                    #region 搜索参数
                    //取日期的每个周四
                    int week = 4;
                    DateTime result = DateTime.Now.AddDays(7).AddDays(-Convert.ToInt32(DateTime.Now.AddDays(7 - week).DayOfWeek));
                    //出发日期
                    string dateTime = result.AddDays(7 * t).ToString("yyyy-MM-dd");
                    //出发城市
                    string defCity = _info[i].Substring(0, 3);
                    if (defCity == "DMK")
                    {
                        defCity = "BKK";
                    }
                    else if (defCity == "PEK")
                    {
                        defCity = "BJS";
                    }
                    else if (defCity == "PVG")
                    {
                        defCity = "SHA";
                    }
                    else if (defCity == "XIY")
                    {
                        defCity = "SIA";
                    }
                    //目的城市
                    string arrCity = _info[i].Substring(3, 3);
                    if (arrCity == "DMK")
                    {
                        arrCity = "BKK";
                    }
                    else if (arrCity == "PEK")
                    {
                        arrCity = "BJS";
                    }
                    else if (arrCity == "PVG")
                    {
                        arrCity = "SHA";
                    }
                    else if (arrCity == "XIY")
                    {
                        arrCity = "SIA";
                    }
                    //航司
                    string hs = _info[i].Substring(6);
                    //出发城市名
                    string defCityE = "";
                    //目的城市名
                    string arrCityE = "";
                    foreach (var st in _infoE)
                    {
                        if (st.Contains(defCity))
                        {
                            string[] s = st.Split(new char[] { '=' });
                            defCityE = System.Web.HttpUtility.UrlEncode(System.Web.HttpUtility.UrlEncode(s[0], Encoding.GetEncoding("GB2312")));
                        }
                    }
                    foreach (var item in _infoE)
                    {
                        if (item.Contains(arrCity))
                        {
                            string[] p = item.Split(new char[] { '=' });
                            arrCityE = System.Web.HttpUtility.UrlEncode(System.Web.HttpUtility.UrlEncode(p[0], Encoding.GetEncoding("GB2312")));
                        }
                    }
                    #endregion

                    #region 搜航线
                    string SearchUrl = string.Format("https://sijipiao.alitrip.com/ie/flight_searcher.htm?searchBy=1280&b2g=0&formNo=-1&agentId=-1&tripType=0&depCityName={0}&depCity={1}&arrCityName={2}&arrCity={3}&depDate={4}&arrDate=&cardId=", defCityE, defCity, arrCityE, arrCity, dateTime);
                    driver.Navigate().GoToUrl(SearchUrl);
                    YZMFind(driver);
                    #endregion

                    #region 选航司
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(2));
                            driver.FindElement(By.Id("J_airline")).Click();
                            var res = driver.FindElements(By.XPath(".//*[contains(@class, 'simulate-list')]"));


                            ReadOnlyCollection<IWebElement> carr = res[1].FindElements(By.TagName("li"));
                            foreach (var item2 in carr)
                            {
                                if (item2.FindElement(By.TagName("input")).GetAttribute("value") == hs)
                                {
                                    driver.FindElement(By.Id("J_airline")).Click();
                                    item2.Click();
                                    YZMFind(driver);
                                    flag = true;
                                    break;
                                }
                            }
                            flag = true;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("element is not attached"))
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(2));
                                flag = false;
                            }
                        }
                        if (flag)
                        {
                            break;
                        }
                    }
                    var rest = driver.FindElements(By.XPath(".//*[@id='J_filter']/dl/dd/span"));
                    if (rest.Count == 0)
                    {
                        continue;
                    }
                    #endregion

                    #region 取数据
                    Thread.Sleep(1500);
                    //航班总节点
                    IWebElement airline = driver.FindElement(By.Id("J_DepResultContainer"));
                    //航班信息节点
                    ReadOnlyCollection<IWebElement> order = airline.FindElements(By.XPath(".//*[@class='J_FlightItem item-root']"));
                    foreach (var item1 in order)
                    {
                        //点击选择航班按钮
                        item1.FindElement(By.TagName("button")).Click();
                        YZMFind(driver);
                        Thread.Sleep(3000);
                        ReadOnlyCollection<IWebElement> title = item1.FindElements(By.TagName("span"));
                        var fn =hs + title[0].Text.Trim().Split(new string[] { hs }, StringSplitOptions.RemoveEmptyEntries)[1];

                        var doc1 = new HtmlAgilityPack.HtmlDocument();
                        doc1.LoadHtml(driver.PageSource);
                        //代理商节点
                        HtmlNodeCollection agentNode = doc1.DocumentNode.SelectNodes("//*[@id=\"J_AgentResultContainer\"]/div/div/table/tbody/tr/td[1]/div[1]/span[2]");
                        HtmlNodeCollection agentNode1 = doc1.DocumentNode.SelectNodes("//*[@id='J_AgentResultContainer']/div/div/table/tbody/tr/td[1]/div/span[3]/@data-nick");
                        //票价节点
                        HtmlNodeCollection priceNode = doc1.DocumentNode.SelectNodes(".//*[@id='J_AgentResultContainer']/div/div/table/tbody/tr/td[5]/div/p[1]/span");
                        //税价节点
                        HtmlNodeCollection taxNode = doc1.DocumentNode.SelectNodes(".//*[@id='J_AgentResultContainer']/div/div/table/tbody/tr/td[5]/div/p[2]/span");
                        if (agentNode == null || agentNode1 == null)
                        {
                            continue;
                        }

                        CatchData data = new CatchData();
                        data.Flight = new FlightInfo();
                        data.Flight.PlatId = 1;
                        data.Flight.DepDate = dateTime;
                        data.Flight.CatchDate = DateTime.Now;
                        data.Flight.AirLine = _info[i].Substring(0, 6);
                        data.Flight.CarrierName = hs;
                        if (fn.Length == 1)
                        {
                            data.Flight.FlightNum = fn[0].ToString();
                        }
                        else
                        {
                            data.Flight.FlightNum = fn[0]+"/"+fn[1];
                        }
                        data.AgentList = new List<FlightAgentInfo>();
                        for (int j = 0; j < agentNode.Count; j++)
                        {

                            if (agentNode[j].InnerText == "")
                            {

                                //特卖的排名
                                int key = 0;

                                if (agentNode1[key].Attributes[2].Value.Contains("一路无忧"))
                                {

                                    //data.Flight.AdultPrice = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", ""));
                                    data.Flight.Tax = int.Parse(taxNode[j].InnerText.Trim().Replace("¥", ""));
                                    data.Flight.Price = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", ""));
                                    data.Flight.Rank = j + 1;

                                }
                                else
                                {
                                    data.AgentList.Add(new FlightAgentInfo
                                    {
                                        AgentName = agentNode1[key].Attributes[2].Value,
                                        AgentPrice = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", "")),
                                        AgentRank = j + 1,
                                        AgentTax = int.Parse(taxNode[j].InnerText.Trim().Replace("¥", "")),
                                    });
                                }
                                key++;
                            }
                            else
                            {
                                try
                                {
                                    if (agentNode[j].InnerText.Trim().Contains("一路无忧"))
                                    {
                                        //data.Flight.AdultPrice = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", ""));
                                        data.Flight.Tax = int.Parse(taxNode[j].InnerText.Trim().Replace("¥", ""));
                                        data.Flight.Price = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", ""));
                                        data.Flight.Rank = j + 1;
                                    }
                                    else
                                    {
                                        data.AgentList.Add(new FlightAgentInfo
                                        {
                                            AgentName = agentNode[j].InnerText.Trim(),
                                            AgentPrice = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", "")),
                                            AgentRank = j + 1,
                                            AgentTax = int.Parse(taxNode[j].InnerText.Trim().Replace("¥", ""))
                                        });
                                    }
                                }
                                catch (Exception)
                                {
                                    if (agentNode[j].InnerText.Trim().Contains("一路无忧"))
                                    {
                                        data.Flight.Price = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", ""));
                                        data.Flight.Rank = j + 1;
                                    }
                                    else
                                    {
                                        data.AgentList.Add(new FlightAgentInfo
                                        {
                                            AgentName = agentNode[j].InnerText.Trim(),
                                            AgentPrice = int.Parse(priceNode[j].InnerText.Trim().Replace("¥", "")),
                                            AgentRank = j + 1,
                                        });
                                    }

                                }
                            }
                        }
                        #region 存数据
                        string str = JsonConvert.SerializeObject(data);
                        var req = HttpWebRequest.Create("http://192.168.2.162:9494/api/Flight") as HttpWebRequest;
                        req.Method = "post";
                        byte[] postdatabyte = Encoding.UTF8.GetBytes(str);
                        req.ContentLength = postdatabyte.Length;
                        Stream stream;
                        stream = req.GetRequestStream();
                        stream.Write(postdatabyte, 0, postdatabyte.Length);
                        stream.Close();
                        var res = req.GetResponse() as HttpWebResponse;
                        Stream streams = res.GetResponseStream();
                        byte[] bytes = new byte[res.ContentLength];
                        streams.Read(bytes, 0, bytes.Length);
                        string text = Encoding.UTF8.GetString(bytes);
                        #endregion

                        #region 关闭预订
                        Thread.Sleep(TimeSpan.FromSeconds(2));
                        driver.FindElement(By.ClassName("sub-icon")).Click();
                        YZMFind(driver);
                        #endregion
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    }
                    #endregion

                }
                #endregion

                #region 取每周二的数据
                //for (int i = 0; i < _info.Length; i++)
                //{
                //    #region 搜索参数
                //    int m = 2;
                //    var result = DateTime.Now.AddDays(7).AddDays(-Convert.ToInt32(DateTime.Now.AddDays(7 - m).DayOfWeek));
                //    string dateTime = result.AddDays(7 * t).ToString("yyyy-MM-dd");
                //    var v1 = _info[i].Split('=');
                //    string defCity = _info[i].Substring(0, 3);
                //    string arrCity = _info[i].Substring(3, 3);
                //    string hs = _info[i].Substring(6);
                //    string defCityE = "";
                //    string arrCityE = "";
                //    foreach (var st in _infoE)
                //    {
                //        if (st.Contains(defCity))
                //        {
                //            string[] s = st.Split(new char[] { '=' });
                //            defCityE = s[0];
                //        }
                //    }
                //    foreach (var item in _infoE)
                //    {
                //        if (item.Contains(arrCity))
                //        {
                //            string[] p = item.Split(new char[] { '=' });
                //            arrCityE = p[0];
                //        }
                //    }
                //    #endregion

                //    #region 搜航线
                //    string SearchUrl = string.Format("https://sijipiao.alitrip.com/ie/flight_searcher.htm?searchBy=1280&b2g=0&formNo=-1&agentId=-1&tripType=0&depCityName=&depCity={0}&arrCityName=&arrCity={1}&depDate={2}&arrDate=&cardId=", defCity, arrCity, dateTime);
                //    driver.Navigate().GoToUrl(SearchUrl);
                //    #endregion

                //    #region 选航司

                //    while (true)
                //    {
                //        IWebElement tt = driver.FindElement(By.Id("J_airline"));
                //        if (tt.Text != "")
                //        {
                //            Thread.Sleep(800);
                //            break;
                //        }
                //        Thread.Sleep(1000);
                //    }

                //    driver.FindElement(By.Id("J_airline")).Click();
                //    var res = driver.FindElements(By.XPath(".//*[contains(@class, 'simulate-list')]"));
                //    if (res[1].Text.Contains(hs))
                //    {
                //        ReadOnlyCollection<IWebElement> carr = res[1].FindElements(By.TagName("li"));
                //        foreach (var item in carr)
                //        {
                //            if (item.Text == hs)
                //            {
                //                driver.FindElement(By.Id("J_airline")).Click();
                //                Thread.Sleep(800);
                //                item.Click();
                //                break;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        continue;
                //    }
                //    Thread.Sleep(1000);
                //    #endregion

                //    #region 取数据
                //    IWebElement airline = driver.FindElement(By.Id("J_DepResultContainer"));
                //    ReadOnlyCollection<IWebElement> order = airline.FindElements(By.XPath(".//*[@class='J_FlightItem item-root']"));


                //    LinePrice Line = new LinePrice()
                //    {
                //        OrgCity = defCity,
                //        DstCity = arrCity,
                //        FromDate = dateTime,
                //        CarrierName = hs,
                //        routings = new List<routing>()
                //    };

                //    foreach (var item in order)
                //    {
                //        //点击选择航班按钮
                //        item.FindElement(By.TagName("button")).Click();
                //        Thread.Sleep(1000);
                //        while (driver.PageSource.Contains("checkcode"))
                //        {
                //            Thread.Sleep(TimeSpan.FromSeconds(8));
                //            if (!driver.PageSource.Contains("checkcode"))
                //            {
                //                break;
                //            }
                //        }
                //        ReadOnlyCollection<IWebElement> title = item.FindElements(By.TagName("span"));
                //        string z = Regex.Match(title[0].Text, "[\u4e00-\u9fbb]+").ToString();
                //        var fn = title[0].Text.Split(new string[] { z }, StringSplitOptions.RemoveEmptyEntries);
                //        var doc1 = new HtmlAgilityPack.HtmlDocument();
                //        doc1.LoadHtml(driver.PageSource);
                //        //代理商
                //        HtmlNodeCollection agentNode = doc1.DocumentNode.SelectNodes("//*[@id=\"J_AgentResultContainer\"]/div/div/table/tbody/tr/td[1]/div[1]/span[2]");
                //        HtmlNodeCollection agentNode1 = doc1.DocumentNode.SelectNodes("//*[@id='J_AgentResultContainer']/div/div/table/tbody/tr/td[1]/div/span[3]/@data-nick");
                //        //*[@id="J_AgentResultContainer"]/div[2]/div/table/tbody/tr/td[1]/div/span[3]
                //        //票价
                //        HtmlNodeCollection priceNode = doc1.DocumentNode.SelectNodes(".//*[@id='J_AgentResultContainer']/div/div/table/tbody/tr/td[5]/div/p[1]/span");
                //        //税价
                //        HtmlNodeCollection taxNode = doc1.DocumentNode.SelectNodes(".//*[@id='J_AgentResultContainer']/div/div/table/tbody/tr/td[5]/div/p[2]/span");
                //        if (agentNode == null || agentNode1 == null)
                //        {
                //            continue;
                //        }
                //        routing routing = new routing();
                //        routing.fromSegments = new List<FromSegment>();
                //        if (fn.Length == 1)
                //        {
                //            routing.fromSegments.Add(new FromSegment
                //            {
                //                carrier = Regex.Match(fn[0], "[A-Z]+").ToString(),
                //                flightNumber = fn[0],
                //            });
                //        }
                //        else
                //        {
                //            routing.fromSegments.Add(new FromSegment
                //            {
                //                carrier = Regex.Match(fn[1], "[A-Z]+").ToString(),
                //                flightNumber = fn[1],
                //            });
                //        }

                //        routing.providers = new List<ProviderInfo>();
                //        for (int p = 0; p < agentNode.Count; p++)
                //        {
                //            if (agentNode[p].InnerText == "")
                //            {
                //                routing.providers.Add(new ProviderInfo
                //                {
                //                    ProviderName = agentNode1[p].Attributes[2].Value,
                //                    adultPrice = int.Parse(priceNode[p].InnerText.Trim().Replace("¥", "")),
                //                    childPrice = int.Parse(priceNode[p].InnerText.Trim().Replace("¥", "")),
                //                    adultTax = int.Parse(taxNode[p].InnerText.Trim().Replace("¥", "")),
                //                    childTax = int.Parse(taxNode[p].InnerText.Trim().Replace("¥", ""))
                //                });
                //            }
                //            else
                //            {
                //                routing.providers.Add(new ProviderInfo
                //                {
                //                    ProviderName = agentNode[p].InnerText.Trim(),
                //                    adultPrice = int.Parse(priceNode[p].InnerText.Trim().Replace("¥", "")),
                //                    childPrice = int.Parse(priceNode[p].InnerText.Trim().Replace("¥", "")),
                //                    adultTax = int.Parse(taxNode[p].InnerText.Trim().Replace("¥", "")),
                //                    childTax = int.Parse(taxNode[p].InnerText.Trim().Replace("¥", ""))
                //                });
                //            }

                //        }
                //        Line.routings.Add(routing);
                //        #region 关闭预订
                //        driver.FindElement(By.ClassName("sub-icon")).Click();
                //        #endregion
                //        Thread.Sleep(TimeSpan.FromSeconds(1));
                //    }
                //    #endregion
                //    str = JsonConvert.SerializeObject(Line);
                //    sw.WriteLine(str);
                //}
                #endregion
            }
        }
        private static void YZMFind(IWebDriver driver)
        {
            Task.Run(() =>
            {

                while (driver.PageSource.Contains("J_CodeContainer"))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(8));
                    if (!driver.PageSource.Contains("J_CodeContainer"))
                    {
                        break;
                    }
                }
                while (driver.PageSource.Contains("checklogin"))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(8));
                    if (!driver.Url.Contains("checklogin") && !driver.PageSource.Contains("checkcode"))
                    {
                        break;
                    }
                }
            }).Wait();
        }
    }
}

