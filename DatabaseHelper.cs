using Microsoft.Data.SqlClient;

namespace HastaneVeriTabaniSistemi
{
    public static class DatabaseHelper
    {
        private const string ConnectionString =
            @"Server=ALPEREN;
              Database=HastaneVeriTabani;
              Trusted_Connection=True;
              Encrypt=True;
              TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}