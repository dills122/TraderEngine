﻿using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraderEngine
{
    class Trader
    {
        public int TradersID;
        public List<int> TraderID = new List<int> { 14534, 40324, 82183, 22344, 78492 };
        public List<int> InUse = new List<int> { 0, 0, 0, 0, 0 };
        Global_Functions glob = new Global_Functions();
        public Trader()
        {
            Random rnd = new Random();
            int id = rnd.Next(TraderID.Count());
            TradersID = TraderID[id];
            InUse[id] = 1; 
        }

        public int GetTraderID()
        {
            return TradersID;
        }
        /// <summary>
        /// Checks to make sure there is enough stock to buy
        /// </summary>
        /// <param name="StockID"></param>
        /// <param name="AmountToBuy"></param>
        /// <returns></returns>
        public bool CheckStockAmount(int StockID, int AmountToBuy)
        {
            SqlConnection conn = glob.Connect();
            conn.Open();
            string sql = "select Amount FROM CURRENT_STOCK_PRICES as CS where CS.StockID = @StockID ";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            int AmountAvaliable = (int) cmd.ExecuteScalar();
            if (AmountAvaliable >= AmountToBuy)
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        public float GetCurrentStockPrice(int StockID)
        {
            float CurrentStockPrice = 0;
            SqlConnection conn = glob.Connect();
            conn.Open();
            string sql = "select CS.StockPrice FROM CURRENT_STOCK_PRICES CS where CS.StockID=@StockID ";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                CurrentStockPrice = (float)(double)dr["StockPrice"];
            }
            dr.Close();
            glob.CloseDB(conn);
            return CurrentStockPrice;
        }

        public float AdjustStockPrice(int StockID, int UserID,int Amount, int Action, int StockTransLog)
        {
            string Now = DateTime.Now.ToString();
            string AnHourAgo = DateTime.Now.AddHours(-1).ToString();

            int SellsCount = 0;
            int BuysCount = 0;
            double Multiplier = .0035;

            SqlConnection conn = glob.Connect();
            conn.Open();
            string sql = "select AmountBefore, AmountAfter FROM STOCK_TRANS_LOG Where StockID=@StockID and [Date]>=@HourAgo and [Date]<=@Now";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("HourAgo", AnHourAgo));
            cmd.Parameters.Add(new SqlParameter("Now", Now));
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                if ((int)dr["AmountBefore"] > (int)dr["AmountAfter"])
                {
                    SellsCount++;
                }
                else
                {
                    BuysCount++;
                }
            }
            dr.Close();

            if (SellsCount > BuysCount)
            {
                Multiplier *= -1;
            }
            else if (SellsCount == BuysCount)
            {
                Multiplier = 1;
            }
            
            //Check to see if the last transaction was a buy or sell

            //Then recalculate the Stock value 

            //Then recalculate with the multiplier 

            //Insert the new stock price into the DB

            glob.CloseDB(conn);

            return 0;
        }

        /// <summary>
        /// Sells Stock for a User and adds the Transactions to the Logs
        /// </summary>
        /// <param name="UserID">Users Submitting the Request</param>
        /// <param name="StockID">Stock Selling</param>
        /// <param name="Amount">Amount of Stock to sell</param>
        /// <returns></returns>
        public bool SellStock(int UserID, int StockID, int Amount)
        {
            float CurrentStockPrice = GetCurrentStockPrice(StockID);
            float Cost = CurrentStockPrice * Amount;
            SqlConnection conn = glob.Connect();
            conn.Open();

            //Removes the stock to the Users Account
            string sql = "update USER_STOCKS set Amount=(Amount - @Amount), [Date]=@Date where StockID=@StockID and UserID=@UserID ";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("Amount", Amount));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();


            //Removes the bought stock from the market
            sql = "update USER_CASH set Cash=(Cash + @Cash), [Date]=@Date where UserID=@UserID ";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("Cash", Cost));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();


            //Add the Transaction to the Log
            sql = "Insert into USER_TRANS_LOG (UserID, StockID, Method, Amount, Cost, [Date]) Values (@UserID, @StockID, @Method, @Amount, @Cost, @Date)";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("Method", "0"));
            //Need to change this to the recalculated value
            cmd.Parameters.Add(new SqlParameter("Amount", Amount));
            cmd.Parameters.Add(new SqlParameter("Cost", Cost));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();

            int AmountAfterSell = 0;
            


            //Adds the bought stock from the market
            sql = "update CURRENT_STOCK_PRICES set Amount=(Amount + @Amount) output inserted.Amount, inserted.StockPrice where StockID=@StockID ";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("Amount", Amount));
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                AmountAfterSell = (int) dr["Amount"];
            }
            dr.Close();

            //Readjusts the Stock Price 
            //Have not come up with the equation to adjust the price yet

            //Add the Transaction to the Log
            sql = "insert into STOCK_TRANS_LOG  (UserID, StockID, BeforeChange, AfterChange, AmountBefore, AmountAfter, [Date]) Values (@UserID, @StockID, @MoneyBefore, @MoneyAfter, @AmountBefore, @AmountAfter, @Date)";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("MoneyBefore", CurrentStockPrice));
            //Need to change this to the recalculated value
            cmd.Parameters.Add(new SqlParameter("MoneyAfter", CurrentStockPrice));
            cmd.Parameters.Add(new SqlParameter("AmountBefore", (AmountAfterSell - Amount)));
            cmd.Parameters.Add(new SqlParameter("AmountAfter", AmountAfterSell));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();

            glob.CloseDB(conn);
            return true;
        }


        /// <summary>
        /// Buys Stock on the market
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="StockID"></param>
        /// <param name="Amount"></param>
        /// <returns></returns>
        public bool BuyStock(int UserID, int StockID, int Amount)
        {
            float CurrentStockPrice = 0;
            float Cash = 0;
            SqlConnection conn = glob.Connect();
            conn.Open();
            string sql = "select CS.StockPrice FROM CURRENT_STOCK_PRICES CS where CS.StockID=@StockID ";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                CurrentStockPrice = (float) (double) dr["StockPrice"];
            }
            dr.Close();

            sql = "select Cash FROM [USER] as U inner join USER_CASH as UC on UC.UserID = U.UserID where U.UserID=@UserID";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Cash = (float) (double) dr["Cash"];
            }
            dr.Close();

            if ((CurrentStockPrice * Amount <= Cash) && CurrentStockPrice != 0)
            {
               bool StockBough =  AdjustMarketStock(StockID, UserID, Amount);
               bool UserUpdated = AdjustUser(UserID, (CurrentStockPrice * Amount), Amount, StockID);
                if (StockBough == true && UserUpdated == true)
                {
                    return true;
                }
            }

            glob.CloseDB(conn);
            return false;
        }

        /// <summary>
        /// Buys the Stock for a User 
        /// </summary>
        /// <param name="StockID">Stock to be purchased</param>
        /// <param name="UserID">User Buying the Stock</param>
        /// <param name="StockBought">Amount of Stock to buy</param>
        /// <returns></returns>
        public bool AdjustMarketStock(int StockID, int UserID, float StockBought)
        {
            int AmountAfterBuy = 0;
            float StockPrice = 0;

            SqlConnection conn = glob.Connect();
            conn.Open();
            //Removes the bought stock from the market
            string sql = "update CURRENT_STOCK_PRICES set Amount=(Amount - @Amount) output inserted.Amount, inserted.StockPrice where StockID=@StockID ";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("Amount", StockBought));
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                AmountAfterBuy = (int)dr["Amount"];
                StockPrice = (float) (double) dr["StockPrice"];
            }
            dr.Close();

            //Readjusts the Stock Price 
            //Have not come up with the equation to adjust the price yet

            //Add the Transaction to the Log
            sql = "insert into STOCK_TRANS_LOG  (UserID, StockID, BeforeChange, AfterChange, AmountBefore, AmountAfter, [Date]) Values (@UserID, @StockID, @MoneyBefore, @MoneyAfter, @AmountBefore, @AmountAfter, @Date)";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("MoneyBefore", StockPrice));
            //Need to change this to the recalculated value
            cmd.Parameters.Add(new SqlParameter("MoneyAfter", StockPrice));
            cmd.Parameters.Add(new SqlParameter("AmountBefore", (AmountAfterBuy + StockBought)));
            cmd.Parameters.Add(new SqlParameter("AmountAfter", AmountAfterBuy));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();

            glob.CloseDB(conn);
            return true;

        }

        /// <summary>
        /// Adds the stock to the Users Account
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="Cost"></param>
        /// <param name="Amount"></param>
        /// <param name="StockID"></param>
        /// <returns></returns>
        public bool AdjustUser(int UserID, float Cost, int Amount, int StockID)
        {
            SqlConnection conn = glob.Connect();
            conn.Open();
            //Removes the bought stock from the market
            string sql = "update USER_CASH set Cash=(Cash - @Cash), [Date]=@Date where UserID=@UserID ";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("Cash", Cost));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();

            //Adds the stock to the Users Account
            sql = "update USER_STOCKS set Amount=(Amount + @Amount), [Date]=@Date where StockID=@StockID and UserID=@UserID ";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("Amount", Amount));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();


            //Add the Transaction to the Log
            sql = "Insert into USER_TRANS_LOG (UserID, StockID, Method, Amount, Cost, [Date]) Values (@UserID, @StockID, @Method, @Amount, @Cost, @Date)";
            cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));
            cmd.Parameters.Add(new SqlParameter("UserID", UserID));
            cmd.Parameters.Add(new SqlParameter("Method", 1));
            //Need to change this to the recalculated value
            cmd.Parameters.Add(new SqlParameter("Amount", Amount));
            cmd.Parameters.Add(new SqlParameter("Cost", Cost));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now.ToString()));
            cmd.ExecuteNonQuery();

            return true;
        }
    }
}
