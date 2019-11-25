using System;
using System.Data.SqlClient;

namespace Migrator
{    
    public static class Class1
    {
        public static void Run(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))  
            {  
                connection.Open();  
                // Do work here; connection closed on following line.  
            };
        }
    }
}
