using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;

namespace SEIDR.WindowMonitor.Models
{
    public class Dashboard
    {
        public SqlCommand Source;
        public DatabaseConnection connection;
        public int CurrentPage = 0;
        public List<DashboardPage> MyPages = new List<DashboardPage>();
        public Dashboard(SqlCommand source, DatabaseConnection connect)
        {
            Source = source;
            connection = connect;
        }
        ~Dashboard()
        {
            Source.Dispose();
            Source = null;
        }
        public DashboardPage RefreshList()
        {
            MyPages.Clear();
            DataTable dt = new DataTable();
            using (SqlConnection c = new SqlConnection(connection.ConnectionString))
            {
                c.Open();
                Source.Connection = c;
                SqlDataAdapter sda = new SqlDataAdapter(Source);

                sda.Fill(dt);
                c.Close();
            }
            int p = 1;
            foreach (DataRow r in dt.Rows)
            {
                MyPages.Add(new DashboardPage(r, p++));
            }
            if (CurrentPage > MyPages.Count)
                CurrentPage = 0;
            return MyPages[CurrentPage];
        }
        public DashboardPage this[int pageNumber]
        {
            get
            {
                if (pageNumber < 1 || pageNumber > MyPages.Count)
                    throw new Exception("Page out of bounds");
                CurrentPage = pageNumber - 1;
                return MyPages[CurrentPage];
            }
        }
    }
}
