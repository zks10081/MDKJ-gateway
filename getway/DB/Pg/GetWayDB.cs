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
            DataTable dataTable = PgConnect.SelectAsync(querySql);
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
