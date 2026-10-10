using System.Data;

namespace getway.DB.Pg
{
    internal class DigitMapDB
    {
        /// <summary>
        /// 根据 IP 查询网关的拨号计划
        /// </summary>
        public static string getDigitMapByIp(string ip)
        {
            string querySql = $"select digitmap from dm_digitmap where ip = '{ip}'";
            DataTable dataTable = PgConnect.SelectAsync(querySql);
            if (dataTable.Rows.Count > 0)
            {
                return dataTable.Rows[0]["digitmap"].ToString();
            }
            return "";
        }

        /// <summary>
        /// 根据 IP 修改网关的拨号计划
        /// </summary>
        public static void UpdateDigitMapByIp(string ip, string digitmap)
        {
            string queryCount = $"select count(*) from dm_digitmap where ip = '{ip}'";
            DataTable dt = PgConnect.SelectAsync(queryCount);
            if (int.Parse(dt.Rows[0]["count"].ToString()) == 0)
            {
                string insertSQL = $"insert into dm_digitmap(ip, digitmap) values('{ip}', '{digitmap}')";
                PgConnect.ExecuteNonQuery(insertSQL);
            }
            else
            {
                string updateSQL = $"update dm_digitmap set digitmap = '{digitmap}' where ip = '{ip}'";
                PgConnect.ExecuteNonQuery(updateSQL);
            }
        }
    }
}
