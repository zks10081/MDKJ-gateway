namespace getway.ViewModel
{
    class GetWayViewMode
    {

        private readonly Random _random = new Random();
        public List<GetWayIpInfo> GetWayList { get; set; } = new List<GetWayIpInfo>();

        public GetWayViewMode()
        {
            InitGetWayList();

        }
        public void InitGetWayList()
        {
            for (int i = 0; i < 4; i++)
            {
                String randomIp = $"{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}";
                GetWayList.Add(new GetWayIpInfo() { ip = randomIp });
            }
        }
    }


    class GetWayIpInfo
    {
        public String ip { get; set; }
    }
}
