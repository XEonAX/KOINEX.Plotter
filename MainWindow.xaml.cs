﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PusherClient;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using LiveCharts.Defaults;
using System.Timers;
using System.Diagnostics;

namespace KOINEX.Plotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Pusher pusherClient;
        private readonly string pusher_key = "9197b0bfdf3f71a4064e";
        public Currency Bitcoin { get; set; }
        public Currency Ether { get; set; }
        public Currency Ripple { get; set; }
        public Currency Litecoin { get; set; }
        public Currency Bitcoin_Cash { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            pusherClient = new Pusher(pusher_key, new PusherOptions()
            {
                Cluster = "ap2"
            });

            Bitcoin = new Currency(pusherClient);
            Bitcoin.Initialize(Currencies.Types.Bitcoin);

            Ether = new Currency(pusherClient);
            Ether.Initialize(Currencies.Types.Ether);

            Ripple = new Currency(pusherClient);
            Ripple.Initialize(Currencies.Types.Ripple);

            Litecoin = new Currency(pusherClient);
            Litecoin.Initialize(Currencies.Types.Litecoin);

            Bitcoin_Cash = new Currency(pusherClient);
            Bitcoin_Cash.Initialize(Currencies.Types.Bitcoin_Cash);

            pusherClient.Connect();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Bitcoin.ResetAxes.Execute(null);
            Ether.ResetAxes.Execute(null);
            Ripple.ResetAxes.Execute(null);
            Litecoin.ResetAxes.Execute(null);
            Bitcoin_Cash.ResetAxes.Execute(null);
        }

    }







    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
    }
}