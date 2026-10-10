using System.Data;

namespace getway.DB.Pg
{
    internal class HotLineDB
    {
        //查询热线信息
        public static Dictionary<string, string> QueryHotLine(string fsp, string ip)
        {
            string querySql = $"select hottime,hotnum from dm_gateway_hotline where ip = '{ip}' and fsp = '{fsp}';";
            DataTable dataTable = PgConnect.SelectAsync(querySql)
                ?? throw new InvalidOperationException("数据库无响应，请检查网关地址与连接");
            string hottime = "5";
            string hotnum = "-";
            if (dataTable.Rows.Count > 0)
            {
                hottime = dataTable.Rows[0]["hottime"].ToString();
                hotnum = dataTable.Rows[0]["hotnum"].ToString();
            }
            return new Dictionary<string, string>()
            {
                {"hottime", hottime},
                {"hotnum", hotnum},
            };
        }

        //更新热线信息
        public static void UpdateHotLine(string fsp, string ip, string number, string hottime, string hotnum)
        {
            string querySql = $"select count(*) from dm_gateway_hotline where ip = '{ip}' and fsp = '{fsp}'";
            DataTable dataTable = PgConnect.SelectAsync(querySql);
            string updateSql = "";
            if (int.Parse(dataTable.Rows[0]["count"].ToString()) > 0)
            {
                updateSql = $"update dm_gateway_hotline set number = '{number}',set hottime = '{hottime}',set hotnum = '{hotnum}' where ip = '{ip}' and fsp = '{fsp}'; ";
            }
            else
            {
                updateSql = $"insert into dm_gateway_hotline(number,hottime,hotnum,ip,fsp) values('{number}','{hottime}','{hotnum}','{ip}','{fsp}');";
            }
            PgConnect.ExecuteNonQuery(updateSql);
        }

    }
}
