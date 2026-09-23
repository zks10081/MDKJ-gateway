using getway.Model;
using System.Data;

namespace getway.DB.Pg
{
    internal class GetWayDB
    {

        public static List<GetWayModel> QueryGetWayList()
        {
            List<GetWayModel> list = new List<GetWayModel>();
            string querySql = "select f,server_ip as ip from dm_server_monitor where select_subzone = '网关'";
            DataTable dataTable = PgConnect.SelectAsync(querySql)
                ?? throw new InvalidOperationException("数据库无响应，请检查网关地址与连接");
            foreach (DataRow item in dataTable.Rows)
            {
                list.Add(new GetWayModel()
                {
                    FrameId = int.Parse(item["f"].ToString()),
                    IP = item["ip"].ToString()
                });
            }
            return list;
        }

        /// <summary>
        /// 增加一个网关
        /// </summary>
        public static void AddGatewayModel(GetWayModel gateway)
        {

            string insertSQL = $"insert into dm_server_monitor(server_name, server_ip, create_time, select_subzone, f, digitmap)values('{gateway.IP}（客户端增加）', '{gateway.IP}', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', '网关', {0}, '')";
            PgConnect.ExecuteNonQuery(insertSQL);
        }

        /// <summary>
        /// 删除一个网关
        /// </summary>
        public static void DeleteOne(string ip)
        {
            string deleteSQL = $"delete from dm_server_monitor where server_ip = '{ip}'";
            PgConnect.ExecuteNonQuery(deleteSQL);
        }






    }
}
