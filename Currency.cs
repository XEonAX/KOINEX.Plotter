using AEonAX.Shared;
using LiveCharts;
using LiveCharts.Defaults;
using Newtonsoft.Json;
using PusherClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KOINEX.Plotter
{
    public class Currency : NotifyBase
    {
        public ChartValues<ScatterPoint> SellChartValues { get; set; }
        public ChartValues<ScatterPoint> BuyChartValues { get; set; }
        public ChartValues<ScatterPoint> TradeChartValues { get; set; }



        public Currency(Pusher pusherClient)
        {
            _pusherClient = pusherClient;
            SellChartValues = new ChartValues<ScatterPoint>();
            BuyChartValues = new ChartValues<ScatterPoint>();
            TradeChartValues = new ChartValues<ScatterPoint>();
            MarketData = new MarketData();
            ResetAxes = new SimpleCommand()
            {
                ExecuteDelegate = x =>
                {
                    AxisMaxY = 1;
                    AxisMinY = 0;
                    AxisMaxX = 1;
                    AxisMinX = 0;
                },
            };

            Connect = new SimpleCommand()
            {
                ExecuteDelegate = x => _Connect(),
                CanExecuteDelegate = x => !IsConnected,
            };

            Disconnect = new SimpleCommand()
            {
                ExecuteDelegate = x => _Disconnect(),
                CanExecuteDelegate = x => IsConnected,
            };


            SellLabel = (ChartPoint x) =>
              {
                  return CurrencyName + " Sell " + x.X + " @ " + string.Format("{0:C}", x.Y) + " -" + x.Weight + " = " + string.Format("{0:C}", x.X * x.Y);
              };
            BuyLabel = (ChartPoint x) =>
            {
                return CurrencyName + " Buy " + x.X + " @ " + string.Format("{0:C}", x.Y) + " -" + x.Weight + " = " + string.Format("{0:C}", x.X * x.Y);
            };
            TradeLabel = (ChartPoint x) =>
            {
                return CurrencyName + " Trade " + x.X + " @ " + string.Format("{0:C}", x.Y) + " = " + string.Format("{0:C}", x.X * x.Y);
            };
        }

        private void _Disconnect()
        {
            _pusherChannel.UnbindAll();
            _pusherChannel.Unsubscribe();
            //_open_buy_orderswriter.Close();
            //_open_sell_orderswriter.Close();
            //_order_transactionswriter.Close();
            //_market_datawriter.Close();
            IsConnected = false;
        }

        private string _CurrencyName;

        internal void Initialize(Currencies.Types name = Currencies.Types.Ether)
        {
            CurrencyName = name.ToString();
            Icon = Currencies.Icons[name];


            //_Connect();
        }

        public string CurrencyName
        {
            get { return _CurrencyName; }
            set
            {
                if (_CurrencyName != value)
                {
                    _CurrencyName = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private void _Connect()
        {
            if (IsConnected)
            {
                return;
            }
            SellChartValues.Clear();
            BuyChartValues.Clear();
            TradeChartValues.Clear();

            _pusherChannel = _pusherClient.Subscribe("my-channel-" + CurrencyName.ToLower());
            _pusherChannel.Subscribed += _pusherChannel_Subscribed;
            {
                _pusherChannel.Bind(CurrencyName.ToLower() + "_open_buy_orders", (dynamic data) =>
                {

                    string msg = data.message.data.ToString(Formatting.None);
                    ClearTrades();
                    //BuyChartValues.Clear();
                    double highest_bid = double.MinValue;
                    foreach (dynamic order in data.message.data)
                    {
                        UpdateOrderWeight(order, BuyChartValues);
                        if (((double)order.price_per_unit) > MarketData.Lowest_Ask)
                        {
                            OnOutlier(new MessageEventArgs(CurrencyName
                                + ": Rich Buyer @ "
                                + string.Format("{0:C}", (double)order.price_per_unit)
                                + @"/"
                                + string.Format("{0:C}", MarketData.Lowest_Ask)
                                + " for "
                                + (double)order.total_quantity
                                + " for Total "
                                + string.Format("{0:C}", (double)order.price_per_unit * (double)order.total_quantity)));
                            //MarketData.Highest_Bid = (double)order.price_per_unit;
                        }
                        if (((double)order.price_per_unit)> highest_bid)
                        {
                            highest_bid = (double)order.price_per_unit;
                        }
                    }
                    MarketData.Highest_Bid = highest_bid;
                    CleanUp(BuyChartValues, data.message.data);
                    //_open_buy_orderswriter.WriteLine("[" + DateTime.Now + "] " + msg);
                });
            }
            //_open_sell_orderswriter = File.AppendText(CurrencyName + "_open_sell_orders.log");
            {
                _pusherChannel.Bind(CurrencyName.ToLower() + "_open_sell_orders", (dynamic data) =>
                {

                    string msg = data.message.data.ToString(Formatting.None);
                    ClearTrades();
                    double lowest_ask = double.MaxValue;
                    foreach (dynamic order in data.message.data)
                    {
                        UpdateOrderWeight(order, SellChartValues);
                        if (((double)order.price_per_unit) < MarketData.Highest_Bid)
                        {
                            OnOutlier(new MessageEventArgs(CurrencyName
                                + ": Fool Seller @ "
                                + string.Format("{0:C}", (double)order.price_per_unit)
                                + @"/"
                                + string.Format("{0:C}", MarketData.Highest_Bid)
                                + " for "
                                + (double)order.total_quantity
                                + " for Total "
                                + string.Format("{0:C}", (double)order.price_per_unit * (double)order.total_quantity)));
                            //MarketData.Lowest_Ask = (double)order.price_per_unit;
                        }
                        if (((double)order.price_per_unit) < lowest_ask)
                        {
                            lowest_ask = (double)order.price_per_unit;
                        }
                    }
                    MarketData.Lowest_Ask = lowest_ask;
                    CleanUp(SellChartValues, data.message.data);
                    //_open_sell_orderswriter.WriteLine("[" + DateTime.Now + "] " + msg);
                });
            }
            //_order_transactionswriter = File.AppendText(CurrencyName + "_order_transactions.log");
            {
                _pusherChannel.Bind(CurrencyName.ToLower() + "_order_transactions", (dynamic data) =>
                {

                    string msg = data.message.ToString(Formatting.None);
                    //(SellChartValues as IEnumerable<ChartPoint>).Where()
                    //SellChartValues.Clear();
                    //BuyChartValues.Clear();
                    //TradeChartValues.Clear();
                    foreach (dynamic order in data.message.open_sell_orders)
                    {
                        UpdateOrderWeight(order, SellChartValues);
                    }
                    CleanUp(SellChartValues, data.message.open_sell_orders);
                    foreach (dynamic order in data.message.open_buy_orders)
                    {
                        UpdateOrderWeight(order, BuyChartValues);
                        //BuyChartValues.Add(new ScatterPoint((double)order.total_quantity, (double)order.price_per_unit, (double)order.remaining_quantity));
                    }
                    CleanUp(BuyChartValues, data.message.open_buy_orders);
                    foreach (dynamic order in data.message.order_transactions)
                    {
                        UpdateOrder(order, TradeChartValues);
                        //TradeChartValues.Add(new ScatterPoint((double)order.quantity, (double)order.price_per_unit));
                    }
                    CleanUpTrade(TradeChartValues, data.message.order_transactions);
                    //_order_transactionswriter.WriteLine("[" + DateTime.Now + "] " + msg);
                });
            }


            //_market_datawriter = File.AppendText(CurrencyName + "_market_data.log");
            {
                _pusherChannel.Bind(CurrencyName.ToLower() + "_market_data", (dynamic data) =>
                {
                    string msg = data.message.data.ToString(Formatting.None);
                    var marketdata = data.message.data;
                    //MarketData.Highest_Bid = marketdata.highest_bid;
                    MarketData.Last_Traded_Price = marketdata.last_traded_price;
                    //MarketData.Lowest_Ask = marketdata.lowest_ask;
                    MarketData.Max = marketdata.max;
                    MarketData.Min = marketdata.min;
                    MarketData.Vol = marketdata.vol;

                    if (MarketData.AlertMore == 0)
                    {
                        MarketData.AlertMore = marketdata.last_traded_price * (1.000 + MarketData.Variation);
                    }
                    if (MarketData.AlertLess == 0)
                    {
                        MarketData.AlertLess = marketdata.last_traded_price * (1.000 - MarketData.Variation);
                    }

                    if (MarketData.AlertMore < (double)marketdata.last_traded_price)
                    {
                        OnAlert(new MessageEventArgs(CurrencyName + " gone up " + string.Format("{0:C}", marketdata.last_traded_price) + " over " + string.Format("{0:C}", MarketData.AlertMore)));
                        MarketData.AlertMore = marketdata.last_traded_price * (1.000 + MarketData.Variation);
                        MarketData.AlertLess = marketdata.last_traded_price * (1.000 - MarketData.Variation);
                        UpAlert.Play();
                        //SystemSounds.Hand.Play();
                    }

                    if (MarketData.AlertLess > (double)marketdata.last_traded_price)
                    {
                        OnAlert(new MessageEventArgs(CurrencyName + " gone down " + string.Format("{0:C}", marketdata.last_traded_price) + " below " + string.Format("{0:C}", MarketData.AlertLess)));
                        MarketData.AlertMore = marketdata.last_traded_price * (1.000 + MarketData.Variation);
                        MarketData.AlertLess = marketdata.last_traded_price * (1.000 - MarketData.Variation);
                        DownAlert.Play();
                        SystemSounds.Exclamation.Play();
                    }
                    //_market_datawriter.WriteLine("[" + DateTime.Now + "] " + msg);
                });
            }



            //m.Bind("fc3711cd-4b9e-409c-bf5a-5ecccbe45810" + "_" + CurrencyName + "_open_orders", (dynamic data) => {
            //    string msg = data.message.ToString(Formatting.None);
            //});

        }

        private void _pusherChannel_Subscribed(object sender)
        {
            IsConnected = true;
        }

        private void ClearTrades()
        {
            while (TradeChartValues.Count != 1 && TradeChartValues.Count != 0)
            {
                TradeChartValues.RemoveAt(0);
            }
        }

        public SimpleCommand ResetAxes { get; set; }

        public Func<ChartPoint, string> SellLabel { get; set; }
        public Func<ChartPoint, string> BuyLabel { get; set; }
        public Func<ChartPoint, string> TradeLabel { get; set; }



        public Geometry Icon { get; set; }

        private void CleanUpTrade(IChartValues values, dynamic orders)
        {
            IEnumerable<dynamic> y = orders.Children();
            var z = (values as IEnumerable<ScatterPoint>).Where(x => (y.Where(order => x.X == (double)order.quantity && x.Y == (double)order.price_per_unit).Count() == 0));
            foreach (var d in z)
            {
                values.Remove(d);
            }
        }

        private void CleanUp(IChartValues values, dynamic orders)
        {
            IEnumerable<dynamic> y = orders.Children();
            var z = (values as IEnumerable<ScatterPoint>).Where(x => (y.Where(order => x.X == (double)order.total_quantity && x.Y == (double)order.price_per_unit).Count() == 0));
            foreach (var d in z)
            {
                values.Remove(d);
            }
        }

        private void UpdateOrder(dynamic order, IChartValues values)
        {
            var z = (values as IEnumerable<ScatterPoint>).Where(x => (x.X == (double)order.quantity && x.Y == (double)order.price_per_unit));
            if (z.Count() == 0)
            {
                values.Add(new ScatterPoint((double)order.quantity, (double)order.price_per_unit));
            }
        }

        private void UpdateOrderWeight(dynamic order, IChartValues values)
        {
            var z = (values as IEnumerable<ScatterPoint>).Where(x => (x.X == (double)order.total_quantity && x.Y == (double)order.price_per_unit));
            if (z.Count() > 0)
            {
                foreach (var pt in z)
                {
                    pt.Weight = (double)order.remaining_quantity;
                }
            }
            else
            {
                if ((double)order.price_per_unit > AxisMaxY || AxisMaxY == 1)
                {
                    AxisMaxY = (double)order.price_per_unit;
                    //OnOutlier(new MessageEventArgs(CurrencyName + ": Max Price " + string.Format("{0:C}", (double)order.price_per_unit) + " for " + (double)order.total_quantity + " for Total " + string.Format("{0:C}", (double)order.price_per_unit * (double)order.total_quantity)));
                }
                else if ((double)order.price_per_unit < AxisMinY || AxisMinY == 0)
                {
                    AxisMinY = (double)order.price_per_unit;
                    //OnOutlier(new MessageEventArgs(CurrencyName + ": Min Price " + string.Format("{0:C}", (double)order.price_per_unit) + " for " + (double)order.total_quantity + " for Total " + string.Format("{0:C}", (double)order.price_per_unit * (double)order.total_quantity)));
                }


                if ((double)order.total_quantity > AxisMaxX || AxisMaxX == 1)
                {
                    AxisMaxX = (double)order.total_quantity;
                    //OnOutlier(new MessageEventArgs(CurrencyName + ": Max Quantity " + (double)order.total_quantity + " @ " + string.Format("{0:C}", (double)order.price_per_unit) + " for Total " + string.Format("{0:C}", (double)order.price_per_unit * (double)order.total_quantity)));
                }
                else if ((double)order.total_quantity < AxisMinX || AxisMinX == 0)
                {
                    AxisMinX = (double)order.total_quantity;
                }
                values.Add(new ScatterPoint((double)order.total_quantity, (double)order.price_per_unit, (double)order.remaining_quantity));
            }
        }











        private double _AxisMaxY = 1;
        private double _AxisMinY = 0;
        private double _AxisMaxX = 1;
        private double _AxisMinX = 0;
        private Pusher _pusherClient;

        public double AxisMaxY
        {
            get { return _AxisMaxY; }
            set
            {
                _AxisMaxY = value;
                NotifyPropertyChanged();
            }
        }
        public double AxisMinY
        {
            get { return _AxisMinY; }
            set
            {
                _AxisMinY = value;
                Debug.Print(DateTime.Now.ToString() + " AxisMinY = " + AxisMinY);
                NotifyPropertyChanged();
            }
        }

        public double AxisMaxX
        {
            get { return _AxisMaxX; }
            set
            {
                _AxisMaxX = value;
                NotifyPropertyChanged();
            }
        }
        public double AxisMinX
        {
            get { return _AxisMinX; }
            set
            {
                _AxisMinX = value;
                //Debug.Print("AxisMinX = " + AxisMinX);
                NotifyPropertyChanged();
            }
        }

        public SimpleCommand Connect { get; set; }
        public MarketData MarketData { get; set; }


        bool _IsConnected;
        private Channel _pusherChannel;
        private SoundPlayer UpAlert = new SoundPlayer("up.wav");
        private SoundPlayer DownAlert = new SoundPlayer("down.wav");

        //private StreamWriter _open_buy_orderswriter;
        //private StreamWriter _open_sell_orderswriter;
        //private StreamWriter _order_transactionswriter;
        //private StreamWriter _market_datawriter;

        public bool IsConnected
        {
            get { return _IsConnected; }
            set
            {
                _IsConnected = value;
                NotifyPropertyChanged();
            }
        }

        public SimpleCommand Disconnect { get; set; }


        public event EventHandler<MessageEventArgs> Alert;
        protected virtual void OnAlert(MessageEventArgs e)
        {
            Alert?.Invoke(this, e);
        }



        public event EventHandler<MessageEventArgs> Outlier;
        protected virtual void OnOutlier(MessageEventArgs e)
        {
            Outlier?.Invoke(this, e);
        }


    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public MessageEventArgs(string msg)
        {
            Message = msg;
        }
    }

    public class MarketData : NotifyBase
    {
        private double _Max;
        public double Max
        {
            get { return _Max; }
            set
            {
                _Max = value;
                NotifyPropertyChanged();
            }
        }


        private double _Min;
        public double Min
        {
            get { return _Min; }
            set
            {
                _Min = value;
                NotifyPropertyChanged();
            }
        }

        private double _Vol;
        public double Vol
        {
            get { return _Vol; }
            set
            {
                _Vol = value;
                NotifyPropertyChanged();
            }
        }

        private double _Last_Traded_Price;
        public double Last_Traded_Price
        {
            get { return _Last_Traded_Price; }
            set
            {
                _Last_Traded_Price = value;
                NotifyPropertyChanged();
            }
        }

        private double _Lowest_Ask = double.MaxValue;
        public double Lowest_Ask
        {
            get { return _Lowest_Ask; }
            set
            {
                _Lowest_Ask = value;
                NotifyPropertyChanged();
            }
        }

        private double _Highest_Bid= double.MinValue;
        public double Highest_Bid
        {
            get { return _Highest_Bid; }
            set
            {
                _Highest_Bid = value;
                NotifyPropertyChanged();
            }
        } 

        private double _AlertLess;
        public double AlertLess
        {
            get { return _AlertLess; }
            set
            {
                _AlertLess = value;
                NotifyPropertyChanged();
            }
        }

        private double _AlertMore;
        public double AlertMore
        {
            get { return _AlertMore; }
            set
            {
                _AlertMore = value;
                NotifyPropertyChanged();
            }
        }


        public double Variation { get; set; } = 0.0001;

    }

    static class Currencies
    {
        internal static Dictionary<Types, Geometry> Icons { get; set; }
        static Currencies()
        {
            Icons = new Dictionary<Types, Geometry>()
            {
                { Types.Bitcoin,Geometry.Parse("F1 M100,125z M0,0z M78.81,49.53A18,18,0,0,0,76.35,47.53A17.67,17.67,0,0,0,65.79,20.71L65.79,13.27A2.27,2.27,0,0,0,63.52,11L56.71,11A2.27,2.27,0,0,0,54.44,13.27L54.44,20.09 43.09,20.09 43.09,13.27A2.27,2.27,0,0,0,40.81,11L34,11A2.27,2.27,0,0,0,31.73,13.27L31.73,20.09 18.25,20.09A2.27,2.27,0,0,0,16,22.35L16,29.18A2.27,2.27,0,0,0,18.27,31.45L24.14,31.45 24.14,68.54 18.25,68.54A2.27,2.27,0,0,0,16,70.82L16,77.65A2.27,2.27,0,0,0,18.27,79.92L31.73,79.92 31.73,86.74A2.27,2.27,0,0,0,34,89L40.82,89A2.27,2.27,0,0,0,43.09,86.73L43.09,79.91 54.44,79.91 54.44,86.73A2.27,2.27,0,0,0,56.71,89L63.53,89A2.27,2.27,0,0,0,65.8,86.73L65.8,79.91 65.86,79.91A18.28,18.28,0,0,0,78.35,75.14A17.8,17.8,0,0,0,78.81,49.53z M35.48,31.46L61.38,31.46A6.44,6.44,0,0,1,61.38,44.33L35.48,44.33z M70.78,66.67A6.4,6.4,0,0,1,66.23,68.56L35.48,68.56 35.48,55.7 66.23,55.7A6.44,6.44,0,0,1,70.78,66.7z") },
                { Types.Ether,Geometry.Parse("F1 M24,24z M0,0z M11.944,17.97L4.58,13.62 11.943,24 19.313,13.62 11.941,17.97 11.944,17.97z M12.056,0L4.69,12.223 12.055,16.577 19.42,12.227 12.056,0z") },
                { Types.Ripple,Geometry.Parse("F1 M226.777,226.777z M0,0z M196.224,139.515C188.634,135.353,180.376,133.425,172.243,133.489L172.312,133.45C161.154,133.578 152.001,124.9 151.87,114.07 151.741,103.384 160.435,94.603 171.375,94.238L171.375,94.233 171.355,94.223C179.631,94.183 188.004,92.078 195.632,87.676 218.442,74.511 225.941,45.889 212.381,23.746 198.818,1.602 169.334,-5.677 146.524,7.488 123.714,20.652 116.216,49.275 129.777,71.418 135.425,80.645 132.303,92.57 122.799,98.056 113.43,103.463 101.366,100.59 95.611,91.673L95.606,91.675 95.606,91.7C91.459,84.749 85.417,78.744 77.693,74.508 54.59,61.836 25.276,69.744 12.223,92.175 -0.83,114.601 7.32,143.057 30.424,155.728 53.529,168.4 82.839,160.491 95.893,138.061 95.972,137.926 96.042,137.786 96.119,137.65L96.119,137.683 96.124,137.685C101.669,128.631 113.679,125.494 123.171,130.7 132.799,135.981 136.192,147.836 130.754,157.18 117.699,179.611 125.85,208.065 148.953,220.735 172.057,233.408 201.37,225.498 214.423,203.068 227.474,180.64 219.327,152.187 196.224,139.515z") },
                { Types.Litecoin,Geometry.Combine(
                    Geometry.Parse("F1 M512,640z M0,0z M204.622,89.863L204.622,89.863 150.193,422.136 370.85,422.136 370.85,364.765 234.778,364.765 277.806,89.863z"),
                    Geometry.Parse("F1 M512,640z M0,0z M317.513,139.878L317.513,139.878 309.811,185.954 141.151,267.86 148.92,221.38z"),
                    GeometryCombineMode.Union,null
                    )},
                { Types.Bitcoin_Cash,Geometry.Combine(
                    Geometry.Combine(
                        Geometry.Parse("F1 M100,125z M0,0z M-893.3,1003.5L-902.3,1003.5 -902.3,1017.3 -893.7,1017.3C-892.6,1017.3 -891.3,1017.2 -890.4,1016.8 -888,1015.8 -886.7,1013.2 -886.7,1010.3 -886.6,1006.3 -889.1,1003.5 -893.3,1003.5z"),
                        Geometry.Parse("F1 M100,125z M0,0z M-888.4,990.6C-888.4,988.3 -889.3,986.3 -891.1,985.3 -892.1,984.8 -893.5,984.6 -895.1,984.6L-902.3,984.6 -902.3,996.7 -894.1,996.7C-890.5,996.7,-888.4,994.2,-888.4,990.6z"),
                        GeometryCombineMode.Union,null
                    ),
                    Geometry.Parse("F1 M100,125z M0,0z M-899,953.5C-925.2,953.5 -946.5,974.8 -946.5,1001 -946.5,1027.2 -925.2,1048.5 -899,1048.5 -872.8,1048.5 -851.5,1027.2 -851.5,1001 -851.5,974.8 -872.8,953.5 -899,953.5z M-887.1,1023.4C-889.1,1024,-890.9,1024.2,-892.9,1024.2L-892.9,1030.7C-892.9,1031,-893.1,1031.2,-893.4,1031.2L-897.1,1031.2C-897.4,1031.2,-897.6,1031,-897.6,1030.7L-897.6,1024.2 -902.5,1024.2 -902.5,1030.7C-902.5,1031,-902.7,1031.2,-903,1031.2L-906.7,1031.2C-907,1031.2,-907.2,1031,-907.2,1030.7L-907.2,1024.2 -915.7,1024.2C-916.2,1024.2,-916.6,1023.8,-916.6,1023.3L-916.6,1018.7C-916.6,1018.2,-916.2,1017.8,-915.7,1017.8L-913.7,1017.8C-912.2,1017.8,-911.1,1016.6,-911.1,1015.2L-911.1,986.9C-911.1,985.4,-912.3,984.3,-913.7,984.3L-915.7,984.3C-916.2,984.3,-916.6,983.9,-916.6,983.4L-916.6,978.8C-916.6,978.3,-916.2,977.9,-915.7,977.9L-907.2,977.9 -907.2,971.4C-907.2,971.1,-907,970.9,-906.7,970.9L-903,970.9C-902.7,970.9,-902.5,971.1,-902.5,971.4L-902.5,977.9 -897.6,977.9 -897.6,971.4C-897.6,971.1,-897.4,970.9,-897.1,970.9L-893.4,970.9C-893.1,970.9,-892.9,971.1,-892.9,971.4L-892.9,978C-890.5,978.1 -888.6,978.4 -886.7,979.2 -882.5,980.8 -879.6,984.7 -879.6,989.9 -879.6,994.3 -881.6,997.9 -885.3,999.8L-885.3,999.9C-880.1,1001.5 -877.8,1006 -877.8,1010.9 -877.9,1017.2 -882.1,1021.8 -887.1,1023.4z"),
                    GeometryCombineMode.Union,null
                    )
                }

            };
        }
        internal enum Types
        {
            Bitcoin,
            Ether,
            Ripple,
            Litecoin,
            Bitcoin_Cash
        }
    }
}
