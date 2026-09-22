using getway.Util;
using Npgsql;
using System.Data;

namespace getway.DB.Pg
{
    internal class PgConnect
    {

        public static int ExecuteNonQuery(String sqlText)
        {
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(DefaulConfig.DB_STRING + ";Timeout=3");
                DataTable data = null;

                conn.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(sqlText, conn))
                {
                    return command.ExecuteNonQuery();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

            return -1;

        }

        public static DataTable SelectAsync(String sqlText)
        {
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(DefaulConfig.DB_STRING + ";Timeout=3");
                DataTable data = new DataTable();

                conn.Open();
                using (NpgsqlDataAdapter sqldap = new NpgsqlDataAdapter(sqlText, conn))
                {
                    sqldap.Fill(data);
                }
                return data;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }


    }
}
