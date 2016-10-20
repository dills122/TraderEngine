using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraderEngine
{
    class Global_Functions
    {
        public string conString = "Data Source=DESKTOP-Q3GCVQ8\\SQLEXPRESS;Initial Catalog=STOCK_TRADER;Integrated Security=True";



        public string getConString()
        {
            return conString;
        }
        public SqlConnection Connect()
        {
            SqlConnection conn = new SqlConnection(conString);
            return conn;
        }
        public void CloseDB(SqlConnection conn)
        {
            conn.Close();
        }
    }
}
