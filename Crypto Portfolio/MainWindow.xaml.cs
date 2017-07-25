using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        public List<object> gridObjects = new List<object>();
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
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            while (apiCompletedFlag == false)
            {
                // Wait to avoid race condition on grid creation call before api coin prices are returned.
                // This would result in argument out of range exception. When this worker thread is completed,
                // the thread should wait until the API request is completed before generating grid.
                Thread.Sleep(1);
            }
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
                // ~.5 seconds per coin price to retrieve each price, varies by connection/location
                // to coinmarketcap.com. .5 is an assumption to provide a relative progress by coin.
                Thread.Sleep(500);
                worker.ReportProgress(i);

                // Stop filling progress bar and break if the api call is done, after will set progress to 100.
                if (apiCompletedFlag == true)
                {
                    break;
                }
            }
            worker.ReportProgress(100);
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void workerAPI_DoWork(object sender, DoWorkEventArgs e)
        {
            apiCompletedFlag = false;
            coinPriceList = restObject.requestPrice(coinList);
        }


        private void refreshAllCoinPricesTimer()
        {
            //removeGridItems();
            coinLogFileImport();
            worker.RunWorkerAsync();
            // TODO build timer to update prices 5 times per minute (every 20 seconds).
        }

        private void createCoinGrid(List<string> coinList)
        {
            removeGridItems();
            decimal coinTotal;
            int coinIndex = 0;
            decimal coinTotalValue = 0;
            

            // Build grid from lists
            try
            {
                foreach (string coinName in coinList)
                {
                    coinTotal = coinPriceList[coinIndex] * (decimal)coinHoldings[coinIndex];
                    
                    GridItems gridItem = new GridItems
                    {
                        name = coinName,
                        holdings = coinHoldings[coinIndex],
                        price = coinPriceList[coinIndex],
                        total = coinTotal.ToString("C2")
                    };

                    gridObjects.Add(gridItem);
                    CoinListView.Items.Add(gridItem);

                    if (coinIndex < coinHoldings.Count())
                    {
                        coinIndex++;
                    }
                    coinTotalValue = coinTotalValue + coinTotal;
                }

                //Add totals for final Row
                
                GridItems gridItemTotal = new GridItems
                {
                    name = "Grand Total Value",
                    total = coinTotalValue.ToString("C2")
                };

                gridObjects.Add(gridItemTotal);
                CoinListView.Items.Add(gridItemTotal);
            }
            catch (Exception e)
            {
                string error = "error" + e;
                // Gotta catch em all...
            }
        }

        private void removeGridItems()
        {
            // TODO: Need to fix the clearing/removing of list view 
            // before updating the next set of prices. Problem stems from
            // the current list view being dynamicly created using new GridItems
            // which contain no reference, clear and remove methods don't work.
            try
            {

                // THIS DOESNT WORK!?!?
                CoinListView.Items.Clear();
                gridObjects.Clear();
                CoinListView.DataContext = gridObjects;
                /*
                foreach (object o in gridObjects)
                {
                    CoinListView.Items.Remove(o);
                }

                for (int i = 0; i <= gridObjects.Count(); i++)
                {
                    CoinListView.Items.Remove(gridObjects[i]);
                }
                */
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
