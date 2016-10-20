using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraderEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            Trader tr = new Trader();

            int StockID = 1;

            Global_Functions glob = new Global_Functions();
            SqlConnection conn = glob.Connect();
            conn.Open();
            SqlCommand cmd = new SqlCommand("select StockSymbol,StockPrice,Amount FROM STOCK as S inner join CURRENT_STOCK_PRICES as CS on CS.StockID = S.StockID where S.StockID = @StockID",conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Console.WriteLine("Before Buying Stock");
                Console.WriteLine("Stock Symbol: " + dr["StockSymbol"]);
                Console.WriteLine("Stock Price: " + dr["StockPrice"]);
                Console.WriteLine("Amount: " + dr["Amount"]);
            }
            dr.Close();

            tr.BuyStock(1, StockID, 100);

            cmd = new SqlCommand("select StockSymbol,StockPrice,Amount FROM STOCK as S inner join CURRENT_STOCK_PRICES as CS on CS.StockID = S.StockID where S.StockID = @StockID", conn);
            cmd.Parameters.Add(new SqlParameter("StockID", StockID));

            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Console.WriteLine("After Buying Stock");
                Console.WriteLine("Stock Symbol: " + dr["StockSymbol"]);
                Console.WriteLine("Stock Price: " + dr["StockPrice"]);
                Console.WriteLine("Amount: " + dr["Amount"]);
            }
            dr.Close();

            Console.ReadLine();
        }
    }
}
