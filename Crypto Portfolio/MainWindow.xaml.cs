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
using RestSharp;
using MahApps.Metro.Controls;
using System.IO;
using System.ComponentModel;
using System.Threading;

namespace Crypto_Portfolio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public RestAPI restObject = new RestAPI();
        public List<string> coinList = new List<string>();
        public List<double> coinHoldings = new List<double>();
        public List<decimal> coinPriceList = new List<decimal>();
        public int workerDifficulty = 0;
        BackgroundWorker worker = new BackgroundWorker();
        BackgroundWorker workerAPI = new BackgroundWorker();
        bool apiCompletedFlag = false;

        public object DataSource { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            workerAPI.DoWork += workerAPI_DoWork;
            workerAPI.RunWorkerCompleted += WorkerAPI_RunWorkerCompleted;

            // TODO: Fix Race Condition between worker threads not showing grid. Argument out of range exception
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            createCoinGrid(coinList);
        }

        private void WorkerAPI_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            apiCompletedFlag = true;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            workerDifficulty = coinList.Count();
            workerAPI.RunWorkerAsync();
            int ticker = (100 / workerDifficulty) + 1;
            for (int i = 0; i <= 100; i += ticker)
            {
                // ~.28 seconds per coin price to retrieve each price
                Thread.Sleep(280);
                worker.ReportProgress(i);
            }
            worker.ReportProgress(100);
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void workerAPI_DoWork(object sender, DoWorkEventArgs e)
        {
            coinPriceList = restObject.requestPrice(coinList);
        }


        private void refreshAllCoinPricesTimer()
        {
            // TODO build timer to update prices 5 times per minute (every 20 seconds).
        }

        private void createCoinGrid(List<string> coinList)
        {
            decimal coinTotal;
            int coinIndex = 0;
            decimal coinTotalValue = 0;

            // Build grid from lists
            try
            {
                foreach (string coinName in coinList)
                {
                    double coinsHeld = coinHoldings[coinIndex];
                    coinTotal = coinPriceList[coinIndex] * (decimal)coinsHeld;
                    CoinListView.Items.Add(new GridItems
                    {
                        name = coinName,
                        holdings = coinsHeld,
                        price = coinPriceList[coinIndex],
                        total = coinTotal.ToString("C2")
                    });
                    if (coinsHeld < coinHoldings.Count())
                    {
                        coinIndex++;
                    }
                    coinTotalValue = coinTotalValue + coinTotal;
                }

                //Add totals for final Row
                CoinListView.Items.Add(new GridItems
                {
                    name = "Grand Total Value",
                    total = coinTotalValue.ToString("C2")
                });
            }
            catch (Exception e)
            {
                // Gotta catch em all...
            }
        }

        public class GridItems
        {
            public string name { get; set; }
            public double holdings { get; set; }
            public decimal price { get; set; }
            public string total { get; set; }
        }

        private void UpdatePriceButtonClicked(object sender, RoutedEventArgs e)
        {
            CoinListView.Items.Clear();
            coinLogFileImport();
            worker.RunWorkerAsync();     
        }

        private void coinLogFileImport()
        {
            int instructionSkip = 4;
            int lineCounter = 1;
            string path = Environment.CurrentDirectory;
            var lines = File.ReadLines(path + "\\CoinLog.txt");

            try
            {
                foreach (string line in lines)
                {
                    // Skip first 4 instructional lines
                    if (instructionSkip == 0)
                    {
                        // Add coin name to list
                        if (!isEven(lineCounter))
                        {
                            coinList.Add(line);
                        }
                        if (isEven(lineCounter))
                        {
                            double holding = Double.Parse(line);
                            //If coin holding is zero remove the coin name entry and do not add value.
                            if (holding == 0)
                            {
                                coinList.RemoveAt(coinList.Count() - 1);
                            }
                            else
                            {
                                // Add coin holding to list
                                coinHoldings.Add(holding);
                            }
                        }
                    }
                    else
                    {
                        instructionSkip--;
                    }
                    lineCounter++;
                }
            }
            catch (Exception e)
            {
                // Prompt an error message relating to invalid log file.
            }
        }

        private bool isEven(int lineCounter)
        {
            if(lineCounter%2 == 0)
            {
                return true;
            }
            return false;
        }
    }
}
