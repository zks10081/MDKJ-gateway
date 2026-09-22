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




    }
}
